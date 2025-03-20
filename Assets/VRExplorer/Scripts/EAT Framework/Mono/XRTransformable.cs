using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRExplorer;

public class XRTransformable : MonoBehaviour, ITransformableEntity
{
    public Transform destination;

    public Vector3 DeltaPosition => destination.position - transform.position;

    public Vector3 DeltaRotation => new Vector3(0, 0, 0);

    public Vector3 DeltaScale => new Vector3(0, 0, 0);

    public float TriggeringTime => 3f;

    public string Name => Str.Transformable;

    private XRBaseInteractable interactable;

    private void Start()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

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
        interactable.selectExited.Invoke(e);
        interactable.hoverExited.Invoke(h);
        interactable.lastSelectExited.Invoke(e);
        interactable.lastHoverExited.Invoke(h);
        interactable.deactivated.Invoke(a);
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
        interactable.selectEntered.Invoke(e);
        interactable.hoverEntered.Invoke(h);
        interactable.firstSelectEntered.Invoke(e);
        interactable.firstHoverEntered.Invoke(h);
        interactable.activated.Invoke(a);
    }
}