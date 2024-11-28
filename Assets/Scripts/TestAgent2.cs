using BNG;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Machine Learning Agent：强化学习智能体
/// </summary>
public class TestAgent2 : Agent
{
    private List<Grabbable> _environmentGrabbables;      //场景中的可抓取物体
    private Dictionary<Grabbable, bool> _environmentGrabbablesState;
    private Vector3[] _initialGrabbablePositions;   //可抓取物体的初始位置
    private Quaternion[] _initialGrabbableRotations;//可抓取物体的初始旋转
    private Vector3 _initialPosition;       // Agent的初始位置
    private Quaternion _initialRotation;    // Agent的初始旋转
    private bool _isGrabbing = false;

    public float curReward = 0f;

    public Transform itemRoot;
    public Grabbable neareastGrabbable;     // 最近的可抓取物体
    public HandController handController;

    public float grabReward = 5f;  // 抓取奖励
    public float grabbingReward = 0.01f;
    public float releaseReward = 3f;





    [Tooltip("是否正在训练模式下（trainingMode）")]
    public bool trainingMode;

    public float AreaDiameter = 7.5f;    // 场景的半径大小估算


    public float moveSpeed = 4f;

    private new Rigidbody rigidbody;
    private NavMeshAgent navMeshAgent;  // 引入 NavMeshAgent

    /// <summary>
    /// 初始化智能体
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        rigidbody = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();  // 获取 NavMeshAgent 组件

        GetEnvironmentGrabbables(out _environmentGrabbables, out _environmentGrabbablesState);
        GetNearestGrabbable(out neareastGrabbable);

        StoreAllGrabbableObjects();   // 保存场景中，可抓取物体的初始位置和旋转
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        if(!trainingMode)
        {
            MaxStep = 0;         //非训练模式下，无最大步数限制（MaxStep=0）
        }
    }

    /// <summary>
    /// 在一个训练回合(Episode)开始的时候，重置这个智能体
    /// </summary>
    public override void OnEpisodeBegin()
    {
        curReward = 0f;
        _isGrabbing = false;
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        ResetAllGrabbableObjects();
        transform.SetPositionAndRotation(_initialPosition, _initialRotation);
    }

    /// <summary>
    /// 在每次Agent接收到新行为时调用
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        var discreteActions = actions.DiscreteActions;
        if(discreteActions[0] == 0)
        {
            // 0 是松开
            if(_isGrabbing)
            {
                handController.grabber.TryRelease();
                _isGrabbing = false;
                AddReward(releaseReward);
                curReward += releaseReward;
            }
        }
        else
        {

            // 1 是抓取
            if(!_isGrabbing)    // 不在抓取状态下
            {
                if(handController.grabber.TryGrab())    // 成功抓取
                {
                    handController.grabber.GrabGrabbable(neareastGrabbable);
                    _environmentGrabbablesState[neareastGrabbable] = true;
                    AddReward(grabReward);
                    curReward += grabReward;
                    _isGrabbing = true;
                }
            };
        }

        if(discreteActions[1] == 0)
        {
            // 0 是原地随机移动
            // 设置随机抽搐的范围
            float twitchRange = 3.0f; // 随机抽搐的半径范围
            float randomOffsetX = Random.Range(-twitchRange, twitchRange); // X方向的随机偏移
            float randomOffsetZ = Random.Range(-twitchRange, twitchRange); // Z方向的随机偏移

            // 计算新位置（在当前位置的附近随机抽搐）
            Vector3 randomPosition = transform.position + new Vector3(randomOffsetX, 0, randomOffsetZ);

            // 随机旋转（模拟抽搐时的随机转圈）
            float randomRotationY = Random.Range(-30f, 30f); // 在 -30 到 30 度范围内旋转
            transform.Rotate(0, randomRotationY, 0);

            // 设置目标位置（如果使用 NavMeshAgent）
            navMeshAgent.SetDestination(randomPosition);
            navMeshAgent.speed = moveSpeed * 0.3f; // 抽搐时速度较慢
        }
        else
        {
            // 1 是下一个目标
            // 计算目标位置并更新 NavMeshAgent 的目标位置
            if(neareastGrabbable != null)
            {
                navMeshAgent.SetDestination(neareastGrabbable.transform.position);  // 设置目标位置为最近的可抓取物体
            }
            navMeshAgent.speed = moveSpeed;
        }

    }

    /// <summary>
    /// 定义智能体收集观测数据的行为
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        Quaternion relativeRotation = transform.localRotation.normalized;
        Vector3 ToNeareastGrabbable = neareastGrabbable.transform.position - transform.position;
        float relativeDistance = ToNeareastGrabbable.magnitude / AreaDiameter;

        sensor.AddObservation(relativeRotation);
        sensor.AddObservation(ToNeareastGrabbable.normalized);
        sensor.AddObservation(relativeDistance);
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
            //_environmentGrabbables[i].gameObject.SetActive(true);
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
            .Where(grabbable => grabbable.gameObject.activeInHierarchy)
            .OrderBy(grabbable => Vector3.Distance(transform.position, grabbable.transform.position))
            .FirstOrDefault();
    }

    private void Update()
    {
        GetNearestGrabbable(out neareastGrabbable);
        if(!_environmentGrabbablesState.Values.ToList().Contains(false)) // 所有都抓取过一遍
        {
            if(trainingMode)
            {
                EndEpisode();

            }
            else
            {
                ResetAllGrabbableObjects();

            }
        }
    }

    private void FixedUpdate()
    {
        if(_isGrabbing)
        {
            AddReward(grabbingReward);
            curReward += grabbingReward;
        }
    }

}
