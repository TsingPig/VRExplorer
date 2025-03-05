using System.Threading.Tasks;
using UnityEngine;

namespace VRExplorer
{
    /// <summary>
    /// ´¥·¢¶¯×÷
    /// TriggerAction
    /// Definition: TriggerAction is an action that has two state, triggerring and triggerred.
    /// The former will be activated absolutely when the VRExplorer started to trigger a TriggerableEntity.
    /// The latter will be activated later, when the VRExplorer finished to trigger the TriggerableEntity.
    /// For instance, a button is a TriggerableEntity. When the VRExplorer presses it,
    /// the button switched to the triggerring state. And when the VRExplorer releases it,
    /// the button switched to the triggered state.
    /// A joystick is also a triggerableEntity.
    /// </summary>
    public class TriggerAction : BaseAction
    {
        protected float _triggerringTime = 0f;
        protected ITriggerableEntity _transformableEntity;

        public TriggerAction(float triggerringTime, ITriggerableEntity triggerableEntity)
        {
            Name = "TriggerAction";
            _triggerringTime = triggerringTime;
            _transformableEntity = triggerableEntity;
        }

        public override async Task Execute()
        {
            await base.Execute();

            EntityManager.Instance.TriggerState(_transformableEntity, ITriggerableEntity.TriggerableState.Triggerring);
            _transformableEntity.Triggerring();

            float time = Time.time;
            while(Time.time - time <= _triggerringTime) await Task.Yield();

            EntityManager.Instance.TriggerState(_transformableEntity, ITriggerableEntity.TriggerableState.Triggerred);
            _transformableEntity.Triggerred();
        }
    }
}