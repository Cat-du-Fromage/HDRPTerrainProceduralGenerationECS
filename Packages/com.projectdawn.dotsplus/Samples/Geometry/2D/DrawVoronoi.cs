using UnityEngine;
using Unity.Collections;
using ProjectDawn.Mathematics;
using ProjectDawn.Geometry2D;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static ProjectDawn.Mathematics.math2;
using static ProjectDawn.Geometry2D.ShapeGizmos;

public class DrawVoronoi : MonoBehaviour
{
    public List<Transform> Transforms;
    public bool IncludeChildTransforms = true;
    public Rectangle Bounds;

    void OnDrawGizmos()
    {
        var bounds = new Rectangle(Bounds.Position + transform.position.asfloat().xy, Bounds.Size);
        DrawWireRectangle(bounds, Color.white);

        using (var builder = new VoronoiBuilder(1, Allocator.Temp))
        {
            if (Transforms != null)
            {
                foreach (var transform in Transforms)
                {
                    if (transform == null)
                        continue;
                    builder.Add(new float2(transform.position.x, transform.position.y));
                }
            }

            if (IncludeChildTransforms)
            {
                var childTransforms = transform.GetComponentsInChildren<Transform>();
                foreach (var transform in childTransforms)
                {
                    if (transform == this.transform)
                        continue;
                    builder.Add(new float2(transform.position.x, transform.position.y));
                }
            }

            var diagram = new VoronoiDiagram(bounds, Allocator.Temp);
            {
                builder.Construct(ref diagram);

                for (int cellIndex = 0; cellIndex < diagram.Cells.Length; ++cellIndex)
                {
                    var cell = diagram.Cells[cellIndex];
                    var color = Color.HSVToRGB((float)cellIndex / diagram.Cells.Length, 1, 1);
                    color.a = 0.3f;
                    DrawVoronoiCell(diagram, diagram.Cells[cellIndex], color);
                    ShapeGizmos.DrawText(cell.Site, $"{(char)(65 + cellIndex)}", Color.white);
                }
            }
            diagram.Dispose();
        }
    }

    static void DrawVoronoiCell(in VoronoiDiagram voronoiDiagram, VoronoiCell cell, Color color)
    {
#if UNITY_EDITOR
        int count = 0;
        for (var itr = cell.Begin; itr != cell.End; itr.MoveNext())
            count++;

        if (count == 0)
            return;

        var points = new Vector3[count];
        var index = 0;
        for (var itr = cell.Begin; itr != cell.End; itr.MoveNext())
            points[index++] = voronoiDiagram.Vertices[itr.Value.ToVertexIndex].Point.asvector3();

        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawAAConvexPolygon(points);
#endif
    }
}
