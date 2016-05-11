using UnityEngine;
using System.Collections;

namespace FTC {
	public class DestroyTimed : MonoBehaviour {
		
		float startTime;
		public float DestroyTime = 5f;

		void Start () {
			startTime = Time.time;
		}

		void Update () {
			if (Time.time > startTime + DestroyTime) {
				GameObject.Destroy (gameObject);
				enabled = false;
			}
		}
	}
}