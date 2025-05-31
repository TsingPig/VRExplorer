using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class VRMonkey : VRTest
{		
	bool move;
	
	public override void Initialize(){
		move = true;
	}
	
	
	public override Vector3 Move(){
		if(move){
			UpdateMoves();
			System.Random rnd = new System.Random();
			int n = rnd.Next(0, moves.Count);
			Debug.Log(n);
			Debug.Log(moves);
			return transform.position + moves[n] * moveStep;
		}else{
			return transform.position;
		}
	}
	
	public override Quaternion Turn(){
		System.Random rnd = new System.Random();
		int r = rnd.Next(0, 2);
		if (r == 0){
			move = false;
			UpdateTurns();
			int n = rnd.Next(0, turns.Count);
			
			Debug.Log(n);
			Debug.Log(turns.Count);
			internalangle = internalangle + turns[n] * turnStep;

			return Quaternion.Euler(internalangle);
		}else{
			move = true;
			return transform.rotation;
		}
	}
}
