using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//Class for controlling UI/UX, game state, and world grid
public class Grid : MonoBehaviour {
	public float nodeRadius = 0.5f;		// Half-size of node
	public Node[,] grid;				// Real-world grid of nodes
	private int drawWait = 10;			// Time that OnDrawGizmos must wait before re-drawing grid
	private Vector3 mouse;				// Position of last mouse click
	private bool canDropWall;			// Can the user put down walls?
	private bool canDropRobot;			// Can the user place the robot?
	private float camSize;				// Half-size that camera must move towards
	private float camPos;				// Position that camera must move towards
	private RobotController rbcontrol;	// Reference to robot
	private RobotLocalize rblocal;	// Reference to robot
	public static Grid instance;		// Static instance of this Grid
	public int state = 0;				// Current state of user interaction
	public float nodeDiameter;			// Full-size of node
	public LayerMask wallsLayer;		// LayerMask for Walls
	public LayerMask notWallsLayer;		// LayerMask for empty Nodes
	public Vector2 gridSize;			// 2D size of grid
	public GameObject wallPreFab;		// Copy of wall gameobject
	public GameObject robotMapPreFab;	// Copy of robot map gameobject
	public GameObject robotLocalizePreFab; // Copy of robot localize gameobject
	public GameObject emptyPreFab;		// Copy of empty node gameobject (invisible box collider)
	public Transform WallsParent;		// GameObject used for organizing wall and empty gameobjects
	public Slider Ysize;				// Reference to UI Slider
	public Slider Xsize;				// Reference to UI Slider
	public Slider runType;				// Reference to UI Slider
	public Slider radarStrength;		// Reference to UI Slider
	public Button Next;					// Reference to UI Button
	public Button Restart;				// Reference to UI Button
	public Text info;					// Reference to UI Text

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= Unity Specific -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	//Initialization (occurs before first frame starts)
	private void Awake(){
		//Only allow one static instance
		if (instance == null){
			instance = this;
		} else if (instance != this){
			Destroy(this);
		}

		//Set initial values for essential variables
		camPos = Camera.main.transform.position.x;
		camSize = 20;
		nodeDiameter = nodeRadius * 2;
		gridSize = new Vector2(10f, 10f);
		Restart.gameObject.SetActive(false);
		
		//Start simulation
		GenerateGrid();
		controlState();
	}

	//Player Input analysis (occurs every frame)
	private void Update(){
		//Wait a small amount of time in between wall placement to avoid multiple instantiations
		if (drawWait > 0) drawWait -= 1;
		//Check if player clicked and place object depending on game state
		if (Input.GetButton("Fire1")) {
			mouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition));
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			//Check that player clicked on clickable object
			if (Physics.Raycast(ray, 50, notWallsLayer)){
				Node mouseNode = nodeFromWorldPoint(mouse);
				if (canDropWall && mouseNode.notWall){
					//Create wall object (SetParent and naming for debugging and organizational porpuses)
					GameObject newWall = (GameObject) Instantiate(wallPreFab, mouseNode.position, Quaternion.identity);
					newWall.transform.SetParent(WallsParent);
					String n = mouseNode.gridPosition[0].ToString();
					n += mouseNode.gridPosition[1].ToString();
					newWall.name = n;
					//Set node to wall
					mouseNode.notWall = false;
				} else if (canDropRobot && mouseNode.notWall) {
					GameObject rb;
					//Depending on selected game type, drop relevant robot
					if (runType.value == 1){
						rb = (GameObject) Instantiate(robotLocalizePreFab, mouseNode.position, Quaternion.identity);
						rblocal = rb.GetComponent<RobotLocalize>();
						//How far the robot can see
						rblocal.radarStrength = radarStrength.value;
					} else {
						rb = (GameObject) Instantiate(robotMapPreFab, mouseNode.position, Quaternion.identity);
						rbcontrol = rb.GetComponent<RobotController>();
						//How far the robot can see
						rbcontrol.radarStrength = radarStrength.value;
					}
					//Only allow one robot to be dropped and let the player start the simulation
					canDropRobot = false;
					Next.interactable = true;
				}
			}
        }

		//Allow player to delete wall objects placed in the game
		if (Input.GetButton("Fire2")) {
			mouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition));
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, 50, wallsLayer)){
				Node mouseNode = nodeFromWorldPoint(mouse);
				if (canDropWall && !mouseNode.notWall){
					String n = mouseNode.gridPosition[0].ToString();
					n += mouseNode.gridPosition[1].ToString();
					Destroy(GameObject.Find(n));
					mouseNode.notWall = true;
				}
			}
        }

		//Always check if camera is in the correct position and size
		//If not, MoveTowards the correct values
		if (camPos != Camera.main.transform.position.x){
			Vector3 pos = Camera.main.transform.position;
			pos.x = Mathf.MoveTowards(pos.x, camPos, 0.5f);
			Camera.main.transform.position = pos;
		}
		if (camSize != Camera.main.orthographicSize)
			Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, camSize, 0.25f);
	}

	//For in-editor visual aid (does not appear in built game)
	private void OnDrawGizmos() {
		if (grid != null && drawWait == 0 && state < 2){
			Node rb = nodeFromWorldPoint(mouse);
			foreach(Node n in grid){
				//Draw grid for easy placement of walls
				Color col = (n.notWall)?Color.white:Color.black;
				if (n == rb) col = Color.cyan;
				col.a = 0.2f;
				Gizmos.color = col;
				Gizmos.DrawWireCube(n.position, Vector3.one * (nodeDiameter - 0.1f));
			}
		}
	}
	
	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= UI controllers -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //
	//Set the X size of the grid
	public void setX(Slider s){
		drawWait = 10;
		gridSize.x = s.value * 2;
		resetSize();
		calculateCameraSize();
	}

	//Set the Y size of the grid
	public void setY(Slider s){
		drawWait = 10;
		gridSize.y = s.value * 2;
		resetSize();
		calculateCameraSize();
	}

	//Move to the next game state
	public void nextButton(){
		state++;
		controlState();
	}

	//Allow robots to update UI when simulation is over
	public void finished(){
		Restart.gameObject.SetActive(true);
	}

	//Update the UI based on game state
	private void controlState(){
		//State 0: User can choose map size and game type
		if (state == 0){
			info.text = "Choose Map Dimensions";
			Xsize.gameObject.SetActive(true);
			Ysize.gameObject.SetActive(true);
			runType.gameObject.SetActive(true);
			canDropWall = false;
			canDropRobot = false;
			Next.interactable = true;
		//State 1: User can draw any wall nodes they like
		} else if (state == 1){
			info.text = "Draw Walls\n\nLeft Click to Create Walls\nRight Click to Destroy Walls";
			Xsize.gameObject.SetActive(false);
			Ysize.gameObject.SetActive(false);
			runType.gameObject.SetActive(false);
			canDropWall = true;
			Next.interactable = true;
			Next.GetComponentInChildren<Text>().text = "Next";
			calculateCameraSize();
		//State 2: User can place robot on any open nodes
		} else if (state == 2){
			info.text = "Place Robot";
			canDropWall = false;
			canDropRobot = true;
			Next.interactable = false;
			Next.GetComponentInChildren<Text>().text = "Run";
			GenerateGrid();
		//State 3: User starts the simlutaion
		} else if (state == 3){
			info.text = "";
			Next.gameObject.SetActive(false);
			if (runType.value == 0) {
				if (gridSize.x >= gridSize.y) camSize *= 2;
				camPos = gridSize.x / 2 + 1;
				StartCoroutine(rbcontrol.Rotation());
			} else {
				StartCoroutine(rblocal.TakeStep());
			}
		}
	}

	//Reload the scene for new simulation
	public void restart(){
		SceneManager.LoadScene("Main_Scene");
	}

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Grid controllers =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	//Allow robots to get static instance of world grid
	public static Grid get(){
		return instance;
	}

	//Change size of grid based on updated X or Y values
	private void resetSize(){
		Vector3 scale = transform.localScale;
		scale.x = gridSize.x;
		scale.y = gridSize.y;
		transform.localScale = scale;
		GenerateGrid();
	}

	//Generate new grid of nodes based on user input
	public void GenerateGrid(){
		int gx = (int) gridSize.x;
		int gy = (int) gridSize.y;
		grid = new Node[gx,gy];

		//Start from bottom left and work up and right
		Vector2 bottomLeft = transform.position - Vector3.right * gx/2 - Vector3.up * gy/2;
		for (int x = 0; x < gx; x++){
			for (int y = 0; y < gy; y++){
				//Calculate all the factors necessary to create a node
				Vector2 worldPoint = bottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius) + Vector2.up * (y * nodeDiameter + nodeRadius);
				bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, wallsLayer));
				int[] gridPos = {x,y};
				grid[x,y] = new Node(false, walkable, worldPoint, gridPos);
				//Generate all empty nodes for mapping robot
				if (canDropRobot && walkable && runType.value == 0) {
					GameObject empty = (GameObject) Instantiate(emptyPreFab, worldPoint, Quaternion.identity);
					empty.transform.SetParent(WallsParent);
				}
			}
		}
	}

	//Get node from position in the world
	public Node nodeFromWorldPoint(Vector2 worldPos){
		float percentX = Mathf.Clamp01((worldPos.x + gridSize.x/2) / gridSize.x);
		float percentY = Mathf.Clamp01((worldPos.y + gridSize.y/2) / gridSize.y);

		return grid[Mathf.RoundToInt((gridSize.x - 1) * percentX), Mathf.RoundToInt((gridSize.y - 1) * percentY)];
	}

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Misc =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	//Recalculate the camera based on game state and grid size
	private void calculateCameraSize(){
		float size = camSize;
		int extra = canDropWall?1:6;
		if ((gridSize.y / Screen.height) > (gridSize.x / Screen.width)){
			size = gridSize.y / 2;
		} else {
			size = (gridSize.x * Screen.height / Screen.width) / 2;
		}
		camSize = size + extra;
	}
}