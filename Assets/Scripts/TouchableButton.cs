using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using NewtonVR;

namespace FTC
{
	public class TouchableButton : MonoBehaviour {

		float onEnableTime;
		float lastPushTime;
		float minPushTime = 0.25f;

		void OnEnable () {
			onEnableTime = Time.time;
		}

		void OnTriggerEnter (Collider other) {
			if (Time.time < lastPushTime + minPushTime) {
				//Debug.Log ("Before min push time");
				return;
			}

			if (Time.time < onEnableTime + minPushTime) {
				//Debug.Log ("Before min enable time");
				return;
			}

			if (other.CompareTag ("Player")) {
				NVRHand h = other.GetComponent <NVRHand> ();
				if (h != null) {
					h.Controller.TriggerHapticPulse (500);
					lastPushTime = Time.time;
					GetComponent <Button> ().onClick.Invoke ();
				}
			}
		}
	}
}