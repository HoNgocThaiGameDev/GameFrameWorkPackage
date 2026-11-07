using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "Audio Clips", menuName = "Data/Core/Audio Clips")]
    public class AudioClips : ScriptableObject
    {
        [BoxGroup("UI", "UI")]
        public AudioClip buttonSound;

        [BoxGroup("UI")]
        public AudioClip click_fail;

        [BoxGroup("Gameplay", "Gameplay")]
        public AudioClip blockPick;

        [BoxGroup("Gameplay")]
        public AudioClip blockPut;

        [BoxGroup("Gameplay")]
        public AudioClip imageComplete;

        [BoxGroup("Gameplay")]
        public AudioClip win;
        
        [BoxGroup("Gameplay")]
        public AudioClip lose;

        [BoxGroup("Gameplay")]
        public AudioClip revive;

        [BoxGroup("Gameplay")]
        public AudioClip actionDone;

        [BoxGroup("Gameplay")]
        public AudioClip brightest_star;
    }
}

// -----------------
// Audio Controller v 0.4
// -----------------