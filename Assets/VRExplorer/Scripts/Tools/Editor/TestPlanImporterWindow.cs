#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VRExplorer
{
    public class TestPlanImporterWindow : EditorWindow
    {
        private string filePath = Str.TestPlanPath;
        private GameObject selectedObject;  // 用于选择场景中的物体

        [MenuItem("Tools/VR Explorer/Import Test Plan")]
        public static void ShowWindow()
        {
            GetWindow<TestPlanImporterWindow>("Test Plan Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Test Plan Importer", EditorStyles.boldLabel);

            // 物体选择器
            selectedObject = (GameObject)EditorGUILayout.ObjectField("Select Object", selectedObject, typeof(GameObject), true);

            // 打印GUID按钮
            if(GUILayout.Button("Print Object GUID") && selectedObject != null)
            {
                string guid = FileIdResolver.GetObjectGuid(selectedObject);
                Debug.Log($"GUID for {selectedObject.name}: {guid}");
                EditorGUIUtility.systemCopyBuffer = guid;  // 复制到剪贴板
                ShowNotification(new GUIContent($"GUID copied to clipboard: {guid}"));
            }

            if(GUILayout.Button("Print Object FileID") && selectedObject != null)
            {
                try
                {
                    long fileId = FileIdResolver.GetObjectFileID(selectedObject);
                    if(fileId != 0)
                    {
                        Debug.Log($"FileID for {selectedObject.name}: {fileId}");
                        EditorGUIUtility.systemCopyBuffer = fileId.ToString();
                        ShowNotification(new GUIContent($"FileID copied to clipboard: {fileId}"));
                    }
                    else
                    {
                        Debug.LogError($"Failed to get FileID for {selectedObject.name}. Is it a scene object?");
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError($"Failed to get FileID: {e.Message}");
                }
            }

            GUILayout.Space(30);

            // TestPlan文件路径选择
            GUILayout.BeginHorizontal();
            filePath = EditorGUILayout.TextField("Test Plan Path", filePath);
            if(GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                filePath = EditorUtility.OpenFilePanel("Select Test Plan", "Assets", "json");
            }
            GUILayout.EndHorizontal();


            // 导入按钮
            if(GUILayout.Button("Import Test Plan"))
            {
                VREscaper.ImportTestPlan(filePath);
            }

            if(GUILayout.Button("Remove Test Plan"))
            {
                if(EditorUtility.DisplayDialog("Remove Test Plan",
                   "This will remove all components added by the test plan. Continue?",
                   "Yes", "No"))
                {
                    VREscaper.RemoveTestPlan(filePath);
                }
            }
        }
    }

}
#endif
