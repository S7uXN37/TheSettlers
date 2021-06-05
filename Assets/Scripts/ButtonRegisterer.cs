using UnityEngine;
using System.Collections;

public class ButtonRegisterer : MonoBehaviour {
	public UIManager.ButtonType type;
	public int index;

	private UnityEngine.UI.Button self;

	void Awake () {
		self = GetComponent<UnityEngine.UI.Button> ();
		GameObject.FindGameObjectWithTag ("GameController").GetComponent<UIManager> ().Register (self, type, index);
	}
}
