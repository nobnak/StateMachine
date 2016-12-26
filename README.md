# FSM(Finite State Machine) for Unity

## Usage
######Declare states
```cs
enum State { Idol, Walk, Run, Sleep, Eat }
```
######Implement states
```cs
FSM<State> fsm = new FSM<State>(thisMonoBehaviour);
fsm.State(State.Idol).Enter((fsm) => { ... }).Update((fsm) => { ... }).Exit((fsm)=> { ... });
fsm.State(State.Walk) ...
```
######Start FSM
```cs
fsm.Goto(State.Idol);
```

## Git Submodule
######Add this as a submodule
```
git submodule add https://github.com/nobnak/StateMachine.git Assets/Packages/StateMachine
```
