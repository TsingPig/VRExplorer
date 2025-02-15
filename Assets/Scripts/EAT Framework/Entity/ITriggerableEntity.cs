namespace VRExplorer
{
    public interface ITriggerableEntity : IBaseEntity
    {
        public enum TriggerableState
        {
            Triggerring,
            Triggerred
        }

        void Triggerring();

        void Triggerred();
    }
}