using BNG;

namespace VRAgent
{
    public interface GrabbableEntity : BaseEntity
    {
        public enum BoxState
        {
            Grabbed
        }

        Grabbable Grabbable { get; }

        void OnGrabbed();

    }
}