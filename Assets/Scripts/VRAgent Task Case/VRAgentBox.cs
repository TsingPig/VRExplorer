using BNG;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRAgent;

public class VRAgentBox : MonoBehaviour, BoxEntity
{

    Grabbable grabbable;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
        grabbable.OnGrabbed += OnGrabbed;
    }

    void OnGrabbed()
    {
        Debug.Log("(Custom) Grabbed");
    }


    #region Entity Region
    public string Name => "VRAgentBox";

    void BoxEntity.OnGrabbed()
    {
        SceneAnalyzer.Instance.TriggerState(this, BoxEntity.BoxState.Grabbed);
    }
    #endregion
}