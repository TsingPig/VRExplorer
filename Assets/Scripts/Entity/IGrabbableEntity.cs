using BNG;

namespace VRAgent
{
    public interface IGrabbableEntity : IBaseEntity
    {
        public enum GrabbableState
        {
            Grabbed
        }

        Grabbable Grabbable { get; }

        void OnGrabbed();
    }
}