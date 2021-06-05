using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BuyButton : MonoBehaviour {
	private ButtonRegisterer br;
	private Button button;
	private GameController gc;

	[HideInInspector]
	public bool isOverridden = false;

	void Awake () {
		gc = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();
		br = GetComponent<ButtonRegisterer> ();
		button = GetComponent<Button> ();
	}

	void Update () {
		if (isOverridden)
			return;
		if (Building.isInSetupPhase) {
			switch (br.index) {
			case 0:
				button.interactable = !gc.setup_settPlaced;
				break;
			case 2:
			case 3:
				button.interactable = gc.setup_settPlaced && !gc.setup_connPlaced;
				break;
			case 1:
			case 4:
				button.interactable = false;
				break;
			}
		} else {
			switch (br.index) {
			case 0:
				button.interactable = gc.players[gc.pIndex].CanBuild (Building.Type.Settlement, gc.buildingCosts[Building.Type.Settlement]);
				break;
			case 1:
				button.interactable = gc.players[gc.pIndex].CanBuild (Building.Type.City, gc.buildingCosts[Building.Type.City]);
				break;
			case 2:
				button.interactable = gc.players[gc.pIndex].CanBuild (Building.Type.Road, gc.buildingCosts[Building.Type.Road]);
				break;
			case 3:
				button.interactable = gc.players[gc.pIndex].CanBuild (Building.Type.Ship, gc.buildingCosts[Building.Type.Ship]);
				break;
			case 4:
				button.interactable = gc.CanTakeCard ();
				break;
			}
		}
	}
}
