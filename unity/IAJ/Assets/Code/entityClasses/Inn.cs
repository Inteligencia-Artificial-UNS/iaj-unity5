using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Inn : Building {
	
	public float healCoefficient;
    public List<EObject> content = new List<EObject>();

    private Dictionary<Agent, Interval> forbiddenEntry;		
	
	public override void Start(){
		base.Start();
		this._type = "inn";

        SimulationState.getInstance().addInn(this);
		
		forbiddenEntry = new Dictionary<Agent, Interval>();
        this.restock();
	}	
	
	
	public void heal(Agent agent){
		SimulationState.getInstance().stdout.Send(" a ");
		updateForbidden(agent);
		SimulationState.getInstance().stdout.Send(" b ");
		if (isForbidden(agent))
			return;		
		SimulationState.getInstance().stdout.Send(" c ");
		if (agent.life < agent.lifeTotal)
			agent.addLife(Mathf.CeilToInt(agent.lifeTotal * healCoefficient));
			//ss.stdout.Send (Mathf.CeilToInt(agent.lifeTotal * healCoefficient));
			//agent.addLife(Mathf.CeilToInt(1));
		SimulationState.getInstance().stdout.Send(" d ");
		Debug.Log ("Estoy en la posada");
		//ss.stdout.Send("Agente en la posada");
	}

    protected override Dictionary<string, string> getPerceptionProps() {
        Dictionary<string, string> percProps = base.getPerceptionProps();
        List<string> forbAgents = new List<string>();
        foreach (Agent ag in forbiddenEntry.Keys.ToList())
        {
            if (isForbidden(ag))
                forbAgents.Add("[" + ag.getPrologId() + "," + forbiddenEntry[ag].getEnd() + "]");
        }
        percProps.Add("forbidden_entry", PrologList.AtomList<string>(forbAgents));
        List<string> contentPl = new List<string>();
        foreach (EObject eo in this.content)
        {
            contentPl.Add(eo.toProlog());
        }
        percProps.Add("has", PrologList.AtomList<string>(contentPl));
        return percProps;
    }

    private bool isForbidden(Agent agent) {
		return forbiddenEntry.ContainsKey(agent) && forbiddenEntry[agent].contains(SimulationState.getInstance().getTime());
	}
	
	private void updateForbidden(Agent agent) {		
		cleanForbidden();
		int currentTime = SimulationState.getInstance().getTime();
		if (!forbiddenEntry.ContainsKey(agent)) {				
			//Register as forbidden in the future
			int forbidStart = SimulationState.getInstance().getTime() + getTimeToRecover(agent);
			Interval forbidInterval = new Interval(forbidStart, forbidStart + 200);
			forbiddenEntry[agent] = forbidInterval;			
		}
		
	}
	
	private int getTimeToRecover(Agent agent) {
		int lifeToRecover = agent.lifeTotal - agent.life;
		int timeToRecover = (int)(lifeToRecover / (agent.lifeTotal * healCoefficient));
		return timeToRecover + 10; // +10 to ensure the time is enough
	}
	
/*	private bool entryForbidden(Agent agent) {
		int currentTime = SimulationState.getInstance().getTime();
		if (forbiddenEntry.ContainsKey(agent)) {
			Interval interval = forbiddenEntry[agent];
			if (interval.getEnd() <= currentTime)
					forbiddenEntry.Remove(agent);
			return interval.contains(currentTime);				
		} else {
			int forbidStart = SimulationState.getInstance().getTime() + 50;
			Interval forbidInterval = new Interval(forbidStart, forbidStart + 200);
			forbiddenEntry[agent] = forbidInterval;
			return false;
		}
		
	}
*/
	private void cleanForbidden() {
		int currentTime = SimulationState.getInstance().getTime();
		foreach(Agent ag in forbiddenEntry.Keys) {
			Interval interval = forbiddenEntry[ag];
			if (interval.getEnd() < currentTime)
				forbiddenEntry.Remove(ag);				
		}
	}

    private void restock() {
        List<EObject> potionsList = this.content.Where(item => item is Potion).ToList();
        if (potionsList.Count == 0) {
            Potion potion = Potion.Create(new Vector3(0, 0, 0));
            this.put(potion);
        }

        foreach (Booster.BoosterType boosterType in Enum.GetValues(typeof(Booster.BoosterType))) {
            List<EObject> boostersList = this.content.Where(item =>
                                                            item is Booster && (item as Booster).Type == boosterType).ToList();
            if (boostersList.Count == 0) {
                Booster booster = Booster.Create(new Vector3(0, 0, 0), boosterType);
                this.put(booster);
            }
        }
    }

    public void put(EObject obj) {
        obj.gameObject.SetActive(false);
        obj.transform.position = this.transform.position;
        content.Add(obj);
    }

    public bool has(EObject item) {
        return content.Contains(item);
    }

    public void sell(EObject item) {
        content.Remove(item);
        restock();
    }

    public void buy(EObject item)
    {
        content.Add(item);
    }
}
