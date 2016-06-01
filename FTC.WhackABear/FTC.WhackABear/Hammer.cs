using UnityEngine;
using System.Collections;
using FTC.Core;
using FTC.Interfaces;

namespace FTC.WhackABear {
	public class Hammer : FtcBehaviourScript {

		public bool IsBroken {
			get {
				return NumCracks > 2;
			}
			set {
				return;
			}
		}
		public INVRInteractable Interactable;
		public ParticleSystem Splinters;
		public int NumCracks = 0;
		public Mesh CrackedMesh;
		public Mesh BrokenMesh;
		public Transform ForwardTransform;
		public Transform BackTransform;
		public AudioClip CrackClip;
		public AudioClip BreakClip;
		Material [] hammerMats;

		public override void OnEnable ()
		{
			FtcDependencies d = gameObject.GetComponent <FtcDependencies> ();
			ForwardTransform = d.GetDependency <GameObject> ("Forward").transform;
			BackTransform = d.GetDependency <GameObject> ("Back").transform;
			Splinters = d.GetDependency <GameObject> ("SplinterParticles").GetComponent <ParticleSystem> ();
			CrackedMesh = d.GetDependency <Mesh> ("ScoreHammerCracked1");
			BrokenMesh = d.GetDependency <Mesh> ("ScoreHammerCracked2");
			BreakClip = d.GetDependency <AudioClip> ("HammerBreakClip");
			CrackClip = d.GetDependency <AudioClip> ("HammerCrackClip");

			Interactable = gameObject.GetComponent (typeof(INVRInteractable)) as INVRInteractable;

			hammerMats = gameObject.GetComponent <Renderer> ().materials;
		}

		public void SetColor (GummyBearColor newBearColor) {
			foreach (Material mat in hammerMats) {
				if (mat.name.Contains ("Color")) {
					Color c = Color.white;
					ColorSelector.GetColorFromBearColor (newBearColor, out c);
					mat.color = c;
					return;
				}
			}
		}

		public void Crack () {
			NumCracks++;
			foreach (Material mat in hammerMats) {
				mat.SetTexture ("DetailMask", null);
			}
			gameObject.GetComponent <AudioSource> ().PlayOneShot (CrackClip);
			Splinters.Play ();
			if (NumCracks < 3) {
				gameObject.GetComponent <MeshFilter> ().mesh = CrackedMesh;
			} else {
				gameObject.GetComponent <MeshFilter> ().mesh = BrokenMesh;
				gameObject.GetComponent <AudioSource> ().PlayOneShot (BreakClip);
				//get the top collision mesh and remove it
				transform.Find ("ScoreHammerCollisionMesh").gameObject.SetActive (false);
			}
		}
	}
}
