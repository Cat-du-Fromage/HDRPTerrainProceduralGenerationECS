#if HYBRID_ENTITIES_CAMERA_CONVERSION
using System;
using Unity.Entities;

namespace RTTCamera
{
    [Serializable]
    public struct Data_CameraSettings : IComponentData
    {
        public int RotationSpeed;
        public int BaseMoveSpeed;
        public int ZoomSpeed;
        public int Sprint;
        public float MaxClamp;
        public float MinClamp;
    }
}
#endif