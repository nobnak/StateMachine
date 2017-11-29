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
    public class FSM<T> : System.IDisposable, IFSM where T : struct, System.IComparable {
        public enum TransitionModeEnum { Queued = 0, Immediate }

        Dictionary<T, StateData> _stateMap = new Dictionary<T, StateData>();

        bool _enabled;
        FSMRunner _runner;
        StateData _current;
        StateData _last;
        bool _nextStateQueued;
        T _nextStateName;
        TransitionModeEnum transitionMode;

        public FSM(MonoBehaviour target, TransitionModeEnum transitionMode) {
            if ((_runner = target.GetComponent<FSMRunner> ()) == null)
                _runner = target.gameObject.AddComponent<FSMRunner> ();
            _runner.Add (this);
            _enabled = true;
            this.transitionMode = transitionMode;
        }
        public FSM(MonoBehaviour target):this(target, TransitionModeEnum.Queued) { }

            public StateData State(T name) {
            StateData state;
            if (!TryGetState (name, out state))
                state = _stateMap [name] = new StateData (name);
            return state;
        }
        public T Current { get { return (_current == null ? default(T) : _current.name); } }
        public T Last { get { return (_last == null ? default(T) : _last.name); } }
        public bool Enabled {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public FSM<T> Goto(T nextStateName) {
            switch (transitionMode) {
                default:
                    return GotoQueued(nextStateName);
                case TransitionModeEnum.Immediate:
                    return GotoImmediate(nextStateName);
            }
        }
        public FSM<T> GotoQueued(T nextStateName) { 
            if (_current != null && _current.name.CompareTo(nextStateName) == 0) {
                return this;
            }
            if (_nextStateQueued)
                Debug.LogFormat ("The next state is already queued {0}", nextStateName);
            _nextStateQueued = true;

            _nextStateName = nextStateName;
            return this;
        }
        public FSM<T> GotoImmediate(T nextStateName) {
            StateData next;
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
            if (!_enabled)
                return;
            if (_nextStateQueued) {
                _nextStateQueued = false;
                GotoImmediate (_nextStateName);
            }
            if (_current != null)
                _current.UpdateState (this);
        }
        public bool TryGetState(T name, out StateData state) {
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

        public class StateData { 
            public readonly T name;

            System.Action<FSM<T>> _enter;
            System.Action<FSM<T>> _update;
            System.Action<FSM<T>> _exit;

            public StateData(T name) {
                this.name = name;
            }

            public StateData Enter(System.Action<FSM<T>> enter) {
                this._enter = enter;
                return this;
            }
            public StateData Update(System.Action<FSM<T>> update) {
                this._update = update;
                return this;
            }
            public StateData Exit(System.Action<FSM<T>> exit) {
                this._exit = exit;
                return this;
            }

            public StateData EnterState(FSM<T> fsm) {
                if (_enter != null)
                    _enter (fsm);
                return this;
            }
            public StateData UpdateState(FSM<T> fsm) {
                if (_update != null)
                    _update (fsm);
                return this;
            }
            public StateData ExitState(FSM<T> fsm) {
                if (_exit != null)
                    _exit (fsm);
                return this;
            }
        }
    }
}
