#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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

            //����ѡ����
            selectedObject = (GameObject)EditorGUILayout.ObjectField("Select Object", selectedObject, typeof(GameObject), true);

            // ��ӡGUID��ť
            if(GUILayout.Button("Print Object GUID") && selectedObject != null)
            {
                string guid = VREscaper.GetObjectGuid(selectedObject);
                Debug.Log($"GUID for {selectedObject.name}: {guid}");
                EditorGUIUtility.systemCopyBuffer = guid;  // ���Ƶ�������
                ShowNotification(new GUIContent($"GUID copied to clipboard: {guid}"));
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
    }
}
#endif