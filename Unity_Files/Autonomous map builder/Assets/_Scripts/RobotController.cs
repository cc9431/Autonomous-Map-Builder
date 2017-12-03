using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour {
	private Node[,] robotGrid;
	private Grid worldGrid;
	private Vector2 offsetX;

	void Start(){
		worldGrid = Grid.get();
		int gx = (int) worldGrid.gridSize.x;
		int gy = (int) worldGrid.gridSize.y;
		robotGrid = new Node[gx, gy];

		offsetX = new Vector2(gx/2 + 1, 0);

		Vector2 bottomLeft = new Vector2(worldGrid.gameObject.transform.position.x,worldGrid.gameObject.transform.position.y);
		bottomLeft -= (Vector2.right * gx/2 + Vector2.up * gy/2);
		bottomLeft += offsetX;

		for (int x = 0; x < gx; x++){
			for (int y = 0; y < gy; y++){
				Vector2 gridPoint = bottomLeft + Vector2.right * (x * worldGrid.nodeDiameter + worldGrid.nodeRadius) + Vector2.up * (y * worldGrid.nodeDiameter + worldGrid.nodeRadius);
				robotGrid[x, y] = new Node(true, false, gridPoint);
			}
		}
	}

	private void Scan(){
		// Scan surroundings (get info from Grid.cs)
		// Apply learned info to robotGrid
		// Find frontier
	}

	private void Goto(Node pos){
		// Go to pos
		// Scan()
	}

	private void UpdateGrid(Node node){
		// Similar to GenerateGrid()
		// Update current knowledge of map
		
	}

	private void OnDrawGizmos() {
		// Draw grid based on current knowledge
		if (worldGrid.state == 3){
			foreach(Node n in robotGrid){
				if (!n.unknown){
					if (n.notWall){
						if (n.isFrontier) Gizmos.color = Color.yellow;
						else Gizmos.color = Color.green;
					} else Gizmos.color = Color.black;
				} else Gizmos.color = Color.gray;
				
				Gizmos.DrawCube(n.position + offsetX, Vector3.one * worldGrid.nodeDiameter);
			}
		}
	}
}
