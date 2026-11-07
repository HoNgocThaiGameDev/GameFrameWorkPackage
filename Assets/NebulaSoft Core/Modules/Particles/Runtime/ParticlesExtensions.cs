using UnityEngine;

namespace NebulaSoft
{
    public static class ParticlesExtensions
    {
        /// <summary>
        /// Extends the functionality of the ParticleSystem class to initiate playback of the particle system 
        /// with an optional delay, utilizing the PlayParticle method from the ParticlesController.
        /// </summary>
        /// <param name="particleSystem">The ParticleSystem instance to be played.</param>
        /// <param name="delay">The duration (in seconds) to wait before playing the particle system. Default is 0.</param>
        /// <returns>A ParticleCase representing the activated particle system.</returns>
        public static ParticleCase PlayCase(this ParticleSystem particleSystem, float delay = 0)
        {
            return ParticlesController.PlayParticle(particleSystem, delay);
        }
    }
}
