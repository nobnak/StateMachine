using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StateMashineSys {

    public class StateMachine<TStateEnum> where TStateEnum : IComparable<TStateEnum> {

        public Dictionary<TStateEnum, IState> StateMap { get; } = new Dictionary<TStateEnum, IState>();
        public Dictionary<TStateEnum, IWire> WireMap { get; } = new Dictionary<TStateEnum, IWire>();

        public IState State(TStateEnum state) =>
            !StateMap.TryGetValue(state, out var cstate)
                ? cstate = StateMap[state] = new CState()
                : cstate;

        public IWire Wire(TStateEnum start, TStateEnum goal) =>
            !WireMap.TryGetValue(start, out var wire)
                ? wire = WireMap[start] = new CWire() { Start = start, Goal = goal }
                : wire;

        public bool Change(TStateEnum next) {
            return false;
        }
        public void Update() {

        }

        #region classes
        public interface IState {
            System.Action Enter { get; set; }
            System.Action Update { get; set; }
            System.Action Exit { get; set; }
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
        }
        public class CWire : IWire {
            public TStateEnum Start { get; set; }
            public TStateEnum Goal { get; set; }
            public System.Func<bool> Condition { get; set; }
        }
        }
        #endregion
    }



}
