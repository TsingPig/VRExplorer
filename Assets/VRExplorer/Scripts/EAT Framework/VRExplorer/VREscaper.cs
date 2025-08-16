using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRExplorer.Mono;

namespace VRExplorer
{
    public class VREscaper : BaseExplorer
    {
        private int _index = 0;
        private List<MonoBehaviour> _monos = new List<MonoBehaviour>();
        
        public bool useFileID = true;

        /// <summary>
        /// 通过 GUID 获取物体
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 通过 YAML 序列化时的 FileID 查找 GameObject
        /// 仅在 Editor 下可用
        /// </summary>
        public static GameObject FindGameObjectByFileID(long fileId)
        {
#if UNITY_EDITOR
            // 获取当前打开的场景
            var scene = EditorSceneManager.GetActiveScene();
            if(!scene.isLoaded)
            {
                Debug.LogError("没有加载场景");
                return null;
            }

            // 遍历场景中所有根对象
            foreach(var rootObj in scene.GetRootGameObjects())
            {
                var result = SearchInChildren(rootObj.transform, fileId);
                if(result != null)
                    return result;
            }
#endif
            return null;
        }

#if UNITY_EDITOR
        private static GameObject SearchInChildren(Transform parent, long fileId)
        {
            // 用 GlobalObjectId 生成 ID
            GlobalObjectId gid = GlobalObjectId.GetGlobalObjectIdSlow(parent.gameObject);

            if((long)gid.targetObjectId == (long)fileId)
            {
                return parent.gameObject;
            }

            foreach(Transform child in parent)
            {
                var result = SearchInChildren(child, fileId);
                if(result != null)
                    return result;
            }
            return null;
        }
#endif

        private static GameObject FindGameObject(string id, bool useFileID)
        {
            if(useFileID)
            {
                if(long.TryParse(id, out long fileID))
                {
                    return FindGameObjectByFileID(fileID);
                }
                else
                {
                    Debug.LogError($"Invalid FileID: {id}");
                    return null;
                }
            }
            else
            {
                return FindGameObjectByGuid(id);
            }
        }


        /// <summary>
        /// 获得场景物体 Fild ID (从 Yaml中解析）
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static long GetSceneObjectFileID(GameObject go)
        {
            if(go == null) return 0;

            var scenePath = go.scene.path;

            // 1. prefab asset 资源里的 GameObject
            if(string.IsNullOrEmpty(scenePath))
                return GetPrefabObjectFileID(go);

            // 2. prefab instance 在场景里
            if(PrefabUtility.IsPartOfPrefabInstance(go))
                return GetPrefabInstanceFileID(go, scenePath);

            // 3. 普通场景对象（你原来的逻辑）
            return GetSceneYamlFileID(go, scenePath);
        }

        private static long GetSceneYamlFileID(GameObject go, string scenePath)
        {
            var sceneLines = File.ReadAllLines(scenePath);
            for(int i = 0; i < sceneLines.Length; i++)
            {
                if(sceneLines[i].Contains("m_Name: " + go.name))
                {
                    for(int j = i; j >= 0; j--)
                    {
                        var match = Regex.Match(sceneLines[j], @"--- !u!1 &(\d+)");
                        if(match.Success && long.TryParse(match.Groups[1].Value, out long fileID))
                            return fileID;
                    }
                }
            }
            return 0;
        }

        private static long GetPrefabObjectFileID(GameObject go)
        {
#if UNITY_EDITOR
            var prefabPath = AssetDatabase.GetAssetPath(go);
            if(!string.IsNullOrEmpty(prefabPath) && File.Exists(prefabPath))
            {
                var lines = File.ReadAllLines(prefabPath);
                for(int i = 0; i < lines.Length; i++)
                {
                    if(lines[i].Contains("m_Name: " + go.name))
                    {
                        for(int j = i; j >= 0; j--)
                        {
                            var match = Regex.Match(lines[j], @"--- !u!1 &(\d+)");
                            if(match.Success && long.TryParse(match.Groups[1].Value, out long fileID))
                                return fileID;
                        }
                    }
                }
            }
#endif
            return 0;
        }

        private static long GetPrefabInstanceFileID(GameObject go, string scenePath)
        {
#if UNITY_EDITOR
            var sceneLines = File.ReadAllLines(scenePath);

            // Step1: 找到 PrefabInstance 节点
            for(int i = 0; i < sceneLines.Length; i++)
            {
                if(sceneLines[i].StartsWith("--- !u!1001 &")) // PrefabInstance
                {
                    string prefabInstanceId = sceneLines[i].Substring("--- !u!1001 &".Length);

                    string guid = null;

                    // Step2: 读取 SourcePrefab 的 guid
                    for(int j = i + 1; j < sceneLines.Length; j++)
                    {
                        if(sceneLines[j].Contains("m_SourcePrefab:"))
                        {
                            var match = Regex.Match(sceneLines[j], @"guid: ([0-9a-fA-F]+)");
                            if(match.Success)
                            {
                                guid = match.Groups[1].Value;
                                break;
                            }
                        }

                        // PrefabInstance 结束
                        if(sceneLines[j].StartsWith("--- !u!"))
                            break;
                    }

                    if(string.IsNullOrEmpty(guid))
                        continue;

                    // Step3: 用 guid 找到 prefab 路径
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    if(!File.Exists(prefabPath))
                        continue;

                    // Step4: 在 prefab 文件里查找 GameObject 的 FileID
                    var prefabLines = File.ReadAllLines(prefabPath);
                    for(int k = 0; k < prefabLines.Length; k++)
                    {
                        if(prefabLines[k].Contains("m_Name: " + go.name))
                        {
                            for(int j = k; j >= 0; j--)
                            {
                                var match = Regex.Match(prefabLines[j], @"--- !u!1 &(\d+)");
                                if(match.Success && long.TryParse(match.Groups[1].Value, out long fileID))
                                {
                                    return fileID;
                                }
                            }
                        }
                    }
                }
            }
#endif

            Debug.LogError("Cannot resolve prefab instance FileID for: " + go.name);
            return 0;
        }



        /// <summary>
        /// 获取 Object GUID
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
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




        protected override bool TestFinished => _index >= _monos.Count;

        private static TaskList GetTaskListFromJson(string filePath = Str.TestPlanPath)
        {
            if(!File.Exists(filePath))
            {
                Debug.LogError($"Test plan file not found at: {filePath}");
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                TaskList taskList = JsonUtility.FromJson<TaskList>(jsonContent);
                if(taskList == null)
                {
                    Debug.LogError("Failed to parse test plan JSON");
                }
                return taskList;
            }
            catch(Exception e)
            {
                Debug.LogError($"Failed to import test plan: {e.Message}\n{e.StackTrace}");
            }
            return null;
        }

        public static void ImportTestPlan(string filePath = Str.TestPlanPath, bool useFileID = true)
        {
            TaskList tasklist = GetTaskListFromJson(filePath);
            foreach(var taskUnit in tasklist.taskUnit)
            {
                foreach(var action in taskUnit.actionUnits)
                {
                    if(action.type == "Grab")
                    {
                        // Handle grab action with two GUIDs
                        GameObject objA = FindGameObject(action.objectA, useFileID);
                        GameObject objB = FindGameObject(action.objectB, useFileID);
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

        public static void RemoveTestPlan(string filePath = Str.TestPlanPath, bool useFileID = true)
        {
            TaskList tasklist = GetTaskListFromJson(filePath);
            foreach(var taskUnit in tasklist.taskUnit)
            {
                foreach(var action in taskUnit.actionUnits)
                {
                    if(action.type == "Grab")
                    {
                        GameObject objA = FindGameObject(action.objectA, useFileID);
                        if(objA != null)
                        {
                            // 直接移除XRGrabbable组件
                            XRGrabbable grabbable = objA.GetComponent<XRGrabbable>();
                            if(grabbable != null)
                            {
                                UnityEngine.Object.DestroyImmediate(grabbable, true);
                                Debug.Log($"Removed XRGrabbable component from {objA.name}");

                                if(PrefabUtility.IsPartOfPrefabAsset(objA))
                                {
                                    EditorUtility.SetDirty(objA);
                                    AssetDatabase.SaveAssets();
                                }
                            }
                        }
                    }
                    // 可以添加其他action类型的移除逻辑
                }
            }
        }

        private new void Start()
        {
            base.Start();
            var taskList = GetTaskListFromJson();  // 初始化_taskList
            foreach(var taskUnit in taskList.taskUnit)
            {
                foreach(var action in taskUnit.actionUnits)
                {
                    GameObject objA = FindGameObject(action.objectA, useFileID);
                    _monos.Add(objA.GetComponent<MonoBehaviour>());
                }
            }
        }

        protected override void GetNextMono(out MonoBehaviour nextMono)
        {
            nextMono = _monos[_index++];
        }

        protected override async Task SceneExplore()
        {
            if(!TestFinished)
            {
                await TaskExecutation();
            }
        }
    }

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
}