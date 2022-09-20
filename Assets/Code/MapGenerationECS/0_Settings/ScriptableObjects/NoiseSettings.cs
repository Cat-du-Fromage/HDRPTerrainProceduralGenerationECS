using UnityEngine;
using Unity.Mathematics;
    
using static Unity.Mathematics.math;

namespace KWZTerrainECS
{
    [CreateAssetMenu(fileName = "NoiseSettingsECS", menuName = "KWZTerrainECS/NoiseSettings")]
    public class NoiseSettings : ScriptableObject
    {
        public uint Seed;
        public int Octaves = 4;
        public float Lacunarity = 1.8f;
        public float Persistence = 0.5f;
        public float Scale = 1;
        public float HeightMultiplier = 1;
        public float2 Offset;

        private void OnEnable()
        {
            Seed = max(1, Seed);
            Octaves = max(1, Octaves);
            Lacunarity = max(1f, Lacunarity);
            Scale = max(0.0001f, Scale);
            HeightMultiplier = max(1f, HeightMultiplier);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Seed = max(1, Seed);
            Octaves = max(1, Octaves);
            Lacunarity = max(1f, Lacunarity);
            Scale = max(0.0001f, Scale);
            HeightMultiplier = max(1f, HeightMultiplier);
        }
#endif

        public static implicit operator NoiseSettingsData(NoiseSettings data)
        {
            return new NoiseSettingsData
            {
                Seed = data.Seed,
                Octaves = data.Octaves,
                Lacunarity = data.Lacunarity,
                Persistence = data.Persistence,
                Scale = data.Scale,
                HeightMultiplier = data.HeightMultiplier,
                Offset = data.Offset
            };
        }
    }
    
    //USE FOR JOB
    public struct NoiseSettingsData
    {
        public uint Seed;
        public int Octaves;
        public float Lacunarity;
        public float Persistence;
        public float Scale;
        public float HeightMultiplier;
        public float2 Offset;
    }
}