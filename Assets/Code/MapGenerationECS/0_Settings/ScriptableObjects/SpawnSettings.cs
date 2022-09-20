using UnityEngine;

using static System.Convert;

namespace KWZTerrainECS
{
    [CreateAssetMenu(fileName = "SpawnSettings", menuName = "KWZTerrainECS/SpawnSettings")]
    public class SpawnSettings : ScriptableObject
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        
        public readonly int NumSectors = 5;
        
        public bool Center;
        public bool Top;
        public bool Bottom;
        public bool Left;
        public bool Right;
        
        public bool isNull => !Center && !Top && !Bottom && !Left && !Right;
        public int NumActiveSectors => ToInt32(Center) + ToInt32(Top) + ToInt32(Bottom) + ToInt32(Left) + ToInt32(Right);
        
        
        public bool this[int index]
        {
            get
            {
                return index switch
                {
                    0 => Center,
                    1 => Top,
                    2 => Bottom,
                    3 => Left,
                    4 => Right,
                    _ => Center
                };
            }
        }
    }
}
