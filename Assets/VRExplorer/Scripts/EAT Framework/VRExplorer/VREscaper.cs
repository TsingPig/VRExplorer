using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// ͨ�� GUID ��ȡ����
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
        /// ͨ�� YAML ���л�ʱ�� FileID ���� GameObject
        /// ���� Editor �¿���
        /// </summary>
        /// <param name="fileId">Ŀ�� FileID</param>
        /// <returns>�ҵ��� GameObject�����δ�ҵ��򷵻� null</returns>
        public static GameObject FindGameObjectByFileID(long fileId)
        {

            return null;
        }


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



        public static long GetObjectFileID(GameObject go)
        {
            PropertyInfo inspectorModeInfo =
                typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

            SerializedObject serializedObject = new SerializedObject(go);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

            SerializedProperty localIdProp =
                serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

            long fileID = localIdProp.intValue;
            Debug.Log(fileID);
            return fileID;
        }

        /// <summary>
        /// ��ȡ Object GUID
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static string GetObjectGuid(GameObject go)
        {
            if(go == null) return null;

            // 1. �����Ԥ������Դ
            if(PrefabUtility.IsPartOfPrefabAsset(go))
            {
                return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(go));
            }
            // 2. ����ǳ����е����壨������Editor�У�
            else if(Application.isEditor)
            {
                // ʹ��GlobalObjectId��ȡ�ȶ��ĳ�������ID
                GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(go);
                return globalId.ToString(); // ��ʽ�磺"Scene:GlobalObjectId_V1-2-xxxx-64330974-0"
            }
            // 3. ����ʱ���˷���
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
                            // ֱ���Ƴ�XRGrabbable���
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
                    // �����������action���͵��Ƴ��߼�
                }
            }
        }

        private new void Start()
        {
            base.Start();
            var taskList = GetTaskListFromJson();  // ��ʼ��_taskList
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