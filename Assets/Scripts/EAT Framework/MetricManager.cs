using System;
using TsingPigSDK;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VRAgent
{
    public class MetricManager : Singleton<MetricManager>
    {
        private int _curFinishCount = 0;
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

        public void ShowMetrics()
        {
            Debug.Log(new RichText()
                .Add("TriggeredStateCount: ", bold: true)
                .Add(GetTotalTriggeredStateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", TotalStateCount: ", bold: true)
                .Add(GetTotalStateCount.ToString(), bold: true, color: Color.yellow));
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
    }
}