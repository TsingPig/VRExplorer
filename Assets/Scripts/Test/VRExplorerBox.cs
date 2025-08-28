using BNG;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using VRExplorer;

public class VRExplorerBox : MonoBehaviour, ITriggerableEntity
{
    public float TriggeringTime => 0.5f;

    public string Name => Str.Triggerable;

    public void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }

    public void OnReleased()
    {
        Debug.Log("(Custom) Released");
    }

    public void Triggerred()
    {
        OnReleased();
    }

    public void Triggerring()
    {
        OnGrabbed();
    }
}