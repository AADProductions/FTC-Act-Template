using UnityEngine;
using System.Collections;
using FTC.Core;

namespace FTC.WhackABear {
	public class ToyBoxPiece : FtcBehaviourScript
	{
		public bool BonkedDown = false;
		public bool WasMetalLastRound = false;
		public GummyBear Bear;
		public AudioSource audio;
		public Animation anim;

		public override void OnEnable ()
		{
			audio = gameObject.GetComponent <AudioSource> ();
			anim = gameObject.GetComponent <Animation> ();
		}

		public void BonkDown () {
			BonkedDown = true;
			audio.pitch = Random.Range (0.9f, 1.1f);
			audio.Play ();
			anim.Play ("ToyBoxPieceBonk");
		}
	}
}