using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public enum LocomotionType {
        Teleport,
        SmoothLocomotion,
        None
    }

    /// <summary>
    /// The BNGPlayerController handles basic player movement
    /// </summary>
    public class BNGPlayerController : MonoBehaviour {

        [Header("Camera Options : ")]

        [Tooltip("If true the CharacterController will move along with the HMD, as long as there are no obstacle's in the way")]
        public bool MoveCharacterWithCamera = true;

        [Tooltip("If true the CharacterController will rotate it's Y angle to match the HMD's Y angle")]
        public bool RotateCharacterWithCamera = true;

        [Header("Transform Setup ")]

        [Tooltip("The TrackingSpace represents your tracking space origin.")]
        public Transform TrackingSpace;

        [Tooltip("The CameraRig is a Transform that is used to offset the main camera. The main camera should be parented to this.")]
        public Transform CameraRig;

        [Tooltip("The CenterEyeAnchor is typically the Transform that contains your Main Camera")]
        public Transform CenterEyeAnchor;

        [Header("Ground checks : ")]
        [Tooltip("Raycast against these layers to check if player is grounded")]
        public LayerMask GroundedLayers;

        /// <summary>
        /// 0 means we are grounded
        /// </summary>
        [Tooltip("How far off the ground the player currently is. 0 = Grounded, 1 = 1 Meter in the air.")]
        public float DistanceFromGround = 0;

        [Header("Player Capsule Settings : ")]
        /// <summary>
        /// Minimum Height our Player's capsule collider can be (in meters)
        /// </summary>
        [Tooltip("Minimum Height our Player's capsule collider can be (in meters)")]
        public float MinimumCapsuleHeight = 0.4f;

        /// <summary>
        /// Maximum Height our Player's capsule collider can be (in meters)
        /// </summary>
        [Tooltip("Maximum Height our Player's capsule collider can be (in meters)")]
        public float MaximumCapsuleHeight = 3f;        

        [HideInInspector]
        public float LastTeleportTime;

        [Header("Player Y Offset : ")]
        /// <summary>
        /// Offset the height of the CharacterController by this amount
        /// </summary>
        [Tooltip("Offset the height of the CharacterController by this amount")]
        public float CharacterControllerYOffset = -0.025f;

        /// <summary>
        /// Height of our camera in local coords
        /// </summary>
        [HideInInspector]
        public float CameraHeight;

        [Header("Misc : ")]                

        [Tooltip("If true the Camera will be offset by ElevateCameraHeight if no HMD is active or connected. This prevents the camera from falling to the floor and can allow you to use keyboard controls.")]
        public bool ElevateCameraIfNoHMDPresent = true;

        [Tooltip("How high (in meters) to elevate the player camera if no HMD is present and ElevateCameraIfNoHMDPresent is true. 1.65 = about 5.4' tall. ")]
        public float ElevateCameraHeight = 1.65f;

        /// <summary>
        /// If player goes below this elevation they will be reset to their initial starting position.
        /// If the player goes too far away from the center they may start to jitter due to floating point precisions.
        /// Can also use this to detect if player somehow fell through a floor. Or if the "floor is lava".
        /// </summary>
        [Tooltip("Minimum Y position our player is allowed to go. Useful for floating point precision and making sure player didn't fall through the map.")]
        public float MinElevation = -6000f;

        /// <summary>
        /// If player goes above this elevation they will be reset to their initial starting position.
        /// If the player goes too far away from the center they may start to jitter due to floating point precisions.
        /// </summary>
        public float MaxElevation = 6000f;

        [HideInInspector]
        public float LastPlayerMoveTime;

        // The controller to manipulate
        CharacterController characterController;

        // Use smooth movement if available
        SmoothLocomotion smoothLocomotion;

        // Optional components can be used to update LastMoved Time
        PlayerClimbing playerClimbing;

        // This the object that is currently beneath us
        public RaycastHit groundHit;
        Transform mainCamera;

        private Vector3 _initialPosition;

        void Start() {
            characterController = GetComponentInChildren<CharacterController>();
            smoothLocomotion = GetComponentInChildren<SmoothLocomotion>();

            mainCamera = GameObject.FindGameObjectWithTag("MainCamera").transform;

            if (characterController) {
                _initialPosition = characterController.transform.position;
            }
            else {
                _initialPosition = transform.position;
            }

            playerClimbing = GetComponentInChildren<PlayerClimbing>();
        }

        void Update() {

            // Sanity check for camera
            if (mainCamera == null && Camera.main != null) {
                mainCamera = Camera.main.transform;
            }
            
            // Update the Character Controller's Capsule Height to match our Camera position
            UpdateCharacterHeight();

            // Update the position of our camera rig to account for our player's height
            UpdateCameraRigPosition();

            // After positioning the camera rig, we can update our main camera's height
            UpdateCameraHeight();

            CheckCharacterCollisionMove();

            if (characterController) {

                // Align TrackingSpace with Camera
                if (RotateCharacterWithCamera) {
                    RotateTrackingSpaceToCamera();
                }
            }
        }
       
        void FixedUpdate() {

            UpdateDistanceFromGround();

            CheckPlayerElevationRespawn();
        }

        /// <summary>
        /// Check if the player has moved beyond the specified min / max elevation
        /// Player should never go above or below 6000 units as physics can start to jitter due to floating point precision
        /// Maybe they clipped through a floor, touched a set "lava" height, etc.
        /// </summary>
        public virtual void CheckPlayerElevationRespawn() {

            // No need for elevation checks
            if(MinElevation == 0 && MaxElevation == 0) {
                return;
            }

            // Check Elevation based on Character Controller height
            if(characterController != null && (characterController.transform.position.y < MinElevation || characterController.transform.position.y > MaxElevation)) {
                Debug.Log("Player out of bounds; Returning to initial position.");
                characterController.transform.position = _initialPosition;
            }
        }

        public virtual void UpdateDistanceFromGround() {

            if(characterController) {
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

        public virtual void RotateTrackingSpaceToCamera() {
            Vector3 initialPosition = TrackingSpace.position;
            Quaternion initialRotation = TrackingSpace.rotation;

            // Move the character controller to the proper rotation / alignment
            characterController.transform.rotation = Quaternion.Euler(0.0f, CenterEyeAnchor.rotation.eulerAngles.y, 0.0f);

            // Now we can rotate our tracking space back to initial position / rotation
            TrackingSpace.position = initialPosition;
            TrackingSpace.rotation = initialRotation;
        }

        public virtual void UpdateCameraRigPosition() {

            float yPos = CharacterControllerYOffset;

            // Get character controller position based on the height and center of the capsule
            if (characterController != null) {
                yPos = -(0.5f * characterController.height) + characterController.center.y + CharacterControllerYOffset;
            }
            
            // Offset the capsule a bit while climbing. This allows the player to more easily hoist themselves onto a ledge / platform.
            if (playerClimbing != null && playerClimbing.GrippingAtLeastOneClimbable()) {
                yPos -= 0.25f;
            }

            // If no HMD is active, bump our rig up a bit so it doesn't sit on the floor
            if(!InputBridge.Instance.HMDActive && ElevateCameraIfNoHMDPresent) {
                yPos += ElevateCameraHeight;
            }

            CameraRig.transform.localPosition = new Vector3(CameraRig.transform.localPosition.x, yPos, CameraRig.transform.localPosition.z);
        }

        public virtual void UpdateCharacterHeight() {
            float minHeight = MinimumCapsuleHeight;
            // Increase Min Height if no HMD is present. This prevents our character from being really small
            if(!InputBridge.Instance.HMDActive && minHeight < 1f) {
                minHeight = 1f;
            }

            // Update Character Height based on Camera Height.
            if(characterController) {
                characterController.height = Mathf.Clamp(CameraHeight + CharacterControllerYOffset - characterController.skinWidth, minHeight, MaximumCapsuleHeight);

                // If we are climbing set the capsule center upwards
                if (playerClimbing != null && playerClimbing.GrippingAtLeastOneClimbable()) {
                    characterController.height = playerClimbing.ClimbingCapsuleHeight;
                    characterController.center = new Vector3(0, playerClimbing.ClimbingCapsuleCenter, 0);
                }
                else {
                    characterController.center = new Vector3(0, -0.25f, 0);
                }
            }
        }

        public virtual void UpdateCameraHeight() {
            // update camera height
            if (CenterEyeAnchor) {
                CameraHeight = CenterEyeAnchor.localPosition.y;
            }
        }

        /// <summary>
        /// Move the character controller to new camera position
        /// </summary>
        public virtual void CheckCharacterCollisionMove() {

            if(!MoveCharacterWithCamera || characterController == null) {
                return;
            }
            
            Vector3 initialCameraRigPosition = CameraRig.transform.position;
            Vector3 cameraPosition = CenterEyeAnchor.position;
            Vector3 delta = cameraPosition - characterController.transform.position;
            
            // Ignore Y position
            delta.y = 0;

            // Move Character Controller and Camera Rig to Camera's delta
            if (delta.magnitude > 0.0f) {

                if(smoothLocomotion) {
                    smoothLocomotion.MoveCharacter(delta);
                }
                else if(characterController) {
                    characterController.Move(delta);
                }

                // Move Camera Rig back into position
                CameraRig.transform.position = initialCameraRigPosition;
            }
        }

        public bool IsGrounded() {

            // Immediately check for a positive from a CharacterController if it's present
            if(characterController != null) {
                if(characterController.isGrounded) {
                    return true;
                }
            }

            // DistanceFromGround is a bit more reliable as we can give a bit of leniency in what's considered grounded
            return DistanceFromGround <= 0.001f;
        }
    }
}
