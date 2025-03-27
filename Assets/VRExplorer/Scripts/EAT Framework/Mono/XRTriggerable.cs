using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using VRExplorer;

public class XRTriggerable : MonoBehaviour, ITriggerableEntity
{
    private XRBaseInteractable _interactable;
    private XRBaseInteractor _interactor;
    public List<UnityEvent> events = new List<UnityEvent>();

    private void Start()
    {
        if(_interactable == null) _interactable = GetComponent<XRBaseInteractable>();
        if(_interactor == null) _interactor = GetComponent<XRBaseInteractor>();
    }

    public float TriggeringTime => 1.5f;

    public string Name => Str.Triggerable;

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
        if(_interactable)
        {
            _interactable.selectExited.Invoke(e);
            _interactable.hoverExited.Invoke(h);
            _interactable.lastSelectExited.Invoke(e);
            _interactable.lastHoverExited.Invoke(h);
            _interactable.deactivated.Invoke(a);
        }

        e = new SelectExitEventArgs() {  };
        h = new HoverExitEventArgs() {};
        a = new DeactivateEventArgs() { interactorObject = interactor };
        if(_interactor)
        {
            try
            {
                _interactor.selectExited.Invoke(e);
                _interactor.hoverExited.Invoke(h);
            }
            catch(Exception except)
            {
                Debug.Log(except.ToString());
            }

        }

        foreach(var eve in events)
        {
            eve?.Invoke();
        }
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
        if(_interactable)
        {
            _interactable.selectEntered.Invoke(e);
            _interactable.hoverEntered.Invoke(h);
            _interactable.firstSelectEntered.Invoke(e);
            _interactable.firstHoverEntered.Invoke(h);
            _interactable.activated.Invoke(a);
        }
        e = new SelectEnterEventArgs() {  };
        h = new HoverEnterEventArgs() { };
        if(_interactor)
        {
            try
            {
                _interactor.selectEntered.Invoke(e);
                _interactor.hoverEntered.Invoke(h);
            }
            catch(Exception except)
            {
                Debug.Log(except.ToString());
            }
        }

    }
}