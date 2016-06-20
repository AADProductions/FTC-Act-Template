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
		//global stats used for scoring bonuses
		public static int NumBearsSpawned = 0;
		public static int NumBearsSquashed = 0;
		public static int NumWrongColors = 0;
		public static int NumMetalBearsHit = 0;
		public static int NumWrongTouched = 0;
		public static float QuickestSquashTime = Mathf.Infinity;
		public static float LastTimeSquashed = 0f;

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
		public AudioClip SquashFailClip;
		public AudioClip PopClip;
		public GameObject ParticleBlobsPrefab;
		public int PieceIndex;
		float nextHapticPulseTime;
		ushort nextHapticPulseScale;
		Vector3 squashScale = Vector3.one;
		INVRInteractable interactable;
		Rigidbody rb;
		Color bearColor;

		public void Pop () {
			if (!Squashed) {
				GameObject.Instantiate (ParticleBlobsPrefab, transform.position, Quaternion.identity);
				AudioSource.PlayClipAtPoint (PopClip, transform.position);
			}
			GameObject.Destroy (gameObject);
		}

		public override void OnEnable ()
		{
			ColorSelector.GetColorFromName (gameObject.name, out BearColor, out bearColor);

			FtcDependencies d = gameObject.GetComponent <FtcDependencies> ();
			SquashedMesh = d.GetDependency <Mesh> ("GummyBearSquished");
			SquashClip = d.GetDependency <AudioClip> ("BearSquashClip");
			SquashFailClip = d.GetDependency <AudioClip> ("BearSquashFailClip");
			MetalBangClip = d.GetDependency <AudioClip> ("MetalBangClip");
			PopClip = d.GetDependency <AudioClip> ("PopClip");
			ParticleBlobsPrefab = d.GetDependency <GameObject> ("ParticleBlobsPrefab");
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

			if (Act.Current.Difficulty == ActDifficulty.Easy) {
				NumBearsSpawned++;
			} else {
				//only count as 'spawned' if it's the right color
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
				NumMetalBearsHit++;
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

			if (canSquash && IsReady && dot > MinCollisionDot) {
				if (collisionMagnitude > MinCollisionMagnitude) {
					nextHapticPulseScale += 1200;
					Squash ();
				} else {
					//this counts as a 'touch' but not a squash
					//if we're using colors then bump up num wrong touched
					if (Act.Current.Difficulty != ActDifficulty.Easy) {
						WhackABearAct act = Act.Current.Script as WhackABearAct;
						if (act.BearColor != BearColor) {
							NumWrongTouched++;
						}
					}
				}
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

			//record fast squish stat
			float timeBetweenSquashes = LastTimeSquashed - Time.time;
			LastTimeSquashed = Time.time;
			QuickestSquashTime = Mathf.Min (timeBetweenSquashes, QuickestSquashTime);

			if (Act.Current.Difficulty == ActDifficulty.Easy) {
				//there are no colors on easy, just squash
				AudioSource.PlayClipAtPoint (SquashClip, transform.position);
				Act.Current.Cheer ();
				NumBearsSquashed++;
			} else {
				//on med/hard, you have to hit the right color
				WhackABearAct act = Act.Current.Script as WhackABearAct;
				if (act.BearColor == BearColor) {
					AudioSource.PlayClipAtPoint (SquashClip, transform.position);
					Act.Current.Cheer ();
					NumBearsSquashed++;
				} else {
					AudioSource.PlayClipAtPoint (SquashFailClip, transform.position);
					Act.Current.Boo ();
					NumWrongColors++;
				}
			}

			gameObject.GetComponent<Collider> ().enabled = false;

			GameObject blobsGo = GameObject.Instantiate (ParticleBlobsPrefab, transform.position, Quaternion.Euler (-90f, 0f, 0f)) as GameObject;
			ParticleSystemRenderer r = (ParticleSystemRenderer)blobsGo.GetComponent <Renderer> ();
			r.material.color = bearColor;

			Squashed = true;
			gameObject.GetComponent <MeshFilter> ().sharedMesh = SquashedMesh;
			ToyBoxParent.OnBearSquashed (this);
		}
	}
}