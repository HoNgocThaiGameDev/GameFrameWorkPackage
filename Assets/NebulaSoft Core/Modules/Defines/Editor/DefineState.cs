namespace NebulaSoft
{
    [System.Serializable]
    public class DefineState
    {
        public readonly string Define;
        public readonly bool State;

        public DefineState(string define, bool state)
        {
            Define = define;
            State = state;
        }
    }
}