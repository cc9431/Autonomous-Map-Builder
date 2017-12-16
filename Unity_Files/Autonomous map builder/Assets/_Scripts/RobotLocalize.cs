using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RobotLocalize : MonoBehaviour {
	private Particle[] particles;			// Grid based on robot's knowledge
	private Grid worldGrid;					// Reference to the real grid
	private List<GameObject> Visualization;	// 
	public GameObject particlePrefab;		//
	private GameObject particleParent;		//
	public float radarStrength;				// Strength of robot's visual field
	private int gy;							// Y size of grid
	private int gx;							// X size of grid

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= Unity Specific -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	void Start(){
		particleParent = GameObject.Find("Particles");
		worldGrid = Grid.get();
		gx = (int) worldGrid.gridSize.x;
		gy = (int) worldGrid.gridSize.y;
		particles = new Particle[gx * gy];
		Visualization = new List<GameObject>();

		// Initialize Grid based on current knowledge: grid size and current position
		for (int y = 0; y < gy; y++){
			for (int x = 0; x < gx; x++){
				int index = (y * gx) + x;
				particles[index] = new Particle(worldGrid.grid[x, y]);
				Visualization.Add((GameObject) Instantiate(particlePrefab, particles[index].position, Quaternion.identity));
				Visualization[index].transform.SetParent(particleParent.transform);
			}
		}

		Visualize();
	}

	private void Visualize(){
		foreach(Transform child in particleParent.transform)
			Destroy(child.gameObject);

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

		int count = 0;
		foreach(int k in ratios.Keys){
			float size = ratios[k]/(float)particles.Length;
			if (size == 1) worldGrid.finished();
			Vector3 newPos = new Vector3(positions[k].x, positions[k].y, -4f);
			GameObject current = (GameObject) Instantiate(particlePrefab, newPos, Quaternion.identity);
			current.transform.localScale = new Vector3(size, size, 0.1f);
			current.transform.SetParent(particleParent.transform);
			Visualization[count] = current;
			count++;
		}
	}

	// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- Localization functions -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

	public IEnumerator TakeStep(){
		Vector3 next = PickNeighbor();
		Vector2 step = next - transform.position;
	
		transform.rotation = Quaternion.Euler(CalculateHeading(step.x, step.y));
		transform.position = next;
		
		OdomentryProb(step);
		SensorProb();
		Resample();
		Visualize();
		yield return new WaitForSeconds(1);
		StartCoroutine(TakeStep());
	}

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

	private void OdomentryProb(Vector2 dir){
		foreach(Particle p in particles){
			// Apply movement
			// Check bounds
			// Check wall
			Node oldPos = worldGrid.nodeFromWorldPoint(p.position);
			int x = (int) dir.x + oldPos.gridPosition[0];
			int y = (int) dir.y + oldPos.gridPosition[1];
			Vector3 newPos = p.position + dir;
			p.position = newPos;

			if (LegalStep(x, y)) p.prob = 0.9f;
			else p.prob = 0f;
		}
	}

	private void SensorProb(){
		RaycastHit robotHit;
		float dist = 0;
		if (Physics.Raycast(transform.position, transform.up, out robotHit))
			dist = robotHit.distance;

		foreach(Particle p in particles){
			RaycastHit pHit;
			float pDist = 0;
			if (Physics.Raycast(p.position, transform.up, out pHit))
				pDist = pHit.distance;

			if (dist == pDist) p.prob *= 1f;
			else {
				if (dist > 0 && pDist > 0) p.prob *= 0.5f;
				else p.prob *= 0.15f;
			}
		}
		
	}

	private void Resample(){
		Particle[] resample = new Particle[particles.Length];
		for(int i = 0; i < resample.Length; i++){
			float rand = Random.Range(0f, 1f);
			List<Particle> options = new List<Particle>();
			for (int j = 0; j < particles.Length; j++){
				if (particles[j].prob > rand) options.Add(particles[j]);
			}
			if (options.Count > 0)
				resample[i] = new Particle(options[Random.Range(0, options.Count)]);
			else
				resample[i] = new Particle(particles[Random.Range(0, particles.Length)]);
		}

		particles = resample;
	}

	private bool LegalStep(int x, int y){
		return (y < gy && y >= 0 && x < gx && x >= 0 && worldGrid.grid[x, y].notWall);
	}

	private Vector3 CalculateHeading(float x, float y){
		float rad = Mathf.Atan2(x, y);
		float deg = rad * (180 / Mathf.PI);

		Vector3 look = new Vector3(0f, 0f, -deg);

		return look;
	}
}
