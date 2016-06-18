using UnityEngine;
using System.Collections;

namespace FTC
{
	public class Noisemaker : MonoBehaviour
	{
		static float minTimeBetweenCollisions = 0.2f;
		static float maxVolume = 0.75f;
		public bool PlayAtPoint = false;
		public AudioClip CollisionNoise;
		public float MinMagnitude = 2f;
		public float VolumeMultiplier = 0.5f;
		float lastCollisionTime;
		public Collider [] IgnoreCollisions;

		void OnCollisionEnter (Collision collision)
		{
			if (IgnoreCollisions.Length > 0) {
				for (int i = 0; i < IgnoreCollisions.Length; i++) {
					if (collision.collider == IgnoreCollisions [i]) {
						return;
					}
				}
			}
			/*if (!Experiment.Current.ExperimentSpawned)
				return;*/

			if (Time.time > lastCollisionTime + minTimeBetweenCollisions) {
				float magnitude = collision.relativeVelocity.magnitude;
				lastCollisionTime = Time.time;
				if (magnitude > MinMagnitude) {
					if (PlayAtPoint) {
						AudioSource.PlayClipAtPoint (CollisionNoise, collision.contacts [0].point, Mathf.Clamp (magnitude * VolumeMultiplier, 0f, maxVolume));
					} else {
						gameObject.GetComponent <AudioSource> ().PlayOneShot (CollisionNoise, Mathf.Clamp (magnitude * VolumeMultiplier, 0f, maxVolume));
					}
				}
			}
		}
	}
}