using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MinimalStateMashine {

	public class StateMachine<TStateEnum> : StateMachine<TStateEnum, CState<TStateEnum>, CWire<TStateEnum>> {

		public override CState<TStateEnum> CreateState(TStateEnum state)
			=> new CState<TStateEnum>(state);
		public override CWire<TStateEnum> CreateWire(TStateEnum start, TStateEnum goal)
			=> new CWire<TStateEnum>(start, goal);
	}

	public abstract class StateMachine<TStateEnum, TState, TWire> 
		where TState : IState<TStateEnum>
		where TWire : IWire<TStateEnum>
	{

        public event System.Action<System.Exception> Unhandled;
		public event System.Action<TStateEnum?, TStateEnum?> Changed;

        public UpdateSequenceEnum UpdateSeq { get; protected set; }
        public bool Overwrite { get; set; } = false;

        public Dictionary<TStateEnum, TState> _StateMap { get; } = new Dictionary<TStateEnum, TState>();
        public Dictionary<(TStateEnum s0, TStateEnum s1), TWire> _WireMap { get; } = new Dictionary<(TStateEnum, TStateEnum), TWire>();

        public TStateEnum CurrState { get => _Curr != null ? _Curr.Target : default; }
        public IState<TStateEnum> _Curr { get; protected set; }
		public Queue<IState<TStateEnum>> _TransitionQueue { get; protected set; } = new Queue<IState<TStateEnum>>();

		protected int _transitionCallDepth = -1;

		public abstract TState CreateState(TStateEnum state);
		public abstract TWire CreateWire(TStateEnum start, TStateEnum goal);

        public StateBuilder<TStateEnum, TState, TWire> State(TStateEnum state) {
            if (!_StateMap.TryGetValue(state, out var cstate)) cstate = _StateMap[state] = CreateState(state);
            return new StateBuilder<TStateEnum, TState, TWire>(this, cstate);
        }

        public WireBuilder<TStateEnum, TState, TWire> Wire(TStateEnum start, TStateEnum goal) {
            if (!_WireMap.TryGetValue((start, goal), out var cwire))
                cwire = _WireMap[(start, goal)] = CreateWire(start, goal);
            return new WireBuilder<TStateEnum, TState, TWire>(this, cwire);
        }
		public bool Transit(TStateEnum next) {
			var res = _Request(next);

			try {
				var depth = Interlocked.Increment(ref _transitionCallDepth);
				if (depth == 0) _TransitStates();
			} finally {
				Interlocked.Decrement(ref _transitionCallDepth);
			}

			return res;
		}
        public void Update() {
			lock (this) {
				try {
					UpdateSeq = UpdateSequenceEnum.Update;
					_Curr?.Update?.Invoke();
				} catch (System.Exception e) {
					HandleException(e);
				} finally {
					UpdateSeq = default;
				}
			}
        }

		#region methods
		private bool _Request(TStateEnum next) {
			if (UpdateSeq == UpdateSequenceEnum.Exit)
				throw new System.InvalidOperationException("Cannnot call from Exit()");

			if (_Curr != null) {
				if (!_WireMap.TryGetValue((_Curr.Target, next), out var wire)) {
					Debug.LogWarning($"Wire ({_Curr.Target}->{next}) is not declared.");
					return false;
				} else if (wire.Condition != null && !wire.Condition()) {
					Debug.LogWarning($"Wire ({wire}) condition not satisfied.");
					return false;
				}
			}

			if (!_StateMap.TryGetValue(next, out var goal))
				throw new System.InvalidProgramException($"State({next}) not registered");

			_TransitionQueue.Enqueue(goal);
			return true;
		}
		private void _TransitStates() {
			while (_TransitionQueue.Count > 0) {
				try {
					UpdateSeq = UpdateSequenceEnum.Exit;
					_Curr?.Exit?.Invoke();
				} catch(System.Exception e) {
					HandleException(e);
				}

				var prev = _Curr;
				_Curr = _TransitionQueue.Dequeue();

				try {
					UpdateSeq = UpdateSequenceEnum.Enter;
					_Curr?.Enter?.Invoke();
				} catch(System.Exception e) {
					HandleException(e);
				}

				Changed?.Invoke(prev != null ? prev.Target : default, _Curr != null ? _Curr.Target : default);
			}
		}
		private void HandleException(System.Exception e) {
			if (_Curr != null && _Curr.Handle != null)
				_Curr.Handle.Invoke(e);
			else
				Unhandled?.Invoke(e);
		}
		#endregion
	}

	#region classes
	public enum UpdateSequenceEnum { Init = 0, Enter, Update, Exit }
	public interface IState<TStateEnum> {
		TStateEnum Target { get; }
		System.Action Enter { get; set; }
		System.Action Update { get; set; }
		System.Action Exit { get; set; }
		System.Action<System.Exception> Handle { get; set; }
	}
	public interface IWire<TStateEnum> {
		TStateEnum Start { get; }
		TStateEnum Goal { get; }
		System.Func<bool> Condition { get; set; }
	}

	public class CState<TStateEnum> : IState<TStateEnum> {
		public TStateEnum Target { get; }
		public System.Action Enter { get; set; }
		public System.Action Update { get; set; }
		public System.Action Exit { get; set; }
		public System.Action<System.Exception> Handle { get; set; }

		public CState(TStateEnum target) => this.Target = target;
		public override string ToString() => $"<{GetType().Name}: {Target}>";
	}
	public class CWire<TStateEnum> : IWire<TStateEnum> {
		public TStateEnum Start { get; }
		public TStateEnum Goal { get; }
		public System.Func<bool> Condition { get; set; }

		public CWire(TStateEnum start, TStateEnum goal) {
			this.Start = start;
			this.Goal = goal;
		}
		public override string ToString() => $"<{GetType().Name}: ({Start},{Goal})>";
	}

	public class StateBuilder<TStateEnum, TState, TWire>
		where TState : IState<TStateEnum>
		where TWire : IWire<TStateEnum>
	{
		public StateMachine<TStateEnum, TState, TWire> fsm { get; }
		public TState state { get; }

		public StateBuilder(StateMachine<TStateEnum, TState, TWire> fsm, TState state) {
			this.fsm = fsm;
			this.state = state;
		}
		public StateBuilder<TStateEnum, TState, TWire> Enter(System.Action f) {
			state.Enter = f;
			return this;
		}
		public StateBuilder<TStateEnum, TState, TWire> Update(System.Action f) {
			state.Update = f;
			return this;
		}
		public StateBuilder<TStateEnum, TState, TWire> Exit(System.Action f) {
			state.Exit = f;
			return this;
		}
		public StateBuilder<TStateEnum, TState, TWire> Handle(System.Action<System.Exception> f) {
			state.Handle = f;
			return this;
		}
	}
	public class WireBuilder<TStateEnum, TState, TWire>
		where TState : IState<TStateEnum>
		where TWire : IWire<TStateEnum> 
	{
		public StateMachine<TStateEnum, TState, TWire> fsm { get; }
		public TWire wire { get; }

		public WireBuilder(StateMachine<TStateEnum, TState, TWire> fsm, TWire wire) {
			this.fsm = fsm;
			this.wire = wire;
		}

		public WireBuilder<TStateEnum, TState, TWire> Condition(System.Func<bool> f) {
			wire.Condition = f;
			return this;
		}
		public WireBuilder<TStateEnum, TState, TWire> Wire(TStateEnum next) {
			return fsm.Wire(wire.Goal, next);
		}
	}
	#endregion
}
