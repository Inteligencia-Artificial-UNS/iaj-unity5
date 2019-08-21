using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

using Pathfinding.Nodes;
using Pathfinding;

[RequireComponent (typeof (RigidBodyController))]

public class Agent : Entity {
	
	public  int   lifeTotal = 50;
	private float _nodeSize = 2; 		// size of the node in the world measure
										// Hardcodeado. Traté de hacerlo andar dinámicamente, pero no pude.
										// Es el mismo número que el node size definido en el objeto A*, por IDE
	private float _delta = 0.1f;   		// time between ticks of "simulation"
	public  int   life;
	public  int   skill = 100;
    public  int   gold = 1000;             // In cents

    private Home home = null;
	
	private RigidBodyController       _controller;	
	private List<PerceivableNode>     nodeList;							// TODO: Borrar. Es para test nomás
	public  List<EObject>             backpack = new List<EObject>();	
	public  List<EObject>             dropped  = new List<EObject>();
	public  Dictionary<string, float> actionDurations;
	public  AgentState                agentState;

	private Booster defenseBooster = null;
    private Booster attackBooster = null;

    public  int   _depthOfSight;		// radius of vision, in nodes
	public  float _reach   = 1;			// radius of reach (of objects), in world magnitude
	private bool   toogle = false;
	//public  int   velocity = 5;			// TODO: revisar si esto va
	
	private Texture2D lifeLevelTex;
	
	public  delegate       void ActionFinished(ActionResult result);
	private ActionFinished actionFinished = delegate(ActionResult x) { };
	
	public static Agent Create(	GameObject prefab, 
								Vector3 position, 
								IEngine se,
								string description, 
								string name, 
								int lifeTotal) 
	{
		GameObject gameObj    = Instantiate(prefab, position, Quaternion.identity) as GameObject;
		Agent      agent      = gameObj.GetComponent<Agent>();
		
		agent.lifeTotal 	  = lifeTotal;
		agent.life            = lifeTotal;
		agent._description    = description;
		agent._name           = name;
		agent._delta	      = se._delta;
		agent._type		      = "agent";
		agent.actionDurations = new Dictionary<string, float>(se.actionDurations); // copio las duraciones de las acciones

		List<Home> homes = SimulationState.getInstance().getHomes();
		if (homes.Any()) {
			agent.home = homes[SimulationState.getInstance().getAgentCount() % homes.Count];
			agent.gameObject.transform.GetComponent<Renderer>().material.color = agent.home.color;
		}
		return agent;
	}
	
	public GUIStyle infoStyle;
	public GUIStyle nameStyle;	
	public GUIStyle lifeStyle;
	public GUIStyle lifeLevelStyle;
	
	void OnGUI () {
		Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
		//GUI.Label(new Rect(screenPos.x, screenPos.y, 10,5), _name);
		int infoHeight = 25;
		int infoWidth = 100;
		Rect infoRect = new Rect(screenPos.x - infoWidth/2, Screen.height - screenPos.y - infoHeight - 15, infoWidth, infoHeight);
		GUI.BeginGroup(infoRect, infoStyle);
		GUILayout.BeginVertical(infoStyle, GUILayout.Width(infoWidth));			
			GUILayout.Label(_name, nameStyle);
			/*Energy level*/
			GUILayout.BeginVertical(lifeStyle);
			float totalLifeWidth = 50f;
		    float lifeWidth = totalLifeWidth*((float)life/lifeTotal);
			GUILayout.Box("",GUILayout.Width(totalLifeWidth), GUILayout.Height(5));			
			if (Event.current.type == EventType.Repaint && life != 0) {
				Rect lastRect = GUILayoutUtility.GetLastRect();
				Rect newRect = new Rect (lastRect.x, lastRect.y, Math.Max(lifeWidth, 4f), lastRect.height); 
				lifeLevelStyle = new GUIStyle (GUI.skin.box);
				//lifeLevelStyle.normal.background = MakeTex (1, 1, Color.green);
				lifeLevelStyle.normal.background = lifeLevelTex;
				GUI.Box (newRect, "", lifeLevelStyle);
			}
			GUILayout.EndVertical();
		GUILayout.EndVertical();
		GUI.EndGroup();
	}

	public override void Start(){
		base.Start();		
		this._controller = this.GetComponent<RigidBodyController>();
		InvokeRepeating("execute", 0, _delta);
		GetComponent<Rigidbody>().sleepVelocity = 0f;
		lifeLevelTex = MakeTex (1, 1, Color.green);
    }

	public static Texture2D MakeTex( int width, int height, Color col ) {
		Color[] pix = new Color[width * height];
		for( int i = 0; i < pix.Length; ++i )
		{
			pix[ i ] = col;
		}
		Texture2D result = new Texture2D( width, height );
		result.SetPixels( pix );
		result.Apply();
		return result;
	}
	
	public void setCallback(ActionFinished s){
		actionFinished = s;
	}
	
	// esto se ejecuta en cada ciclo de "simulación"
	void execute(){
		position = transform.position;
		nodeList = this.perceptNodes();
		checkInsideBuilding();
		updateInstalledBoosters();

    }

    private void updateInstalledBoosters() {
        Booster maxDefenseBooster = MaxDefenseObject;
        if (maxDefenseBooster != defenseBooster)
        {
            // Just deactivate if defenseBooster is still carried by the agent
            if (defenseBooster != null && defenseBooster.transform.parent == transform)
            {
                defenseBooster.gameObject.SetActive(false);
            }
            if (maxDefenseBooster != null)
            {
                maxDefenseBooster.transform.GetComponent<Rigidbody>().isKinematic = true;
                maxDefenseBooster.transform.GetComponent<Collider>().enabled = false;
                maxDefenseBooster.transform.parent = transform;
                maxDefenseBooster.transform.localPosition = new Vector3(0, .6f, 0);
                maxDefenseBooster.gameObject.SetActive(true);
            }
            defenseBooster = maxDefenseBooster;
        }

        Booster maxAttackBooster = MaxAttackObject;
        if (maxAttackBooster != attackBooster)
        {
            // Just deactivate if attackBooster is still carried by the agent
            if (attackBooster != null && attackBooster.transform.parent == transform)
            {
                attackBooster.gameObject.SetActive(false);
            }
            if (maxAttackBooster != null)
            {
                maxAttackBooster.transform.GetComponent<Rigidbody>().isKinematic = true;
                maxAttackBooster.transform.GetComponent<Collider>().enabled = false;
                maxAttackBooster.transform.parent = transform;
                maxAttackBooster.transform.localPosition = new Vector3(0, 0, 0);
                maxAttackBooster.gameObject.SetActive(true);
            }
            attackBooster = maxAttackBooster;
        }
    }

	private void checkInsideBuilding(){
		int currentNode = this.getNode().GetIndex();				
		if (SimulationState.getInstance().nodeToInn.ContainsKey(currentNode)) {
			Inn inn = SimulationState.getInstance().nodeToInn[currentNode];
			inn.heal(this);
		}					
	}
	
	public void noopPosCon() {
		//Invoke("stoppedAction", actionDurations["noop"]);
	}
	
	public bool movePreConf(int node){
		Node actualNode = AstarPath.active.GetNearest(transform.position).node as GridNode;		
		return PerceivableNode.connections(actualNode as GridNode).Contains(node);
	}
	
	public void movePosCond(int node){
		float cost = _controller.move((Vector3)(AstarPath.active.graphs[0].nodes[node].position));
		subLife((int)(cost+0.5)); //0.5 de redondeo, dado que el cast a int trunca.
        this.gold += 10;
        // this.gold += 100; // To test
    }
	
	// posiblemente innecesario
	public void moveToPosition(Vector3 target){
		_controller.move(target);	
	}
	
	public void stopActionAfter(float time) {
		Invoke("stoppedAction", time);
	}
	
	public void stoppedAction() {		
		if (!isConscious()) {
			//TODO: DIEEEEEEE
			dropEverything();			
			Invoke("wakeUp", 20);
		} else		
			actionFinished(ActionResult.success);
	}

	private void wakeUp() {
		setLife((int)(lifeTotal * .2f));
		actionFinished(ActionResult.success);
	}

	public Boolean isConscious() {
		return life > 0;
	}

	public void setUnconscious() {
		setLife(0);
		dropEverything();
	}

	public void actionFailed(){
		actionFinished(ActionResult.failure);
	}
	
	public void subLife(int dif) {
		if(this.life - dif <= 0)
			setLife(0);				
		else
			setLife(life - dif);	
	}

	public void addLife(int dif) {
		if(this.life + dif >= this.lifeTotal)
			setLife(this.lifeTotal);
		else 
			setLife(life + dif);	
	}

	public void setLife(int life) {
		this.life = life;
		//updateEnergyLevel();
	}
	
//	private void updateEnergyLevel() {
//		//SimulationEngineComponentScript.ss.stdout.Send("energybar "+transform.Find("energybar").Find("energyLevel").ToString());		
//		Transform energyLevel = transform.Find("energybar").Find("energyLevel");
//		Vector3 origScale = energyLevel.localScale;
//		Vector3 origPos = energyLevel.localPosition;
//		energyLevel.localScale = new Vector3((float)life/lifeTotal, origScale.y, origScale.z);
//		energyLevel.localPosition = new Vector3(-((1f - energyLevel.localScale.x)/2f), origPos.y, origPos.z);
//		//SimulationEngineComponentScript.ss.stdout.Send("energyLevel: "+energyLevel.localScale.x);
//		//SimulationEngineComponentScript.ss.stdout.Send("energyPos: "+energyLevel.localPosition);		
//		//transform.Find("energyLevel").
//	}

	public void addSkill(int diff) {
		skill += diff;
	}
	
	public bool pickupPreCon(EObject obj){				
		return obj.isAtGround() && isReachableObject(obj);     //TODO: revisar si isReachableObject es necesario
	}
	
	public void pickupPosCon(EObject obj){
		obj.gameObject.SetActive(false);
		this.backpack.Add(obj);
	}
	
	//DEPRECATED
	public void pickup(EObject obj) {
		
		if (isReachableObject(obj)){
			obj.gameObject.SetActive(false);
			this.backpack.Add(obj);
		}
		else{
			// TODO: excepcion? devolver falso? guardarlo en algun lado?
		}
	}
	
	public bool dropPreCon(EObject obj){
		return backpack.Contains(obj);     
	}
	
	public void dropPosCon(EObject obj){
		Vector3 newPosition = this.transform.position;
		this.backpack.Remove(obj);
		Home home = SimulationState.getInstance().getHomeFromNode(getNode());
		if (obj is Gold && home != null) {
			// Add gold to home
			home.put(obj);
			return;
		}
        // Drop installed booster
        if (obj.transform.parent == transform) {
            obj.transform.parent = null;
            obj.transform.GetComponent<Collider>().enabled = true;
            obj.transform.GetComponent<Rigidbody>().isKinematic = false;
        }
        newPosition.y += 2.5f;
        obj.setPosition(newPosition);
		obj.GetComponent<Rigidbody>().AddForce(new Vector3(20,20,20)); //TODO: cambiar esta fruta
        obj.gameObject.SetActive(true);
    }

	public void dropEverything() {
		foreach (EObject obj in new List<EObject>(backpack))
			dropPosCon(obj);
	}
	
	public bool attackPreCon(Agent target) {
		if (target.Equals(this) || life <= 0 || target.life <= 0)
			return false;
		
		Vector3 distance = this.transform.position - target.transform.position;
		return distance.magnitude < 11f;
	}
	
	public void attackPosCon(Agent target) {
        int diceSides = 100; //TODO add as setting
        System.Random dice = new System.Random();
        int attackPlusAg = this.AttackPlus;
        int defensePlusTargetAg = target.DefensePlus;
        int randomPlusAg = dice.Next(diceSides);
        int randomPlusTargetAg = dice.Next(diceSides);		
        int attackPowerAg = skill + attackPlusAg + randomPlusAg;
        int resistanceTargetAg = target.skill + defensePlusTargetAg + randomPlusTargetAg;
		//SimulationEngineComponentScript.ss.stdout.Send("attack : power = "+attackPowerAg+" resist = "+resistanceTargetAg +". ");
		if (attackPowerAg > resistanceTargetAg) {
			int harm = attackPowerAg - resistanceTargetAg;
			target.subLife(harm);
			Dictionary<SimulationConfig.AgAttributes, float> actionEffects = SimulationConfig.actionEffectsOnAttributes[ActionType.attack];
			this.skill = this.skill + (int)actionEffects[SimulationConfig.AgAttributes.XP];
			//SimulationEngineComponentScript.ss.stdout.Send("success. skill = "+ skill+". ");
		}
		Transform bubblegun = transform.Find("bubbleGun");
		bubblegun.LookAt(target.position);
        ParticleSystem gunParticles = bubblegun.GetComponent<ParticleSystem>();
        Color bubbleColor = attackPlusAg >= Booster.getAttackPlus(Booster.BoosterType.SuperAttack) ? Booster.getColor(Booster.BoosterType.SuperAttack) :
                                                   (this.AttackPlus >= Booster.getAttackPlus(Booster.BoosterType.Attack) ? Booster.getColor(Booster.BoosterType.Attack) : Color.white);
        bubblegun.GetComponent<ParticleSystem>().startColor = bubbleColor;
        gunParticles.Play();	
	}
	
	private void attackStop() {
		
	}
	
	public bool castSpellOpenPreCon(Building building, EObject potion){
		return building != null && !building.isOpen()
			&& potion != null && backpack.Contains(potion);
	}
	
	public void castSpellOpenPosCon(Building building, EObject potion){
		SimulationState.getInstance().stdout.Send(" M1 ");
		building.setOpen(true);
		SimulationState.getInstance().stdout.Send(" M2 ");
		backpack.Remove(potion);
		SimulationState.getInstance().stdout.Send(" M3 ");
		castSpellEffect();
		SimulationState.getInstance().stdout.Send(" M4 ");
	}

	public bool castSpellSleepPreCon(Agent target, EObject potion){
		SimulationState.getInstance().stdout.Send(target._name);
		SimulationState.getInstance().stdout.Send(potion.ToString());
        if (target == null) // Can this happen?
            return false;
        Vector3 distance = this.transform.position - target.transform.position;
        return target != null && target.isConscious()
			&& potion != null && backpack.Contains(potion) && distance.magnitude < 11f;
	}
	
	public void castSpellSleepPosCon(Agent target, EObject potion){
		target.setUnconscious();		
		backpack.Remove(potion);
		castSpellEffect();
		target.spelledEffect();
	}

	public void castSpellEffect() {
		transform.GetComponent<Light>().enabled = true;
		transform.GetComponent<Light>().color = Color.white;
		Invoke("disableLight", 3);
	}

	public void spelledEffect() {
		transform.GetComponent<Light>().enabled = true;
		transform.GetComponent<Light>().color = Color.red;
		Invoke("disableLight", 3);
	}

	private void disableLight() {
		transform.GetComponent<Light>().enabled = false;
	}

	//DEPRECATED
	public void drop(EObject obj) {
		
		if (backpack.Contains(obj)){
			Vector3 newPosition = this.transform.position;
			this.backpack.Remove(obj);
			obj.gameObject.SetActive(true);
			newPosition.y += 2.5f;
			obj.transform.position = newPosition;
			if (!obj._type.Equals("potion"))
				obj.GetComponent<Rigidbody>().AddForce(new Vector3(20,20,20)); //TODO: cambiar esta fruta
		}
		else{
			//TODO: excepcion? devolver falso? guardarlo en algun lado?
		}
	}

    public bool buyPreCond(Inn inn, EObject item) {
        return inn != null && inn.getNode() == this.getNode() && inn.has(item) && this.gold >= item.Price * 100;
    }

    public void buyPosCond(Inn inn, EObject item) {
        this.backpack.Add(item);
        inn.sell(item);
        this.gold -= item.Price * 100;
    }

    public bool sellPreCond(Inn inn, EObject item)
    {
        return inn != null && inn.getNode() == this.getNode() && this.backpack.Contains(item);
    }

    public void sellPosCond(Inn inn, EObject item)
    {
        this.backpack.Remove(item);
        inn.buy(item);
        this.gold += item.Price * 100;
    }

    public Home getHome() {
		return home;
	}

    public int AttackPlus {
        get {
            return this.backpack.Aggregate(0, (maxAttack, next) =>
                                    next is Booster && (next as Booster).Attack > maxAttack ?
                                    (next as Booster).Attack : maxAttack);
        }
    }

    public int DefensePlus {
        get {
            return this.backpack.Aggregate(0, (maxDefense, next) =>
                                           next is Booster && (next as Booster).Defense > maxDefense ?
                                           (next as Booster).Defense : maxDefense);
        }
    }

    public Booster MaxDefenseObject
    {
        get
        {
            return this.backpack
                       .Where((obj) => obj is Booster && (obj as Booster).isDefense())
                       .Aggregate(null as Booster, (maxDefense, next) =>
                                           (maxDefense == null || (next as Booster).Defense > maxDefense.Defense) ?
                                           (next as Booster) : maxDefense);
        }
    }

    public Booster MaxAttackObject
    {
        get
        {
            return this.backpack
                       .Where((obj) => obj is Booster && !(obj as Booster).isDefense())
                       .Aggregate(null as Booster, (max, next) =>
                                  (max == null || (next as Booster).Attack > max.Attack) ?
                                           (next as Booster) : max);
        }
    }

    public void perceive(Percept p){						
		p.addEntitiesRange(perceptObjects<Agent>("agent").Cast<IPerceivableEntity>().ToList());
        p.addEntitiesRange(perceptObjects<Gold> ("gold") .Cast<IPerceivableEntity>().ToList());
		p.addEntitiesRange(perceptObjects<Inn>  ("inn")  .Cast<IPerceivableEntity>().ToList());
		p.addEntitiesRange(perceptObjects<Grave>  ("grave")  .Cast<IPerceivableEntity>().ToList());
		p.addEntitiesRange(perceptObjects<Home>  ("home")  .Cast<IPerceivableEntity>().ToList());
		p.addEntitiesRange(perceptObjects<Potion>  ("potion")  .Cast<IPerceivableEntity>().ToList());
        p.addEntitiesRange(perceptObjects<Booster>("booster").Cast<IPerceivableEntity>().ToList());
        p.addEntitiesRange(perceptNodes()				 .Cast<IPerceivableEntity>().ToList());
	}

    protected override Dictionary<string, string> getPerceptionProps()
    {
        Dictionary<string, string> percProps = base.getPerceptionProps();
        percProps.Add("life", this.life.ToString());
        percProps.Add("lifeTotal", this.lifeTotal.ToString());
        percProps.Add("skill", this.skill.ToString());
        percProps.Add("gold", Math.Floor(this.gold/100f).ToString());
        percProps.Add("lastAction", "[" + this.agentState.lastAction.toProlog() + "," + this.agentState.lastActionTime + "]");
        percProps.Add("home", (home != null) ? home.getName() : "no_home");
        percProps.Add("attackPlus", this.AttackPlus.ToString());

        List<string> backpackPl = new List<string>();
        foreach (EObject eo in this.backpack)
        {
            backpackPl.Add(eo.toProlog());
        }
        percProps.Add("has", PrologList.AtomList<string>(backpackPl));
        return percProps;
    }


    public string selfProperties(bool inProlog = true){
		Building building = inBuilding();
		string   inside   = building != null ? building._name : "no";
		string   aux;
		if (inProlog){ //future implementations will have XML generations instead of prolog
			aux = string.Format("selfProperties({0}, {1}, {2}, {3}, {4}, {5})",
				this._name,
				this.agentState.lastActionResult,
//				"todo",
//				"todo",
				this.life,
				this.lifeTotal,
				new PrologList(this.backpack).toProlog(),
				inside);
		}
		else{
			throw new NotImplementedException();
		}
		return aux;
	}
	
	public Building inBuilding(){
		return null; //TODO: implementar
	}
		
	private List<ObjectType> perceptObjects<ObjectType>(string type, string layer = "perception") 
		where ObjectType : Component // cuánta magia
									 // acá restrinjo el tipo pasado por parámetro
		{
		Collider[] colliders = 
			Physics.OverlapSphere(this.transform.position, _depthOfSight * _nodeSize,
				1 << LayerMask.NameToLayer(layer)); // usa mascaras, con el << agregas con BITWISE-OR
		List<ObjectType> aux = new List<ObjectType>();
		
    	foreach (Collider hit in colliders) {    		
			if (hit.tag == type) {
				ObjectType hitObj = hit.gameObject.GetComponent<ObjectType>();
				GridNode hitNode = AstarPath.active.GetNearest(hitObj.transform.position).node as GridNode;
				if (isVisibleNode(hitNode) && hitNode.walkable)  //to avoid perceiving an object and not perceiving its associated node.
					aux.Add(hit.gameObject.GetComponent<ObjectType>());
			} 
    	}
		return aux;
	}
	
	private List<PerceivableNode> perceptNodes(){
		GridNode gridNode = AstarPath.active.GetNearest(transform.position).node as GridNode;
		List   <PerceivableNode> connections = new List   <PerceivableNode>();
		Queue  <BFNode>          q           = new Queue  <BFNode>         ();
		HashSet<Node>            visited     = new HashSet<Node>           ();
		//Breadth First Search
		BFNode t = new BFNode(0, gridNode);
		q.Enqueue(t);
		while (q.Count > 0){
			t = q.Dequeue();
			if (t.node._node.walkable) //Si el nodo seleccionado es transitable, entonces lo agrego al conjunto resultado.
				connections.Add(t.node);
			if (t.depth < _depthOfSight){ //si no está en el límite, agr
				foreach(Node node in t){ //Itero por vecinos conectados y no conectados del nodo en la grilla, y los agrego a la frontera
										 //siempre que esten en dentro de la distancia de vision.
					if (!(visited.Contains(node)) && //famoso if de reglón de ancho, warpeado
						isVisibleNode(node))
					{	
						visited.Add(node);
						q.Enqueue(new BFNode(t.depth + 1, node as GridNode));
					}
				}
			}
		}
		return connections;	
	}
	
	//check if the node is in a visible distance
	private bool isVisibleNode(Node node){
		
		return (new Int3(transform.position) -
				node.position).worldMagnitude < _depthOfSight * _nodeSize;
	}

	private bool isReachableObject(EObject obj){		
		return (obj.transform.position - transform.position).magnitude < _depthOfSight * _nodeSize;
	}		
}

//Breadth first nodes
class BFNode{
	public PerceivableNode node;
	public int             depth;
	
	public BFNode(int depth, PerceivableNode node){
		this.node  = node;
		this.depth = depth;
	}
	
	public BFNode(int depth, GridNode node){
		this.node  = new PerceivableNode(node);
		this.depth = depth;
	}

	// Devuelve un iterador para los vecinos del nodo. Incluye los vecinos del nodo en la grilla no conectados por arcos,
	// dado que nos va a interesar expandir la busqueda desde ellos para llegar a otros nodos efectivamente 
	// conectados al grafo principal.
	public IEnumerator<Node> GetEnumerator(){
		GridGraph graph            = AstarPath.active.astarData.gridGraph;
		Node[]    nodes            = graph.nodes;
		int []    neighbourOffsets = graph.neighbourOffsets;
		Node      aux;
		int       index;
        
		for (int i = 0; i < 8; i++){ //las 8 conexiones posibles de cada nodo
			index = node._node.GetIndex();
			
			if(node._node.GetConnection(i)){ //connected nodes
				aux = nodes[index + neighbourOffsets[i]];
				if (aux.walkable){	
					yield return aux;
				}
			} else { //disconnected nodes
				int neighIndex = index + neighbourOffsets[i];
				if (neighIndex >= 0 && neighIndex < nodes.Count()) {
					aux = nodes[neighIndex];
					yield return aux ;
				}
			}
		}
	}
}
