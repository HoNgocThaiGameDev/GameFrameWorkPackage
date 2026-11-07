using System;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// ParticleCase is responsible for managing individual particle instances, controlling their lifecycle,
    /// position, rotation, scale, and handling special behaviors attached to the particle system.
    /// </summary>
    public sealed class ParticleCase
    {
        // Time at which the particle will be disabled. Initialized to -1, meaning no disable time is set.
        private float disableTime = -1;

        /// <summary>
        /// The ParticleSystem component that controls the visual behavior of the particle.
        /// </summary>
        public readonly ParticleSystem ParticleSystem;

        /// <summary>
        /// The special behavior script, if any, attached to the particle prefab. Allows custom logic for activation/deactivation.
        /// </summary>
        public readonly ParticleBehaviour SpecialBehavior;

        /// <summary>
        /// Indicates whether the particle system's parent should be reset (detached) when disabled.
        /// </summary>
        public readonly bool ResetParent;

        /// <summary>
        /// Event that is invoked when the particle is disabled, allowing for additional cleanup or callbacks.
        /// </summary>
        public event SimpleCallback Disabled;

        /// <summary>
        /// A flag that indicates if the particle should be forcefully disabled.
        /// </summary>
        public bool IsForceStopped { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ParticleCase class, setting up the particle object and system.
        /// </summary>
        /// <param name="particle">The particle object that this case will manage.</param>
        /// <param name="isDelayed">Determines if the particle should start playing immediately or later (if delayed).</param>
        public ParticleCase(ParticleSystem particleSystem, bool isDelayed, bool resetParent)
        {
            if (particleSystem == null)
            {
                Debug.LogError("ParticleCase constructor error: 'particleSystem' cannot be null. Please provide a valid ParticleSystem instance.");
                return;
            }

            // Store the particle data
            ParticleSystem = particleSystem;

            ResetParent = resetParent;
            IsForceStopped = false;

            // If not delayed, immediately play the particle system
            if (!isDelayed)
            {
                ParticleSystem.Play();
            }
            // If delayed, stop the particle system until it should start playing
            else
            {
                ParticleSystem.Stop();
            }

            // Get special behavior component from the pooled object
            SpecialBehavior = ParticleSystem.GetComponent<ParticleBehaviour>();

            if (SpecialBehavior != null)
            {
                // Trigger the special activation behavior
                SpecialBehavior.OnParticleActivated();
            }
        }

        /// <summary>
        /// Disables the particle system and handles any special behavior for when the particle is disabled.
        /// </summary>
        public void OnDisable()
        {
            if (ParticleSystem != null)
            {
                // Detach from any parent object and stop the particle system
                if (ResetParent)
                    ParticleSystem.transform.SetParent(null);

                ParticleSystem.Stop();

                // Deactivate the particle game object to return it to the pool
                ParticleSystem.gameObject.SetActive(false);
            }

            // If the particle has special behavior, trigger its OnParticleDisabled event
            if (SpecialBehavior != null)
            {
                SpecialBehavior.OnParticleDisabled();
            }

            Disabled?.Invoke();
        }

        /// <summary>
        /// Forcefully disables the particle system, with an optional stop behavior (e.g., stopping emission or completely stopping).
        /// </summary>
        /// <param name="stopBehavior">Determines the method for stopping the particle system (e.g., stop emission or completely stop).</param>
        public void ForceDisable(ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting)
        {
            IsForceStopped = true;

            if(ParticleSystem != null)
            {
                if (ResetParent)
                    ParticleSystem.transform.SetParent(null);

                ParticleSystem.Stop(true, stopBehavior);
            }

            Disabled?.Invoke();
        }

        /// <summary>
        /// Applies a custom action to the main particle system and any child particle systems attached to it.
        /// This allows for dynamic modifications to the particle systems.
        /// 
        /// Example usage:
        /// <code>
        /// particleCase.ApplyToParticles((ParticleSystem particleSystem) =>
        /// {
        ///     ParticleSystem.MainModule main = particleSystem.main; // Access the main module of the particle system
        ///     main.startColor = Color.red;  // Change the start color of the particle to red
        /// });
        /// </code>
        /// In this example, the start color of the main particle system and its child particle systems is set to red.
        /// </summary>
        /// <param name="action">The action to apply to the particle systems (e.g., change settings, play or stop).</param>
        public void ApplyToParticles(Action<ParticleSystem> action)
        {
            // Apply the action to the main particle system
            action?.Invoke(ParticleSystem);

            // Get all child particle systems and apply the action to them as well
            ParticleSystem[] childSystems = ParticleSystem.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem ps in childSystems)
            {
                // Avoid applying the action to the main particle system again
                if (ps != ParticleSystem)
                {
                    action?.Invoke(ps);
                }
            }
        }

        /// <summary>
        /// Checks whether the particle should be forcefully disabled. This could be either due to a forced disable
        /// flag being set or because the particle's duration has elapsed.
        /// </summary>
        /// <returns>True if the particle needs to be disabled, otherwise false.</returns>
        public bool IsForceDisabledRequired()
        {
            if (IsForceStopped)
                return true;

            if (disableTime != -1 && Time.time > disableTime)
                return true;

            return false;
        }
        
        /// <summary>
        /// Sets onDisabled callback that will be invoked when the particle is disabled
        /// </summary>
        public ParticleCase SetOnDisabled(SimpleCallback onDisabled)
        {
            Disabled = onDisabled;

            return this;
        }

        /// <summary>
        /// Sets the position of the particle system in the world.
        /// </summary>
        /// <param name="position">The world position to move the particle system to.</param>
        /// <returns>Returns the current ParticleCase for method chaining.</returns>
        public ParticleCase SetPosition(Vector3 position)
        {
            ParticleSystem.transform.position = position;

            return this;
        }

        /// <summary>
        /// Sets the scale of the particle system.
        /// </summary>
        /// <param name="scale">The scale to apply to the particle system.</param>
        /// <returns>Returns the current ParticleCase for method chaining.</returns>
        public ParticleCase SetScale(Vector3 scale)
        {
            ParticleSystem.transform.localScale = scale;

            return this;
        }

        /// <summary>
        /// Sets the rotation of the particle system.
        /// </summary>
        /// <param name="rotation">The rotation to apply to the particle system.</param>
        /// <returns>Returns the current ParticleCase for method chaining.</returns>
        public ParticleCase SetRotation(Quaternion rotation)
        {
            ParticleSystem.transform.localRotation = rotation;

            return this;
        }

        /// <summary>
        /// Sets the duration after which the particle system should be disabled.
        /// </summary>
        /// <param name="duration">The duration in seconds for how long the particle system should stay active.</param>
        /// <returns>Returns the current ParticleCase for method chaining.</returns>
        public ParticleCase SetDuration(float duration)
        {
            disableTime = Time.time + duration;

            return this;
        }

        /// <summary>
        /// Sets a target for the particle system to follow.
        /// </summary>
        /// <param name="followTarget">The Transform that the particle system should follow.</param>
        /// <param name="localPosition">The local position of the particle system relative to the follow target.</param>
        /// <returns>Returns the current ParticleCase for method chaining.</returns>
        public ParticleCase SetTarget(Transform followTarget, Vector3 localPosition)
        {
            ParticleSystem.transform.SetParent(followTarget);
            ParticleSystem.transform.localPosition = localPosition;

            return this;
        }
    }
}
