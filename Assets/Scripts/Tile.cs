using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
	public enum Type {Gold, Desert, Wood, Wheat, Clay, Wool, Ore, Ocean,
		HarbourClay, HarbourWheat, HarbourWool, HarbourOre, HarbourWood, Harbour3To1,
		Null
	};
	public static Type[] spawnableTypes = new Type[] {
		Type.Gold,
		Type.Desert,
		Type.Wood,
		Type.Wheat,
		Type.Clay,
		Type.Wool,
		Type.Ore,
		Type.Ocean
	};
	public static Type[] noChipTypes = new Type[]{
		Type.Desert,
		Type.Ocean
	};
	public static Type[] harbourTypes = new Type[] {
		Type.HarbourClay,
		Type.HarbourWheat,
		Type.HarbourWool,
		Type.HarbourOre,
		Type.HarbourWood,
		Type.Harbour3To1
	};
	public static System.Predicate<Tile> isHarbourTile = delegate (Tile obj) {
		return isHarbourType (obj.type);
	};
	public static System.Predicate<Tile.Type> isHarbourType = delegate (Tile.Type obj) {
		return System.Array.Exists (harbourTypes, delegate (Tile.Type t) {
			return t == obj;
		});
	};
	public static System.Predicate<Tile> isLandTile = delegate (Tile obj) {
		return isLandType (obj.type);
	};
	public static System.Predicate<Tile.Type> isLandType = delegate (Tile.Type obj) {
		return HasChip (obj) || obj == Type.Desert;
	};
	public static System.Predicate<Tile> isOceanTile = delegate (Tile obj) {
		return isOceanType (obj.type);
	};
	public static System.Predicate<Tile.Type> isOceanType = delegate(Tile.Type obj) {
		return obj == Type.Ocean;
	};

	public Type type;
	[Range(2,12)]
	public int chipNumber;
	public ParticleSystem onPickup;
	public ParticleInterface particleInterface {
		get {
			return pi;
		}
	}
	[HideInInspector]
	public Vector2 mapPosition;
	[HideInInspector]
	public Edge harbourShoreEdge;
	[HideInInspector]
	public bool hasBandit, hasPirate;
	[HideInInspector]
	public List<Edge> adjacentEdges;
	[HideInInspector]
	public List<Corner> adjacentCorners;

	private Material tileMat;
	private Material chipMat;
	private ParticleInterface pi;

	void Awake() {
		adjacentEdges = new List<Edge> ();
		adjacentCorners = new List<Corner> ();
		pi = transform.Find ("pickupParticles").GetComponent<ParticleInterface> ();
		Draw ();
	}
	
	public void Draw() {
		tileMat = transform.Find ("prop_tileBase").GetComponent<MeshRenderer> ().material;
		Texture tileTex = Resources.Load ("texture_tile_" + type.ToString().ToLower()) as Texture;
		tileMat.SetTexture ("_MainTex", tileTex);

		Transform chipT = transform.Find ("prop_chip");
		if (HasChip(type)) {
			chipMat = chipT.GetComponent<MeshRenderer> ().material;
			Texture chipTex = Resources.Load ("texture_chip_" + chipNumber) as Texture;
			chipMat.SetTexture ("_MainTex", chipTex);
		} else {
			chipT.gameObject.SetActive (false);
		}

		ParticleInterface p = transform.Find ("pickupParticles").GetComponent<ParticleInterface> ();
		p.type = ToResource (type);
		p.Draw ();
	}

	public void Reset(Type _type, int _chipNumber) {
		type = _type;
		chipNumber = _chipNumber;
		Draw ();

		if (isHarbourType (type)) {
			Vector3 toTarget = harbourShoreEdge.position - transform.position;
			Quaternion rot = Quaternion.LookRotation (-1 * toTarget, Vector3.up);
			transform.Rotate (0f, rot.eulerAngles.y + 30f, 0f);
		}
	}

	public static bool HasChip(Type t) {
		System.Predicate<Type> equalsT = delegate(Type t2) {
			return t == t2;
		};
		return !System.Array.Exists (harbourTypes,  equalsT) && !System.Array.Exists(noChipTypes, equalsT) ;
	}

	public static bool HasLandTile(List<Tile> tiles) {
		foreach (Tile t in tiles) {
			if (isLandTile (t))
				return true;
		}
		return false;
	}

	public static bool HasOceanTile (List<Tile> tiles) {
		foreach (Tile t in tiles) {
			if (isOceanTile (t))
				return true;
		}
		return false;
	}
	public static bool HasHarbourTile (List<Tile> tiles) {
		foreach (Tile t in tiles) {
			if (isHarbourTile (t))
				return true;
		}
		return false;
	}

	public static Player.Resource ToResource (Type t) {
		switch (t) {
		case Type.Clay:
			return Player.Resource.Clay;
		case Type.Wood:
			return Player.Resource.Wood;
		case Type.Wool:
			return Player.Resource.Wool;
		case Type.Wheat:
			return Player.Resource.Wheat;
		case Type.Ore:
			return Player.Resource.Ore;
		default:
			return Player.Resource.Null;
		}
	}
}