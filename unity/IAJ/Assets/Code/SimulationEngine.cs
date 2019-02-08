using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

/******************************************************************************/
// SYSTEM AUXILIARY CLASSES

/******************************************************************************/

/* All mailboxes that are read from unity run in non blocking mode, so as not to 
 * block unity's execution.
 * All mailboxes that are read by the agents run in blocking mode, becuase they 
 * are used to synchronize them with unity.
 */

public class MailBox<T> {

    private Queue<T>  queue;
    private Semaphore semaphore;
    private bool      nbmode;

    public MailBox(bool nbmode) {
        this.nbmode = nbmode;
        queue       = new System.Collections.Generic.Queue<T>();
        semaphore   = new Semaphore(0, Int32.MaxValue);
    }

    public void Send(T item) {
        lock (this) {
            queue.Enqueue(item);
            semaphore.Release();
        }            
    }

    /* 
     * The method guarantees that an item will be dequeued, blocking 
     * if the queue is empty until an item is inserted and it can be
     * dequeued.
     * 
     * This Recv method will first block on the semaphore, 
     * which indicates whether there are any items in the queue or not.
     * If there are elements in the queue, the semaphore's value 
     * should be greater than zero, and the caller will attempt to 
     * acquire the mutex.
     * Once the mutex is acquired, it will check to see if the queue 
     * is in fact non-empty, and dequeu an item. 
     * This check is rather redundant if all things go well, 
     * because the caller should never get to the point in which it 
     * will attempt a Recv if the semaphore's value is non-zero.
     * In practice, the call to Count should always return a value 
     * greater than zero, the method always return true, and item
     * always be assigned an element dequeued from the queue. 
     * However, I am loath to remove the check until the code is 
     * more tested in case there is some situation I have not 
     * considered.
     * 
     * The wait must be performed first on the semaphore because if it
     * is done first on the mutex, the caller may acquire the mutex 
     * when the queue is empty. No other caller may acquire the mutex
     * to insert an item in the queue, and a deadlock will occur.  
     */
    public bool Recv(out T item) {
        bool result = false;
        item = default(T);

        if (nbmode) {
            Debug.LogError("PANIC! queue is in non-blocking mode, blocking recv called.");
        }
        else {
            semaphore.WaitOne();
            lock (this) {
                if (queue.Count > 0) {
                    item = (T)queue.Dequeue();
                    result = true;
                } else {
                    Debug.LogError("PANIC! queue is empty.");
                }
            }
        }
        return result;
    }

    public bool NBRecv(out T item) {
        bool result = false;
        item = default(T);

        if (nbmode) {
            lock (this) {
                if (queue.Count > 0) {
                    item = (T)queue.Dequeue();
                    result = true;
                }
            }
        }
        else {
            Debug.LogError("PANIC: queue used in blocking mode, non-blocking recv called.");            
        }
        return result;
    }

    public bool IsEmpty() {
        return (queue.Count == 0);
    }
    
    public bool NotEmpty() {
        return (queue.Count > 0);
    }
}

/******************************************************************************/
// SIMULATION STATIC ENTITIES

public class Position { 
    public int x;
    public int y;
    public int z;

    public Position() {
    }

    public Position(string s) {
        // TODO
    }
}

public class PerceptRequest {

    public int agentID;
    public MailBox<Percept> agentPerceptMailbox;

    public PerceptRequest(int AID, MailBox<Percept> APM) {
        // Aca va informacion que usa Unity para generar la percepcion
        // principalmente, para que agente se esta generando
        // con esto, unity identifica el gameobject correspondiente 
        // al agente, y una vez obtenido eso, corre los spherecast, etc
        // ademas, tiene una referencia al mailbox de percepciones del agente
        // en la cual unity va a insertar la percepcion generada
        this.agentID             = AID;
        this.agentPerceptMailbox = APM;
    }
}

public class InstantiateRequest {

	public AgentConnection agentConnection;

    public InstantiateRequest(AgentConnection AC) {
		this.agentConnection = AC;
    }
}

public enum ActionType {
    unknown, goodbye, noop, move, attack, pickup, drop, cast_spell, buy
};

public enum ActionResult {
    success, failure
};

public class Action {
    
    public ActionType type     = ActionType.noop;
    public string     actionID = "0"; // ID of the action, provided by agent.
    public int        agentID  = 0;   // ID of the agent performing the action.    
	public string 	  targetID = "";  // ID of an agent that is the recipient of the action.
	public int        targetNodeID = 0; // ID of the target node of a move action.
    public string     objectID = "";
    public float      duration = 0f;
    public Position   position;
	public String 	  prologString = "none";
	public String 	  description = "";

    public Action() {
    }

    public Action(SimulationState ss, int aid, string xml) {

        // It might be the case that the string passed in null or 
        // empty, in which case a default action of type noop is made.
        if ((xml == null) || (xml == "")) {
            this.type = ActionType.noop;
        }
        else {
            // TODO finish implemeting
            XmlDocument document;
            string      type_str;

            try {
                document = new XmlDocument();
                document.LoadXml(xml);

                type_str = document.SelectSingleNode("/action/type").InnerText;
                this.agentID  = aid;
                this.actionID = document.SelectSingleNode("/action/id").InnerText;
                this.duration = ss.config.actionDurations[type_str];
				this.prologString = type_str;
                
                if (type_str == "goodbye") {
                    this.type = ActionType.goodbye;
                }
                if (type_str == "noop") {
                    this.type = ActionType.noop;
                }
                if (type_str == "move") {
                    this.type     = ActionType.move;
                    //this.position = new Position(document.SelectSingleNode("/action/position").Value);
					
					//Acá no me anduvo Value, y si InnerText. No sé por qué
					this.targetNodeID = Convert.ToInt32(document.SelectSingleNode("/action/position").InnerText);
					this.prologString += "("+targetNodeID+")";
                }
                if (type_str == "attack") {
                    this.type     = ActionType.attack;
					//SimulationEngineComponentScript.ss.stdout.Send("Name? " + document.SelectSingleNode("/action/agent/id").InnerText);
                    this.objectID = document.SelectSingleNode("/action/agent/id").InnerText;
					this.prologString += "("+objectID+")";
                }
                if (type_str == "pickup") {
                    this.type     = ActionType.pickup;
                    this.objectID = document.SelectSingleNode("/action/object/id").InnerText;
					this.prologString += "("+objectID+")";
                }
                if (type_str == "drop") {
                    this.type     = ActionType.drop;
                    this.objectID = document.SelectSingleNode("/action/object/id").InnerText;
					this.prologString += "("+objectID+")";
                }
				if (type_str == "cast_spell") {
                    this.type     = ActionType.cast_spell;
					this.description  = document.SelectSingleNode("/action/spell").InnerText;
					this.targetID = document.SelectSingleNode("/action/target/id").InnerText;
                    this.objectID = document.SelectSingleNode("/action/potion/id").InnerText;
					this.prologString += "("+description+"("+targetNodeID+","+objectID+"))";
                }
                if (type_str == "buy") {
                    this.type = ActionType.buy;
                    this.targetID = document.SelectSingleNode("/action/target/id").InnerText;
                    this.objectID = document.SelectSingleNode("/action/object/id").InnerText;
                    this.prologString += "(" + targetNodeID + "," + objectID + ")";
                }
            }
            catch (System.Xml.XmlException) {
                // TODO somehow signal failure to agent
                //ss.stdout.Send(String.Format("AC {0}: Error: bad action xml.", agentID));
            }
        }
    }
	
	public String toProlog() {
		return prologString;
	}
	
}

//TODO: revisar si esto va
public class ObjectState {
    
    public Position position;
    public string   name;
    public string   type;

    public ObjectState(Position position, string name, string type) {
        this.position = position;
        this.name     = name;
        this.type     = type;
    }
}

public class AgentState {
    
    public string                name;
    public int                   agentID;
    public Position              position;
	public Action                lastAction;
	public int                	 lastActionTime;
    public ActionResult          lastActionResult;
	public Agent				 agentController;
		
    public MailBox<Action>       actions;
    public MailBox<ActionResult> results;
    public MailBox<Percept>      percepts;
	
    public AgentState(SimulationState ss, string name, int id, Vector3 spawnSite, GameObject prefab) {
        this.agentID           = id;
		this.lastAction        = new Action();
        this.lastActionResult  = ActionResult.success;
        this.actions           = ss.readyActionQueue;
        this.results           = new MailBox<ActionResult>(false);
        this.percepts          = new MailBox<Percept>(false);
        this.agentController   = Agent.Create(prefab, spawnSite, ss, "", name, 450);  
		
		this.agentController.setCallback(results.Send);
		this.agentController.agentState = this;
	}

    public String toString() {
        return String.Format("Agent {0} \n    id:  {1} \n    pos: {2}\n    LAR: {3}\n", 
            name, agentID, position, lastActionResult);
    }
}

/******************************************************************************/
// SIMULATION ENGINE CORE

// Runs within Unity3D
public class SimulationEngine {

    public int               currentAgentID = 1;
    public int               currentRespawn = 0;
    public ConnectionHandler connectionHandler;
    public SimulationState   simulationState;

    public SimulationEngine(SimulationState ss) {
        simulationState   = ss;

        connectionHandler = new ConnectionHandler(ss);
    }

    /* Don't get confused by the fact that SimulationEngine has start()
     * and stop() methods, it does not run its own thread. 
     * It just encapsulates all the other objects and threads that 
     * make up the simulation engine. 
     * Correspondingly, it has no run() method.
     */
    public void start() {
        // Tell all the threads to start.
        connectionHandler.start();
    }

    public void stop() {
        // Tell all the threads to stop.
        connectionHandler.stop();
    }
	
	public void dynamicEnvUpdate() {
		simulationState.gameTime++;	
	}

    public void generatePercepts() {
				
		
        MailBox<PerceptRequest> requests = simulationState.perceptRequests;

        PerceptRequest request;
        Percept        percept;

        // aca hay que sacar todos los requests de percepciones de la cola 
        // y para cada uno, generar la percepcion correspondiente
        while (requests.NotEmpty()) {
            if (requests.NBRecv(out request)) {
                percept = new Percept(simulationState, request.agentID);
                request.agentPerceptMailbox.Send(percept);
            } 
        } 
    }

    public void handleActions() {

        Action                      currentAction;
        MailBox<Action>             raq    = simulationState.readyActionQueue;
        Dictionary<int, AgentState> agents = simulationState.agents;

        while (raq.NotEmpty()) {
            // get action from the ready action queue
            // if the action is executable,
            //     put it in unity's action queue
            //     let the agent know that its action was executable
            // else
            //     let the agent know that its action was not executable
            //simulationState.stdout.Send(String.Format("AH: there are actions to process...\n"));
            if (raq.NBRecv(out currentAction)) {
                int agentID = currentAction.agentID;
                try {
					AgentState agentState = agents[agentID];
					agentState.lastAction = currentAction;
				agentState.lastActionTime = SimulationState.getInstance().getTime();
                    if (simulationState.executableAction(currentAction)) {
                        //simulationState.stdout.Send(String.Format("AH: the action is executable.\n"));
                        //agents[agentID].results.Send(ActionResult.success);
                        agents[agentID].lastActionResult = ActionResult.success;
                        simulationState.applyActionEffects(currentAction);
                    }
                    else {
                        //simulationState.stdout.Send(String.Format("AH: the action is not executable.\n"));
                        agents[agentID].results.Send(ActionResult.failure);
                        agents[agentID].lastActionResult = ActionResult.failure;
                    }
                }
                catch (System.Collections.Generic.KeyNotFoundException e) {
                    simulationState.stdout.Send(String.Format("AH: Error: agent id {0} not present in agent database.\n", agentID));
					Debug.LogError(e.ToString());
                }
            }
        }
    }

    public void instantiateAgents(GameObject prefab) {
		
		InstantiateRequest          request;
		int                         agentID;
		string                      name;
		AgentState                  state;	
        Vector3                     spawnPosition;	
		
        /*
        Instantiating an agent in the simulation consists of associating the 
        AgentConnection with an AgentID and an AgentState.
        If the agent has not been previously instantiated, a new AgentState must 
        be created, including an AgentController which will represent it graphically.
        If the agent has already been instantiated, then its old AgentID and AgentState
        must be recovered from the SimulationState, and associated to the new AgentConnection.
        */

        /* Esto lo que hace es elegir una posicion donde va a spawnear el agente.
           Cada vez que un agente se instancia (por primera vez), se incrementa en uno 
           modulo la cantidad de sitios de instanciacion. 
        */
		while (simulationState.instantiateRequests.NotEmpty()) {
			if (simulationState.instantiateRequests.NBRecv(out request)) {
				name = request.agentConnection.name;

                spawnPosition = GameObject.FindGameObjectsWithTag("Respawn")[currentRespawn].transform.position;
                currentRespawn = (currentRespawn + 1) % GameObject.FindGameObjectsWithTag("Respawn").Length;

				if (simulationState.agentIDs.TryGetValue(name, out agentID)) {
					// esta
                    request.agentConnection.agentState = simulationState.agents[agentID];
                }
                else {
                    // no esta
                    agentID = currentAgentID;
                    state   = new AgentState(simulationState, name, agentID, spawnPosition, prefab);
                    request.agentConnection.agentState = state;
                    
                    simulationState.agentIDs.Add(name, agentID);
                    simulationState.agents.Add(agentID, state); 
                    
                    currentAgentID++;
                }
                connectionHandler.instantiationResults.Send(true);
			}
		} 
    }
	
	public void instantiateDummyAgent(string name, GameObject prefab) {
		int agentID = currentAgentID;		
		Vector3 spawnPosition = GameObject.FindGameObjectsWithTag("Respawn")[currentRespawn].transform.position;
        currentRespawn = (currentRespawn + 1) % GameObject.FindGameObjectsWithTag("Respawn").Length;
		
        AgentState state   = new AgentState(simulationState, name , agentID, spawnPosition, prefab);                            
        simulationState.agentIDs.Add(name, agentID);
        simulationState.agents.Add(agentID, state);                     
        currentAgentID++;
	}
		
}
