namespace VRExplorer
{
    public interface ITriggerableEntity : IBaseEntity
    {
        public enum TriggerableState
        {
            Triggerring,
            Triggerred
        }

        float TriggeringTime { get; }

        void Triggerring();

        void Triggerred();
    }

}