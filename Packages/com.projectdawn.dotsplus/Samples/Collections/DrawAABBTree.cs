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

public class DrawAABBTree : MonoBehaviour
{
    public List<Transform> Transforms;
    public bool IncludeChildTransforms = true;
    public Transform Target;
    public TargetType Type;
    public float Cost;

    public enum TargetType
    {
        Rectangle,
        Line,
    }

    struct Volume : ISurfaceArea<Volume>, IUnion<Volume>
    {
        public Rectangle Rectangle;

        public Volume(Rectangle rectangle)
        {
            Rectangle = rectangle;
        }
        public float SurfaceArea() => Rectangle.Perimeter;
        public Volume Union(Volume value) => new Volume(Rectangle.Union(Rectangle, value.Rectangle));
    }

    private void OnDrawGizmos()
    {
        using (var tree = new UnsafeAABBTree<Volume>(1, Allocator.Temp))
        {
            foreach (var transform in Transforms)
            {
                if (transform == null)
                    continue;
                tree.Add(new Volume(new Rectangle(transform.position.asfloat().xy - 0.5f, 1)));
            }

            if (IncludeChildTransforms)
            {
                var childTransforms = transform.GetComponentsInChildren<Transform>();
                foreach (var transform in childTransforms)
                {
                    if (transform == this.transform)
                        continue;
                    tree.Add(new Volume(new Rectangle(transform.position.asfloat().xy - 0.5f, 1)));
                }
            }

            Cost = tree.Cost();

            if (Target != null)
            {
                var rectangle = new Rectangle(Target.position.asfloat().xy - 0.5f, 1);
                var line = new Line(Target.position.asfloat().xy, Target.position.asfloat().xy + new float2(1, 0));
                var nodesToVitis = new UnsafeStack<UnsafeAABBTree<Volume>.Handle>(1, Allocator.Temp);
                nodesToVitis.Push(tree.Root);
                while (nodesToVitis.TryPop(out var node))
                {
                    if (node.Valid && Type == TargetType.Rectangle && tree[node].Rectangle.Overlap(rectangle))
                    {
                        DrawWireRectangle(tree[node].Rectangle, tree.IsLeaf(node) ? Color.red : Color.gray);
                        nodesToVitis.Push(tree.Left(node));
                        nodesToVitis.Push(tree.Right(node));
                    }
                    else if (node.Valid && Type == TargetType.Line && tree[node].Rectangle.Overlap(line))
                    {
                        DrawWireRectangle(tree[node].Rectangle, tree.IsLeaf(node) ? Color.red : Color.gray);
                        nodesToVitis.Push(tree.Left(node));
                        nodesToVitis.Push(tree.Right(node));
                    }
                    else
                    {
                        DrawNode(tree, node);
                    }
                }
                nodesToVitis.Dispose();

                if (Type == TargetType.Rectangle)
                    DrawWireRectangle(rectangle, Color.yellow);
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
        DrawWireRectangle(tree[node].Rectangle, tree.IsLeaf(node) ? Color.green : Color.white);
        DrawNode(tree, tree.Left(node));
        DrawNode(tree, tree.Right(node));
    }
}
