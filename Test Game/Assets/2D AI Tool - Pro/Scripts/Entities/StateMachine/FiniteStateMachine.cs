namespace AI2DTool
{
    public class FiniteStateMachine
    {
        public EntityState CurrentState { get; private set; }
        public EntityState PreviouState { get; private set; }

        public void Initialize(EntityState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void ChangeState(EntityState newState)
        {
            PreviouState = CurrentState;

            CurrentState.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }
    }
}