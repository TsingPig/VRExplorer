using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AssetCounter : MonoBehaviour
{
    [MenuItem("Tools/Count Project Files %#&c")] // Shift + Ctrl + Alt + C
    public static void CountAllAssets()
    {
        CountActiveGameObjectsInScene();
        string assetsPath = Application.dataPath; // 获取Assets目录绝对路径
        int totalFiles = CountFilesInDirectory(assetsPath, includeMeta: true);
        Debug.Log($"Total files in Assets (including .meta): {totalFiles}");

        // 示例：统计特定类型文件
        int scriptFiles = CountFilesByExtension(assetsPath, ".cs");
        Debug.Log($"Total C# scripts: {scriptFiles}");

        int prefabFiles = CountFilesByExtension(assetsPath, ".prefab");
        Debug.Log($"Total prefabs: {prefabFiles}");
    }

    public static void CountActiveGameObjectsInScene()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int activeCount = 0;
        int totalCount = 0;

        foreach(GameObject root in rootObjects)
        {
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            totalCount += allTransforms.Length;

            foreach(Transform t in allTransforms)
            {
                if(t.gameObject.activeInHierarchy) // Check if GameObject is active
                {
                    activeCount++;
                }
            }
        }

        Debug.Log($"Scene GameObject Report:\n" +
                  $"Active GameObjects: {activeCount}\n" +
                  $"Inactive GameObjects: {totalCount - activeCount}\n" +
                  $"Total GameObjects: {totalCount}\n" +
                  $"Root Objects: {rootObjects.Length}");
    }

    /// <summary>
    /// 递归统计目录下所有文件数量
    /// </summary>
    private static int CountFilesInDirectory(string path, bool includeMeta = false)
    {
        if(!Directory.Exists(path))
        {
            Debug.LogError($"Directory not found: {path}");
            return 0;
        }

        int count = 0;

        // 统计当前目录文件
        foreach(var file in Directory.GetFiles(path))
        {
            if(includeMeta || !file.EndsWith(".meta"))
            {
                count++;
            }
        }

        // 递归统计子目录
        foreach(var dir in Directory.GetDirectories(path))
        {
            count += CountFilesInDirectory(dir, includeMeta);
        }

        return count;
    }

    /// <summary>
    /// 按扩展名统计文件数量（不区分大小写）
    /// </summary>
    private static int CountFilesByExtension(string rootPath, string extension)
    {
        if(!extension.StartsWith("."))
            extension = "." + extension;

        var allFiles = Directory.GetFiles(rootPath, "*" + extension, SearchOption.AllDirectories)
                              .Where(file => !file.EndsWith(".meta")); // 排除.meta文件

        return allFiles.Count();
    }
}