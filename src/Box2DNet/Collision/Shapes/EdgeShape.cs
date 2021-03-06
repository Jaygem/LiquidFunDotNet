﻿/*
  Box2DX Copyright (c) 2009 Ihar Kalasouski http://code.google.com/p/box2dx
  Box2D original C++ version Copyright (c) 2006-2009 Erin Catto http://www.gphysics.com

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Box2DNet;
using Box2DNet.Common;

namespace Box2DNet.Collision
{
    public class EdgeShape : Shape
    {
        public Vec2 _v1 { get; set; }
        public Vec2 _v2 { get; set; }

        public float _length;

        public Vec2 _normal;

        public Vec2 _direction;

        // Unit vector halfway between m_direction and m_prevEdge.m_direction:
        public Vec2 _cornerDir1;

        // Unit vector halfway between m_direction and m_nextEdge.m_direction:
        public Vec2 _cornerDir2;

        public bool _cornerConvex1;
        public bool _cornerConvex2;

        public EdgeShape _nextEdge;
        public EdgeShape _prevEdge;
        public Vec2 m_vertex0 { get; set; }
        public bool m_hasVertex0;
        public Vec2 m_vertex3{get;set;}
        public bool m_hasVertex3;
        public Vec2[] Vertices { get; set; }

        public EdgeShape()
		{
			_type = ShapeType.EdgeShape;
			_radius = Settings.PolygonRadius;
            m_vertex0 = new Vec2(0, 0);
            m_hasVertex0 = true;
        }
        public dynamic SetType
        {
            set{ _type = value; }
        }

		public override void Dispose()
		{
			if (_prevEdge != null)
			{
				_prevEdge._nextEdge = null;
			}

			if (_nextEdge != null)
			{
				_nextEdge._prevEdge = null;
			}
		}

		public void Set(Vec2 v1, Vec2 v2)
		{
			_v1 = v1;
			_v2 = v2;
            this.Vertices = new Vec2[] { v1, v2 };
			_direction = _v2 - _v1;
			_length = _direction.Normalize();
			_normal = Vec2.Cross(_direction, 1.0f);

			_cornerDir1 = _normal;
			_cornerDir2 = -1.0f * _normal;
		}

		public override bool TestPoint(XForm transform, Vec2 p)
		{
			return false;
		}

		public override SegmentCollide TestSegment(XForm transform, out float lambda, out Vec2 normal, Segment segment, float maxLambda)
		{
			Vec2 r = segment.P2 - segment.P1;
			Vec2 v1 = Common.MathB2.Mul(transform, _v1);
			Vec2 d = Common.MathB2.Mul(transform, _v2) - v1;
			Vec2 n = Vec2.Cross(d, 1.0f);

			float k_slop = 100.0f * Common.Settings.FLT_EPSILON;
			float denom = -Vec2.Dot(r, n);

			// Cull back facing collision and ignore parallel segments.
			if (denom > k_slop)
			{
				// Does the segment intersect the infinite line associated with this segment?
				Vec2 b = segment.P1 - v1;
				float a = Vec2.Dot(b, n);

				if (0.0f <= a && a <= maxLambda * denom)
				{
					float mu2 = -r.X * b.Y + r.Y * b.X;

					// Does the segment intersect this segment?
					if (-k_slop * denom <= mu2 && mu2 <= denom * (1.0f + k_slop))
					{
						a /= denom;
						n.Normalize();
						lambda = a;
						normal = n;
						return SegmentCollide.HitCollide;
					}
				}
			}

			lambda = 0;
			normal = new Vec2();
			return SegmentCollide.MissCollide;
		}

		public override void ComputeAABB(out AABB aabb, XForm transform)
		{
			Vec2 v1 = Common.MathB2.Mul(transform, _v1);
			Vec2 v2 = Common.MathB2.Mul(transform, _v2);

			Vec2 r = new Vec2(_radius, _radius);
			aabb.LowerBound = Common.MathB2.Min(v1, v2) - r;
			aabb.UpperBound = Common.MathB2.Max(v1, v2) + r;
		}

		public override void ComputeMass(out MassData massData, float density)
		{
			massData.Mass = 0.0f;
			massData.Center = _v1;
			massData.I = 0.0f;
		}

		public void SetPrevEdge(EdgeShape edge, Vec2 cornerDir, bool convex)
		{
			_prevEdge = edge;
			_cornerDir1 = cornerDir;
			_cornerConvex1 = convex;
		}

		public void SetNextEdge(EdgeShape edge, Vec2 cornerDir, bool convex)
		{
			_nextEdge = edge;
			_cornerDir2 = cornerDir;
			_cornerConvex2 = convex;
		}

		public override float ComputeSubmergedArea(Vec2 normal, float offset, XForm xf, out Vec2 c)
		{
			//Note that v0 is independent of any details of the specific edge
			//We are relying on v0 being consistent between multiple edges of the same body
			Vec2 v0 = offset * normal;
			//Vec2 v0 = xf.position + (offset - b2Dot(normal, xf.position)) * normal;

			Vec2 v1 = Common.MathB2.Mul(xf, _v1);
			Vec2 v2 = Common.MathB2.Mul(xf, _v2);

			float d1 = Vec2.Dot(normal, v1) - offset;
			float d2 = Vec2.Dot(normal, v2) - offset;

			if (d1 > 0.0f)
			{
				if (d2 > 0.0f)
				{
					c = new Vec2();
					return 0.0f;
				}
				else
				{
					v1 = -d2 / (d1 - d2) * v1 + d1 / (d1 - d2) * v2;
				}
			}
			else
			{
				if (d2 > 0.0f)
				{
					v2 = -d2 / (d1 - d2) * v1 + d1 / (d1 - d2) * v2;
				}
				else
				{
					//Nothing
				}
			}

			// v0,v1,v2 represents a fully submerged triangle
			float k_inv3 = 1.0f / 3.0f;

			// Area weighted centroid
			c = k_inv3 * (v0 + v1 + v2);

			Vec2 e1 = v1 - v0;
			Vec2 e2 = v2 - v0;

			return 0.5f * Vec2.Cross(e1, e2);
		}

        internal bool RayCast(RayCastOutput output, RayCastInput input, XForm xf, int v)
        {
            Vec2 p1 = MathB2.MulT(xf.R, input.P1 - xf.Position);
            Vec2 p2 = MathB2.MulT(xf.R, input.P2 - xf.Position);
            Vec2 d = p2 - p1;

            Vec2 v1 = _v1;
            Vec2 v2 = _v2;
            Vec2 e = v2 - v1;
            Vec2 normal = new Vec2(e.Y, -e.X);
            normal.Normalize();

            // q = p1 + t * d
            // dot(normal, q - v1) = 0
            // dot(normal, p1 - v1) + t * dot(normal, d) = 0
            float numerator = MathB2.Dot(normal, v1 - p1);
            float denominator = MathB2.Dot(normal, d);

            if (denominator == 0.0f)
            {
                return false;
            }

            float t = numerator / denominator;
            if (t < 0.0f || input.MaxFraction < t)
            {
                return false;
            }

            Vec2 q = p1 + t * d;

            // q = v1 + s * r
            // s = dot(q - v1, r) / dot(r, r)
            Vec2 r = v2 - v1;
            float rr = MathB2.Dot(r, r);
            if (rr == 0.0f)
            {
                return false;
            }

            float s = MathB2.Dot(q - v1, r) / rr;
            if (s < 0.0f || 1.0f < s)
            {
                return false;
            }

            output.Fraction = t;
            if (numerator > 0.0f)
            {
                output.Normal = -MathB2.Mul(xf.R, normal);
            }
            else
            {
                output.Normal = MathB2.Mul(xf.R, normal);
            }
            return true;
        }

        public float Length
		{
			get { return _length; }
		}
		public Vec2 Vertex1
		{
			get { return _v1; }
		}

		public Vec2 Vertex2
		{
			get { return _v2; }
		}

		public Vec2 NormalVector
		{
			get { return _normal; }
		}

		public Vec2 DirectionVector
		{
			get { return _direction; }
		}

		public Vec2 Corner1Vector
		{
			get { return _cornerDir1; }
		}

		public Vec2 Corner2Vector
		{
			get { return _cornerDir2; }
		}

		public override int GetSupport(Vec2 d)
		{
			return Vec2.Dot(_v1, d) > Vec2.Dot(_v2, d) ? 0 : 1;
		}

		public override Vec2 GetSupportVertex(Vec2 d)
		{
			return Vec2.Dot(_v1, d) > Vec2.Dot(_v2, d) ? _v1 : _v2;
		}

		public override Vec2 GetVertex(int index)
		{
			Box2DNetDebug.Assert(0 <= index && index < 2);
			if (index == 0) return _v1;
			else return _v2;
		}

		public bool Corner1IsConvex
		{
			get { return _cornerConvex1; }
		}

		public bool Corner2IsConvex
		{
			get { return _cornerConvex2; }
		}

		public override float ComputeSweepRadius(Vec2 pivot)
		{
			float ds1 = Vec2.DistanceSquared(_v1, pivot);
			float ds2 = Vec2.DistanceSquared(_v2, pivot);
			return Common.MathB2.Sqrt(Common.MathB2.Max(ds1, ds2));
		}
        public void ComputeDistance(XForm xf, Vec2 p, float distance, Vec2 normal, int childIndex)
        {

            Vec2 v1 = MathB2.Mul(xf, _v1);
            Vec2 v2 = MathB2.Mul(xf, _v2);
            
            Vec2 d = p - v1;
            Vec2 s = v2 - v1;
            float ds = MathB2.Dot(d, s);
            if (ds > 0)
            {
                float s2 = MathB2.Dot(s, s);
                if (ds > s2)
                {
                    d = p - v2;
                }
                else
                {
                    d -= ds / s2 * s;
                }
            }

            float d1 = d.Length();
            distance = d1;
            normal = d1 > 0 ? 1 / d1 * d : new Vec2(0,0);

        }
    }
}
