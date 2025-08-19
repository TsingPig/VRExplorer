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
    }
}