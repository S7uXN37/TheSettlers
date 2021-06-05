using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TradeManager : MonoBehaviour {
	public Transform menu;

	private bool isOpen {
		get {
			return menu.gameObject.activeSelf;
		}
		set {
			menu.gameObject.SetActive (value);
			Time.timeScale = value ? 0f : 1f;
		}
	}
	private Player self;
	private Player other;

	private Slider clay, wood, wool, wheat, ore;
	private Text nameSelf, claySelf, woodSelf, woolSelf, wheatSelf, oreSelf, nameOther, clayOther, woodOther, woolOther, wheatOther, oreOther;

	void Awake() {
		clay = menu.Find ("playerSelf/input/clay/res_slider").GetComponent<Slider> ();
		wood = menu.Find ("playerSelf/input/wood/res_slider").GetComponent<Slider> ();
		wool = menu.Find ("playerSelf/input/wool/res_slider").GetComponent<Slider> ();
		wheat = menu.Find ("playerSelf/input/wheat/res_slider").GetComponent<Slider> ();
		ore = menu.Find ("playerSelf/input/ore/res_slider").GetComponent<Slider> ();

		nameSelf = menu.Find ("playerSelf/name").GetComponent<Text> ();
		claySelf = menu.Find ("playerSelf/resources/clay/res_count").GetComponent<Text> ();
		woodSelf = menu.Find ("playerSelf/resources/wood/res_count").GetComponent<Text> ();
		woolSelf = menu.Find ("playerSelf/resources/wool/res_count").GetComponent<Text> ();
		wheatSelf = menu.Find ("playerSelf/resources/wheat/res_count").GetComponent<Text> ();
		oreSelf = menu.Find ("playerSelf/resources/ore/res_count").GetComponent<Text> ();

		nameOther = menu.Find ("playerOther/name").GetComponent<Text> ();
		clayOther = menu.Find ("playerOther/resources/clay/res_count").GetComponent<Text> ();
		woodOther = menu.Find ("playerOther/resources/wood/res_count").GetComponent<Text> ();
		woolOther = menu.Find ("playerOther/resources/wool/res_count").GetComponent<Text> ();
		wheatOther = menu.Find ("playerOther/resources/wheat/res_count").GetComponent<Text> ();
		oreOther = menu.Find ("playerOther/resources/ore/res_count").GetComponent<Text> ();

		isOpen = false;
	}

	public void Open (Player _self, Player _other) {
		isOpen = true;
		self = _self;
		other = _other;

		nameSelf.text = self.name;
		nameOther.text = other.name;

		clay.maxValue = other.resources [Player.Resource.Clay];
		wood.maxValue = other.resources [Player.Resource.Wood];
		wool.maxValue = other.resources [Player.Resource.Wool];
		wheat.maxValue = other.resources [Player.Resource.Wheat];
		ore.maxValue = other.resources [Player.Resource.Ore];

		clay.minValue = -1 * self.resources [Player.Resource.Clay];
		wood.minValue = -1 * self.resources [Player.Resource.Wood];
		wool.minValue = -1 * self.resources [Player.Resource.Wool];
		wheat.minValue = -1 * self.resources [Player.Resource.Wheat];
		ore.minValue = -1 * self.resources [Player.Resource.Ore];

		clay.value = 0f;
		wood.value = 0f;
		wool.value = 0f;
		wheat.value = 0f;
		ore.value = 0f;

		clay.interactable = clay.minValue != clay.maxValue;
		wood.interactable = wood.minValue != wood.maxValue;
		wool.interactable = wool.minValue != wool.maxValue;
		wheat.interactable = wheat.minValue != wheat.maxValue;
		ore.interactable = ore.minValue != ore.maxValue;

		claySelf.text = "" + self.resources [Player.Resource.Clay];
		woodSelf.text = "" + self.resources [Player.Resource.Wood];
		woolSelf.text = "" + self.resources [Player.Resource.Wool];
		wheatSelf.text = "" + self.resources [Player.Resource.Wheat];
		oreSelf.text = "" + self.resources [Player.Resource.Ore];

		clayOther.text = "" + other.resources [Player.Resource.Clay];
		woodOther.text = "" + other.resources [Player.Resource.Wood];
		woolOther.text = "" + other.resources [Player.Resource.Wool];
		wheatOther.text = "" + other.resources [Player.Resource.Wheat];
		oreOther.text = "" + other.resources [Player.Resource.Ore];
	}

	Slider ResourceToSlider (Player.Resource res) {
		switch (res) {
		case Player.Resource.Wood:
			return wood;
		case Player.Resource.Wool:
			return wool;
		case Player.Resource.Clay:
			return clay;
		case Player.Resource.Wheat:
			return wheat;
		case Player.Resource.Ore:
			return ore;
		default:
			return null;
		}
	}

	bool BankAccept () {
		List<Player.Resource> ttos = new List<Player.Resource> ();
		foreach (Player.Resource r in self.hasTwoToOne.Keys) {
			if (self.hasTwoToOne[r])
				ttos.Add (r);
		}
		
		Slider[] twoToOnes = new Slider[ttos.Count];
		for (int i = 0; i < twoToOnes.Length; i++) {
			twoToOnes [i] = ResourceToSlider (ttos [i]);
		}
		if (twoToOnes.Length > 0 && IsTwoToOne (twoToOnes)) // can do 2:1 => must do 2:1
			return true;
		else if (self.hasThreeToOne && IsThreeToOne () && !IsFourToOne ())  // can do 3:1 => must do 3:1
			return true;
		else {
			return IsFourToOne (); // can do nothing else => must do 4:1
		}
	}

	bool IsFourToOne () {
		int allowedOut = 0;
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * clay.value, 0, 19) / 4f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * wood.value, 0, 19) / 4f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * wool.value, 0, 19) / 4f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * wheat.value, 0, 19) / 4f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * ore.value, 0, 19) / 4f);
		
		int reqOut = 0;
		reqOut += (int)Mathf.Clamp (clay.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wood.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wool.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wheat.value, 0, 19);
		reqOut += (int)Mathf.Clamp (ore.value, 0, 19);
		
		bool isExact = true;
		isExact &= Mathf.Clamp (-1 * clay.value, 0, 19) % 4 == 0;
		isExact &= Mathf.Clamp (-1 * wood.value, 0, 19) % 4 == 0;
		isExact &= Mathf.Clamp (-1 * wool.value, 0, 19) % 4 == 0;
		isExact &= Mathf.Clamp (-1 * wheat.value, 0, 19) % 4 == 0;
		isExact &= Mathf.Clamp (-1 * ore.value, 0, 19) % 4 == 0;
		
		return reqOut == allowedOut && isExact;
	}

	bool IsThreeToOne () {
		int allowedOut = 0;
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * clay.value, 0, 19) / 3f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * wood.value, 0, 19) / 3f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * wool.value, 0, 19) / 3f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * wheat.value, 0, 19) / 3f);
		allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * ore.value, 0, 19) / 3f);
		
		int reqOut = 0;
		reqOut += (int)Mathf.Clamp (clay.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wood.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wool.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wheat.value, 0, 19);
		reqOut += (int)Mathf.Clamp (ore.value, 0, 19);
		
		bool isExact = true;
		isExact &= Mathf.Clamp (-1 * clay.value, 0, 19) % 3 == 0;
		isExact &= Mathf.Clamp (-1 * wood.value, 0, 19) % 3 == 0;
		isExact &= Mathf.Clamp (-1 * wool.value, 0, 19) % 3 == 0;
		isExact &= Mathf.Clamp (-1 * wheat.value, 0, 19) % 3 == 0;
		isExact &= Mathf.Clamp (-1 * ore.value, 0, 19) % 3 == 0;
		
		return reqOut == allowedOut && isExact;
	}

	bool IsTwoToOne (Slider[] res) {
		int allowedOut = 0;
		foreach (Slider r in res)
			allowedOut += Mathf.FloorToInt (Mathf.Clamp (-1 * r.value, 0, 19) / 2f);
		
		int reqOut = 0;
		reqOut += (int)Mathf.Clamp (clay.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wood.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wool.value, 0, 19);
		reqOut += (int)Mathf.Clamp (wheat.value, 0, 19);
		reqOut += (int)Mathf.Clamp (ore.value, 0, 19);
		
		bool isExact = true;
		foreach (Slider r in res)
			isExact &= Mathf.Clamp (-1 * r.value, 0, 19) % 2 == 0;
		
		return reqOut == allowedOut && isExact;
	}

	public void Accept() {
		if (other.name == "Bank")
		if (!BankAccept ())
			return;

		isOpen = false;

		List<ResourceIntEntry> res = new List<ResourceIntEntry> {
			new ResourceIntEntry (Player.Resource.Clay, (int) clay.value),
			new ResourceIntEntry (Player.Resource.Wood, (int) wood.value),
			new ResourceIntEntry (Player.Resource.Wool, (int) wool.value),
			new ResourceIntEntry (Player.Resource.Wheat, (int) wheat.value),
			new ResourceIntEntry (Player.Resource.Ore, (int) ore.value)
		};

		self.AddResources (res);
		other.SubtractResources (res);
	}

	public void Refuse() {
		isOpen = false;
	}
}
