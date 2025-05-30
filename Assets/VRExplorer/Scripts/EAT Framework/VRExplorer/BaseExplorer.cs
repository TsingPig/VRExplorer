using BNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static UnityEditor.PlayerSettings;
using Random = UnityEngine.Random;

namespace VRExplorer
{
    public abstract class BaseExplorer : MonoBehaviour
    {
        private Vector3 _sceneCenter;
        private int _explorationEventIndex = 0;
        private int _explorationEventsExecuted = 0;
        private bool _applicationQuitting = false;

        protected Vector3[] _initMonoPos;
        protected Quaternion[] _initMonoRot;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;

        protected UnityEvent _nextExplorationEvent
        {
            get
            {
                var e = explorationEvents[_explorationEventIndex];
                _explorationEventIndex = (_explorationEventIndex + 1) % explorationEvents.Count;
                _explorationEventsExecuted++;
                return e;
            }
        }
        [Header("Experimental Configuration")]
        [SerializeField] private float reportCoverageDuration = 5f;
        [Tooltip("Set it to true when you are sure all the Interactable Objects can be covered")]
        [SerializeField] private bool exitAfterTesting = true;

        [Header("Exploration Configuration")]
        public HandController leftHandController;
        public float moveSpeed = 6f;
        public float explorationEventFrequency;
        public bool randomInitPos = false;
        public bool drag = false;
        public List<UnityEvent> explorationEvents = new List<UnityEvent>();

        [Header("Show For Debug")]
        [SerializeField] protected float _areaDiameter = 7.5f;
        [SerializeField] protected List<BaseAction> _curTask = new List<BaseAction>();
        [SerializeField] protected MonoBehaviour _nextMono;

        #region 场景信息预处理（Scene Information Preprocessing)

        /// <summary>
        /// 存储所有物体的变换信息
        /// </summary>
        protected void StoreMonoPos()
        {
            _initMonoPos = new Vector3[EntityManager.Instance.monoState.Count];
            _initMonoRot = new Quaternion[EntityManager.Instance.monoState.Count];
            int i = 0;
            foreach(var entity in EntityManager.Instance.monoState.Keys)
            {
                _initMonoPos[i] = entity.transform.position;
                _initMonoRot[i] = entity.transform.rotation;
                i++;
            }
        }

        /// <summary>
        /// 重置加载所有物体的位置和旋转
        /// </summary>
        protected virtual void ResetMonoPos()
        {
            int i = 0;
            var monoKeys = EntityManager.Instance.monoState.Keys.ToList();

            foreach(var mono in monoKeys)
            {
                if(randomInitPos)
                {
                    mono.transform.position = _meshCenters[Random.Range(0, _meshCenters.Length - 1)] + new Vector3(0, 10f, 0);
                }
                else
                {
                    mono.transform.position = _initMonoPos[i];
                    mono.transform.rotation = _initMonoRot[i];
                }

                Rigidbody rb = mono.transform.GetComponent<Rigidbody>();
                if(rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                i++;
            }
        }

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

        #endregion 场景信息预处理（Scene Information Preprocessing)

        #region 基于行为执行的场景探索（Scene Exploration with Behaviour Executation）

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        /// <summary>
        /// 重复执行场景探索。
        /// 初始时记录场景信息，当结束运行时自动结束异步任务。
        /// </summary>
        /// <returns></returns>
        protected async Task RepeatSceneExplore()
        {
            ExperimentManager.Instance.StartRecording();
            StoreMonoPos();
            while(!_applicationQuitting)
            {
                await SceneExplore();
                //ExperimentManager.Instance.ShowMetrics();
                for(int i = 0; i < 30; i++)
                {
                    await Task.Yield();
                }
                if(exitAfterTesting && (_explorationEventsExecuted >= explorationEvents.Count) && EntityManager.Instance.monoState.Values.All(value => value))
                {
                    //ExperimentManager.Instance.ExperimentFinish();
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
        }

        /// <summary>
        /// 任务生成器，通过输入Mono信息，解析Entity标识符名，返回对应的任务模型
        /// </summary>
        /// <param name="mono">当前需要交互的Mono</param>
        /// <returns></returns>
        protected List<BaseAction> TaskGenerator(MonoBehaviour mono)
        {
            List<BaseAction> task = new List<BaseAction>();

            switch(EntityManager.Instance.monoEntitiesMapping[mono][0].Name)
            {
                case Str.Transformable: task = TransformTask(EntityManager.Instance.GetEntity<ITransformableEntity>(mono)); break;
                case Str.Triggerable: task = TriggerTask(EntityManager.Instance.GetEntity<ITriggerableEntity>(mono)); break;
                case Str.Grabbable: task = GrabTask(EntityManager.Instance.GetEntity<IGrabbableEntity>(mono)); break;
                case Str.Gun:
                task = GrabAndShootGunTask(EntityManager.Instance.GetEntity<IGrabbableEntity>(mono),
                    EntityManager.Instance.GetEntity<ITriggerableEntity>(mono)); break;
            }
            return task;
        }

        protected async Task AutonomousEventInvocation()
        {
            _curTask = BaseTask();
            Debug.Log(new RichText()
                .Add("Task: ", bold: true)
                .Add("BaseTask", bold: true, color: Color.yellow));
            foreach(var action in _curTask)
            {
                await action.Execute();
            }
        }

        protected async Task TaskExecutation()
        {
            try
            {
                GetNextMono(out _nextMono);
                _curTask = TaskGenerator(_nextMono);
                Debug.Log(new RichText()
                    .Add("Mono of Task: ", bold: true)
                    .Add(_nextMono.name, bold: true, color: Color.yellow));
                foreach(var action in _curTask)
                {
                    await action.Execute();
                }
                EntityManager.Instance.UpdateMonoState(_nextMono, true);
            }
            catch(Exception except)
            {
                Debug.LogError(except.ToString());
            }
        }

        /// <summary>
        /// 场景探索。
        /// 基于条件分支实现了PFSM
        /// </summary>
        /// <returns></returns>
        protected async Task SceneExplore()
        {
            bool explorationEventsCompleted = (_explorationEventsExecuted >= explorationEvents.Count);
            bool monoTasksCompleted = EntityManager.Instance.monoState.Values.All(value => value);
            if(!explorationEventsCompleted && !monoTasksCompleted)
            {
                float FSM = Random.Range(0, 1f);
                if(FSM <= explorationEventFrequency)
                {
                    await AutonomousEventInvocation();
                }
                else
                {
                    await TaskExecutation();
                }
            }
            else if(!explorationEventsCompleted)
            {
                await AutonomousEventInvocation();
            }
            else if(!monoTasksCompleted)
            {
                await TaskExecutation();
            }
        }

        /// <summary>
        /// 计算下一个交互的 mono
        /// </summary>
        /// <param name="mono"></param>
        protected abstract void GetNextMono(out MonoBehaviour mono);

        #endregion

        #region 任务预定义（Task Pre-defined）

        /// <summary>
        /// 随机获得一个偏移量
        /// </summary>
        /// <param name="originalPos"></param>
        /// <param name="twitchRange"></param>
        /// <returns></returns>
        private Vector3 GetRandomTwitchTarget(Vector3 originalPos, float twitchRange = 8f)
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
        private List<BaseAction> GrabTask(IGrabbableEntity grabbableEntity)
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

        private List<BaseAction> BaseTask()
        {
            List<BaseAction> task = new List<BaseAction>()
            {
                new MoveAction(_navMeshAgent, moveSpeed, GetRandomTwitchTarget(transform.position)),
                new BaseAction(_nextExplorationEvent.Invoke)
            };
            return task;
        }

        /// <summary>
        /// 触发任务
        /// </summary>
        /// <param name="triggerableEntity"></param>
        /// <returns></returns>
        private List<BaseAction> TriggerTask(ITriggerableEntity triggerableEntity)
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
        private List<BaseAction> TransformTask(ITransformableEntity transformableEntity)
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

        /// <summary>
        /// Task to approach the gun and fire
        /// </summary>
        /// <param name="grabbableEntity">Grabbable Entity  of the gun Mono</param>
        /// <param name="triggerableEntity">Triggerable Entity  of the gun Mono</param>
        /// <returns>Task in the form List<Action> </returns>
        private List<BaseAction> GrabAndShootGunTask(IGrabbableEntity grabbableEntity, ITriggerableEntity triggerableEntity)
        {
            List<BaseAction> task = new List<BaseAction>()
            {
                new MoveAction(_navMeshAgent, moveSpeed, grabbableEntity.transform.position),
                new GrabAction(leftHandController, grabbableEntity, new List<BaseAction>()
                {
                    new ParallelAction(new List<BaseAction>()
                    {
                        new MoveAction(_navMeshAgent, moveSpeed, GetRandomTwitchTarget(transform.position)),
                        new TriggerAction(2.5f, triggerableEntity)
                    }),
                    new ParallelAction(new List<BaseAction>()
                    {
                        new MoveAction(_navMeshAgent, moveSpeed, GetRandomTwitchTarget(transform.position)),
                        new TriggerAction(2.5f, triggerableEntity)
                    })
                })
            };
            return task;
        }

        #endregion 任务预定义（Task Pre-defined）

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            EntityManager.Instance.RegisterAllEntities();
            EntityManager.Instance.vrexplorerMono = this;
            ExperimentManager.Instance.reportCoverageDuration = reportCoverageDuration;
            ExperimentManager.Instance.ExperimentFinishEvent += () =>
            {
                //ResetMonoPos();
            };
            //Application.logMessageReceived += HandleException; 
        }

        private void Start()
        {
            _triangulation = NavMesh.CalculateTriangulation();
            ParseNavMesh(out _sceneCenter, out _areaDiameter, out _meshCenters);
            Invoke("RepeatSceneExplore", 2f);
        }
    }
}