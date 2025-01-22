using UnityEngine;
using UnityEngine.Events;
using VRAgent;

public class VRAgentButton : MonoBehaviour, ITriggerableEntity
{
    #region Entity Region

    public string Name => Str.Button;

    void ITriggerableEntity.Triggerring()
    {
        OnPress?.Invoke();
    }

    void ITriggerableEntity.Triggerred()
    {
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