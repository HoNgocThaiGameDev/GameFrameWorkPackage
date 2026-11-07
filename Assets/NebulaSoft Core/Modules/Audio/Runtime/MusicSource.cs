using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    [RequireComponent(typeof(AudioSource))]
    public class MusicSource : MonoBehaviour
    {
        private const float DEFAULT_FADE_DURATION = 0.3f;

        private static MusicSource defaultMusicSource;

        private static MusicSource activeMusicSource;
        public static MusicSource ActiveMusicSource => activeMusicSource;

        [SerializeField] bool activateAutomatically = false;

        private AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        private TweenCase fadeTweenCase;

        private float volumeMultiplier = 1.0f;
        private bool initialized;

        private void Start()
        {
            if(activateAutomatically)
            {
                Init();
                Activate();
            }
        }

        public void Init()
        {
            if (initialized) return;
            initialized = true;
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;

            volumeMultiplier = audioSource.volume;

            audioSource.volume = AudioController.GetVolume(AudioType.Music) * volumeMultiplier;

            AudioController.VolumeChanged += OnVolumeChanged;
        }

        public void Unload()
        {
            initialized = false;
            fadeTweenCase.KillActive();
            AudioController.VolumeChanged -= OnVolumeChanged;
        }

        private void OnDestroy()
        {
            Unload();
        }

        public void SetAsDefault()
        {
            defaultMusicSource = this;
        }

        public void Activate()
        {
            if (activeMusicSource == this) return;

            if(activeMusicSource != null)
            {
                activeMusicSource.audioSource.volume = 0.0f;
                activeMusicSource.audioSource.Stop();
            }

            audioSource.Play();

            Fade(1.0f, DEFAULT_FADE_DURATION);

            activeMusicSource = this;
        }

        public void SetVolume(float volume)
        {
            audioSource.volume = volume * AudioController.GetVolume(AudioType.Music) * volumeMultiplier;
        }

        public void Fade(float value, float duration, float delay = 0, SimpleCallback onComplete = null)
        {
            fadeTweenCase.KillActive();

            fadeTweenCase = Tween.DoFloat(audioSource.volume, value, duration, (value) =>
            {
                audioSource.volume = value * AudioController.GetVolume(AudioType.Music) * volumeMultiplier;
            }, delay).OnComplete(onComplete);
        }

        private void OnVolumeChanged(AudioType audioType, float volume)
        {
            if (audioType != AudioType.Music) return;

            audioSource.volume = volume * volumeMultiplier;
        }

        public bool IsActive()
        {
            return activeMusicSource == this;
        }

        public static void ActivateDefault()
        {
            if (activeMusicSource == defaultMusicSource) return;

            defaultMusicSource.Activate();
        }

        private static void UnloadStatic()
        {
            defaultMusicSource = null;
            activeMusicSource = null;
        }
    }
}
