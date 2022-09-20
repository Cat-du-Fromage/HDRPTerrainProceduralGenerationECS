#if HYBRID_ENTITIES_CAMERA_CONVERSION
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Entities.ComponentType;
using static RTTCamera.CameraUtility;
using static Unity.Mathematics.math;
using static Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

namespace RTTCamera
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(GatherCameraInputSystem))]
    public partial class CameraMovementSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Tag_Camera>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            Entity cameraEntity = GetSingletonEntity<Tag_Camera>();
            if (HasComponent<Tag_SelectionBox>(cameraEntity))
            {
                InitializeCompanionGameObject(cameraEntity);
            }
        }

        private void InitializeCompanionGameObject(Entity cameraEntity)
        {
            Camera camera = EntityManager.GetComponentObject<Camera>(cameraEntity);
            if (!camera.gameObject.TryGetComponent(out SelectionRectangleECS comp))
            {
                comp = camera.gameObject.AddComponent<SelectionRectangleECS>();
            }
            comp.Initialize(EntityManager, cameraEntity);
        }

        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;

            Entities
            .WithName("CameraMovement")
            .WithBurst()
            .WithAll<Tag_Camera>()
            .ForEach((ref Translation translation, ref Rotation rotation, in LocalToWorld ltw, in Data_CameraInputs camInputs, in Data_CameraSettings settings) => 
            {
                //TRANSLATION
                float3 cameraForwardXZ = new float3(ltw.Forward.x, 0, ltw.Forward.z);
                float2 moveAxis = camInputs.MoveAxis;

                float3 cameraRightValue = select(ltw.Right, -ltw.Right, moveAxis.x > 0);
                float3 xAxisRotation = select(zero, cameraRightValue, camInputs.MoveAxis.x != 0);

                float3 cameraForwardValue = select(-cameraForwardXZ, cameraForwardXZ, moveAxis.y > 0);
                float3 zAxisRotation = select(zero, cameraForwardValue, camInputs.MoveAxis.y != 0);

                float3 currentPosition = translation.Value;
                float ySpeedMultiplier = max(1f, currentPosition.y);
                int moveSpeed = settings.BaseMoveSpeed * select(1, settings.Sprint, camInputs.IsSprint);

                float3 zoomPosition = camInputs.Zoom * settings.ZoomSpeed * deltaTime * up();
                float3 horizontalPosition = ySpeedMultiplier * moveSpeed * deltaTime * (xAxisRotation + zAxisRotation);
                translation.Value = translation.Value + zoomPosition + horizontalPosition;

                //ROTATION
                if(!any(camInputs.RotationDragDistanceXY)) return;
                quaternion rotationVal = rotation.Value;
            
                float2 distanceXY = camInputs.RotationDragDistanceXY;
                rotationVal = RotateFWorld(rotationVal,0f,distanceXY.x * deltaTime,0f);//Rotation Horizontal
                rotation.Value = rotationVal;

                rotationVal = RotateFSelf(rotationVal,-distanceXY.y * deltaTime,0f,0f);//Rotation Vertical
                float angleX = clampAngle(degrees(rotationVal.ToEulerAngles(RotationOrder.ZXY).x), settings.MinClamp, settings.MaxClamp);
                
                float2 currentRotationEulerYZ = rotation.Value.ToEulerAngles(RotationOrder.ZXY).yz;
                rotation.Value = quaternion.EulerZXY(new float3(radians(angleX), currentRotationEulerYZ));
            }).Run();
        }
    }
    
}
#endif