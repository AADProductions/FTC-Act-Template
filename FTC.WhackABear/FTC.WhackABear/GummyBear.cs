using UnityEngine;
using System.Collections;
using FTC.Interfaces;
using FTC.Core;

namespace FTC.WhackABear {

	public enum GummyBearColor {
		Blue,
		Red,
		Yellow,
		Green
	}

	public class GummyBear : FtcBehaviourScript
	{
		public bool IsMetal = false;
		public bool IsReady = false;
		public bool Squashed = false;
		public bool Attached = false;
		public float MinCollisionMagnitude = 3f;
		public float MinCollisionDot = 0.5f;
		public GummyBearColor BearColor;
		public Mesh SquashedMesh;
		public ToyBox ToyBoxParent;
		public AudioClip MetalBangClip;
		public AudioClip SquashClip;
		public GameObject ParticleBlobsPrefab;
		public int PieceIndex;
		float nextHapticPulseTime;
		ushort nextHapticPulseScale;
		Vector3 squashScale = Vector3.one;
		INVRInteractable interactable;
		Rigidbody rb;
		Color bearColor;

		public override void OnEnable ()
		{
			ColorSelector.GetColorFromName (gameObject.name, out BearColor, out bearColor);

			FtcDependencies dependencies = gameObject.GetComponent <FtcDependencies> ();
			SquashedMesh = dependencies.GetDependency <Mesh> ("GummyBearSquished");
			SquashClip = dependencies.GetDependency <AudioClip> ("BearSquashClip");
			MetalBangClip = dependencies.GetDependency <AudioClip> ("MetalBangClip");
			ParticleBlobsPrefab = dependencies.GetDependency <GameObject> ("ParticleBlobsPrefab");
		}

		public override void Start ()
		{
			rb = gameObject.GetComponent <Rigidbody> ();

			if (!IsMetal) {
				transform.localScale = new Vector3 (1f, 0.8f, 1f);
			}

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

		public override void OnCollisionEnter (Collision collision)
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
			Rigidbody attachedRigidBody = collision.collider.attachedRigidbody;
			if (attachedRigidBody != null) {
				FtcBehaviour fb = collision.collider.attachedRigidbody.GetComponent <FtcBehaviour> ();
				if (fb != null) {
					Hammer h = fb.Script as Hammer;
					if (h != null) {
						float hammerForwardDot = Vector3.Dot (h.ForwardTransform.forward, Vector3.down);
						float hammerBackDot = Vector3.Dot (h.ForwardTransform.forward, Vector3.down);
						if (hammerForwardDot < MinCollisionDot && hammerBackDot < MinCollisionDot) {
							//Debug.Log ("Forward or backward not less than min collision dot");
							canSquash = false;
						}
					}
				}
			}

			if (canSquash && IsReady && collisionMagnitude > MinCollisionMagnitude && dot > MinCollisionDot) {
				nextHapticPulseScale += 1200;
				Squash ();
			}
		}

		public override void Update ()
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
			if (interactable != null) {
				FtcBehaviour fb = interactable.gameObject.GetComponent <FtcBehaviour> ();
				if (fb != null) {
					Hammer h = fb.Script as Hammer;
					if (h != null) {
						h.Crack ();
					}
				}
			}
			AudioSource.PlayClipAtPoint (MetalBangClip, transform.position);
			Act.Current.Boo ();
		}

		void Attach (Rigidbody toBody)
		{
			Debug.Log ("Attaching");
			Attached = true;
			//become a new 'end'
			//parent it under the activation object (so it will be turned off correctly)
			transform.parent = Act.Current.ActivationObject.transform;
			rb.isKinematic = false;
			HingeJoint joint = gameObject.AddComponent <HingeJoint> ();
			joint.breakForce = 3000f;
			joint.breakTorque = 3000f;
			joint.axis = Random.onUnitSphere;
			joint.connectedBody = toBody;
			joint.enableCollision = true;
			joint.enablePreprocessing = false;
			gameObject.tag = "End";
			ToyBoxParent.OnBearAttached (this);
			AudioSource.PlayClipAtPoint (MetalBangClip, transform.position);
			//now we're no longer a bear, we're a magnet
			//so destroy this script
			GameObject.Destroy (this.behaviour);
		}

		void Squash ()
		{
			if (Squashed)
				return;

			Animation anim = gameObject.GetComponent <Animation> ();
			anim.Stop ();
			transform.localPosition = Vector3.up * 0.01f;
			AudioSource audio = gameObject.GetComponent <AudioSource> ();
			audio.Stop ();

			WhackABearAct act = Act.Current.Script as WhackABearAct;
			if (act.BearColor == BearColor) {
				Act.Current.Cheer ();
			} else {
				Act.Current.Boo ();
			}

			gameObject.GetComponent<Collider> ().enabled = false;

			GameObject blobsGo = GameObject.Instantiate (ParticleBlobsPrefab, transform.position, Quaternion.Euler (-90f, 0f, 0f)) as GameObject;
			ParticleSystemRenderer r = (ParticleSystemRenderer)blobsGo.GetComponent <Renderer> ();
			r.material.color = bearColor;

			Squashed = true;
			AudioSource.PlayClipAtPoint (SquashClip, transform.position);
			gameObject.GetComponent <MeshFilter> ().sharedMesh = SquashedMesh;
			ToyBoxParent.OnBearSquashed (this);
		}
	}
}