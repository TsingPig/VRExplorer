using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace HenryLab
{
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
            _interactable = transform.GetComponent<XRBaseInteractable>();
            if(_interactor == null) _interactor = gameObject.AddComponent<XRDirectInteractor>();
        }
    }
}