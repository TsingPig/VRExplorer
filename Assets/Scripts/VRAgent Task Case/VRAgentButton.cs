using UnityEngine;
using UnityEngine.Events;
using VRAgent;

public class VRAgentButton : MonoBehaviour, ButtonEntity
{
    #region Entity Region

    public string Name => "VRAgentButton";

    void ButtonEntity.OnPress()
    {
        SceneAnalyzer.Instance.TriggerState(this, ButtonEntity.ButtonState.Pressed);
        OnPress?.Invoke();
    }

    void ButtonEntity.OnRelease()
    {
        SceneAnalyzer.Instance.TriggerState(this, ButtonEntity.ButtonState.Released);
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