using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class SimpleLongSave : ISaveObject
    {
        [SerializeField] long value;
        public virtual long Value
        {
            get => value; set
            {
                this.value = value;
            }
        }

        public virtual void Flush() { }
    }
}