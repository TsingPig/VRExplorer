namespace VRAgent
{
    public interface BoxEntity : BaseEntity
    {
        public enum BoxState
        {
            Grabbed
        }

        void OnGrabbed();
    }
}