using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {
	public GameObject tilePrefab;
	public float tileHalfWidth = 4f;
	public Transform tileHolder;
	public Map currentMap;
	public event System.Action MapGenerated;

	private float tileHalfHeight;
	private float tileProjWidth;

	private float xOffset;
	private float yOffset;
	private Coord[,] coordMap;
	private Tile[,] tileMap;
	private Corner[,] cornerMap;
	private List<Edge> edgeMap;
	private System.Random pseudo;

	void Awake() {
		tileHalfHeight = Mathf.Cos (30 * Mathf.Deg2Rad) * tileHalfWidth;
		tileProjWidth = tileHalfWidth * Mathf.Cos (60 * Mathf.Deg2Rad);
	}

	void Start() {
		currentMap.seed = MainMenuController.seed;
		currentMap.width = MainMenuController.width;
		currentMap.height = MainMenuController.height;
		NewMap (currentMap);
	}

	public void NewMap(Map m) {
		List<GameObject> children = new List<GameObject>();
		foreach (Transform child in tileHolder.transform) {
			children.Add(child.gameObject);
		}
		children.ForEach(child => Destroy(child));

		currentMap = m;

		pseudo = new System.Random (currentMap.seed);
		xOffset = tileHalfHeight * (currentMap.width - 0.5f);
		yOffset = (tileHalfWidth + tileProjWidth) * (currentMap.height - 1) / 2f;
		Coord.offset = new Vector3 (-xOffset, 0f, -yOffset);

		currentMap.Init ();
		CalculateTilePositions ();
		PlaceTiles ();

		if (MapGenerated != null)
			MapGenerated ();
	}
	public void NewMap () {
		NewMap (currentMap);
	}

	public Vector3 GetOffset() {
		return Coord.offset;
	}
	public Vector3 GetTileSize() {
		return new Vector3 (tileHalfWidth, tileHalfHeight, tileProjWidth);
	}
	public Corner[,] GetCornerMap() {
		return cornerMap;
	}
	public List<Edge> GetEdgeMap() {
		return edgeMap;
	}
	public Tile[,] GetTileMap () {
		return tileMap;
	}

	void CalculateTilePositions() {
		coordMap = new Coord[currentMap.width, currentMap.height];
		cornerMap = new Corner[(coordMap.GetLength (0) + 1) * 2, coordMap.GetLength (1) + 1];

		for (int x = 0; x < coordMap.GetLength (0); x++) {
			for(int y = 0; y < coordMap.GetLength (1); y++) {
				float yPos = y * (tileHalfWidth + tileProjWidth);
				float xPos = x * 2 * tileHalfHeight;
				if (y % 2 == 0) {
					xPos += tileHalfHeight;
				}

				coordMap[x,y] = new Coord(xPos, yPos, x, y);
			}
		}

		for (int x = 0; x < cornerMap.GetLength (0); x++) {
			for (int y = 0; y < cornerMap.GetLength (1); y++) {
				if(Corner.IsValidIndex(x, y, cornerMap))
					cornerMap[x, y] = new Corner (CornerToPosition(x, y));
			}
		}

		for (int x = 0; x < cornerMap.GetLength (0); x++) {
			for (int y = 0; y < cornerMap.GetLength (1); y++) {
				if (!Corner.IsValidIndex(x, y, cornerMap))
					continue;
				Corner c = cornerMap[x, y];
				c.adjacentCorners = GetAdjacentCorners (x, y);
			}
		}
	}

	void PlaceTiles() {
		tileMap = new Tile[coordMap.GetLength(0), coordMap.GetLength(1)];

		// VERIFYING MAP SPECS
		int minTileCount = tileMap.GetLength(0)* tileMap.GetLength(1);
		int tileAmount = 0;
		int chipAmount = 0;
		int noChipAmount = 0;

		foreach (Tile.Type key in currentMap.tiles.Keys)
			tileAmount += currentMap.tiles [key];
		foreach (int key in currentMap.chips.Keys)
			chipAmount += currentMap.chips [key];
		foreach (Tile.Type key in Tile.noChipTypes)
			noChipAmount += currentMap.tiles [key];

		if (tileAmount < minTileCount || chipAmount + noChipAmount < minTileCount) {
			print ("map size: " + minTileCount);
			print ("tiles specified: " + tileAmount);
			print ("tiles specified, that need chips: " + (tileAmount - noChipAmount));
			print ("chips specified: " + chipAmount);
			Debug.LogError ("Map invalid! Saving you a crash right here ;)");
			return;
		}

		// Creating shuffled queues for chips and tiles
		List<Tile.Type> unshuffledTiles = new List<Tile.Type> ();
		foreach (Tile.Type key in currentMap.tiles.Keys) {
			for (int i = 0; i < currentMap.tiles[key]; i++)
				unshuffledTiles.Add (key);
		}

		List<int> unshuffledChips = new List<int> ();
		foreach (int key in currentMap.chips.Keys) {
			for (int i = 0; i < currentMap.chips[key]; i++)
				unshuffledChips.Add (key);
		}

		List<Tile.Type> unshuffledHarbours = new List<Tile.Type> ();
		foreach (Tile.Type key in currentMap.harbours.Keys) {
			for (int i = 0; i < currentMap.harbours[key]; i++)
				unshuffledHarbours.Add (key);
		}

		Queue<Tile.Type> shuffledTiles = new Queue<Tile.Type> (Util.ShuffledList (unshuffledTiles, pseudo));
		Queue<Tile.Type> shuffledHarbours = new Queue<Tile.Type> (Util.ShuffledList (unshuffledHarbours, pseudo));
		Queue<int> shuffledChips = new Queue<int> (Util.ShuffledList (unshuffledChips, pseudo));

		for (int x = 0; x < coordMap.GetLength (0); x++) {
			for(int y = 0; y < coordMap.GetLength (1); y++) {
				Coord c = coordMap[x, y];
	
				Tile t = (Instantiate (tilePrefab, c.position - Vector3.up * 0.03f, tilePrefab.transform.rotation) as GameObject).GetComponent<Tile> ();
				t.gameObject.transform.parent = tileHolder;
	
				// CORNER LINKS
				List<Corner> tileCorners = GetTileCorners(c.mapX, c.mapY);
				foreach(Corner corner in tileCorners) {
					if (corner != null) {
						corner.adjacentTiles.Add (t);
						t.adjacentCorners.Add (corner);
					}
				}
	
				// VARIABLES
				Tile.Type type = shuffledTiles.Dequeue ();
	
				int number = 1;
				if (Tile.HasChip(type)) {
					number = shuffledChips.Dequeue ();
				}
	
				t.Reset (type, number);
				t.mapPosition = new Vector2 (c.mapX, c.mapY);

				// ASSIGN TO MAP
				tileMap [c.mapX, c.mapY] = t;
			}
		}

		edgeMap = new List<Edge> ();
		foreach (Corner c in cornerMap) {
			if(c == null)
				continue;
			List<Corner> adj = c.adjacentCorners;
			foreach (Corner a in adj) {
				Edge edge = new Edge(c, a);
				if ( !edgeMap.Exists(delegate(Edge obj) {return obj == edge;}) ) {
					edgeMap.Add (edge);
					c.adjacentEdges.Add (edge);
					a.adjacentEdges.Add (edge);
					foreach (Tile t in edge.adjacentTiles) {
						t.adjacentEdges.Add (edge);
					}
				}
			}
		}
		foreach (Edge e in edgeMap) {
			e.CalculateAdjacents ();
		}

		List<Tile> nonHarbourShore = new List<Tile> ();
		foreach (Tile tile in tileMap) {
			if (tile.type == Tile.Type.Ocean) {
				List<Corner> shoreCorners = GetTileCorners ((int) tile.mapPosition.x, (int) tile.mapPosition.y).FindAll (Corner.hasLand);
				if (shoreCorners.Count > 0) {
					if (shuffledHarbours.Count > 0) {
						Tile.Type type = shuffledHarbours.Dequeue ();
						bool hasAdjHarbour = GetTileCorners ((int) tile.mapPosition.x, (int) tile.mapPosition.y).Exists (Corner.hasHarbour);
						if (type != Tile.Type.Null && !hasAdjHarbour) {
							List<Edge> targets = new List<Edge> ();
							List<Edge> checkedEdges = new List<Edge> ();
							foreach (Corner shoreCorner in shoreCorners) {
								foreach (Edge shoreEdge in shoreCorner.adjacentEdges.FindAll (Edge.isShore)) {
									System.Predicate<Edge> isSame = delegate(Edge obj) {
										return obj == shoreEdge;
									};
									if (checkedEdges.Exists (isSame)) {
										targets.Add (shoreEdge);
									}
									
									checkedEdges.Add (shoreEdge);
								}
							}
							
							Edge target = targets [pseudo.Next (0, targets.Count)];
							tile.harbourShoreEdge = target;
							tile.Reset (type, tile.chipNumber);
						} else {
							nonHarbourShore.Add (tile);
						}
					}
				}
			}
		}
		
		Queue<Tile> shuffledNonHarbours = new Queue<Tile> (Util.ShuffledList (nonHarbourShore, pseudo));
		while (shuffledHarbours.Count > 0 && shuffledNonHarbours.Count > 0) {
			Tile.Type type = shuffledHarbours.Dequeue ();
			if (type != Tile.Type.Null) {
				Tile tile = shuffledNonHarbours.Dequeue ();
				
				List<Corner> shoreCorners = GetTileCorners ((int) tile.mapPosition.x, (int) tile.mapPosition.y).FindAll (Corner.hasLand);
				List<Edge> targets = new List<Edge> ();
				List<Edge> checkedEdges = new List<Edge> ();
				foreach (Corner shoreCorner in shoreCorners) {
					foreach (Edge shoreEdge in shoreCorner.adjacentEdges.FindAll (Edge.isShore)) {
						System.Predicate<Edge> isSame = delegate(Edge obj) {
							return obj == shoreEdge;
						};
						if (checkedEdges.Exists (isSame)) {
							targets.Add (shoreEdge);
						}
						
						checkedEdges.Add (shoreEdge);
					}
				}
				
				Edge target = targets [pseudo.Next (0, targets.Count)];
				tile.harbourShoreEdge = target;
				tile.Reset (type, tile.chipNumber);
			}
		}
	}

	public List<Corner> GetTileCorners(int _x, int _y) {
		List<Corner> corners = new List<Corner> ();

		for (int x = _x * 2; x <= _x * 2 + 2; x++) {
			for (int y = _y; y <= _y + 1; y++) {
				corners.Add (cornerMap [x + 1 - _y % 2, y]);
			}
		}

		return corners;
	}

	List<Corner> GetAdjacentCorners(int _x, int _y) {
		List<Corner> corners = new List<Corner> ();
		
		if(Corner.IsValidIndex(_x - 1, _y, cornerMap))
			corners.Add (cornerMap[_x - 1, _y]);

		if(Corner.IsValidIndex(_x + 1, _y, cornerMap))
			corners.Add (cornerMap[_x + 1, _y]);

		int y = _y + ((_x + 1 - _y % 2) % 2 == 0 ? 1 : -1);
		if(Corner.IsValidIndex(_x, y, cornerMap))
			corners.Add (cornerMap[_x, y]);
		
		return corners;
	}

	Vector3 CornerToPosition (int _x, int _y) {
		if (!Corner.IsValidIndex(_x, _y, cornerMap))
			return Vector3.zero;

		float x = tileHalfHeight * (_x - 1);
		float z = (tileProjWidth + tileHalfWidth) * (_y ) - tileHalfWidth;

		if ((_y % 2 == 0 && _x % 2 != 0) || (_y % 2 != 0 && _x % 2 == 0)) {
			z += tileProjWidth;
		}

		return new Vector3 (x, 0f, z) + Coord.offset;
	}

	bool IsInMapRange<T>(int x, int y, T[,] map) {
		if (x < map.GetLength (0) && x >= 0 && y < map.GetLength (1) && y >= 0)
			return true;
		else
			return false;
	}

	public int GetFairnessRating () {
		float variance = 0f;
		float mean = 0f;
		int n = 0;
		foreach (Corner c in cornerMap)
			if (c != null) {
				mean += c.rating;
				n++;
			}
		mean /= n;

		foreach (Corner c in cornerMap)
			if (c != null) {
				variance += (c.rating - mean) * (c.rating - mean);
			}
		variance /= n;

		return (int) ((15 - variance) * 10f);
	}

	// DEV MODE
	void OnDrawGizmos() {
		/*if(cornerMap != null)
		for (int x = 0; x < cornerMap.GetLength (0); x++) {
			for (int y = 0; y < cornerMap.GetLength (1); y++) {
				if(!Corner.IsValidIndex(x, y, cornerMap))
					continue;

				float colorPercent = 0f;
				foreach (Tile t in cornerMap[x, y].adjacentTiles) {
					// colorPercent += 0.33f;
					int chance = 6 - Mathf.Abs(t.chipNumber - 7);
					colorPercent += chance / 18f;
				}

				Gizmos.color = Color.Lerp(Color.black, Color.white, colorPercent);
				Gizmos.DrawCube(cornerMap[x, y].position, Vector3.one);
			}
		}

		Gizmos.color = Color.blue;
		foreach (Edge e in edgeMap) {
			//Gizmos.DrawCube (e.position, Vector3.one);
			Vector3 alongEdge = new Vector3(Mathf.Sin(e.yRotation * Mathf.Deg2Rad) * 1f, 0f, Mathf.Cos (e.yRotation * Mathf.Deg2Rad) * 1f);
			Debug.DrawLine (e.position, e.position + alongEdge);
			UnityEditor.Handles.color = Color.black;
			UnityEditor.Handles.Label (e.position, ""+e.yRotation);
		}*/
	}
	// END DEV MODE
}

public class Corner {
	public List<Tile> adjacentTiles;
	public List<Corner> adjacentCorners;
	public List<Edge> adjacentEdges;
	public List<Building> buildings;
	public Vector3 position;
	public bool visited = false;

	public static System.Predicate<Corner> hasLand = delegate (Corner obj) {
		return obj.adjacentTiles.Exists (Tile.isLandTile);
	};
	public static System.Predicate<Corner> hasHarbour = delegate (Corner obj) {
		return obj.adjacentTiles.Exists (Tile.isHarbourTile);
	};

	/// <summary>
	/// Gets the rating, ranging from 3 to 18.
	/// </summary>
	/// <value>The rating.</value>
	public float rating {
		get {
			int chance = 0;
			foreach (Tile t in adjacentTiles)
				chance += 6 - Mathf.Abs(t.chipNumber - 7);
			return chance;
		}
	}

	public Corner(Vector3 pos) {
		adjacentTiles = new List<Tile> ();
		adjacentCorners = new List<Corner> ();
		adjacentEdges = new List<Edge> ();
		buildings = new List<Building> ();
		position = pos;
	}
	
	public static bool IsValidIndex(int x, int y, Corner[,] map) {
		if ((x == 0) && (y == 0 || y == map.GetLength (1) - 1))
			return false;
		else if (x < 0 || x >= map.GetLength (0) || y < 0 || y >= map.GetLength (1))
			return false;
		else
			return true;
	}
}

public class Edge {
	public static System.Predicate<Edge> isShore = delegate(Edge obj) {
		return obj.adjacentTiles.Exists (Tile.isLandTile) && obj.adjacentTiles.Exists (Tile.isOceanTile);
	};

	public Corner start;
	public Corner end;
	public List<Tile> adjacentTiles;
	public List<Edge> adjacentEdges;
	public List<Building> buildings;
	public float yRotation;
	public Vector3 position;

	public Edge(Corner c1, Corner c2) {
		start = c1;
		end = c2;
		adjacentTiles = new List<Tile> ();
		adjacentEdges = new List<Edge> ();
		buildings = new List<Building> ();
		position = (start.position + end.position) * 0.5f;

		float dx = start.position.x - end.position.x;
		float dy = start.position.z - end.position.z;
		if (Mathf.Abs (dx) < float.Epsilon)
			yRotation = 0f;
		else
			yRotation = Mathf.Atan (dx/dy) * Mathf.Rad2Deg;
		if (yRotation > 360f)
			yRotation = 360 - yRotation;
		if (yRotation < 0f)
			yRotation = 360 + yRotation;

	}

	public void CalculateAdjacents () {
		List<Tile> adjStart = start.adjacentTiles;
		List<Tile> adjEnd = end.adjacentTiles;
		foreach(Tile t1 in adjStart) {
			foreach (Tile t2 in adjEnd) {
				if (Object.ReferenceEquals(t1,t2)) {
					adjacentTiles.Add (t1);
				}
			}
		}

		List<Edge> aStart = start.adjacentEdges;
		List<Edge> aEnd = end.adjacentEdges;
		foreach(Edge e in aStart) {
			if(!aEnd.Exists(delegate (Edge other) {return e == other;})) {
				adjacentEdges.Add (e);
			}
		}
		foreach(Edge e in aEnd) {
			if(!aStart.Exists(delegate (Edge other) {return e == other;})) {
				adjacentEdges.Add (e);
			}
		}
	}

	public Corner OppositeCorner (Corner c) {
		if (start == c)
			return end;
		else
			return start;
	}

	public static bool operator ==(Edge e1, Edge e2) {
		return (e1.start == e2.start && e1.end == e2.end) || (e1.end == e2.start && e1.start == e2.end);
	}

	public static bool operator !=(Edge e1, Edge e2) {
		return !(e1 == e2);
	}

	public override bool Equals(System.Object obj) {
		if (obj is Edge)
			return this == (obj as Edge);
		else
			return false;
	}

	public override int GetHashCode() {
		return start.GetHashCode () * end.GetHashCode ();
	}
}

public class Coord {
	public static Vector3 offset = Vector3.zero;
	
	public float worldX;
	public float worldY;
	public int mapX;
	public int mapY;
	public bool useOffset = true;

	public Coord(float world_x, float world_y, int map_x, int map_y) {
		worldX = world_x;
		worldY = world_y;
		mapX = map_x;
		mapY = map_y;
	}

	public Coord(float world_x, float world_y) {
		worldX = world_x;
		worldY = world_y;
		mapX = -1;
		mapY = -1;
	}
	
	public Vector3 position {
		get {
			return new Vector3(worldX, 0f, worldY) + (useOffset ? offset : Vector3.zero);
		}
	}
}

[System.Serializable]
public class Map {
	public int seed = 10;
	public int width = 7;
	public int height = 7;
	[Header("Tile specs")]
	[Tooltip("Do NOT enter harbours here!")]
	public List<TileEntry> tileEntries;
	public List<ChipEntry> chipEntries;
	[Tooltip("Should have the same size as there are ocean tiles, Null should be used to fill any non-harbour tiles")]
	public List<TileEntry> harbourEntries;

	public Dictionary<Tile.Type, int> tiles;
	public Dictionary<int, int> chips;
	public Dictionary<Tile.Type, int> harbours;

	public void Init ()
	{
		tiles = new Dictionary<Tile.Type, int> ();
		chips = new Dictionary<int, int> ();
		harbours = new Dictionary<Tile.Type, int> ();
		foreach(TileEntry te in tileEntries)
			tiles.Add (te.tileType, te.count);
		foreach(ChipEntry ce in chipEntries)
			chips.Add (ce.chipNumber, ce.count);
		foreach (TileEntry te in harbourEntries)
			harbours.Add (te.tileType, te.count);
	}

	[System.Serializable]
	public class TileEntry {
		public Tile.Type tileType;
		public int count;
	}

	[System.Serializable]
	public class ChipEntry {
		public int chipNumber;
		public int count;
	}
}