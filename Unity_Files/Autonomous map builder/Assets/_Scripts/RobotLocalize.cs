using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//Script Attached to Localization Robot
public class RobotLocalize : MonoBehaviour {
	private List<Particle> particles;		// Grid based on robot's knowledge
	private Grid worldGrid;					// Reference to the real grid
	private List<GameObject> Visualization;	// List of instansiated particle prefabs
	private int gy;							// Y size of grid
	private int gx;							// X size of grid
	private int particleNum;				// Amount of total particles
	private float highestProb;				// Highest particle probability for each time step
	private GameObject particleParent;		// Empty GameObject for Particle storage
	public GameObject particlePrefab;		// Particle vizualization prefab
	public float radarStrength;				// Strength of robot's visual field

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= Unity Specific -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	//Called when user creates robot
	void Start(){
		particleParent = GameObject.Find("Particles");
		worldGrid = Grid.get();
		gx = (int) worldGrid.gridSize.x;
		gy = (int) worldGrid.gridSize.y;
		particles = new List<Particle>();
		Visualization = new List<GameObject>();
		particleNum = gy*gx*4;
		highestProb = 0;

		//Evenly distribute particles across grid cells
		for (int y = 0; y < gy; y++){
			for (int x = 0; x < gx; x++){
				//Create particle at each grid cell for each possible heading
				for (int i = 0; i < 4; i++){
					Particle p = new Particle(worldGrid.grid[x, y], 90*i);
					GameObject vis = (GameObject) Instantiate(particlePrefab, p.position, Quaternion.identity);

					particles.Add(p);
					vis.transform.SetParent(particleParent.transform);
					Visualization.Add(vis);
				}
			}
		}

		Visualize();
	}

	
	//Vizualizes the particles above grid map drawn by user
	private void Visualize(){
		//Removes particle visualizations from last time step
		foreach(Transform child in particleParent.transform)
			Destroy(child.gameObject);
		//Computes the percentage of total particles per grid cell
		// at current time step
		Dictionary<int, int> ratios = new Dictionary<int, int>();
		Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();
		foreach(Particle p in particles){
			int[] pos = worldGrid.nodeFromWorldPoint(p.position).gridPosition;
			int index = pos[1] * gx + pos[0];
			if (ratios.ContainsKey(index))
				ratios[index]++;
			else{
				ratios[index] = 1;
				positions[index] = p.position;
			}
		}
		//Draws particles for current time step
		// size based on percentage of total particles 
		// per grid cell at current time step
		int count = 0;
		foreach(int k in ratios.Keys){
			float size = ratios[k]/(float)particles.Count;
			if (size == 1){ 
				worldGrid.finished();
				StopAllCoroutines();
			}
			Vector3 newPos = new Vector3(positions[k].x, positions[k].y, -4f);
			GameObject current = (GameObject) Instantiate(particlePrefab, newPos, Quaternion.identity);
			current.transform.localScale = new Vector3(size, size, 0.1f);
			current.transform.SetParent(particleParent.transform);
			Visualization[count] = current;
			count++;
		}
	}

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Localization functions -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	//Coroutine started by "Run" button
	//Implements Monte Carlo Localization
	public IEnumerator TakeStep(){
		//Choose next grid
		Vector3 next = PickNeighbor();
		Vector2 step = next - transform.position;
		//Update heading of robot to point at next
		float prevRot = transform.rotation.eulerAngles.z;
		transform.rotation = Quaternion.Euler(CalculateHeading(step.x, step.y));
		transform.position = next;
		float rotate =  transform.rotation.eulerAngles.z - prevRot;

		OdomentryProb(1, rotate);
		SensorProb();
		Resample();
		Visualize();
		yield return new WaitForSeconds(.5f);
		StartCoroutine(TakeStep());
	}

	//Returns position of robots next step
	//Choses random neighbor of previous position
	// that is in bounds and not a wall
	private Vector2 PickNeighbor(){
		Node gridPos = worldGrid.nodeFromWorldPoint(transform.position);
		int y = gridPos.gridPosition[1];
		int x = gridPos.gridPosition[0];

		List<Vector2> options = new List<Vector2>();

		if (LegalStep(x, y + 1)) options.Add(worldGrid.grid[x, y + 1].position);
		if (LegalStep(x, y - 1)) options.Add(worldGrid.grid[x, y - 1].position);
		if (LegalStep(x + 1, y)) options.Add(worldGrid.grid[x + 1, y].position);
		if (LegalStep(x - 1, y)) options.Add(worldGrid.grid[x - 1, y].position);

		return options[Random.Range(0, options.Count)];
	}

	//Applies robot movement to all particles
	//Computes prior probabilities according
	// to the legality of each particle making that step
	private void OdomentryProb(float magnitude, float theta){
		List<Particle> toDelete = new List<Particle>();
		List<Vector2> unique = new List<Vector2>();
		foreach(Particle p in particles){
			int[] oldGridPos = worldGrid.nodeFromWorldPoint(p.position).gridPosition;
			p.rotate(theta);
			int[] gridDelta = p.move(magnitude);
			if (!LegalStep(oldGridPos[0] + gridDelta[0], oldGridPos[1] + gridDelta[1])) toDelete.Add(p);
			else {
				if (!unique.Contains(p.position)) unique.Add(p.position);
			}
		}
		//Remove particles that go out of bounds/into a wall
		foreach(Particle p in toDelete){
			particles.Remove(p);
		}

		//Calculate prior prob based on number of unique particle locations
		foreach(Particle p in particles){
			p.prob = 1 / unique.Count;
		}
	}

	//Weighs odometry probabilities based on simulated
	// IR sensor
	private void SensorProb(){
		RaycastHit robotHit;
		float dist = 0;
		if (Physics.Raycast(transform.position, transform.up, out robotHit))
			dist = robotHit.distance;
		//Compares the real robot sensor readings to what the robot
		// would sense given at each particle position and heading
		foreach(Particle p in particles){
			RaycastHit pHit;
			float pDist = 0;
			Vector3 direction = new Vector3(-1 * Mathf.Sin(Mathf.Deg2Rad * p.heading), Mathf.Cos(Mathf.Deg2Rad * p.heading), 0);
			if (Physics.Raycast(p.position, direction, out pHit))
				pDist = pHit.distance;
			//Weight probabilities	
			if (dist == pDist) p.prob *= 1f;
			else {
				if (dist > 0 && pDist > 0) p.prob *= 0.5f;
				else p.prob *= 0.15f;
			}
			if (p.prob > highestProb) highestProb = p.prob;
		}
		
	}

	//Creates array of particles for next time step
	// based on their weighted probabilities
	//Replacement occurs after selection, so the same
	// particle may be chosen multiple times
	private void Resample(){
		List<Particle> resample = new List<Particle>();
		for(int i = 0; i < particleNum; i++){
			float rand = Random.Range(0f, highestProb - 0.1f);
			List<Particle> options = new List<Particle>();
			for (int j = 0; j < particles.Count; j++){
				if (particles[j].prob > rand) options.Add(particles[j]);
			}
			resample.Add(new Particle(options[Random.Range(0, options.Count)]));
		}

		particles = resample;
	}

	//Check if x,y grid position is within bounds of map
	// and is not a wall
	private bool LegalStep(int x, int y){
		return (y < gy && y >= 0 && x < gx && x >= 0 && worldGrid.grid[x, y].notWall);
	}

	//Calculates heading of robot, to point at x,y
	// for vizualization purposes
	private Vector3 CalculateHeading(float x, float y){
		float rad = Mathf.Atan2(x, y);
		float deg = rad * (180 / Mathf.PI);

		Vector3 look = new Vector3(0f, 0f, -deg);

		return look;
	}
}
