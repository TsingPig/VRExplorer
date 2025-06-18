using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using System.IO;
using System.Xml;
using System.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameObjectConfigManager : EditorWindow
{
    #region UI Variables
    private GameObject targetObject;
    private string exportPath = "Assets/GameObjectConfig.xml";
    private string exportFolder = "Assets/VRExplorerExports";
    private string importPath = "Assets/GameObjectConfig.xml";
    private string importFolder = "Assets/VRExplorerExports";
    private bool exportPrefabAssets = true;
    private bool importPrefabAssets = true;
    #endregion

    [MenuItem("Tools/GameObject Config Manager")]
    public static void ShowWindow()
    {
        GetWindow<GameObjectConfigManager>("Config Manager");
    }

    private void OnGUI()
    {
        DrawExportUI();
        DrawImportUI();
        DrawCleanupUI();
    }

    #region Cleanup Functionality
    private void DrawCleanupUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("Cleanup", EditorStyles.boldLabel);

        if(GUILayout.Button("Remove All VRExplorer Scripts"))
        {
            if(EditorUtility.DisplayDialog("Confirmation",
                "This will permanently delete all scripts in the VRExplorer namespace. Continue?",
                "Delete", "Cancel"))
            {
                RemoveVRExplorerScripts();
            }
        }
    }

    [MenuItem("Tools/Remove VRExplorer Scripts")]
    public static void RemoveVRExplorerScripts()
    {
        try
        {
            // Clean up scene objects
            var sceneScripts = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()
                .Where(script => script != null)
                .ToArray();

            int sceneDeletedCount = 0;
            foreach(var script in sceneScripts)
            {
                Type type = script.GetType();
                if(type.Namespace != null && type.Namespace.StartsWith("VRExplorer"))
                {
                    Undo.DestroyObjectImmediate(script);
                    sceneDeletedCount++;
                }
            }

            // Clean up prefab assets
            int prefabDeletedCount = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach(string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if(prefab != null)
                {
                    MonoBehaviour[] components = prefab.GetComponents<MonoBehaviour>();
                    bool modified = false;

                    // Remove components from back to front
                    for(int i = components.Length - 1; i >= 0; i--)
                    {
                        var component = components[i];
                        if(component == null) continue;

                        Type type = component.GetType();
                        if(type.Namespace != null && type.Namespace.StartsWith("VRExplorer"))
                        {
                            UnityEngine.Object.DestroyImmediate(component, true);
                            prefabDeletedCount++;
                            modified = true;
                        }
                    }

                    if(modified)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Removed {sceneDeletedCount} VRExplorer scripts from scene and {prefabDeletedCount} from prefabs");
            EditorUtility.DisplayDialog("Complete",
                $"Removed {sceneDeletedCount} VRExplorer scripts from scene and {prefabDeletedCount} from prefabs", "OK");
        }
        catch(Exception e)
        {
            Debug.LogError($"Cleanup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error",
                $"Failed to remove scripts: {e.Message}", "OK");
        }
    }
    #endregion

    #region UI Drawing
    private void DrawExportUI()
    {
        GUILayout.Label("Export Configuration", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
        exportPrefabAssets = EditorGUILayout.Toggle("Include Prefab Assets", exportPrefabAssets);

        if(GUILayout.Button("Export Single GameObject"))
        {
            if(targetObject != null)
            {
                ExportConfig(targetObject, exportPath);
            }
            else
            {
                Debug.LogError("Please select a GameObject.");
            }
        }

        GUILayout.Space(10);
        exportFolder = EditorGUILayout.TextField("Export Folder", exportFolder);
        if(GUILayout.Button("Batch Export VRExplorer Objects"))
        {
            ExportAllVRExplorerObjects(exportFolder);
        }
    }

    private void DrawImportUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("Import Configuration", EditorStyles.boldLabel);
        importPath = EditorGUILayout.TextField("Import File Path", importPath);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Import Options", EditorStyles.boldLabel);
        importPrefabAssets = EditorGUILayout.Toggle("Apply to Prefab Assets", importPrefabAssets);

        if(GUILayout.Button("Import Single Config"))
        {
            ImportConfig(importPath);
        }

        GUILayout.Space(10);
        importFolder = EditorGUILayout.TextField("Import Folder", importFolder);
        if(GUILayout.Button("Batch Import Configs"))
        {
            ImportAllConfigs(importFolder);
        }
    }
    #endregion

    #region Export Functions
    private void ExportConfig(GameObject obj, string path)
    {
        try
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("GameObject");

            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(obj);
            root.SetAttribute("isPrefabAsset", isPrefabAsset.ToString());

            if(isPrefabAsset)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                root.SetAttribute("guid", guid);
                root.SetAttribute("name", obj.name);
                root.SetAttribute("path", assetPath);
            }
            else
            {
                root.SetAttribute("guid", GetGameObjectUniqueId(obj));
                root.SetAttribute("name", obj.name);
                root.SetAttribute("path", GetGameObjectPath(obj));
            }

            xmlDoc.AppendChild(root);

            // Export all VRExplorer namespace components
            MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
            foreach(var script in scripts)
            {
                if(script == null) continue;

                Type type = script.GetType();
                if(type.Namespace == null || !type.Namespace.StartsWith("VRExplorer"))
                    continue;

                ExportScriptData(xmlDoc, root, script, type);
            }

            // Ensure directory exists
            string dir = Path.GetDirectoryName(path);
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            xmlDoc.Save(path);
            Debug.Log($"Successfully exported config to {path}");
            AssetDatabase.Refresh();
        }
        catch(Exception e)
        {
            Debug.LogError($"Export failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ExportScriptData(XmlDocument xmlDoc, XmlElement parent, MonoBehaviour script, Type type)
    {
        XmlElement scriptElement = xmlDoc.CreateElement("Script");
        scriptElement.SetAttribute("type", type.FullName);
        scriptElement.SetAttribute("enabled", script.enabled.ToString());

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach(var field in fields)
        {
            if(field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
            {
                try
                {
                    object value = field.GetValue(script);
                    XmlElement fieldElement = xmlDoc.CreateElement("Field");
                    fieldElement.SetAttribute("name", field.Name);
                    fieldElement.SetAttribute("type", field.FieldType.FullName);
                    fieldElement.InnerText = SerializeValue(value, script.gameObject);
                    scriptElement.AppendChild(fieldElement);
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"Failed to serialize field {field.Name} in {type.Name}: {e.Message}");
                }
            }
        }
        parent.AppendChild(scriptElement);
    }

    private void ExportAllVRExplorerObjects(string folderPath)
    {
        try
        {
            if(!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var exportTargets = new HashSet<GameObject>();

            // Add scene objects
            GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(GameObject root in allObjects)
            {
                foreach(Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = child.gameObject;
                    if(HasVRExplorerComponent(go))
                    {
                        exportTargets.Add(go);
                    }
                }
            }

            // Add prefab assets if enabled
            if(exportPrefabAssets)
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach(string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if(prefab != null && HasVRExplorerComponent(prefab))
                    {
                        exportTargets.Add(prefab);
                    }
                }
            }

            // Export all targets
            foreach(GameObject go in exportTargets)
            {
                string safeName = go.name.Replace("/", "_").Replace("\\", "_");
                string filePath = Path.Combine(folderPath, $"{safeName}_{Guid.NewGuid()}.xml");
                ExportConfig(go, filePath);
            }

            Debug.Log($"Successfully exported {exportTargets.Count} GameObjects to {folderPath}");
            AssetDatabase.Refresh();
        }
        catch(Exception e)
        {
            Debug.LogError($"Batch export failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private bool HasVRExplorerComponent(GameObject go)
    {
        MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>();
        foreach(var script in scripts)
        {
            if(script == null) continue;
            Type type = script.GetType();
            if(type.Namespace != null && type.Namespace.StartsWith("VRExplorer"))
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Import Functions
    private void ImportConfig(string path)
    {
        if(!File.Exists(path))
        {
            Debug.LogError($"Config file not found: {path}");
            return;
        }

        try
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);

            XmlElement root = xmlDoc.DocumentElement;
            string guid = root.GetAttribute("guid");
            string originalPath = root.GetAttribute("path");
            bool isPrefabAsset = bool.Parse(root.GetAttribute("isPrefabAsset"));

            GameObject target = null;

            if(isPrefabAsset && importPrefabAssets)
            {
                // Load prefab asset directly
                target = AssetDatabase.LoadAssetAtPath<GameObject>(originalPath);
                if(target == null)
                {
                    Debug.LogError($"Prefab asset not found at: {originalPath}");
                    return;
                }

                // Remove existing VRExplorer components
                RemoveComponentsFromPrefab(target);

                // Apply new configuration
                ApplyConfigurationToPrefab(xmlDoc, root, target);

                // Save changes
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Successfully updated prefab asset: {target.name}");
                return;
            }
            else if(!isPrefabAsset || !importPrefabAssets)
            {
                // Handle scene objects
                target = FindGameObjectByUniqueId(guid);
                if(target == null && !string.IsNullOrEmpty(originalPath))
                {
                    target = FindOrCreateGameObjectByPath(originalPath);
                }

                if(target == null)
                {
                    Debug.LogError($"Failed to locate or create target GameObject for {originalPath}");
                    return;
                }

                // Remove existing VRExplorer components
                RemoveExistingVRExplorerComponents(target);

                // Apply configuration
                ApplyConfiguration(xmlDoc, root, target);

                Debug.Log($"Successfully imported config to {target.name}");
                EditorUtility.SetDirty(target);
            }
        }
        catch(Exception e)
        {
            Debug.LogError($"Import failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private void RemoveComponentsFromPrefab(GameObject prefab)
    {
        MonoBehaviour[] components = prefab.GetComponents<MonoBehaviour>();
        bool modified = false;

        // Remove from back to front
        for(int i = components.Length - 1; i >= 0; i--)
        {
            var component = components[i];
            if(component == null) continue;

            Type type = component.GetType();
            if(type.Namespace != null && type.Namespace.StartsWith("VRExplorer"))
            {
                UnityEngine.Object.DestroyImmediate(component, true);
                modified = true;
            }
        }

        if(modified)
        {
            EditorUtility.SetDirty(prefab);
        }
    }

    private void ApplyConfigurationToPrefab(XmlDocument xmlDoc, XmlElement root, GameObject prefab)
    {
        foreach(XmlNode scriptNode in root.SelectNodes("Script"))
        {
            XmlElement scriptElement = (XmlElement)scriptNode;
            string typeName = scriptElement.GetAttribute("type");
            bool enabled = bool.Parse(scriptElement.GetAttribute("enabled"));

            Type type = Type.GetType(typeName);
            if(type == null)
            {
                Debug.LogWarning($"Type not found: {typeName}");
                continue;
            }

            // Add component directly to prefab
            MonoBehaviour script = (MonoBehaviour)prefab.AddComponent(type);
            script.enabled = enabled;

            // Apply field values
            foreach(XmlNode fieldNode in scriptElement.SelectNodes("Field"))
            {
                XmlElement fieldElement = (XmlElement)fieldNode;
                string fieldName = fieldElement.GetAttribute("name");
                string fieldType = fieldElement.GetAttribute("type");
                string valueStr = fieldElement.InnerText;

                FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if(field == null) continue;

                try
                {
                    object value = DeserializeValue(valueStr, field.FieldType, prefab);
                    field.SetValue(script, value);
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"Failed to set field {field.Name} in {type.Name}: {e.Message}");
                }
            }
        }
    }

    private void RemoveExistingVRExplorerComponents(GameObject target)
    {
        MonoBehaviour[] existingScripts = target.GetComponents<MonoBehaviour>();
        List<MonoBehaviour> toDestroy = new List<MonoBehaviour>();

        foreach(var script in existingScripts)
        {
            if(script == null) continue;
            Type type = script.GetType();
            if(type.Namespace != null && type.Namespace.StartsWith("VRExplorer"))
            {
                toDestroy.Add(script);
            }
        }

        foreach(var script in toDestroy)
        {
            DestroyImmediate(script);
        }
    }

    private void ApplyConfiguration(XmlDocument xmlDoc, XmlElement root, GameObject target)
    {
        foreach(XmlNode scriptNode in root.SelectNodes("Script"))
        {
            XmlElement scriptElement = (XmlElement)scriptNode;
            string typeName = scriptElement.GetAttribute("type");
            bool enabled = bool.Parse(scriptElement.GetAttribute("enabled"));

            Type type = Type.GetType(typeName);
            if(type == null)
            {
                Debug.LogWarning($"Type not found: {typeName}");
                continue;
            }

            MonoBehaviour script = (MonoBehaviour)target.AddComponent(type);
            script.enabled = enabled;

            foreach(XmlNode fieldNode in scriptElement.SelectNodes("Field"))
            {
                XmlElement fieldElement = (XmlElement)fieldNode;
                string fieldName = fieldElement.GetAttribute("name");
                string fieldType = fieldElement.GetAttribute("type");
                string valueStr = fieldElement.InnerText;

                FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if(field == null) continue;

                try
                {
                    object value = DeserializeValue(valueStr, field.FieldType, target);
                    field.SetValue(script, value);
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"Failed to set field {field.Name} in {type.Name}: {e.Message}");
                }
            }
        }
    }

    private void ImportAllConfigs(string folderPath)
    {
        if(!Directory.Exists(folderPath))
        {
            Debug.LogError($"Import folder not found: {folderPath}");
            return;
        }

        try
        {
            string[] configFiles = Directory.GetFiles(folderPath, "*.xml");
            foreach(string file in configFiles)
            {
                ImportConfig(file);
            }

            Debug.Log($"Successfully imported {configFiles.Length} configs from {folderPath}");
            AssetDatabase.Refresh();
        }
        catch(Exception e)
        {
            Debug.LogError($"Batch import failed: {e.Message}\n{e.StackTrace}");
        }
    }
    #endregion

    #region GUID System
    private string GetGameObjectUniqueId(GameObject go)
    {
        if(go == null) return null;

        if(PrefabUtility.IsPartOfPrefabAsset(go))
        {
            string assetPath = AssetDatabase.GetAssetPath(go);
            return $"Asset:{AssetDatabase.AssetPathToGUID(assetPath)}";
        }

        if(PrefabUtility.IsPartOfPrefabInstance(go))
        {
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if(!string.IsNullOrEmpty(assetPath))
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                return $"Instance:{guid}";
            }
        }

        GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(go);
        return $"Scene:{globalId.ToString()}";
    }

    private GameObject FindGameObjectByUniqueId(string uniqueId)
    {
        if(string.IsNullOrEmpty(uniqueId)) return null;

        if(uniqueId.StartsWith("Asset:"))
        {
            string guid = uniqueId.Substring(6);
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if(!string.IsNullOrEmpty(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
        }
        else if(uniqueId.StartsWith("Instance:"))
        {
            string guid = uniqueId.Substring(9);
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if(!string.IsNullOrEmpty(assetPath))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if(prefab != null)
                {
                    GameObject[] instances = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach(GameObject instance in instances)
                    {
                        if(PrefabUtility.GetCorrespondingObjectFromSource(instance) == prefab)
                        {
                            return instance;
                        }
                    }

                    return PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                }
            }
        }
        else if(uniqueId.StartsWith("Scene:"))
        {
            string globalIdStr = uniqueId.Substring(6);
            GlobalObjectId globalId;
            if(GlobalObjectId.TryParse(globalIdStr, out globalId))
            {
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
            }
        }

        return null;
    }
    #endregion

    #region Utility Functions
    private string GetGameObjectPath(GameObject go)
    {
        if(PrefabUtility.IsPartOfPrefabAsset(go))
        {
            return AssetDatabase.GetAssetPath(go);
        }

        if(go.transform.parent == null)
            return go.name;

        return GetGameObjectPath(go.transform.parent.gameObject) + "/" + go.name;
    }

    private GameObject FindGameObjectByPath(string path)
    {
        if(string.IsNullOrEmpty(path)) return null;

        if(path.StartsWith("Assets/") && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach(GameObject root in rootObjects)
        {
            if(path == root.name) return root;

            Transform found = root.transform.Find(path);
            if(found != null) return found.gameObject;

            string[] parts = path.Split('/');
            Transform current = root.transform;

            for(int i = 0; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if(current == null) break;
            }

            if(current != null) return current.gameObject;
        }

        return null;
    }

    private GameObject FindOrCreateGameObjectByPath(string path)
    {
        GameObject existing = FindGameObjectByPath(path);
        if(existing != null) return existing;

        if(path.StartsWith("Assets/") && File.Exists(path))
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        string[] parts = path.Split('/');
        if(parts.Length == 0) return null;

        GameObject current = null;
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach(GameObject root in rootObjects)
        {
            if(root.name == parts[0])
            {
                current = root;
                break;
            }
        }

        if(current == null)
        {
            current = new GameObject(parts[0]);
            Undo.RegisterCreatedObjectUndo(current, "Create GameObject");
        }

        for(int i = 1; i < parts.Length; i++)
        {
            Transform child = current.transform.Find(parts[i]);
            if(child == null)
            {
                GameObject newChild = new GameObject(parts[i]);
                Undo.RegisterCreatedObjectUndo(newChild, "Create GameObject");
                newChild.transform.SetParent(current.transform);
                current = newChild;
            }
            else
            {
                current = child.gameObject;
            }
        }

        return current;
    }

    private string SerializeValue(object value, GameObject context)
    {
        if(value == null) return "null";

        Type t = value.GetType();

        if(t.IsPrimitive || t == typeof(string) || t.IsEnum)
            return value.ToString();

        if(t == typeof(Vector3))
        {
            Vector3 v = (Vector3)value;
            return $"Vector3({v.x:F3},{v.y:F3},{v.z:F3})";
        }

        if(t == typeof(Vector2))
        {
            Vector2 v = (Vector2)value;
            return $"Vector2({v.x:F3},{v.y:F3})";
        }

        if(t == typeof(Quaternion))
        {
            Quaternion q = (Quaternion)value;
            return $"Quaternion({q.x:F3},{q.y:F3},{q.z:F3},{q.w:F3})";
        }

        if(t == typeof(Color))
        {
            return ColorUtility.ToHtmlStringRGBA((Color)value);
        }

        if(t == typeof(Transform))
        {
            Transform tf = (Transform)value;
            if(tf == null) return "null";

            return $"TransformRef({GetGameObjectUniqueId(tf.gameObject)})";
        }

        if(t == typeof(GameObject))
        {
            GameObject go = (GameObject)value;
            if(go == null) return "null";

            return $"GameObjectRef({GetGameObjectUniqueId(go)})";
        }

        if(typeof(IList).IsAssignableFrom(t))
        {
            var list = value as IList;
            if(list == null) return "null";

            List<string> parts = new List<string>();
            foreach(var item in list)
            {
                parts.Add(SerializeValue(item, context));
            }
            return $"List[{string.Join(";", parts)}]";
        }

        if(typeof(IDictionary).IsAssignableFrom(t))
        {
            var dict = value as IDictionary;
            if(dict == null) return "null";

            List<string> entries = new List<string>();
            foreach(DictionaryEntry entry in dict)
            {
                entries.Add($"{SerializeValue(entry.Key, context)}:{SerializeValue(entry.Value, context)}");
            }
            return $"Dict[{string.Join(";", entries)}]";
        }

        return $"[Unsupported:{t.Name}]";
    }

    private object DeserializeValue(string valueStr, Type targetType, GameObject context)
    {
        if(valueStr == "null") return null;

        if(targetType == typeof(string)) return valueStr;
        if(targetType == typeof(int)) return int.Parse(valueStr);
        if(targetType == typeof(float)) return float.Parse(valueStr);
        if(targetType == typeof(bool)) return bool.Parse(valueStr);
        if(targetType.IsEnum) return Enum.Parse(targetType, valueStr);

        if(targetType == typeof(Vector3))
        {
            string[] parts = valueStr.Replace("Vector3(", "").Replace(")", "").Split(',');
            return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        }

        if(targetType == typeof(Vector2))
        {
            string[] parts = valueStr.Replace("Vector2(", "").Replace(")", "").Split(',');
            return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
        }

        if(targetType == typeof(Quaternion))
        {
            string[] parts = valueStr.Replace("Quaternion(", "").Replace(")", "").Split(',');
            return new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
        }

        if(targetType == typeof(Color))
        {
            Color color;
            if(ColorUtility.TryParseHtmlString("#" + valueStr, out color))
                return color;
            return Color.white;
        }

        if(targetType == typeof(GameObject))
        {
            if(valueStr.StartsWith("GameObjectRef("))
            {
                string guid = valueStr.Substring("GameObjectRef(".Length).TrimEnd(')');
                return FindGameObjectByUniqueId(guid);
            }
            return null;
        }

        if(targetType == typeof(Transform))
        {
            if(valueStr.StartsWith("TransformRef("))
            {
                string guid = valueStr.Substring("TransformRef(".Length).TrimEnd(')');
                GameObject go = FindGameObjectByUniqueId(guid);
                return go != null ? go.transform : null;
            }
            return null;
        }

        if(typeof(IList).IsAssignableFrom(targetType))
        {
            if(valueStr.StartsWith("List[") && valueStr.EndsWith("]"))
            {
                string content = valueStr.Substring(5, valueStr.Length - 6);
                string[] items = content.Split(';');

                IList list = (IList)Activator.CreateInstance(targetType);
                Type elementType = targetType.IsGenericType ?
                    targetType.GetGenericArguments()[0] :
                    targetType.GetElementType();

                foreach(string item in items)
                {
                    object value = DeserializeValue(item, elementType, context);
                    list.Add(value);
                }
                return list;
            }
            return null;
        }

        if(typeof(IDictionary).IsAssignableFrom(targetType))
        {
            if(valueStr.StartsWith("Dict[") && valueStr.EndsWith("]"))
            {
                string content = valueStr.Substring(5, valueStr.Length - 6);
                string[] entries = content.Split(';');

                IDictionary dict = (IDictionary)Activator.CreateInstance(targetType);
                Type[] genericArgs = targetType.GetGenericArguments();
                Type keyType = genericArgs[0];
                Type valueType = genericArgs[1];

                foreach(string entry in entries)
                {
                    string[] parts = entry.Split(':');
                    if(parts.Length == 2)
                    {
                        object key = DeserializeValue(parts[0], keyType, context);
                        object value = DeserializeValue(parts[1], valueType, context);
                        dict.Add(key, value);
                    }
                }
                return dict;
            }
            return null;
        }

        Debug.LogWarning($"Unsupported type for deserialization: {targetType.Name}");
        return null;
    }
    #endregion
}