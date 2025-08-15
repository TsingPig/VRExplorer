#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;

namespace VRExplorer
{
    public class TestPlanImporterWindow : EditorWindow
    {
        private string filePath = Str.TestPlanPath;
        private GameObject selectedObject;  // ����ѡ�񳡾��е�����

        [MenuItem("Tools/VR Explorer/Import Test Plan")]
        public static void ShowWindow()
        {
            GetWindow<TestPlanImporterWindow>("Test Plan Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Test Plan Importer", EditorStyles.boldLabel);

            // ����ѡ����
            selectedObject = (GameObject)EditorGUILayout.ObjectField("Select Object", selectedObject, typeof(GameObject), true);

            // ��ӡGUID��ť
            if(GUILayout.Button("Print Object GUID") && selectedObject != null)
            {
                string guid = VREscaper.GetObjectGuid(selectedObject);
                Debug.Log($"GUID for {selectedObject.name}: {guid}");
                EditorGUIUtility.systemCopyBuffer = guid;  // ���Ƶ�������
                ShowNotification(new GUIContent($"GUID copied to clipboard: {guid}"));
            }

            if(GUILayout.Button("Print Object FileID") && selectedObject != null)
            {
                try
                {
                    long fileId = GetSceneObjectFileID(selectedObject);
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
            GUILayout.Space(20);

            // TestPlan�ļ�·��ѡ��
            GUILayout.BeginHorizontal();
            filePath = EditorGUILayout.TextField("Test Plan Path", filePath);
            if(GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                filePath = EditorUtility.OpenFilePanel("Select Test Plan", "Assets", "json");
            }
            GUILayout.EndHorizontal();

            // ���밴ť
            if(GUILayout.Button("Import Test Plan"))
            {
                VREscaper.ImportTestPlan(filePath);
            }
        }

        private long GetSceneObjectFileID(GameObject go)
        {
            if(go == null) return 0;

            var scenePath = go.scene.path;
            if(string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("Scene not saved to disk!");
                return 0;
            }

            var lines = System.IO.File.ReadAllLines(scenePath);
            for(int i = 0; i < lines.Length; i++)
            {
                if(lines[i].Contains("m_Name: " + go.name))
                {
                    // ���ϲ��� "--- !u!1 &<fileID>"
                    for(int j = i; j >= 0; j--)
                    {
                        if(lines[j].StartsWith("--- !u!1 &"))
                        {
                            string fileIDStr = lines[j].Substring("--- !u!1 &".Length);
                            if(long.TryParse(fileIDStr, out long fileID))
                                return fileID;
                        }
                    }
                }
            }

            return 0;
        }

    }
}
#endif