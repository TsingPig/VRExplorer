using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using NUnit.Framework;

public class VRAgentJoystick : MonoBehaviour
{

    public class ValueChangeEvent : UnityEvent<float>
    { }

    public ValueChangeEvent OnXValueChanged = new ValueChangeEvent();

    public ValueChangeEvent OnYValueChanged = new ValueChangeEvent();

    private void Start()
    {
        var a = GetComponent<XRGrabInteractable>();
    }
}