using System;
using UnityEngine;
using FTC.Core;
using FTC.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace FTC.WhackABear
{
	public class WhackABearAct : FtcAct
	{
		public ToyBox[] ToyBoxes;
		public ColorSelector [] ColorSelectors;
		public LittleMagnet Magnet;
		public Hammer Hammer;
		public GameObject SelectionGrid;
		public GummyBearColor BearColor;
		public Animation DropBoxesAnimation;
		public float MetalBearOdds = 0.1f;

		public static string GetName () {
			return "Whack-A-Bear";
		}

		public static ActBonus [] GetBonusDefinitions () {
			ActBonus [] bonuses = new ActBonus [5];
			ActClassInfo actInfo = new ActClassInfo ("FTC.WhackABear.WhackABearAct", "FTC.WhackABear");
			bonuses [0] = new ActBonus ("MajorityOfBearsSquashed", "9/10 Bears Squashed", ActDifficulty.None, actInfo.ActKey, 0);
			bonuses [1] = new ActBonus ("NoWrongColorsSquashed", "No Wrong Colors Squashed", ActDifficulty.None, actInfo.ActKey, 1);
			bonuses [2] = new ActBonus ("NoMetalBearsHit", "No Metal Bears Hit", ActDifficulty.None, actInfo.ActKey, 2);
			bonuses [3] = new ActBonus ("QuickSquish", "Quick-Squisher!", ActDifficulty.None, actInfo.ActKey, 3);
			bonuses [4] = new ActBonus ("NoWrongColorsTouched", "No Wrong Colors Touched", ActDifficulty.None, actInfo.ActKey, 4);

			return bonuses;
		}

		public override void LookForObjects ()
		{
			ToyBoxes = new ToyBox [5] {
				act.GetDependency <GameObject> ("ToyBox1").GetComponent <FtcBehaviour> ().Script as ToyBox,
				act.GetDependency <GameObject> ("ToyBox2").GetComponent <FtcBehaviour> ().Script as ToyBox,
				act.GetDependency <GameObject> ("ToyBox3").GetComponent <FtcBehaviour> ().Script as ToyBox,
				act.GetDependency <GameObject> ("ToyBox4").GetComponent <FtcBehaviour> ().Script as ToyBox,
				act.GetDependency <GameObject> ("ToyBox5").GetComponent <FtcBehaviour> ().Script as ToyBox,
			};

			ColorSelectors = new ColorSelector [4] {
				act.GetDependency <GameObject> ("SelectorGreen").GetComponent <FtcBehaviour> ().Script as ColorSelector,
				act.GetDependency <GameObject> ("SelectorYellow").GetComponent <FtcBehaviour> ().Script as ColorSelector,
				act.GetDependency <GameObject> ("SelectorRed").GetComponent <FtcBehaviour> ().Script as ColorSelector,
				act.GetDependency <GameObject> ("SelectorBlue").GetComponent <FtcBehaviour> ().Script as ColorSelector,
			};

			Hammer = act.GetDependency <GameObject> ("Hammer").GetComponent <FtcBehaviour> ().Script as Hammer;
			Magnet = act.GetDependency <GameObject> ("Magnet").GetComponent <FtcObject> ().Script as LittleMagnet;
			DropBoxesAnimation = act.GetDependency <GameObject> ("DropBoxesAnimation").GetComponent <Animation> ();
			SelectionGrid = act.GetDependency <GameObject> ("SelectionGrid");
		}

		public override IEnumerator ActIntroduceOverTime ()
		{
			//reset our global gummy bear sats
			GummyBear.NumBearsSpawned = 0;
			GummyBear.NumBearsSquashed = 0;
			GummyBear.NumWrongColors = 0;
			GummyBear.NumMetalBearsHit = 0;
			GummyBear.NumWrongTouched = 0;
			GummyBear.QuickestSquashTime = Mathf.Infinity;
			GummyBear.LastTimeSquashed = 0f;

			//turn on the correct boxes
			//set the score per cheer & score loss speed
			switch (act.Difficulty) {
			case ActDifficulty.Easy:
			default:
				//
				act.TimeToComplete = 60;
				act.ScorePerCheer = 0.01225f;//substantially lower since every bear hits
				act.ScoreLoseSpeed = 0.0075f;
				ToyBoxes [1].gameObject.SetActive (false);
				ToyBoxes [2].gameObject.SetActive (false);
				ToyBoxes [3].gameObject.SetActive (false);
				ToyBoxes [4].gameObject.SetActive (false);
				break;

			case ActDifficulty.Medium:
				act.TimeToComplete = 75;
				act.ScorePerCheer = 0.0925f;
				act.ScoreLoseSpeed = 0.055f;
				ToyBoxes [0].gameObject.SetActive (false);
				ToyBoxes [1].gameObject.SetActive (false);
				ToyBoxes [2].gameObject.SetActive (false);
				break;

			case ActDifficulty.Hard:
				act.TimeToComplete = 75;
				act.ScorePerCheer = 0.0125f;
				act.ScoreLoseSpeed = 0.055f;
				ToyBoxes [3].gameObject.SetActive (false);
				ToyBoxes [4].gameObject.SetActive (false);
				break;
			}

			//turn off selection grid immediately
			SelectionGrid.SetActive (false);

			//TEMP - disabling magnet for now
			Magnet.gameObject.SetActive (false);

			//tell the spotlights to focus on the hammer
			Spotlights.Current.SetTargets (new Transform[] { Hammer.transform }, true);
			//if we're on the hard difficulty
			//wait for the player to select their hammer color
			if (act.Difficulty != ActDifficulty.Easy) {
				//activate the selection grid
				SelectionGrid.SetActive (true);

				//wait for the player to pick up the hammer
				while (!Hammer.Interactable.IsAttached) {
					yield return null;
				}

				//tell the ceiling spotlight to focus on the selectors
				Spotlights.Current.SetTargets (null, true);
				Spotlights.Current.SetCeilingTarget (ColorSelectors [0].transform.parent);
				bool pickedColor = false;
				while (!pickedColor) {
					foreach (ColorSelector cs in ColorSelectors) {
						if (cs.Selected) {
							BearColor = cs.BearColor;
							Hammer.SetColor (BearColor);
							cs.Pop ();
							pickedColor = true;
							break;
						}
					}
					yield return null;
				}

			} else {
				//otherwise just wait for player to pick up the hammer
				//wait for the player to pick up the hammer
				while (!Hammer.Interactable.IsAttached || (TutorialDialog.ShowTutorials && TutorialDialog.Current != null)) {
					yield return null;
				}
			}

			//drop the boxes
			DropBoxesAnimation.Play ();
			//then wait a half-second
			//then play impact particles
			yield return new WaitForSeconds (0.5f);
			foreach (ToyBox t in ToyBoxes) {
				if (t.gameObject.activeSelf) {
					t.TotalDuration = act.TimeToComplete;
					t.Drop ();
					//destroy the remaining color selectors
					foreach (ColorSelector c in ColorSelectors) {
						c.Pop ();
					}
				}
				DropBoxesAnimation.gameObject.GetComponent <AudioSource> ().Play ();
			}
			//let the dust settle
			yield return new WaitForSeconds (0.5f);

			//bonk down the starting bits
			foreach (ToyBox t in ToyBoxes) {
				if (t.gameObject.activeSelf) {
					yield return act.StartCoroutine (t.PopDownPieces ());
				}
			}

			//start the act!
			act.ActBegin ();
			yield break;
		}

		public override IEnumerator ActUpdateOverTime ()
		{
			//tell all the toyboxes to start
			//also point the spotlights at the active toyboxes
			List<ToyBox> activeToyboxes = new List<ToyBox> ();
			List<Transform> toyboxTransforms = new List<Transform> ();
			for (int i = 0; i < ToyBoxes.Length; i++) {
				if (ToyBoxes [i].gameObject.activeSelf) {
					activeToyboxes.Add (ToyBoxes [i]);
					toyboxTransforms.Add (ToyBoxes [i].transform);
					ToyBoxes [i].SetBearColor (BearColor);
				}
			}
			Spotlights.Current.SetCeilingTarget (null);
			Spotlights.Current.SetTargets (toyboxTransforms.ToArray (), false);

			while (act.TimeSoFar < act.TimeToComplete) {
				//choose a random toybox for popping up a bear (will always be 0 on easy)
				int randomToybox = UnityEngine.Random.Range (0, activeToyboxes.Count);
				var task = activeToyboxes [randomToybox].DoPatternSet (act.NormalizedTimeSoFar, MetalBearOdds);
				while (task.MoveNext ()) {
					yield return task.Current;
				}

				//if the hammer gets broken, the act is over
				if (Hammer.IsBroken) {
					act.Gasp ();
					act.Boo ();
					foreach (ToyBox t in ToyBoxes) {
						t.Stop ();
					}
					//turn off spotlights and focus the ceiling spotlight on the hammer
					Spotlights.Current.SetTargets (null, true);
					Spotlights.Current.SetCeilingTarget (Hammer.transform);
					yield return new WaitForSeconds (4f);
					act.ActScore ();
					yield break;
				}
				yield return null;
			}
			foreach (ToyBox tb in activeToyboxes) {
				tb.Finish ();
			}
			act.ActScore ();
			yield break;
		}

		public override void PrepareForScoring ()
		{
			base.PrepareForScoring ();

			if (GummyBear.NumWrongColors == 0) {
				act.AchieveBonus ("NoWrongColorsSquashed");
			}
			if (GummyBear.NumWrongTouched == 0) {
				act.AchieveBonus ("NoWrongColorsTouched");
			}
			if (GummyBear.NumMetalBearsHit == 0) {
				act.AchieveBonus ("NoMetalBearsHit");
			}
			if (GummyBear.QuickestSquashTime < 0.25f) {
				act.AchieveBonus ("QuickSquish");
			}
			if (((float)GummyBear.NumBearsSquashed / (float)GummyBear.NumBearsSpawned) >= 0.9f) {
				act.AchieveBonus ("MajorityOfBearsSquashed");
			}
		}
	}
}

