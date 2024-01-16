using UnityEngine;

namespace Seb
{
	public static class Maths
	{

		public const float PI = 3.1415926f;
		public const float TAU = 2 * PI;
		public const float Epsilon = 1.175494351E-38f;

		// ------ # Random Point Generation ------

		public static Vector2 RandomPointInCircle(System.Random rng)
		{
			Vector2 pointOnCircle = RandomPointInCircle(rng);
			float r = Mathf.Sqrt((float)rng.NextDouble());
			return pointOnCircle * r;
		}

		public static Vector2 RandomPointOnCircle(System.Random rng)
		{
			float angle = (float)rng.NextDouble() * 2 * PI;
			float x = Mathf.Cos(angle);
			float y = Mathf.Sin(angle);
			return new Vector2(x, y);
		}

		public static Vector3 RandomPointOnSphere(System.Random rng)
		{
			float x = RandomNormal(rng, 0, 1);
			float y = RandomNormal(rng, 0, 1);
			float z = RandomNormal(rng, 0, 1);
			return new Vector3(x, y, z).normalized;
		}

		public static Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c, System.Random rng)
		{
			double randA = rng.NextDouble();
			double randB = rng.NextDouble();
			if (randA + randB > 1)
			{
				randA = 1 - randA;
				randB = 1 - randB;
			}
			return a + (b - a) * (float)randA + (c - a) * (float)randB;
		}


		// ------ # Intersection and distance tests ------

		public static RaySphereResult RayIntersectsSphere(Vector3 rayOrigin, Vector3 rayDir, Vector3 centre, float radius)
		{
			Vector3 offset = rayOrigin - centre;
			const float a = 1;
			float b = 2 * Vector3.Dot(offset, rayDir);
			float c = Vector3.Dot(offset, offset) - radius * radius;
			float d = b * b - 4 * c; // Discriminant from quadratic formula

			// Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
			if (d > 0)
			{
				float s = Mathf.Sqrt(d);
				float dstToSphereNear = Mathf.Max(0, -b - s) / (2 * a);
				float dstToSphereFar = (-b + s) / (2 * a);

				// Ignore intersections that occur behind the ray
				if (dstToSphereFar >= 0)
				{
					return new RaySphereResult()
					{
						intersects = true,
						dstToSphere = dstToSphereNear,
						dstThroughSphere = dstToSphereFar - dstToSphereNear
					};
				}
			}
			// Ray did not intersect sphere
			return new RaySphereResult()
			{
				intersects = false,
				dstToSphere = Mathf.Infinity,
				dstThroughSphere = 0
			};
		}


		public static Vector3 ClosestPointOnLineSegment(Vector3 p, Vector3 a1, Vector3 a2)
		{
			Vector3 lineDelta = a2 - a1;
			Vector3 pointDelta = p - a1;
			float sqrLineLength = lineDelta.sqrMagnitude;

			if (sqrLineLength == 0)
				return a1;

			float t = Mathf.Clamp01(Vector3.Dot(pointDelta, lineDelta) / sqrLineLength);
			return a1 + lineDelta * t;
		}

		public static float DistanceToLineSegment(Vector3 p, Vector3 a1, Vector3 a2)
		{
			Vector3 closestPoint = ClosestPointOnLineSegment(p, a1, a2);
			return (p - closestPoint).magnitude;
		}

		public static (bool intersects, Vector2 point) LineIntersectsLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			float d = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
			// Check if parallel
			if (ApproximatelyEqual(d, 0))
			{
				return (false, Vector2.zero);
			}

			float n = (a1.x - b1.x) * (b1.y - b2.y) - (a1.y - b1.y) * (b1.x - b2.x);
			float t = n / d;
			Vector2 intersectionPoint = a1 + (a2 - a1) * t;
			return (true, intersectionPoint);
		}

		public static (bool intersects, Vector2 point) RayIntersectsLine(Vector2 rayOrigin, Vector2 rayDir, Vector2 b1, Vector2 b2)
		{
			Vector2 a1 = rayOrigin;
			Vector2 a2 = a1 + rayDir;
			float d = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
			// Check if parallel
			if (ApproximatelyEqual(d, 0))
			{
				return (false, Vector2.zero);
			}

			float n = (a1.x - b1.x) * (b1.y - b2.y) - (a1.y - b1.y) * (b1.x - b2.x);
			float t = n / d;
			Vector2 intersectionPoint = rayOrigin + rayDir * t;
			bool intersectsInFrontOfRay = t >= 0;
			return (intersectsInFrontOfRay, intersectionPoint);
		}

		public static int SideOfLine(Vector2 p, Vector2 a, Vector2 b)
		{
			float r = (b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y);
			return System.Math.Sign(r);
		}

		public static bool PointOnSameSideOfLine(Vector2 p1, Vector2 p2, Vector2 a1, Vector2 a2)
		{
			return SideOfLine(p1, a1, a2) == SideOfLine(p2, a1, a2);
		}

		// ------ # Miscellaneous ------

		public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
		{
			// Thanks to https://math.stackexchange.com/a/1951650
			Vector3 ortho = Vector3.Cross(c - a, b - a);
			float parallogramArea = ortho.magnitude;
			return parallogramArea * 0.5f;
		}

		public static float TriangleAreaSigned2D(Vector2 a, Vector2 b, Vector2 c)
		{
			return 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
		}

		public static bool TriangleContainsPoint(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
		{
			// Thanks to https://stackoverflow.com/a/14382692
			float area = TriangleAreaSigned2D(a, b, c);
			float s = (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y) * Mathf.Sign(area);
			float t = (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y) * Mathf.Sign(area);
			return s >= 0 && t >= 0 && s + t < 2 * Mathf.Abs(area);
		}


		public static bool TriangleIsClockwise(Vector2 a, Vector2 b, Vector2 c)
		{
			return TriangleAreaSigned2D(a, b, c) < 0;
		}

		public static float RandomNormal(System.Random rng, float mean = 0, float standardDeviation = 1)
		{
			float theta = 2 * Mathf.PI * (float)rng.NextDouble();
			float rho = Mathf.Sqrt(-2 * Mathf.Log((float)rng.NextDouble()));
			float scale = standardDeviation * rho;
			return mean + scale * Mathf.Cos(theta);
		}

		public static int WeightedRandomIndex(System.Random prng, float[] weights)
		{
			float weightSum = 0;
			for (int i = 0; i < weights.Length; i++)
			{
				weightSum += weights[i];
			}

			float randomValue = (float)prng.NextDouble() * weightSum;
			float cumul = 0;

			for (int i = 0; i < weights.Length; i++)
			{
				cumul += weights[i];
				if (randomValue < cumul)
				{
					return i;
				}
			}

			return weights.Length - 1;
		}

		public static float ArcLengthBetweenPointsOnUnitSphere(Vector3 a, Vector3 b)
		{
			return Mathf.Atan2(Vector3.Cross(a, b).magnitude, Vector3.Dot(a, b));
		}

		public static float ArcLengthBetweenPointsOnSphere(Vector3 a, Vector3 b, float sphereRadius)
		{
			return ArcLengthBetweenPointsOnUnitSphere(a.normalized, b.normalized) * sphereRadius;
		}

		public static Vector3[] GetPointsOnSphereSurface(int numPoints, float radius = 1)
		{
			Vector3[] points = new Vector3[numPoints];
			const double goldenRatio = 1.618033988749894; // (1 + sqrt(5)) / 2
			const double angleIncrement = System.Math.PI * 2 * goldenRatio;

			System.Threading.Tasks.Parallel.For(0, numPoints, i =>
			{
				double t = (double)i / numPoints;
				double inclination = System.Math.Acos(1 - 2 * t);
				double azimuth = angleIncrement * i;

				double x = System.Math.Sin(inclination) * System.Math.Cos(azimuth);
				double y = System.Math.Sin(inclination) * System.Math.Sin(azimuth);
				double z = System.Math.Cos(inclination);
				points[i] = new Vector3((float)x, (float)y, (float)z) * radius;
			});
			return points;
		}

		public static (Vector2 centre, Vector2 size) BoundingBox(Vector2[] points)
		{
			if (points.Length == 0)
			{
				return (Vector2.zero, Vector2.zero);
			}

			Vector2 min = points[0];
			Vector2 max = points[0];
			for (int i = 1; i < points.Length; i++)
			{
				Vector2 p = points[i];
				min = new Vector2(Min(min.x, p.x), Min(min.y, p.y));
				max = new Vector2(Max(max.x, p.x), Max(max.y, p.y));
			}

			Vector2 centre = (min + max) / 2;
			Vector2 size = max - min;
			return (centre, size);
		}



		public static bool ApproximatelyEqual(float a, float b) => System.Math.Abs(a - b) < Epsilon;
		public static float Min(float a, float b) => a < b ? a : b;
		public static float Max(float a, float b) => a > b ? a : b;
		
		public struct RaySphereResult
		{
			public bool intersects;
			public float dstToSphere;
			public float dstThroughSphere;
		}
	}
}