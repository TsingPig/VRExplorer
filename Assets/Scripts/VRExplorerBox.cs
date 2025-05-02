using BNG;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using VRExplorer;

public class VRExplorerBox : MonoBehaviour, IGrabbableEntity
{
    private void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }

    #region Entity Region

    [ExcludeFromCodeCoverage] public string Name => Str.Grabbable;

    [ExcludeFromCodeCoverage] public Grabbable Grabbable => GetComponent<Grabbable>();

    public Transform Destination => null;

    [ExcludeFromCodeCoverage]
    void IGrabbableEntity.OnGrabbed()
    {
        OnGrabbed();
    }

    #endregion Entity Region
}