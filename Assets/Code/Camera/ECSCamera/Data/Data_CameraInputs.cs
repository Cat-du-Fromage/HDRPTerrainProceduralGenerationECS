#if HYBRID_ENTITIES_CAMERA_CONVERSION
using Unity.Entities;
using Unity.Mathematics;
namespace RTTCamera
{
    public struct Data_CameraInputs : IComponentData
    {
        public bool IsSprint;
        public float Zoom;
        public float2 MoveAxis;
        public float2 RotationDragDistanceXY;
    }
}
#endif