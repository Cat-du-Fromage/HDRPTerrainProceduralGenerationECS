#if HYBRID_ENTITIES_CAMERA_CONVERSION
using System;
using Unity.Entities;

namespace RTTCamera
{
    [Serializable]
    public class Object_InputsControl : IComponentData
    {
        public Controls Value;
    }
}
#endif