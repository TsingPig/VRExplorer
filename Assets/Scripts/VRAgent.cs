using BNG;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
/// <summary>
/// Machine Learning Agent：强化学习智能体
/// </summary>
public class VRAgent : Agent
{

    [Tooltip("是否正在训练模式下（trainingMode）")]
    public bool trainingMode;

    [Tooltip("右手变换")]
    public Transform modelsRight;
    [Tooltip("左手变换")]
    public Transform modelsLeft;


    [Tooltip("智能体摄像机")]
    public Camera agentCamera;

    /// <summary>
    /// 已经完成的抓取次数
    /// </summary>
    public int GrabbablerGrabbed
    {
        get;
        private set;
    }

    /// <summary>
    /// 鸟的物理中心位置
    /// </summary>
    public Vector3 BirdCenterPosition
    {
        get { return transform.position; }
        private set { transform.position = value; }
    }
    //等价于：public Vector3 BirdCenterPosition=>transform.position;

    /// <summary>
    /// 右手位置
    /// </summary>
    public Vector3 ModelsRightCenterPosition
    {
        get { return modelsRight.position; }
        private set { modelsRight.position = value; }
    }

    /// <summary>
    /// 右手位置
    /// </summary>
    public Vector3 ModelsLeftCenterPosition
    {
        get { return modelsLeft.position; }
        private set { modelsLeft.position = value; }
    }


    [Tooltip("当移动的时候施加在小鸟身上的力")]
    public float moveForce = 2f;

    [Tooltip("向上或者向下的俯冲旋转速度")]
    public float pitchSpeed = 100f;
    public float maxPitchAngle = 80f;       //最大俯冲角度

    [Tooltip("Y轴偏航旋转（角）速度")]
    public float yawSpeed = 100f;

    //平滑改变俯冲和偏航角速度率（-1f~1f）
    private float smoothPitchSpeedRate = 0f;
    private float smoothYawSpeedRate = 0f;
    private float smoothChangeRate = 2f;





    //在派生类中定义了与基类中同名的成员，导致隐藏了基类的成员。
    //使用new关键字显示的隐藏基类中的成员
    new private Rigidbody rigidbody;
    private FlowerArea flowerArea;      //agent正在处于的flowerArea，“一对一捆绑”
    private Flower nearestFlower;       //离agent最近的花



    private const float ModelsHandRadius = 0.008f; //手与Grabber的最大可碰撞距离

    private bool frozen = false;          //Agent是否处于非飞行状态



    /// <summary>
    /// 初始化智能体
    /// </summary>
    public override void Initialize()
    {
        //override重写virtual方法，再base.Initialize()表示调用被重写的这个父类方法
        //这加起来相当于对虚方法进行功能的扩充。
        base.Initialize();
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();      //小鸟是FlowerArea的直接孩子
        //MaxStep用于限制在训练模式下，在某个环境中能够执行的最大步数
        if(!trainingMode)
        {
            MaxStep = 0;         //非训练模式下，无最大步数限制（MaxStep=0）
        }
    }

    /// <summary>
    /// 在一个训练回合(Episode)开始的时候，重置这个智能体
    /// 我们会将智能体清空速度状态、采蜜状态，并且在训练模式下，重置小鸟所处
    /// 的花的区域<see cref="FlowerArea"/>，并随机化是否要在花前产卵。
    /// 然后移动到新的随机位置、并重新计算当前最近的花。
    /// </summary>
    public override void OnEpisodeBegin()
    {
        GrabbablerGrabbed = 0;                     //重置抓取的物体
        rigidbody.velocity = Vector3.zero;       //将速度和角速度归零
        rigidbody.angularVelocity = Vector3.zero;

        //默认情况下，要面向花
        bool inFrontOfFlower = true;

        //base.OnEpisodeBegin();
        if(trainingMode)
        {
            //当训练模式下、在每个花区域（flowerArea）只有一个智能体Agent的时候
            //这时候一只鸟捆绑到一个花的区域上。
            flowerArea.ResetFlowerPlants();

            //有50%的情况，让小鸟面朝花
            inFrontOfFlower = Random.value > .5f;
        }

        //将智能体随机移动到一个新的点位
        MoveToSafeRandomPosition(inFrontOfFlower);


    }

    /// <summary>
    /// 在每次Agent（可以是玩家、神经网络或其他形式的决策实体）接收到一个新行为的时候调用。(action received)
    /// 根据接收到的行为更新Agent的状态、执行特定的动作或触发相关的事件。
    /// 这个方法允许根据不同的行为来驱动Agent在游戏中进行相应的操作和决策，
    /// 以实现智能体的行为控制和学习。
    /// Index 0:x方向改变量（+1=向右，-1=向左）
    /// Index 1:y方向改变量(+1=up,-1=down)
    /// Index 2:z方向改变量(+1=forward,-1=backward)
    /// Index 3:俯冲角度改变量(+1=pitch up,-1=pitch down)
    /// Index 4:偏航角度改变量(+1=yaw turn right,-1=yaw turn left)
    /// </summary>
    /// <param name="actions">ActionBuffers类型对象。
    /// 用于存储接收到的行为的信息。通过访问actions对象的属性和方法，
    /// 可以获取和解析行为的具体内容，
    /// 例如位移、旋转、攻击等。</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if(frozen) return;
        //获取输入行为的数据
        var vectorAction = actions.ContinuousActions;
        //计算目标移动向量, targetDirection(dx,dy,dz)
        Vector3 targetMoveDirection = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        //在这个小鸟移动方向上，施加一个力
        rigidbody.AddForce(targetMoveDirection * moveForce);


        //获得当前旋转的状态(由于旋转的角度都是欧拉角，所以这里获得旋转的欧拉角
        Vector3 curRotation = transform.rotation.eulerAngles;

        //从输入行为中计算俯冲角速度率（-1~1）、偏航角速度率（-1~1）
        float targetPitchSpeedRate = vectorAction[3];
        float targetYawSpeedRate = vectorAction[4];

        //平滑计算，将smooth平滑计算过渡到targetDelta上。
        //smooth的中间过程代表当前已经计算到的、应该附加的变化量。
        smoothPitchSpeedRate = Mathf.MoveTowards(smoothPitchSpeedRate, targetPitchSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        smoothYawSpeedRate = Mathf.MoveTowards(smoothYawSpeedRate, targetYawSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        //p+=Rdp*dp*dt,y=Rdy*dy*dt
        float pitch = curRotation.x + smoothPitchSpeedRate * Time.fixedDeltaTime * pitchSpeed;
        float yaw = curRotation.y + smoothYawSpeedRate * Time.fixedDeltaTime * yawSpeed;
        if(pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

        //计算完后，将新得到的旋转角度覆盖到当前旋转状态。
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    /// <summary>
    /// 定义智能体收集观测数据的行为：如何获取和处理观测数据，
    /// 以便进行训练和决策。
    /// </summary>
    /// <param name="sensor">传进来的向量传感器，包含了智能体当前状态、环境的信息。
    /// 用来当作观测数据///</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        ////sensor.AddObservation(观测数据)用于将观测数据添加到智能体感知器，用于训练智能体
        ////当最近的花还没有设置出来的时候，要传递进去一个空的10维float数组
        //if(nearestFlower == null)
        //{
        //    sensor.AddObservation(new float[10]);
        //    return;
        //}
        ////添加：相对于父物体的局部旋转，即相对于小岛的旋转（4）
        ////单位四元数是长度为1的四元数，用于表示旋转方向
        //Quaternion relativeRotation = transform.localRotation.normalized;
        ////添加：指向花的向量(3)
        //Vector3 toFlower = nearestFlower.FlowerCenterPosition - BeakTipCenterPosition;
        ////toFlower.Normalize();
        ////添加：判断身体是否朝向花开口(+1代表直接在花面前，-1代表在花后面）(1)
        ////用向量点乘，A dot B > 0表示朝向相同，表示面朝花。 <0相反。为0则垂直
        //float positionAlignment = Vector3.Dot(toFlower.normalized,
        //    -nearestFlower.FlowerUpVector.normalized);
        ////添加：判断是否鸟喙朝向花开口(正，则表示鸟喙朝向花开口）(1)
        ////float beakTipAlignment = Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized);
        ////添加：鸟喙到花的相对（相对小岛）距离（1）
        //float relativeDistance = toFlower.magnitude / FlowerArea.areaDiameter;
        //sensor.AddObservation(relativeRotation);
        //sensor.AddObservation(toFlower.normalized);
        //sensor.AddObservation(positionAlignment);
        //sensor.AddObservation(beakTipAlignment);
        //sensor.AddObservation(relativeDistance);
        //总共10个观察

    }

    /// <summary>
    /// 当智能体的行为参数类型被设置为"Heuristic Only"调用这个函数。
    /// 返回值将被传递到<see cref="OnActionReceived(ActionBuffers)"/>
    /// 在启发式模式下，手动编写智能体的行为逻辑（启发式算法）而神经网络
    /// Index 0:x方向改变量（+1=向右，-1=向左）
    /// Index 1:y方向改变量(+1=up,-1=down)
    /// Index 2:z方向改变量(+1=forward,-1=backward)
    /// Index 3:俯冲角度改变量(+1=pitch up,-1=pitch down)
    /// Index 4:偏航角度改变量(+1=yaw turn right,-1=yaw turn left)
    /// </summary>
    /// <param name="actionsOut">存储智能体的行为输出</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector3 left = Vector3.zero;     //x
        Vector3 up = Vector3.zero;       //y
        Vector3 forward = Vector3.zero; //z
        float pitch = 0f;
        float yaw = 0f;
        //用户输入控制
        //将用户输入表示的移动和旋转，映射到上面的向量和浮点数
        if(Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if(Input.GetKey(KeyCode.S)) forward = (-1f) * transform.forward;

        if(Input.GetKey(KeyCode.LeftArrow)) left = (-1f) * transform.right;
        else if(Input.GetKey(KeyCode.RightArrow)) left = transform.right;

        if(Input.GetKey(KeyCode.Space)) left = transform.up;
        else if(Input.GetKey(KeyCode.LeftControl)) left = (-1f) * transform.up;

        if(Input.GetKey(KeyCode.UpArrow)) pitch = -1f;
        else if(Input.GetKey(KeyCode.DownArrow)) pitch = 1f;

        if(Input.GetKey(KeyCode.A)) yaw = -1f;
        else if(Input.GetKey(KeyCode.D)) yaw = 1f;

        Vector3 combinedDirection = (forward + up + left).normalized;

        actionsOut.ContinuousActions.Array[0] = combinedDirection.x;
        actionsOut.ContinuousActions.Array[1] = combinedDirection.y;
        actionsOut.ContinuousActions.Array[2] = combinedDirection.z;
        actionsOut.ContinuousActions.Array[3] = pitch;
        actionsOut.ContinuousActions.Array[4] = yaw;
    }

    /// <summary>
    /// 在玩家控制模式下，控制冻结智能体
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "训练模式不支持冻结智能体。");
        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    /// 在玩家控制模式下，解冻智能体
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "训练模式不支持解冻智能体。");
        frozen = false;
        rigidbody.WakeUp();
    }

    /// <summary>
    /// 将智能体重新移动到一个安全（比如不在碰撞体内）的位置处。
    /// 如果是在花前面，同时需要将喙伸入到花中
    /// </summary>
    /// <param name="inFrontOfFlower">是否要选择一个花前面的点。</param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;        //尝试保留次数（最大能够尝试次数）
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        //一直循环直到找到一个可以的点、或者超过尝试保留次数。
        while(!safePositionFound && attemptsRemaining > 0)
        {
            if(inFrontOfFlower)
            {   //需要面朝花
                //随机挑选一朵花
                int flowersCount = flowerArea.Flowers.Count;
                Flower randomFlower = flowerArea.Flowers[Random.Range(0, flowersCount)];

                //到花上面 10~20cm的距离 （disance*花向上的开口向量=偏移量Offset）
                float distanceFromFlower = Random.Range(.1f, .2f);
                //偏移量
                Vector3 offset = randomFlower.FlowerUpVector * distanceFromFlower;
                //潜在位置
                potentialPosition = randomFlower.transform.position + offset;

                //从潜在位置指向花中心的向量
                Vector3 toVFlower = potentialPosition - randomFlower.FlowerCenterPosition;
                //构造旋转鸟头的向量

                /*
                 Z axis will be aligned with forward, X axis aligned with cross product between forward 
                and upwards, and Y axis aligned with cross product between Z and X.
                 */
                potentialRotation = Quaternion.LookRotation(toVFlower, Vector3.up);
                //也就是说当LookRotation的两个向量不正交的时候，首先将Z轴对齐Forward向量，
                //并且用第二个参数约束大致的半界范围。
                //这会让鸟看向花的中心，并且不会颠倒。
            }
            else
            {   //不需要面朝花。只需要随机一个地面上的位置。随机生成一个离地高度
                float height = Random.Range(1.2f, 2.5f);
                //随机生成一个距离区域中心的距离
                float radius = Random.Range(2f, 7f);
                //随机选择一个方向，绕着Y轴旋转
                Quaternion direction = Quaternion.AngleAxis(Random.Range(-180f, 180f), Vector3.up);

                /*
                 四元数（Quaternion）与向量（Vector3）之间的乘法运算并不是传统的向量乘法。
                在Unity中，当一个四元数（Quaternion）与一个向量（Vector3）相乘时，使用的是四元数的旋转操作。
                具体地说，通过将四元数表示的旋转应用于向量，可以实现将该向量绕着旋转所表示的轴进行旋转的效果。
                 */
                //四元数和向量相乘表示这个向量按照这个四元数进行旋转之后得到的新的向量。
                Vector3 offset = Vector3.up * height + direction * Vector3.forward * radius;
                potentialPosition = flowerArea.FlowerAreaCenter + offset;

                //设置随机俯冲(Pitch)和偏航(Yaw)
                float pitch = Random.Range(-60f, 60f);          //俯冲按照x轴旋转
                float yaw = Random.Range(-180f, 180f);           //偏航按照y轴旋转
                potentialRotation = Quaternion.Euler(pitch, yaw, 0);

            }
            //需要判断是否新位置是否会产生碰撞
            safePositionFound = Physics.CheckSphere(potentialPosition, 0.05f);
            attemptsRemaining--;
        }
        //在循环结束的时候，要么超过保留次数（safe..=false)，要么找到安全位置
        //Debug.Assert(condition,message)传入一个条件，进行断言
        //当断言条件为假的时候，打印出message
        Debug.Assert(safePositionFound, "没有找到安全合适的随机位置");

        BirdCenterPosition = potentialPosition;
        //transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }


    ///// <summary>
    ///// 触发器触发停留事件:当智能体的碰撞体触碰到刚体的时候，
    ///// 我们需要确定该物体拥有Grabbable组件
    ///// </summary>
    ///// <param name="collider">花蜜碰撞体</param>
    //private void OnTriggerStay(Collider collider)
    //{
    //    if(collider.GetComponent<Grabbable>())
    //    {
    //        Vector3 closePointToBeakTip = collider.ClosestPoint(BeakTipCenterPosition);

    //        //表示鸟喙能够吃到花蜜
    //        if(Vector3.Distance(closePointToBeakTip, BeakTipCenterPosition) < ModelsHandRadius)
    //        {
    //            //找到花蜜碰撞体对应的Flower花
    //            Flower flower = flowerArea.GetFlowerFromNectar(collider);
    //            //尝试去吃掉0.01f的花蜜。
    //            //？//注意：这个事件是每0.02秒发生一次、一秒发生50次。
    //            GrabbablerGrabbed += 1;
    //            if(trainingMode)
    //            {
    //                ////用向量点乘，A dot B > 0表示朝向相同，表示面朝花。 <0相反。为0则垂直
    //                ////添加：判断是否鸟喙朝向花开口(正，则表示鸟喙朝向花开口）(1)
    //                //float beakTipAlignment = Vector3.Dot(beakTip.forward.normalized,
    //                //-nearestFlower.FlowerUpVector.normalized);
    //                float forwardAlignment = Vector3.Dot(transform.forward.normalized,
    //                    -nearestFlower.FlowerUpVector.normalized);
    //                //基础奖励0.01f，如果是正对着花进行采食，额外加最多0.02f分。
    //                float bonus = .02f * Mathf.Clamp01(forwardAlignment);
    //                float baseIncrement = .01f;
    //                float increment = baseIncrement + bonus;
    //                AddReward(increment);
    //            }
    //        }
    //        //记得更新flower
    //        if(!nearestFlower.HasNectar)
    //        {
    //            UpdateNearestFlower();
    //        }
    //    }
    //}



    private void Update()
    {
        //画一条从鸟喙指向最近的花的线
        if(nearestFlower != null)
        {
            //Debug.DrawLine(BeakTipCenterPosition, nearestFlower.FlowerCenterPosition, Color.green);


        }
    }
    private void FixedUpdate()
    {
        //要考虑对手抢走花的时候，更新
        if(nearestFlower != null && nearestFlower.HasNectar == false)
        {
            //UpdateNearestFlower();
        }
    }

    private void Start()
    {
        // 获取当前物体及其所有子物体的 Collider
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach(Collider col in colliders)
        {
            // 对手部Grabber绑定碰撞事件
            if(col.gameObject.name == "Grabber")
            {
                var collisionHandler = col.gameObject.AddComponent<GrabberCollisionHandler>();
                collisionHandler.vrAgent = this;
            }
        }
    }

    // 手部碰撞时调用
    public void OnGrabberCollisionEnter(Collision collision)
    {
        if(collision.collider.GetComponent<Grabbable>() != null && trainingMode)
        {
            //AddReward(-0.5f);
            Debug.Log("碰撞Grabbable物体");
        }
        if(collision.collider.GetComponent<Grabbable>() != null && !trainingMode)
        {
            //AddReward(-0.5f);
            Debug.Log("非trainingMode，碰撞Grabbable物体");
        }

    }

    // 子物体触发时调用
    public void OnGrabberTriggerEnter(Collider other)
    {
        Debug.Log("检测到 Grabber 子物体的触发事件");
        // 在此处处理触发逻辑
    }
}
