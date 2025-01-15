using BNG;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRAgent;

public class VRAgentBox : MonoBehaviour, GrabbableEntity
{

    void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }


    #region Entity Region
    public string Name => "VRAgentBox";

    void GrabbableEntity.OnGrabbed()
    {
        SceneAnalyzer.Instance.TriggerState(this, GrabbableEntity.BoxState.Grabbed);
        OnGrabbed();
    }

    #endregion
}