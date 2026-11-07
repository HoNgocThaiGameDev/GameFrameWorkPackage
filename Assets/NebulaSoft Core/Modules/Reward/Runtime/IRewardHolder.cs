namespace NebulaSoft
{
    public interface IRewardHolder
    {
        bool IsDirty { get; }
        void MarkAsDirty();
    }
}