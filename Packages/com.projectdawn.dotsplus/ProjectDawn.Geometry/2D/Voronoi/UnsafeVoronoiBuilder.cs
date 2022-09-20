/*
 * Created by SharpDevelop.
 * User: Burhan
 * Date: 17/06/2014
 * Time: 11:30 م
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
 
 /*
 * The author of this software is Steven Fortune.  Copyright (c) 1994 by AT&T
 * Bell Laboratories.
 * Permission to use, copy, modify, and distribute this software for any
 * purpose without fee is hereby granted, provided that this entire notice
 * is included in all copies of any software which is or includes a copy
 * or modification of this software and in all copies of the supporting
 * documentation for such software.
 * THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
 * WARRANTY.  IN PARTICULAR, NEITHER THE AUTHORS NOR AT&T MAKE ANY
 * REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY
 * OF THIS SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.
 */

/* 
 * This code was originally written by Stephan Fortune in C code.  I, Shane O'Sullivan,
 * have since modified it, encapsulating it in a C++ class and, fixing memory leaks and
 * adding accessors to the Voronoi Edges.
 * Permission to use, copy, modify, and distribute this software for any
 * purpose without fee is hereby granted, provided that this entire notice
 * is included in all copies of any software which is or includes a copy
 * or modification of this software and in all copies of the supporting
 * documentation for such software.
 * THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
 * WARRANTY.  IN PARTICULAR, NEITHER THE AUTHORS NOR AT&T MAKE ANY
 * REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY
 * OF THIS SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.
 */

/* 
 * Java Version by Zhenyu Pan
 * Permission to use, copy, modify, and distribute this software for any
 * purpose without fee is hereby granted, provided that this entire notice
 * is included in all copies of any software which is or includes a copy
 * or modification of this software and in all copies of the supporting
 * documentation for such software.
 * THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
 * WARRANTY.  IN PARTICULAR, NEITHER THE AUTHORS NOR AT&T MAKE ANY
 * REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY
 * OF THIS SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.
 */
 
 /*
 * C# Version by Burhan Joukhadar
 * 
 * Permission to use, copy, modify, and distribute this software for any
 * purpose without fee is hereby granted, provided that this entire notice
 * is included in all copies of any software which is or includes a copy
 * or modification of this software and in all copies of the supporting
 * documentation for such software.
 * THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
 * WARRANTY.  IN PARTICULAR, NEITHER THE AUTHORS NOR AT&T MAKE ANY
 * REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY
 * OF THIS SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.
 */
 
using System;
using System.Collections.Generic;
using ProjectDawn.Collections;
using ProjectDawn.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static ProjectDawn.Mathematics.math2;
using static Unity.Mathematics.math;

namespace ProjectDawn.Geometry2D.LowLevel.Unsafe
{
    /// <summary>
    /// Voronoi builder used for construcing varonoi shapes.
    /// </summary>
    public unsafe struct UnsafeVoronoiBuilder : IDisposable
    {
        UnsafeList<Vertex> m_Vertices;
        UnsafeList<Edge> m_Edges;

        UnsafeLinkedList<HalfEdge> m_HalfEdges;
        UnsafeLinkedList<HalfEdge>.Handle m_LeftHalfEdge;
        UnsafeLinkedList<HalfEdge>.Handle m_RightHalfEdge;

        UnsafePriorityQueue<SiteEvent, SiteComparer> m_SiteEvents;
        UnsafePriorityQueue<IntersectionEvent, IntersectionComparer> m_IntersectionEvents;

        /// <summary>
        /// Returns the number of sites.
        /// </summary>
        public int NumSites => m_SiteEvents.Length;

        public UnsafeVoronoiBuilder(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Vertices = new UnsafeList<Vertex>(initialCapacity << 1, allocator);
            m_Edges = new UnsafeList<Edge>((initialCapacity << 2) + (initialCapacity << 1), allocator);

            m_HalfEdges = new UnsafeLinkedList<HalfEdge>((initialCapacity << 2) + (initialCapacity << 1), allocator);
            m_LeftHalfEdge = default;
            m_RightHalfEdge = default;

            m_SiteEvents = new UnsafePriorityQueue<SiteEvent, SiteComparer>(initialCapacity, allocator);
            m_IntersectionEvents = new UnsafePriorityQueue<IntersectionEvent, IntersectionComparer>((initialCapacity << 2) + (initialCapacity << 1), allocator);
        }

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public static UnsafeVoronoiBuilder* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeVoronoiBuilder* data = AllocatorManager.Allocate<UnsafeVoronoiBuilder>(allocator);

            data->m_Vertices = new UnsafeList<Vertex>(initialCapacity << 2, allocator);
            data->m_Edges = new UnsafeList<Edge>((initialCapacity << 2) + (initialCapacity << 1), allocator);

            data->m_HalfEdges = new UnsafeLinkedList<HalfEdge>((initialCapacity << 2) + (initialCapacity << 1), allocator);
            data->m_LeftHalfEdge = default;
            data->m_RightHalfEdge = default;

            data->m_SiteEvents = new UnsafePriorityQueue<SiteEvent, SiteComparer>(initialCapacity, allocator);
            data->m_IntersectionEvents = new UnsafePriorityQueue<IntersectionEvent, IntersectionComparer>((initialCapacity << 2) + (initialCapacity << 1), allocator);

            return data;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        public static void Destroy(UnsafeVoronoiBuilder* data)
        {
            CollectionChecks.CheckNull(data);
            var allocator = data->m_Vertices.Allocator;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
        }

        /// <summary>
        /// Adds new site into voronoi builder.
        /// Returns false if point already exists.
        /// </summary>
        /// <param name="point">New site point.</param>
        public bool Add(double2 point)
        {
            return m_SiteEvents.EnqueueUnique(new SiteEvent {
                Point = point,
                SiteIndex = m_SiteEvents.Length });
        }

        /// <summary>
        /// Constructs voronoi and outputs the data into the generic structure.
        /// </summary>
        /// <typeparam name="T">Type type of voronoi output.</typeparam>
        /// <param name="output">The output.</param>
        public void Construct<T>(ref T output) where T : IVoronoiOutput
        {
            if (m_Vertices.Length != 0 || m_Edges.Length != 0)
            {
                m_Vertices.Clear();
                m_Edges.Clear();
                m_HalfEdges.Clear();
            }

            int numSites = m_SiteEvents.Length;
            m_Vertices.SetCapacity(numSites << 2);
            m_Edges.SetCapacity((numSites << 2) + (numSites << 1));

            if (numSites > 2)
            {
                FortunesAlgorithm(ref output);
            }
            else if (numSites == 2)
            {
                BruteForceAlgorithm(m_SiteEvents.Dequeue(), m_SiteEvents.Dequeue(), ref output);
            }
            else if (numSites == 1)
            {
                BruteForceAlgorithm(m_SiteEvents.Dequeue(), ref output);
            }
        }

        /// <summary>
        /// Removes all elements of this queue.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_SiteEvents.Clear();
            m_Vertices.Clear();
            m_Edges.Clear();
            m_IntersectionEvents.Clear();
            m_HalfEdges.Clear();
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            m_SiteEvents.Dispose();
            m_Vertices.Dispose();
            m_Edges.Dispose();
            m_IntersectionEvents.Dispose();
            m_HalfEdges.Dispose();
        }

        Edge* CreateSitesBisectingEdge(SiteEvent s1, SiteEvent s2)
        {
            int edgeIndex = m_Edges.Length;
            m_Edges.AddNoResize(new Edge());
            Edge* edge = m_Edges.Ptr + edgeIndex;
            
            edge->RightSite = s1;
            edge->LeftSite = s2;
            
            edge->LeftVertex = null;
            edge->RightVertex = null;
            
            double2 d = s2.Point - s1.Point;

            double2 ad = abs(d);
            edge->c = dot(s1.Point, d) + dot(d, d) * 0.5;
            
            if (ad.x > ad.y)
            {
                edge->a = 1.0;
                edge->b = d.y / d.x;
                edge->c /= d.x;
            }
            else
            {
                edge->a = d.x / d.y;
                edge->b = 1.0;
                edge->c /= d.y;
            }
            
            return edge;
        }

        bool IntersectionHalfEdges(UnsafeLinkedList<HalfEdge>.Handle el1, UnsafeLinkedList<HalfEdge>.Handle el2, out Vertex* vertex)
        {
            vertex = null;

            Edge* e1 = m_HalfEdges.GetUnsafePtr(el1)->Edge;
            Edge* e2 = m_HalfEdges.GetUnsafePtr(el2)->Edge;

            // This can happen with the dummy half edges that created at initialize
            if (e1 == null || e2 == null)
                return false;

            // if the two edges bisect the same parent, return null
            if (e1->LeftSite.SiteIndex == e2->LeftSite.SiteIndex)
                return false;

            double determinant = e1->a * e2->b - e1->b * e2->a;
            if (-1.0e-10 < determinant && determinant < 1.0e-10)
                return false;

            double2 point = new double2(e1->c * e2->b - e2->c * e1->b, e2->c * e1->a - e1->c * e2->a) / determinant;

            UnsafeLinkedList<HalfEdge>.Handle el;
            Edge* e;
            if (e1->LeftSite.CompareTo(e2->LeftSite) == -1)
            {
                el = el1;
                e = e1;
            }
            else
            {
                el = el2;
                e = e2;
            }

            bool right_of_site = point.x >= e->LeftSite.Point.x;
            if ((right_of_site && m_HalfEdges.GetUnsafePtr(el)->Side == HalfEdgeSide.Left)
                || (!right_of_site && m_HalfEdges.GetUnsafePtr(el)->Side == HalfEdgeSide.Right))
                return false;

            // create a new site at the point of intersection - this is a new vector
            // event waiting to happen
            int vertexIndex = m_Vertices.Length;
            m_Vertices.AddNoResize(new Vertex { Point = point, Index = vertexIndex });
            vertex = m_Vertices.Ptr + vertexIndex;
            return true;
        }

        bool right_of(UnsafeLinkedList<HalfEdge>.Handle el, double2 p)
        {
            Edge* e;
            SiteEvent topsite;
            bool right_of_site;
            bool above, fast;
            double dxp, dyp, dxs, t1, t2, t3, yl;

            var ptr = m_HalfEdges.GetUnsafePtr(el);

            e = ptr->Edge;
            topsite = e->LeftSite;

            if (p.x > topsite.Point.x)
                right_of_site = true;
            else
                right_of_site = false;

            if (right_of_site && ptr->Side == HalfEdgeSide.Left)
                return true;
            if (!right_of_site && ptr->Side == HalfEdgeSide.Right)
                return false;

            if (e->a == 1.0)
            {
                dxp = p.x - topsite.Point.x;
                dyp = p.y - topsite.Point.y;
                fast = false;

                if ((!right_of_site & (e->b < 0.0)) | (right_of_site & (e->b >= 0.0)))
                {
                    above = dyp >= e->b * dxp;
                    fast = above;
                }
                else
                {
                    above = p.x + p.y * e->b > e->c;
                    if (e->b < 0.0)
                        above = !above;
                    if (!above)
                        fast = true;
                }
                if (!fast)
                {
                    dxs = topsite.Point.x - (e->RightSite).Point.x;
                    above = e->b * (dxp * dxp - dyp * dyp)
                    < dxs * dyp * (1.0 + 2.0 * dxp / dxs + e->b * e->b);

                    if (e->b < 0)
                        above = !above;
                }
            }
            else // e->b == 1.0
            {
                yl = e->c - e->a * p.x;
                t1 = p.y - yl;
                t2 = p.x - topsite.Point.x;
                t3 = yl - topsite.Point.y;
                above = t1 * t1 > t2 * t2 + t3 * t3;
            }
            return (ptr->Side == HalfEdgeSide.Left ? above : !above);
        }

        void CreateIntersectionEvent(UnsafeLinkedList<HalfEdge>.Handle he, Vertex* v, double offset )
        {
            m_IntersectionEvents.Enqueue(new IntersectionEvent
            { 
                HalfEdge = he,
                Vertex = v,
                YStar = v->Point.y + offset,
            });
        }

        void RemoveIntersectionEvent(UnsafeLinkedList<HalfEdge>.Handle he)
        {
            ref var data = ref m_IntersectionEvents.m_Data;
            for (var itr = data.Begin; itr != data.End; itr = data.Next(itr))
            {
                if (data[itr].HalfEdge == he)
                {
                    data.RemoveAt(itr);
                    return;
                }
            }
        }

        UnsafeLinkedList<HalfEdge>.Handle CreateHalfEdge(UnsafeLinkedList<HalfEdge>.Handle he, Edge* edge, HalfEdgeSide side)
        {
            return m_HalfEdges.Insert(m_HalfEdges.Next(he), new HalfEdge
            {
                Edge = edge,
                Side = side,
            });
        }
        
        UnsafeLinkedList<HalfEdge>.Handle RightHalfEdge(UnsafeLinkedList<HalfEdge>.Handle he) => m_HalfEdges.Next(he);
        UnsafeLinkedList<HalfEdge>.Handle LeftHalfEdge(UnsafeLinkedList<HalfEdge>.Handle he) => m_HalfEdges.Previous(he);
        bool IsDummy(UnsafeLinkedList<HalfEdge>.Handle he) => he == m_LeftHalfEdge || he == m_RightHalfEdge;

        SiteEvent LeftSite(UnsafeLinkedList<HalfEdge>.Handle he) => m_HalfEdges.GetUnsafePtr(he)->LeftSite;

        SiteEvent RightSite(UnsafeLinkedList<HalfEdge>.Handle he) => m_HalfEdges.GetUnsafePtr(he)->RightSite;

        void RemoveHalfEdge(UnsafeLinkedList<HalfEdge>.Handle he)
        {
            m_HalfEdges.RemoveAt(he);
        }
        
        UnsafeLinkedList<HalfEdge>.Handle LeftHalfEdge(double2 point)
        {
            UnsafeLinkedList<HalfEdge>.Handle he = m_LeftHalfEdge;
            
            /* Now search linear list of halfedges for the correct one */
            if ( he == m_LeftHalfEdge || ( he != m_RightHalfEdge && right_of (he, point) ) )
            {
                // keep going right on the list until either the end is reached, or
                // you find the 1st edge which the point isn't to the right of
                do
                {
                    he = m_HalfEdges.Next(he);
                }
                while ( he != m_RightHalfEdge && right_of(he, point) );
                he = m_HalfEdges.Previous(he);
            }
            else
                // if the point is to the left of the HalfEdge, then search left for
                // the HE just to the left of the point
            {
                do
                {
                    he = m_HalfEdges.Previous(he);
                }
                while ( he != m_LeftHalfEdge && !right_of(he, point) );
            }
            
            return he;
        }

        void AddVertexToEdge<T>(Edge* edge, Vertex* vertex, HalfEdgeSide side, ref T output) where T : IVoronoiOutput
        {
            if (side == HalfEdgeSide.Left)
            {
                edge->LeftVertex = vertex;
                if (edge->RightVertex != null)
                    AddEdge(edge, ref output);
            }
            else // if (side == HalfEdgeSide.Right)
            {
                edge->RightVertex = vertex;
                if (edge->LeftVertex != null)
                    AddEdge(edge, ref output);
            }
        }

        void AddEdge<T>(Edge* edge, ref T output) where T : IVoronoiOutput
        {
            output.ProcessEdge(edge->a, edge->b, edge->c, edge->LeftVertexIndex, edge->RightVertexIndex, edge->LeftSite.SiteIndex, edge->RightSite.SiteIndex);
        }

        void FortunesAlgorithm<T>(ref T output) where T : IVoronoiOutput
        {
            // Add dummy half edges to the left and the right side
            m_LeftHalfEdge = m_HalfEdges.Add(new HalfEdge());
            m_RightHalfEdge = m_HalfEdges.Add(new HalfEdge());

            if (m_SiteEvents.TryDequeue(out SiteEvent bottomsite))
                output.ProcessSite(bottomsite.Point, bottomsite.SiteIndex);

            while (true)
            {
                if (!m_SiteEvents.IsEmpty && (m_IntersectionEvents.IsEmpty || m_SiteEvents.Peek().CompareTo(m_IntersectionEvents.Peek()) == -1))
                {
                    SiteEvent e = m_SiteEvents.Dequeue();
                    output.ProcessSite(e.Point, e.SiteIndex);

                    // get the first HalfEdge to the LEFT of the new site
                    UnsafeLinkedList<HalfEdge>.Handle lbnd = LeftHalfEdge(e.Point);
                    // get the first HalfEdge to the RIGHT of the new site
                    UnsafeLinkedList<HalfEdge>.Handle rbnd = RightHalfEdge(lbnd);

                    SiteEvent bot = IsDummy(lbnd) ? bottomsite : RightSite(lbnd);
                    // create a new edge that bisects
                    Edge* edge = CreateSitesBisectingEdge(bot, e);

                    // Left bisector logic
                    UnsafeLinkedList<HalfEdge>.Handle lbisector = CreateHalfEdge(lbnd, edge, HalfEdgeSide.Left);
                    // if the new bisector intersects with the left edge,
                    // remove the left edge's vertex, and put in the new one
                    if ((IntersectionHalfEdges(lbnd, lbisector, out Vertex* vertex)))
                    {
                        RemoveIntersectionEvent(lbnd);
                        CreateIntersectionEvent(lbnd, vertex, distance(vertex->Point, e.Point));
                    }

                    // Right bisector logic
                    UnsafeLinkedList<HalfEdge>.Handle rbisector = CreateHalfEdge(lbisector, edge, HalfEdgeSide.Right);
                    // if this new bisector intersects with the new HalfEdge
                    if ((IntersectionHalfEdges(rbisector, rbnd, out vertex)))
                    {
                        // push the HE into the ordered linked list of vertices
                        CreateIntersectionEvent(rbisector, vertex, distance(vertex->Point, e.Point));
                    }
                }
                else if (!m_IntersectionEvents.IsEmpty)
                {
                    IntersectionEvent e = m_IntersectionEvents.Dequeue();

                    UnsafeLinkedList<HalfEdge>.Handle lbnd = e.HalfEdge;
                    // get the HalfEdge to the left of the above HE
                    UnsafeLinkedList<HalfEdge>.Handle llbnd = LeftHalfEdge(lbnd);
                    // get the HalfEdge to the right of the above HE
                    UnsafeLinkedList<HalfEdge>.Handle rbnd = RightHalfEdge(lbnd);
                    // get the HalfEdge to the right of the HE to the right of the lowest HE
                    UnsafeLinkedList<HalfEdge>.Handle rrbnd = RightHalfEdge(rbnd);

                    // get the Site to the left of the left HE which it bisects
                    SiteEvent bot = IsDummy(lbnd) ? bottomsite : LeftSite(lbnd);
                    // get the Site to the right of the right HE which it bisects
                    SiteEvent top = IsDummy(rbnd) ? bottomsite : RightSite(rbnd);
                    HalfEdgeSide side;
                    if (bot.Point.y > top.Point.y)
                    {
                        // if the site to the left of the event is higher than the site
                        // to the right of it, then swap them
                        swap(ref bot, ref top);
                        side = HalfEdgeSide.Right;
                    }
                    else
                    {
                        side = HalfEdgeSide.Left;
                    }

                    Vertex* eventVertex = e.Vertex; // get the vertex that caused this event
                    eventVertex->Index = output.ProcessVertex(eventVertex->Point);

                    // earlier since we didn't know when it would be processed
                    AddVertexToEdge(m_HalfEdges[lbnd].Edge, eventVertex, m_HalfEdges[lbnd].Side, ref output);
                    // set the endpoint of
                    // the left HalfEdge to be this vector
                    AddVertexToEdge(m_HalfEdges[rbnd].Edge, eventVertex, m_HalfEdges[rbnd].Side, ref output);
                    // set the endpoint of the right HalfEdge to
                    // be this vector
                    RemoveHalfEdge(lbnd); // mark the lowest HE for
                    // deletion - can't delete yet because there might be pointers
                    // to it in Hash Map
                    RemoveIntersectionEvent(rbnd);
                    // remove all vertex events to do with the right HE
                    RemoveHalfEdge(rbnd); // mark the right HE for
                    // deletion - can't delete yet because there might be pointers
                    // to it in Hash Map

                    Edge* edge = CreateSitesBisectingEdge(bot, top); // create an Edge (or line)
                    // that is between the two Sites. This creates the formula of
                    // the line, and assigns a line number to it
                    UnsafeLinkedList<HalfEdge>.Handle bisector = CreateHalfEdge(llbnd, edge, side); // create a HE from the Edge 'e',
                    // right of the left HE
                    AddVertexToEdge(edge, eventVertex, (int)HalfEdgeSide.Right - side, ref output); // set one endpoint to the new edge
                    // to be the vector point 'v'.
                    // If the site to the left of this bisector is higher than the
                    // right Site, then this endpoint
                    // is put in position 0; otherwise in pos 1

                    // if left HE and the new bisector intersect, then delete
                    // the left HE, and reinsert it
                    if ((IntersectionHalfEdges(llbnd, bisector, out Vertex* vertex)))
                    {
                        RemoveIntersectionEvent(llbnd);
                        CreateIntersectionEvent(llbnd, vertex, distance(vertex->Point, bot.Point));
                    }

                    // if right HE and the new bisector intersect, then
                    // reinsert it
                    if ((IntersectionHalfEdges(bisector, rrbnd, out vertex)))
                    {
                        CreateIntersectionEvent(bisector, vertex, distance(vertex->Point, bot.Point));
                    }
                }
                else
                {
                    break;
                }
            }

            for (var lbnd = RightHalfEdge(m_LeftHalfEdge); lbnd != m_RightHalfEdge; lbnd = RightHalfEdge(lbnd))
            {
                AddEdge(m_HalfEdges[lbnd].Edge, ref output);
            }

            output.Build();
        }

        void BruteForceAlgorithm<T>(SiteEvent s0, SiteEvent s1, ref T output) where T : IVoronoiOutput
        {
            output.ProcessSite(s0.Point, s0.SiteIndex);
            output.ProcessSite(s1.Point, s1.SiteIndex);

            Edge* edge = CreateSitesBisectingEdge(s0, s1);
            AddEdge(edge, ref output);

            output.Build();
        }

        void BruteForceAlgorithm<T>(SiteEvent s0, ref T output) where T : IVoronoiOutput
        {
            output.ProcessSite(s0.Point, s0.SiteIndex);;
            output.Build();
        }

        internal struct Vertex
        {
            public double2 Point;
            public int Index;
        }

        internal unsafe struct Edge
        {
            public double a, b, c;
            public Vertex* LeftVertex;
            public Vertex* RightVertex;
            public SiteEvent RightSite;
            public SiteEvent LeftSite;

            public int LeftVertexIndex => LeftVertex != null ? LeftVertex->Index : -1;
            public int RightVertexIndex => RightVertex != null ? RightVertex->Index : -1;
        }

        internal struct SiteEvent
        {
            public double2 Point;
            public int SiteIndex;

            public double2 EventPoint => Point;

            public int CompareTo(SiteEvent value)
            {
                double2 s1 = EventPoint;
                double2 s2 = value.EventPoint;
                if (s1.y < s2.y) return -1;
                if (s1.y > s2.y) return 1;
                if (s1.x < s2.x) return -1;
                if (s1.x > s2.x) return 1;
                return 0;
            }

            public int CompareTo(IntersectionEvent value)
            {
                double2 s1 = EventPoint;
                double2 s2 = value.EventPoint;
                if (s1.y < s2.y) return -1;
                if (s1.y > s2.y) return 1;
                if (s1.x < s2.x) return -1;
                if (s1.x > s2.x) return 1;
                return 0;
            }
        }

        internal enum HalfEdgeSide
        {
            Left,
            Right,
        }

        internal unsafe struct HalfEdge
        {
            public Edge* Edge;
            public HalfEdgeSide Side;

            public SiteEvent LeftSite => Side == HalfEdgeSide.Left ? Edge->RightSite : Edge->LeftSite;
            public SiteEvent RightSite => Side == HalfEdgeSide.Left ? Edge->LeftSite : Edge->RightSite;
        }

        internal unsafe struct IntersectionEvent
        {
            public UnsafeLinkedList<HalfEdge>.Handle HalfEdge;
            public Vertex* Vertex;
            public double YStar;

            public double2 EventPoint => new double2(Vertex->Point.x, YStar);
        }

        internal struct SiteComparer : IComparer<SiteEvent>
        {
            public int Compare(SiteEvent a, SiteEvent b) => a.CompareTo(b);
        }

        internal unsafe struct IntersectionComparer : IComparer<IntersectionEvent>
        {
            public int Compare(IntersectionEvent a, IntersectionEvent b)
            {
                double2 s1 = a.EventPoint;
                double2 s2 = b.EventPoint;
                if (s1.y < s2.y) return -1;
                if (s1.y > s2.y) return 1;
                if (s1.x < s2.x) return -1;
                if (s1.x > s2.x) return 1;
                return 0;
            }
        }
    }
}