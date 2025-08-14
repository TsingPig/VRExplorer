#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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

            //物体选择器
            selectedObject = (GameObject)EditorGUILayout.ObjectField("Select Object", selectedObject, typeof(GameObject), true);

            // 打印GUID按钮
            if(GUILayout.Button("Print Object GUID") && selectedObject != null)
            {
                string guid = VREscaper.GetObjectGuid(selectedObject);
                Debug.Log($"GUID for {selectedObject.name}: {guid}");
                EditorGUIUtility.systemCopyBuffer = guid;  // 复制到剪贴板
                ShowNotification(new GUIContent($"GUID copied to clipboard: {guid}"));
            }

            GUILayout.Space(20);

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
        }
    }
}
#endif