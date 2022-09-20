using UnityEngine;
using UnityEngine.InputSystem;

using static Unity.Mathematics.math;

namespace RTTCamera
{
    public partial class CameraSystem : MonoBehaviour, Controls.ICameraControlActions
    {
        public bool DrawRectangle;
        public bool DontDestroy;
        
        [SerializeField]private CameraInputData cameraData;
        
        //Cache Data
        public Controls controls {get; private set; }
        public Transform CameraTransform {get; private set; }

        //Inputs
        private bool IsRotating;
        private bool IsSprinting;
        private float Zoom;
        private Vector2 MouseStartPosition, MouseEndPosition;
        private Vector2 MoveAxis;
        
        //UPDATED MOVE SPEED
        private int MoveSpeed => IsSprinting ? cameraData.baseMoveSpeed * cameraData.sprint : cameraData.baseMoveSpeed;
        private void Awake()
        {
            CameraTransform = transform;
            
            if (controls == null)
            {
                controls = new Controls();
            }
            if (!controls.CameraControl.enabled)
            {
                controls.CameraControl.Enable();
                controls.CameraControl.SetCallbacks(this);
            }

            HasSelection();
            
            if(DontDestroy)
                DontDestroyOnLoad(gameObject);
        }

        private void HasSelection()
        {
            bool hasComp = TryGetComponent(out SelectionRectangle comp);
            if (DrawRectangle && !hasComp)
                gameObject.AddComponent<SelectionRectangle>();
            else if(!DrawRectangle && hasComp)
                Destroy(comp);
        }
        
        private void Update()
        {
            if (!IsRotating && MoveAxis == Vector2.zero && Zoom == 0) return;
            //Rotation
            Quaternion newRotation = CameraTransform.rotation;
            if (IsRotating) 
                newRotation = GetCameraRotation();
            
            //Position
            Vector3 newPosition = CameraTransform.position;
            if (MoveAxis != Vector2.zero) 
                newPosition = GetCameraPosition(newPosition, CameraTransform.forward, CameraTransform.right);
            if (Zoom != 0) 
                newPosition = Vector3.up * Zoom + newPosition;
            
            //Update
            CameraTransform.SetPositionAndRotation(newPosition, newRotation);
        }

        private void OnDestroy()
        {
            controls.CameraControl.Disable();
        }

        private Vector3 GetCameraPosition(in Vector3 cameraPosition, in Vector3 cameraForward, in Vector3 cameraRight)
        {
            //real forward of the camera (aware of the rotation)
            Vector3 cameraForwardXZ = new Vector3(cameraForward.x, 0, cameraForward.z);
            
            Vector3 xAxisRotation = MoveAxis.x != 0 ? (MoveAxis.x > 0 ? -cameraRight : cameraRight) : Vector3.zero;
            Vector3 zAxisRotation = MoveAxis.y != 0 ? (MoveAxis.y > 0 ? cameraForwardXZ : -cameraForwardXZ) : Vector3.zero;
            
            return cameraPosition + (xAxisRotation + zAxisRotation) * (max(1f,cameraPosition.y) * MoveSpeed * Time.deltaTime);
        }

        private Quaternion GetCameraRotation()
        {
            if (MouseEndPosition == MouseStartPosition) return CameraTransform.rotation;
            Quaternion rotation = CameraTransform.rotation;
            
            Vector2 distanceXY = (MouseEndPosition - MouseStartPosition) * cameraData.rotationSpeed;
            
            rotation = RotateFWorld(rotation ,0f, distanceXY.x * Time.deltaTime,0f); //Rotation Horizontal
            rotation = RotateFSelf(rotation,-distanceXY.y * Time.deltaTime, 0f, 0f); //Rotation Vertical
            
            float clampedXAxis = ClampAngle(rotation.eulerAngles.x, cameraData.MinClamp, cameraData.MaxClamp);
            rotation.eulerAngles = new Vector3(clampedXAxis, rotation.eulerAngles.y, 0);
            //Debug.Log($"rotation.eulerAngles: {rotation.eulerAngles}");
            MouseStartPosition = MouseEndPosition;
            return rotation;
            
            float ClampAngle(float lfAngle, float lfMin, float lfMax)
            {
                if (lfAngle < -180f) lfAngle += 360f;
                if (lfAngle > 180) lfAngle -= 360f;
                return Mathf.Clamp(lfAngle, lfMin, lfMax);
            }
            
            Quaternion RotateFWorld(Quaternion rot, float x, float y, float z)
            {
                Quaternion eulerRot = Quaternion.Euler(x, y, z);
                rot *= (Quaternion.Inverse(rotation) * eulerRot * rotation);
                return rot;
            }
            
            Quaternion RotateFSelf(Quaternion localRotation, float x, float y, float z)
            {
                Quaternion eulerRot = Quaternion.Euler(x, y, z);
                localRotation *= eulerRot;
                return localRotation;
            }
        }

        //EVENTS CALLBACK
        //==============================================================================================================

        public void OnMouvement(InputAction.CallbackContext context)
        {
            MoveAxis = !context.canceled ? context.ReadValue<Vector2>() : Vector2.zero;
        }

        public void OnRotation(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Started:
                    MouseStartPosition = context.ReadValue<Vector2>();
                    IsRotating = true;
                    break;
                case InputActionPhase.Performed:
                    MouseEndPosition = context.ReadValue<Vector2>();
                    break;
                case InputActionPhase.Canceled:
                    IsRotating = false;
                    break;
            }
        }

        public void OnZoom(InputAction.CallbackContext context)
        {
            Zoom = !context.canceled ? context.ReadValue<float>() : 0;
        }

        public void OnFaster(InputAction.CallbackContext context)
        {
            IsSprinting = !context.canceled;
        }
    }
}