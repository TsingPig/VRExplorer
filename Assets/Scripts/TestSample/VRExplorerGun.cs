using BNG;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using VRExplorer;

public class VRExplorerGun : MonoBehaviour
{
    public void Fire(GameObject target)
    {
        Debug.Log("Gun Fired");
        DestroyImmediate(target);
    }

    public void Reload()
    {
        Debug.Log("Gun Reloaded");
    }
}