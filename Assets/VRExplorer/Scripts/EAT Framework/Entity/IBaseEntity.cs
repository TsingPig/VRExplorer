using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace VRExplorer
{
    /// <summary>
    /// BaseEntity
    /// Definition: Entity is the terminal interface used for binding to the MonoBehaviour-Derived class,
    /// such as XRJoystick, which can be classified as an entity like a button, having 2 states.
    /// The state is presented in the form of enum HashSet.
    /// The entity event is presented in the form of method that needs to be implemented in the MonBehaviour-Derived class.
    /// So, it is easily for VRExplorer to trigger the UnityEvent that practically exist in the class.
    /// And it is also easily for SceneAnalyzer to get the entity relation coverage using the reflection.
    /// </summary>
    public interface IBaseEntity
    {
        string Name { get; }

        Transform transform { get; }
    }
}