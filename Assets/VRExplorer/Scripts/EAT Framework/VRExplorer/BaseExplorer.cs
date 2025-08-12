using BNG;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace VRExplorer
{
    public abstract class BaseExplorer : MonoBehaviour
    {
        private bool _applicationQuitting = false;

        protected Vector3 _sceneCenter;
        protected Vector3[] _initMonoPos;
        protected Quaternion[] _initMonoRot;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;

        [Header("Experimental Configuration")]
        [SerializeField] private float reportCoverageDuration = 5f;

        [Tooltip("Set it to true when you are sure all the Interactable Objects can be covered")]
        [SerializeField] private bool exitAfterTesting = true;

        [Header("Exploration Configuration")]
        public HandController leftHandController;

        public float moveSpeed = 6f;
        public bool randomInitPos = false;
        public bool drag = false;


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
        /// 测试结束的标记
        /// </summary>
        protected abstract bool TestFinished { get; }

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
                if(exitAfterTesting && TestFinished)
                {
                    //ExperimentManager.Instance.ExperimentFinish();
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
        }

        /// <summary>
        /// 场景探索。
        /// 基于条件分支实现了PFSM
        /// </summary>
        /// <returns></returns>
        protected abstract Task SceneExplore();

        /// <summary>
        /// 任务生成器，通过输入Mono信息，解析Entity标识符名，返回对应的任务模型
        /// </summary>
        /// <param name="mono">当前需要交互的Mono</param>
        /// <returns></returns>
        protected abstract List<BaseAction> TaskGenerator(MonoBehaviour mono);

        protected abstract Task AutonomousEventInvocation();

        /// <summary>
        /// 计算下一个交互的 mono
        /// </summary>
        /// <param name="mono"></param>
        protected abstract void GetNextMono(out MonoBehaviour mono);

        #endregion 基于行为执行的场景探索（Scene Exploration with Behaviour Executation）

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