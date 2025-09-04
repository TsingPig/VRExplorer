using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class VRExplorerJoystick : MonoBehaviour
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