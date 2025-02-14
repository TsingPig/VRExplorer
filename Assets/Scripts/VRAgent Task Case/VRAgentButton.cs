using UnityEngine;
using UnityEngine.Events;
using VRAgent;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

public class VRAgentButton : MonoBehaviour, ITriggerableEntity
{
    #region Entity Region

    [ExcludeFromCodeCoverage]public string Name => Str.Button;

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