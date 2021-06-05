using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Building : MonoBehaviour {
	public enum Type {
		Settlement, City, Road, Ship,
		Null, Bandit, Pirate
	};
	public static bool isInSetupPhase = true;
	public static System.Predicate<Building> isSettlement = delegate(Building obj) {
		return obj.type == Type.Settlement || obj.type == Type.City;
	};
	public static System.Predicate<Building> isRoad = delegate(Building obj) {
		return obj.type == Type.Road;
	};
	public static System.Predicate<Building> isShip = delegate(Building obj) {
		return obj.type == Type.Ship;
	};

	public Type type;
	public Color tint;
	public int ownerIndex;
	[HideInInspector]
	public Building onlyNextTo = null;
	[HideInInspector]
	public Tile notNextTo = null;

	private GameController gc;
	[HideInInspector]
	public Edge closestEdge;
	[HideInInspector]
	public Corner closestCorner;
	[HideInInspector]
	public Tile closestTile;

	private System.Predicate<Building> isOwnSettlement;
	private System.Predicate<Building> isOwnRoad;
	private System.Predicate<Building> isOwnShip;

	void Awake() {
		gc = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();
	}

	void Start() {
		GetComponent<MeshRenderer> ().material.color = tint;

		isOwnSettlement = delegate (Building obj) {
			return isSettlement(obj) && obj.ownerIndex == ownerIndex;
		};
		isOwnRoad = delegate (Building obj) {
			return isRoad(obj) && obj.ownerIndex == ownerIndex;
		};
		isOwnShip = delegate (Building obj) {
			return isShip(obj) && obj.ownerIndex == ownerIndex;
		};
	}

	public static Corner FindClosestCorner (Vector3 point, Corner[,] cornerMap) {
		Corner closest = cornerMap[0,0];
		float closestSqrDistanceToCorner = float.MaxValue;
		foreach(Corner c in cornerMap) {
			if(c == null)
				continue;
			float sqrDist = (c.position - point).sqrMagnitude;
			if (sqrDist < closestSqrDistanceToCorner) {
				closestSqrDistanceToCorner = sqrDist;
				closest = c;
			}
		}
		return closest;
	}
	public static Edge FindCloesetEdge (Vector3 point, List<Edge> edges) {
		Edge cloesest = edges[0];
		float closestSqrDistanceToEdge = float.MaxValue;
		foreach(Edge e in edges) {
			float sqrDist = (e.position - point).sqrMagnitude;
			if (sqrDist < closestSqrDistanceToEdge) {
				closestSqrDistanceToEdge = sqrDist;
				cloesest = e;
			}
		}
		return cloesest;
	}
	public static Tile FindCloesetTile (Vector3 point, Tile[,] tiles) {
		Tile cloesest = tiles[0, 0];
		float closestSqrDistanceToEdge = float.MaxValue;
		foreach(Tile t in tiles) {
			float sqrDist = (t.transform.position - point).sqrMagnitude;
			if (sqrDist < closestSqrDistanceToEdge) {
				closestSqrDistanceToEdge = sqrDist;
				cloesest = t;
			}
		}
		return cloesest;
	}
	public static int TypeToPoints (Type t) {
		switch (t) {
		case Type.City:
			return 2;
		case Type.Settlement:
			return 1;
		case Type.Null:
		case Type.Pirate:
		case Type.Road:
		case Type.Ship:
		default:
			return 0;
		}
	}

	public void SnapTo (Vector3 point) {
		switch (type) {
		case Type.City:
		case Type.Settlement:
			closestCorner = FindClosestCorner (point, gc.map.GetCornerMap ());
			transform.position = closestCorner.position;
			break;
		case Type.Ship:
		case Type.Road:
			closestEdge = FindCloesetEdge (point, gc.map.GetEdgeMap ());
			transform.position = closestEdge.position;
			transform.rotation = Quaternion.Euler (new Vector3(0f, 90 + closestEdge.yRotation, 0f));
			break;
		case Type.Bandit:
		case Type.Pirate:
			closestTile = FindCloesetTile (point, gc.map.GetTileMap ());
			transform.position = closestTile.transform.position;
			break;
		default:
			transform.position = point;
			break;
		}
	}

	public void SnapTo (Vector3 point, Vector3 offset) {
		SnapTo (point);
		transform.position += offset;
	}

	public bool PlaceDown (Vector3 point, Vector3 offset) {
		SnapTo (point, offset);

		switch (type) {
		case Type.City:
			if (closestCorner.buildings.Count == 1)
			if (closestCorner.buildings[0].type == Type.Settlement && closestCorner.buildings[0].ownerIndex == ownerIndex) {
				Destroy (closestCorner.buildings[0].gameObject);
				closestCorner.buildings.Clear ();
				closestCorner.buildings.Add (this);
				gc.players [ownerIndex].cities.Add (this);
				return true;
			}
			break;
		case Type.Settlement:
			if (
				Tile.HasLandTile (closestCorner.adjacentTiles)
			    && closestCorner.buildings.Count == 0
			    && !FindSettlementNextToCorner (closestCorner)
			    && (FindRoadFromCorner (closestCorner) || FindShipFromCorner (closestCorner) || isInSetupPhase) )
			{
				closestCorner.buildings.Add (this);

				List<Tile> harbours = closestCorner.adjacentTiles.FindAll (Tile.isHarbourTile);

				foreach (Tile harbour in harbours) {
					if (harbour.harbourShoreEdge.start != closestCorner && harbour.harbourShoreEdge.end != closestCorner)
						continue;

					switch (harbour.type) {
					case Tile.Type.HarbourWood:
						gc.players [ownerIndex].hasTwoToOne [Player.Resource.Wood] = true;
						break;
					case Tile.Type.HarbourWool:
						gc.players [ownerIndex].hasTwoToOne [Player.Resource.Wool] = true;
						break;
					case Tile.Type.HarbourWheat:
						gc.players [ownerIndex].hasTwoToOne [Player.Resource.Wheat] = true;
						break;
					case Tile.Type.HarbourOre:
						gc.players [ownerIndex].hasTwoToOne [Player.Resource.Ore] = true;
						break;
					case Tile.Type.HarbourClay:
						gc.players [ownerIndex].hasTwoToOne [Player.Resource.Clay] = true;
						break;
					case Tile.Type.Harbour3To1:
						gc.players [ownerIndex].hasThreeToOne = true;
						break;
					}
				}
				gc.players [ownerIndex].settlements.Add (this);
				return true;
			}
			break;
		case Type.Ship:
			if (
				( Tile.HasOceanTile (closestEdge.adjacentTiles) || Tile.HasHarbourTile (closestEdge.adjacentTiles) )
				&& closestEdge.buildings.Count == 0
				&& (FindShipFromEdge (closestEdge) || FindOwnSettlementFromEdge (closestEdge)) )
			{
				if (onlyNextTo != null) {
					if (!HasSettlementOnEdge (closestEdge, onlyNextTo))
						return false;
				}

				foreach (Tile t in closestEdge.adjacentTiles){
					if (t.hasPirate)
						return false;
				}

				closestEdge.buildings.Add (this);
				if (!gc.movingShip)
					gc.players [ownerIndex].ships.Add (this);
				return true;
			}
			break;
		case Type.Road:
			if (
				Tile.HasLandTile (closestEdge.adjacentTiles)
				&& closestEdge.buildings.Count == 0
				&& (FindRoadFromEdge (closestEdge) || FindOwnSettlementFromEdge (closestEdge)) )
			{
				if (onlyNextTo != null) {
					if (!HasSettlementOnEdge (closestEdge, onlyNextTo))
						return false;
				}
				closestEdge.buildings.Add (this);
				gc.players [ownerIndex].roads.Add (this);
				return true;
			}
			break;
		case Type.Bandit:
			if (Tile.isLandTile (closestTile)) {
				if (notNextTo != null) {
					if (closestTile == notNextTo)
						return false;
				}
				
				closestTile.hasBandit = true;
				if (!isInSetupPhase)
					gc.StealAtTile (closestTile, false, 1);
				return true;
			}
			break;
		case Type.Pirate:
			if (Tile.isOceanTile (closestTile) || Tile.isHarbourTile (closestTile)) {
				if (notNextTo != null) {
					if (closestTile == notNextTo)
						return false;
				}

				closestTile.hasPirate = true;
				if (!isInSetupPhase)
					gc.StealAtTile (closestTile, false, 1);
				return true;
			}
			break;
		default:
			return true;
		}
		
		return false;
	}

	public void Highlight () {
		GetComponent<MeshRenderer> ().material.color = Color.white;
	}
	public void Unhighlight () {
		GetComponent<MeshRenderer> ().material.color = tint;
	}

	bool FindRoadFromCorner (Corner c) {
		foreach (Edge e in c.adjacentEdges) {
			if (e.buildings.Count > 0)
				if (isOwnRoad(e.buildings[0]))
					return true;
		}
		return false;
	}
	
	bool FindShipFromCorner (Corner c) {
		foreach (Edge e in c.adjacentEdges) {
			if (e.buildings.Count > 0)
				if (isOwnShip(e.buildings[0]))
					return true;
		}
		return false;
	}

	bool FindOwnSettlementFromEdge (Edge e) {
		if (e.start.buildings.Exists (isOwnSettlement))
			return true;
		if (e.end.buildings.Exists (isOwnSettlement))
			return true;
		return false;
	}
	bool HasSettlementOnEdge (Edge e, Building b) {
		System.Predicate<Building> isSame = delegate (Building obj) {
			return obj == b;
		};
		if (e.start.buildings.Exists (isSame))
			return true;
		if (e.end.buildings.Exists (isSame))
			return true;
		return false;
	}
	bool FindSettlementFromEdge (Edge e) {
		if (e.start.buildings.Exists (isSettlement))
			return true;
		if (e.end.buildings.Exists (isSettlement))
			return true;
		return false;
	}

	bool FindShipFromEdge (Edge e) {
		return FindShipFromCorner (e.start) || FindShipFromCorner (e.end);
	}
	
	bool FindRoadFromEdge (Edge e) {
		return FindRoadFromCorner (e.start) || FindRoadFromCorner (e.end);
	}
	
	bool FindSettlementNextToCorner (Corner c) {
		foreach (Edge e in c.adjacentEdges) {
			if (FindSettlementFromEdge (e))
				return true;
		}
		return false;
	}
}
