using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameObjectConfigExporter : EditorWindow
{
    private GameObject targetObject;
    private string exportPath = "Assets/GameObjectConfig.xml";

    [MenuItem("Tools/Export GameObject Config To XML")]
    public static void ShowWindow()
    {
        GetWindow<GameObjectConfigExporter>("Export Config");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export GameObject Config", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);

        if(GUILayout.Button("Export"))
        {
            if(targetObject != null)
                ExportConfig(targetObject, exportPath);
            else
                Debug.LogError("Please select a GameObject.");
        }
    }

    private void ExportConfig(GameObject obj, string path)
    {
        XmlDocument xmlDoc = new XmlDocument();
        XmlElement root = xmlDoc.CreateElement("GameObject");
        root.SetAttribute("name", obj.name);
        xmlDoc.AppendChild(root);

        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();

        foreach(var script in scripts)
        {
            if(script == null) continue;

            Type type = script.GetType();
            XmlElement scriptElement = xmlDoc.CreateElement("Script");
            scriptElement.SetAttribute("type", type.FullName);
            scriptElement.SetAttribute("enabled", script.enabled.ToString());

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach(var field in fields)
            {
                // 只导出可序列化字段
                if(field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                {
                    object value = field.GetValue(script);
                    XmlElement fieldElement = xmlDoc.CreateElement("Field");
                    fieldElement.SetAttribute("name", field.Name);
                    fieldElement.SetAttribute("type", field.FieldType.FullName);
                    fieldElement.InnerText = SerializeValue(value);
                    scriptElement.AppendChild(fieldElement);
                }
            }

            root.AppendChild(scriptElement);
        }

        xmlDoc.Save(path);
        Debug.Log($"Exported config to {path}");
        AssetDatabase.Refresh();
    }

    private string SerializeValue(object value)
    {
        if(value == null)
            return "None";

        Type t = value.GetType();

        if(t.IsPrimitive || t == typeof(string) || t.IsEnum)
            return value.ToString();

        if(t == typeof(Vector3))
            return ((Vector3)value).ToString("F3");  // e.g., (1.0, 2.0, 3.0)

        if(t == typeof(Vector2))
            return ((Vector2)value).ToString("F3");

        if(t == typeof(Color))
            return ColorUtility.ToHtmlStringRGBA((Color)value);

        if(t == typeof(Transform))
        {
            Transform tf = (Transform)value;
            return tf != null ? tf.name : "None";
        }

        if(t == typeof(GameObject))
        {
            GameObject go = (GameObject)value;
            return go != null ? go.name : "None";
        }

        if(typeof(IList).IsAssignableFrom(t))
        {
            var list = value as IList;
            if(list == null) return "None";

            List<string> parts = new List<string>();
            foreach(var item in list)
            {
                parts.Add(SerializeValue(item));
            }
            return string.Join("|", parts);  // 用竖线分隔数组项
        }

        return $"[Unsupported: {t.Name}]";
    }

}
