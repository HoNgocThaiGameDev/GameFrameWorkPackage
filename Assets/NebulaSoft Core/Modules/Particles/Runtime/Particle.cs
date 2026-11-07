using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class Particle
    {
        // The name of the particle, used for registration and identification purposes.
        [SerializeField] string particleName;

        // The prefab that contains the particle effect (e.g., GameObject with ParticleSystem).
        [SerializeField] GameObject particlePrefab;

        /// <summary>
        /// The name of the particle (used for registration and identification)
        /// </summary>
        public string ParticleName => particleName;

        /// <summary>
        /// The prefab that contains the particle effect
        /// </summary>
        public GameObject ParticlePrefab => particlePrefab;

        /// <summary>
        /// Boolean flag indicating if the particle has a special behavior attached (like a custom script).
        /// This property is automatically determined when the particle is initialized.
        /// </summary>
        public bool SpecialBehaviour { get; private set; }

        /// <summary>
        /// Pool for managing instances of this particle (for object pooling)
        /// </summary>
        public Pool ParticlePool { get; private set; }

        [System.NonSerialized]
        private bool isInitialized;

        public Particle(string particleName, GameObject particlePrefab)
        {
            this.particleName = particleName;
            this.particlePrefab = particlePrefab;

            // Validation: check if name isn't empty
            if (string.IsNullOrEmpty(particleName))
                Debug.LogError("[Particles]: Particle name can't be empty!");

            // Validation: check if prefab is exists
            if (particlePrefab == null)
                Debug.LogError($"[Particles]: Prefab isn't linked for {particleName} particle");
        }

        /// <summary>
        /// Initializes the particle, setting up object pooling and checking for special behaviors.
        /// </summary>
        public void Init()
        {
            // Validation: check if name isn't empty
            if (string.IsNullOrEmpty(particleName))
            {
                Debug.LogError("[Particles]: Particle name can't be empty!");

                return;
            }

            // Validation: check if prefab is exists
            if (particlePrefab == null)
            {
                Debug.LogError($"[Particles]: Prefab isn't linked for {particleName} particle");

                return;
            }

            // If the particle is already initialized, exit early to avoid redundant setup.
            if (isInitialized) return;

            // Mark the particle as initialized to prevent repeated setup.
            isInitialized = true;

            // Create a pool for this particle prefab to efficiently reuse instances.
            ParticlePool = new Pool(particlePrefab, $"Particle_{ParticleName}");

            // Check if the particle prefab contains a custom behavior script (ParticleBehaviour).
            SpecialBehaviour = particlePrefab.GetComponent<ParticleBehaviour>();

            // Check if prefab contains ParticleSystem component
            if (particlePrefab.GetComponent<ParticleSystem>() == null)
            {
                Debug.LogError($"[Particles]: Particle ({particleName}) prefab doesn't contain a ParticleSystem component!", particlePrefab);
            }
        }

        public void Destroy()
        {
            isInitialized = false;

            PoolManager.DestroyPool(ParticlePool);

            ParticlePool = null;
        }

        /// <summary>
        /// Plays the particle effect through the ParticlesController, optionally applying a delay.
        /// </summary>
        public ParticleCase Play(float delay = 0)
        {
            return ParticlesController.PlayParticle(this, delay);
        }
    }
}