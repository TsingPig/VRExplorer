using System.Threading.Tasks;
using UnityEngine;

namespace VRExplorer
{
    public class TransformAction : TriggerAction
    {
        private Transform _targetTransform;
        private Vector3 _deltaPosition;
        private Vector3 _deltaRotation;
        private Vector3 _deltaScale;

        public TransformAction(ITransformableEntity transformableEntity, float triggerringTime, Vector3 deltaPosition, Vector3 deltaRotation, Vector3 deltaScale)
            : base(triggerringTime, transformableEntity)
        {
            Name = "TransformAction";
            _deltaPosition = deltaPosition;
            _deltaRotation = deltaRotation;
            _deltaScale = deltaScale;
            _targetTransform = transformableEntity.transform;
        }

        public override async Task Execute()
        {
            await base.Execute();

            EntityManager.Instance.TriggerState(_triggerableEntity, ITriggerableEntity.TriggerableState.Triggerring);
            _triggerableEntity.Triggerring();


            Vector3 startPosition = _targetTransform.localPosition;
            Quaternion startRotation = _targetTransform.localRotation;
            Vector3 startScale = _targetTransform.localScale;
            Vector3 targetPosition = startPosition + _deltaPosition;
            Quaternion targetRotation = Quaternion.Euler(startRotation.eulerAngles + _deltaRotation);
            Vector3 targetScale = startScale + _deltaScale;

            float elapsedTime = 0f;

            while(elapsedTime < _triggerringTime && _targetTransform != null)
            {
                float t = elapsedTime / _triggerringTime;
                _targetTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                _targetTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                _targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                elapsedTime += Time.deltaTime;
                await Task.Yield();
            }

            _targetTransform.localPosition = targetPosition;
            _targetTransform.localRotation = targetRotation;
            _targetTransform.localScale = targetScale;

            EntityManager.Instance.TriggerState(_triggerableEntity, ITriggerableEntity.TriggerableState.Triggerred);
            _triggerableEntity.Triggerred();


        }


    }
}