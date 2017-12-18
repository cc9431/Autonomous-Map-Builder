using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle {
	public Vector2 position;
	public float heading;
	public float prob;

	public Particle(Node node, float headZ){
		position = node.position;
		prob = 0;
		heading = headZ;
	}

	public Particle(Particle particle){
		position = particle.position;
		prob = particle.prob;
		heading = particle.heading;
	}

	public void rotate(float theta){
		heading = (heading + theta) % 360;
	}
	//Moves particles and returns change in grid position
	public int[] move(float magnitude){
		int dX = -1* (int) Mathf.Sin(Mathf.Deg2Rad * heading);
		int dY = (int) Mathf.Cos(Mathf.Deg2Rad * heading);
		position.x += magnitude * dX;
		position.y += magnitude * dY;
		int[] change = {dX,dY};
		return change;
	}
}
