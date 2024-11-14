using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BNG {

    public enum MovementVector {
        HMD,
        Controller
    }    

    public class SmoothLocomotion : MonoBehaviour {

        [Header("Movement : ")]
        public float MovementSpeed = 1.25f;

        [Tooltip("(Optional) If specified, this transform's forward direction will determine the movement direction ")]
        public Transform ForwardDirection;

        [Tooltip("Used to determine which direction to move. Example : Left Thumbstick Axis or Touchpad. ")]
        public List<InputAxis> inputAxis = new List<InputAxis>() { InputAxis.LeftThumbStickAxis };

        [Tooltip("Input Action used to affect movement")]
        public InputActionReference MoveAction;

        [Tooltip("If true, movement events will only be sent if the Application has focus (Or Play window, if running in the Unity Editor)")]
        public bool RequireAppFocus = true;

        [Header("Sprint : ")]
        public float SprintSpeed = 1.5f;

        [Tooltip("The key(s) to use to initiate sprint. You can also override the SprintKeyDown() function to determine your sprint criteria.")]
        public List<ControllerBinding> SprintInput = new List<ControllerBinding>() { ControllerBinding.None };

        [Tooltip("Unity Input Action used to enable sprinting")]
        public InputActionReference SprintAction;

        [Header("Strafe : ")]
        public float StrafeSpeed = 1f;
        public float StrafeSprintSpeed = 1.25f;

        [Header("Jump : ")]
        [Tooltip("Amount of 'force' to apply to the player during Jump")]
        public float JumpForce = 3f;

        [Tooltip("The key(s) to use to initiate a jump. You can also override the CheckJump() function to determine your jump criteria.")]
        public List<ControllerBinding> JumpInput = new List<ControllerBinding>() { ControllerBinding.None };

        [Tooltip("Unity Input Action used to initiate a jump")]
        public InputActionReference JumpAction;

        [Header("Air Control : ")]
        [Tooltip("Can the player move when not grounded? Set to true if you want to be able to move the joysticks and have the player respond to input even when not grounded.")]
        public bool AirControl = true;

        [Tooltip("How fast the player can move in the air if AirControl = true. Example : 0.5 = Player will move at half the speed of MovementSpeed")]
        public float AirControlSpeed = 1f;

        BNGPlayerController playerController;
        CharacterController characterController;

        // Left / Right
        float movementX;

        // Up / Down
        float movementY;

        // Forwards / Backwards
        float movementZ;

        bool movementDisabled = false;

        private float _verticalSpeed = 0; // Keep track of vertical speed

        #region Events
        public delegate void OnBeforeMoveAction();
        public static event OnBeforeMoveAction OnBeforeMove;

        public delegate void OnAfterMoveAction();
        public static event OnAfterMoveAction OnAfterMove;
        #endregion

        public virtual void Update() {
            CheckControllerReferences();
            UpdateInputs();            
            MoveCharacter();
        }

        public virtual void CheckControllerReferences() {
            // Component may be called while disabled, so check for references here
            if (playerController == null) {
                playerController = GetComponentInParent<BNGPlayerController>();
            }

            if(characterController == null) {
                characterController = GetComponent<CharacterController>();
            }
        }

        public virtual void UpdateInputs() {

            // Start by resetting our previous frame's inputs
            movementX = 0;
            movementY = 0;
            movementZ = 0;

            // Start with VR Controller Input
            Vector2 primaryAxis = GetMovementAxis();
            if (playerController && playerController.IsGrounded()  ) {
                movementX = primaryAxis.x;
                movementZ = primaryAxis.y;
            }
            else if(AirControl) {
                movementX = primaryAxis.x * AirControlSpeed;
                movementZ = primaryAxis.y * AirControlSpeed;
            }

            if (CheckJump()) {
                movementY += JumpForce;
            }

            if(CheckSprint()) {
                movementX *= StrafeSprintSpeed;
                movementZ *= SprintSpeed;
            }
            else {
                movementX *= StrafeSpeed;
                movementZ *= MovementSpeed;
            }            
        }

        public virtual Vector2 GetMovementAxis() {

            // Use the largest, non-zero value we find in our input list
            Vector3 lastAxisValue = Vector3.zero;

            // Check raw input bindings
            if(inputAxis != null) {
                for (int i = 0; i < inputAxis.Count; i++) {
                    Vector3 axisVal = InputBridge.Instance.GetInputAxisValue(inputAxis[i]);

                    // Always take this value if our last entry was 0. 
                    if (lastAxisValue == Vector3.zero) {
                        lastAxisValue = axisVal;
                    }
                    else if (axisVal != Vector3.zero && axisVal.magnitude > lastAxisValue.magnitude) {
                        lastAxisValue = axisVal;
                    }
                }
            }

            // Check Unity Input Action if we have application focus
            bool hasRequiredFocus = RequireAppFocus == false || RequireAppFocus && Application.isFocused;
            if (MoveAction != null && hasRequiredFocus) {
                Vector3 axisVal = MoveAction.action.ReadValue<Vector2>();

                // Always take this value if our last entry was 0. 
                if (lastAxisValue == Vector3.zero) {
                    lastAxisValue = axisVal;
                }
                else if (axisVal != Vector3.zero && axisVal.magnitude > lastAxisValue.magnitude) {
                    lastAxisValue = axisVal;
                }
            }

            return lastAxisValue;
        }        

        public virtual void MoveCharacter() {

            // Bail early if no elligible for movement
            if(movementDisabled || characterController == null) {
                return;
            }

            Vector3 moveDirection = new Vector3(movementX, movementY, movementZ);

            if(ForwardDirection != null) {
                moveDirection = ForwardDirection.TransformDirection(moveDirection);
            }
            else {
                moveDirection = transform.TransformDirection(moveDirection);
            }

            // Check for jump value
            if (playerController != null && playerController.IsGrounded() && !movementDisabled) {
                // Reset jump speed if grounded
                _verticalSpeed = 0;
                if (CheckJump()) {
                    _verticalSpeed = JumpForce;
                }
            }

            moveDirection.y = _verticalSpeed;

            if(playerController) {
                playerController.LastPlayerMoveTime = Time.time;
            }

            if(moveDirection != Vector3.zero) {
                MoveCharacter(moveDirection * Time.deltaTime);
            }
        }

        public float Magnitude;

        public virtual void MoveCharacter(Vector3 motion) {

            // Can bail immediately if no movement is required

            if(motion == null || motion == Vector3.zero) {
                return;
            }

            Magnitude = (float)Math.Round(motion.magnitude * 1000f) / 1000f;

            bool callEvents = Magnitude > 0.0f;

            CheckControllerReferences();

            // Call any Before Move Events
            if(callEvents) {
                OnBeforeMove?.Invoke();
            }

            if(characterController && characterController.enabled) {
                characterController.Move(motion);
            }

            // Call any After Move Events
            if (callEvents) {
                OnAfterMove?.Invoke();
            }
        }

        public virtual bool CheckJump() {

            // Don't jump if not grounded
            if(playerController != null && !playerController.IsGrounded()) {
                return false;
            }

            // Check for bound controller button
            for (int x = 0; x < JumpInput.Count; x++) {
                if (InputBridge.Instance.GetControllerBindingValue(JumpInput[x])) {
                    return true;
                }
            }

            // Check Unity Input Action value
            if (JumpAction != null && JumpAction.action.ReadValue<float>() > 0) {
                return true;
            }

            return false;
        }

        public virtual bool CheckSprint() {

            // Check for bound controller button
            for (int x = 0; x < SprintInput.Count; x++) {
                if (InputBridge.Instance.GetControllerBindingValue(SprintInput[x])) {
                    return true;
                }
            }

            // Check Unity Input Action
            if (SprintAction != null) {
                return SprintAction.action.ReadValue<float>() == 1f;
            }

            return false;
        }

        public virtual void EnableMovement() {
            movementDisabled = false;
        }

        public virtual void DisableMovement() {
            movementDisabled = true;
        }
    }
}

