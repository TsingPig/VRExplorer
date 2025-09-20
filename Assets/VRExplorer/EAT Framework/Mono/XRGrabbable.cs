using BNG;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRExplorer.Mono
{
    public class XRGrabbable : XRBase, IGrabbableEntity
    {
        public new string Name => Str.Grabbable;

        /// <summary>
        /// 当释放物体时是否启用物理模拟
        /// 如果为true，物体会受到重力、碰撞等物理影响
        /// 如果为false，物体在释放瞬间速度设置为0。
        /// </summary>
        [Tooltip("启用时释放物体会受惯性影响")]
        public bool usePhysicsOnRelease = false;

        /// <summary>
        /// 物体处于抓取状态时，链接抓取点的物理模拟方式
        /// </summary>
        [Tooltip("物体处于抓取状态时，链接抓取点的物理模拟方式")]
        public GrabPhysics GrabPhysics = GrabPhysics.Kinematic;

        /// <summary>
        /// 启用时同步到抓取点的位置
        /// </summary>
        [Tooltip("启用时同步到抓取点的位置")]
        public GrabType GrabMechanic = GrabType.Snap;

        public Transform destination = null;

        public Grabbable Grabbable
        {
            get
            {
                var g = GetComponent<Grabbable>();
                if(!g) g = gameObject.AddComponent<Grabbable>();
                g.GrabPhysics = GrabPhysics;
                g.GrabMechanic = GrabMechanic;
                return g;
            }
        }

        public Transform Destination => destination;

        public void OnGrabbed()
        {
            var obj = EntityManager.Instance.vrexplorerMono.gameObject;
            XRDirectInteractor interactor;
            if(!obj.TryGetComponent(out interactor))
            {
                interactor = obj.AddComponent<XRDirectInteractor>();
            }
            if(!obj.GetComponent<ActionBasedController>())
            {
                obj.AddComponent<ActionBasedController>();
            }
            var e = new SelectEnterEventArgs() { interactorObject = interactor };
            var h = new HoverEnterEventArgs() { interactorObject = interactor };
            var a = new ActivateEventArgs() { interactorObject = interactor };
            if(_interactable)
            {
                _interactable.selectEntered.Invoke(e);
                _interactable.hoverEntered.Invoke(h);
                _interactable.firstSelectEntered.Invoke(e);
                _interactable.firstHoverEntered.Invoke(h);
                _interactable.activated.Invoke(a);
            }
        }

        public void OnReleased()
        {
            if(!usePhysicsOnRelease && Grabbable.gameObject.GetComponent<Rigidbody>())
            {
                Grabbable.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
        }
    }
}