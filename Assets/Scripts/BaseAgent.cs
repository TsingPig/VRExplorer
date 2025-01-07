using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseAgent : MonoBehaviour
{
    private SceneAnalyzer sceneAnalyzer;
    private int curFinishCount = 0;
    private float roundStartTIme = 0f;

    protected Dictionary<Grabbable, bool> _environmentGrabbablesState;
    protected Vector3[] _initialGrabbablePositions;   //可抓取物体的初始位置
    protected Quaternion[] _initialGrabbableRotations;//可抓取物体的初始旋转
    protected NavMeshAgent navMeshAgent;  // 引入 NavMeshAgent



    public List<Grabbable> _environmentGrabbables;      //场景中的可抓取物体
    public bool drag = false;

    
    public Transform itemRoot;
    public Grabbable nextGrabbable;     // 最近的可抓取物体
    public HandController handController;
    public float AreaDiameter = 7.5f;    // 场景的半径大小估算
    public float moveSpeed = 10f;
    public bool randomGrabble = false;


    protected IEnumerator MoveToNextGrabbable()
    {
        GetNextGrabbable(out nextGrabbable);

        if(nextGrabbable != null)
        {
            navMeshAgent.SetDestination(nextGrabbable.transform.position);  // 设置目标位置为最近的可抓取物体
        }
        navMeshAgent.speed = moveSpeed;

        float maxTimeout = 10f; // 最大允许的时间（秒），如果超过这个时间还没到达目标，就认为成功
        float startTime = Time.time;  // 记录开始时间

        while(navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
        {
            // 检查是否超时
            if(Time.time - startTime > maxTimeout)
            {
                Debug.LogWarning($"超时！{GetType().Name} 没有在指定时间内到达目标位置，强制视为成功.");
                break;  // 超时，跳出循环，认为目标已到达
            }

            yield return null;
        }

        // 到了目标地点（或超时认为已到达）
        _environmentGrabbablesState[nextGrabbable] = true;

        if(drag)
        {
            handController.grabber.GrabGrabbable(nextGrabbable);
            StartCoroutine(Drag()); // 开始拖拽
        }
        else
        {
            if(_environmentGrabbablesState.Values.All(value => value)) // 如果所有值都为 true
            {
                Debug.Log($"{GetType().Name}完成{curFinishCount}次, 花费{(roundStartTIme - Time.time):F2}秒");
                ResetAllGrabbableObjects();
                curFinishCount += 1;

                yield return null;
            }
            StartCoroutine(MoveToNextGrabbable());
        }
    }


    /// <summary>
    /// 随机抽搐
    /// </summary>
    protected IEnumerator RandomTwitch()
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
    protected IEnumerator Drag()
    {
        Debug.Log("开始Drag");

        yield return StartCoroutine(RandomTwitch());

        if(handController.grabber.HoldingItem)
        {
            handController.grabber.TryRelease();
        }

        Debug.Log("释放");

        if(_environmentGrabbablesState.Values.All(value => value)) // 如果所有值都为 true
        {
            ResetAllGrabbableObjects();
            curFinishCount += 1;
            yield return null;
        }
        StartCoroutine(MoveToNextGrabbable());
    }

    /// <summary>
    /// 获取场景中所有的可抓取物体列表
    /// </summary>
    protected void GetEnvironmentGrabbables(out List<Grabbable> grabbables, out Dictionary<Grabbable, bool> grabbableState)
    {
        grabbables = new List<Grabbable>();
        grabbableState = new Dictionary<Grabbable, bool>();

        
        foreach(GameObject obj in sceneAnalyzer.grabObjects)
        {
            var grabbable = obj.GetComponent<Grabbable>();
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
    protected void StoreAllGrabbableObjects()
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
    protected virtual void ResetAllGrabbableObjects()
    {
        roundStartTIme = Time.time;
        for(int i = 0; i < _environmentGrabbables.Count; i++)
        {
            _environmentGrabbablesState[_environmentGrabbables[i]] = false;

            if(randomGrabble)
            {
                float randomX = Random.Range(-AreaDiameter, AreaDiameter);
                float randomZ = Random.Range(-AreaDiameter, AreaDiameter);
                float randomY = 2.5f;
                Vector3 newPosition = itemRoot.position + new Vector3(randomX, randomY, randomZ);
                _environmentGrabbables[i].transform.position = newPosition;
            }
            else
            {
                _environmentGrabbables[i].transform.position = _initialGrabbablePositions[i];

            }
            Rigidbody rb = _environmentGrabbables[i].GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }


    protected void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();  // 获取 NavMeshAgent 组件
        sceneAnalyzer = GetComponent<SceneAnalyzer>();

        GetEnvironmentGrabbables(out _environmentGrabbables, out _environmentGrabbablesState);

        StoreAllGrabbableObjects();   // 保存场景中，可抓取物体的初始位置和旋转
        ResetAllGrabbableObjects();

        StartCoroutine(MoveToNextGrabbable());
    }

    protected abstract void GetNextGrabbable(out Grabbable nextGrabbbable);

}