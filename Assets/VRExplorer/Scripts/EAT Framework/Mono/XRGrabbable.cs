using BNG;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRExplorer;

public class XRGrabbable : MonoBehaviour, IGrabbableEntity
{
    private XRBaseInteractable interactable;
    public string Name => Str.Grabbable;

    /// <summary>
    /// Whether to enable physics simulation when releasing the object.
    /// If true, the object will be affected by gravity, collisions, and other physics.
    /// If false, the object's velocity will be set to zero upon release.
    /// </summary>
    [Tooltip("If enabled, the released object will be affected by physics (gravity/collisions). If disabled, the object will remain stationary.")]
    public bool usePhysicsOnRelease = false;

    /// <summary>
    /// Kinematic Physics locks the object in place on the hand / grabber. PhysicsJoint allows collisions with the environment.
    /// </summary>
    [Tooltip("Kinematic Physics locks the object in place on the hand / grabber. Physics Joint and Velocity types allow collisions with the environment.")]
    public GrabPhysics GrabPhysics = GrabPhysics.Kinematic;

    /// <summary>
    /// Snap to a location or grab anywhere on the object
    /// </summary>
    [Tooltip("Snap to a location or grab anywhere on the object")]
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

    public void OnReleased()
    {
        if(usePhysicsOnRelease)
        {
            Grabbable.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }
}