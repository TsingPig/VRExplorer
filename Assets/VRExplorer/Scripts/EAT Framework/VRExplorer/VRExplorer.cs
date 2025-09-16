using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace VRExplorer
{
    public class VRExplorer : BaseExplorer
    {
        private int _autonomousEventIndex = 0;
        private int _autonomousEventsExecuted = 0;
        [SerializeField] protected MonoBehaviour _nextMono;

        [Range(0f, 1f)] public float autonomousEventFrequency;
        [Range(0f, 3.0f)] public float autonomousEventInterval = 0.75f;

        public List<UnityEvent> autonomousEvents = new List<UnityEvent>();
        protected UnityEvent _nextAutonomousEvent
        {
            get
            {
                var e = autonomousEvents[_autonomousEventIndex];
                _autonomousEventIndex = (_autonomousEventIndex + 1) % autonomousEvents.Count;
                _autonomousEventsExecuted++;
                return e;
            }
        }

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

        #endregion 场景信息预处理（Scene Information Preprocessing)

        #region 基于行为执行的场景探索（Scene Exploration with Behaviour Executation）

        /// <summary>
        /// 重复执行场景探索。
        /// 初始时记录场景信息，当结束运行时自动结束异步任务。
        /// </summary>
        /// <returns></returns>
        protected override async Task RepeatSceneExplore()
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
                if(TestFinished)
                {
                    //ExperimentManager.Instance.ExperimentFinish();
                    if(exitAfterTesting)
                    {
                        UnityEditor.EditorApplication.isPlaying = false;
                    }
                    else
                    {
                        // 实验结束后 不选择退出，重置所有状态循环实验
                        ResetExploration();
                    }
                }
            }
        }

        /// <summary>
        /// 场景探索。
        /// 基于条件分支实现了PFSM
        /// </summary>
        /// <returns></returns>
        protected override async Task SceneExplore()
        {
            bool explorationEventsCompleted = (_autonomousEventsExecuted >= autonomousEvents.Count);
            bool monoTasksCompleted = EntityManager.Instance.monoState.Values.All(value => value);
            if(!explorationEventsCompleted && !monoTasksCompleted)
            {
                float FSM = Random.Range(0, 1f);
                if(FSM <= autonomousEventFrequency)
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

        protected override async Task TaskExecutation()
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
       
        protected override void ResetExploration()
        {
            EntityManager.Instance.ResetAllEntites();
            ResetMonoPos();
            _autonomousEventIndex = 0;
            _autonomousEventsExecuted = 0;
        }

        protected override bool TestFinished => (_autonomousEventsExecuted >= autonomousEvents.Count) && EntityManager.Instance.monoState.Values.All(value => value);

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

        /// <summary>
        /// 基于距离选择最近的交互对象
        /// </summary>
        /// <param name="nextMono"></param>
        protected void GetNextMono(out MonoBehaviour nextMono)
        {
            nextMono = EntityManager.Instance.monoState.Keys
                .Where(mono => mono != null && !mono.Equals(null))
                .Where(mono => EntityManager.Instance.monoState[mono] == false)
                .OrderBy(mono => Vector3.Distance(transform.position, mono.transform.position))
                .FirstOrDefault();
        }



        /// <summary>
        /// Autonomous Event (如释放技能等无交互对象的事件)的调用
        /// </summary>
        /// <returns></returns>
        protected async Task AutonomousEventInvocation()
        {
            _curTask = BaseTask();
            Debug.Log(new RichText()
                .Add("Task: ", bold: true)
                .Add("BaseTask", bold: true, color: Color.yellow));
            foreach(var action in _curTask)
            {
                await action.Execute();
                await Task.Delay(TimeSpan.FromSeconds(autonomousEventInterval));
            }
        }

        #endregion

        #region 任务预定义（Task Pre-defined）

        /// <summary>
        /// 用于执行 AutonomousEvent 的Task
        /// </summary>
        /// <returns></returns>
        private List<BaseAction> BaseTask()
        {
            List<BaseAction> task = new List<BaseAction>()
           {
               new BaseAction(_nextAutonomousEvent.Invoke)
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

        private new void Awake()
        {
            base.Awake();
            EntityManager.Instance.RegisterAllEntities();
        }
    }
}