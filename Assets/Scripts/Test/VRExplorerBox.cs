using BNG;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using VRExplorer;

public class VRExplorerBox : MonoBehaviour
{
    public void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }

    public void OnReleased()
    {
        Debug.Log("(Custom) Released");
    }

}