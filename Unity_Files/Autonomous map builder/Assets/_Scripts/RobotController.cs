using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour {
	private Node[,] robotGrid;
	private Grid worldGrid;
	private Vector2 offsetX;
	public static float radarStrength;
	public static bool canRun;

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= Unity Specific -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	void Start(){
		canRun = false;
		worldGrid = Grid.get();
		int gx = (int) worldGrid.gridSize.x;
		int gy = (int) worldGrid.gridSize.y;
		robotGrid = new Node[gx, gy];

		offsetX = new Vector2(gx/2 + 1, 0);

		Vector2 bottomLeft = new Vector2(worldGrid.gameObject.transform.position.x,worldGrid.gameObject.transform.position.y);
		bottomLeft -= (Vector2.right * gx/2 + Vector2.up * gy/2);
		bottomLeft += offsetX;

		// Initialize Grid based on current knowledge: grid size and current position
		for (int x = 0; x < gx; x++){
			for (int y = 0; y < gy; y++){
				Vector2 gridPoint = bottomLeft + Vector2.right * (x * worldGrid.nodeDiameter + worldGrid.nodeRadius) + Vector2.up * (y * worldGrid.nodeDiameter + worldGrid.nodeRadius);
				int[] gridPos = {x, y};
				robotGrid[x, y] = new Node(true, false, gridPoint, gridPos);
			}
		}

		// TEST
		StartCoroutine(ScanRotation());
	}

	void Update(){
		if(canRun){
			// Check current Node
			// If node is frontier, rotate and scan
			// Else goto next frontier node
		}
	}
	
	private void OnDrawGizmos() {
		// Draw grid based on current knowledge
		Node curNode = robotNodeFromWorldPoint(transform.position);
		if (worldGrid.state == 3){
			foreach(Node n in robotGrid){
				if (n == curNode) Gizmos.color = Color.green;
				else if (!n.unknown){
					if (n.notWall){
						if (n.isFrontier) Gizmos.color = Color.yellow;
						else Gizmos.color = Color.white;
					} else Gizmos.color = Color.black;
				} else Gizmos.color = Color.gray;
				
				Gizmos.DrawCube(n.position + offsetX, Vector3.one * worldGrid.nodeDiameter);
			}
		}
	}

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Mapping functions -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	private void ScanAndUpdate(){
		// Scan surroundings (get info from Grid.cs)
		// Apply learned info to robotGrid
		// Find frontier
		Ray ray = new Ray(transform.position, transform.up);
		RaycastHit[] hits = Physics.RaycastAll(ray, radarStrength);
		foreach(RaycastHit hit in hits){
			Node worldNode = worldGrid.nodeFromWorldPoint(hit.point);
			Node robotNode = robotNodeFromWorldPoint(hit.point);
			robotNode.unknown = false;
			robotNode.notWall = worldNode.notWall;
		}
	}

	private bool CheckFrontier(int[] nodePos){
		int x = nodePos[0];
		int y = nodePos[1];
		int gridX = (int) worldGrid.gridSize.x;
		int gridY = (int) worldGrid.gridSize.y;
		
		bool up = false;
		bool down = false;
		bool right = false;
		bool left = false;
		
		if (y + 1 < gridY) up = robotGrid[x, y + 1].unknown;
		if (y - 1 >= 0) 	down = robotGrid[x, y - 1].unknown;
		if (x + 1 < gridX) right = robotGrid[x + 1, y].unknown;
		if (x - 1 >= 0) 	left = robotGrid[x - 1, y].unknown;
		
		return (up || down || right || left);
	}

	IEnumerator ScanRotation(){
        float startRotation = transform.eulerAngles.z;
        float endRotation = startRotation + 360.0f;
		float duration = 3f;
        float t = 0.0f;
        while (t  < duration){
            t += Time.deltaTime;
            float zRotation = Mathf.Lerp(startRotation, endRotation, t / duration) % 360.0f;
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, zRotation);
			ScanAndUpdate();
            yield return null;
        }
    }

	private void Goto(Node pos){
		// Go to pos
		// Scan()
	}

	private Node robotNodeFromWorldPoint(Vector2 worldPos){
		int gx = (int) worldGrid.gridSize.x;
		int gy = (int) worldGrid.gridSize.y;

		float percentX = Mathf.Clamp01((worldPos.x + gx/2) / gx);
		float percentY = Mathf.Clamp01((worldPos.y + gy/2) / gy);

		return robotGrid[Mathf.RoundToInt((gx - 1) * percentX), Mathf.RoundToInt((gy - 1) * percentY)];
	}
}
