using BNG;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.AI;

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
        private Func<ITriggerableEntity> _triggerableEntityHandle;
        private ITriggerableEntity _triggerableEntity;
        private float _triggerringTime = 0f;

        public TriggerAction(Func<ITriggerableEntity> triggerableEntityHandle, float triggerringTime = 0f)
        {
            actionName = "TriggerAction";
            _triggerableEntityHandle = triggerableEntityHandle;
            _triggerringTime = triggerringTime;
        }

        public async Task Execute(float triggerringTime = 0f)
        {
            _triggerringTime = triggerringTime;
            await Execute();
        }

        public override async Task Execute()
        {
            _triggerableEntity = _triggerableEntityHandle();

            _triggerableEntity.Triggerring();

            float time = Time.time;
            while(Time.time - time <= _triggerringTime) await Task.Yield();

            _triggerableEntity.Triggerred();

        }

    }
}