using UnityEngine;
using System.Collections;

namespace FTC {
	public class DestroyTimedOnGrip : MonoBehaviour {

		float startTime;
		public float DestroyTime = 5f;
		bool gripped = false;
		bool released = false;

		void OnPlayerGrip (GameObject hand) {
			gripped = true;
		}

		void OnPlayerRelease (GameObject hand) {
			released = true;
			startTime = Time.time;
		}

		void Update () {
			if (gripped && released && Time.time > startTime + DestroyTime) {
				GameObject.Destroy (gameObject);
				enabled = false;
			}
		}
	}
}