using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Pathfinding.Nodes;
using Pathfinding;

public class Agent : Entity {

	private int lifeTotal = 100;
	// private int vida_min = 0;
	public int life;
	public int depthOfSight;
	private float nodeSize; //
	
	public List<EObject> backpack = new List<EObject>();
	
	public Agent(string description, string name) 
		: base(description, "agent", name, false){
		
		this.life     = lifeTotal;
		//this.nodeSize = AstarPath.active.astarData.gridGraph.nodeSize;
		this.nodeSize = 2; //hardcodeado. Traté de hacerlo andar dinámicamente, pero no pude.
						   //Es el mismo número que el node size definido en el objeto A*, por IDE	
	}
	
	void Update(){
		
		//this.perceptNodes();
	}
	
	public void subLife(int dif) {
		if(this.life - dif <= 0){
			this.life = 0;	
			//TODO: DIEEEEEEE
		} else {
			this.life = life - dif;	
		}
	}
	
	public void addLife(int dif) {
		if(this.life + dif >= this.lifeTotal){
			this.life = this.lifeTotal;
		} else {
			this.life += dif;	
		}
	}
	
	public void pickup(EObject obj) {
		//TODO: 
		// - check the distance between agent and object
		// - remove object from the game
		this.backpack.Add(obj);
	}
	
	public void drop(EObject obj) {
		//TODO: 
		// - add object to the game
		this.backpack.Remove(obj);

	}
	
	public override Hashtable perception(){
		Hashtable p = base.perception();
		p["life"]      = life;
		p["lifeTotal"] = lifeTotal;
		p["backpack"]  = backpack;
		
		p["agentsSeen"] = perceptObjects("agent");
		p["goldSeen"]   = perceptObjects("gold");
		//TODO:
		// - cast spherecast
		return p;
	}
	
	ArrayList perceptObjects(string type){
		Collider[] colliders = 
			Physics.OverlapSphere(this.transform.position, 5,  1 << LayerMask.NameToLayer("perception"));
		ArrayList aux = new ArrayList();
		
    	foreach (Collider hit in colliders) {		
			if (hit.GetComponent<Entity>().type == type)
				aux.Add(hit);
    	}
		return aux;
	}
	
	List<GridNode> perceptNodes(){
		GridGraph graph = AstarPath.active.astarData.gridGraph;
		GridNode  node  = AstarPath.active.GetNearest(transform.position).node as GridNode;
		Node[]    nodes = graph.nodes;
		int       index = node.GetIndex();
		
		int[] neighbourOffsets = graph.neighbourOffsets;
		
		List<GridNode>    connections = new List<GridNode>();
		Queue<BFNode>     q           = new Queue<BFNode>();
		HashSet<GridNode> visited     = new HashSet<GridNode>();
				
		//BFS
		BFNode t = new BFNode(0, node);
		q.Enqueue(t);
		Node aux;
		
		while (q.Count > 0){
			t = q.Dequeue();
			connections.Add(t.node);
			if (t.depth < depthOfSight){ //si no está en el límite, agrego nodos
				for (int i = 0; i < 8; i++){ //las 8 conexiones posibles de cada nodo
					index = t.node.GetIndex();

					if(t.node.GetConnection(i)){
						aux = nodes[index + neighbourOffsets[i]];

						if (!(visited.Contains((GridNode)(aux))) && //famoso if de reglón de ancho, warpeado
							isVisibleNode(aux) &&
							aux.walkable)
						{	
							visited.Add((GridNode)aux);
							q.Enqueue(new BFNode(t.depth + 1, (GridNode)aux));
						
						}
					}
				}
			}
		}
		
		// TEST ONLY
		
//		for(int i = 0; i < connections.Count; i++){
//			Debug.DrawLine(transform.position, (Vector3)((Node) connections[i]).position,Color.red);
//		}
		// TEST ONLY
		
		return connections;		
	}
	
	//check if the node is in a visible distance
	private bool isVisibleNode(Node node){
		return (new Int3(transform.position) -
				node.position).worldMagnitude < depthOfSight * nodeSize;
	}
}

//Breadth first nodes
class BFNode{
	public GridNode node;
	public int depth;
	
	public BFNode(int depth, GridNode node){
		this.node  = node;
		this.depth = depth;
	}
}