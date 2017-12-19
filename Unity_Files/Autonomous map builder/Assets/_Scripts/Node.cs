using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//Class for representing nodes in the graph of the simulated world
public class Node : IComparable<Node> {
	public bool unknown;		// From robot's perspective
	public bool notWall;		// Walkable
	public Vector2 position;	// Position in world space
	public bool isFrontier;		// Edge node in graph
	public int[] gridPosition;	// Position in the Node's Grid (i.e. [x, y])
	public float fCost; 		// f-cost for A*
	public float gCost; 		// g-cost for G*
	public GameObject myObject;	// Object that the node is attached to (for visualization)

	// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Class Functions -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //
	//Constructor
	//Called when grids of knowledge are being initialized
	public Node(bool not_known, bool not_wall, Vector2 pos, int[] gridPos){
		unknown = not_known;
		notWall = not_wall;
		position = pos;
		gridPosition = gridPos;

		fCost = 1000;
		gCost = 1000;
		isFrontier = false;
	}

	//Node comparing for A* based on f cost
	public int CompareTo(Node otherNode){
		if (this.fCost > otherNode.fCost) return 1;
		else if (this.fCost == otherNode.fCost) return 0;
		else return -1;
	}

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Discovery Functions -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= //

	//Set Node to frontier of known world
	public void MakeFrontier(){
		myObject.GetComponent<Renderer>().material.color = Color.yellow;
		isFrontier = true;
	}

	//Set Node to known and walkable
	public void MakeEmpty(){
		myObject.GetComponent<Renderer>().material.color = Color.white;
		unknown = false;
		notWall = true;
		isFrontier = false;
	}

	//Set Node to known and unwalkable
	public void MakeWall(){
		myObject.GetComponent<Renderer>().material.color = Color.black;
		unknown = false;
		notWall = false;
	}

	//Show that this node is the robot's current location
	public void MakeRobot(){
		myObject.GetComponent<Renderer>().material.color = Color.green;
	}
}
