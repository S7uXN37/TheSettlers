using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class SliderLink : MonoBehaviour {
	public Slider target;
	public Color pos = Color.green;
	public Color zero = Color.white;
	public Color neg = Color.red;
	public float factor = 1f;

	private Text text;

	void Awake() {
		text = GetComponent<Text> ();
	}

	void Update() {
		float val = target.value * factor;
		text.text = "" + val;
		text.color = val >= 0 ? (val == 0 ? zero : pos ) : neg;
	}
}
