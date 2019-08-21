using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Booster : EObject
{

    public enum BoosterType
    {
        Attack, SuperAttack, Defense, SuperDefense
    }

    public static int getAttackPlus(BoosterType booster)
    {
        switch (booster)
        {
            case BoosterType.Attack: return 50;
            case BoosterType.SuperAttack: return 100;
            default: return 0;
        }
    }

    public static int getDefensePlus(BoosterType booster)
    {
        switch (booster)
        {
            case BoosterType.Defense: return 50;
            case BoosterType.SuperDefense: return 100;
            default: return 0;
        }
    }

    private static int getPrice(BoosterType booster)
    {
        switch (booster)
        {
            case BoosterType.Attack: return 20;
            case BoosterType.SuperAttack: return 40;
            case BoosterType.Defense: return 20;
            default: return 40; // BoosterType.SuperDefense
        }
    }

    public static Color getColor(BoosterType booster)
    {
        switch (booster)
        {
            case BoosterType.Attack: return Color.yellow;
            case BoosterType.SuperAttack: return Color.yellow;
            case BoosterType.Defense: return Color.white;
            default: return Color.white; // BoosterType.SuperDefense
        }
    }

    public BoosterType Type { get; private set; }
    public int Attack { get; set; }
    public int Defense { get; set; }

    public bool isDefense()
    {
        return Type == BoosterType.Defense || Type == BoosterType.SuperDefense;
    }

    public void Awake()
    {
        // Put field initialization on awake since Start is not called for 
        // objects that are de-activaded on the beginning (see Inn.put)
        this._type = "booster";
        this.weigth = 2;
    }

    public override void Start()
    {
        base.Start();
        if (!createdByCode)
        {
            SimulationState.getInstance().addEntityObject(this);
        }
        //gameObject.GetComponent<Renderer>().material.color = Booster.getColor(this.Type);
        //gameObject.GetComponent<Renderer>().material.color = Color.blue;
    }

    public static Booster Create(Vector3 position, BoosterType boosterType)
    {
        return Create(position, boosterType, getAttackPlus(boosterType), getDefensePlus(boosterType), getPrice(boosterType));
    }

    private static Booster Create(Vector3 position, BoosterType boosterType, int attack, int defense, int price)
    {
        Object prefab = boosterType == BoosterType.Defense || boosterType == BoosterType.SuperDefense ?
                                                   SimulationState.getInstance().helmetPrefab :
                                                   SimulationState.getInstance().armorPrefab;
        GameObject gameObj = Instantiate(prefab, position, Quaternion.identity) as GameObject;
        Booster booster = gameObj.GetComponent<Booster>();
        gameObj.GetComponent<Renderer>().material.color = Booster.getColor(boosterType);
        booster.Type = boosterType;
        booster.createdByCode = true;
        booster._type = "booster";
        booster.Price = price;
        booster.Attack = attack;
        booster.Defense = defense;
        if (booster.isDefense())
        {
            booster.transform.localScale = new Vector3(boosterType == BoosterType.SuperDefense ? 1.5f : 1.1f,
                                                       1.1f,
                                                       boosterType == BoosterType.SuperDefense ? 1.5f : 1.1f);
        }
        else
        {
            booster.transform.localScale = new Vector3(booster.transform.localScale.x,
                                                       boosterType == BoosterType.SuperAttack ? .5f : .2f,
                                                       booster.transform.localScale.z);
        }
        SimulationState.getInstance().addEntityObject(booster);
        return booster;
    }

    protected override Dictionary<string, string> getPerceptionProps()
    {
        Dictionary<string, string> percProps = base.getPerceptionProps();
        if (this.Attack > 0) {
            percProps.Add("attack", this.Attack.ToString());
        }
        if (this.Defense > 0) {
            percProps.Add("defense", this.Defense.ToString());
        }
        return percProps;
    }

    protected override string getPrologType()
    {
        switch (this.Type)
        {
            case BoosterType.Attack: return "ammo";
            case BoosterType.SuperAttack: return "ammo";
            case BoosterType.Defense: return "helmet";
            default: return "helmet";
        }
    }

}
