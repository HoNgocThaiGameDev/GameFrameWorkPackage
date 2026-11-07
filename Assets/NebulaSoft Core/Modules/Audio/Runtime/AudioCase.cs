using System.Collections;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class AudioCase
    {
        public readonly AudioSource Source;
        public readonly AudioType Type;

        public AudioCallback onAudioEnded;
        private Coroutine endCoroutine;

        public AudioCase(AudioClip clip, AudioSource source, AudioType type, AudioCallback callback = null)
        {
            Source = source;
            Source.clip = clip;

            Type = type;

            onAudioEnded = callback;
        }

        public AudioCase OnComplete(AudioCallback callback)
        {
            onAudioEnded = callback;

            endCoroutine = Tween.InvokeCoroutine(OnAudioEndCoroutine(Source.clip.length));

            return this;
        }

        public virtual void Play()
        {
            Source.Play();
        }

        public void Stop()
        {
            Source.Stop();

            if (endCoroutine != null)
                Tween.StopCustomCoroutine(endCoroutine);
        }

        public void FadeOut(float value, float time, bool stop = false)
        {
            TweenCase tweenCase = Source.DOVolume(value, time);

            if (stop)
            {
                tweenCase.OnComplete(delegate
                {
                    Source.Stop();
                });
            }
        }

        public void FadeIn(float value, float time)
        {
            Source.DOVolume(value, time);
        }

        private IEnumerator OnAudioEndCoroutine(float clipDuration)
        {
            yield return new WaitForSeconds(clipDuration);

            onAudioEnded.Invoke();
        }

        public delegate void AudioCallback();
    }
}