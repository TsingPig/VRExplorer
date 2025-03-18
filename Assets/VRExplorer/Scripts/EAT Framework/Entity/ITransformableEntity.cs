using UnityEngine;

namespace VRExplorer
{
    public interface ITransformableEntity : ITriggerableEntity
    {
        Vector3 DeltaPosition { get; }

        Vector3 DeltaRotation { get; }

        Vector3 DeltaScale { get; }
    }
}