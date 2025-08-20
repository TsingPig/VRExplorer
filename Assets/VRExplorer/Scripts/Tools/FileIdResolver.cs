#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Policy;

namespace VRExplorer
{

    /// <summary>
    /// 构建 FileID -> GameObject 的映射
    /// </summary>
    public static class FileIdResolver
    {

        public static long GetObjectFileID(GameObject go)
        {
            if(go == null)
            {
                Debug.LogWarning("GameObject is null!");
                return 0;
            }

            // 获取 GlobalObjectId
            GlobalObjectId gid = GlobalObjectId.GetGlobalObjectIdSlow(go);
            string gidString = gid.ToString(); // 例如 "GlobalObjectId_V1-2-GUID-Part1-Part2-Part3"

            // 分割字符串
            string[] parts = gidString.Split('-');

            if(parts.Length < 2)
            {
                Debug.LogWarning("GlobalObjectId format unexpected: " + gidString);
                return 0;
            }

            // 判断 prefab instance 或普通 GameObject
            // prefab instance: 取最后一段
            // 普通对象: 取倒数第二段
            long fileID;
            if(go.scene.isLoaded && PrefabUtility.IsPartOfPrefabInstance(go))
            {
                // prefab instance，最后一段
                if(long.TryParse(parts[parts.Length - 1], out fileID))
                    return fileID;
            }
            else
            {
                // 普通 GameObject，倒数第二段
                if(long.TryParse(parts[parts.Length - 2], out fileID))
                    return fileID;
            }

            Debug.LogWarning("Failed to parse FileID from GlobalObjectId: " + gidString);
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

        public static GameObject FindGameObjectByFileID(long fileId)
        {
            if(fileId == 0)
            {
                Debug.LogWarning("FileID is 0");
                return null;
            }

            // 遍历场景中所有 GameObject
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach(GameObject go in allObjects)
            {
                long goFileId = GetObjectFileID(go);
                if(goFileId == fileId)
                {
                    if(go.scene.isLoaded && PrefabUtility.IsPartOfPrefabInstance(go))
                    {
                        GameObject g = go;
                        while(GetObjectFileID(g.transform.parent.gameObject) == fileId)
                        {
                            g = g.transform.parent.gameObject;
                        }
                        return g;
                    }
                    else
                    {
                        return go;
                    }
                }
            }

            // 如果是Prefab资源（不是场景实例），可以扫描所有Prefab
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach(string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab != null)
                {
                    long prefabFileId = GetObjectFileID(prefab);
                    if(prefabFileId == fileId)
                    {
                        return prefab;
                    }
                }
            }

            Debug.LogWarning($"Cannot find GameObject with FileID: {fileId}");
            return null;
        }

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

        public static GameObject FindGameObject(string id, bool useFileID)
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
    }
}
#endif
