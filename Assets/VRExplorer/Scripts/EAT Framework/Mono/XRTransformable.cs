using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRExplorer.Mono
{
    public class XRTransformable : XRBase, ITransformableEntity
    {
        public new string Name => Str.Transformable;

        public float triggerringTime = 0.5f;
        public Vector3 deltaRotation;
        public Vector3 deltaScale;
        public Vector3 deltaPosition;

        public Vector3 DeltaPosition => deltaPosition;

        public Vector3 DeltaRotation => deltaRotation;

        public Vector3 DeltaScale => deltaScale;

        public float TriggeringTime => triggerringTime;

        public void Triggerred()
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
            var e = new SelectExitEventArgs() { interactorObject = interactor };
            var h = new HoverExitEventArgs() { interactorObject = interactor };
            var a = new DeactivateEventArgs() { interactorObject = interactor };
            _interactable.selectExited.Invoke(e);
            _interactable.hoverExited.Invoke(h);
            _interactable.lastSelectExited.Invoke(e);
            _interactable.lastHoverExited.Invoke(h);
            _interactable.deactivated.Invoke(a);
        }

        public void Triggerring()
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
            _interactable.selectEntered.Invoke(e);
            _interactable.hoverEntered.Invoke(h);
            _interactable.firstSelectEntered.Invoke(e);
            _interactable.firstHoverEntered.Invoke(h);
            _interactable.activated.Invoke(a);
        }
    }
}