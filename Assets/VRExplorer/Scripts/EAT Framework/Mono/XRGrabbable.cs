using BNG;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRExplorer;

public class XRGrabbable : MonoBehaviour, IGrabbableEntity
{
    private XRBaseInteractable interactable;
    public string Name => Str.Grabbable;
    public Transform destination = null;

    public Grabbable Grabbable
    {
        get
        {
            var g = GetComponent<Grabbable>();
            if(!g) g = gameObject.AddComponent<Grabbable>();
            g.GrabPhysics = GrabPhysics.Kinematic;
            return g;
        }
    }

    private void Start()
    {
        interactable = GetComponent<XRBaseInteractable>();
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
        interactable.selectEntered.Invoke(e);
        interactable.hoverEntered.Invoke(h);
        interactable.firstSelectEntered.Invoke(e);
        interactable.firstHoverEntered.Invoke(h);
        interactable.activated.Invoke(a);
    }
}