using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TsingPigSDK;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace VRExplorer
{
    public class VRExplorer : BaseExplorer
    {
        private int _autonomousEventIndex = 0;
        private int _autonomousEventsExecuted = 0;

        [Range(0f, 1f)] public float autonomousEventFrequency;
        [Range(0f, 3.0f)] public float autonomousEventInterval = 0.75f;

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
        /// ����̽����
        /// ����������֧ʵ����PFSM
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

        /// <summary>
        /// ���ھ���ѡ������Ľ�������
        /// </summary>
        /// <param name="nextMono"></param>
        protected override void GetNextMono(out MonoBehaviour nextMono)
        {
            nextMono = EntityManager.Instance.monoState.Keys
                .Where(mono => mono != null && !mono.Equals(null))
                .Where(mono => EntityManager.Instance.monoState[mono] == false)
                .OrderBy(mono => Vector3.Distance(transform.position, mono.transform.position))
                .FirstOrDefault();
        }

        protected override void ResetExploration()
        {
            EntityManager.Instance.ResetAllEntites();
            ResetMonoPos();
            _autonomousEventIndex = 0;
            _autonomousEventsExecuted = 0;
        }

        /// <summary>
        /// Autonomous Event (���ͷż��ܵ��޽���������¼�)�ĵ���
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


        /// <summary>
        /// ����ִ�� AutonomousEvent ��Task
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
    }
}