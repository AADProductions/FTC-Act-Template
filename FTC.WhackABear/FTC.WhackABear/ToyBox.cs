using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using FTC.Core;

namespace FTC.WhackABear {
	public class ToyBox : FtcBehaviourScript
	{
		//using a class for these so we can serialize in inspector
		//and to make them easier to sort
		//a little ugly but that's ok
		[Serializable]
		public class ToyBoxPieceOdds
		{
			public ToyBoxPieceOdds () {
				NumItems = 0;
				Odds = 0f;
			}

			public ToyBoxPieceOdds (int numItems, float odds) {
				NumItems = numItems;
				Odds = odds;
			}

			[Range (1, 6)]
			public int NumItems;
			[Range (0f, 1f)]
			public float Odds;

			public override string ToString ()
			{
				return "Items: " + NumItems.ToString () + " - Odds: " + Odds.ToString ("G3");
			}
		}

		public ToyBoxPiece[] ToyBoxPieces;
		public Transform[] BearSpawnPoints;
		public GameObject[] BearPrefabs;
		public GameObject MetalBearPrefab;
		public List<ToyBoxPieceOdds> NumPiecesOddsStart;
		public List<ToyBoxPieceOdds> NumPiecesOddsEnd;
		public AnimationCurve NumPiecesBlendCurve;
		public AnimationCurve MetalBearOddsCurve;
		public AnimationCurve TimePerBearMultiplierCurve;
		public GameObject ParticleImpactPrefab;
		public float TimePerBearMultiplier = 1.25f;
		public float[] TimePerBearStart;
		public float[] TimePerBearEnd;
		public float MetalBearOdds = 0.1f;
		public float TotalDuration = 60f;
		public List<ToyBoxPieceOdds> numPiecesOdds = new List<ToyBoxPieceOdds> ();
		public List<float> timePerBear = new List<float> ();
		float nudgeDelay = 0.1f;
		float timeStarted = 0f;
		float lastTimeSpawned;
		HashSet<int> currentPieces = new HashSet<int> ();
		bool stopped = false;

		public override void OnEnable ()
		{
			TimePerBearStart = new float[6] { 0.1f, 0.1f, 0.1f, 0.09f, 0.08f, 0.07f };
			TimePerBearEnd = new float[6] { 0.1f, 0.09f, 0.08f, 0.07f, 0.06f, 0.05f };
			timePerBear = new List<float> (6) { 0f, 0f, 0f, 0f, 0f, 0f };

			NumPiecesOddsStart = new List<ToyBoxPieceOdds> ();
			NumPiecesOddsStart.Add (new ToyBoxPieceOdds (1, 0.823f));
			NumPiecesOddsStart.Add (new ToyBoxPieceOdds (2, 0.316f));
			NumPiecesOddsStart.Add (new ToyBoxPieceOdds (3, 0.037f));
			NumPiecesOddsStart.Add (new ToyBoxPieceOdds (4, 0.0f));
			NumPiecesOddsStart.Add (new ToyBoxPieceOdds (5, 0.0f));
			NumPiecesOddsStart.Add (new ToyBoxPieceOdds (6, 0.0f));

			NumPiecesOddsEnd = new List<ToyBoxPieceOdds> ();
			NumPiecesOddsEnd.Add (new ToyBoxPieceOdds (1, 0.125f));
			NumPiecesOddsEnd.Add (new ToyBoxPieceOdds (2, 0.275f));
			NumPiecesOddsEnd.Add (new ToyBoxPieceOdds (3, 0.664f));
			NumPiecesOddsEnd.Add (new ToyBoxPieceOdds (4, 0.643f));
			NumPiecesOddsEnd.Add (new ToyBoxPieceOdds (5, 0.202f));
			NumPiecesOddsEnd.Add (new ToyBoxPieceOdds (6, 0.029f));

			FtcDependencies d = gameObject.GetComponent <FtcDependencies> ();
			ToyBoxPieces = new ToyBoxPiece[] {
				d.GetDependency <GameObject> ("Piece1").GetComponent <FtcPhysicsBehaviour> ().Script as ToyBoxPiece,
				d.GetDependency <GameObject> ("Piece2").GetComponent <FtcPhysicsBehaviour> ().Script as ToyBoxPiece,
				d.GetDependency <GameObject> ("Piece3").GetComponent <FtcPhysicsBehaviour> ().Script as ToyBoxPiece,
				d.GetDependency <GameObject> ("Piece4").GetComponent <FtcPhysicsBehaviour> ().Script as ToyBoxPiece,
				d.GetDependency <GameObject> ("Piece5").GetComponent <FtcPhysicsBehaviour> ().Script as ToyBoxPiece,
				d.GetDependency <GameObject> ("Piece6").GetComponent <FtcPhysicsBehaviour> ().Script as ToyBoxPiece,
			};

			BearSpawnPoints = new Transform[] {
				d.GetDependency <GameObject> ("GummyBearPosition1").transform,
				d.GetDependency <GameObject> ("GummyBearPosition2").transform,
				d.GetDependency <GameObject> ("GummyBearPosition3").transform,
				d.GetDependency <GameObject> ("GummyBearPosition4").transform,
				d.GetDependency <GameObject> ("GummyBearPosition5").transform,
				d.GetDependency <GameObject> ("GummyBearPosition6").transform,
			};

			BearPrefabs = new GameObject[] {
				d.GetDependency <GameObject> ("GummyBearBlue"),
				d.GetDependency <GameObject> ("GummyBearGreen"),
				d.GetDependency <GameObject> ("GummyBearYellow"),
				d.GetDependency <GameObject> ("GummyBearRed"),
			};

			ParticleImpactPrefab = d.GetDependency <GameObject> ("ParticleImpactPrefab");
			MetalBearPrefab = d.GetDependency <GameObject> ("GummyBearMetal");

			FtcCurves c = gameObject.GetComponent <FtcCurves> ();
			NumPiecesBlendCurve = c.Curves [0];
			MetalBearOddsCurve = c.Curves [1];
			TimePerBearMultiplierCurve = c.Curves [2];

			//turn off our helper renderers
			foreach (Transform t in BearSpawnPoints) {
				t.gameObject.GetComponent <Renderer> ().enabled = false;
			}
			//create our odds array & time array
			numPiecesOdds = new List<ToyBoxPieceOdds> ();
			for (int i = 0; i < ToyBoxPieces.Length; i++) {
				ToyBoxPieceOdds odds = new ToyBoxPieceOdds ();
				odds.NumItems = i + 1;
				odds.Odds = 1f / i;
				numPiecesOdds.Add (odds);
			}
			//normalize the odds just in case we screwed up the values
			//they should be in order so replace the NumItems values to keep things simple
			float cumulativeOdds = 0f;
			for (int i = 0; i < NumPiecesOddsStart.Count; i++) {
				cumulativeOdds += NumPiecesOddsStart [i].Odds;
				NumPiecesOddsStart [i].NumItems = i + 1;
			}
			for (int i = 0; i < NumPiecesOddsStart.Count; i++) {
				NumPiecesOddsStart [i].Odds /= cumulativeOdds;
			}
			cumulativeOdds = 0f;
			for (int i = 0; i < NumPiecesOddsEnd.Count; i++) {
				cumulativeOdds += NumPiecesOddsEnd [i].Odds;
				NumPiecesOddsEnd [i].NumItems = i + 1;
			}
			for (int i = 0; i < NumPiecesOddsEnd.Count; i++) {
				NumPiecesOddsEnd [i].Odds /= cumulativeOdds;
			}
		}

		public void OnBearSquashed (GummyBear bear)
		{
			//remove the piece index from the list
			//so we don't play the animation later
			//currentPieces.Remove (bear.PieceIndex);
			//ToyBoxPieces [bear.PieceIndex].Play ("ToyBoxPieceDown");
		}

		public void OnBearAttached (GummyBear bear)
		{
			ToyBoxPieces [bear.PieceIndex].Bear = null;
			ToyBoxPieces [bear.PieceIndex].anim.Play ("ToyBoxPieceDown");
		}

		public void PopUpBears () {
			behaviour.StartCoroutine (PopUpBearsOverTime ());
		}

		public void Stop () {
			stopped = true;
		}

		public void Drop () {
			GameObject.Instantiate (ParticleImpactPrefab, transform.position, Quaternion.identity);
		}

		IEnumerator DoPatternSet (int iterations, float normalizedTime, float metalBearOdds)
		{
			//this gives a rhythm to each pattern set
			int numPiecesMid = GetNumberOfPieces (normalizedTime);
			int numPiecesMin = Mathf.Clamp (numPiecesMid - 1, 1, ToyBoxPieces.Length);
			float timePerIteration = numPiecesMin
			                          * GetTimePerBear (normalizedTime, numPiecesMid)
			                          * TimePerBearMultiplierCurve.Evaluate (normalizedTime)
			                          * TimePerBearMultiplier;
			//vary the time per iteration a bit so each pattern set has a consistent 'feel'
			timePerIteration *= UnityEngine.Random.Range (0.9f, 1.1f);
			int numPiecesMax = Mathf.Clamp (numPiecesMin, numPiecesMin + 1, ToyBoxPieces.Length);

			for (int x = 0; x < iterations; x++) {

				if (stopped)
					yield break;

				//see how many we'll put up each iteration
				int numPieces = UnityEngine.Random.Range (numPiecesMin, numPiecesMax + 1);//add 1 to the max, it's not a limit
				//----start a new iteration----//
				//randomly choose them
				if (numPieces == 1 && currentPieces.Count < ToyBoxPieces.Length) {
					//if we only have one
					//and we didn't show ALL of them last time
					//make sure we're picking a different one this time
					int nextIndex = UnityEngine.Random.Range (0, ToyBoxPieces.Length);
					while (currentPieces.Contains (nextIndex)) {
						nextIndex = UnityEngine.Random.Range (0, ToyBoxPieces.Length);
					}
					currentPieces.Clear ();
					currentPieces.Add (nextIndex);
				} else {
					currentPieces.Clear ();
					//add random indexes until we've filled the hashset
					while (currentPieces.Count < numPieces) {
						currentPieces.Add (UnityEngine.Random.Range (0, ToyBoxPieces.Length));
					}
				}

				//----remove pieces that still have a metal bear----//
				//(we assume there are none to begin with to keep thing simple)
				for (int i = 0; i < ToyBoxPieces.Length; i++) {
					if (ToyBoxPieces [i].Bear != null) {
						currentPieces.Remove (i);
					}
				}

				//----create the bears----//
				bool spawnedMetalBear = false;
				foreach (int pieceIndex in currentPieces) {
					//if the piece already has a metal bear
					//then don't replace it - it's stuck until they magnet it away
					if (ToyBoxPieces [pieceIndex].Bear != null) {
						continue;
					}
					GameObject newBearGo = null;
					GummyBear newBear = null;
					//don't put a metal bear on a spot where there was one last frame
					//that's to prevent shoving another metal bear onto the hovering magnet
					if (!ToyBoxPieces [pieceIndex].WasMetalLastRound &&
					     UnityEngine.Random.value < MetalBearOddsCurve.Evaluate (normalizedTime) &&
					     !spawnedMetalBear) {
						newBearGo = GameObject.Instantiate (MetalBearPrefab);
						newBear = newBearGo.GetComponent <FtcPhysicsBehaviour> ().Script as GummyBear;
						newBear.IsMetal = true;
						spawnedMetalBear = true;
					} else {
						newBearGo = GameObject.Instantiate (BearPrefabs [UnityEngine.Random.Range (0, BearPrefabs.Length)]);
						newBear = newBearGo.GetComponent <FtcPhysicsBehaviour> ().Script as GummyBear;
						newBear.IsMetal = false;
					}
					newBearGo.transform.parent = BearSpawnPoints [pieceIndex];
					newBearGo.transform.localPosition = Vector3.zero;
					newBearGo.transform.localRotation = Quaternion.Euler (0f, UnityEngine.Random.Range (-10f, 10f), 0f);
					newBear.PieceIndex = pieceIndex;
					newBear.ToyBoxParent = this;
					lastTimeSpawned = Time.time;
					ToyBoxPieces [pieceIndex].WasMetalLastRound = newBear.IsMetal;
					ToyBoxPieces [pieceIndex].anim.Play ("ToyBoxPieceUp");
					ToyBoxPieces [pieceIndex].Bear = newBear;
				}
				//play the sounds with offsets
				foreach (int pieceIndex in currentPieces) {
					//play the sound
					ToyBoxPieces [pieceIndex].audio.pitch = UnityEngine.Random.Range (0.8f, 1.2f);
					ToyBoxPieces [pieceIndex].audio.Play ();
					yield return new WaitForSeconds (UnityEngine.Random.Range (0.01f, 0.05f));
				}
				//wait until the bears are ready to be hit, then activate them
				while (Time.time < lastTimeSpawned + nudgeDelay) {
					yield return null;
				}
				foreach (ToyBoxPiece piece in ToyBoxPieces) {
					if (piece.Bear != null) {
						piece.Bear.IsReady = true;
					}
				}

				//----wait----//
				while (Time.time < lastTimeSpawned + timePerIteration) {

					if (stopped)
						yield break;

					yield return null;
				}

				//----wrap up iteration----//
				//we can't hit them any more
				foreach (ToyBoxPiece piece in ToyBoxPieces) {
					if (piece.Bear != null) {
						piece.Bear.IsReady = false;
					}
				}
				//make them all go away
				Animation waitAnim = null;
				foreach (int pieceIndex in currentPieces) {
					//TEMP - don't wait for metal bears any more
					//if (ToyBoxPieces [pieceIndex].Bear == null || !ToyBoxPieces [pieceIndex].Bear.IsMetal) {
					if (ToyBoxPieces [pieceIndex].Bear != null) {
						if (!ToyBoxPieces [pieceIndex].anim.IsPlaying ("ToyBoxPieceDown")) {
							waitAnim = ToyBoxPieces [pieceIndex].anim;
							waitAnim.Play ("ToyBoxPieceDown");
						}
					}
				}
				if (waitAnim != null) {
					//wait for animation to finish before destroying bears
					while (waitAnim ["ToyBoxPieceDown"].normalizedTime < 1f) {
						yield return null;
					}
				}
				//once we're done, destroy all the bears we've got
				foreach (ToyBoxPiece piece in ToyBoxPieces) {
					//if we're on hard difficulty, don't drop metal
					//TEMP - we're dropping them anyway
					if (piece.Bear != null) {// && !piece.Bear.IsMetal) {
						GameObject.Destroy (piece.Bear.gameObject);
						piece.Bear = null;
					}
				}
				yield return null;
			}

			yield break;
		}

		float GetTimePerBear (float normalizedTime, int numBears)
		{
			//blend our values
			float blendAmount = NumPiecesBlendCurve.Evaluate (normalizedTime);
			for (int i = 0; i < timePerBear.Count; i++) {
				timePerBear [i] = Mathf.Lerp (TimePerBearStart [i], TimePerBearEnd [i], blendAmount);
			}
			return timePerBear [numBears - 1];
		}

		int GetNumberOfPieces (float normalizedTime)
		{
			float blendAmount = NumPiecesBlendCurve.Evaluate (normalizedTime);
			int numPieces = 1;
			//blend our number of pieces curve from start to finished based on time
			for (int i = 0; i < NumPiecesOddsStart.Count; i++) {
				for (int j = 0; j < NumPiecesOddsEnd.Count; j++) {
					if (NumPiecesOddsStart [i].NumItems == NumPiecesOddsEnd [j].NumItems) {
						numPiecesOdds [i].NumItems = NumPiecesOddsStart [i].NumItems;
						numPiecesOdds [i].Odds = Mathf.Lerp (NumPiecesOddsStart [i].Odds, NumPiecesOddsEnd [j].Odds, blendAmount);
					}
				}
			}
			//sort the list so lowest probabilities are first
			numPiecesOdds.Sort (
				delegate (ToyBoxPieceOdds t1, ToyBoxPieceOdds t2) {
					return (t1.Odds.CompareTo (t2.Odds)); 
				}
			);
			//now pick a value based on the weights
			float randomValue = UnityEngine.Random.value;
			float cumulative = 0f;
			for (int i = 0; i < numPiecesOdds.Count; i++) {
				cumulative += numPiecesOdds [i].Odds;
				if (randomValue < cumulative) {
					numPieces = numPiecesOdds [i].NumItems;
					break;
				}
			}
			return numPieces;
		}

		IEnumerator PopUpBearsOverTime ()
		{
			for (int i = 0; i < ToyBoxPieces.Length; i++) {
				ToyBoxPieces [i].BonkDown ();
				yield return new WaitForSeconds (UnityEngine.Random.value * 0.1f);
			}
			timeStarted = Time.time;
			lastTimeSpawned = Time.time;
			while (Time.time - timeStarted < TotalDuration) {

				if (stopped)
					yield break;

				int iterations = UnityEngine.Random.Range (2, 5);
				float normalizedTime = ((Time.time - timeStarted) / TotalDuration);
				var task = DoPatternSet (iterations, normalizedTime, MetalBearOdds);
				while (task.MoveNext ()) {
					yield return task.Current;
				}
				yield return null;
			}
		}
	}
}