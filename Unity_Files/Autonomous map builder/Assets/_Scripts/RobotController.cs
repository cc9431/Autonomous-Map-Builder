using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RobotController : MonoBehaviour {
	private Node[,] robotGrid;			// Grid based on robot's knowledge
	private Grid worldGrid;				// Reference to the real grid
	private Vector2 offsetX;			// Offset for displaying robot's grid
	private Vector2 lastFrontier;		// Store the most recent frontier to not get stuck
	public float radarStrength;			// Strength of robot's visual field
	public GameObject nodePrefab;		// For visualizing Nodes

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= Unity Specific -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	void Start(){
		worldGrid = Grid.get();
		int gx = (int) worldGrid.gridSize.x;
		int gy = (int) worldGrid.gridSize.y;
		robotGrid = new Node[gx, gy];

		offsetX = new Vector2(gx + worldGrid.nodeDiameter, 0);

		Vector2 bottomLeft = new Vector2(worldGrid.gameObject.transform.position.x, worldGrid.gameObject.transform.position.y);
		bottomLeft -= (Vector2.right * gx/2 + Vector2.up * gy/2);
		bottomLeft += offsetX;

		// Initialize Grid based on current knowledge: grid size and current position
		for (int x = 0; x < gx; x++){
			for (int y = 0; y < gy; y++){
				Vector2 worldPoint = bottomLeft + Vector2.right * (x * worldGrid.nodeDiameter + worldGrid.nodeRadius) + Vector2.up * (y * worldGrid.nodeDiameter + worldGrid.nodeRadius);
				int[] gridPos = {x, y};
				robotGrid[x, y] = new Node(true, true, worldPoint, gridPos);
				GameObject current = (GameObject) Instantiate(nodePrefab, worldPoint, Quaternion.identity);
				robotGrid[x, y].myObject = current;
			}
		}

		int[] robotPos = worldGrid.nodeFromWorldPoint(transform.position).gridPosition;
		robotGrid[robotPos[0], robotPos[1]].unknown = false;
		robotNodeFromWorldPoint(transform.position).MakeRobot();
	}

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Mapping functions -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	public IEnumerator Rotation(){
        float startRotation = transform.eulerAngles.z;
        float endRotation = startRotation + 360.0f;
		float duration = 7f;
        float t = 0.0f;
		while(t < duration){
			t += Time.deltaTime;
			float zRotation = Mathf.Lerp(startRotation, endRotation, t / duration) % 360.0f;
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, zRotation);
			ScanAndUpdate();
			yield return null;
		}
		yield return StartCoroutine(AfterScan());
    }

	private void ScanAndUpdate(){
		// Scan surroundings (get info from Grid.cs)
		// Apply learned info to robotGrid
		// Find frontier
		//Debug.DrawRay(transform.position,transform.up * radarStrength, Color.white);
		Ray ray = new Ray(transform.position, transform.up);
		RaycastHit[] hits = Physics.RaycastAll(ray, radarStrength);
		hits = hits.OrderBy(hit=>hit.distance).ToArray();
		foreach(RaycastHit hit in hits){
			GameObject hitObject = hit.collider.gameObject;
			Node worldNode = worldGrid.nodeFromWorldPoint(hitObject.transform.position);
			Node robotNode = robotNodeFromWorldPoint(hitObject.transform.position);
			
			if (worldNode.notWall) robotNode.MakeEmpty();
			else robotNode.MakeWall();

			if (!robotNode.notWall) break;
		}
		robotNodeFromWorldPoint(transform.position).MakeRobot();
	}

	private IEnumerator AfterScan(){
		List<Vector2> next = CreateFrontier();
		if(next.Count == 0){
			worldGrid.finished();
			StopAllCoroutines();
		} else{
			if(next.Count != 1 && next[0] == lastFrontier) next = next.GetRange(1, next.Count - 1);
			lastFrontier = next[0];
			List<Vector2> path = AStarPath(next);
			if (path != null){
				foreach(Vector2 pos in path){
					robotNodeFromWorldPoint(transform.position).MakeEmpty();
					transform.position = pos - offsetX;
					robotNodeFromWorldPoint(transform.position).MakeRobot();
					yield return new WaitForSeconds(0.1f);
				}
			}
		}
		yield return StartCoroutine(Rotation());
	}

	private List<Vector2> CreateFrontier(){
		List<Vector2> closestNodes = new List<Vector2>();
		foreach(Node node in robotGrid){
			if(!node.unknown && node.notWall){
				int x = node.gridPosition[0];
				int y = node.gridPosition[1];
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

				if (up || down || right || left) node.MakeFrontier();
				else node.MakeEmpty();

				if (node.isFrontier) closestNodes.Add(node.position);
			}
		}
		Vector3 robotGridPos = new Vector3(offsetX.x, offsetX.y, 1f);
		if (closestNodes.Count > 0){
			closestNodes = closestNodes.OrderBy(x=>Vector2.Distance(x,transform.position + robotGridPos)).ToList();
			//Debug.Log("Frontier:" + closestNodes[0]);
		}
		return closestNodes;
	}

	private List<Vector2> AStarPath(List<Vector2> frontiers){
		List<Vector2> path = new List<Vector2>();
		MinHeap<Node> heap = new MinHeap<Node>();
		HashSet<Node> closed = new HashSet<Node>();
		Dictionary<Node,Node> cameFrom = new Dictionary<Node, Node>();
		
		Vector2 next = frontiers[0];

		Node target = robotNodeFromWorldPoint(next - offsetX);
		heap.Add(robotNodeFromWorldPoint(transform.position));
		bool found = false;

		while(heap.Count > 0){
			Node current = heap.RemoveMin();
			if(!closed.Contains(current)) {
				closed.Add(current);
				if(current.gridPosition == target.gridPosition) {
					path = createPath(cameFrom, current);
					found = true;
					break;
				}

				foreach(Node n in GridNeighbors(current)){
					if(!closed.Contains(n) && !cameFrom.ContainsKey(n)){
						n.fCost = Vector2.Distance(n.position, target.position) + worldGrid.nodeDiameter;
						heap.Add(n);
						cameFrom.Add(n, current);
					}
				}
			}
		}

		if (found) return path;
		else if (frontiers.Count > 1) return AStarPath(frontiers.GetRange(1,frontiers.Count - 1));
		else{
			Debug.Log("PROBLEM");
			return null;
		}
	}

	private List<Vector2> createPath(Dictionary<Node,Node> cameFrom, Node current){
		List<Vector2> path = new List<Vector2>();
		path.Add(current.position);
		while(cameFrom.ContainsKey(current)){
			current = cameFrom[current];
			path.Insert(0, current.position);
		}

		return path;
	}

	private List<Node> GridNeighbors(Node node){
		List<Node> neighbors = new List<Node>();
		int x = node.gridPosition[0];
		int y = node.gridPosition[1];
		int gridX = (int) worldGrid.gridSize.x;
		int gridY = (int) worldGrid.gridSize.y;

		if (y + 1 < gridY){
			Node up = robotGrid[x, y + 1];
			if (up.notWall && !up.unknown) neighbors.Add(up);
		} if (y - 1 >= 0){
			Node down = robotGrid[x, y - 1];
			if (down.notWall && !down.unknown) neighbors.Add(down);
		} if (x + 1 < gridX){
			Node right = robotGrid[x + 1, y];
			if (right.notWall && !right.unknown) neighbors.Add(right);
		} if (x - 1 >= 0){
			Node left = robotGrid[x - 1, y];
			if (left.notWall && !left.unknown) neighbors.Add(left);
		}

		return neighbors;
	}

	private Node robotNodeFromWorldPoint(Vector2 worldPos){
		int gx = (int) worldGrid.gridSize.x;
		int gy = (int) worldGrid.gridSize.y;

		float percentX = Mathf.Clamp01((worldPos.x + gx/2) / gx);
		float percentY = Mathf.Clamp01((worldPos.y + gy/2) / gy);

		return robotGrid[Mathf.RoundToInt((gx - 1) * percentX), Mathf.RoundToInt((gy - 1) * percentY)];
	}
}
