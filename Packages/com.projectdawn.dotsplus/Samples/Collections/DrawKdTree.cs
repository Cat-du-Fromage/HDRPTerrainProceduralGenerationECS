using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectDawn.Collections;
using ProjectDawn.Mathematics;
using Unity.Mathematics;
using Unity.Collections;

public class DrawKdTree : MonoBehaviour
{
    public List<Transform> Transforms;
    public bool IncludeChildTransforms = true;
    public Transform Target;
    public int FindNearestCount = 1;
    public float FindNearestRadius = 0;
    public Rect Bounds;

    public int FindNearestSearchCount;

    struct TreeComparer : IKdTreeComparer<float2>
    {
        public int Compare(float2 x, float2 y, int depth)
        {
            int axis = depth % 2;
            return x[axis].CompareTo(y[axis]);
        }

        public float DistanceSq(float2 x, float2 y)
        {
            return math.distancesq(x, y);
        }

        public float DistanceToSplitSq(float2 x, float2 y, int depth)
        {
            int axis = depth % 2;
            return (x[axis] - y[axis]) * (x[axis] - y[axis]);
        }
    }

    private void OnDrawGizmos()
    {
        DrawRect(Bounds, Color.white);

        using (var tree = new NativeKdTree<float2, TreeComparer>(Allocator.TempJob, new TreeComparer()))
        {
            foreach (var transform in Transforms)
            {
                if (transform == null)
                    continue;
                tree.Add(new float2(transform.position.x, transform.position.y));
            }

            if (IncludeChildTransforms)
            {
                var childTransforms = transform.GetComponentsInChildren<Transform>();
                foreach (var transform in childTransforms)
                {
                    if (transform == this.transform)
                        continue;
                    tree.Add(new float2(transform.position.x, transform.position.y));
                }
            }

            if (tree.Length > 0)
            {
                DrawNode(tree.Root, Bounds, 0);

                var target = new float2(Target.position.x, Target.position.y);

                if (Target != null)
                {
                    if (FindNearestRadius > 0)
                    {
                        DrawWireCircle(target, FindNearestRadius, new Color(0, 1, 0, 1));

                        var points = new NativeList<float2>(Allocator.Temp);
                        tree.FindRadius(target, FindNearestRadius, ref points, out FindNearestSearchCount);
                        foreach (var point in points)
                        {
                            DrawWireCircle(point, 0.1f, Color.green);
                        }
                        points.Dispose();
                    }
                    else if (FindNearestCount > 1)
                    {
                        var points = new NativeList<float2>(Allocator.Temp);
                        tree.FindNearestRange(target, FindNearestCount, ref points, out FindNearestSearchCount);

                        foreach (var point in points)
                        {
                            DrawLine(target, point, Color.green);
                            DrawWireCircle(point, 0.1f, Color.green);
                        }

                        points.Dispose();
                    }
                    else
                    {
                        var nearest = tree.FindNearest(target, out FindNearestSearchCount).Value;
                        DrawLine(target, nearest, Color.green);
                        DrawWireCircle(nearest, 0.1f, Color.green);
                    }

                }
            }
        }
    }

    void DrawNode(in NativeKdTree<float2, TreeComparer>.Iterator itr, Rect bounds, int depth)
    {
        if (!itr.Valid)
            return;

        var left = itr.Left;
        var right = itr.Right;

        int axis = depth % 2;
        switch (axis)
        {
            case 0:
                {
                    float start = bounds.position.x;
                    float split = itr.Value.x;
                    float end = bounds.xMax;

                    DrawVerticalLine(new float2(split, bounds.center.y), bounds.size.y*0.5f, Color.red);

                    if (left.Valid)
                        DrawNode(left, new Rect(new Vector2(start, bounds.position.y), new Vector2(split - start, bounds.size.y)), depth+1);
                    if (right.Valid)
                        DrawNode(right, new Rect(new Vector2(split, bounds.position.y), new Vector2(end - split, bounds.size.y)), depth+1);

                    break;
                }
            case 1:
                {
                    float start = bounds.position.y;
                    float split = itr.Value.y;
                    float end = bounds.yMax;

                    DrawHorizontalLine(new float2(bounds.center.x, split), bounds.size.x * 0.5f, Color.blue);

                    if (left.Valid)
                        DrawNode(left, new Rect(new Vector2(bounds.position.x, start), new Vector2(bounds.size.x, split - start)), depth+1);
                    if (right.Valid)
                        DrawNode(right, new Rect(new Vector2(bounds.position.x, split), new Vector2(bounds.size.x, end - split)), depth+1);
                    break;
                }
        }

    }

    void DrawVerticalLine(float2 origin, float size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(origin.x, origin.y - size, 0), new Vector3(origin.x, origin.y + size, 0));
    }

    void DrawHorizontalLine(float2 origin, float size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(origin.x - size, origin.y, 0), new Vector3(origin.x + size, origin.y, 0));
    }

    void DrawLine(float2 from, float2 to, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(from.asvector3(), to.asvector3());
    }

    void DrawRect(Rect rect, Color color)
    {
        Gizmos.color = color;
        var a = new Vector3(rect.xMin, rect.yMin);
        var b = new Vector3(rect.xMax, rect.yMin);
        var c = new Vector3(rect.xMax, rect.yMax);
        var d = new Vector3(rect.xMin, rect.yMax);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    void DrawWireCircle(float2 origin, float radius, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawWireDisc(origin.asvector3(), Vector3.forward, radius);
#endif
    }
}
