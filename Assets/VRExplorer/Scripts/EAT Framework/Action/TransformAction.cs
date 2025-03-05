using System.Threading.Tasks;
using UnityEngine;

namespace VRExplorer
{
    /// <summary>
    /// ƽ���任����
    /// TransformAction
    /// Definition: TransformAction is an action that linearly transitions an object's transform
    /// (position, rotation, or scale) from its current state to a target state over a specified duration.
    /// This action is a subclass of TriggerAction, allowing it to be used in trigger-based scenarios.
    /// </summary>
    public class TransformAction : TriggerAction
    {
        private Transform _targetTransform;
        private Vector3 _deltaPosition;
        private Vector3 _deltaRotation;
        private Vector3 _deltaScale;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="transformableEntity">�ɱ任ʵ��</param>
        /// <param name="triggerringTime">����ʱ�䣨����ʱ�䣩</param>
        /// <param name="deltaPosition">λ������</param>
        /// <param name="deltaRotation">��ת������ŷ���ǣ�</param>
        /// <param name="deltaScale">��������</param>
        public TransformAction(ITransformableEntity transformableEntity, float triggerringTime, Vector3 deltaPosition, Vector3 deltaRotation, Vector3 deltaScale)
            : base(triggerringTime, transformableEntity)
        {
            Name = "TransformAction";
            _deltaPosition = deltaPosition;
            _deltaRotation = deltaRotation;
            _deltaScale = deltaScale;
            _targetTransform = transformableEntity.transform;
        }

        /// <summary>
        /// ִ��ƽ���任
        /// </summary>
        public override async Task Execute()
        {
            await base.Execute();

            EntityManager.Instance.TriggerState(_transformableEntity, ITriggerableEntity.TriggerableState.Triggerring);
            _transformableEntity.Triggerring();

            Vector3 startPosition = _targetTransform.position;
            Quaternion startRotation = _targetTransform.rotation;
            Vector3 startScale = _targetTransform.localScale;

            Vector3 targetPosition = startPosition + _deltaPosition;
            Quaternion targetRotation = startRotation * Quaternion.Euler(_deltaRotation);
            Vector3 targetScale = startScale + _deltaScale;

            float elapsedTime = 0f;

            while(elapsedTime < _triggerringTime)
            {
                float t = elapsedTime / _triggerringTime;

                _targetTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                _targetTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                _targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

                elapsedTime += Time.deltaTime;
                await Task.Yield();
            }

            _targetTransform.position = targetPosition;
            _targetTransform.rotation = targetRotation;
            _targetTransform.localScale = targetScale;
            EntityManager.Instance.TriggerState(_transformableEntity, ITriggerableEntity.TriggerableState.Triggerred);
            _transformableEntity.Triggerred();
        }
    }
}