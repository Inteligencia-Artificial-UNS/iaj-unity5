using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Home : Building {

	public SimulationState ss;
	
	public List<EObject> content = new List<EObject>();	

	public Color color = Color.yellow;
	private Texture2D texture;
		
	public override void Start() {
		SimulationState.getInstance().stdout.Send("entro Start");
		base.Start();
		this._type = "home";
		this.ss    = (GameObject.FindGameObjectWithTag("GameController").
			GetComponent(typeof(SimulationEngineComponentScript))
			as SimulationEngineComponentScript).engine as SimulationState;
				
		this.ss.addHome(this);
		gameObject.GetComponent<Renderer>().material.color = color;
		texture = Agent.MakeTex(2, 2, color);			
	}
	
	public void put(EObject obj){		
		SimulationState.getInstance().stdout.Send("entro put");
		obj.gameObject.SetActive(false);
		obj.transform.position = this.transform.position;
		content.Add(obj);				
		SimulationState.getInstance().stdout.Send("salio put");
	}
	
	public override void setOpen(bool open) {
		if (open) {
			foreach (EObject obj in content) {				
				drop(obj);				
			}						
			content.Clear();			
		}
	}
	
	public override bool isOpen() {
		return false;
	}

	public Color getColor() {
		return color;
	}
	
	public void drop(EObject obj) {				
		obj.gameObject.SetActive(true);
		Vector3 newPosition = this.transform.position;
		newPosition.y += 2.5f;
		obj.transform.position = newPosition;
		obj.GetComponent<Rigidbody>().AddForce(new Vector3(20,20,20));		
	}

	public Texture2D getTexture() {
		return texture;
	}

    protected override Dictionary<string, string> getPerceptionProps() {
        Dictionary<string, string> percProps = base.getPerceptionProps();
        List<string> contentPl = new List<string>();
        foreach (EObject eo in this.content)
        {
            contentPl.Add(eo.toProlog());
        }
        percProps.Add("has", PrologList.AtomList<string>(contentPl));
        return percProps;
    }

}
