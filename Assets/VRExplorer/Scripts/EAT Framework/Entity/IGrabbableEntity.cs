using BNG;
using UnityEngine;

namespace VRExplorer
{
    public interface IGrabbableEntity : IBaseEntity
    {
        Transform Destination { get; }

        public enum GrabbableState
        {
            Grabbed
        }

        Grabbable Grabbable { get; }

        void OnGrabbed();

        void OnReleased();
    }
}