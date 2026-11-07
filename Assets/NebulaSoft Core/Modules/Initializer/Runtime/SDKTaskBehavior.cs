namespace NebulaSoft
{
    public interface ISDKTaskBehavior
    {
        public LoadingTask Task { get; }

        public void Init(SDKInitializer initializer);
    }
}
