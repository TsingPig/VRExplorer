namespace VRAgent
{
    public interface GrabbableEntity : BaseEntity
    {
        public enum BoxState
        {
            Grabbed
        }

        void OnGrabbed();
    }
}