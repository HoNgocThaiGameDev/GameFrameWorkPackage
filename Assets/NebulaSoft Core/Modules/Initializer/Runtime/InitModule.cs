using UnityEngine;
using System.Collections;

namespace NebulaSoft
{
    public abstract class InitModule : ScriptableObject
    {
        public abstract string ModuleName { get; }

        /// <summary>Optional startup work completed before any module creates its components.</summary>
        public virtual IEnumerator Prepare() => null;

        public abstract void CreateComponent();
        public virtual System.Type[] Dependencies => System.Array.Empty<System.Type>();
        public virtual void ValidateConfiguration() { }
        public virtual void Shutdown() { }
    }
}
