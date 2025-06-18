using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
namespace VRExplorer.Mono
{
    public class XRTransformable : XRBase, ITransformableEntity
    {
        public new string Name => Str.Transformable;

        public float triggeringTime = 0.5f;

        public Transform destination;

        public Vector3 DeltaPosition => destination.position - transform.position;

        public Vector3 DeltaRotation => new Vector3(0, 0, 0);

        public Vector3 DeltaScale => new Vector3(0, 0, 0);

        public float TriggeringTime => triggeringTime;

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