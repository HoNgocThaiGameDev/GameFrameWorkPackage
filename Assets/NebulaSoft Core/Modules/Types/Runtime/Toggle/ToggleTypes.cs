using UnityEngine;
using UnityEngine.Serialization;

namespace NebulaSoft
{
    [System.Serializable]
    public abstract class ToggleType<T>
    {
        [SerializeField] bool enabled;
        public bool Enabled => enabled;

        [FormerlySerializedAs("newValue")]
        [SerializeField] T value;
        public T Value => value;

        public ToggleType(bool enabled, T value)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public T Handle(T value)
        {
            if (enabled)
            {
                return this.value;
            }
            else
            {
                return value;
            }
        }
    }

    [System.Serializable]
    public class BoolToggle : ToggleType<bool>
    {
        public BoolToggle(bool enabled, bool value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class FloatToggle : ToggleType<float>
    {
        public FloatToggle(bool enabled, float value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class IntToggle : ToggleType<int>
    {
        public IntToggle(bool enabled, int value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class LongToggle : ToggleType<long>
    {
        public LongToggle(bool enabled, long value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class StringToggle : ToggleType<string>
    {
        public StringToggle(bool enabled, string value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class DoubleToggle : ToggleType<double>
    {
        public DoubleToggle(bool enabled, double value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class ObjectToggle : ToggleType<GameObject>
    {
        public ObjectToggle(bool enabled, GameObject value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class AudioClipToggle : ToggleType<AudioClip>
    {
        public AudioClipToggle(bool enabled, AudioClip value) : base(enabled, value) { }
    }

    [System.Serializable]
    public class Vector3Toggle : ToggleType<Vector3>
    {
        public Vector3Toggle(bool enabled, Vector3 value) : base(enabled, value) { }
    }
}