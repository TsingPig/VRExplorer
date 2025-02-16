using BNG;

namespace VRExplorer
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