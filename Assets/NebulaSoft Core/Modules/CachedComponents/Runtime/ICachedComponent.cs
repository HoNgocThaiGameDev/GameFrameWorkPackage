using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Interface for caching and applying component properties.
    /// This can be useful in various scenarios such as:
    /// 1. Prefab Instantiation: Ensuring consistency by caching and applying properties to new instances.
    /// 2. State Management: Saving and restoring the state of components during gameplay save/load operations.
    /// 3. Component Reset: Resetting components to their initial states, useful in object pooling systems.
    /// 4. Editor Tools: Quickly applying predefined settings to components in custom editor tools.
    /// 5. Runtime Configuration: Dynamically changing component properties at runtime and reverting or reapplying as needed.
    /// </summary>
    /// <typeparam name="T">Type of the component to be cached and applied.</typeparam>
    public interface ICachedComponent<T> where T : Component
    {
        /// <summary>
        /// Applies the cached properties to the specified component.
        /// </summary>
        /// <param name="component">The component to which the cached properties will be applied.</param>
        void Apply(T component);

        /// <summary>
        /// Caches the properties of the specified component.
        /// </summary>
        /// <param name="component">The component from which the properties will be cached.</param>
        void Cache(T component);
    }
}