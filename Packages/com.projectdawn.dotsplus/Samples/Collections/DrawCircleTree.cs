using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectDawn.Collections.LowLevel.Unsafe;
using ProjectDawn.Mathematics;
using ProjectDawn.Geometry2D;
using Unity.Mathematics;
using Unity.Collections;
using static ProjectDawn.Mathematics.math2;
using static ProjectDawn.Geometry2D.ShapeGizmos;
using ProjectDawn.Collections;

public class DrawCircleTree : MonoBehaviour
{
    public List<Transform> Transforms;
    public bool IncludeChildTransforms = true;
    public Transform Target;
    public TargetType Type;
    public float Cost;

    public enum TargetType
    {
        Circle,
        Line,
    }

    struct Volume : ISurfaceArea<Volume>, IUnion<Volume>
    {
        public Circle Circle;

        public Volume(Circle rectangle)
        {
            Circle = rectangle;
        }
        public float SurfaceArea() => Circle.Perimeter;
        public Volume Union(Volume value) => new Volume(Circle.Union(Circle, value.Circle));
    }

    private void OnDrawGizmos()
    {
        using (var tree = new UnsafeAABBTree<Volume>(1, Allocator.Temp))
        {
            foreach (var transform in Transforms)
            {
                if (transform == null)
                    continue;
                tree.Add(new Volume(new Circle(transform.position.asfloat().xy, 1)));
            }

            if (IncludeChildTransforms)
            {
                var childTransforms = transform.GetComponentsInChildren<Transform>();
                foreach (var transform in childTransforms)
                {
                    if (transform == this.transform)
                        continue;
                    tree.Add(new Volume(new Circle(transform.position.asfloat().xy, 1)));
                }
            }

            Cost = tree.Cost();

            if (Target != null)
            {
                var rectangle = new Circle(Target.position.asfloat().xy, 1);
                var line = new Line(Target.position.asfloat().xy, Target.position.asfloat().xy + new float2(1, 0));
                var nodesToVitis = new UnsafeStack<UnsafeAABBTree<Volume>.Handle>(1, Allocator.Temp);
                nodesToVitis.Push(tree.Root);
                while (nodesToVitis.TryPop(out var node))
                {
                    if (node.Valid && Type == TargetType.Circle && tree[node].Circle.Overlap(rectangle))
                    {
                        DrawWireCircle(tree[node].Circle, tree.IsLeaf(node) ? Color.red : Color.gray);
                        nodesToVitis.Push(tree.Left(node));
                        nodesToVitis.Push(tree.Right(node));
                    }
                    else if (node.Valid && Type == TargetType.Line && tree[node].Circle.Overlap(line))
                    {
                        DrawWireCircle(tree[node].Circle, tree.IsLeaf(node) ? Color.red : Color.gray);
                        nodesToVitis.Push(tree.Left(node));
                        nodesToVitis.Push(tree.Right(node));
                    }
                    else
                    {
                        DrawNode(tree, node);
                    }
                }
                nodesToVitis.Dispose();

                if (Type == TargetType.Circle)
                    DrawWireCircle(rectangle, Color.yellow);
                else if (Type == TargetType.Line)
                    DrawLine(line, Color.yellow);
            }
            else
            {
                DrawNode(tree, tree.Root);
            }
        }
    }

    void DrawNode(UnsafeAABBTree<Volume> tree, UnsafeAABBTree<Volume>.Handle node)
    {
        if (!node.Valid)
            return;
        DrawWireCircle(tree[node].Circle, tree.IsLeaf(node) ? Color.green : Color.white);
        DrawNode(tree, tree.Left(node));
        DrawNode(tree, tree.Right(node));
    }
}
