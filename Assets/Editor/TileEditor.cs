using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Tile))]
public class TileEditor : Editor {
	public override void OnInspectorGUI ()
	{
		if (DrawDefaultInspector ()) {
			UpdateTile();
		}
		
		if (GUILayout.Button ("Update tile")) {
			UpdateTile();
		}
	}

	void UpdateTile() {
		if (!Application.isPlaying)
			return;
		Tile t = target as Tile;
		t.Draw ();
	}
}
