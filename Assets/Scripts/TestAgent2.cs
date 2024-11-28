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
    private List<GameObject> _environmentGrabbables;      //场景中的可抓取物体
    private Vector3[] _initialGrabbablePositions;   //可抓取物体的初始位置
    private Quaternion[] _initialGrabbableRotations;//可抓取物体的初始旋转
    private Vector3 _initialPosition;       // Agent的初始位置
    private Quaternion _initialRotation;    // Agent的初始旋转

    public Transform itemRoot;
    public GameObject neareastGrabbable;     // 最近的可抓取物体
    public float collisionReward = 5f;  // 抓取奖励
    public float boundaryPunishment = -1f;

    [Tooltip("是否正在训练模式下（trainingMode）")]
    public bool trainingMode;

    public float AreaDiameter = 20f;    // 场景的半径大小估算

    private float smoothPitchSpeedRate = 0f;
    private float smoothYawSpeedRate = 0f;
    private float smoothChangeRate = 2f;
    private float pitchSpeed = 100f;
    private float maxPitchAngle = 80f;       //最大俯冲角度
    private float yawSpeed = 100f;
    private float moveForce = 4f;

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

        GetEnvironmentGrabbables(out _environmentGrabbables);
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
        var vectorAction = actions.ContinuousActions;
        Vector3 targetMoveDirection = new Vector3(vectorAction[0], 0, vectorAction[1]);

        // 计算目标位置并更新 NavMeshAgent 的目标位置
        if(neareastGrabbable != null)
        {
            navMeshAgent.SetDestination(neareastGrabbable.transform.position);  // 设置目标位置为最近的可抓取物体
        }

        // 使用 NavMeshAgent 来移动
        if(navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.Move(targetMoveDirection * moveForce * Time.deltaTime);
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
    private void GetEnvironmentGrabbables(out List<GameObject> grabbables)
    {
        grabbables = new List<GameObject>();
        foreach(Transform child in itemRoot.transform)
        {
            grabbables.Add(child.gameObject);
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
            _environmentGrabbables[i].gameObject.SetActive(true);
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
    private void GetNearestGrabbable(out GameObject nearestGrabbable)
    {
        nearestGrabbable = _environmentGrabbables
            .Where(grabbable => grabbable.activeInHierarchy)
            .OrderBy(grabbable => Vector3.Distance(transform.position, grabbable.transform.position))
            .FirstOrDefault();
    }

    private void Update()
    {
        GetNearestGrabbable(out neareastGrabbable);
    }

    private void FixedUpdate()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {

        if(collision.gameObject.CompareTag("Grabbable"))
        {
            AddReward(collisionReward);
            collision.gameObject.SetActive(false);

            if(_environmentGrabbables.Count(grabbable => grabbable.activeInHierarchy) == 0)
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
            Debug.Log("collisionReward");
        }
        else if(collision.gameObject.CompareTag("Boundary"))
        {
            AddReward(boundaryPunishment);
            Debug.Log("boundaryPunishment");
        }
    }

}
