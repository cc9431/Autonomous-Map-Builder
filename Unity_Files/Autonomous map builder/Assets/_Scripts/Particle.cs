using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle {
	public Vector2 position;
	public float prob;

	public Particle(Node node){
		position = node.position;
		prob = 0;
	}

	public Particle(Particle particle){
		position = particle.position;
		prob = particle.prob;
	}
}
