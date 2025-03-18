using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VRGuide : VRTest
{
	bool fetched;
	List<Quaternion> cachedTurns = new List<Quaternion>();
	Queue<Vector3> visited = new Queue<Vector3>();
	int memory = 10;
	
	public virtual void Initialize(){
		fetched = false;
	}
	
	private float calCutDistance(Vector3 movePos, HashSet<GameObject> inDistControls){
		float minCutDist = Mathf.Infinity;
		foreach(GameObject obj in inDistControls){
			float cutDist = calCutDistance(movePos, obj.transform.position);
			if(cutDist < minCutDist){
				minCutDist = cutDist;
			}
		}
		return minCutDist;
	}
	
	private void intersect(float d0i, float d0s, float d0t, float d1s, float d1t, float d2s, float d2t, float bd1h, float bd1l, float bd2h, float bd2l, FaceInfo fi, Vector3 dimension){
        float proportion = (d0i - d0s) / (d0t - d0s);
		float d1i = d1s + proportion * (d1t - d1s);
		float d2i = d2s + proportion * (d2t - d2s);
		if(d1i <= bd1h && d1i >= bd1l && d2i <= bd2h && d2i >= bd2l){
//			Vector3 interpoint = new Vector3(d0i, d1i, d2i);
			float dist = Mathf.Abs(d0i - d0t);//Vector3.Distance(interpoint, new Vector3(d0t, d1t, d2t));
			if(fi.CheckDist(dist)){
				Surface face = new Surface(dimension.x, dimension.y, dimension.z, d0i);
				face.SetVertices(bd1h, bd1l, bd2h, bd2l);
				fi.Update(face, dist);
			}
		}
	}
	
	private Surface acquireFacingSurface(Vector3 movePos, Vector3 targetPos){
		float xmax = Mathf.Max(movePos.x, targetPos.x);
		float xmin = Mathf.Min(movePos.x, targetPos.x);
		float ymax = Mathf.Max(movePos.y, targetPos.y);
		float ymin = Mathf.Min(movePos.y, targetPos.y);
		float zmax = Mathf.Max(movePos.z, targetPos.z);
		float zmin = Mathf.Min(movePos.z, targetPos.z);
		FaceInfo fi = new FaceInfo();
		foreach(GameObject obj in objects){
			Renderer r = obj.GetComponent<Renderer>();
			if(r != null && r.isVisible){
				Vector3 center = r.bounds.center;
				Vector3 extent = r.bounds.extents;
				float xplus = center.x + extent.x;
				if (xplus < xmax && xplus > xmin){
					 intersect(xplus, movePos.x, targetPos.x, movePos.y, targetPos.y, movePos.z, targetPos.z, center.y + extent.y, center.y - extent.y, center.z + extent.z, center.z - extent.z, fi, new Vector3(1, 0, 0));
				}
				float xminus = center.x - extent.x;
				if (xminus < xmax && xplus > xmin){
					 intersect(xminus, movePos.x, targetPos.x, movePos.y, targetPos.y, movePos.z, targetPos.z, center.y + extent.y, center.y - extent.y, center.z + extent.z, center.z - extent.z, fi, new Vector3(1, 0, 0));
				}
				float yplus = center.y + extent.y;
				if (yplus < ymax && yplus > ymin){
					 intersect(yplus, movePos.y, targetPos.y, movePos.x, targetPos.x, movePos.z, targetPos.z, center.x + extent.x, center.x - extent.x, center.z + extent.z, center.z - extent.z, fi, new Vector3(0, 1, 0));
				}
				float yminus = center.y - extent.y;
				if (yminus < ymax && yminus > ymin){
					 intersect(yminus, movePos.y, targetPos.y, movePos.x, targetPos.x, movePos.z, targetPos.z, center.x + extent.x, center.x - extent.x, center.z + extent.z, center.z - extent.z, fi, new Vector3(0, 1, 0));
				}
				float zplus = center.z + extent.z;
				if (zplus < zmax && zplus > zmin){
					 intersect(zplus, movePos.z, targetPos.z, movePos.x, targetPos.x, movePos.y, targetPos.y, center.x + extent.x, center.x - extent.x, center.y + extent.y, center.y - extent.y, fi, new Vector3(0, 0, 1));
				}
				float zminus = center.z - extent.z;
				if (zminus < zmax && zminus > zmin){
					 intersect(zminus, movePos.z, targetPos.z, movePos.x, targetPos.x, movePos.y, targetPos.y, center.x + extent.x, center.x - extent.x, center.y + extent.y, center.y - extent.y, fi, new Vector3(0, 0, 1));
				}
			}
		}
		return fi.GetFaceSurface();
	}

	
	private float calCutDistance(Vector3 movePos, Vector3 targetPos){
		Surface face = acquireFacingSurface(movePos, targetPos);
		if(face == null){
			return 0;
		}
		List<Surface> cuts = face.acquireCuts(targetPos);
		float minCutDist = Mathf.Infinity;
		foreach(Surface cut in cuts){
			float dist = cut.Distance(movePos);
			if (dist < minCutDist){
				minCutDist = dist;
			}
		}
		return minCutDist;
	}
	
	public override Vector3 Move(){
		if(cachedTurns.Count > 0){
			return transform.position;
		}else{
			FetchControls();
			UpdateMoves();
			fetched = false;
			HashSet<GameObject> inDistControls = new HashSet<GameObject>();			
			foreach(KeyValuePair<GameObject, ControlInfo> entry in controls){
				ControlInfo info = (ControlInfo) entry.Value;
				if(info.getTriggered() == 0){
					GameObject obj = info.getObject();
					float distance = Vector3.Distance(transform.position, obj.transform.position);
					if(distance < triggerlimit){
						inDistControls.Add(obj);
					}
				}
			}
			if(inDistControls.Count == 0){
				Debug.Log("All controls are triggered");
				return Vector3.zero;
			}else{
				Vector3 best = Vector3.zero;
				float mindiscut = Mathf.Infinity;
				foreach(Vector3 move in moves){
					Vector3 dest = transform.position + move * moveStep;
					if(Visited(dest)){
						continue;
					}
					float distance = calCutDistance(dest, inDistControls);
					if(distance < mindiscut){
						best = move;
						mindiscut = distance;
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
	protected class Surface{
		float A; //slope on x
		float B; //slope on y
		float C; //slope on z
		float D; //constant
		//Ax + By + Cz = D
		//four bounds of the surface presents a face of an enclosing box
		float d1h;
		float d1l;
		float d2h;
		float d2l;
		public Surface(float A, float B, float C, float D){
			this.A = A;
			this.B = B;
			this.C = C;
			this.D = D;
		}
		public void SetVertices(float d1h, float d1l, float d2h, float d2l){
			this.d1h = d1h;
			this.d1l = d1l;
			this.d2h = d2h;
			this.d2l = d2l;
		}
		public float Distance(Vector3 point){
			//Ax + Cz + By0 + (-D) = 0 is the project line of the surface on the ground
			//y0 is the y coordinate of the camera
			return Mathf.Abs(this.A*point.x + this.C*point.z + this.B*point.y - this.D) / Mathf.Sqrt(this.A*this.A + this.C*this.C);
			
		}
		private Vector3 acquireCutVector(float d1t, float d2t, float d1f, float d2f){
			return new Vector3(d2t-d2f, d1f-d1t, d1f*d2t-d1t*d2f);
		}
		public List<Surface> acquireCuts(Vector3 targetPos){
			List<Surface> faces = new List<Surface>();
			if(A != 0){
				if (targetPos.y < d1h){
					Vector3 f1v = acquireCutVector(targetPos.x, targetPos.y, D, d1h);
					faces.Add(new Surface(f1v.x, f1v.y, 0, f1v.z));
				}
				if (targetPos.y > d1l){
					Vector3 f1v = acquireCutVector(targetPos.x, targetPos.y, D, d1l);
					faces.Add(new Surface(f1v.x, f1v.y, 0, f1v.z));
				}
				if (targetPos.z < d2h){
					Vector3 f1v = acquireCutVector(targetPos.x, targetPos.z, D, d2h);
					faces.Add(new Surface(f1v.x, 0, f1v.y, f1v.z));
				}
				if (targetPos.z > d2l){
					Vector3 f1v = acquireCutVector(targetPos.x, targetPos.z, D, d2l);
					faces.Add(new Surface(f1v.x, 0, f1v.y, f1v.z));
				}
			}else if(B != 0){
				if (targetPos.x < d1h){
					Vector3 f1v = acquireCutVector(targetPos.y, targetPos.x, D, d1h);
					faces.Add(new Surface(f1v.y, f1v.x, 0, f1v.z));
				}
				if (targetPos.x > d1l){
					Vector3 f1v = acquireCutVector(targetPos.y, targetPos.x, D, d1l);
					faces.Add(new Surface(f1v.y, f1v.x, 0, f1v.z));
				}
				if (targetPos.z < d2h){
					Vector3 f1v = acquireCutVector(targetPos.y, targetPos.z, D, d2h);
					faces.Add(new Surface(0, f1v.x, f1v.y, f1v.z));
				}
				if (targetPos.z > d2l){
					Vector3 f1v = acquireCutVector(targetPos.y, targetPos.z, D, d2l);
					faces.Add(new Surface(0, f1v.x, f1v.y, f1v.z));
				}
			}else if(C != 0){
				if (targetPos.x < d1h){
					Vector3 f1v = acquireCutVector(targetPos.z, targetPos.x, D, d1h);
					faces.Add(new Surface(f1v.y, 0, f1v.x, f1v.z));
				}
				if (targetPos.x > d1l){
					Vector3 f1v = acquireCutVector(targetPos.z, targetPos.x, D, d1l);
					faces.Add(new Surface(f1v.y, 0, f1v.x, f1v.z));
				}
				if (targetPos.y < d2h){
					Vector3 f1v = acquireCutVector(targetPos.z, targetPos.y, D, d2h);
					faces.Add(new Surface(0, f1v.y, f1v.x, f1v.z));
				}
				if (targetPos.y > d2l){
					Vector3 f1v = acquireCutVector(targetPos.z, targetPos.y, D, d2l);
					faces.Add(new Surface(0, f1v.y, f1v.x, f1v.z));
				}
			}
			return faces;
		}
	}
	protected class FaceInfo{
		Surface faceSurface;
		float dist;
		public FaceInfo(){
			this.faceSurface = null;
			this.dist = Mathf.Infinity;
		}
		public bool CheckDist(float newdist){
			return newdist < dist;
		}
		public void Update(Surface face, float dist){
			this.faceSurface = face;
			this.dist = dist;
		}
		public Surface GetFaceSurface(){
			return faceSurface;
		}
	}
}
