using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public class ParticlesController : MonoBehaviour
    {
        // Array of particles to be registered and managed.
        [SerializeField] Particle[] particles;

        // Dictionary to register particles by their hash code for quick access.
        private static Dictionary<int, Particle> registerParticles = new Dictionary<int, Particle>();

        // List to hold currently active particle cases.
        private static List<ParticleCase> activeParticles = new List<ParticleCase>();

        // List to manage particles that are activated with a delay.
        private static List<TweenCase> delayedParticles = new List<TweenCase>();

        /// <summary>
        /// Initializes the particles controller by registering all particles in the array.
        /// Also starts a coroutine to continuously check for active particles.
        /// </summary>
        public void Init()
        {
            // Register particles from the array.
            for (int i = 0; i < particles.Length; i++)
            {
                RegisterParticle(particles[i]);
            }

            // Start the coroutine to monitor active particles.
            StartCoroutine(CheckForActiveParticles());
        }

        /// <summary>
        /// Clears all active and delayed particles, stopping them and removing from lists.
        /// </summary>
        private void OnDestroy()
        {
            // Kill all delayed particles.
            for (int i = 0; i < delayedParticles.Count; i++)
            {
                delayedParticles[i].KillActive();
            }
            delayedParticles.Clear();

            // Remove all active particles.
            activeParticles.Clear();

            // Destroy pool objects
            foreach (Particle particle in registerParticles.Values)
            {
                particle.Destroy();
            }
            registerParticles.Clear();
        }

        /// <summary>
        /// Coroutine to check for active particles and manage their lifecycle.
        /// </summary>
        private IEnumerator CheckForActiveParticles()
        {
            while (true)
            {
                // Wait for several frames to allow for updates.
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                // Loop through active particles in reverse to avoid index issues during removal.
                for (int i = activeParticles.Count - 1; i >= 0; i--)
                {
                    // Check if the particle case is still valid.
                    if (activeParticles[i] != null && activeParticles[i].ParticleSystem != null)
                    {
                        // If the particle requires forced disable, stop it.
                        if (activeParticles[i].IsForceDisabledRequired())
                        {
                            activeParticles[i].ParticleSystem.Stop();
                            
                            activeParticles.RemoveAt(i);

                            continue;
                        }

                        // If the particle system is not alive, disable and remove it.
                        if (!activeParticles[i].ParticleSystem.IsAlive(true))
                        {
                            activeParticles[i].OnDisable();

                            activeParticles.RemoveAt(i);
                        }
                    }
                    else
                    {
                        // If the particle case is null, remove it from the active list.
                        activeParticles.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Activates a particle with an optional delay.
        /// </summary>
        /// <param name="particle">The particle to activate.</param>
        /// <param name="delay">The delay before activation.</param>
        /// <returns>A ParticleCase representing the activated particle.</returns>
        private static ParticleCase ActivateParticle(Particle particle, float delay = 0)
        {
            // Create or retrieve the pooled particle object
            GameObject particleObject = particle.ParticlePool.GetPooledObject();

            // Get the ParticleSystem component from the particle object
            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();

            return ActivateParticle(particleSystem, delay, true);
        }

        /// <summary>
        /// Activates a particle with an optional delay.
        /// </summary>
        /// <param name="particleSystem">The ParticleSystem to activate.</param>
        /// <param name="delay">The delay before activation.</param>
        /// <returns>A ParticleCase representing the activated particle.</returns>
        private static ParticleCase ActivateParticle(ParticleSystem particleSystem, float delay = 0, bool resetParent = true)
        {
            bool isDelayed = delay > 0;

            // Activate game object
            particleSystem.gameObject.SetActive(true);

            // Create a new ParticleCase for the activated particle.
            ParticleCase particleCase = new ParticleCase(particleSystem, isDelayed, resetParent);

            // If delayed, create a tween case to activate it after the delay.
            if (isDelayed)
            {
                TweenCase delayTweenCase = null;

                delayTweenCase = Tween.DelayedCall(delay, () =>
                {
                    // Play the particle system.
                    particleCase.ParticleSystem.Play();

                    // Add the particle case to the active list.
                    activeParticles.Add(particleCase);

                    // Remove from delayed particles list.
                    delayedParticles.Remove(delayTweenCase);
                });

                // Add the delay tween case to the list.
                delayedParticles.Add(delayTweenCase);

                return particleCase;
            }

            // Immediately add the active particle case to the list.
            activeParticles.Add(particleCase);

            return particleCase;
        }

        #region Register
        /// <summary>
        /// Registers a particle, ensuring it has a valid name and prefab.
        /// </summary>
        /// <param name="particle">The particle to register.</param>
        /// <returns>An integer hash code for the registered particle, or -1 if registration fails.</returns>
        public static int RegisterParticle(Particle particle)
        {
            // Validate particle name.
            if (string.IsNullOrEmpty(particle.ParticleName))
            {
                Debug.LogError("[Particle Controller]: Particle can't be initialized with empty name!");

                // Registration failed.
                return -1;
            }

            // Validate particle prefab.
            if (particle.ParticlePrefab == null)
            {
                Debug.LogError("[Particle Controller]: Particle can't be initialized without linked prefab!");

                // Registration failed.
                return -1;
            }

            // Get the hash of the particle name.
            int particleHash = particle.ParticleName.GetHashCode();
            if (!registerParticles.ContainsKey(particleHash))
            {
                // Initialize the particle.
                particle.Init();

                // Add to the registration dictionary.
                registerParticles.Add(particleHash, particle);

                // Return the particle hash code.
                return particleHash;
            }
            else
            {
                Debug.LogError($"[Particle Controller]: Particle with name {particle.ParticleName} already register!");
            }

            // Registration failed.
            return -1;
        }

        /// <summary>
        /// Registers a particle by name and prefab.
        /// </summary>
        /// <param name="particleName">The name of the particle.</param>
        /// <param name="particlePrefab">The prefab associated with the particle.</param>
        /// <returns>An integer hash code for the registered particle.</returns>
        public static int RegisterParticle(string particleName, GameObject particlePrefab)
        {
            return RegisterParticle(new Particle(particleName, particlePrefab));
        }
        #endregion

        #region Play
        /// <summary>
        /// Plays a particle by its name with an optional delay.
        /// </summary>
        /// <param name="particleName">The name of the particle to play.</param>
        /// <param name="delay">The delay before playing the particle.</param>
        /// <returns>A ParticleCase representing the played particle.</returns>
        public static ParticleCase PlayParticle(string particleName, float delay = 0)
        {
            // Get the hash of the particle name.
            int particleHash = particleName.GetHashCode();

            // Check if the particle is registered and activate it.
            if (registerParticles.ContainsKey(particleHash))
            {
                return ActivateParticle(registerParticles[particleHash], delay);
            }

            Debug.LogError($"[Particles System]: Particle with type {particleName} is missing!");

            // Particle not found.
            return null;
        }

        /// <summary>
        /// Plays a particle by its hash with an optional delay.
        /// </summary>
        /// <param name="particleHash">The hash of the particle to play.</param>
        /// <param name="delay">The delay before playing the particle.</param>
        /// <returns>A ParticleCase representing the played particle.</returns>
        public static ParticleCase PlayParticle(int particleHash, float delay = 0)
        {
            // Check if the particle is registered and activate it.
            if (registerParticles.ContainsKey(particleHash))
            {
                return ActivateParticle(registerParticles[particleHash], delay);
            }

            Debug.LogError($"[Particles System]: Particle with hash {particleHash} is missing!");

            // Particle not found.
            return null;
        }

        /// <summary>
        /// Plays a particle instance directly with an optional delay.
        /// </summary>
        /// <param name="particle">The particle instance to play.</param>
        /// <param name="delay">The delay before playing the particle.</param>
        /// <returns>A ParticleCase representing the played particle.</returns>
        public static ParticleCase PlayParticle(Particle particle, float delay = 0)
        {
            if (particle == null)
            {
                Debug.LogError("PlayParticle error: 'particle' cannot be null. Please provide a valid Particle instance.");

                return null;
            }

            return ActivateParticle(particle, delay);
        }

        /// <summary>
        /// Initiates playback of a specified particle instance, allowing for an optional delay.
        /// </summary>
        /// <param name="particleSystem">The ParticleSystem instance to be played.</param>
        /// <param name="delay">The duration (in seconds) to wait before playing the particle. Default is 0.</param>
        /// <returns>A ParticleCase representing the activated particle, or null if the operation failed.</returns>
        public static ParticleCase PlayParticle(ParticleSystem particleSystem, float delay = 0)
        {
            if (particleSystem == null)
            {
                Debug.LogError("PlayParticle error: 'particleSystem' cannot be null. Please provide a valid ParticleSystem instance.");

                return null;
            }

            return ActivateParticle(particleSystem, delay, false);
        }
        #endregion

        /// <summary>
        /// Checks if a particle with the specified name exists in the particle registry.
        /// </summary>
        /// <param name="particleName">The name of the particle to check.</param>
        /// <returns>Returns true if the particle exists in the registry, otherwise false.</returns>
        public static bool HasParticle(string particleName)
        {
            // Get the hash of the particle name.
            int particleHash = particleName.GetHashCode();

            return registerParticles.ContainsKey(particleHash);
        }

        /// <summary>
        /// Checks if a particle with the specified hash exists in the particle registry.
        /// </summary>
        /// <param name="particleHash">The hash of the particle to check.</param>
        /// <returns>Returns true if the particle exists in the registry, otherwise false.</returns>
        public static bool HasParticle(int particleHash)
        {
            return registerParticles.ContainsKey(particleHash);
        }
    }
}
