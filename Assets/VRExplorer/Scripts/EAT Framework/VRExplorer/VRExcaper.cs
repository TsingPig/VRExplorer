using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRExplorer.Mono;

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
        public string objectA;
        public string objectB;
    }


    public class VREscaper : BaseExplorer
    {
        public static GameObject FindGameObjectByGuid(string guid)
        {
            if(string.IsNullOrEmpty(guid)) return null;

            // First try to find in scene objects
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach(GameObject go in allObjects)
            {
                if(GetObjectGuid(go) == guid)
                    return go;
            }

            // Then try to find in prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach(string prefabGuid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab != null && GetObjectGuid(prefab) == guid)
                    return prefab;
            }

            return null;
        }
        public static string GetObjectGuid(GameObject go)
        {
            if(go == null) return null;

            // 1. 如果是预制体资源
            if(PrefabUtility.IsPartOfPrefabAsset(go))
            {
                return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(go));
            }
            // 2. 如果是场景中的物体（且是在Editor中）
            else if(Application.isEditor)
            {
                // 使用GlobalObjectId获取稳定的场景对象ID
                GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(go);
                return globalId.ToString(); // 格式如："Scene:GlobalObjectId_V1-2-xxxx-64330974-0"
            }
            // 3. 运行时回退方案
            else
            {
                return go.GetInstanceID().ToString();
            }
        }

        public TaskList taskList = new TaskList();

        protected override bool TestFinished => throw new NotImplementedException();

        public static void ImportTestPlan(string filePath = Str.TestPlanPath)
        {
            if(!File.Exists(filePath))
            {
                Debug.LogError($"Test plan file not found at: {filePath}");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var taskList = JsonUtility.FromJson<TaskList>(jsonContent);

                if(taskList == null)
                {
                    Debug.LogError("Failed to parse test plan JSON");
                    return;
                }

                foreach(var taskUnit in taskList.taskUnit)
                {
                    foreach(var action in taskUnit.actionUnits)
                    {
                        if(action.type == "Grab")
                        {
                            // Handle grab action with two GUIDs
                            GameObject objA = FindGameObjectByGuid(action.objectA);
                            GameObject objB = FindGameObjectByGuid(action.objectB);
                            XRGrabbable grabbable = objA.GetComponent<XRGrabbable>();
                            if(grabbable == null)
                            {
                                grabbable = objA.AddComponent<XRGrabbable>();
                                Debug.Log($"Added XRGrabbable component to {objA.name}");
                            }
                            else
                            {
                                Debug.Log($"{objA.name} already has XRGrabbable component");
                            }

                            // Set destination to objectB
                            grabbable.destination = objB.transform;
                            Debug.Log($"Set {objA.name}'s destination to {objB.name}");

                            // Mark as dirty and save if it's a prefab
                            if(PrefabUtility.IsPartOfPrefabAsset(objA))
                            {
                                EditorUtility.SetDirty(objA);
                                AssetDatabase.SaveAssets();
                            }

                        }
                        // Add other action type handlers as needed
                    }
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