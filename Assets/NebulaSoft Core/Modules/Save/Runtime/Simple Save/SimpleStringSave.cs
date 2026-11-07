using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class SimpleStringSave : ISaveObject
    {
        [SerializeField] string value;
        public virtual string Value
        {
            get => value;
            set => this.value = value;
        }

        public virtual void Flush() { }
    }
}