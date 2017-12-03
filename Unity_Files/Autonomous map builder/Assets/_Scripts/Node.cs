using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {
	public bool unknown;
	public bool notWall;
	public Vector2 position;
	public bool isFrontier;
	public bool isCharger;

	public Node(bool not_known, bool not_wall, Vector2 pos){
		unknown = not_known;
		notWall = not_wall;
		position = pos;

		isFrontier = false;
		isCharger = false;
	}
}
