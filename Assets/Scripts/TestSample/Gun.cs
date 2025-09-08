using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject targetObject;     // target to fire and destory
    public void Fire()
    {
        if(targetObject != null)
        {
            DestroyImmediate(targetObject);
            Debug.Log("Gun Fired");
        }
    }
}