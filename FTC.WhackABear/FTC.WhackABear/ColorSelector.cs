using UnityEngine;
using System.Collections;
using FTC.Core;
using FTC.Interfaces;

namespace FTC.WhackABear
{
	public class ColorSelector : FtcBehaviourScript
	{
		public bool Selected = false;
		public GummyBearColor BearColor;
		public GameObject ParticleBlobsPrefab;
		public AudioClip PopClip;
		Color bearColor;

		public override void OnEnable ()
		{
			GetColorFromName (gameObject.name, out BearColor, out bearColor);
			FtcDependencies d = gameObject.GetComponent <FtcDependencies> ();
			ParticleBlobsPrefab = d.GetDependency <GameObject> ("ColorSelectionBlobsPrefab");
			PopClip = d.GetDependency <AudioClip> ("SelectorPopClip");
		}

		public void Pop () {
			GameObject blobsGo = GameObject.Instantiate (ParticleBlobsPrefab, transform.position, Quaternion.Euler (-90f, 0f, 0f)) as GameObject;
			//ParticleSystemRenderer r = (ParticleSystemRenderer)blobsGo.GetComponent <Renderer> ();
			//r.material.color = bearColor;

			AudioSource a = gameObject.GetComponent <AudioSource> ();
			a.pitch = Random.Range (0.9f, 1.1f);
			a.PlayOneShot (PopClip);
			gameObject.GetComponent <Collider> ().enabled = false;
			gameObject.GetComponent <Renderer> ().enabled = false;
		}

		public override void OnCollisionEnter (Collision collision)
		{
			if (TutorialDialog.Current != null && !TutorialDialog.Current.PlayedOnce)
				return;

			if (collision.contacts [0].otherCollider.CompareTag ("Player") && collision.relativeVelocity.magnitude > 0.1f) {
				Selected = true;
			}
		}

		public static void GetColorFromName (string name, out GummyBearColor bearColor, out Color color) {
			if (name.Contains ("Red")) {
				bearColor = GummyBearColor.Red;
			} else if (name.Contains ("Blue")) {
				bearColor = GummyBearColor.Blue;
			} else if (name.Contains ("Green")) {
				bearColor = GummyBearColor.Green;
			} else if (name.Contains ("Yellow")) {
				bearColor = GummyBearColor.Yellow;
			} else {
				//default
				bearColor = GummyBearColor.Blue;
			}
			GetColorFromBearColor (bearColor, out color);
		}

		public static void GetColorFromBearColor (GummyBearColor bearColor, out Color color) {
			switch (bearColor) {
			case GummyBearColor.Blue:
			default:
				color = new Color32 (0, 150, 255, 255);
				break;

			case GummyBearColor.Green:
				color = new Color32 (0, 255, 115, 255);
				break;

			case GummyBearColor.Red:
				color = new Color32 (255, 0, 0, 255);
				break;

			case GummyBearColor.Yellow:
				color = new Color32 (255, 185, 0, 255);
				break;
			}
		}
	}
}
