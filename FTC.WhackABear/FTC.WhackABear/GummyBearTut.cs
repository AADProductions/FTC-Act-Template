using FTC.Core;
using UnityEngine;

namespace FTC.WhackABear
{
	public class GummyBearTut : FtcBehaviourScript
	{
		public override void OnEnable ()
		{
			transform.localScale = Vector3.one * 0.01f;
			//play the wiggly animation and give it a random offset
			Animation anim = gameObject.GetComponent <Animation> ();
			if (anim != null) {
				anim.Play ("GummyBearWiggle");
				anim ["GummyBearWiggle"].normalizedTime = Random.value;
			}
			//play the giggle animation and give it a random offset
			AudioSource audio = gameObject.GetComponent <AudioSource> ();
			if (audio != null) {
				audio.pitch = Random.Range (0.9f, 1.1f);
				audio.timeSamples = Random.Range (0, audio.clip.samples);
				audio.Play ();
			}
		}

		public override void Start ()
		{
			gameObject.SetActive (false);
		}

		public override void Update ()
		{
			transform.localScale = Vector3.Lerp (transform.localScale, Vector3.one, Time.deltaTime * 10f);
		}
	}
}

