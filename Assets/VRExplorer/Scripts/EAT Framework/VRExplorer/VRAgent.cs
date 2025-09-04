using System;
using Unity.Plastic.Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRExplorer.Mono;
using VRExplorer.JSON;
using BNG;
using Microsoft.SqlServer.Server;

namespace VRExplorer
{
    public class VRAgent : BaseExplorer
    {
        private int _index = 0;
        private List<MonoBehaviour> _monos = new List<MonoBehaviour>();

        public bool useFileID = true;

        protected override bool TestFinished => _index >= _monos.Count;

        private static FileIdManagerMono GetOrCreateManager()
        {
            FileIdManagerMono manager = GameObject.FindObjectOfType<FileIdManagerMono>();
            if(manager == null)
            {
                GameObject go = new GameObject("FileIdManager");
                manager = go.AddComponent<FileIdManagerMono>();
                Debug.Log("Created FileIdManager in scene");
            }
            return manager;
        }

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
                // TaskList taskList = JsonUtility.FromJson<TaskList>(jsonContent);  ��֧�ֶ�̬
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

        public static void ImportTestPlan(string filePath = Str.TestPlanPath, bool useFileID = true)
        {
            TaskList tasklist = GetTaskListFromJson(filePath);

            // ��ȡ������FileIdManager
            FileIdManagerMono manager = GetOrCreateManager();
            manager.Clear();

            foreach(var taskUnit in tasklist.taskUnits)
            {
                foreach(var action in taskUnit.actionUnits)
                {
                    GameObject objA = FileIdResolver.FindGameObject(action.objectA, useFileID);

                    if(objA != null)
                        manager.Add(action.objectA, objA);


                    if(action.type == "Grab")
                    {
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

                        GrabActionUnit grabAction = action as GrabActionUnit;

                        if(grabAction.objectB != null)
                        {
                            // Handle grab action with two GUIDs
                            GameObject objB = FileIdResolver.FindGameObject(grabAction.objectB, useFileID);

                            if(objB != null)
                                manager.Add(grabAction.objectB, objB);

                            // Set destination to objectB
                            grabbable.destination = objB.transform;
                            Debug.Log($"Set {objA.name}'s destination to {objB.name}");
                        }
                        else if(grabAction.targetPosition != null)// ʹ�� Vector3��Ϊ target
                        {
                            Vector3 targetPos = (Vector3)grabAction.targetPosition;

                            // �Ȳ��ҳ������Ƿ�������ʱĿ��
                            GameObject targetObj = GameObject.Find($"{objA.name}_TargetPosition");
                            if(targetObj == null)
                            {
                                targetObj = new GameObject($"{objA.name}_TargetPosition_{Str.TempTargetTag}");
                                targetObj.transform.position = targetPos;

                                // ����ʱĿ��ӱ�ǣ��������ɾ��
                                targetObj.tag = Str.TempTargetTag;
                            }
                            else
                            {
                                targetObj.transform.position = targetPos; // ����λ��
                            }

                            grabbable.destination = targetObj.transform;
                            Debug.Log($"Set {objA.name}'s destination to position {targetPos}");
                        }
                        else
                        {
                            Debug.LogError("Lacking of Destination");
                        }

                        // Mark as dirty and save if it's a prefab
                        if(PrefabUtility.IsPartOfPrefabAsset(objA))
                        {
                            EditorUtility.SetDirty(objA);
                            AssetDatabase.SaveAssets();
                        }
                    }
                    else if(action.type == "Trigger")
                    {

                        TriggerActionUnit triggerAction = action as TriggerActionUnit;
                        if(triggerAction == null) continue;

                        XRTriggerable triggerable = objA.GetComponent<XRTriggerable>() ?? objA.AddComponent<XRTriggerable>();
                        if(triggerAction.trigerringTime != null) triggerable.triggeringTime = (float)triggerAction.trigerringTime;
                        FileIdResolver.BindEventList(triggerAction.triggerringEvents, triggerable.triggerringEvents);
                        FileIdResolver.BindEventList(triggerAction.triggerredEvents, triggerable.triggerredEvents);

                        if(PrefabUtility.IsPartOfPrefabAsset(objA))
                        {
                            EditorUtility.SetDirty(objA);
                            AssetDatabase.SaveAssets();
                        }
                    }
                    else if(action.type == "Transform")
                    {
                        TransformActionUnit transformAction = action as TransformActionUnit;
                        if(transformAction == null) continue;

                        XRTransformable transformable = objA.GetComponent<XRTransformable>();
                        if(transformable == null)
                        {
                            transformable = objA.AddComponent<XRTransformable>();
                            Debug.Log($"Added XRTransformable component to {objA.name}");
                        }

                        // ���ñ任����
                        if(transformAction.trigerringTime != null) transformable.triggerringTime = (float)transformAction.trigerringTime;
                        transformable.deltaPosition = transformAction.deltaPosition;
                        transformable.deltaRotation = transformAction.deltaRotation;
                        transformable.deltaScale = transformAction.deltaScale;

                        if(transformAction.trigerringTime != null)
                            transformable.triggerringTime = (float)transformAction.trigerringTime;

                        if(PrefabUtility.IsPartOfPrefabAsset(objA))
                        {
                            EditorUtility.SetDirty(objA);
                            AssetDatabase.SaveAssets();
                        }
                    }

                }
            }
        }

        public static void RemoveTestPlan(string filePath = Str.TestPlanPath, bool useFileID = true)
        {
            // �Ƴ���ʱĿ������
            var tempTargets = GameObject.FindGameObjectsWithTag(Str.TempTargetTag);
            foreach(var t in tempTargets)
            {
                DestroyImmediate(t);
            }

            // �Ƴ������� FileIdManager
            FileIdManagerMono manager = FindObjectOfType<FileIdManagerMono>();
            if(manager != null)
                DestroyImmediate(manager.gameObject);

            TaskList tasklist = GetTaskListFromJson(filePath);
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
                            // ����¼��б�
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

        private new void Start()
        {
            base.Start();
            var taskList = GetTaskListFromJson();  // ��ʼ��_taskList

            foreach(var taskUnit in taskList.taskUnits)
            {
                foreach(var action in taskUnit.actionUnits)
                {
                    GameObject objA = FindObjectOfType<FileIdManagerMono>().GetObject(action.objectA);
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

        protected override void ResetExploration()
        {
        }
    }



}