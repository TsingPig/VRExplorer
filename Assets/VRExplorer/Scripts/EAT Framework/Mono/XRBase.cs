using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRExplorer.Mono
{
    public class XRBase : MonoBehaviour, IBaseEntity
    {
        public string Name => "Base";

        public bool useGravity = true;
        protected XRBaseInteractable _interactable;
        protected XRBaseInteractor _interactor;

        private void Awake()
        {
            EntityManager.Instance.RegisterEntity(this);
        }

        protected void Start()
        {
            _interactable = transform.GetComponent<XRBaseInteractable>();
            if(_interactable == null)
            {
                _interactable = gameObject.AddComponent<XRGrabInteractable>();
                transform.GetComponent<Rigidbody>().useGravity = useGravity;
            }
            if(_interactor == null) _interactor = gameObject.AddComponent<XRDirectInteractor>();
        }
    }
}