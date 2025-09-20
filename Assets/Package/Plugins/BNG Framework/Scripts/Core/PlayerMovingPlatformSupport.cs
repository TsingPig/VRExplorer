using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class PlayerMovingPlatformSupport : MonoBehaviour {

        [Header("Ground checks : ")]
        [Tooltip("Raycast against these layers to check if player is on a moving platform")]
        public LayerMask GroundedLayers;

        // The object currently below us
        protected RaycastHit groundHit;

        // Use smooth movement if available
        SmoothLocomotion smoothLocomotion;

        // Move characterController with platform if smoothlocomotion is not available
        CharacterController characterController;

        private Transform _initialCharacterParent;

        protected float DistanceFromGround;

        MovingPlatform currentPlatform;

        // Were we on the platform last frame
        bool wasOnPlatform;
        bool requiresReparent; // Should we reparent the player after we hop off?

        void Start() {
            smoothLocomotion = GetComponentInChildren<SmoothLocomotion>();
            characterController = GetComponentInChildren<CharacterController>();

            _initialCharacterParent = transform.parent;
        }

        void Update() {
            CheckMovingPlatform();
        }

        void FixedUpdate() {
            UpdateDistanceFromGround();
        }

        public virtual void CheckMovingPlatform() {
            bool onMovingPlatform = false;

            if (groundHit.collider != null && DistanceFromGround < 0.01f) {
                currentPlatform = groundHit.collider.gameObject.GetComponent<MovingPlatform>();

                if (currentPlatform) {
                    onMovingPlatform = true;

                    // This is another potential method of moving the character instead of parenting it
                    if (currentPlatform.MovementMethod == MovingPlatformMethod.PositionDifference && currentPlatform != null && currentPlatform.PositionDelta != Vector3.zero) {
                        if (smoothLocomotion) {
                            smoothLocomotion.MoveCharacter(currentPlatform.PositionDelta);
                        }
                        else if (characterController) {
                            characterController.Move(currentPlatform.PositionDelta);
                        }
                    }

                    // For now we can parent the characterController object to move it along. Rigidbodies may want to change friction materials or alter the player's velocity
                    if (currentPlatform.MovementMethod == MovingPlatformMethod.ParentToPlatform && characterController != null) {
                        if (onMovingPlatform) {
                            characterController.transform.parent = groundHit.collider.transform;
                            requiresReparent = true;
                        }
                    }
                }
            }

            // Check if we need to reparent the character after hopping off a platform
            if(!onMovingPlatform && wasOnPlatform && requiresReparent) {
                characterController.transform.parent = _initialCharacterParent;
            }

            wasOnPlatform = onMovingPlatform;
        }

        public virtual void UpdateDistanceFromGround() {

            if (characterController) {
                if (Physics.Raycast(characterController.transform.position, -characterController.transform.up, out groundHit, 20, GroundedLayers, QueryTriggerInteraction.Ignore)) {
                    DistanceFromGround = Vector3.Distance(characterController.transform.position, groundHit.point);
                    DistanceFromGround += characterController.center.y;
                    DistanceFromGround -= (characterController.height * 0.5f) + characterController.skinWidth;

                    // Round to nearest thousandth
                    DistanceFromGround = (float)Math.Round(DistanceFromGround * 1000f) / 1000f;
                }
                else {
                    DistanceFromGround = 9999f;
                }
            }
            // No CharacterController found. Update Distance based on current transform position
            else {
                if (Physics.Raycast(transform.position, transform.up, out groundHit, 20, GroundedLayers, QueryTriggerInteraction.Ignore)) {
                    DistanceFromGround = Vector3.Distance(transform.position, groundHit.point);
                    // Round to nearest thousandth
                    DistanceFromGround = (float)Math.Round(DistanceFromGround * 1000f) / 1000f;
                }
                else {
                    DistanceFromGround = 9999f;
                }
            }
        }
    }
}

