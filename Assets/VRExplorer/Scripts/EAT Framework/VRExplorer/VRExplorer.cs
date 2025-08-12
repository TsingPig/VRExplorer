using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace VRExplorer
{
    public class VRExplorer : BaseExplorer
    {
        private int _autonomousEventIndex = 0;
        private int _autonomousEventsExecuted = 0;
        public float autonomousEventFrequency;
        public List<UnityEvent> autonomousEvents = new List<UnityEvent>();

        protected override bool TestFinished => (_autonomousEventsExecuted >= autonomousEvents.Count) && EntityManager.Instance.monoState.Values.All(value => value);

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

        protected override void GetNextMono(out MonoBehaviour nextMono)
        {
            nextMono = EntityManager.Instance.monoState.Keys
                .Where(mono => mono != null && !mono.Equals(null))
                .Where(mono => EntityManager.Instance.monoState[mono] == false)
                .OrderBy(mono => Vector3.Distance(transform.position, mono.transform.position))
                .FirstOrDefault();
        }

        /// <summary>
        /// 任务生成器，通过输入Mono信息，解析Entity标识符名，返回对应的任务模型
        /// </summary>
        /// <param name="mono">当前需要交互的Mono</param>
        /// <returns></returns>
        protected override List<BaseAction> TaskGenerator(MonoBehaviour mono)
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

        protected override async Task AutonomousEventInvocation()
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

        /// <summary>
        /// 用于执行 AutonomousEvent 的Task
        /// </summary>
        /// <returns></returns>
        private List<BaseAction> BaseTask()
        {
            List<BaseAction> task = new List<BaseAction>()
           {
               new MoveAction(_navMeshAgent, moveSpeed, GetRandomTwitchTarget(transform.position)),
               new BaseAction(_nextAutonomousEvent.Invoke)
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
    }
}