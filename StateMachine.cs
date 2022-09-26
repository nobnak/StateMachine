using System;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace StateMashineSys {

    public class StateMachine<TStateEnum> {

        public event System.Action<System.Exception> Unhandled;

        public UpdateSequenceEnum UpdateSeq { get; protected set; }
        public bool Overwrite { get; set; } = false;

        public Dictionary<TStateEnum, CState> _StateMap { get; } = new Dictionary<TStateEnum, CState>();
        public Dictionary<(TStateEnum s0, TStateEnum s1), CWire> _WireMap { get; } = new Dictionary<(TStateEnum, TStateEnum), CWire>();

        public TStateEnum CurrState { get => _Curr != null ? _Curr.Target : default; }
        public TStateEnum NextState { get => _Next != null ? _Next.Target : default; }
        public IState _Curr { get; protected set; }
        public IState _Next { get; protected set; }

        public StateBuilder<CState> State(TStateEnum state) {
            if (!_StateMap.TryGetValue(state, out var cstate)) cstate = _StateMap[state] = new CState(state);
            return new StateBuilder<CState>(cstate);
        }

        public WireBuilder<CWire> Wire(TStateEnum start, TStateEnum goal) {
            if (!_WireMap.TryGetValue((start, goal), out var cwire))
                cwire = _WireMap[(start, goal)] = new CWire(start, goal);
            return new WireBuilder<CWire>(cwire);
        }

        public bool Change(TStateEnum next) {
            if (UpdateSeq == UpdateSequenceEnum.Exit)
                throw new InvalidOperationException("Cannnot call from Exit()");

            if (_Next != null && !Overwrite)
                return false;

            if (_Curr != null 
                && _WireMap.TryGetValue((_Curr.Target, next), out var wire)
                && (wire.Condition == null || wire.Condition()))
                return false;

            if (!_StateMap.TryGetValue(next, out var goal))
                throw new InvalidProgramException($"State({next}) not registered");

            _Next = goal;
            return true;
        }
        public void Update() {
            lock (this) {
                try {
                    if (_Next != null) {
                        do {
                            UpdateSeq = UpdateSequenceEnum.Exit;
                            _Curr?.Exit?.Invoke();

                            _Curr = _Next;
                            _Next = null;

                            UpdateSeq = UpdateSequenceEnum.Enter;
                            _Curr?.Enter?.Invoke();
                        } while (_Next != null);
                    } else {
                        UpdateSeq = UpdateSequenceEnum.Update;
                        _Curr?.Update?.Invoke();
                    }
                } catch (System.Exception e) {
                    if (_Curr != null && _Curr.Handle != null)
                        _Curr.Handle.Invoke(e);
                    else
                        Unhandled?.Invoke(e);
                } finally {
                    UpdateSeq = default;
                }
            }
        }

        #region classes
        public enum UpdateSequenceEnum { Init = 0, Enter, Update, Exit }
        public interface IState {
            TStateEnum Target { get; }
            System.Action Enter { get; set; }
            System.Action Update { get; set; }
            System.Action Exit { get; set; }
            System.Action<System.Exception> Handle { get; set; }
        }
        public interface IWire {
            TStateEnum Start { get; }
            TStateEnum Goal { get; }
            System.Func<bool> Condition { get; set; }
        }

        public class CState : IState {
            public TStateEnum Target { get; }
            public System.Action Enter { get; set; }
            public System.Action Update { get; set; }
            public System.Action Exit { get; set; }
            public System.Action<System.Exception> Handle { get; set; }

            public CState(TStateEnum target) => this.Target = target;
            public override string ToString() => $"<{GetType().Name}: {Target}>";
        }
        public class CWire : IWire {
            public TStateEnum Start { get; }
            public TStateEnum Goal { get; }
            public System.Func<bool> Condition { get; set; }

            public CWire(TStateEnum start, TStateEnum goal) {
                this.Start = start;
                this.Goal = goal;
            }
            public override string ToString() => $"<{GetType().Name}: ({Start},{Goal})>";
        }

        public class StateBuilder<TState> where TState : IState {
            public TState state { get; }

            public StateBuilder(TState state) {
                this.state = state;
            }
            public StateBuilder<TState> Enter(System.Action f) {
                state.Enter = f;
                return this;
            }
            public StateBuilder<TState> Update(System.Action f) {
                state.Update = f;
                return this;
            }
            public StateBuilder<TState> Exit(System.Action f) {
                state.Exit = f;
                return this;
            }
            public StateBuilder<TState> Handle(System.Action<System.Exception> f) {
                state.Handle = f;
                return this;
            }

            public static implicit operator TState(StateBuilder<TState> sb) => sb.state;
        }
        public class WireBuilder<TWire> where TWire : IWire {
            public TWire wire { get; }

            public WireBuilder(TWire wire) {
                this.wire = wire;
            }

            public WireBuilder<TWire> Condition(System.Func<bool> f) {
                wire.Condition = f;
                return this;
            }

            public static implicit operator TWire(WireBuilder<TWire> wb) => wb.wire;
        }
        #endregion
    }
}
