using UnityEngine;

public class GrabberCollisionHandler : MonoBehaviour
{
    public VRAgent vrAgent;  // 引用父物体脚本

    private void OnCollisionEnter(Collision collision)
    {
        vrAgent?.OnGrabberCollisionEnter(collision);  // 调用父物体方法传递碰撞信息
    }

    private void OnTriggerEnter(Collider other)
    {
        vrAgent?.OnGrabberTriggerEnter(other);  // 调用父物体方法传递触发信息
    }
}
