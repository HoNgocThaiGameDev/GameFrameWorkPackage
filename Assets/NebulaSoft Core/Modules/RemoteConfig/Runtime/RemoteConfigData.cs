using Newtonsoft.Json;

namespace NebulaSoft
{
    public abstract class RemoteConfigData
    {
        [JsonIgnore]
        public abstract string Key { get; }

        [JsonIgnore]
        public virtual bool PrettyPrint { get; } = false;

        /// <summary>Return false when this override cannot be used safely by its consumer.</summary>
        public virtual bool Validate() => true;
    }
}
