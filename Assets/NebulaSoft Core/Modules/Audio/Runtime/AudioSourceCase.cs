using UnityEngine;

namespace NebulaSoft
{
    public class AudioSourceCase
    {
        private AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        public bool IsPlaying => audioSource.isPlaying;

        private AudioType audioType;
        private float clipVolume;

        private GameObject gameObject;
        public GameObject GameObject => gameObject;

        public AudioSourceCase()
        {
            gameObject = new GameObject("[AUDIO SOURCE OBJECT]");

            if (Application.isPlaying) GameObject.DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            AudioController.ApplyDefaultSettings(ref audioSource);
        }

        public void Play(AudioClip audioClip, float clipVolume, AudioType type = AudioType.Sound)
        {
            audioType = type;
            this.clipVolume = clipVolume;

            audioSource.clip = audioClip;
            audioSource.volume = clipVolume * AudioController.GetVolume(audioType);

            audioSource.Play();
        }

        public void OverrideVolume(AudioType type, float volume)
        {
            if (!audioSource.isPlaying || audioType != type) return;

            audioSource.volume = volume * clipVolume;
        }
    }
}
