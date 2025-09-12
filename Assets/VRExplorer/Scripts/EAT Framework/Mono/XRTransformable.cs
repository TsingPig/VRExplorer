using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRExplorer.Mono
{
    public class XRTransformable : XRTriggerable, ITransformableEntity
    {
        public new string Name => Str.Transformable;

        public float triggerringTime = 0.5f;
        public Vector3 deltaRotation;
        public Vector3 deltaScale;
        public Vector3 deltaPosition;

        public Vector3 DeltaPosition => deltaPosition;

        public Vector3 DeltaRotation => deltaRotation;

        public Vector3 DeltaScale => deltaScale;

    }
}