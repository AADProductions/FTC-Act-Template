using UnityEngine;
using System.Collections;
using FTC.Interfaces;
using System;

namespace NewtonVR
{
	public abstract class NVRInteractable : MonoBehaviour, INVRInteractable
    {
        public Rigidbody Rigidbody;

        public bool CanAttach = true;
        
        public bool DisableKinematicOnAttach = true;
        public bool EnableKinematicOnDetach = false;
        public float DropDistance = 3;

		public Action OnBeginInteraction { get; set; }
		public Action OnEndInteraction { get; set; }

		public NVRHand AttachedHand = null;
		public INVRHand AttachedHandInterface {
			get {
				return AttachedHand;
			}
		}

        protected Collider[] Colliders;
        protected Vector3 ClosestHeldPoint;

        protected bool WasUsingGravity = false;

        public virtual bool IsAttached
        {
            get
            {
                return AttachedHand != null;
            }
        }

        protected virtual void Awake()
        {   
            if (Rigidbody == null)
                Rigidbody = this.GetComponent<Rigidbody>();

            if (Rigidbody == null)
            {
                Debug.LogError("There is no rigidbody attached to this interactable.");
            }

            Colliders = this.GetComponentsInChildren<Collider>();
        }

        protected virtual void Start()
        {
            NVRInteractables.Register(this, Colliders);
        }

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true)
            {
                float shortestDistance = float.MaxValue;

                for (int index = 0; index < Colliders.Length; index++)
                {
                    //todo: this does not do what I think it does.
                    Vector3 closest = Colliders[index].ClosestPointOnBounds(AttachedHand.transform.position);
                    float distance = Vector3.Distance(AttachedHand.transform.position, closest);

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        ClosestHeldPoint = closest;
                    }
                }

                if (shortestDistance > DropDistance)
                {
                    DroppedBecauseOfDistance();
                }
            }
        }

        //Remove items that go too high or too low.
        protected virtual void Update()
        {
            if (this.transform.position.y > 10000 || this.transform.position.y < -10000)
            {
                if (AttachedHand != null)
                    AttachedHand.EndInteraction(this);

                Destroy(this.gameObject);
            }
        }

        public virtual void BeginInteraction(NVRHand hand)
        {
            AttachedHand = hand;

            if (DisableKinematicOnAttach == true)
            {
                Rigidbody.isKinematic = false;
            }

            WasUsingGravity = Rigidbody.useGravity;

			if (OnBeginInteraction != null) {
				OnBeginInteraction.Invoke ();
			}
        }

        public virtual void InteractingUpdate(NVRHand hand)
        {
            if (hand.UseButtonUp == true)
            {
                UseButtonUp();
            }

            if (hand.UseButtonDown == true)
            {
                UseButtonDown();
            }
        }

        public void ForceDetach()
        {
            if (AttachedHand != null)
                AttachedHand.EndInteraction(this);

            if (AttachedHand != null)
                EndInteraction();
        }

        public virtual void EndInteraction()
        {
            AttachedHand = null;
            ClosestHeldPoint = Vector3.zero;

            if (EnableKinematicOnDetach == true)
            {
                Rigidbody.isKinematic = true;
            }

            Rigidbody.useGravity = WasUsingGravity;

			if (OnEndInteraction != null) {
				OnEndInteraction.Invoke ();
			}
        }

        protected virtual void DroppedBecauseOfDistance()
        {
            AttachedHand.EndInteraction(this);
        }

        public virtual void UseButtonUp()
        {

        }

        public virtual void UseButtonDown()
        {

        }

        protected virtual void OnDestroy()
        {
            ForceDetach();
        }
    }
}
