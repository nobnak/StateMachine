# Minimal State Machine for Unity

- [Sample Project](https://github.com/nobnak/StateMachineTest)

## Example
```cs
enum StateEnum { Init = 0, Started, Finished }
StateMachine<StateEnum> fsm;

void OnEnable() {
    fsm = new StateMachine<StateEnum>();

    // States
    fsm.State(StateEnum.Init)
      .Update(() => {
          if (clicked)
              fsm.Transit(StateEnum.Started);
      });

    fsm.State(StateEnum.Started)
      .Enter(() => Debug.Log("Started"))
      .Update(() => fsm.Transit(StateEnum.Finished));

    fsm.State(StateEnum.Finished)
      .Enter(() => Debug.Log("Finished"));
      
    // Wire
    fsm.Wire(StateEnum.Init, StateEnum.Started)
      .Wire(StateEnum.Finished);
    
    // Entry state
    fsm.Transit(StateEnum.Init);
}
void Update() {
    fsm.Update();
}
```
