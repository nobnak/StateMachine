# FSM for Unity

## Usage
######Add this as a submodule
```
git submodule add git@github.com:nobnak/StateMachine.git Assets/Packages/StateMachine
```
######Declare states
```cs
enum State { Idol, Walk, Run, Sleep, Eat }
```
######Implement states
```cs
FSM<State> fsm = new FSM<State>(thisMonoBehaviour);
fsm.State(State.Idol).Enter((fsm) => { ... }).Update((fsm) => { ... }).Exit((fsm)=> { ... });
```
######Start FSM
```cs
fsm.Goto(State.Idol);
```
