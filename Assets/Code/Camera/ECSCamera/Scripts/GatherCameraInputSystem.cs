#if HYBRID_ENTITIES_CAMERA_CONVERSION
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTTCamera
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class GatherCameraInputSystem : SystemBase
    {
        private Controls controls;
        private Controls.CameraControlActions cameraControls;

        private float zoomValue;
        private float2 startMouseDrag;
        private float2 endMouseDrag;
        
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Tag_Camera>();
            controls = new Controls();
            cameraControls = controls.CameraControl;
        }

        protected override void OnStartRunning()
        {
            EntityManager.SetComponentData(GetSingletonEntity<Tag_Camera>(), new Object_InputsControl(){Value = controls});
            controls.Enable();
            
            cameraControls.Rotation.started   += OnRotationStart;
            cameraControls.Rotation.performed += OnRotationPerformed;

            cameraControls.Zoom.performed += OnZoomPerformed;
            cameraControls.Zoom.canceled  += OnZoomCanceled;
        }

        protected override void OnStopRunning()
        {
            cameraControls.Rotation.started   -= OnRotationStart;
            cameraControls.Rotation.performed -= OnRotationPerformed;

            cameraControls.Zoom.performed -= OnZoomPerformed;
            cameraControls.Zoom.canceled  -= OnZoomCanceled;
            controls.Disable();
        }

        protected override void OnUpdate()
        {
            InputSystem.Update();

            bool isSprinting = cameraControls.Faster.IsPressed();
            float zoom = zoomValue;
            float2 moveAxis = cameraControls.Mouvement.ReadValue<Vector2>();
            float2 startUpdated = startMouseDrag;
            float2 endUpdated = endMouseDrag;
            
            Entities
            .WithBurst()
            .WithAll<Tag_Camera>()
            .ForEach((ref Data_CameraInputs cameraInputs, in Data_CameraSettings settings) =>
            {
                float2 distanceXY = (endUpdated - startUpdated) * settings.RotationSpeed;
                cameraInputs = new Data_CameraInputs()
                {
                    IsSprint = isSprinting,
                    Zoom = zoom,
                    MoveAxis = moveAxis,
                    RotationDragDistanceXY = distanceXY
                };
            }).Run();
            startMouseDrag = endMouseDrag;
        }
        
        private void OnRotationStart(InputAction.CallbackContext ctx) => startMouseDrag = ctx.ReadValue<Vector2>();
        private void OnRotationPerformed(InputAction.CallbackContext ctx) => endMouseDrag = ctx.ReadValue<Vector2>();
        private void OnZoomPerformed(InputAction.CallbackContext ctx) => zoomValue = ctx.ReadValue<float>();
        private void OnZoomCanceled(InputAction.CallbackContext ctx) => zoomValue = 0;
    }
}
#endif