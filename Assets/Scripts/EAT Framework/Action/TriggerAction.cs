using System.Threading.Tasks;
using UnityEngine;

namespace VRAgent
{
    /// <summary>
    /// ´¥·¢¶¯×÷
    /// TriggerAction
    /// Definition: TriggerAction is an action that has two state, triggerring and triggerred.
    /// The former will be activated absolutely when the VRAgent started to trigger a TriggerableEntity.
    /// The latter will be activated later, when the VRAgent finished to trigger the TriggerableEntity.
    /// For instance, a button is a TriggerableEntity. When the VRAgent presses it,
    /// the button switched to the triggerring state. And when the VRAgent releases it,
    /// the button switched to the triggered state.
    /// A joystick is also a triggerableEntity.
    /// </summary>
    public class TriggerAction : BaseAction
    {
        private float _triggerringTime = 0f;
        private ITriggerableEntity _triggerableEntity;

        public TriggerAction(float triggerringTime, ITriggerableEntity triggerableEntity)
        {
            Name = "TriggerAction";
            _triggerringTime = triggerringTime;
            _triggerableEntity = triggerableEntity;
        }

        public override async Task Execute()
        {
            await base.Execute();

            EntityManager.Instance.TriggerState(_triggerableEntity, ITriggerableEntity.TriggerableState.Triggerring);
            _triggerableEntity.Triggerring();

            float time = Time.time;
            while(Time.time - time <= _triggerringTime) await Task.Yield();

            EntityManager.Instance.TriggerState(_triggerableEntity, ITriggerableEntity.TriggerableState.Triggerred);
            _triggerableEntity.Triggerred();
        }
    }
}