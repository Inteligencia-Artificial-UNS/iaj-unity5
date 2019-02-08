using UnityEngine;
using System.Collections;

public class Gold : EObject {

    public void Awake()
    {
        // Put field initialization on awake since Start is not called for 
        // objects that are de-activaded on the beginning (see Inn.put)
        this._type = "gold";
        this.weigth = 2;
    }

    public override void Start(){
		base.Start();
		this._engine = SimulationState.getInstance();
		if (!createdByCode){
			_engine.addGold(this);
		}
	}	
		
	public static Gold Create(Vector3 position) {
		
		Object  prefab = SimulationState.getInstance().goldPrefab;			
		GameObject gameObj = Instantiate(prefab, position, Quaternion.identity) as GameObject;
		Gold       gold    = gameObj.GetComponent<Gold>();
		gold.createdByCode = true;
		gold._type 		   = "gold";
		gold._engine	   = SimulationState.getInstance();		
		gold._engine.addGold(gold);
		return gold;
	}
	
}
