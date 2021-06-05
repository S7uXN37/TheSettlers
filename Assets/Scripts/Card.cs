using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour {
	public enum Type {Wood, Ore, Wheat, Clay, Wool,
		Cathedral, Library, Parliament, University, Market,
		Monopoly, Invention, Road, Knight,
		Null};
	public Type type;

	private Material cardMat;

	void Awake() {
		cardMat = GetComponent<MeshRenderer> ().material;
		Texture cardTex = Resources.Load ("texture_card_" + type.ToString().ToLower()) as Texture;
		cardMat.SetTexture ("_MainTex", cardTex);
	}
}