using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理花簇(FlowerPlant)的列表、添加花
/// </summary>
public class FlowerArea : MonoBehaviour
{
    //Agent（智能体）和花可以使用的区域的直径
    //用于规范 Agent与花的相对距离
    public const float areaDiameter = 20f;

    public Vector3 FlowerAreaCenter
    {
        get { return transform.position; }
    }
    //花簇的列表
    private List<GameObject> flowerPlants;

    /// <summary>
    /// <see cref="Flower"/>列表。外部获取、内部修改的索引器，让小鸟不能够修改
    /// </summary>
    public List<Flower> Flowers
    {
        get;
        private set;
    }


    //花蜜触发器 -> 花（Flower组件）的字典
    //用于让小鸟在触发到某一个花蜜的时候，找到对应的花
    //设置了Nectar标签（tag），当小鸟碰到一个触发事件的时候，
    //我们会首先判断是否已经撞到了一个花蜜(NectarTagCollider)
    private Dictionary<Collider, Flower> nectarFlowerDictionary;



    /// <summary>
    /// 重置花和花簇
    /// </summary>
    public void ResetFlowerPlants()
    {
        //绕着Y轴旋转每一簇花簇，并且在X和Z轻微旋转
        foreach (var flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);

            /*
            常用四元数选择：

            (1)欧拉角到四元数的转换
            Quaternion quaternion = Quaternion.Euler(xRotation, yRotation, zRotation);
            

            (2)旋转矩阵到四元数（forward是物体的前方矢量，upwards是上方向矢量），
            Quaternion quaternion = Quaternion.LookRotation(forward, upwards);
            使得物体旋转后面向forward、头顶上是upwards
            
            (3)轴角度选择（angle是角度，axis是要旋转的轴）
            Quaternion quaternion = Quaternion.AngleAxis(angle, axis);
            
            (4)插值旋转（输入起始四元数和目标四元数，t介于0~1之间)
            Quaternion result = Quaternion.Lerp(startQuaternion, targetQuaternion, t);

             */
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation,
                yRotation, zRotation);

        }

        //恢复每一朵花的碰撞体
        foreach (var flower in Flowers)
        {
            flower.ResetFlower();
        }
    }


    /// <summary>
    /// 获得花蜜对应的<see cref="Flower"/>组件 
    /// </summary>
    /// <param name="collider">花蜜触发器</param>
    /// <returns>对应的<see cref="Flower"/>组件 </returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    /// <summary>
    /// 初始状态下，将这个<see cref="FlowerArea"/>下，
    /// 所有的<see cref="Flower"/>和<see cref="flowerPlant"/>都加入到<see cref="Flowers"/>和<see cref="flowerPlants"/>
    /// </summary>
    private void Start()
    {

        FindChildFlowers(transform);
    }

    /// <summary>
    /// 查找一个给定transform下的所有的Flower和FlowerPlants
    /// </summary>
    /// <param name="transform">需要检查的父变换</param>
    private void FindChildFlowers(Transform parent)
    {
        //递归终止条件1：找到一簇没有Flower的花簇
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag("FlowerPlant"))
            {
                flowerPlants.Add(child.gameObject);  //找到一簇花簇,加入到花簇列表中
                FindChildFlowers(child);     //递归查找
            }
            else
            {
                Flower flower = child.GetComponent<Flower>();   //不是花簇，就应该查找花
                if (flower != null)
                {   //递归终止条件2：找到一朵花。这是因为花里面默认不会嵌套花
                    Flowers.Add(flower);
                    //将花蜜触发器加到Collider->Flower字典中
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else
                {
                    FindChildFlowers(child);//也没有花，应该递归查找
                }

            }
        }
    }
}
