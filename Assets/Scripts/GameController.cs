using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour {
	public Transform buildingHolder;
	public List<BuildingPrefabEntry> buildingPrefabs;
	public Building bandit;
	public Building pirate;
	public List<BuildingCost> buildCostEntries;
	public List<ResourceIntEntry> devCost;
	public int pointsToWin;
	public MapGenerator map;
	public Player[] players;
	public Player bank;
	public List<CardIntEntry> cardEntries;

	public event System.Action OnMissingResources;
	public event System.Action OnNextTurn;
	public event System.Action OnNewBuilding;
	public TradeManager trading {
		get {
			return tradeManager;
		}
	}

	private TradeManager tradeManager;
	private Dictionary<Building.Type, GameObject> prefabDictionary;
	[HideInInspector]
	public Dictionary<Building.Type, List<ResourceIntEntry>> buildingCosts;
	[HideInInspector]
	public Building hoverInstance = null;
	[HideInInspector]
	public int pIndex = 0;
	private int turnsInSetup;
	private int turn = 0;
	[HideInInspector]
	public bool setup_settPlaced;
	[HideInInspector]
	public bool setup_connPlaced;
	[HideInInspector]
	public Building setup_newSettlement;
	[HideInInspector]
	public bool movingShip = false;
	private Vector3 priorShipPos;
	private List<Building> moveableShips;
	private List<Building> shipsPlacedThisTurn;
	private Queue<Card.Type> cardStack;
	private int connPlaced = 0;
	private bool freeBuying = false;
	[HideInInspector]
	public bool isGameWon = false;

	private UIManager ui;
	
	void Awake() {
		if (MainMenuController.used) {
			Player[] newPlayers = new Player[MainMenuController.names.Count];
			for (int i = 0; i < newPlayers.Length; i++) {
				Player tmp = players [i];
				tmp.name = MainMenuController.names [i];
				newPlayers [i] = tmp;
			}
			players = newPlayers;
		}

		List<Card.Type> cardTypes = new List<Card.Type> ();
		foreach (CardIntEntry cie in cardEntries) {
			for (int i = 0; i < cie.value; i++)
				cardTypes.Add (cie.type);
		}
		cardStack = new Queue<Card.Type> (Util.ShuffledList (cardTypes));

		shipsPlacedThisTurn = new List<Building> ();
		turnsInSetup = 2 * players.Length;
		ui = GetComponent<UIManager> ();
		tradeManager = GetComponent<TradeManager> ();
		prefabDictionary = new Dictionary<Building.Type, GameObject> ();
		foreach(BuildingPrefabEntry e in buildingPrefabs)
			prefabDictionary.Add(e.type, e.prefab);

		buildingCosts = new Dictionary<Building.Type, List<ResourceIntEntry>> ();
		foreach (BuildingCost bc in buildCostEntries)
			buildingCosts.Add (bc.type, bc.cost);

		foreach (Player p in players)
			p.Init ();
		bank.Init ();
	}

	void Start () {
		ui.Init (players.Length);
		NextTurn ();
	}

	void Update() {
		BuildingUpdate ();

		if (!isGameWon)
		foreach (Player p in players) {
			if (p.totalPoints >= pointsToWin) {
				ui.ShowWinScreen (p);
				isGameWon = true;
			}
		}
	}

	void BuildingUpdate() {
		if (hoverInstance != null) {
			Ray camRay = Camera.main.ScreenPointToRay (Input.mousePosition);
			Plane castPlane = new Plane (Vector3.up, Vector3.up);
			float castDistance;
			
			if (castPlane.Raycast (camRay, out castDistance)) {
				Vector3 point = camRay.GetPoint (castDistance);
				hoverInstance.SnapTo (point);
			}
		}

		if (Input.GetMouseButtonDown (0)) {
			if (hoverInstance != null) {
				if (hoverInstance.PlaceDown (hoverInstance.transform.position, Vector3.up * 0.1f)) {
					if (!movingShip && !(hoverInstance.type == Building.Type.Bandit || hoverInstance.type == Building.Type.Pirate))
						players[pIndex].availBuildings[hoverInstance.type]--;

					if (hoverInstance.type == Building.Type.City)
						players[pIndex].availBuildings[Building.Type.Settlement]++;

					if (hoverInstance.type == Building.Type.Ship)
						shipsPlacedThisTurn.Add (hoverInstance);

					if (hoverInstance.type == Building.Type.Ship || hoverInstance.type == Building.Type.Road)
						connPlaced++;
					
					if (hoverInstance.type == Building.Type.Ship || hoverInstance.type == Building.Type.Road || hoverInstance.type == Building.Type.Settlement)
						CalculateLongestTradeRoute ();
					
					if (hoverInstance.type == Building.Type.Bandit || hoverInstance.type == Building.Type.Pirate) {
						ui.EnableButtons (true, true, true, true);
						CheckPlayCard ();
					}

					if(!Building.isInSetupPhase) {
						if (!movingShip && !(hoverInstance.type == Building.Type.Bandit || hoverInstance.type == Building.Type.Pirate) && !freeBuying)
							players[pIndex].Pay (buildingCosts[hoverInstance.type]);
					} else {
						if (hoverInstance.type == Building.Type.Settlement) {
							setup_settPlaced = true;
							setup_newSettlement = hoverInstance;
							if (reverse) {
								foreach (Tile t in setup_newSettlement.closestCorner.adjacentTiles) {
									Player.Resource r = Tile.ToResource (t.type);
									if (r != Player.Resource.Null)
										players [pIndex].Receive (new ResourceIntEntry (r, 1));
									t.particleInterface.Activate (1);
								}
							}
						}
						else if (hoverInstance.type == Building.Type.Road || hoverInstance.type == Building.Type.Ship)
							setup_connPlaced = true;

						if (setup_settPlaced && setup_connPlaced)
							NextTurn ();
					}

					hoverInstance = null;
					if (movingShip) {
						movingShip = false;
						priorShipPos = Vector3.zero;
						moveableShips.Clear ();
						ui.EnableButtons (false, false, true, false);
					}

					if (OnNewBuilding != null)
						OnNewBuilding ();
				}
			}

			if (movingShip && hoverInstance == null) {
				Ray camRay = Camera.main.ScreenPointToRay (Input.mousePosition);
				Plane castPlane = new Plane (Vector3.up, Vector3.up);
				float castDistance;
				
				if (castPlane.Raycast (camRay, out castDistance)) {
					Vector3 point = camRay.GetPoint (castDistance);
					
					Edge selectedEdge = Building.FindCloesetEdge (point, map.GetEdgeMap ());
					if (selectedEdge.buildings.Count > 0) {
						Building selectedShip = selectedEdge.buildings [0];
						if (moveableShips.Exists (delegate (Building obj) {return obj == selectedShip;}))
							ChooseShip (selectedShip);
					}
				}
			}
		}
	}

	public void Build (Building.Type type) {
		if (Building.isInSetupPhase || freeBuying) {
			UpdateHover (type);
			return;
		}

		if (players [pIndex].CanBuild (type, buildingCosts [type]))
			UpdateHover (type);
		else if (OnMissingResources != null)
			OnMissingResources ();
	}

	void UpdateHover (Building.Type newType) {
		if (hoverInstance != null) {
			if (hoverInstance.type == newType)
				return;
			Destroy (hoverInstance.gameObject);
		}

		hoverInstance = (Instantiate (prefabDictionary [newType], Vector3.zero, prefabDictionary [newType].transform.rotation) as GameObject).GetComponent<Building> ();
		hoverInstance.type = newType;
		hoverInstance.tint = players [pIndex].tint;
		hoverInstance.transform.parent = buildingHolder;
		hoverInstance.ownerIndex = pIndex;

		if (Building.isInSetupPhase && setup_settPlaced && !setup_connPlaced && (Building.isRoad (hoverInstance) || Building.isShip (hoverInstance))) {
			hoverInstance.onlyNextTo = setup_newSettlement;
		}
	}

	private bool reverse = false;
	public void NextTurn() {
		if (Building.isInSetupPhase && pIndex + 1 == players.Length && !reverse) {
			reverse = true;
			pIndex++;
		}

		if (turn != 0) {
			if (reverse)
				pIndex--;
			else
				pIndex++;
		}

		turn++;
		if (Building.isInSetupPhase && turn > turnsInSetup) {
			PlaceVillains ();
			Building.isInSetupPhase = false;
			ui.EnableButtons (true, true, true, true);
			reverse = false;
			pIndex++;
		}

		if (pIndex < 0)
			pIndex += players.Length;
		pIndex = pIndex % players.Length;

		if (Building.isInSetupPhase) {
			setup_settPlaced = false;
			setup_connPlaced = false;
		} else {
			ui.EnableButtons (false, false, false, true);
		}

		shipsPlacedThisTurn.Clear ();

		StopCoroutine ("RoadCardLoop");
		ui.EnableButtons (true, false, false, false); // undo roadCard
		freeBuying = false;

		if (OnNextTurn != null)
			OnNextTurn ();
	}

	public void PlaceVillains () {
		bandit = (Instantiate (bandit.gameObject, Vector3.zero, bandit.gameObject.transform.rotation) as GameObject).GetComponent<Building> ();
		bandit.type = Building.Type.Bandit;
		pirate = (Instantiate (pirate.gameObject, Vector3.zero, pirate.gameObject.transform.rotation) as GameObject).GetComponent<Building> ();
		pirate.type = Building.Type.Pirate;
		List<Tile> land = new List<Tile> ();
		foreach (Tile t in map.GetTileMap ()) {
			if (Tile.isLandTile (t))
				land.Add (t);
		}
		bandit.PlaceDown (land [Random.Range (0, land.Count)].transform.position, Vector3.up * 0.1f);
		List<Tile> water = new List<Tile> ();
		foreach (Tile t in map.GetTileMap ()) {
			if (Tile.isOceanTile (t) || Tile.isHarbourTile (t))
				water.Add (t);
		}
		pirate.PlaceDown (water [Random.Range (0, water.Count)].transform.position, Vector3.up * 0.1f);
	}

	public void MoveShip () {
		if (Building.isInSetupPhase)
			return;

		moveableShips = new List<Building> ();
		foreach (Building ship in players [pIndex].ships) {
			bool hasNeighbourSettStart = false;
			bool hasNeighbourSettEnd = false;
			bool hasPirateNextToShip = false;
			List<Building> startShips = new List<Building> ();
			List<Building> endShips = new List<Building> ();

			if (ship.closestEdge.start.buildings.Count > 0)
				hasNeighbourSettStart = true;
			if (ship.closestEdge.end.buildings.Count > 0)
				hasNeighbourSettEnd = true;

			foreach (Edge adj in ship.closestEdge.start.adjacentEdges) {
				if (adj != ship.closestEdge)
					if (adj.buildings.Count > 0)
						if (adj.buildings [0].type == Building.Type.Ship && adj.buildings [0].ownerIndex == pIndex)
							if (!startShips.Exists (delegate(Building obj) {return obj == adj.buildings [0];}))
								startShips.Add (adj.buildings [0]);
			}
			foreach (Edge adj in ship.closestEdge.end.adjacentEdges) {
				if (adj != ship.closestEdge)
					if (adj.buildings.Count > 0)
						if (adj.buildings [0].type == Building.Type.Ship && adj.buildings [0].ownerIndex == pIndex)
							if (!endShips.Exists (delegate(Building obj) {return obj == adj.buildings [0];}))
								endShips.Add (adj.buildings [0]);
			}
			foreach (Tile t in ship.closestEdge.adjacentTiles) {
				if (t.hasPirate)
					hasPirateNextToShip = true;
			}

			if (shipsPlacedThisTurn.Exists (delegate (Building obj) {return obj == ship;}) || hasPirateNextToShip)
				continue;

			if (hasNeighbourSettStart || hasNeighbourSettEnd) {
				if (hasNeighbourSettStart) {
					if (endShips.Count == 0) {
						moveableShips.Add (ship);
					}
				}
				if (hasNeighbourSettEnd) {
					if (startShips.Count == 0) {
						moveableShips.Add (ship);
					}
				}
			} else {
				if (endShips.Count > 0 && startShips.Count > 0) {
					// would be in middle
				} else {
					moveableShips.Add (ship);
				}
			}
		}

		foreach (Building ship in moveableShips)
			ship.Highlight ();

		if (moveableShips.Count > 0) {
			movingShip = true;
			ui.DisableButtons (false, false, true, true);
		}
	}
	public void ChooseShip (Building ship) {
		priorShipPos = new Vector3(ship.transform.position.x, 1f, ship.transform.position.z);
		ship.closestEdge.buildings.Clear ();
		ship.onlyNextTo = null;
		hoverInstance = ship;
		foreach (Building b in moveableShips)
			b.Unhighlight ();
	}
	public void RevertShipMove () {
		if (hoverInstance != null) {
			hoverInstance.PlaceDown (priorShipPos, Vector3.up * 0.1f);
			hoverInstance = null;
		}
		movingShip = false;
		foreach (Building b in moveableShips)
			b.Unhighlight ();
		moveableShips.Clear ();
		ui.EnableButtons (false, false, true, true);
	}

	public void OpenTrade (int otherPlayerIndex) {
		if (otherPlayerIndex == 4) {
			tradeManager.Open (players[pIndex], bank);
		} else
			tradeManager.Open (players [pIndex], players [otherPlayerIndex]);
	}

	public void DiceRolled (int roll) {
		if (roll == 7) {
			for (int playerInd = 0; playerInd < players.Length; playerInd++) {
				Player p = players [playerInd];
				int resCount = p.resourceCount;
				if (resCount > 7) {
					StealFrom (playerInd, true, Mathf.FloorToInt (resCount / 2f));
				}
			}
			ui.ShowBanditChoice ();
			return;
		}

		Dictionary<Player.Resource, int> [] pickedUpRes = new Dictionary<Player.Resource, int> [players.Length];
		for (int i = 0; i < pickedUpRes.Length; i++) {
			pickedUpRes [i] = new Dictionary<Player.Resource, int> ();
			pickedUpRes [i].Add (Player.Resource.Clay, 0);
			pickedUpRes [i].Add (Player.Resource.Wood, 0);
			pickedUpRes [i].Add (Player.Resource.Wool, 0);
			pickedUpRes [i].Add (Player.Resource.Wheat, 0);
			pickedUpRes [i].Add (Player.Resource.Ore, 0);
		}

		foreach (Tile t in map.GetTileMap ()) {
			int collected = 0;
			foreach (Corner c in t.adjacentCorners) {
				if (c == null)
					continue;
				if (c.buildings.Count == 0)
					continue;

				if (t.chipNumber == roll && !t.hasBandit) {
					Building b = c.buildings [0];
					Player.Resource r = Tile.ToResource (t.type);
					if (r != Player.Resource.Null) {
						int amount = b.type == Building.Type.Settlement ? 1 : (b.type == Building.Type.City ? 2 : 0);
						pickedUpRes [b.ownerIndex][r] += amount;
						collected += amount;
					}
				}
			}
			t.particleInterface.Activate (collected);
		}

		Dictionary<Player.Resource, int> combined = new Dictionary<Player.Resource, int> ();
		combined.Add (Player.Resource.Clay, 0);
		combined.Add (Player.Resource.Wood, 0);
		combined.Add (Player.Resource.Wool, 0);
		combined.Add (Player.Resource.Wheat, 0);
		combined.Add (Player.Resource.Ore, 0);
		foreach (Dictionary<Player.Resource, int> playerReceive in pickedUpRes) {
			foreach (Player.Resource key in playerReceive.Keys) {
				combined [key] += playerReceive [key];
			}
		}
		List<ResourceIntEntry> combinedRes = new List<ResourceIntEntry> ();
		foreach (Player.Resource key in combined.Keys) {
			combinedRes.Add (new ResourceIntEntry (key, combined [key]));
		}
		if (!bank.CanPay (combinedRes))
			return;

		for (int i = 0; i < pickedUpRes.Length; i++) {
			Dictionary<Player.Resource, int> d = pickedUpRes[i];
			List<ResourceIntEntry> res = new List<ResourceIntEntry> ();
			foreach (Player.Resource r in d.Keys) {
				res.Add (new ResourceIntEntry(r, d[r]));
			}
			players[i].Receive (res);
		}

		if (roll != 7)
			CheckPlayCard ();
	}

	public void SelectBandit () {
		ui.HideBanditChoice ();
		ui.DisableButtons (true, true, true, true);
		Tile prev = bandit.closestTile;
		prev.hasBandit = false;
		hoverInstance = bandit;
		hoverInstance.notNextTo = prev;
	}
	public void SelectPirate () {
		ui.HideBanditChoice ();
		ui.DisableButtons (true, true, true, true);
		Tile prev = pirate.closestTile;
		prev.hasPirate = false;
		hoverInstance = pirate;
		hoverInstance.notNextTo = prev;
	}

	public void StealAtTile (Tile tile, bool toBank, int amount) {
		List<int> adjPlayers = new List<int> ();

		foreach (Corner c in map.GetTileCorners ((int) tile.mapPosition.x, (int) tile.mapPosition.y)) {
			if (c.buildings.Count > 0) {
				int otherIndex = c.buildings [0].ownerIndex;
				if (otherIndex != pIndex)
					if (players [otherIndex].resourceCount >= amount)
						if (!adjPlayers.Exists (delegate (int obj) {return obj == otherIndex;}))
							adjPlayers.Add (otherIndex);
			}
		}
		foreach (Edge e in tile.adjacentEdges) {
			if (e.buildings.Count > 0) {
				int otherIndex = e.buildings [0].ownerIndex;
				if (otherIndex != pIndex)
					if (players [otherIndex].resourceCount >= amount)
						if (!adjPlayers.Exists (delegate (int obj) {return obj == otherIndex;}))
							adjPlayers.Add (otherIndex);
			}
		}

		if (adjPlayers.Count > 1)
			ui.ShowPlayerChoice (adjPlayers, StealFrom, toBank, amount);
		else if (adjPlayers.Count == 1) {
			StealFrom (adjPlayers [0], toBank, amount);
		}
	}

	public void StealFrom (int playerIndex, bool toBank, int amount) {
		List<Player.Resource> res = new List<Player.Resource> ();

		Dictionary<Player.Resource, int> stolen = new Dictionary<Player.Resource, int> ();
		stolen.Add (Player.Resource.Clay, 0);
		stolen.Add (Player.Resource.Wood, 0);
		stolen.Add (Player.Resource.Wool, 0);
		stolen.Add (Player.Resource.Wheat, 0);
		stolen.Add (Player.Resource.Ore, 0);

		foreach (Player.Resource key in players [playerIndex].resources.Keys) {
			for (int i = 0; i < players [playerIndex].resources [key]; i++)
				res.Add (key);
		}

		Queue<Player.Resource> shuffledRes = new Queue<Player.Resource> (Util.ShuffledList (res));
		for (int i = 0; i < amount && shuffledRes.Count > 0; i++) {
			Player.Resource r = shuffledRes.Dequeue ();
			stolen [r]++;
		}

		List<ResourceIntEntry> lostRes = new List<ResourceIntEntry> ();
		foreach (Player.Resource key in stolen.Keys) {
			lostRes.Add (new ResourceIntEntry (key, stolen [key]));
		}

		if (toBank) {
			players [playerIndex].Pay (lostRes);
		} else {
			players [playerIndex].SubtractResources (lostRes);
			players [pIndex].AddResources (lostRes);
		}
	}

	public bool CanTakeCard () {
		return cardStack.Count > 0 && players [pIndex].CanPay (devCost);
	}
	public void TakeCard () {
		players [pIndex].Pay (devCost);
		Card.Type type = cardStack.Dequeue ();
		print ("card drawn: " + type.ToString ());
		players [pIndex].cardHeld = type;
	}
	void PlayCard (Card.Type type) {
		switch (type) {
		case Card.Type.Null:
			return;
		case Card.Type.Knight:
			int maxKnights = 0;
			Player currentMax = null;
			foreach (Player p in players) {
				if (p.knights > maxKnights)
					maxKnights = p.knights;
			}
			players [pIndex].knights++;
			if (players [pIndex].knights > maxKnights && players [pIndex].knights >= 3) {
				if (currentMax != null)
					currentMax.hasKnightAchievement = false;
				players [pIndex].hasKnightAchievement = true;
			}
			ui.ShowBanditChoice ();
			break;
		case Card.Type.Road:
			connPlaced = 0;
			freeBuying = true;
			ui.OverrideBuyButtons (false, false, true, true, false);
			StartCoroutine ("RoadCardLoop");
			break;
		case Card.Type.Monopoly:
			ui.ShowResourceChoice (ExecuteMonopoly, "Monopoly: Receive all resources of one type");
			break;
		case Card.Type.Invention:
			ui.ShowResourceChoice (ReceiveTwoRes, "Invention: Receive two resources of one type");
			break;
		default:
			players [pIndex].cardPoints++;
			break;
		}
	}
	IEnumerator RoadCardLoop () {
		while (connPlaced < 2)
			yield return null;
		ui.EnableButtons (true, false, false, false);
		freeBuying = false;
	}

	public void ReceiveTwoRes (Player.Resource res) {
		players [pIndex].Receive (new ResourceIntEntry (res, 2));
	}
	public void ExecuteMonopoly (Player.Resource res) {
		int resReceived = 0;
		for (int i = 0; i < players.Length; i++) {
			if (i != pIndex) {
				int availRes = players [i].resources [res];
				resReceived += availRes;
				players [i].SubtractResource (new ResourceIntEntry (res, availRes));
			}
		}
		players [pIndex].AddResource (new ResourceIntEntry (res, resReceived));
	}
	public void CheckPlayCard () {
		PlayCard (players [pIndex].cardHeld);
		players [pIndex].cardHeld = Card.Type.Null;
	}

	public void CalculateLongestTradeRoute () {
		int maxLength = 0;
		int secondLength = 0;
		Player bestPlayer = null;

		foreach (Player p in players) {
			int maxPath = 0;

			foreach (Building ship in p.ships) {
				int longestInPath = SearchForLongestPath (ship, p.id);
				if (longestInPath > maxPath)
					maxPath = longestInPath;
			}
			foreach (Building road in p.roads) {
				int longestInPath = SearchForLongestPath (road, p.id);
				if (longestInPath > maxPath)
					maxPath = longestInPath;
			}

			if (maxPath > maxLength) {
				secondLength = maxLength;
				maxLength = maxPath;
				bestPlayer = p;
			} else if (maxPath == maxLength) {
				secondLength = maxPath;
			}
		}

		if (maxLength >= 5) {
			foreach (Player p in players)
				p.hasTradeRouteAchievement = false;
			if (maxLength != secondLength)
				bestPlayer.hasTradeRouteAchievement = true;
		}
	}
	private int SearchForLongestPath (Building ship, int playerID) { // construct Edge-Path, initiate search
		List<Edge> path = new List<Edge>();
		
		List<Edge> edgesLookedAt = new List<Edge>();
		Queue<Corner> cornerQueue = new Queue<Corner>();
		
		cornerQueue.Enqueue (ship.closestEdge.start);
		cornerQueue.Enqueue (ship.closestEdge.end);
		
		while (cornerQueue.Count > 0) {
			Corner c  = cornerQueue.Dequeue();
			
			List<Edge> newEdges = c.adjacentEdges.FindAll(delegate(Edge obj) {
				return !edgesLookedAt.Contains(obj);
			} );
			
			foreach (Edge e in newEdges) {
				cornerQueue.Enqueue(e.OppositeCorner(c));
				
				edgesLookedAt.Add(e);
				if (e.buildings.Count > 0)
					if (e.buildings[0].ownerIndex == playerID)
						path.Add(e);
			}
		}

		return FindLongestPathInGraph (path, playerID);
	}
	private int FindLongestPathInGraph (List<Edge> path, int playerID) { // search for dead end, then start longestPathSearch
		Queue<Corner> cornerQueue = new Queue<Corner> ();
		List<Corner> cornersLookedAt = new List<Corner> ();

		if (path.Count <= 0)
			return 0;

		cornerQueue.Enqueue (path[0].start);
		cornersLookedAt.Add (path[0].start);

		while (cornerQueue.Count > 0) {
			Corner t = cornerQueue.Dequeue();

			bool hasNewOutgoing = false;
			foreach (Edge e in t.adjacentEdges) {
				if (!path.Contains(e))
					continue;

				Corner o = e.OppositeCorner(t);
				bool isAccessible = true;
				if (o.buildings.Count > 0)
					if (o.buildings[0].ownerIndex != playerID)
						isAccessible = false;

				bool notChangingType = false;
				foreach (Edge e2 in t.adjacentEdges.FindAll(delegate (Edge obj) {return obj!=e && path.Contains(obj);}))
					if (e2.buildings.Count > 0)
						if (e2.buildings[0].type == e.buildings[0].type)
							notChangingType = true;
				if (!notChangingType && t.buildings.Count <= 0)
					isAccessible = false;

				if (!cornersLookedAt.Contains(o) && isAccessible) {
					cornersLookedAt.Add(o);
					cornerQueue.Enqueue(o);
					hasNewOutgoing = true;
				}
			}

			if (!hasNewOutgoing) { // Dead end found, start search
				return GetLongestPath (path, t, playerID);
			}
		}

		return 0;
	}
	private int GetLongestPath (List<Edge> path, Corner c, int playerID) { // recursively search for longest path
		int dist, max = 0;
		c.visited = true;
		foreach (Edge e in c.adjacentEdges.FindAll(delegate (Edge obj) { return path.Contains(obj); })) {
			Corner o = e.OppositeCorner(c);
			bool isAccessible = true;
			if (o.buildings.Count > 0)
				if (o.buildings[0].ownerIndex != playerID)
					isAccessible = false;

			bool notChangingType = false;
			foreach (Edge e2 in c.adjacentEdges.FindAll(delegate (Edge obj) {return obj!=e && path.Contains(obj);}))
				if (e2.buildings.Count > 0)
					if (e2.buildings[0].type == e.buildings[0].type)
						notChangingType = true;
			if (!notChangingType && c.buildings.Count <= 0)
				isAccessible = false;

			if (!o.visited && isAccessible) {
				dist = 1+GetLongestPath(path, e.OppositeCorner(c), playerID);
				if (dist > max)
					max = dist;
			}
		}
		c.visited = false;
		return max;
	}
}

[System.Serializable]
public class BuildingPrefabEntry {
	public Building.Type type;
	public GameObject prefab;
}
[System.Serializable]
public class BuildingIntEntry {
	public Building.Type type;
	public int value;
	
	public BuildingIntEntry(Building.Type _type, int _value) {
		type = _type;
		value = _value;
	}
}
[System.Serializable]
public class BuildingCost {
	public Building.Type type;
	public List<ResourceIntEntry> cost;
	
	public BuildingCost(Building.Type _type, List<ResourceIntEntry> _cost) {
		type = _type;
		cost = _cost;
	}
}
[System.Serializable]
public class ResourceIntEntry {
	public Player.Resource type;
	public int value;

	public ResourceIntEntry(Player.Resource _type, int _value) {
		type = _type;
		value = _value;
	}
}

[System.Serializable]
public class CardIntEntry {
	public Card.Type type;
	public int value;

	public CardIntEntry (Card.Type _type, int _value) {
		type = _type;
		value = _value;
	}
}