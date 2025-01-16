using UnityEngine;

namespace VRAgent
{
    public interface ITriggerableEntity : IBaseEntity
    {
        public enum TriggerableState
        {
            Triggerring,
            Triggerred
        }

        Transform transform { get; }

        void Triggerring();

        void Triggerred();
    }
}