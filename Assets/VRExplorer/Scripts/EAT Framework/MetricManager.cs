using System;
using System.Collections;
using System.Linq;
using TsingPigSDK;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor.TestTools.CodeCoverage;
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
        public int TriggeredStateCount
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
        public int StateCount { get; set; }

        /// <summary>
        /// 获取总可交互物体个数
        /// </summary>
        public int InteractableCount
        {
            get { return EntityManager.Instance.monoState.Count; }
        }

        public int CoveredInteractableCount
        {
            get { return EntityManager.Instance.monoState.Count((monoPair) => { return monoPair.Value == true; }); }
        }

        public void ShowMetrics()
        {
            Debug.Log(new RichText()
                .Add("TimeCost: ").Add((Time.time - timeStamp).ToString(), bold: true, color: Color.yellow)
                .Add(", TriggeredStateCount: ", bold: true).Add(TriggeredStateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", StateCount: ", bold: true).Add(StateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", CoveredInteractableCount: ", bold: true).Add(CoveredInteractableCount.ToString(), bold: true, color: Color.yellow)
                .Add(", InteractableCount: ", bold: true).Add(InteractableCount.ToString(), bold: true, color: Color.yellow)
                .Add(", Interactable Coverage: ", bold: true).Add($"{CoveredInteractableCount * 100f / InteractableCount:F2}%", bold: true, color: Color.yellow)
                .Add(", StateCount Coverage: ", bold: true).Add($"{TriggeredStateCount * 100f / StateCount:F2}%", bold: true, color: Color.yellow));
            CodeCoverage.GenerateReportWithoutStopping();
        }

        public void RoundFinish(bool quitAfterFirstRound)
        {
            ShowMetrics();
            _curFinishCount++;
            Debug.Log(new RichText().Add("Round ").Add(_curFinishCount.ToString(), color: Color.yellow, bold: true).Add(" finished"));
            StateCount = 0;
            RoundFinishEvent?.Invoke();

            if(quitAfterFirstRound)
            {
                StopAllCoroutines();
            }
        }

        public void StartRecord()
        {
            timeStamp = Time.time;
            StartCoroutine("RecordCoroutine");
        }

        private IEnumerator RecordCoroutine()
        {
            yield return null;
            ShowMetrics();
            yield return new WaitForSeconds(30f);
            StartCoroutine(RecordCoroutine());
        }
    }
}