using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MapGenerator))]
public class MainMenuController : MonoBehaviour {
	public static int seed = 0;
	public static int width = 7;
	public static int height = 5;
	public static List<string> names;
	public static bool used = false;

	private MapGenerator map;
	public InputField inpX, inpY, inpSeed;
	public Text fairness;
	public InputField[] playerNames;

	void Awake () {
		map = GetComponent<MapGenerator> ();
	}

	public void ChangeSeed (int delta)
	{
		map.currentMap.seed += delta;
		seed = map.currentMap.seed;
		map.NewMap ();
		inpSeed.text = "" + seed;
		UpdateFairness ();
	}

	public void LoadGame ()
	{
		// maybe later (serialization and deserialization)
		print ("loading the game not yet implemented");
	}

	public void Play () {
		names = new List<string> ();
		for (int i = 0; i < playerNames.Length; i++) {
			if (playerNames [i].text != "")
				names.Add (playerNames [i].text);
		}
		while (names.Count < 2) {
			names.Add ("Hansruedi-Gabriel");
		}
		used = true;
		Application.LoadLevel ("Main");
	}

	public void Exit ()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit ();
#endif
	}

	public void InputConfirm (int index) {
		switch (index) {
		case 0:
			int newX;
			if (!System.Int32.TryParse (inpX.text, out newX))
				return;
			map.currentMap.width = newX;
			width = newX;
			break;
		case 1:
			int newY;
			if (!System.Int32.TryParse (inpY.text, out newY))
				return;
			if (newY % 2 == 0)
				return;
			map.currentMap.height = newY;
			height = newY;
			break;
		case 2:
			int newSeed;
			if (!System.Int32.TryParse (inpSeed.text, out newSeed))
			    return;
			map.currentMap.seed = newSeed;
			seed = newSeed;
			break;
		}
		map.NewMap ();
		UpdateFairness ();
	}

	void UpdateFairness () {
		fairness.text = "Fairness: " + map.GetFairnessRating ();
	}
}
