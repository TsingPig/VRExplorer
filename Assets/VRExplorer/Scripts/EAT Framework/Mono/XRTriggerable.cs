using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRExplorer;
public class XRTriggerable : MonoBehaviour, ITriggerableEntity
{
    XRBaseInteractable interactable;
    private void Start()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    public float TriggeringTime => 0.5f;

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
        interactable.selectExited.Invoke(e);
        interactable.hoverExited.Invoke(h);
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
        interactable.selectEntered.Invoke(e);
        interactable.hoverEntered.Invoke(h);
    }
}
