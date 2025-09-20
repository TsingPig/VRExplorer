using BNG;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace HenryLab.VRExplorer
{
    public abstract class BaseExplorer : MonoBehaviour
    {
        protected bool _applicationQuitting = false;

        protected Vector3 _sceneCenter;
        protected Vector3[] _initMonoPos;
        protected Quaternion[] _initMonoRot;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;

        [Header("Experimental Configuration")]
        [SerializeField] private float reportCoverageDuration = 5f;

        [Tooltip("Set it to true when you are sure all the Interactable Objects can be covered")]
        [SerializeField] protected bool exitAfterTesting = true;

        [Header("Exploration Configuration")]
        public HandController leftHandController;

        public float moveSpeed = 6f;
        public bool randomInitPos = false;
        public bool drag = false;

        [Header("Show For Debug")]
        [SerializeField] protected float _areaDiameter = 7.5f;

        [SerializeField] protected List<BaseAction> _curTask = new List<BaseAction>();

        #region 场景信息预处理

        /// <summary>
        /// 通过获取NavMesh的所有三角形网格顶点坐标，近似每个Mesh的几何中心、场景集合中心
        /// </summary>
        /// <returns>NavMesh的近似中心</returns>
        private void ParseNavMesh(out Vector3 center, out float radius, out Vector3[] meshCenters)
        {
            int length = _triangulation.vertices.Length / 3;
            center = Vector3.zero;
            meshCenters = new Vector3[length];

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            Vector3 meshCenter = Vector3.zero;
            int vecticesIndex = 0;

            foreach(Vector3 vertex in _triangulation.vertices)
            {
                center += vertex;
                meshCenter += vertex;
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
                vecticesIndex += 1;
                if(vecticesIndex % 3 == 0)
                {
                    meshCenters[vecticesIndex / 3 - 1] = meshCenter / 3f;
                    meshCenter = Vector3.zero;
                }
            }
            center /= length;
            radius = Vector3.Distance(min, max) / 2;
        }

        #endregion 场景信息预处理

        #region 任务预定义

        /// <summary>
        /// 随机获得一个偏移量
        /// </summary>
        /// <param name="originalPos"></param>
        /// <param name="twitchRange"></param>
        /// <returns></returns>
        protected Vector3 GetRandomTwitchTarget(Vector3 originalPos, float twitchRange = 8f)
        {
            Vector3 randomPos = _sceneCenter;
            int attempts = 0;
            int maxAttempts = 50;
            while(attempts < maxAttempts)
            {
                float randomOffsetX = UnityEngine.Random.Range(-1f, 1f) * twitchRange;
                float randomOffsetZ = UnityEngine.Random.Range(-1f, 1f) * twitchRange;
                randomPos = originalPos + new Vector3(randomOffsetX, 0, randomOffsetZ);
                NavMeshPath path = new NavMeshPath();

                if(NavMesh.CalculatePath(originalPos, randomPos, NavMesh.AllAreas, path))
                {
                    if(path.status == NavMeshPathStatus.PathComplete)
                    {
                        break;
                    }
                }
                attempts++;
            }
            return randomPos;
        }

        /// <summary>
        /// 抓取、拖拽任务
        /// </summary>
        /// <param name="grabbableEntity"></param>
        /// <returns></returns>
        protected List<BaseAction> GrabTask(IGrabbableEntity grabbableEntity)
        {
            Vector3 pos;
            if(grabbableEntity.Destination) { pos = grabbableEntity.Destination.position; }
            else { pos = GetRandomTwitchTarget(transform.position); }
            List<BaseAction> task = new List<BaseAction>()
            {
                new MoveAction(_navMeshAgent, moveSpeed, grabbableEntity.transform.position),
                new GrabAction(leftHandController, grabbableEntity, new List<BaseAction>(){
                    new MoveAction(_navMeshAgent, moveSpeed, pos)
                })
            };
            return task;
        }

        /// <summary>
        /// 触发任务
        /// </summary>
        /// <param name="triggerableEntity"></param>
        /// <returns></returns>
        protected List<BaseAction> TriggerTask(ITriggerableEntity triggerableEntity)
        {
            List<BaseAction> task = new List<BaseAction>()
            {
                new MoveAction(_navMeshAgent, moveSpeed, triggerableEntity.transform.position),
                new TriggerAction(triggerableEntity.TriggeringTime, triggerableEntity)
            };
            return task;
        }

        /// <summary>
        /// 变换任务
        /// TransformTask
        /// Definition: Creates a list of actions to perform a smooth transform on a target entity.
        /// </summary>
        /// <param name="transformableEntity">可变换的实体</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="targetRotation">目标旋转</param>
        /// <param name="targetScale">目标缩放</param>
        /// <param name="duration">持续时间</param>
        /// <returns>变换任务列表</returns>
        protected List<BaseAction> TransformTask(ITransformableEntity transformableEntity)
        {
            List<BaseAction> task = new List<BaseAction>()
            {
                new MoveAction(_navMeshAgent, moveSpeed, transformableEntity.transform.position),
                new TransformAction(transformableEntity,
                    transformableEntity.TriggeringTime,
                    transformableEntity.DeltaPosition,
                    transformableEntity.DeltaRotation,
                    transformableEntity.DeltaScale)
            };
            return task;
        }

        #endregion 任务预定义

        #region 基于行为执行的场景探索（Scene Exploration with Behaviour Executation）

        /// <summary>
        /// 执行场景探索。
        /// 初始时记录场景信息，当结束运行时自动结束异步任务。
        /// </summary>
        /// <returns></returns>
        protected abstract Task RepeatSceneExplore();

        /// <summary>
        /// 场景探索
        /// </summary>
        /// <returns></returns>
        protected abstract Task SceneExplore();

        /// <summary>
        /// 任务执行
        /// </summary>
        /// <returns></returns>
        protected abstract Task TaskExecutation();

        /// <summary>
        /// 测试结束的标记
        /// </summary>
        protected abstract bool TestFinished { get; }

        /// <summary>
        /// 重新开始试验
        /// </summary>
        protected abstract void ResetExploration();

        #endregion 基于行为执行的场景探索（Scene Exploration with Behaviour Executation）

        protected void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();

            EntityManager.Instance.vrexplorerMono = this;
            ExperimentManager.Instance.reportCoverageDuration = reportCoverageDuration;
        }

        protected void Start()
        {
            _triangulation = NavMesh.CalculateTriangulation();
            ParseNavMesh(out _sceneCenter, out _areaDiameter, out _meshCenters);
            Invoke("RepeatSceneExplore", 1f);
        }

        protected void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }
    }
}