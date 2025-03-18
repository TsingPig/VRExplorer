using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.EventSystems;
using VRExplorer;
using System.Linq;

public class VRTest : MonoBehaviour
{
	protected static Dictionary<GameObject, ControlInfo> controls = new Dictionary<GameObject, ControlInfo>();
	protected static HashSet<GameObject> objects = new HashSet<GameObject>();
	protected static GameObject triggered;
	int index = 0;
	float passed = 0.0f;
	bool clickStay;
	
	Vector3 origin;
	Vector3 dest;
	Quaternion originrotate;
	Quaternion destrotate;
	protected Vector3 internalangle;
	
	public float moveStep = 6f;
    public float turnStep = 60f;
	protected float triggerlimit = 100f;
	protected Vector3 moveUpperBound = new Vector3 (7f, 4.4f, 11f);
	protected Vector3 moveLowerBound = new Vector3 (-14f, 4.3f, -1f);
	protected Vector3 turnUpperBound = new Vector3 (60f, 180f, 0f);
	protected Vector3 turnLowerBound = new Vector3 (-60f, -180f, 0f);
	
	protected Vector3[] moveOpts = new Vector3[6];
	protected Vector3[] turnOpts = new Vector3[6];
	protected List<Vector3> moves = new List<Vector3>();
	protected List<Vector3> turns = new List<Vector3>();

    private void Awake()
    {
        EntityManager.Instance.RegisterAllEntities();
    }

    float clickStayLength = 3f;
//	float eventGap = 1f;
	
	
    // Start is called before the first frame update

	void StartRecord()
	{
        ExperimentManager.Instance.StartRecording();

        controls.Clear();
        objects.Clear();
        FetchControls();

        moveOpts[0] = new Vector3(1f, 0f, 0f);
        moveOpts[1] = new Vector3(-1f, 0f, 0f);
        moveOpts[2] = new Vector3(0f, 1f, 0f);
        moveOpts[3] = new Vector3(0f, -1f, 0f);
        moveOpts[4] = new Vector3(0f, 0f, 1f);
        moveOpts[5] = new Vector3(0f, 0f, -1f);

        turnOpts[0] = new Vector3(1f, 0f, 0f);
        turnOpts[1] = new Vector3(-1f, 0f, 0f);
        turnOpts[2] = new Vector3(0f, 1f, 0f);
        turnOpts[3] = new Vector3(0f, -1f, 0f);
        turnOpts[4] = new Vector3(0f, 0f, 1f);
        turnOpts[5] = new Vector3(0f, 0f, -1f);

        dest = transform.position;
        destrotate = transform.rotation;

        //		internalangle = new Vector3(0f, 0f, 0f);
        //		transform.eulerAngles = internalangle;

        clickStay = false;
        passed = 0f;

        Initialize();
    }
    void Start()
    {
		Invoke("StartRecord", 2f);

    }
	
	public virtual void Initialize(){

	}

    protected void FetchControls()
    {
        objects.Clear();
        GameObject[] gos = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach(GameObject go in gos)
        {
            objects.Add(go);
            EventTrigger r = go.GetComponent<EventTrigger>();
            if(r != null && !controls.ContainsKey(go))
            {
                controls[go] = new ControlInfo(go);
                Debug.Log("Found Triggerable: " + go.name);
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((eventData) => { UpdateTrigger(); });
                r.triggers.Add(entry);
            }
        }

        // 使用 ToList() 避免直接修改字典
        foreach(GameObject obj in controls.Keys.ToList())
        {
            if(!objects.Contains(obj))
            {
                controls.Remove(obj);
            }
        }
    }


    public static void UpdateTrigger(){
		if(controls.ContainsKey(triggered)){
			Debug.Log("Triggered Recorded:" + controls[triggered]);
			controls[triggered].SetTrigger();
		}
	}

    void FixedUpdate()
    {
		if(clickStay){
			passed = passed + Time.deltaTime;
		    if(passed > clickStayLength){
				clickStay = false;
				passed = 0.0f;
			}
		}else{ 
			transform.position = Vector3.MoveTowards(transform.position, dest, moveStep*0.02f);
			Quaternion q = Quaternion.RotateTowards(transform.rotation, destrotate, turnStep*0.02f);
			q.eulerAngles = new Vector3(q.eulerAngles.x, q.eulerAngles.y, 0f);
			transform.rotation = q;

			if(Vector3.Distance(transform.position, dest) < 0.1f && Quaternion.Angle(transform.rotation, destrotate) < 0.1f){
				destrotate = Turn();
				Debug.Log("rotate:" + destrotate);
				dest = Move();
				Debug.Log("dest:" + dest);
				Trigger();
			}
		}
    }
	
	
	public virtual bool Movable(Vector3 position, int flag){
		switch(flag){
			case 0: return position.x + moveStep < moveUpperBound.x && !Physics.Raycast(position, Vector3.right, moveStep);
			case 1: return position.x - moveStep > moveLowerBound.x && !Physics.Raycast(position, Vector3.left, moveStep);
			case 2: return position.y + moveStep < moveUpperBound.y && !Physics.Raycast(position, Vector3.up, moveStep);
			case 3: return position.y - moveStep > moveLowerBound.y && !Physics.Raycast(position, Vector3.down, moveStep);
			case 4: return position.z + moveStep < moveUpperBound.z && !Physics.Raycast(position, Vector3.forward, moveStep);
			case 5: return position.z - moveStep > moveLowerBound.z && !Physics.Raycast(position, Vector3.back, moveStep);
			default: return false;
		}
	}
	
	public virtual void UpdateMoves(){
		moves.Clear();
		Vector3 position = transform.position;
		for(int i = 0; i < 6; i++){
			if(Movable(position, i)){
				moves.Add(moveOpts[i]);
			}
		}
	}

	public virtual Vector3 Move(){
		return transform.position;
	}
	
	public virtual Quaternion Turn(){
		return transform.rotation;
	}

	public virtual void Trigger(){
		RaycastHit hit;
        bool hitflag = Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity);
		if(hitflag){
			GameObject hitObj = hit.transform.gameObject;
			if(controls.ContainsKey(hitObj)){
				ControlInfo info = controls[hitObj];				
				if(info.getTriggered() == 0){
					triggered = hitObj;
					pointClick(hitObj);
					Debug.Log("Triggering Done!");
				}
			}
		}
	}


	public virtual bool Turnable(Vector3 angle, int flag){
		switch(flag){
			case 0: return angle.x + turnStep < turnUpperBound.x;
			case 1: return angle.x - turnStep > turnLowerBound.x;
			case 2: return angle.y + turnStep < turnUpperBound.y;
			case 3: return angle.y - turnStep > turnLowerBound.y;
			case 4: return angle.z + turnStep < turnUpperBound.z;
			case 5: return angle.z - turnStep > turnLowerBound.z;
			default: return false;
		}
	}

	public virtual void UpdateTurns(){
		turns.Clear();
		Vector3 angle = internalangle;
		for(int i = 0; i < 6; i++){
			if(Turnable(angle, i)){
				turns.Add(turnOpts[i]);
			}
		}
	}



    private static MonoBehaviour FindEntityMono(GameObject obj)
    {
        if(obj == null) return null;
        var monoBehaviour = obj.GetComponent<MonoBehaviour>();
        if(monoBehaviour is IBaseEntity)
        {
            return monoBehaviour;
        }
        if(obj.transform.parent != null)
        {
            return FindEntityMono(obj.transform.parent.gameObject);
        }
        return null;
    }


    protected void pointClick(GameObject obj){
		Debug.Log("clicking " + obj.name);
		PointerEventData pointerData = new PointerEventData(EventSystem.current)
		{
			position = Input.mousePosition
		};
		ExecuteEvents.Execute(obj, pointerData, ExecuteEvents.pointerClickHandler);
		EntityManager.Instance.UpdateMonoState(FindEntityMono(obj), true);
		//MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown | MouseOperations.MouseEventFlags.LeftUp);
		clickStay = true;
	}
	
	protected class ControlInfo{
		GameObject control;
		int triggered;
		public ControlInfo(GameObject obj){
			this.control = obj;
			this.triggered = 0;
		}
		public GameObject getObject(){
			return this.control;
		}
		public int getTriggered(){
			return this.triggered;
		}
		public void SetTrigger(){
			this.triggered = this.triggered + 1;
		}
	}
}