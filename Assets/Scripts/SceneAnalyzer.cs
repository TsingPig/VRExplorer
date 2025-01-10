using BNG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneAnalyzer : MonoBehaviour
{
    /// <summary>
    /// 通过脚本挂载的类对可交互物体进行分类，此处记录可抓取物体的挂载脚本过滤器
    /// </summary>
    public List<string> targetGrabTypeFilter = new List<string>();

    public List<GameObject> grabbableObjects = new List<GameObject>();

    /// <summary>
    /// 场景中的所有物体
    /// </summary>
    GameObject[] allGameObjects;

    /// <summary>
    /// 查找场景中挂载了指定脚本的所有游戏对象
    /// </summary>
    /// <param name="scriptName">脚本名称</param>
    /// <returns>所有挂载了指定脚本的游戏对象列表</returns>
    private List<GameObject> FindObjectsWithScript(string scriptName)
    {
        List<GameObject> result = new List<GameObject>();

        // 遍历所有对象，检查其组件是否包含指定脚本
        foreach(GameObject obj in allGameObjects)
        {
            // 获取该物体上的所有组件
            Component[] components = obj.GetComponents<Component>();

            foreach(Component component in components)
            {
                if(component != null && component.GetType().Name == scriptName)
                {
                    result.Add(obj);
                    break; // 已找到目标脚本，跳过当前对象的其他组件检查
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 通过挂载脚本类型过滤器，分类并记录可抓取的物体
    /// </summary>
    public void AnalyzeScene()
    {
        allGameObjects = FindObjectsOfType<GameObject>();

        foreach(string scriptName in targetGrabTypeFilter)
        {
            List<GameObject> objects = FindObjectsWithScript(scriptName);
            grabbableObjects.AddRange(objects);

            Debug.Log($"脚本 {scriptName} 挂载的对象数量: {objects.Count}");
        }

        foreach(GameObject obj in grabbableObjects)
        {
            if(!obj.GetComponent<Grabbable>())
            {
                obj.AddComponent<Grabbable>();
            }
        }

    }

    /// <summary>
    /// 测试入口
    /// </summary>
    private void Start()
    {
        targetGrabTypeFilter.Add("XRGrabInteractable");
        targetGrabTypeFilter.Add("Grabbable");

        // 分析场景
        AnalyzeScene();
    }
}
