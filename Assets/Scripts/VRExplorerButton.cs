using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Events;
using VRExplorer;

public class VRExplorerButton : MonoBehaviour, ITriggerableEntity
{
    #region Entity Region

    [ExcludeFromCodeCoverage] public string Name => Str.Button;

    [ExcludeFromCodeCoverage] public float TriggeringTime => 0.2f;

    [ExcludeFromCodeCoverage]
    void ITriggerableEntity.Triggerring()
    {
        OnPress?.Invoke();
    }

    [ExcludeFromCodeCoverage]
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