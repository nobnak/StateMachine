using System;
using System.Collections.Generic;
namespace StateMashineSys {

    public class StateMachine<TStateEnum> where TStateEnum : IComparable<TStateEnum> {

        public event System.Action<System.Exception> Unhandled;

        public Dictionary<TStateEnum, IState> StateMap { get; } = new Dictionary<TStateEnum, IState>();
        public Dictionary<TStateEnum, IWire> WireMap { get; } = new Dictionary<TStateEnum, IWire>();

        public UpdateSequenceEnum UpdateSeq { get; protected set; }
        public bool Overwrite { get; set; } = false;
        public IState CurrState { get; protected set; }
        public IState NextState { get; protected set; }

        public IState State(TStateEnum state) =>
            !StateMap.TryGetValue(state, out var cstate)
                ? cstate = StateMap[state] = new CState()
                : cstate;

        public IWire Wire(TStateEnum start, TStateEnum goal) =>
            !WireMap.TryGetValue(start, out var wire)
                ? wire = WireMap[start] = new CWire() { Start = start, Goal = goal }
                : wire;

        public bool Change(TStateEnum next) {
            if (UpdateSeq == UpdateSequenceEnum.Exit)
                throw new InvalidOperationException("Cannnot call from Exit()");
            
            if ((NextState != null && !Overwrite)
                || !WireMap.TryGetValue(next, out var wire)
                || !wire.Condition())
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
            System.Action Enter { get; set; }
            System.Action Update { get; set; }
            System.Action Exit { get; set; }
            System.Action<System.Exception> Handle { get; set; }
        }
        public interface IWire {
            TStateEnum Start { get; set; }
            TStateEnum Goal { get; set; }
            System.Func<bool> Condition { get; set; }
        }

        public class CState : IState {
            public System.Action Enter { get; set; }
            public System.Action Update { get; set; }
            public System.Action Exit { get; set; }
            System.Action<System.Exception> Handle { get; set; }
        }
        public class CWire : IWire {
            public TStateEnum Start { get; set; }
            public TStateEnum Goal { get; set; }
            public System.Func<bool> Condition { get; set; }
        }
        #endregion
    }
}
