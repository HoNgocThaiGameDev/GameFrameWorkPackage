namespace NebulaSoft.IAPStore
{
    public interface IStoreElement
    {
        bool IsActive { get; }
        float Height { get; }

        void Init();
        void PlayAnimation(int elementIndex);
        void OnComplete(SimpleCallback completeCallback);
        void KillTweenCases();
    }
}