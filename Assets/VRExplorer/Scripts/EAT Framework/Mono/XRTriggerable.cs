using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRExplorer;
public class XRTriggerable : MonoBehaviour, ITriggerableEntity
{
    XRGrabInteractable interactor;
    private void Start()
    {
        interactor = GetComponent<XRGrabInteractable>();
    }

    public float TriggeringTime => 0.5f;

    public string Name => Str.Triggerable;

    public void Triggerred()
    {

    }

    public void Triggerring()
    {

    }
}
