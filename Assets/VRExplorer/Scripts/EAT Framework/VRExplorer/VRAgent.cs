using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TsingPigSDK;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using VRExplorer.JSON;
using VRExplorer.Mono;

namespace VRExplorer
{
    public class VRAgent : BaseExplorer
    {
        private int _index = 0;
        private FileIdManager _fileIdManager;

        [SerializeField] private List<TaskUnit> _taskUnits = new List<TaskUnit>();
        public bool useFileID = true;

        protected override bool TestFinished => _index >= _taskUnits.Count;

        private static FileIdManager GetOrCreateManager()
        {
            FileIdManager manager = FindObjectOfType<FileIdManager>();
            if(manager == null)
            {
                GameObject go = new GameObject("FileIdManager");
                manager = go.AddComponent<FileIdManager>();
                Debug.Log("Created FileIdManager in scene");
            }
            return manager;
        }

        private new void Start()
        {
            base.Start();
            _taskUnits = GetTaskListFromJson().taskUnits;  // 初始化_taskList

            //foreach(var taskUnit in taskList.taskUnits)
            //{
            //    foreach(var action in taskUnit.actionUnits)
            //    {
            //        GameObject objA = FindObjectOfType<FileIdManagerMono>().GetObject(action.objectA);
            //        _monos.Add(objA.GetComponent<MonoBehaviour>());
            //    }
            //}
        }

        protected TaskUnit NextTask => _taskUnits[_index++];

        /// <summary>
        /// 重复执行场景探索。
        /// 初始时记录场景信息，当结束运行时自动结束异步任务。
        /// </summary>
        /// <returns></returns>
        protected override async Task RepeatSceneExplore()
        {
            ExperimentManager.Instance.StartRecording();
            //StoreMonoPos();
            while(!_applicationQuitting)
            {
                await SceneExplore();
                //ExperimentManager.Instance.ShowMetrics();
                for(int i = 0; i < 30; i++)
                {
                    await Task.Yield();
                }
                if(TestFinished)
                {
                    //ExperimentManager.Instance.ExperimentFinish();
                    if(exitAfterTesting)
                    {
                        UnityEditor.EditorApplication.isPlaying = false;
                    }
                    else
                    {
                        // 实验结束后 不选择退出，重置所有状态循环实验
                        ResetExploration();
                    }
                }
            }
        }

        protected override async Task SceneExplore()
        {
            if(!TestFinished)
            {
                await TaskExecutation();
            }
        }

        protected override void ResetExploration()
        {
        }

        private static TaskList GetTaskListFromJson()
        {
            string filePath = PlayerPrefs.GetString("TestPlanPath", Str.TestPlanPath);
            if(!File.Exists(filePath))
            {
                Debug.LogError($"Test plan file not found at: {filePath}");
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                // TaskList taskList = JsonUtility.FromJson<TaskList>(jsonContent);  不支持多态
                TaskList taskList = JsonConvert.DeserializeObject<TaskList>(jsonContent);
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

        public static void ImportTestPlan(bool useFileID = true)
        {
            TaskList tasklist = GetTaskListFromJson();

            // 获取场景的FileIdManager
            FileIdManager manager = GetOrCreateManager();
            manager.Clear();
            foreach(var taskUnit in tasklist.taskUnits)
            {
                foreach(var action in taskUnit.actionUnits)
                {
                    GameObject objA = FileIdResolver.FindGameObject(action.objectA, useFileID);
                    if(objA != null) manager.Add(action.objectA, objA);

                    if(action.type == "Grab")
                    {

                        GrabActionUnit grabAction = action as GrabActionUnit;
                        if(grabAction.objectB != null)
                        {
                            GameObject objB = FileIdResolver.FindGameObject(grabAction.objectB, useFileID);
                            if(objB != null)
                                manager.Add(grabAction.objectB, objB);
                        }
                    }
                    else if(action.type == "Trigger")
                    {
                        TriggerActionUnit triggerAction = action as TriggerActionUnit;
                        if(triggerAction == null) continue;

                        manager.AddMonos(triggerAction.triggerringEvents);
                        manager.AddMonos(triggerAction.triggerredEvents);
                    }
                    else if(action.type == "Transform")
                    {

                    }
                }
            }
        }

        public static void RemoveTestPlan(bool useFileID = true)
        {
            // 移除临时目标物体
            var tempTargets = GameObject.FindGameObjectsWithTag(Str.TempTargetTag);
            foreach(var t in tempTargets)
            {
                DestroyImmediate(t);
            }

            // 移除场景的 FileIdManager
            FileIdManager manager = FindObjectOfType<FileIdManager>();
            if(manager != null)
                DestroyImmediate(manager.gameObject);

            TaskList tasklist = GetTaskListFromJson();
            if(tasklist == null) return;

            foreach(var taskUnit in tasklist.taskUnits)
            {
                foreach(var action in taskUnit.actionUnits)
                {
                    GameObject objA = FileIdResolver.FindGameObject(action.objectA, useFileID);
                    if(objA == null) continue;

                    if(action.type == "Grab")
                    {
                        XRGrabbable grabbable = objA.GetComponent<XRGrabbable>();
                        if(grabbable != null)
                        {
                            UnityEngine.Object.DestroyImmediate(grabbable, true);
                            Debug.Log($"Removed XRGrabbable from {objA.name}");
                        }
                    }
                    else if(action.type == "Trigger")
                    {
                        XRTriggerable triggerable = objA.GetComponent<XRTriggerable>();
                        if(triggerable != null)
                        {
                            // 清空事件列表
                            triggerable.triggerringEvents.Clear();
                            triggerable.triggerredEvents.Clear();

                            UnityEngine.Object.DestroyImmediate(triggerable, true);
                            Debug.Log($"Removed XRTriggerable from {objA.name}");
                        }
                    }
                    else if(action.type == "Transform")
                    {
                        XRTransformable transformable = objA.GetComponent<XRTransformable>();
                        if(transformable != null)
                        {
                            UnityEngine.Object.DestroyImmediate(transformable, true);
                            Debug.Log($"Removed XRTransformable from {objA.name}");
                        }

                        if(PrefabUtility.IsPartOfPrefabAsset(objA))
                        {
                            EditorUtility.SetDirty(objA);
                            AssetDatabase.SaveAssets();
                        }
                    }

                    if(PrefabUtility.IsPartOfPrefabAsset(objA))
                    {
                        EditorUtility.SetDirty(objA);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }

        private List<BaseAction> TaskGenerator(TaskUnit taskUnit)
        {
            foreach(var action in taskUnit.actionUnits)
            {
                GameObject objA = GetOrCreateManager().GetObject(action.objectA);

                if(action.type == "Grab")
                {
                    GrabActionUnit grabAction = action as GrabActionUnit;
                    if(grabAction == null) continue;
                    XRGrabbable grabbable = objA.AddComponent<XRGrabbable>();
                    Debug.Log($"Added XRGrabbable component to {objA.name}");


                    if(grabAction.objectB != null)
                    {
                        GameObject objB = FileIdResolver.FindGameObject(grabAction.objectB, useFileID);
                        grabbable.destination = objB.transform;
                    }
                    else if(grabAction.targetPosition != null)// 使用 Vector3作为 target
                    {
                        Vector3 targetPos = (Vector3)grabAction.targetPosition;
                        // 先查找场景中是否已有临时目标
                        GameObject targetObj = GameObject.Find($"{objA.name}_TargetPosition");
                        if(targetObj == null)
                        {
                            targetObj = new GameObject($"{objA.name}_TargetPosition_{Str.TempTargetTag}");
                            targetObj.transform.position = targetPos;

                            // 给临时目标加标记，方便后续删除
                            targetObj.tag = Str.TempTargetTag;
                        }
                        else
                        {
                            targetObj.transform.position = targetPos; // 更新位置
                        }

                        grabbable.destination = targetObj.transform;
                        Debug.Log($"Set {objA.name}'s destination to position {targetPos}");
                    }
                    else
                    {
                        Debug.LogError("Lacking of Destination");
                    }

                    //// Mark as dirty and save if it's a prefab
                    //if(PrefabUtility.IsPartOfPrefabAsset(objA))
                    //{
                    //    EditorUtility.SetDirty(objA);
                    //    AssetDatabase.SaveAssets();
                    //}

                    return GrabTask(grabbable);
                }
                else if(action.type == "Trigger")
                {
                    TriggerActionUnit triggerAction = action as TriggerActionUnit;
                    if(triggerAction == null) continue;
                    XRTriggerable triggerable = objA.AddComponent<XRTriggerable>();
                    Debug.Log($"Added XRTriggerable component to {objA.name}");

                    if(triggerAction.trigerringTime != null) triggerable.triggeringTime = (float)triggerAction.trigerringTime;
                    FileIdResolver.BindEventList(triggerAction.triggerringEvents, triggerable.triggerringEvents);
                    FileIdResolver.BindEventList(triggerAction.triggerredEvents, triggerable.triggerredEvents);

                    return TriggerTask(triggerable);
                    //if(PrefabUtility.IsPartOfPrefabAsset(objA))
                    //{
                    //    EditorUtility.SetDirty(objA);
                    //    AssetDatabase.SaveAssets();
                    //}
                }
                else if(action.type == "Transform")
                {
                    TransformActionUnit transformAction = action as TransformActionUnit;
                    if(transformAction == null) continue;
                    XRTransformable transformable = objA.AddComponent<XRTransformable>();
                    Debug.Log($"Added XRTransformable component to {objA.name}");

                    // 设置变换参数
                    if(transformAction.trigerringTime != null) transformable.triggerringTime = (float)transformAction.trigerringTime;
                    transformable.deltaPosition = transformAction.deltaPosition;
                    transformable.deltaRotation = transformAction.deltaRotation;
                    transformable.deltaScale = transformAction.deltaScale;

                    if(transformAction.trigerringTime != null)
                        transformable.triggerringTime = (float)transformAction.trigerringTime;

                    return TransformTask(transformable);
                    //if(PrefabUtility.IsPartOfPrefabAsset(objA))
                    //{
                    //    EditorUtility.SetDirty(objA);
                    //    AssetDatabase.SaveAssets();
                    //}
                }
            }
            Debug.LogError($"{taskUnit} is null");
            return null;
        }

        protected override async Task TaskExecutation()
        {
            try
            {
                _curTask = TaskGenerator(NextTask);

                foreach(var action in _curTask)
                {
                    await action.Execute();
                }
            }
            catch(Exception except)
            {
                Debug.LogError(except.ToString());
            }
        }
    }
}