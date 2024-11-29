using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TestAgent2 : MonoBehaviour
{
    private List<Grabbable> _environmentGrabbables;      //场景中的可抓取物体
    private Dictionary<Grabbable, bool> _environmentGrabbablesState;
    private Vector3[] _initialGrabbablePositions;   //可抓取物体的初始位置
    private Quaternion[] _initialGrabbableRotations;//可抓取物体的初始旋转
    private NavMeshAgent navMeshAgent;  // 引入 NavMeshAgent
    private bool dragging = false;

    public Transform itemRoot;
    public Grabbable neareastGrabbable;     // 最近的可抓取物体
    public HandController handController;
    public float AreaDiameter = 7.5f;    // 场景的半径大小估算
    public float moveSpeed = 4f;

    private IEnumerator MoveToNearestGrabbable()
    {
        if(neareastGrabbable != null)
        {
            navMeshAgent.SetDestination(neareastGrabbable.transform.position);  // 设置目标位置为最近的可抓取物体
        }
        navMeshAgent.speed = moveSpeed;

        while(navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        // 到了目标地点
        Debug.Log("到达目标位置");
        handController.grabber.GrabGrabbable(neareastGrabbable);
        _environmentGrabbablesState[neareastGrabbable] = true;
        StartCoroutine(Drag()); // 开始拖拽
    }

    /// <summary>
    /// 随机抽搐
    /// </summary>
    private IEnumerator RandomTwitch()
    {
        // 设置随机抽搐的范围
        float twitchRange = 8f; // 随机抽搐的半径范围
        float randomOffsetX = Random.Range(twitchRange / 2, twitchRange); // X方向的随机偏移
        float randomOffsetZ = Random.Range(twitchRange / 2, twitchRange); // Z方向的随机偏移
        randomOffsetX = Random.Range(-1, 1) >= 0 ? randomOffsetX : -randomOffsetX;
        randomOffsetZ = Random.Range(-1, 1) >= 0 ? randomOffsetZ : -randomOffsetZ;
        // 计算新位置（在当前位置的附近随机抽搐）
        Vector3 randomPosition = transform.position + new Vector3(randomOffsetX, 0, randomOffsetZ);

        // 随机旋转（模拟抽搐时的随机转圈）
        float randomRotationY = Random.Range(-30f, 30f); // 在 -30 到 30 度范围内旋转
        transform.Rotate(0, randomRotationY, 0);

        // 设置目标位置（如果使用 NavMeshAgent）
        navMeshAgent.SetDestination(randomPosition);
        navMeshAgent.speed = moveSpeed * 0.6f; // 抽搐时速度较慢
        Debug.Log("开始RandomTwitch");

        while(navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.6f)
        {
            yield return null;
        }

    }

    /// <summary>
    /// 拖拽
    /// </summary>
    /// <param name="dragTime"></param>
    /// <returns></returns>
    private IEnumerator Drag()
    {
        dragging = true;
        Debug.Log("开始Drag");

        yield return StartCoroutine(RandomTwitch());

        if(handController.grabber.HoldingItem)
        {
            handController.grabber.TryRelease();
        }
        dragging = false;

        Debug.Log("释放");

        if(_environmentGrabbablesState.Values.All(value => value)) // 如果所有值都为 true
        {
            ResetAllGrabbableObjects();
            yield return null;
        }
        StartCoroutine(MoveToNearestGrabbable());
    }

    /// <summary>
    /// 获取场景中所有的可抓取物体列表
    /// </summary>
    private void GetEnvironmentGrabbables(out List<Grabbable> grabbables, out Dictionary<Grabbable, bool> grabbableState)
    {
        grabbables = new List<Grabbable>();
        grabbableState = new Dictionary<Grabbable, bool>();
        foreach(Transform child in itemRoot.transform)
        {
            var grabbable = child.GetComponent<Grabbable>();
            if(grabbable)
            {
                grabbables.Add(grabbable);
                grabbableState.Add(grabbable, false);
            }
        }
    }

    /// <summary>
    /// 存储所有场景中可抓取物体的变换信息
    /// </summary>
    private void StoreAllGrabbableObjects()
    {
        _initialGrabbablePositions = new Vector3[_environmentGrabbables.Count];
        _initialGrabbableRotations = new Quaternion[_environmentGrabbables.Count];

        for(int i = 0; i < _environmentGrabbables.Count; i++)
        {
            _initialGrabbablePositions[i] = _environmentGrabbables[i].transform.position;
            _initialGrabbableRotations[i] = _environmentGrabbables[i].transform.rotation;
        }
    }

    /// <summary>
    /// 重置加载所有可抓取物体的位置和旋转
    /// </summary>
    private void ResetAllGrabbableObjects()
    {
        for(int i = 0; i < _environmentGrabbables.Count; i++)
        {
            _environmentGrabbablesState[_environmentGrabbables[i]] = false;

            float randomX = Random.Range(-AreaDiameter, AreaDiameter);
            float randomZ = Random.Range(-AreaDiameter, AreaDiameter);
            float randomY = 2.5f;
            Vector3 newPosition = itemRoot.position + new Vector3(randomX, randomY, randomZ);
            _environmentGrabbables[i].transform.position = newPosition;
            _environmentGrabbables[i].transform.rotation = _initialGrabbableRotations[i];

            Rigidbody rb = _environmentGrabbables[i].GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 获取最近的可抓取物体
    /// </summary>
    private void GetNearestGrabbable(out Grabbable nearestGrabbable)
    {
        nearestGrabbable = _environmentGrabbables
            .Where(grabbable => _environmentGrabbablesState[grabbable] == false)
            .OrderBy(grabbable => Vector3.Distance(transform.position, grabbable.transform.position))
            .FirstOrDefault();
    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();  // 获取 NavMeshAgent 组件

        GetEnvironmentGrabbables(out _environmentGrabbables, out _environmentGrabbablesState);
        GetNearestGrabbable(out neareastGrabbable);

        StoreAllGrabbableObjects();   // 保存场景中，可抓取物体的初始位置和旋转

        StartCoroutine(MoveToNearestGrabbable());
    }

    private void Update()
    {
        GetNearestGrabbable(out neareastGrabbable);
    }
}