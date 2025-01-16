using BNG;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

namespace VRAgent
{
    public abstract class BaseAgent : MonoBehaviour
    {
        private Vector3 _sceneCenter;

        protected Vector3[] _initialGrabbablePositions;
        protected Quaternion[] _initialGrabbableRotations;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;

        protected MoveAction _moveActionHandle;
        protected GrabAction _grabActionHandle;
        protected TriggerAction _triggerActionHandle;

        [Header("Configuration")]
        public HandController leftHandController;
        public XRBaseInteractor rightHandController;
        public float moveSpeed = 6f;
        public bool randomGrabble = false;
        public bool drag = false;

        [Header("Show For Debug")]
        [SerializeField] protected float _areaDiameter = 7.5f;
        [SerializeField] protected BaseAction _curAction;
        [SerializeField] protected List<Grabbable> _grabbables = new List<Grabbable>();


        protected IGrabbableEntity _nextGrabbableEntity;
        protected List<IGrabbableEntity> _grabbableEntities = new List<IGrabbableEntity>();
        protected Dictionary<IGrabbableEntity, bool> _grabbablesStates = new Dictionary<IGrabbableEntity, bool>();

        protected ITriggerableEntity _nextTriggerableEntity;
        protected List<ITriggerableEntity> _triggerableEntity = new List<ITriggerableEntity>();
        protected Dictionary<ITriggerableEntity, bool> _triggerablesStates = new Dictionary<ITriggerableEntity, bool>();

        protected async Task MoveToNextGrabbable()
        {
            GetNextGrabbableEntity(out _nextGrabbableEntity);

            if(_nextGrabbableEntity == null) return;

            await _moveActionHandle.Execute(_nextGrabbableEntity.Grabbable.transform.position);

            _grabbablesStates[_nextGrabbableEntity] = true;

            if(drag && _nextGrabbableEntity.Grabbable)
            {
                await _grabActionHandle.Execute();
            }

            if(_grabbablesStates.Values.All(value => value))
            {
                SceneAnalyzer.Instance.RoundFinish();
            }
            else
            {
                await MoveToNextGrabbable();
            }
        }

        protected abstract void GetNextTriggerableEntity(out ITriggerableEntity nextTriggerableEntity);

        protected abstract void GetNextGrabbableEntity(out IGrabbableEntity nextGrabbableEntity);

        #region 场景信息预处理（Scene Information Preprocessing)

        /// <summary>
        /// 存储所有场景中可抓取物体的变换信息
        /// </summary>
        protected void StoreSceneGrabbableObjects()
        {
            _initialGrabbablePositions = new Vector3[_grabbables.Count];
            _initialGrabbableRotations = new Quaternion[_grabbables.Count];

            for(int i = 0; i < _grabbables.Count; i++)
            {
                _initialGrabbablePositions[i] = _grabbables[i].transform.position;
                _initialGrabbableRotations[i] = _grabbables[i].transform.rotation;
            }
        }

        /// <summary>
        /// 重置加载所有可抓取物体的位置和旋转
        /// </summary>
        protected virtual void ResetSceneGrabbableObjects()
        {
            for(int i = 0; i < _grabbableEntities.Count; i++)
            {
                _grabbablesStates[_grabbableEntities[i]] = false;

                if(randomGrabble)
                {
                    _grabbables[i].transform.position = _meshCenters[Random.Range(0, _meshCenters.Length - 1)] + new Vector3(0, 10f, 0);
                }
                else
                {
                    _grabbables[i].transform.position = _initialGrabbablePositions[i];
                }

                Rigidbody rb = _grabbables[i].GetComponent<Rigidbody>();
                if(rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
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
                _grabbablesStates.Add(grabbableEntity, false);
            }
            foreach(ITriggerableEntity triggerableEntity in SceneAnalyzer.Instance.triggerableEntities)
            {
                _triggerableEntity.Add(triggerableEntity);
                _triggerablesStates.Add(triggerableEntity, false);
            }

            StoreSceneGrabbableObjects();
            ResetSceneGrabbableObjects();

            SceneAnalyzer.Instance.RoundFinishEvent = () =>
            {
                ResetSceneGrabbableObjects();
                _ = MoveToNextGrabbable();
            };

            _moveActionHandle = new MoveAction(_navMeshAgent, moveSpeed);
            _grabActionHandle = new GrabAction(leftHandController, _navMeshAgent, _sceneCenter, moveSpeed, () => { return _nextGrabbableEntity; });
            _triggerActionHandle = new TriggerAction(() => { return _nextTriggerableEntity; });

            _ = MoveToNextGrabbable();
        }
    }
}