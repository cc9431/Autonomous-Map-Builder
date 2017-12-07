using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Node : IComparable<Node> {
	public bool unknown;		// From robot's perspective
	public bool notWall;		// Walkable
	public Vector2 position;	// Position in world space
	public bool isFrontier;		// Edge node in graph
	public bool isCharger;		// Option for adding charging stations for robot
	public int[] gridPosition;	// Position in the Node's Grid (i.e. [x, y])
	public float fCost;
	public float gCost;

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Constructor -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //
	public Node(bool not_known, bool not_wall, Vector2 pos, int[] gridPos){
		unknown = not_known;
		notWall = not_wall;
		position = pos;
		gridPosition = gridPos;

		fCost = 1000;
		gCost = 1000;
		isFrontier = false;
		isCharger = false;
	}

	public int CompareTo(Node otherNode){
		if (this.fCost > otherNode.fCost) return 1;
		else if (this.fCost == otherNode.fCost) return 0;
		else return -1;
	}
}
