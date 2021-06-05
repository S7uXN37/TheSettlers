using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour {
	public enum ButtonType {Trade, Buy, EndTurn, MoveShip, PlayerChoice};

	public Canvas ui;

	[HideInInspector]
	public Button[] buyButtons = new Button[5];
	private Button[] tradeButtons = new Button[5];
	private Button[] playerChoiceButtons = new Button[4];
	private Button endTurn;
	private Button moveShip;
	private PlayerDisplay[] playerDisplays = new PlayerDisplay[4];
	private GameController gc;
	private GameObject pauseMenu;
	private GameObject warningMenu;
	private GameObject diceRollMenu;
	private GameObject banditChoiceMenu;
	private GameObject playerChoiceMenu;
	private GameObject resourceChoiceMenu;
	private GameObject winScreen;
	private Text dieLeftText;
	private Text dieRightText;
	private bool paused = false;
	private bool isShowingWarning = false;

	void Awake() {
		gc = GetComponent<GameController> ();
		gc.OnNextTurn += DiceRoll;
		playerDisplays [0] = ui.gameObject.transform.Find ("players/player_1").GetComponent<PlayerDisplay> ();
		playerDisplays [1] = ui.gameObject.transform.Find ("players/player_2").GetComponent<PlayerDisplay> ();
		playerDisplays [2] = ui.gameObject.transform.Find ("players/player_3").GetComponent<PlayerDisplay> ();
		playerDisplays [3] = ui.gameObject.transform.Find ("players/player_4").GetComponent<PlayerDisplay> ();

		pauseMenu = ui.gameObject.transform.Find ("menu_pause").gameObject;
		warningMenu = ui.gameObject.transform.Find ("menu_warning").gameObject;
		banditChoiceMenu = ui.gameObject.transform.Find ("menu_banditChoice").gameObject;
		playerChoiceMenu = ui.gameObject.transform.Find ("menu_playerChoice").gameObject;
		resourceChoiceMenu = ui.gameObject.transform.Find ("menu_resourceChoice").gameObject;
		winScreen = ui.gameObject.transform.Find ("menu_win").gameObject;
		diceRollMenu = ui.gameObject.transform.Find ("menu_diceRoll").gameObject;
		dieLeftText = diceRollMenu.gameObject.transform.Find ("die_left").GetComponent<Text> ();
		dieRightText = diceRollMenu.gameObject.transform.Find ("die_right").GetComponent<Text> ();

		DisableButtons (false, false, true, true);
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.Escape)) {
			if (gc.trading.menu.gameObject.activeSelf)
				gc.trading.Refuse ();
			else if (isShowingWarning)
				CloseWarning ();
			else if (paused)
				ClosePauseMenu ();
			else if (gc.movingShip)
				gc.RevertShipMove ();
			else if (gc.hoverInstance != null)
				Destroy (gc.hoverInstance.gameObject);
			else
				OpenPauseMenu ();
		}
	}

	public void Register (Button button, ButtonType type, int index) {
		switch (type) {
		case ButtonType.Buy:
			buyButtons [index] = button;
			break;
		case ButtonType.Trade:
			tradeButtons [index] = button;
			break;
		case ButtonType.EndTurn:
			endTurn = button;
			break;
		case ButtonType.MoveShip:
			moveShip = button;
			break;
		case ButtonType.PlayerChoice:
			playerChoiceButtons [index] = button;
			if (index < gc.players.Length) {
				playerChoiceButtons [index].transform.Find ("Text").GetComponent<Text> ().text = gc.players [index].name;
				playerChoiceButtons [index].GetComponent<Image> ().color = gc.players [index].tint;
			}
			break;
		}
	}

	public void Init (int playerCount) {
		switch (playerCount) {
		case 2:
			Destroy(tradeButtons [2].gameObject);
			tradeButtons [2] = null;
			playerDisplays [1].isActive = false;
			tradeButtons [4].gameObject.GetComponent<RectTransform> ().anchoredPosition += new Vector2 (0f, 20f);
			goto case 3;
		case 3:
			Destroy(tradeButtons [3].gameObject);
			tradeButtons [3] = null;
			playerDisplays [0].isActive = false;
			tradeButtons [4].gameObject.GetComponent<RectTransform> ().anchoredPosition += new Vector2 (0f, 20f);
			break;
		default:
			break;
		}


		if (playerCount == 2) {
			playerDisplays[2].playerIndex = 0;
			playerDisplays[3].playerIndex = 1;
		} else if (playerCount == 3) {
			playerDisplays[1].playerIndex = 0;
			playerDisplays[2].playerIndex = 1;
			playerDisplays[3].playerIndex = 2;
		}

		float interval = 500 / playerCount;
		float offset = 52 / playerCount + 56 + interval / 2f;
		offset /= 2f;
		foreach (PlayerDisplay pd in playerDisplays) {
			if (pd == null)
				continue;
			pd.gameObject.GetComponent<RectTransform> ().anchoredPosition = new Vector2 (offset + pd.playerIndex * interval, 0f);
		}

		gc.OnNextTurn += GreyOutTradeButtons;
		GreyOutTradeButtons ();
	}

	public void GreyOutTradeButtons () {
		foreach (Button b in tradeButtons) {
			if (b != null) {
				int index = b.GetComponent<ButtonRegisterer> ().index;
				b.interactable = index != gc.pIndex;
				if (index != 4)
					b.transform.Find ("Text").GetComponent<Text> ().text = gc.players [index].name;
			}
		}
	}

	public void BuySettlement () {
		gc.Build (Building.Type.Settlement);
	}
	public void BuyCity () {
		gc.Build (Building.Type.City);
	}
	public void BuyRoad () {
		gc.Build (Building.Type.Road);
	}
	public void BuyShip () {
		gc.Build (Building.Type.Ship);
	}
	public void BuyDevelopment () {
		gc.TakeCard ();
	}

	public void OpenPauseMenu () {
		pauseMenu.SetActive (true);
		Time.timeScale = 0f;
		paused = true;
	}
	public void ClosePauseMenu () {
		pauseMenu.SetActive (false);
		Time.timeScale = 1f;
		paused = false;
	}
	public void SaveGame () {
		// maybe later (serialization and deserialization)
		print ("saving the game not yet implemented");
	}
	private System.Action onContinueAnyway;
	public void ToMainMenu () {
		if (!gc.isGameWon)
			ShowWarning ();
		onContinueAnyway = LoadMainMenu;
	}
	void LoadMainMenu () {
		Application.LoadLevel ("MainMenu");
	}
	public void ExitGame () {
		if (gc.isGameWon) {
			Quit ();
		} else {
			ShowWarning ();
			onContinueAnyway = Quit;
		}
	}
	void Quit ()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit ();
#endif
	}
	void ShowWarning () {
		warningMenu.SetActive (true);
		isShowingWarning = true;
	}
	public void CloseWarning () {
		warningMenu.SetActive (false);
		isShowingWarning = false;
	}
	public void ContinueAnyway () {
		CloseWarning ();

		if (onContinueAnyway != null)
			onContinueAnyway ();

		onContinueAnyway = null;
	}

	public void DiceRoll () {
		if (Building.isInSetupPhase)
			return;
		diceRollMenu.SetActive (true);
		StartCoroutine (DiceRolling ());
	}
	IEnumerator DiceRolling () {
		float x = 0f;
		float speedL = 2f;
		float speedR = 2f;
		float aL = 0.5f + Random.Range (-0.2f, 0.2f);
		float aR = 0.5f + Random.Range (-0.2f, 0.2f);

		int rollL = 1;
		float timerL = 0f;
		int rollR = 1;
		float timerR = 0f;

		while (speedL > 0.1f || speedR > 0.1f) {
			x += Time.deltaTime;
			speedL = 8 * aL * aL * aL / (x * x + 4 * aL * aL);
			speedR = 8 * aR * aR * aR / (x * x + 4 * aR * aR);
			timerL += speedL;
			timerR += speedR;

			if (timerL > 1f) {
				timerL = 0f;
				rollL = Random.Range (1, 7);
				dieLeftText.text = "" + rollL;
			}
			if (timerR > 1f) {
				timerR = 0f;
				rollR = Random.Range (1, 7);
				dieRightText.text = "" + rollR;
			}

			yield return null;
		}

		dieRightText.color = dieLeftText.color = Color.green;

		yield return new WaitForSeconds (2f);

		gc.DiceRolled (rollL + rollR);
		diceRollMenu.SetActive (false);

		dieRightText.color = dieLeftText.color = Color.white;
	}

	public void ShowBanditChoice () {
		banditChoiceMenu.SetActive (true);
	}
	public void HideBanditChoice () {
		banditChoiceMenu.SetActive (false);
	}

	public void DisableButtons (bool buy, bool trade, bool turn, bool move) {
		if (buy) {
			foreach (Button b in buyButtons) {
				b.GetComponent<BuyButton> ().isOverridden = true;
				b.interactable = false;
			}
		}
		if (trade) {
			foreach (Button b in tradeButtons) {
				if (b != null)
					b.interactable = false;
			}
		}
		if (turn)
			endTurn.interactable = false;
		if (move)
			moveShip.interactable = false;
	}
	public void EnableButtons (bool buy, bool trade, bool turn, bool move) {
		if (buy) {
			foreach (Button b in buyButtons) {
				b.GetComponent<BuyButton> ().isOverridden = false;
			}
		}
		if (trade) {
			GreyOutTradeButtons ();
		}
		if (turn)
			endTurn.interactable = true;
		if (move)
			moveShip.interactable = true;
	}

	private System.Action<int, bool, int> playerChosenAction;
	private bool b1;
	private int i2;
	public void ShowPlayerChoice (List<int> indicies, System.Action<int, bool, int> onChoose, bool _b, int _i) {
		b1 = _b;
		i2 = _i;
		playerChosenAction = onChoose;

		playerChoiceMenu.SetActive (true);

		int choices = indicies.Count;
		float interval = 480f / choices;
		float offset = -240f;

		int n = 0;
		for (int i = 0; i < playerChoiceButtons.Length; i++) {
			if (indicies.Exists (delegate(int obj) {return obj == i;})) {
				float x = interval * (n + 0.5f) + offset;
				playerChoiceButtons [i].gameObject.SetActive (true);
				playerChoiceButtons [i].GetComponent<RectTransform> ().anchoredPosition = new Vector2 (x, 0f);
				n++;
			} else {
				playerChoiceButtons [i].gameObject.SetActive (false);
			}
		}
	}
	public void OnPlayerChosen (int index) {
		playerChoiceMenu.SetActive (false);
		playerChosenAction (index, b1, i2);
	}

	public void OverrideBuyButtons (bool sett, bool city, bool road, bool ship, bool dev) {
		bool[] states = new bool[] {sett, city, road, ship, dev};
		for (int i = 0; i < buyButtons.Length; i++) {
			Button b = buyButtons [i];
			b.GetComponent<BuyButton> ().isOverridden = true;
			b.interactable = states [i];
		}
	}

	private System.Action<Player.Resource> onResChosen;
	public void ShowResourceChoice (System.Action<Player.Resource> onChosen, string title) {
		onResChosen = onChosen;
		resourceChoiceMenu.SetActive (true);
		resourceChoiceMenu.transform.Find ("title").GetComponent<Text> ().text = title;
	}
	public void OnResourceChosen (int index) {
		resourceChoiceMenu.SetActive (false);
		Player.Resource r = Player.Resource.Null;
		switch (index) {
		case 0:
			r = Player.Resource.Clay;
			break;
		case 1:
			r = Player.Resource.Wood;
			break;
		case 2:
			r = Player.Resource.Wool;
			break;
		case 3:
			r = Player.Resource.Wheat;
			break;
		case 4:
			r = Player.Resource.Ore;
			break;
		}
		onResChosen (r);
	}

	public void ShowWinScreen (Player winner) {
		winScreen.SetActive (true);
		string colorHex = Util.ColorToHex (winner.tint);
		winScreen.transform.Find ("title").GetComponent<Text> ().text = "<color=" + colorHex + ">" + winner.name + "</color> won the game!";
	}

	public void AnimatePay (List<ResourceIntEntry> trade, int player) {
		StartCoroutine (AnimationPay (trade, player));
	}
	IEnumerator AnimationPay (List<ResourceIntEntry> trade, int player) {
		yield return null;
	}
}