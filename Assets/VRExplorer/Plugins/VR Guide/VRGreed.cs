using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VRGreed : VRTest
{
	bool fetched;
	List<Quaternion> cachedTurns = new List<Quaternion>();
	Queue<Vector3> visited = new Queue<Vector3>();
	int memory = 10;
	
	public virtual void Initialize(){
		fetched = false;
	}
	
	public override Vector3 Move(){
		if(cachedTurns.Count > 0){
			return transform.position;
		}else{
			FetchControls();
			UpdateMoves();
			fetched = false;
			GameObject closest = null;
			float mindis = Mathf.Infinity;
			foreach(KeyValuePair<GameObject, ControlInfo> entry in controls){
				ControlInfo info = (ControlInfo) entry.Value;
				if(info.getTriggered() == 0){
					GameObject obj = info.getObject();
					float distance = Vector3.Distance(transform.position, obj.transform.position);
					if(distance < mindis){
						mindis = distance;
						closest = obj;
					}
				}
			}
			if(closest == null){
				Debug.Log("All controls are triggered");
				return Vector3.zero;
			}else{
				Vector3 best = Vector3.zero;
				float bestdis = Mathf.Infinity;
				foreach(Vector3 move in moves){
					Vector3 dest = transform.position + move * moveStep;
					if(Visited(dest)){
						continue;
					}
					
					float distance = Vector3.Distance(dest, closest.transform.position);
					if(distance < bestdis){
						best = move;
						bestdis = distance;
					}
				}
				if(best == Vector3.zero){
					System.Random rnd = new System.Random();
					int n = rnd.Next(0, moves.Count);
					return transform.position + moves[n] * moveStep;
				}else{
					Vector3 dest = transform.position + best * moveStep;
					visited.Enqueue(dest);
					if(visited.Count > memory){
						visited.Dequeue();
					}
					return dest;
				}
			}
		}
	}
	
	public bool Visited(Vector3 dest){
		foreach(Vector3 v in visited){
			if(v == dest){
				return true;
			}
		}
		return false;
	}
	
	public override Quaternion Turn(){
		Debug.Log("Start Turning");
		if(!fetched){
			Debug.Log("Iterating controls" + controls.Count);
			foreach (KeyValuePair<GameObject, ControlInfo> entry in controls){
				ControlInfo control = (ControlInfo) entry.Value;
				Debug.Log("next:" + control.getObject());
				if(control.getTriggered() == 0){
					GameObject obj = control.getObject();
					Vector3 relativePos = obj.transform.position - transform.position;
					float dist = Vector3.Distance(transform.position, obj.transform.position);
					Debug.Log(dist);
					if(inscope(relativePos.y / dist) && dist < triggerlimit){
						Debug.Log(obj);
						RaycastHit hit;
						Physics.Raycast(transform.position, relativePos, out hit, triggerlimit);
						if(hit.collider.gameObject == obj){
							Debug.Log("add to cache:" + obj);
							cachedTurns.Add(Quaternion.LookRotation(relativePos, Vector3.up));
						}
					}
				}
			}
			fetched = true;
		}
		int turnCount = cachedTurns.Count;
		if(turnCount > 0){
			Quaternion lookto = cachedTurns[turnCount - 1];
			cachedTurns.RemoveAt(turnCount - 1);
			return lookto;
		}else{
			return transform.rotation;
		}
	}
	
	public bool inscope(float sin){
		if (sin > Mathf.Sin(turnLowerBound.x * 2 * Mathf.PI / 360) 
			&& sin < Mathf.Sin(turnUpperBound.x * 2 * Mathf.PI / 360)) {
			return true;
		}else{
			return false;
		}
	}
}
