#if HYBRID_ENTITIES_CAMERA_CONVERSION
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace RTTCamera
{
    [DisallowMultipleComponent]
    public class AuthoringCameraConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private CameraInputData cameraData;
        [SerializeField] private bool BoxSelection;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (cameraData == null) return;
            dstManager.AddComponent<Tag_Camera>(entity);
            
            dstManager.AddComponentObject(entity, new Object_InputsControl());
            
            dstManager.AddComponent<Data_CameraInputs>(entity);
            Data_CameraSettings settings = new Data_CameraSettings
            {
                RotationSpeed = max(1,cameraData.rotationSpeed/10),
                BaseMoveSpeed = cameraData.baseMoveSpeed,
                ZoomSpeed = cameraData.zoomSpeed,
                Sprint = cameraData.sprint,
                MaxClamp = cameraData.MaxClamp,
                MinClamp = cameraData.MinClamp
            };
            dstManager.AddComponentData(entity, settings);

            if (!BoxSelection) return;
            dstManager.AddComponent<Tag_SelectionBox>(entity);
        }
    }
}
#endif