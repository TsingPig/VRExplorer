using System.Threading.Tasks;
using UnityEngine;

namespace VRExplorer
{
    /// <summary>
    /// 平滑变换动作
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
        /// 构造函数
        /// </summary>
        /// <param name="transformableEntity">可变换实体</param>
        /// <param name="triggerringTime">触发时间（持续时间）</param>
        /// <param name="deltaPosition">位置增量</param>
        /// <param name="deltaRotation">旋转增量（欧拉角）</param>
        /// <param name="deltaScale">缩放增量</param>
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
        /// 执行平滑变换
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