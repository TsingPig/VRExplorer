using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VRAgentButton : MonoBehaviour
{
    public UnityEvent OnPress = new UnityEvent();

    public UnityEvent OnRelease = new UnityEvent();

    private void OnTriggerEnter(Collider other)
    {
        OnPress?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        OnRelease?.Invoke();
    }
}
