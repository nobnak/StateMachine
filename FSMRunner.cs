using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace StateMachine {

    public class FSMRunner : MonoBehaviour {
        List<IFSM> _fsmlist = new List<IFSM>();

        protected FSMRunner() {}

        public FSMRunner Add(IFSM fsm){
            _fsmlist.Add (fsm);
            return this;
        }
        public FSMRunner Remove(IFSM fsm) {
            _fsmlist.Remove (fsm);
            return this;
        }

        void Update() {
            foreach (var fsm in _fsmlist)
                if (fsm != null)
                    fsm.Update ();
        }
    }

    public interface IFSM {
        void Update();
    }
    public class FSM<T> : System.IDisposable, IFSM where T : struct {
        Dictionary<T, State> _stateMap = new Dictionary<T, State>();

        FSMRunner _runner;
        State _current;
        State _last;

        public FSM(MonoBehaviour target) {
            if ((_runner = target.GetComponent<FSMRunner> ()) == null)
                _runner = target.gameObject.AddComponent<FSMRunner> ();
            _runner.Add (this);
        }

        public State Ensure(T name) {
            State state;
            if (!TryGetState (name, out state))
                state = _stateMap [name] = new State (name);
            return state;
        }
        public State CurrentState { get { return _current; } }
        public State LastState { get { return _last; } }

        public FSM<T> Goto(T nextStateName) {
            State next;
            if (!TryGetState (nextStateName, out next) || next == null) {
                Debug.LogWarningFormat ("There is no state {0}", nextStateName);
                return this;
            }
            _last = _current;
            _current = next;
            if (_last != null)
                _last.ExitState (this);
            _current.EnterState (this);
            return this;
        }
        public void Update() {
            if (_current != null)
                _current.UpdateState (this);
        }
        public bool TryGetState(T name, out State state) {
            return _stateMap.TryGetValue (name, out state);
        }

        #region IDisposable implementation
        public void Dispose () {
            if (_runner != null) {
                _runner.Remove (this);
                _runner = null;
            }
        }
        #endregion

        public class State { 
            public readonly T name;

            System.Action<FSM<T>> _enter;
            System.Action<FSM<T>> _update;
            System.Action<FSM<T>> _exit;

            public State(T name) {
                this.name = name;
            }

            public State Enter(System.Action<FSM<T>> enter) {
                this._enter = enter;
                return this;
            }
            public State Update(System.Action<FSM<T>> update) {
                this._update = update;
                return this;
            }
            public State Exit(System.Action<FSM<T>> exit) {
                this._exit = exit;
                return this;
            }

            public State EnterState(FSM<T> fsm) {
                if (_enter != null)
                    _enter (fsm);
                return this;
            }
            public State UpdateState(FSM<T> fsm) {
                if (_update != null)
                    _update (fsm);
                return this;
            }
            public State ExitState(FSM<T> fsm) {
                if (_exit != null)
                    _exit (fsm);
                return this;
            }
        }
    }
}
