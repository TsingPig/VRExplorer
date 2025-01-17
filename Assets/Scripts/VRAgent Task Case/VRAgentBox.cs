using BNG;
using UnityEngine;
using VRAgent;

public class VRAgentBox : MonoBehaviour, IGrabbableEntity
{
    private void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }

    #region Entity Region

    public string Name => Str.Box;

    public Grabbable Grabbable => GetComponent<Grabbable>();

    void IGrabbableEntity.OnGrabbed()
    {
        SceneAnalyzer.Instance.TriggerState(this, IGrabbableEntity.GrabbableState.Grabbed);

        OnGrabbed();
    }

    #endregion Entity Region
}