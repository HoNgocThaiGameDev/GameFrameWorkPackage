using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class AudioClipHandler
    {
        [SerializeField] AudioType audioType;

        [Slider(0.0f, 1.0f)]
        [SerializeField] float clipVolume = 1.0f;

        [SerializeField] bool advancedSettings;

        [ShowIf("advancedSettings")]
        [SerializeField] float minDelay;

        [ShowIf("advancedSettings")]
        [SerializeField] bool dynamicPitch;
        [ShowIf("advancedSettings")]
        [SerializeField] DuoFloat pitchRange = new DuoFloat(0.8f, 1.2f);
        [ShowIf("advancedSettings")]
        [SerializeField] int pitchSteps = 10;
        [ShowIf("advancedSettings")]
        [SerializeField] float pitchResetTime = 1.0f;

        [ShowIf("advancedSettings")]
        [SerializeField] AudioSource customAudioSource;

        [System.NonSerialized]
        private float lastPlayedTime = float.MinValue;

        private int currentPitchStep;
        private float lastPitchStepTime;

        private TweenCase volumeCase;

        public AudioClipHandler(AudioType audioType, float clipVolume)
        {
            this.audioType = audioType;
            this.clipVolume = clipVolume;

            this.advancedSettings = false;

            Init();
        }

        public AudioClipHandler(AudioType audioType, float clipVolume, float minDelay, AudioSource customAudioSource = null)
        {
            this.audioType = audioType;
            this.clipVolume = clipVolume;
            this.minDelay = minDelay;

            this.customAudioSource = customAudioSource;

            this.advancedSettings = true;

            Init();
        }

        public AudioClipHandler(AudioType audioType, float clipVolume, float minDelay, DuoFloat pitchRange, int pitchSteps, float pitchResetTime, AudioSource customAudioSource = null)
        {
            this.audioType = audioType;
            this.clipVolume = clipVolume;
            this.minDelay = minDelay;
            this.pitchRange = pitchRange;
            this.pitchSteps = pitchSteps;
            this.pitchResetTime = pitchResetTime;
            this.customAudioSource = customAudioSource;

            this.advancedSettings = true;
            this.dynamicPitch = true;

            Init();
        }

        public void Init()
        {
            AudioController.VolumeChanged += OnVolumeChanged;

            lastPlayedTime = float.MinValue;
        }

        public void Unload()
        {
            AudioController.VolumeChanged -= OnVolumeChanged;

            volumeCase.KillActive();
        }

        private void OnVolumeChanged(AudioType type, float volume)
        {
            if (!advancedSettings) return;
            if (customAudioSource == null) return;
            if (audioType != type) return;

            customAudioSource.volume = volume * clipVolume;
        }

        public void Play(AudioClip audioClip)
        {
            float pitch = 1.0f;

            if(advancedSettings)
            {
                if (Time.time < lastPlayedTime + minDelay && Time.time >= lastPlayedTime)
                {
                    return;
                }

                lastPlayedTime = Time.time;

                if(dynamicPitch)
                {
                    currentPitchStep++;

                    if(Time.time > lastPitchStepTime)
                        currentPitchStep = 0;

                    lastPitchStepTime = Time.time + pitchResetTime;

                    pitch = pitchRange.Lerp((float)currentPitchStep / pitchSteps);
                }

                if(customAudioSource != null)
                {
                    customAudioSource.clip = audioClip;
                    customAudioSource.volume = AudioController.GetVolume(audioType) * clipVolume;
                    customAudioSource.pitch = pitch;

                    customAudioSource.Play();

                    return;
                }
            }

            AudioController.PlaySound(audioClip, clipVolume, pitch);
        }

        public void Play(AudioClip audioClip, Vector3 position)
        {
            float pitch = 1.0f;

            if (advancedSettings)
            {
                if (Time.time < lastPlayedTime + minDelay) return;

                lastPlayedTime = Time.time;

                if (dynamicPitch)
                {
                    currentPitchStep++;

                    if (Time.time > lastPitchStepTime)
                        currentPitchStep = 0;

                    lastPitchStepTime = Time.time + pitchResetTime;

                    pitch = pitchRange.Lerp((float)currentPitchStep / pitchSteps);
                }

                if (customAudioSource != null)
                {
                    customAudioSource.clip = audioClip;
                    customAudioSource.volume = AudioController.GetVolume(audioType) * clipVolume;
                    customAudioSource.pitch = pitch;

                    customAudioSource.Play();

                    return;
                }
            }

            AudioController.PlaySound(audioClip, position, clipVolume, pitch);
        }

        public TweenCase DoVolume(float volume, float duration)
        {
            if (advancedSettings && customAudioSource != null)
            {
                volumeCase.KillActive();
                volumeCase = Tween.DoFloat(clipVolume, volume, duration, (value) =>
                {
                    clipVolume = value;
                    customAudioSource.volume = AudioController.GetVolume(audioType) * clipVolume;
                });

                return volumeCase;
            }

            return null;
        }

        public void StopPlaying()
        {
            if(advancedSettings && customAudioSource != null)
            {
                customAudioSource.Stop();
            }
        }
    }
}