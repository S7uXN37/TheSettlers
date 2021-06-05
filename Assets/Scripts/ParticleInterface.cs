using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleInterface : MonoBehaviour {
	public Player.Resource type;
	public List<ResourceSpriteEntry> sprites;

	private ParticleSystem ps;
	private Dictionary<Player.Resource, Sprite> spriteDict;

	void Awake () {
		ps = GetComponent<ParticleSystem> ();
		spriteDict = new Dictionary<Player.Resource, Sprite> ();
		foreach (ResourceSpriteEntry rse in sprites)
			spriteDict.Add (rse.res, rse.spr);
	}

	public void Draw () {
		if (type == Player.Resource.Null)
			return;

		Sprite s = spriteDict [type];
		ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer> ();
		psr.material.SetTexture ("_MainTex", s.texture);
	}

	public void Activate (int times) {
		if (type == Player.Resource.Null)
			return;
		leftToEmit += times;
		StopCoroutine ("Emitting");
		StartCoroutine ("Emitting");
	}
	private int leftToEmit = 0;
	private float timer = 0f;
	IEnumerator Emitting () {
		while (leftToEmit > 0) {
			while (timer > 0f) {
				timer -= Time.deltaTime;
				yield return null;
			}
			ps.Emit (1);
			leftToEmit--;
			timer += 0.4f;
		}
	}
}

[System.Serializable]
public class ResourceSpriteEntry {
	public Player.Resource res;
	public Sprite spr;
}