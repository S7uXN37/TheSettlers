using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Util : MonoBehaviour {
	public static T[] ShuffledArray<T>(T[] array, System.Random pseudo) {
		T[] a = array.Clone () as T[];

		for (int i = 0; i < a.Length - 1; i++) {
			int j = pseudo.Next (i, a.Length);
			T tmp = a[i];
			a[i] = a[j];
			a[j] = tmp;
		}

		return a;
	}
	public static T[] ShuffledList<T>(List<T> list, System.Random pseudo) {
		return ShuffledArray<T> (list.ToArray (), pseudo);
	}
	public static T[] ShuffledArray<T>(T[] array) {
		T[] a = array.Clone () as T[];
		
		for (int i = 0; i < a.Length - 1; i++) {
			int j = Random.Range (i, a.Length);
			T tmp = a[i];
			a[i] = a[j];
			a[j] = tmp;
		}
		
		return a;
	}
	public static T[] ShuffledList<T>(List<T> list) {
		return ShuffledArray<T> (list.ToArray ());
	}
	public static string ColorToHex (Color32 c) {
		string hex = c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
		return "#" + hex;
	}
}