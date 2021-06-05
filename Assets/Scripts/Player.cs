using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Player {
	public enum Resource {Wood, Wool, Wheat, Ore, Clay, Null};

	public int id;
	public string name;
	public Color tint;
	public Dictionary<Resource, int> resources;
	public Dictionary<Building.Type, int> availBuildings;
	public Dictionary<Building.Type, int> availBuildingsOriginal;
	public int resourceCount {
		get {
			int resCount = 0;
			foreach (Player.Resource r in resources.Keys) {
				resCount += resources [r];
			}
			return resCount;
		}
	}
	/// <summary>
	/// Points from cards, not buildings or achievements
	/// </summary>
	[HideInInspector]
	public int cardPoints = 0;
	[HideInInspector]
	public int buildingPoints {
		get {
			int p = 0;
			foreach (Building.Type key in availBuildings.Keys) {
				int original = availBuildings [key];
				availBuildingsOriginal.TryGetValue (key, out original);
				p += (original - availBuildings [key]) * Building.TypeToPoints (key);
			}
			return p;
		}
	}
	[HideInInspector]
	public int knights = 0;
	[HideInInspector]
	public bool hasKnightAchievement = false;
	[HideInInspector]
	public bool hasTradeRouteAchievement = false;
	[HideInInspector]
	public int totalPoints {
		get {
			return cardPoints + (hasKnightAchievement ? 2 : 0) + (hasTradeRouteAchievement ? 2 : 0) + buildingPoints;
		}
	}

	public List<BuildingIntEntry> buildingEntries;
	public List<ResourceIntEntry> resourceEntries;
	
	public Dictionary<Player.Resource, bool> hasTwoToOne;
	[HideInInspector]
	public bool hasThreeToOne = false;
	[HideInInspector]
	public List<Building> settlements, cities, roads, ships;
	[HideInInspector]
	public Card.Type cardHeld = Card.Type.Null;

	private GameController gc;
	private UIManager ui;
	
	public void Init() {
		gc = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();
		ui = gc.GetComponent<UIManager> ();

		settlements = new List<Building> ();
		cities = new List<Building> ();
		roads = new List<Building> ();
		ships = new List<Building> ();

		hasTwoToOne = new Dictionary<Resource, bool> ();
		foreach (Resource r in System.Enum.GetValues(typeof(Resource)))
			hasTwoToOne.Add (r, false);
		
		availBuildings = new Dictionary<Building.Type, int> ();
		availBuildingsOriginal = new Dictionary<Building.Type, int> ();
		foreach (BuildingIntEntry bce in buildingEntries) {
			availBuildings.Add (bce.type, bce.value);
			availBuildingsOriginal.Add (bce.type, bce.value);
		}
		
		resources = new Dictionary<Resource, int> ();
		foreach (ResourceIntEntry rie in resourceEntries) {
			resources.Add (rie.type, rie.value);
		}
	}

	public void Pay (List<ResourceIntEntry> purchase) {
		SubtractResources (purchase);
		gc.bank.AddResources (purchase);
		ui.AnimatePay (purchase, id);
	}
	public void Pay (ResourceIntEntry rie) {
		SubtractResource (rie);
		gc.bank.AddResource (rie);
		ui.AnimatePay (new List<ResourceIntEntry> () {rie}, id);
	}
	public void Receive (List<ResourceIntEntry> purchase) {
		AddResources (purchase);
		gc.bank.SubtractResources (purchase);
	}
	public void Receive (ResourceIntEntry rie) {
		AddResource (rie);
		gc.bank.SubtractResource (rie);
	}
	
	public void SubtractResources (List<ResourceIntEntry> res) {
		foreach (ResourceIntEntry cost in res)
			SubtractResource (cost);
	}
	public void SubtractResource (ResourceIntEntry rie) {
		resources [rie.type] -= rie.value;
	}
	public void AddResources (List<ResourceIntEntry> res) {
		foreach (ResourceIntEntry cost in res)
			AddResource (cost);
	}
	public void AddResource (ResourceIntEntry rie) {
		resources [rie.type] += rie.value;
	}

	public bool CanBuild (Building.Type building, List<ResourceIntEntry> price) {
		bool hasRes = availBuildings[building] > 0;
		foreach (ResourceIntEntry cost in price)
			hasRes &= resources [cost.type] >= cost.value;
		
		return hasRes;
	}
	public bool CanPay (List<ResourceIntEntry> res) {
		bool hasRes = true;
		foreach (ResourceIntEntry cost in res)
			hasRes &= resources [cost.type] >= cost.value;
		
		return hasRes;
	}
}