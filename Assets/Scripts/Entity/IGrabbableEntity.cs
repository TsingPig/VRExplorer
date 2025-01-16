using BNG;
using UnityEngine;

namespace VRAgent
{
    public interface IGrabbableEntity : IBaseEntity
    {
        public enum GrabbableState
        {
            Grabbed
        }

        Grabbable Grabbable { get; }
        Transform transform { get; }

        void OnGrabbed();

    }
}