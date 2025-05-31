using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRExplorer;

public class XRBase : MonoBehaviour, IBaseEntity
{
    public string Name => "Base";

    protected XRBaseInteractable _interactable;
    protected XRBaseInteractor _interactor;

    private void Awake()
    {
        EntityManager.Instance.RegisterEntity(this);
    }

    protected void Start()
    {
        if(_interactable == null)
        {
            _interactable = gameObject.AddComponent<XRGrabInteractable>();
            transform.GetComponent<Rigidbody>().useGravity = false;
        }
        if(_interactor == null) _interactor = gameObject.AddComponent<XRDirectInteractor>();
    }
}