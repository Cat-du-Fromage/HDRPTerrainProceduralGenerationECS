using System.Collections.Generic;
using UnityEngine;
using ProjectDawn.Collections;
using ProjectDawn.Mathematics;
using Unity.Mathematics;
using Unity.Collections;

public class DrawPriorityQueue : MonoBehaviour
{
    public List<Transform> Transforms;
    public bool IncludeChildTransforms = true;
    public Transform Target;

    public struct Float2DistanceComparer : IComparer<float2>
    {
        public float2 Center;

        public int Compare(float2 x, float2 y)
        {
            float distanceSq0 = math.distancesq(x, Center);
            float distanceSq1 = math.distancesq(y, Center);
            return distanceSq0.CompareTo(distanceSq1);
        }
    }

    private void OnDrawGizmos()
    {
        var target = Target.position.asfloat().xy;
        using (var queue = new NativePriorityQueue<float2, Float2DistanceComparer>(
            Allocator.TempJob, new Float2DistanceComparer { Center = target }))
        {
            foreach (var transform in Transforms)
            {
                if (transform == null)
                    continue;
                queue.Enqueue(transform.position.asfloat().xy);
            }

            if (IncludeChildTransforms)
            {
                var childTransforms = transform.GetComponentsInChildren<Transform>();
                foreach (var transform in childTransforms)
                {
                    if (transform == this.transform)
                        continue;
                    queue.Enqueue(transform.position.asfloat().xy);
                }
            }

            float2 previous = target;
            while (!queue.IsEmpty)
            {
                float2 current = queue.Dequeue();
                DrawLine(previous, current, Color.green);
                previous = current;
            }
        }
    }

    void DrawLine(float2 from, float2 to, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(from.asvector3(), to.asvector3());
    }
}
