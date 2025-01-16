using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using VRAgent;

public class VRAgentButton : MonoBehaviour, ITriggerableEntity
{
    #region Entity Region

    public string Name => "VRAgentButton";

    void ITriggerableEntity.Triggerring()
    {
        SceneAnalyzer.Instance.TriggerState(this, ITriggerableEntity.TriggerableState.Triggerring);
        OnPress?.Invoke();
    }

    void ITriggerableEntity.Triggerred()
    {
        SceneAnalyzer.Instance.TriggerState(this, ITriggerableEntity.TriggerableState.Triggerred);
        OnRelease?.Invoke();
    }

    #endregion Entity Region

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