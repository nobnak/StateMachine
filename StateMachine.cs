using System;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace StateMashineSys {

    public class StateMachine<TStateEnum> {

        public event System.Action<System.Exception> Unhandled;

        public Dictionary<TStateEnum, CState> StateMap { get; } = new Dictionary<TStateEnum, CState>();
        public Dictionary<(TStateEnum s0, TStateEnum s1), CWire> WireMap { get; } = new Dictionary<(TStateEnum, TStateEnum), CWire>();

        public UpdateSequenceEnum UpdateSeq { get; protected set; }
        public bool Overwrite { get; set; } = false;
        public IState CurrState { get; protected set; }
        public IState NextState { get; protected set; }

        public StateBuilder<CState> State(TStateEnum state) {
            if (!StateMap.TryGetValue(state, out var cstate)) cstate = StateMap[state] = new CState(state);
            return new StateBuilder<CState>(cstate);
        }

        public WireBuilder<CWire> Wire(TStateEnum start, TStateEnum goal) {
            if (!WireMap.TryGetValue((start, goal), out var cwire))
                cwire = WireMap[(start, goal)] = new CWire(start, goal);
            return new WireBuilder<CWire>(cwire);
        }

        public bool Change(TStateEnum next) {
            if (UpdateSeq == UpdateSequenceEnum.Exit)
                throw new InvalidOperationException("Cannnot call from Exit()");

            if (NextState != null && !Overwrite)
                return false;

            if (CurrState != null 
                && WireMap.TryGetValue((CurrState.Target, next), out var wire)
                && (wire.Condition == null || wire.Condition()))
                return false;

            if (!StateMap.TryGetValue(next, out var goal))
                throw new InvalidProgramException($"State({next}) not registered");

            NextState = goal;
            return true;
        }
        public void Update() {
            lock (this) {
                try {
                    if (NextState != null) {
                        do {
                            UpdateSeq = UpdateSequenceEnum.Exit;
                            CurrState?.Exit?.Invoke();

                            CurrState = NextState;
                            NextState = null;

                            UpdateSeq = UpdateSequenceEnum.Enter;
                            CurrState?.Enter?.Invoke();
                        } while (NextState != null);
                    } else {
                        UpdateSeq = UpdateSequenceEnum.Update;
                        CurrState?.Update?.Invoke();
                    }
                } catch (System.Exception e) {
                    if (CurrState != null && CurrState.Handle != null)
                        CurrState.Handle.Invoke(e);
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
