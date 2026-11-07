namespace NebulaSoft
{
    public interface IOverlayPanel
    {
        public bool IsActive { get; }

        public void Init();
        public void Clear();

        public void Show(float duration, SimpleCallback onCompleted);
        public void Hide(float duration, SimpleCallback onCompleted);

        public void SetState(bool state);
        public void SetLoadingState(bool state);
    }
}
