using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ToyBox : MonoBehaviour {

	public Animation [] ToyBoxPieces;
	public Transform [] BearSpawnPoints;
	public GameObject [] BearPrefabs;
	public GameObject MetalBearPrefab;
	public AnimationCurve NumPiecesCurve;
	public AnimationCurve MaxPiecesCurve;
	public float SpawnDelay = 2.5f;
	public float MetalBearOdds = 0.1f;
	public float TotalDuration = 60f;
	public Vector3 RandomNudge;
	float nudgeDelay = 0.1f;
	float timeStarted = 0f;
	List<GummyBear> currentBears = new List<GummyBear> ();
	HashSet<int> currentPieces = new HashSet<int> ();

	void Start () {
		timeStarted = Time.time;
		foreach (Transform t in BearSpawnPoints) {
			t.gameObject.GetComponent <Renderer> ().enabled = false;
		}
		StartCoroutine (PopUpBearsOverTime ());
	}

	public void OnBearSquashed (GummyBear bear) {
		//remove the piece index from the list
		//so we don't play the animation later
		//currentPieces.Remove (bear.PieceIndex);
		//ToyBoxPieces [bear.PieceIndex].Play ("ToyBoxPieceDown");
	}

	IEnumerator PopUpBearsOverTime () {
		float lastTimeSpawned = Time.time;
		while (true) {
			//wait for the remainder of time left 
			yield return new WaitForSeconds (SpawnDelay * currentPieces.Count - (Time.time - lastTimeSpawned));
			//we can't hit them any more
			foreach (GummyBear bear in currentBears) {
				bear.IsReady = false;
			}
			//make them all go away
			Animation waitAnim = null;
			foreach (int pieceIndex in currentPieces) {
				ToyBoxPieces [pieceIndex].Play ("ToyBoxPieceDown");
				if (waitAnim == null) {
					waitAnim = ToyBoxPieces [pieceIndex];
				}
			}
			//wait for animation to finish before destroying bears
			if (waitAnim != null) {
				while (waitAnim.isPlaying) {
					yield return null;
				}
			}
			//once we're done, destroy all the bears we've got
			foreach (GummyBear bear in currentBears) {
				GameObject.Destroy (bear.gameObject);
			}
			currentBears.Clear ();

			//start fresh!

			//see how many we'll put up this time
			float normalizedTime = (Time.time - timeStarted) / TotalDuration;
			float maxPieces = MaxPiecesCurve.Evaluate (normalizedTime + (Random.value * 0.25f));
			int numberOfPieces = Mathf.FloorToInt (NumPiecesCurve.Evaluate (maxPieces));
			//randomly choose them
			if (numberOfPieces == 1 && currentPieces.Count < ToyBoxPieces.Length) {
				//if we only have one
				//and we didn't show ALL of them last time
				//make sure we're picking a different one
				int nextIndex = Random.Range (0, ToyBoxPieces.Length);
				while (currentPieces.Contains (nextIndex)) {
					nextIndex = Random.Range (0, ToyBoxPieces.Length);
				}
				currentPieces.Clear ();
				currentPieces.Add (nextIndex);
			} else {
				currentPieces.Clear ();
				while (currentPieces.Count < numberOfPieces) {
					currentPieces.Add (Random.Range (0, ToyBoxPieces.Length));
				}
			}
			bool spawnedMetalBear = false;
			foreach (int pieceIndex in currentPieces) {
				GameObject newBearGo = null;
				GummyBear newBear = null;
				if (Random.value < MetalBearOdds && !spawnedMetalBear) {
					newBearGo = GameObject.Instantiate (MetalBearPrefab);
					newBear = newBearGo.GetComponent <GummyBear> ();
					newBear.IsMetal = true;
					spawnedMetalBear = true;
				} else {
					newBearGo = GameObject.Instantiate (BearPrefabs [Random.Range (0, BearPrefabs.Length)]);
					newBear = newBearGo.GetComponent <GummyBear> ();
					newBear.IsMetal = false;
				}
				newBearGo.transform.parent = BearSpawnPoints [pieceIndex];
				newBearGo.transform.localPosition = Vector3.zero;
				newBearGo.transform.localRotation = Quaternion.Euler (0f, Random.Range (-10f, 10f), 0f);
				newBear.PieceIndex = pieceIndex;
				newBear.ToyBoxParent = this;
				lastTimeSpawned = Time.time;
				ToyBoxPieces [pieceIndex].Play ("ToyBoxPieceUp");
				currentBears.Add (newBear);
			}
			//play the sounds with offsets
			foreach (int pieceIndex in currentPieces) {
				//play the sound
				AudioSource a = ToyBoxPieces [pieceIndex].gameObject.GetComponent <AudioSource> ();
				a.pitch = Random.Range (0.8f, 1.2f);
				a.Play ();
				yield return new WaitForSeconds (Random.Range (0.01f, 0.05f));
			}
			//give the bear just a little nudge
			while (Time.time < lastTimeSpawned + nudgeDelay) {
				yield return null;
			}
			foreach (GummyBear bear in currentBears) {
				bear.IsReady = true;
				//bear.GetComponent <Rigidbody> ().AddTorque (RandomNudge, ForceMode.Impulse);
			}
		}
	}
}
