using UnityEngine;

/// <summary>
/// 挂载到手部碰撞器上，用于检测是否碰撞到可抓取物体。
/// </summary>
public class GrabberCollisionHandler : MonoBehaviour
{
    public VRAgent vrAgent;  // 引用父物体脚本


    private void OnTriggerEnter(Collider other)
    {
        vrAgent?.OnGrabberTriggerEnter(other);  
    }

    private void OnTriggerStay(Collider other)
    {
        vrAgent?.OnGrabberTriggerStay(other);
    }

    private void OnTriggerExit(Collider other)
    {
        vrAgent?.OnGrabberTriggerExit(other);  // 调用父物体方法传递触发信息
    }
}
