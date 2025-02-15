using System;
using System.Collections;
using System.Linq;
using TsingPigSDK;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VRExplorer
{
    public class MetricManager : Singleton<MetricManager>
    {
        private int _curFinishCount = 0;
        public float timeStamp;

        public event Action RoundFinishEvent;

        /// <summary>
        /// 获取总触发状态个数
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public int GetTotalTriggeredStateCount
        {
            get
            {
                int res = 0;
                foreach(var v in EntityManager.Instance.entityStates.Values)
                {
                    res += v.Count;
                }
                return res;
            }
        }

        /// <summary>
        /// 获取总状态个数
        /// </summary>
        public int GetTotalStateCount { get; set; }

        /// <summary>
        /// 获取总可交互物体个数
        /// </summary>
        public int GetTotalInteractableCount
        {
            get { return EntityManager.Instance.monoState.Count; }
        }

        public int GetTotalCoveredInteractableCount
        {
            get { return EntityManager.Instance.monoState.Count((monoPair) => { return monoPair.Value == true; }); }
        }

        public void ShowMetrics()
        {
            Debug.Log(new RichText()
                .Add("TimeCost: ")
                .Add((Time.time - timeStamp).ToString(), bold: true, color: Color.yellow)
                .Add(", TriggeredStateCount: ", bold: true)
                .Add(GetTotalTriggeredStateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", TotalStateCount: ", bold: true)
                .Add(GetTotalStateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", GetTotalCoveredInteractableCount: ", bold: true)
                .Add(GetTotalCoveredInteractableCount.ToString(), bold: true, color: Color.yellow)
                .Add(", GetTotalInteractableCount: ", bold: true)
                .Add(GetTotalInteractableCount.ToString(), bold: true, color: Color.yellow));
        }

        public void RoundFinish()
        {
            ShowMetrics();
            _curFinishCount++;
            Debug.Log(new RichText()
                .Add("Round ")
                .Add(_curFinishCount.ToString(), color: Color.yellow, bold: true)
                .Add(" finished"));
            GetTotalStateCount = 0;
            RoundFinishEvent?.Invoke();
        }

        public void StartRecord()
        {
            timeStamp = Time.time;
            StartCoroutine("RecordCoroutine");
        }

        private IEnumerator RecordCoroutine()
        {
            ShowMetrics();
            yield return new WaitForSeconds(1f);
            StartCoroutine(RecordCoroutine());
        }
    }
}