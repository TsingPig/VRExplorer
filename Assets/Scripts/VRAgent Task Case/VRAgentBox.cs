using BNG;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRAgent;

public class VRAgentBox : MonoBehaviour, IGrabbableEntity
{

    void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }

    #region Entity Region
    public string Name => "VRAgentBox";

    public Grabbable Grabbable => GetComponent<Grabbable>();


    void IGrabbableEntity.OnGrabbed()
    {
        SceneAnalyzer.Instance.TriggerState(this, IGrabbableEntity.GrabbableState.Grabbed);

        OnGrabbed();
    }

    #endregion
}