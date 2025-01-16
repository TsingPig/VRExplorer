using BNG;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRAgent;

public class VRAgentGrabbable : MonoBehaviour, GrabbableEntity
{

    void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }

    #region Entity Region
    public string Name => "VRAgentGrabbable";

    public Grabbable Grabbable => GetComponent<Grabbable>();

    void GrabbableEntity.OnGrabbed()
    {
        SceneAnalyzer.Instance.TriggerState(this, GrabbableEntity.BoxState.Grabbed);

        OnGrabbed();
    }

    #endregion
}