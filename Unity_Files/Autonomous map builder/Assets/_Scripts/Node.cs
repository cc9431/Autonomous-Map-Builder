using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Node : IComparable<Node> {
	public bool unknown;		// From robot's perspective
	public bool notWall;		// Walkable
	public Vector2 position;	// Position in world space
	public bool isFrontier;		// Edge node in graph
	public int[] gridPosition;	// Position in the Node's Grid (i.e. [x, y])
	public float fCost; 		// f-cost for A*
	public float gCost; 		// g-cost for G*
	public float prob; 			// probability that robot is on node, for localization

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Constructor -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //
	public Node(bool not_known, bool not_wall, Vector2 pos, int[] gridPos){
		unknown = not_known;
		notWall = not_wall;
		position = pos;
		gridPosition = gridPos;

		fCost = 1000;
		gCost = 1000;
		prob = 0;
		isFrontier = false;
	}
	public Node(Node other){
		position = other.position;
		prob = other.prob;
	}

	public int CompareTo(Node otherNode){
		if (this.fCost > otherNode.fCost) return 1;
		else if (this.fCost == otherNode.fCost) return 0;
		else return -1;
	}
}
