using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TestAgent3 : MonoBehaviour
{
    private List<Grabbable> _environmentGrabbables;      //场景中的可抓取物体
    private Dictionary<Grabbable, bool> _environmentGrabbablesState;
    private Vector3[] _initialGrabbablePositions;   //可抓取物体的初始位置
    private Quaternion[] _initialGrabbableRotations;//可抓取物体的初始旋转
    private NavMeshAgent navMeshAgent;  // 引入 NavMeshAgent

    public Transform itemRoot;
    public Grabbable nextGrabbable;     // 最近的可抓取物体
    public HandController handController;
    public float AreaDiameter = 7.5f;    // 场景的半径大小估算
    public float moveSpeed = 4f;


    private float[,] distanceMatrix; // 距离矩阵
    private List<int> hamiltonianPath; // 哈密顿路径结果
    private int curGrabbableIndex = 0;

    private void ComputeDistanceMatrix()
    {
        int count = _environmentGrabbables.Count;
        distanceMatrix = new float[count + 1, count + 1];
        Vector3 agentStartPos = transform.position;
        for(int i = 0; i < count; i++)
        {
            Vector3 grabbablePos = _environmentGrabbables[i].transform.position;
            NavMeshPath agentToGrabbablePath = new NavMeshPath();
            float dist = agentToGrabbablePath.corners.Zip(agentToGrabbablePath.corners.Skip(1), Vector3.Distance).Sum();
            distanceMatrix[count, i] = dist;
            distanceMatrix[i, count] = dist;
        }

        for(int i = 0; i < count; i++)
        {
            for(int j = 0; j < count; j++)
            {
                if(i == j) continue;

                Vector3 start = _environmentGrabbables[i].transform.position;
                Vector3 end = _environmentGrabbables[j].transform.position;

                NavMeshPath path = new NavMeshPath();
                if(NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
                {
                    distanceMatrix[i, j] = path.corners.Zip(path.corners.Skip(1), Vector3.Distance).Sum();
                }
                else
                {
                    distanceMatrix[i, j] = float.MaxValue; // Set to an unreachable value if no path exists
                }
            }
        }
    }


    /// <summary>
    /// 回溯法解决TSP
    /// </summary>
    /// <returns></returns>
    private List<int> SolveTSP()
    {
        int n = _environmentGrabbables.Count;
        List<int> path = new List<int>();
        List<int> bestPath = new List<int>(); // 用来存储最短路径
        float bestDistance = float.MaxValue;  // 用来存储最短路径的距离

        bool[] visited = new bool[n];  // 标记是否访问过某个节点

        // 递归回溯函数
        void Backtrack(int currentNode, float currentDistance, List<int> currentPath)
        {
            // 如果所有节点都访问过了，检查是否是最短路径
            if(currentPath.Count == n)
            {
                if(currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    bestPath = new List<int>(currentPath);  // 更新最短路径
                }
                return;
            }

            // 递归地访问每一个未访问的节点
            for(int i = 0; i < n; i++)
            {
                if(visited[i]) continue;

                // 访问当前节点
                visited[i] = true;
                currentPath.Add(i);
                float newDistance = currentDistance + distanceMatrix[currentNode, i];  // 更新当前路径的距离

                // 递归
                Backtrack(i, newDistance, currentPath);

                // 回溯，撤销选择
                visited[i] = false;
                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }

        // 从初始节点（即代理的起始位置）开始，执行回溯
        visited[0] = true;
        path.Add(0);  // 初始路径包含起点（代理的位置）
        Backtrack(0, 0, path);  // 从起点开始回溯

        return bestPath;
    }


    private IEnumerator MoveToNextGrabbable()
    {
        GetNextGrabbable(out nextGrabbable);
        if(nextGrabbable != null)
        {
            navMeshAgent.SetDestination(nextGrabbable.transform.position);  // 设置目标位置为最近的可抓取物体
        }
        navMeshAgent.speed = moveSpeed;

        while(navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        // 到了目标地点
        Debug.Log("到达目标位置");

        handController.grabber.GrabGrabbable(nextGrabbable);
        _environmentGrabbablesState[nextGrabbable] = true;
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
            yield return null;
        }
        StartCoroutine(MoveToNextGrabbable());
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


        ComputeDistanceMatrix();
        hamiltonianPath = SolveTSP();
        curGrabbableIndex = 0;

    }

    /// <summary>
    /// 获取最近的可抓取物体
    /// </summary>
    private void GetNextGrabbable(out Grabbable nextGrabbable)
    {
        nextGrabbable = _environmentGrabbables[curGrabbableIndex];
        curGrabbableIndex += 1;

    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();  // 获取 NavMeshAgent 组件

        GetEnvironmentGrabbables(out _environmentGrabbables, out _environmentGrabbablesState);

        StoreAllGrabbableObjects();   // 保存场景中，可抓取物体的初始位置和旋转
        ResetAllGrabbableObjects();

        

        StartCoroutine(MoveToNextGrabbable());
    }


}