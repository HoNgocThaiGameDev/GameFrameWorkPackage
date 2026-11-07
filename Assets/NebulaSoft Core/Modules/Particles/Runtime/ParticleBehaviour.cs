using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Base class for defining custom behavior for particles.
    /// Any particle with specific behaviors beyond the default particle system should inherit from this class.
    /// </summary>
    public abstract class ParticleBehaviour : MonoBehaviour
    {        
        /// <summary>
        /// This method is called when the particle is activated.
        /// Example use cases could include triggering additional animations, effects, or custom logic.
        /// </summary>
        public abstract void OnParticleActivated();

        /// <summary>
        /// This method is called when the particle is disabled or deactivated.
        /// Example use cases could include resetting properties, stopping extra effects, or cleaning up resources.
        /// </summary>
        public abstract void OnParticleDisabled();
    }
}