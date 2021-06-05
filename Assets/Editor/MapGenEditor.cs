using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(MapGenerator))]
public class MapGenEditor : Editor {
	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector ();
		

		if (Application.isPlaying) {
			if (GUILayout.Button ("Generate!")) {
				MapGenerator mg = target as MapGenerator;
				mg.NewMap (mg.currentMap);
			}

			if (GUILayout.Button ("Generate random!")) {
				MapGenerator mg = target as MapGenerator;
				mg.currentMap.seed = Random.Range (0,1000);
				mg.NewMap (mg.currentMap);
			}
		}
	}
}
