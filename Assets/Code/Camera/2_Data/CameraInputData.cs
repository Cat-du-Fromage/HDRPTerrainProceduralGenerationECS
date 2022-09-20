using UnityEngine;
using static Unity.Mathematics.math;

namespace RTTCamera
{
    [CreateAssetMenu(fileName = "CameraData", menuName = "RTTCamera", order = 0)]
    public class CameraInputData : ScriptableObject
    {
        [Min(1)]
        public int rotationSpeed; 
        [Min(1)]
        public int baseMoveSpeed; 
        [Min(1)]
        public int zoomSpeed;
        [Min(1)]
        public int sprint;
        
        [Tooltip("How far in degrees can you move the camera Down")]
        public float MaxClamp = 70.0f;
        [Tooltip("How far in degrees can you move the camera Top")]
        public float MinClamp = -30.0f;

        private void OnEnable()
        {
            rotationSpeed = max(1,rotationSpeed);
            baseMoveSpeed = max(1, baseMoveSpeed);
            zoomSpeed = max(1, zoomSpeed);
            sprint = max(baseMoveSpeed, sprint);
        }

        private void Awake()
        {
            rotationSpeed = max(1,rotationSpeed);
            baseMoveSpeed = max(1, baseMoveSpeed);
            zoomSpeed = max(1, zoomSpeed);
            sprint = max(baseMoveSpeed, sprint);
        }
#if HYBRID_ENTITIES_CAMERA_CONVERSION
        public static implicit operator Data_CameraSettings(CameraInputData data)  
        {  
            return new Data_CameraSettings
            {
                RotationSpeed = data.rotationSpeed,
                BaseMoveSpeed = data.baseMoveSpeed,
                ZoomSpeed = data.zoomSpeed,
                Sprint = data.sprint,
                MaxClamp = data.MaxClamp,
                MinClamp = data.MinClamp
            }; 
        }
#endif
    }
}