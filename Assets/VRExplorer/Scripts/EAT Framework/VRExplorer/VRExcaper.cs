using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace VRExplorer
{
    // Supporting classes for JSON deserialization
    [System.Serializable]
    public class TaskList
    {
        public List<TaskUnit> taskUnit;
    }

    [System.Serializable]
    public class TaskUnit
    {
        public List<ActionUnit> actionUnits;
    }

    [System.Serializable]
    public class ActionUnit
    {
        public string type; // "Grab", "Move", "Drop", etc.
        public string objecta;
        public Vector3 position;
    }


    public class VREscaper : BaseExplorer
    {
        public TaskList taskList = new TaskList();

        protected override bool TestFinished => throw new NotImplementedException();

        private void ImportTestPlan(string filePath = Str.TestPlanPath)
        {
            if(!File.Exists(filePath))
            {
                Debug.LogError($"Test plan file not found at: {filePath}");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                taskList = JsonUtility.FromJson<TaskList>(jsonContent);

                if(taskList == null)
                {
                    Debug.LogError("Failed to parse test plan JSON");
                    return;
                }

                foreach(var taskUnit in taskList.taskUnit)
                {
                }

            }
            catch(Exception e)
            {
                Debug.LogError($"Failed to import test plan: {e.Message}\n{e.StackTrace}");
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

        protected override List<BaseAction> TaskGenerator(MonoBehaviour mono)
        {
            throw new NotImplementedException();
        }

        protected override Task AutonomousEventInvocation()
        {
            throw new NotImplementedException();
        }

        protected override Task SceneExplore()
        {
            throw new NotImplementedException();
        }
    }
}