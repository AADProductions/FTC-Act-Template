using UnityEngine;
using System.Collections;

namespace FTC.WhackABear {
	public class ToyBoxPiece : MonoBehaviour {
		public bool BonkedDown = false;
		public bool WasMetalLastRound = false;
		public GummyBear Bear;
		public AudioSource audio;
		public Animation anim;

		void Start () {
			audio = GetComponent <AudioSource> ();
			anim = GetComponent <Animation> ();
		}

		void OnCollisionEnter (Collision collision) {
			if (BonkedDown)
				return;
			
			if (collision.collider.CompareTag ("Player")) {
				BonkedDown = true;
				audio.pitch = Random.Range (0.9f, 1.1f);
				audio.Play ();
				anim.Play ("ToyBoxPieceBonk");
			}
		}
	}
}