using BNG;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Machine Learning Agent：强化学习智能体
/// </summary>
public class VRAgent : Agent
{
    private Grabbable[] _environmentGrabbables;      //场景中的可抓取物体
    private Vector3[] _initialGrabbablePositions;   //可抓取物体的初始位置
    private Quaternion[] _initialGrabbableRotations;//可抓取物体的初始旋转
    private Vector3 _initialPosition;       // Agent的初始位置
    private Quaternion _initialRotation;    // Agent的初始旋转
    
    public bool isGrabbing;


    public SmoothLocomotion smoothLocomotion;
    public BNGPlayerController player;
    public Transform rightHandGrabber;      // 右手变换
    //public Transform leftHandGrabber;       // 左手变换
    public Grabbable neareastGrabbable;     // 最近的可抓取物体
    
    public float grabbedReward = 0.5f;  // 抓取奖励
    public float grabbingReward = 0.0005f; // 持续抓取奖励
    public float ungrabbedReward = 0.2f; // 松手奖励
    public float idlePunishment = -0.00001f;

    /// <summary>
    /// 是否正在抓住物体
    /// </summary>
    public bool IsGrabbing
    {
        get { return isGrabbing; }
        set
        {
            if (value)
            {
                GrabbablerGrabbed += 1;
                AddReward(grabbedReward); // 抓取奖励
                currentReward += grabbedReward;
                Debug.Log($"抓住了物体，奖励增加{grabbedReward}");
            }
            else
            {
                AddReward(ungrabbedReward); // 放开奖励
                currentReward += ungrabbedReward;
                Debug.Log($"放开了物体，奖励增加{ungrabbedReward}");
            }
            isGrabbing = value;
        }
    }

    /// <summary>
    /// 已经完成的抓取次数
    /// </summary>
    public int GrabbablerGrabbed
    {
        get;
        private set;
    }


    public float currentReward = 0f;

    [Tooltip("是否正在训练模式下（trainingMode）")]
    public bool trainingMode;

    public float AreaDiameter = 40f;    // 场景的半径大小估算

    private float smoothPitchSpeedRate = 0f;
    private float smoothYawSpeedRate = 0f;
    private float smoothChangeRate = 2f;
    private float pitchSpeed = 200f;
    private float maxPitchAngle = 20f;       //最大俯冲角度
    private float yawSpeed = 200f;
    private float moveSpeed = 4f;
    private bool frozen = false;          //Agent是否处于非移动状态


    //在派生类中定义了与基类中同名的成员，导致隐藏了基类的成员。
    //使用new关键字显示的隐藏基类中的成员
    private new Rigidbody rigidbody;

    /// <summary>
    /// 初始化智能体
    /// </summary>
    public override void Initialize()
    {
        //override重写virtual方法，再base.Initialize()表示调用被重写的这个父类方法
        //这加起来相当于对虚方法进行功能的扩充。
        base.Initialize();
        player = FindObjectOfType<BNGPlayerController>();
        smoothLocomotion = player.GetComponentInChildren<SmoothLocomotion>();

        //leftHandGrabber = GameObject.Find("LeftController").transform.GetChild(2);
        rightHandGrabber = GameObject.Find("RightController").transform.GetChild(2);


        _environmentGrabbables = GetEnvironmentGrabbables();
        neareastGrabbable = GetNearestGrabbable();

        StoreAllGrabbableObjectsTransform();   // 保存场景中，可抓取物体的初始位置和旋转
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

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
        currentReward = 0;
        // 重置加载所有可抓取物体的位置和旋转
        LoadAllGrabbableObjectsTransform();


        transform.position = _initialPosition; // 设定初始位置
        transform.rotation = _initialRotation;  // 重置旋转


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
        //获取输入行为的数据
        var vectorAction = actions.ContinuousActions;
        //计算目标移动向量, targetDirection(dx,dy,dz)
        Vector3 targetMoveDirection = new Vector3(vectorAction[0], 0, vectorAction[2]);
        // 控制目标移动

        targetMoveDirection = smoothLocomotion.transform.TransformDirection(targetMoveDirection);

        smoothLocomotion.MoveCharacter(targetMoveDirection.normalized * Time.deltaTime * moveSpeed);
        //rigidbody?.AddForce(targetMoveDirection * moveForce);

        //获得当前旋转的状态(由于旋转的角度都是欧拉角，所以这里获得旋转的欧拉角
        Vector3 curRotation = smoothLocomotion.transform.rotation.eulerAngles;

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
        smoothLocomotion.transform.rotation = Quaternion.Euler(pitch, yaw, 0);

        // 获取离散动作
        var discreteActions = actions.DiscreteActions;
        int triggerGrab = discreteActions[0]; // 是否抓取（触发键）
        int gripGrab = discreteActions[1];    // 是否抓取（手柄键）

        InputBridge.Instance.RightTrigger = triggerGrab;
        InputBridge.Instance.RightGrip = gripGrab;

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

        //添加：相对于父物体的局部旋转，即相对于小岛的旋转（4）
        //单位四元数是长度为1的四元数，用于表示旋转方向
        Quaternion relativeRotation = transform.localRotation.normalized;

        ////添加：从右手，指向最近可抓取的物体的向量(3)
        Vector3 rightHandToNeareastGrabbable = neareastGrabbable.transform.position - rightHandGrabber.transform.position;


        //添加：手到最近可抓取物体的相对（相对场景）距离(1）
        float relativeDistance = rightHandToNeareastGrabbable.magnitude / AreaDiameter;


        sensor.AddObservation(relativeRotation);
        sensor.AddObservation(rightHandToNeareastGrabbable.normalized);
        sensor.AddObservation(relativeDistance);

        // 添加：右手的按键状态(2)
        sensor.AddObservation(InputBridge.Instance.RightGrip);
        sensor.AddObservation(InputBridge.Instance.RightTrigger);

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
        Debug.Log("Heuristic");
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        // WASD 移动控制
        Vector3 moveDirection = Vector3.zero;
        if(Input.GetKey(KeyCode.W)) moveDirection += Vector3.forward;
        if(Input.GetKey(KeyCode.S)) moveDirection += Vector3.back;
        if(Input.GetKey(KeyCode.A)) moveDirection += Vector3.left;
        if(Input.GetKey(KeyCode.D)) moveDirection += Vector3.right;

        // 鼠标控制旋转
        float pitch = -Input.GetAxis("Mouse Y"); // 垂直旋转（上下视角）
        float yaw = Input.GetAxis("Mouse X");   // 水平旋转（左右视角）

        // Z/X 键控制抓取动作
        float rightTrigger = Input.GetKey(KeyCode.Z) ? 1f : 0f;
        float rightGrip = Input.GetKey(KeyCode.X) ? 1f : 0f;

        // 归一化移动方向
        moveDirection = moveDirection.normalized;

        // 将输入映射到 ContinuousActions
        continuousActions[0] = moveDirection.x; // X 方向移动
        continuousActions[1] = moveDirection.y; // Y 方向移动
        continuousActions[2] = moveDirection.z; // Z 方向移动
        continuousActions[3] = pitch;           // 垂直视角旋转
        continuousActions[4] = yaw;             // 水平视角旋转

        // 将输入映射到 DiscreteActions (抓取动作)
        discreteActions[0] = rightTrigger > 0 ? 1 : 0; // 扳机抓取
        discreteActions[1] = rightGrip > 0 ? 1 : 0;    // 手柄抓取
    }


    /// <summary>
    /// 在玩家控制模式下，控制冻结智能体
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "训练模式不支持冻结智能体。");
        //frozen = true;

    }

    /// <summary>
    /// 在玩家控制模式下，解冻智能体
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "训练模式不支持解冻智能体。");
        frozen = false;
    }

    /// <summary>
    /// 获取场景中所有的可抓取物体列表
    /// </summary>
    private Grabbable[] GetEnvironmentGrabbables()
    {
        var allGrabbables = Object.FindObjectsOfType<Grabbable>();
        var environmentGrabbables = allGrabbables.Except(transform.GetComponentsInChildren<Grabbable>()).ToArray();
        Debug.Log($"场景中的可交互物体有{environmentGrabbables.Length}个");
        return environmentGrabbables;
    }

    /// <summary>
    /// 存储所有场景中可抓取物体的变换信息
    /// </summary>
    private void StoreAllGrabbableObjectsTransform()
    {
        _initialGrabbablePositions = new Vector3[_environmentGrabbables.Length];
        _initialGrabbableRotations = new Quaternion[_environmentGrabbables.Length];

        for(int i = 0; i < _environmentGrabbables.Length; i++)
        {
            _initialGrabbablePositions[i] = _environmentGrabbables[i].transform.position;
            _initialGrabbableRotations[i] = _environmentGrabbables[i].transform.rotation;
        }
    }

    /// <summary>
    /// 重置加载所有可抓取物体的位置和旋转
    /// </summary>
    private void LoadAllGrabbableObjectsTransform()
    {
        for(int i = 0; i < _environmentGrabbables.Length; i++)
        {
            _environmentGrabbables[i].transform.position = _initialGrabbablePositions[i];
            _environmentGrabbables[i].transform.rotation = _initialGrabbableRotations[i];

            // 如果有 Rigidbody，则清除速度
            Rigidbody rb = _environmentGrabbables[i].GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 获取最近的可抓取物体
    /// </summary>
    private Grabbable GetNearestGrabbable()
    {
        var res = _environmentGrabbables.OrderBy(grabbable => Vector3.Distance(rightHandGrabber.position, grabbable.transform.position)).FirstOrDefault();
        if(res == null)
        {
            _environmentGrabbables = GetEnvironmentGrabbables();
            Debug.LogWarning("出现可抓取物体引用丢失");
            return GetNearestGrabbable();
        }
        return res;
    }

    private void Update()
    {
        //Debug.Log(InputBridge.Instance.RightTrigger);   // 扣动扳机
        //Debug.Log(InputBridge.Instance.RightGrip);      // 抓取
        //Debug.Log(InputBridge.Instance.RightThumbNear); // 大拇指按下
        //InputBridge.Instance.RightGrip = 1f;

        neareastGrabbable = GetNearestGrabbable();

        if(trainingMode)
        {
            AddReward(idlePunishment);
            currentReward += idlePunishment;
        }

    }

    private void FixedUpdate()
    {
        //smoothLocomotion.MoveCharacter(Vector3.forward *  Time.deltaTime);

    }

    private void Start()
    {
        var handler = rightHandGrabber.gameObject.AddComponent<GrabberCollisionHandler>();
        handler.vrAgent = this;
    }


    /// <summary>
    /// Grabber接触到可抓取物体时调用
    /// </summary>
    /// <param name="collider"></param>
    public void OnGrabberTriggerEnter(Collider collider)
    {
        Grabbable grabbable = collider.transform.GetComponent<Grabbable>();
        if(_environmentGrabbables.Contains(grabbable) && trainingMode)
        {
        }

    }

    /// <summary>
    /// Grabber离开可抓取物体时调用
    /// </summary>
    /// <param name="collider"></param>
    public void OnGrabberTriggerExit(Collider collider)
    {
        Grabbable grabbable = collider.transform.GetComponent<Grabbable>();
        if(_environmentGrabbables.Contains(grabbable) && trainingMode)
        {
        }
    }

    /// <summary>
    /// Grabber持续接触可抓取物体时调用
    /// </summary>
    /// <param name="collider"></param>
    public void OnGrabberTriggerStay(Collider collider)
    {
        Grabbable grabbable = collider.transform.GetComponent<Grabbable>();
        if(_environmentGrabbables.Contains(grabbable) && trainingMode)
        {
            // 如果处于抓取状态，给予抓取奖励
            if(InputBridge.Instance.RightGrip == 1f)
            {
                if(!IsGrabbing)
                {
                    IsGrabbing = true; // 更新抓取状态
                }
                else
                {
                    // 处于持续抓取状态
                    AddReward(grabbingReward); // 抓取奖励
                    currentReward += grabbingReward;
                    Debug.Log($"持续抓取，奖励增加{grabbingReward}");
                }
            }
            // 如果从抓取状态切换到非抓取状态，给予放开奖励
            else if(IsGrabbing && InputBridge.Instance.RightGrip < 1f)
            {
                IsGrabbing = false; // 更新抓取状态
            }

        }
    }


}