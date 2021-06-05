using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerDisplay : MonoBehaviour {
	public int playerIndex;
	public bool hasTurn {
		get {
			return turnIndicator.enabled;
		}
		set {
			turnIndicator.enabled = value;
		}
	}
	public bool isActive;
	
	private Text title, clay, wood, wool, wheat, ore, settlement, city, road, ship;
	private Image turnIndicator, bckg;
	private GameController gc;

	void Awake() {
		gc = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();
		turnIndicator = this.gameObject.transform.Find ("player_turnIndicator").GetComponent<Image> ();
		bckg = this.gameObject.transform.Find ("bckg").GetComponent<Image> ();

		isActive = true;
		hasTurn = false;
		title = gameObject.transform.Find ("player_title").GetComponent<Text> ();

		clay = gameObject.transform.Find ("player_res/res_display_clay").Find("res_count").GetComponent<Text> ();
		wood = gameObject.transform.Find ("player_res/res_display_wood").Find("res_count").GetComponent<Text> ();
		wool = gameObject.transform.Find ("player_res/res_display_wool").Find("res_count").GetComponent<Text> ();
		wheat = gameObject.transform.Find ("player_res/res_display_wheat").Find("res_count").GetComponent<Text> ();
		ore = gameObject.transform.Find ("player_res/res_display_ore").Find("res_count").GetComponent<Text> ();

		settlement = gameObject.transform.Find ("player_buildings/build_settlement").Find("build_count").GetComponent<Text> ();
		city = gameObject.transform.Find ("player_buildings/build_city").Find("build_count").GetComponent<Text> ();
		road = gameObject.transform.Find ("player_buildings/build_road").Find("build_count").GetComponent<Text> ();
		ship = gameObject.transform.Find ("player_buildings/build_ship").Find("build_count").GetComponent<Text> ();
	}

	void Update() {
		if (!isActive)
			Destroy (gameObject);

		hasTurn = gc.pIndex == playerIndex;

		Color tint = gc.players [playerIndex].tint;
		bckg.color = new Color (tint.r, tint.g, tint.b, 1f);
		title.text = "" + gc.players [playerIndex].name + ": " + gc.players [playerIndex].totalPoints;

		clay.text = "" + gc.players [playerIndex].resources [Player.Resource.Clay];
		wood.text = "" + gc.players [playerIndex].resources [Player.Resource.Wood];
		wool.text = "" + gc.players [playerIndex].resources [Player.Resource.Wool];
		wheat.text = "" + gc.players [playerIndex].resources [Player.Resource.Wheat];
		ore.text = "" + gc.players [playerIndex].resources [Player.Resource.Ore];

		settlement.text = "" + gc.players [playerIndex].availBuildings [Building.Type.Settlement];
		city.text = "" + gc.players [playerIndex].availBuildings [Building.Type.City];
		road.text = "" + gc.players [playerIndex].availBuildings [Building.Type.Road];
		ship.text = "" + gc.players [playerIndex].availBuildings [Building.Type.Ship];
	}
}
