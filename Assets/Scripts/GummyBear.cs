using UnityEngine;
using System.Collections;
using FTC.Interfaces;
using FTC.Core;

namespace FTC.WhackABear
{
	public class GummyBear : MonoBehaviour
	{
		public bool IsMetal = false;
		public bool IsReady = false;
		public bool Squashed = false;
		public bool Attached = false;
		public float MinCollisionMagnitude = 0.5f;
		public float MinCollisionDot = 0.5f;
		public Color BearColor;
		public Mesh SquashedMesh;
		public ToyBox ToyBoxParent;
		public AudioClip MetalBangClip;
		public AudioClip SquashClip;
		public AudioClip SqueezeClip;
		public GameObject ParticleBlobsPrefab;
		public int PieceIndex;
		float nextHapticPulseTime;
		ushort nextHapticPulseScale;
		Vector3 squashScale = Vector3.one;
		INVRInteractable interactable;
		Rigidbody rb;

		void Start () {
			rb = GetComponent <Rigidbody> ();

			if (!IsMetal) {
				transform.localScale = new Vector3 (1f, 0.8f, 1f);
			}
		}

		public void OnCollisionEnter (Collision collision)
		{
			if (Squashed || Attached)
				return;

			if (IsMetal && collision.collider.CompareTag ("End")) {
				//we've bonked into another bear
				if (IsReady) {
					Attach (collision.collider.attachedRigidbody);
					return;
				}
			}

			bool canSquash = !IsMetal;

			if (!collision.collider.CompareTag ("Player")) {
				canSquash = false;
			}

			interactable = (INVRInteractable)collision.collider.attachedRigidbody.GetComponent (typeof(INVRInteractable));
			if (interactable == null) {
				canSquash = false;
			}

			if (IsMetal) {
				Clang ();
				if (interactable != null && interactable.IsAttached) {
					nextHapticPulseScale += 1200;
				}
				return;
			}

			float collisionMagnitude = collision.relativeVelocity.magnitude;
			float squashAmount = Mathf.Clamp01 (collisionMagnitude / MinCollisionMagnitude);
			nextHapticPulseScale += (ushort)Mathf.FloorToInt (1200 * (1f - squashAmount));
			float dot = Vector3.Dot (collision.contacts [0].normal, Vector3.down);
			//use the dot to determine how much downward pressure to apply to the scale
			squashAmount = squashAmount * Mathf.Clamp (dot, 0.25f, 1f);
			squashScale.y = Mathf.Clamp (squashScale.y - squashAmount, 0.1f, 1f);

			//see if we're hitting from the sides
			Hammer h = collision.collider.attachedRigidbody.GetComponent <Hammer> ();
			if (h != null) {
				float hammerForwardDot = Vector3.Dot (h.ForwardTransform.forward, Vector3.down);
				float hammerBackDot = Vector3.Dot (h.ForwardTransform.forward, Vector3.down);
				if (hammerForwardDot < MinCollisionDot && hammerBackDot < MinCollisionDot) {
					//Debug.Log ("Forward or backward not less than min collision dot");
					canSquash = false;
				}
			}

			if (canSquash && IsReady && collisionMagnitude > MinCollisionMagnitude && dot > MinCollisionDot) {
				nextHapticPulseScale += 1200;
				Squash ();
			}
		}

		public void Update ()
		{
			if (nextHapticPulseScale > 0 && Time.time > nextHapticPulseTime) {
				if (nextHapticPulseScale > 1400) {
					nextHapticPulseScale = 1400;
				}
				nextHapticPulseTime = Time.time + 1f / nextHapticPulseScale;
				if (interactable != null && interactable.IsAttached) {
					interactable.AttachedHandInterface.TriggerHapticPulse (nextHapticPulseScale);
					nextHapticPulseScale = 0;
				}
			}

			if (!IsMetal) {
				squashScale.x = 1f + ((1f - squashScale.y) * 0.5f);
				squashScale.z = 1f + ((1f - squashScale.y) * 0.5f);
				squashScale = Vector3.Lerp (squashScale, Vector3.one, Time.deltaTime * 0.25f);
				transform.localScale = Vector3.Lerp (transform.localScale, squashScale, Time.deltaTime * 10f);
			}
		}

		void Clang ()
		{
			AudioSource.PlayClipAtPoint (MetalBangClip, transform.position);
			//Act.Current.Boo ();
			//fracture hammer?
		}

		void Attach (Rigidbody toBody) {
			Attached = true;
			//become a new 'end'
			transform.parent = null;
			rb.isKinematic = false;
			HingeJoint joint = gameObject.AddComponent <HingeJoint> ();
			joint.breakForce = 3000f;
			joint.breakTorque = 3000f;
			joint.axis = Random.onUnitSphere;
			joint.connectedBody = toBody;
			joint.enableCollision = true;
			joint.enablePreprocessing = false;
			gameObject.tag = "End";
			//now we're no longer a bear, we're a magnet
			GameObject.Destroy (this);
			ToyBoxParent.OnBearAttached (this);
			AudioSource.PlayClipAtPoint (MetalBangClip, transform.position);
		}

		void Squash ()
		{
			if (Squashed)
				return;

			//Act.Current.Cheer ();

			GetComponent<Collider> ().enabled = false;

			GameObject blobsGo = GameObject.Instantiate (ParticleBlobsPrefab, transform.position, Quaternion.Euler (-90f, 0f, 0f)) as GameObject;
			ParticleSystemRenderer r = (ParticleSystemRenderer)blobsGo.GetComponent <Renderer> ();
			r.material.color = BearColor;

			Squashed = true;
			AudioSource.PlayClipAtPoint (SquashClip, transform.position);
			gameObject.GetComponent <MeshFilter> ().sharedMesh = SquashedMesh;
			ToyBoxParent.OnBearSquashed (this);
		}
	}
}