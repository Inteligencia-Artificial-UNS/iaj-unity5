using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Nodes;
using Pathfinding;

public abstract class Entity : MonoBehaviour, IPerceivableEntity {

	public string    _description;
	public string    _type;
	public GridGraph _graph;
	public string    _name;
	public Vector3   position;

    private Transform _transform;


	
	public virtual void Start(){
		_transform = this.transform;
		_graph     = AstarPath.active.astarData.gridGraph;
		//_name      = this.gameObject.name;
		position   = this.transform.position;
    }
	
	
	/*
	public virtual Dictionary<string, System.Object> perception(){
		Dictionary<string, System.Object> p = new Dictionary<string, System.Object>();
		p["name"]        = _name; 					// this is the same name from the monoBehaviour
		//p["type"]        = _type;					// I don't know if this is necessary
		//p["description"] = _description; 			// nor this
		p["position"] 	 = Vector3ToProlog(position);
		p["nearestNode"] = (AstarPath.active.GetNearest(position).node as GridNode).GetIndex();
		
		return p;
	}
	*/
	
	static public string Vector3ToProlog(Vector3 v){
		return String.Format("vector({0}, {1}, {2})", v.x, v.y, v.z);
	}
	
	public string toProlog() {
        Dictionary<string, string> percProps = getPerceptionProps();
        List<string> percPropsList = new List<string>();
        foreach (var propName in percProps.Keys) {
            percPropsList.Add("[" + propName + "," + percProps[propName] + "]");
        }
        string percPropsS = PrologList.AtomList<string>(percPropsList);
        return String.Format("entity({0}, {1}, {2}, {3}, {4})", 
			this._name, 
			this.getPrologType(),
			getNode().GetIndex(),
			Vector3ToProlog(position),
            percPropsS);
	}

    protected virtual Dictionary<string, string> getPerceptionProps() {
        return new Dictionary<string, string>();
    }

	public string getName() {
		return _name;
	}
	
	public string getPrologId() {
		return "["+this.getPrologType() + ","+this._name+"]";
	}

    protected virtual string getPrologType() {
        return this._type;
    }
	
	public void setPosition(Vector3 position) {
		this.transform.position = position;
		this.position = position;
	}

	public virtual GridNode getNode() {
		return AstarPath.active.GetNearest(this.transform.position).node as GridNode;				
	}
	
}
