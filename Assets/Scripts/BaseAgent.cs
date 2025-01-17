using BNG;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

namespace VRAgent
{
    public static class Str
    {
        public const string Box = "box";
        public const string Button = "button";
    }

    public abstract class BaseAgent : MonoBehaviour
    {
        private Vector3 _sceneCenter;

        protected Vector3[] _initEntityPos;
        protected Quaternion[] _initEntityRot;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;

        [Header("Configuration")]
        public HandController leftHandController;
        public XRBaseInteractor rightHandController;
        public float moveSpeed = 6f;
        public bool randomInitPos = false;
        public bool drag = false;

        [Header("Show For Debug")]
        [SerializeField] protected float _areaDiameter = 7.5f;
        [SerializeField] protected BaseAction _curAction;
        [SerializeField] protected List<Grabbable> _grabbables = new List<Grabbable>();
        [SerializeField] protected List<BaseAction> _curTask = new List<BaseAction>();


        protected IBaseEntity _nextEntity;
        protected List<IGrabbableEntity> _grabbableEntities = new List<IGrabbableEntity>();
        protected List<ITriggerableEntity> _triggerableEntity = new List<ITriggerableEntity>();
        protected Dictionary<IBaseEntity, bool> _entities = new Dictionary<IBaseEntity, bool>();

        protected void StartSceneExplore()
        {
            StoreEntityPos();
            _ = SceneExplore();
        }

        protected async Task SceneExplore()
        {
            GetNextEntity(out _nextEntity);
            switch(_nextEntity.Name)
            {
                case Str.Box: _curTask = GrabAndDragBoxTask((IGrabbableEntity)_nextEntity); break;
                case Str.Button: _curTask = PressButtonTask((ITriggerableEntity)_nextEntity); break;
            }

            Debug.Log(new RichText()
                .Add("Entity of Task: ", bold: true)
                .Add(_nextEntity.Name, bold: true, color: Color.yellow));

            foreach(var action in _curTask)
            {
                await action.Execute();
            }

            _entities[_nextEntity] = true;

            if(_entities.Values.All(value => value))
            {
                SceneAnalyzer.Instance.RoundFinish();
            }

            await SceneExplore();
        }

        /// <summary>
        /// 计算下一个交互的实体
        /// </summary>
        /// <param name="nextEntity"></param>
        protected abstract void GetNextEntity(out IBaseEntity nextEntity);

        #region 场景信息预处理（Scene Information Preprocessing)

        /// <summary>
        /// 存储所有实体的变换信息
        /// </summary>
        protected void StoreEntityPos()
        {
            _initEntityPos = new Vector3[_entities.Count];
            _initEntityRot = new Quaternion[_entities.Count];
            int i = 0;
            foreach(var entity in _entities.Keys)
            {
                _initEntityPos[i] = entity.transform.position;
                _initEntityRot[i] = entity.transform.rotation;
                i++;
            }
        }

        /// <summary>
        /// 重置加载所有实体的位置和旋转
        /// </summary>
        protected virtual void ResetEntityPos()
        {
            int i = 0;
            var entitiesKeys = _entities.Keys.ToList();

            foreach(var entity in entitiesKeys)
            {
                _entities[entity] = false;
                if(randomInitPos)
                {
                    entity.transform.position = _meshCenters[Random.Range(0, _meshCenters.Length - 1)] + new Vector3(0, 10f, 0);
                }
                else
                {
                    entity.transform.position = _initEntityPos[i];
                }

                Rigidbody rb = entity.transform.GetComponent<Rigidbody>();
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
        /// 抓取、拖拽箱子任务
        /// </summary>
        /// <param name="grabbableEntity"></param>
        /// <returns></returns>
        private List<BaseAction> GrabAndDragBoxTask(IGrabbableEntity grabbableEntity)
        {
            List<BaseAction> task = new List<BaseAction>()
            {
                new MoveAction(_navMeshAgent, moveSpeed, grabbableEntity.transform.position),
                new GrabAction(leftHandController, grabbableEntity, new List<BaseAction>(){
                    new MoveAction(_navMeshAgent, moveSpeed, GetRandomTwitchTarget(transform.position))
                })
            };
            return task;
        }

        /// <summary>
        /// 按按钮任务
        /// </summary>
        /// <param name="triggerableEntity"></param>
        /// <returns></returns>
        private List<BaseAction> PressButtonTask(ITriggerableEntity triggerableEntity)
        {
            List<BaseAction> task = new List<BaseAction>()
            {
                new MoveAction(_navMeshAgent, moveSpeed, triggerableEntity.transform.position),
                new TriggerAction(0.5f, triggerableEntity)
            };
            return task;
        }

        /// <summary>
        /// 射击任务
        /// </summary>
        /// <param name="grabbableEntity"></param>
        /// <param name="triggerableEntity"></param>
        /// <returns></returns>
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
                        new TriggerAction(0.5f, triggerableEntity)
                    })
                })
            };
            return task;
        }

        #endregion 任务预定义（Task Pre-defined）

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            SceneAnalyzer.Instance.RegisterAllEntities();
        }

        private void Start()
        {
            _triangulation = NavMesh.CalculateTriangulation();
            ParseNavMesh(out _sceneCenter, out _areaDiameter, out _meshCenters);

            foreach(IGrabbableEntity grabbableEntity in SceneAnalyzer.Instance.grabbableEntities)
            {
                _grabbables.Add(grabbableEntity.Grabbable);
                _grabbableEntities.Add(grabbableEntity);
                _entities.Add(grabbableEntity, false);
            }
            foreach(ITriggerableEntity triggerableEntity in SceneAnalyzer.Instance.triggerableEntities)
            {
                _triggerableEntity.Add(triggerableEntity);
                _entities.Add(triggerableEntity, false);
            }

            SceneAnalyzer.Instance.RoundFinishEvent = () =>
            {
                ResetEntityPos();
            };



            ResetEntityPos();
            Invoke("StartSceneExplore", 2f);

        }
    }
}