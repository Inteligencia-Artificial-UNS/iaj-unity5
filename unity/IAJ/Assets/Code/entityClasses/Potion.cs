using UnityEngine;
using System.Collections;

public class Potion : EObject {

    public void Awake() {
        // Put field initialization on awake since Start is not called for 
        // objects that are de-activaded on the beginning (see Inn.put)
        this._type = "potion";
        this.weigth = 2;
        this.Price = 15;
    }

	public override void Start(){
		base.Start();
        this._engine = SimulationState.getInstance();
		if (!createdByCode){
			_engine.addPotion(this);
		}
	}		
	
	public static Potion Create(Vector3 position) {
		
		Object  prefab = SimulationState.getInstance().potionPrefab;			
		GameObject gameObj = Instantiate(prefab, position, Quaternion.identity) as GameObject;	
		Potion       potion    = gameObj.GetComponent<Potion>();
		potion.createdByCode = true;
		potion._type 		   = "potion";
		potion._engine	   = SimulationState.getInstance();		
		potion._engine.addPotion(potion);
		return potion;
	}
}
