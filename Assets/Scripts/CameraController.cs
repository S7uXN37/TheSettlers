using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraController : MonoBehaviour {
	public float sizeIncreasePercent = 0.1f;
	public Rect cameraProj;

	private Camera cam;
	public MapGenerator map;

	void Awake() {
		cam = GetComponent<Camera> ();
		map.MapGenerated += UpdateCamSize;

		Resize ();
	}

	public void Resize () {
		cam.rect = cameraProj;
	}

	public void UpdateCamSize() {
		Vector3 tileHalfSize = map.GetTileSize ();
		float width = tileHalfSize.y * (map.currentMap.width + 0.5f);
		float height = (tileHalfSize.x + tileHalfSize.z) * map.currentMap.height / 2f;
		float orthSize = Mathf.Max (width / cam.aspect, height);
		orthSize *= 1 + sizeIncreasePercent;
		cam.orthographicSize = orthSize;
	}
}
