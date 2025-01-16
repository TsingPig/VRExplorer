using BNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

namespace VRAgent
{
    public abstract class BaseAgent : MonoBehaviour
    {
        private Vector3 _sceneCenter;

        protected Dictionary<Grabbable, bool> _environmentGrabbablesState;
        protected Vector3[] _initialGrabbablePositions;
        protected Quaternion[] _initialGrabbableRotations;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;


        protected MoveAction moveActionHandle;
        protected GrabAction grabActionHandle;


        [Header("Show For Debug")]
        [SerializeField] protected Grabbable nextGrabbable;
        [SerializeField] protected float areaDiameter = 7.5f;
        [SerializeField] protected List<Grabbable> sceneGrabbables;


        [Header("Configuration")]
        public HandController leftHandController;
        public XRBaseInteractor rightHandController;
        public float moveSpeed = 6f;
        public bool randomGrabble = false;
        public bool drag = false;



        protected async Task MoveToNextGrabbable()
        {
            SceneAnalyzer.Instance.ShowMetrics();
            GetNextGrabbable(out nextGrabbable);

            if(nextGrabbable == null) return;

            await moveActionHandle.Execute(nextGrabbable.transform.position);

            _environmentGrabbablesState[nextGrabbable] = true;

            if(drag && nextGrabbable)
            {
                await grabActionHandle.Execute();
            }

            if(_environmentGrabbablesState.Values.All(value => value))
            {
                SceneAnalyzer.Instance.RoundFinish();
            }
            else
            {
                await MoveToNextGrabbable();
            }
        }

        protected abstract void GetNextGrabbable(out Grabbable nextGrabbable);


        /// <summary>
        /// 获取场景中所有的可抓取物体列表。
        /// </summary>
        /// <param name="grabbables">可抓取物体列表</param>
        /// <param name="grabbableState">可抓取物体状态</param>
        protected void GetSceneGrabbables(out List<Grabbable> grabbables, out Dictionary<Grabbable, bool> grabbableState)
        {
            grabbables = new List<Grabbable>();
            grabbableState = new Dictionary<Grabbable, bool>();

            foreach(GameObject grabbableObject in SceneAnalyzer.Instance.grabbableObjects)
            {
                var grabbable = grabbableObject.GetComponent<Grabbable>();
                grabbables.Add(grabbable);
                grabbableState.Add(grabbable, false);
            }
        }

        /// <summary>
        /// 存储所有场景中可抓取物体的变换信息
        /// </summary>
        protected void StoreSceneGrabbableObjects()
        {
            _initialGrabbablePositions = new Vector3[sceneGrabbables.Count];
            _initialGrabbableRotations = new Quaternion[sceneGrabbables.Count];

            for(int i = 0; i < sceneGrabbables.Count; i++)
            {
                _initialGrabbablePositions[i] = sceneGrabbables[i].transform.position;
                _initialGrabbableRotations[i] = sceneGrabbables[i].transform.rotation;
            }
        }

        /// <summary>
        /// 重置加载所有可抓取物体的位置和旋转
        /// </summary>
        protected virtual void ResetSceneGrabbableObjects()
        {
            for(int i = 0; i < sceneGrabbables.Count; i++)
            {
                _environmentGrabbablesState[sceneGrabbables[i]] = false;

                if(randomGrabble)
                {
                    sceneGrabbables[i].transform.position = _meshCenters[Random.Range(0, _meshCenters.Length - 1)] + new Vector3(0, 10f, 0);
                }
                else
                {
                    sceneGrabbables[i].transform.position = _initialGrabbablePositions[i];
                }

                Rigidbody rb = sceneGrabbables[i].GetComponent<Rigidbody>();
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

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            SceneAnalyzer.Instance.RegisterAllEntities();
        }

        private void Start()
        {
            _triangulation = NavMesh.CalculateTriangulation();
            ParseNavMesh(out _sceneCenter, out areaDiameter, out _meshCenters);
            GetSceneGrabbables(out sceneGrabbables, out _environmentGrabbablesState);

            StoreSceneGrabbableObjects();
            ResetSceneGrabbableObjects();

            SceneAnalyzer.Instance.RoundFinishEvent = () =>
            {
                ResetSceneGrabbableObjects();
                _ = MoveToNextGrabbable();
            };

            moveActionHandle = new MoveAction(_navMeshAgent, moveSpeed);
            grabActionHandle = new GrabAction(leftHandController, _navMeshAgent, _sceneCenter, moveSpeed, () => { return nextGrabbable; });
            _ = MoveToNextGrabbable();

        }

    }
}
