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

		public static string GetName () {
			return "Whack-A-Bear";
		}

		public static ActBonus [] GetBonusDefinitions () {
			ActBonus [] bonuses = new ActBonus [5];
			ActClassInfo actInfo = new ActClassInfo ("FTC.WhackABear.WhackABearAct", "FTC.WhackABear");
			bonuses [0] = new ActBonus ("Bonus 1", "Bonus 1 Description", ActDifficulty.None, actInfo.ActKey, 0);
			bonuses [1] = new ActBonus ("Bonus 2", "Bonus 2 Description", ActDifficulty.None, actInfo.ActKey, 1);
			bonuses [2] = new ActBonus ("Bonus 3", "Bonus 3 Description", ActDifficulty.None, actInfo.ActKey, 2);
			bonuses [3] = new ActBonus ("Bonus 4", "Bonus 4 Description", ActDifficulty.None, actInfo.ActKey, 3);
			bonuses [4] = new ActBonus ("Bonus 5", "Bonus 5 Description", ActDifficulty.None, actInfo.ActKey, 4);

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
			//turn off toyboxes and selection grid immediately
			foreach (ToyBox t in ToyBoxes) {
				t.gameObject.SetActive (false);
				SelectionGrid.SetActive (false);
			}

			//TEMP - disabling magnet for now
			Magnet.gameObject.SetActive (false);

			//tell the spotlights to focus on the hammer
			Spotlights.Current.SetTargets (new Transform[] { Hammer.transform }, true);
			//if we're on the hard difficulty
			//wait for the player to select their hammer color
			if (act.Difficulty == ActDifficulty.Hard) {
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
							pickedColor = true;
							break;
						}
					}
					yield return null;
				}

				//make the selectors pop
				//tell the hammer what color we're using
				foreach (ColorSelector cs in ColorSelectors) {
					cs.Pop ();
					Hammer.SetColor (BearColor);
					yield return null;
				}
			} else {
				//otherwise just wait for player to pick up the hammer
				//wait for the player to pick up the hammer
				while (!Hammer.Interactable.IsAttached) {
					yield return null;
				}
			}

			//turn on the correct boxes
			switch (act.Difficulty) {
			case ActDifficulty.Easy:
			default:
				ToyBoxes [0].gameObject.SetActive (true);
				break;

			case ActDifficulty.Medium:
				ToyBoxes [3].gameObject.SetActive (true);
				ToyBoxes [4].gameObject.SetActive (true);
				break;

			case ActDifficulty.Hard:
				ToyBoxes [0].gameObject.SetActive (true);
				ToyBoxes [1].gameObject.SetActive (true);
				ToyBoxes [2].gameObject.SetActive (true);
				break;
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
				}
				DropBoxesAnimation.gameObject.GetComponent <AudioSource> ().Play ();
			}
			//let the dust settle
			yield return new WaitForSeconds (0.5f);

			//tell all the toyboxes to start
			//also point the spotlights at the active toyboxes
			List<Transform> activeToyboxes = new List<Transform> ();
			for (int i = 0; i < ToyBoxes.Length; i++) {
				if (ToyBoxes [i].gameObject.activeSelf) {
					activeToyboxes.Add (ToyBoxes [i].transform);
					ToyBoxes [i].PopUpBears ();
				}
			}
			Spotlights.Current.SetTargets (activeToyboxes.ToArray (), false);

			//start the act!
			act.ActBegin ();
			yield break;
		}

		public override IEnumerator ActUpdateOverTime ()
		{
			while (act.TimeSoFar < act.TimeToComplete) {
				//if the hammer gets broken, the act is over
				if (Hammer.IsBroken) {
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
			act.ActScore ();
			yield break;
		}
	}
}

