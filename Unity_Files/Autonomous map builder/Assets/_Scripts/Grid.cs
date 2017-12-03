﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Grid : MonoBehaviour {
	public float nodeRadius = 0.5f;
	private Node[,] grid;
	private int gridWait = 10;
	private Vector3 mouse;
	private bool canDropWall;
	private bool canDropRobot;
	private float camSize;
	private float camPos;
	public static Grid instance;
	public int state = 0;
	public float nodeDiameter;
	public LayerMask walls;
	public Vector2 gridSize;
	public GameObject wallPreFab;
	public GameObject robotPreFab;
	public Transform Walls;
	public Slider Ysize;
	public Slider Xsize;
	public Button Next;
	public Text info;

	private void Awake(){
		if (instance == null){
			instance = this;
		} else if (instance != this){
			Destroy(this);
		}

		camPos = Camera.main.transform.position.x;
		camSize = 20;
		
		nodeDiameter = nodeRadius * 2;
		gridSize = new Vector2(10f, 10f);
		
		GenerateGrid();
		controlState();
	}

	private void Update(){
		if (gridWait > 0) gridWait -= 1;
		if (Input.GetButton("Fire1")) {
			mouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition));
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray)){
				Node mouseNode = nodeFromWorldPoint(mouse);
				if (mouseNode.notWall){
					if (canDropWall){
						GameObject newWall = (GameObject) Instantiate(wallPreFab, mouseNode.position, Quaternion.identity);
						newWall.transform.SetParent(Walls);
						mouseNode.notWall = false;
					} else if (canDropRobot) {
						Instantiate(robotPreFab, mouseNode.position, Quaternion.identity);
						canDropRobot = false;
						Next.interactable = true;
					}
				}
			}
        }

		if (camPos != Camera.main.transform.position.x) {
			Vector3 pos = Camera.main.transform.position;
			pos.x = Mathf.MoveTowards(pos.x, camPos, 0.5f);
			Camera.main.transform.position = pos;
		}

		if (camSize != Camera.main.orthographicSize)
			Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, camSize, 0.25f);
	}

	public static Grid get(){
		return instance;
	}
	
	public void setX(Slider s){
		gridWait = 10;
		gridSize.x = s.value * 2;
		resetSize();
		calculateCameraSize();
	}

	public void setY(Slider s){
		gridWait = 10;
		gridSize.y = s.value * 2;
		resetSize();
		calculateCameraSize();
	}

	public void nextButton(){
		state++;
		controlState();
	}

	public void previousButton(){
		if (state > 0) state--;
		controlState();
	}

	private void controlState(){
		if (state == 0){
			info.text = "Choose Map Dimensions";
			Xsize.gameObject.SetActive(true);
			Ysize.gameObject.SetActive(true);
			canDropWall = false;
			canDropRobot = false;
			Next.interactable = true;
		}
		else if (state == 1){
			info.text = "Draw Walls";
			Xsize.gameObject.SetActive(false);
			Ysize.gameObject.SetActive(false);
			canDropWall = true;
			Next.interactable = true;
			Next.GetComponentInChildren<Text>().text = "Next";
			calculateCameraSize();
		} else if (state == 2){
			//: Add options for localization here
			info.text = "Place Robot";
			canDropWall = false;
			canDropRobot = true;
			Next.interactable = false;
			Next.GetComponentInChildren<Text>().text = "Run";
		} else if (state == 3){
			//::Run simulation
			info.text = "";
			Next.gameObject.SetActive(false);
			if (gridSize.x >= gridSize.y) camSize *= 2;
			camPos = gridSize.x / 2 + 1;
		}
	}

	private void resetSize(){
		Vector3 scale = transform.localScale;
		scale.x = gridSize.x;
		scale.y = gridSize.y;
		transform.localScale = scale;
		GenerateGrid();
	}

	private void calculateCameraSize(){
		float size = camSize;
		if ((gridSize.y / Screen.height) > (gridSize.x / Screen.width)){
			size = gridSize.y / 2;
		} else {
			size = (gridSize.x * Screen.height / Screen.width) / 2;
		}
		camSize = size + 1;
	}

	public void GenerateGrid(){
		int gx = (int) gridSize.x;
		int gy = (int) gridSize.y;
		grid = new Node[gx,gy];

		Vector2 bottomLeft = transform.position - Vector3.right * gx/2 - Vector3.up * gy/2;
		for (int x = 0; x < gx; x++){
			for (int y = 0; y < gy; y++){
				Vector2 worldPoint = bottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius) + Vector2.up * (y * nodeDiameter + nodeRadius);
				bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, walls));
				grid[x,y] = new Node(false, walkable, worldPoint);
			}
		}
	}

	public Node nodeFromWorldPoint(Vector2 worldPos){
		float percentX = Mathf.Clamp01((worldPos.x + gridSize.x/2) / gridSize.x);
		float percentY = Mathf.Clamp01((worldPos.y + gridSize.y/2) / gridSize.y);

		return grid[Mathf.RoundToInt((gridSize.x - 1) * percentX), Mathf.RoundToInt((gridSize.y - 1) * percentY)];
	}

	private void OnDrawGizmos() {
		if (grid != null && gridWait == 0 && state < 2){
			Node rb = nodeFromWorldPoint(mouse);
			foreach(Node n in grid){
				Color col = (n.notWall)?Color.white:Color.black;
				if (n == rb) col = Color.cyan;
				col.a = 0.2f;
				Gizmos.color = col;
				Gizmos.DrawWireCube(n.position, Vector3.one * (nodeDiameter - 0.1f));
			}
		}
	}
}
/*
rivate Grid grid;
	private bool move = false, canStart = true;

	void Awake()
	{
		grid = GetComponent<Grid> ();
	}

	void Start()
	{
		cachedSeekerPos = seeker.position;
		cachedTargetPos = target.position;
		FindPath (seeker.position, target.position);
	}

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Space))
			move = true;

		if (!move && canStart) {
			if (cachedSeekerPos != seeker.position) {
				cachedSeekerPos = seeker.position;
				FindPath (seeker.position, target.position);
			}
			if (cachedTargetPos != target.position) {
				cachedTargetPos = target.position;
				FindPath (seeker.position, target.position);
			}
		} else 
		{
			AnimatePath();
		}
	}
	
	void AnimatePath()
	{
		move = false;
		canStart = false;
		Vector3 currentPos = seeker.position;
		if (grid.Path != null) 
		{
			//Debug.Log("ANIMATING PATH");
			StartCoroutine(UpdatePosition(currentPos, grid.Path[0], 0));
		}
	}

	IEnumerator UpdatePosition(Vector3 currentPos, Node n, int index) 
	{
		//Debug.Log ("Started Coroutine...");
		float t = 0.0f;
		Vector3 correctedPathPos = new Vector3 (n.GetWorldPos().x, 1, n.GetWorldPos().z);
		while (t < 1.0f) {
			t += Time.deltaTime;
			seeker.position = Vector3.Lerp(currentPos, correctedPathPos, t);
			yield return null;
		}
		//Debug.Log ("Finished updating...");
		seeker.position = correctedPathPos;
		currentPos = correctedPathPos;

		index++;
		if (index < grid.Path.Count)
			StartCoroutine(UpdatePosition(currentPos, grid.Path[index], index));
		else
			canStart = true;
	}
*/