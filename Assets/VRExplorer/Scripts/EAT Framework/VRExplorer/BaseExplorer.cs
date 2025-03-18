using BNG;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace VRExplorer
{
    public abstract class BaseExplorer : MonoBehaviour
    {
        private Vector3 _sceneCenter;

        protected Vector3[] _initMonoPos;
        protected Quaternion[] _initMonoRot;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;

        [Header("Configuration")]
        public HandController leftHandController;

        public float moveSpeed = 6f;
        public bool randomInitPos = false;
        public bool drag = false;
        public float reportCoverageDuration = 5f;

        [Header("Show For Debug")]
        [SerializeField] protected float _areaDiameter = 7.5f;

        [SerializeField] protected List<Grabbable> _grabbables = new List<Grabbable>();
        [SerializeField] protected List<BaseAction> _curTask = new List<BaseAction>();
        [SerializeField] protected MonoBehaviour _nextMono;

        protected void StartSceneExplore()
        {
            ExperimentManager.Instance.StartRecording();
            _ = SceneExplore();
            StoreMonoPos();
        }

        protected List<BaseAction> TaskGenerator(MonoBehaviour mono)
        {
            List<BaseAction> task = new List<BaseAction>();
            switch(EntityManager.Instance.monoEntitiesMapping[mono][0].Name)
            {
                case Str.Transformable: task = TransformTask(EntityManager.Instance.GetEntity<ITransformableEntity>(mono)); break;
                case Str.Triggerable: task = TriggerTask(EntityManager.Instance.GetEntity<ITriggerableEntity>(mono)); break;
                case Str.Grabbable: task = GrabAndDragBoxTask(EntityManager.Instance.GetEntity<IGrabbableEntity>(mono)); break;
                case Str.Gun:
                task = GrabAndShootGunTask(EntityManager.Instance.GetEntity<IGrabbableEntity>(mono),
                    EntityManager.Instance.GetEntity<ITriggerableEntity>(mono)); break;
            }
            return task;
        }

        protected async Task SceneExplore()
        {
            GetNextMono(out _nextMono);
            _curTask = TaskGenerator(_nextMono);

            Debug.Log(new RichText()
                .Add("Mono of Task: ", bold: true)
                .Add(_nextMono.name, bold: true, color: Color.yellow));

            foreach(var action in _curTask) await action.Execute();

            if(!EntityManager.Instance.UpdateMonoState(_nextMono, true))
            {
                await SceneExplore();
            }
        }

        /// <summary>
        /// ������һ�������� mono
        /// </summary>
        /// <param name="mono"></param>
        protected abstract void GetNextMono(out MonoBehaviour mono);

        #region ������ϢԤ����Scene Information Preprocessing)

        /// <summary>
        /// �洢��������ı任��Ϣ
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
        /// ���ü������������λ�ú���ת
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
        /// ͨ����ȡNavMesh���������������񶥵����꣬����ÿ��Mesh�ļ������ġ�������������
        /// </summary>
        /// <returns>NavMesh�Ľ�������</returns>
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

        #endregion ������ϢԤ����Scene Information Preprocessing)

        #region ����Ԥ���壨Task Pre-defined��

        /// <summary>
        /// ������һ��ƫ����
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
        /// ץȡ����ק��������
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
        /// ��������
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
        /// �任����
        /// TransformTask
        /// Definition: Creates a list of actions to perform a smooth transform on a target entity.
        /// </summary>
        /// <param name="transformableEntity">�ɱ任��ʵ��</param>
        /// <param name="targetPosition">Ŀ��λ��</param>
        /// <param name="targetRotation">Ŀ����ת</param>
        /// <param name="targetScale">Ŀ������</param>
        /// <param name="duration">����ʱ��</param>
        /// <returns>�任�����б�</returns>
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

        #endregion ����Ԥ���壨Task Pre-defined��

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            EntityManager.Instance.RegisterAllEntities();
            EntityManager.Instance.vrexplorerMono = this;
            ExperimentManager.Instance.ExperimentFinishEvent += () =>
            {
                ResetMonoPos();
            };
        }

        private void Start()
        {
            _triangulation = NavMesh.CalculateTriangulation();
            ParseNavMesh(out _sceneCenter, out _areaDiameter, out _meshCenters);
            Invoke("StartSceneExplore", 2f);
        }
    }
}