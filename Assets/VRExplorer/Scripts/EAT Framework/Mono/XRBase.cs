using UnityEngine;
using VRExplorer;

public class XRBase : MonoBehaviour, IBaseEntity
{
    public string Name => "Base";

    private void Awake()
    {
        EntityManager.Instance.RegisterEntity(this);
    }
}