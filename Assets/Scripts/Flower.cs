using UnityEngine;

/// <summary>
/// 管理一朵花
/// </summary>
public class Flower : MonoBehaviour
{
    //Tooltip特性使得在Inspector上能够显示出提示效果
    [Tooltip("当花满(fullFlower)的时候的颜色")]
    public Color fullFlowerColor = new Color(1f, 0, .3f);

    [Tooltip("当花空(emtpyFlower)的时候的颜色")]
    public Color emtpyFlowerColor = new Color(.5f, 0, 1f);

    [HideInInspector]
    public Collider nectarCollider;     //花蜜触发器
    private Collider flowerCollider;    //花碰撞体
    private Material flowerMaterial;    //花材质

    private float nectarAmount;         //花蜜数量

    //省去set索引器表示只读，不可以赋值
    //private set私有索引器，可以在类内部进行赋值
    public Vector3 FlowerUpVector       //每一朵花的开口方向
    {
        get { return nectarCollider.transform.up; }
    }
    public Vector3 FlowerCenterPosition //花的中心位置
    {
        get { return nectarCollider.transform.position; }
    }
    //private void Start()
    //{
    //    FlowerCenterPosition = 1;
    //    NectarAmount= 1;
    //}
    public float NectarAmount           //花蜜的数量
    {
        get { return nectarAmount; }
        private set { nectarAmount = value > 0 ? value : 0; }
    }

    public bool HasNectar               //是否有花蜜
    {
        get { return NectarAmount > 0; }
    }

    /// <summary>
    /// 鸟尝试去吃掉花蜜
    /// </summary>
    /// <param name="amount">尝试吃掉的数量</param>
    /// <returns>实际吃掉的数量</returns>
    public float Feed(float amount)
    {
        //Clamp(A,mi,mx)将A限制在[mi,mx]之间并返回实际限制的值
        //takenAmount、amount现在都表示实际吃掉的值
        float takenAmount = Mathf.Clamp(amount, 0f, NectarAmount);

        NectarAmount -= amount;

        if (HasNectar == false)
        {
            //花蜜没有以后，需要将花碰撞体、花蜜触发器 删除
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);
            //在使用 Unity 的内置着色器时，通常会使用 _BaseColor 来表示材质的
            //主要颜色或漫反射颜色。
            flowerMaterial.SetColor("_BaseColor", emtpyFlowerColor);

        }
        return takenAmount;
    }

    /// <summary>
    /// 恢复花的状态
    /// </summary>
    public void ResetFlower()
    {
        NectarAmount = 1f;
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }

    private void Awake()
    {
        //找到花的网格渲染器(Mesh Renderer)
        //Material material=GetComponent<Material>();，错误写法？存疑
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        flowerCollider = transform.GetChild(0).GetComponent<Collider>();
        nectarCollider = transform.GetChild(1).GetComponent<Collider>();

    }

}
