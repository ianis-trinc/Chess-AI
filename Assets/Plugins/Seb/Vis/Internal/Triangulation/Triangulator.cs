using System.Collections.Generic;
using UnityEngine;

namespace Seb.Vis.Internal.Triangulation
{

	public static class Triangulator
	{


		public static (Vector2[] verts, int[] indices) Triangulate(Vector2[] polygonPoints)
		{
			return Triangulate(new Polygon(polygonPoints));
		}

		public static (Vector2[] verts, int[] indices) Triangulate(Polygon polygon)
		{
			int numHoleToHullConnectionVerts = 2 * polygon.numHoles;
			int totalNumVerts = polygon.numPoints + numHoleToHullConnectionVerts;
			int[] tris = new int[(totalNumVerts - 2) * 3];
			LinkedList<Vertex> vertsInClippedPolygon = GenerateVertexList(polygon);
			int triIndex = 0;

			while (vertsInClippedPolygon.Count >= 3)
			{
				bool hasRemovedEarThisIteration = false;
				LinkedListNode<Vertex> vertexNode = vertsInClippedPolygon.First;
				for (int i = 0; i < vertsInClippedPolygon.Count; i++)
				{
					LinkedListNode<Vertex> prevVertexNode = vertexNode.Previous ?? vertsInClippedPolygon.Last;
					LinkedListNode<Vertex> nextVertexNode = vertexNode.Next ?? vertsInClippedPolygon.First;

					if (vertexNode.Value.isConvex)
					{
						if (!TriangleContainsVertex(prevVertexNode.Value, vertexNode.Value, nextVertexNode.Value, vertsInClippedPolygon))
						{
							// check if removal of ear makes prev/next vertex convex (if was previously reflex)
							if (!prevVertexNode.Value.isConvex)
							{
								LinkedListNode<Vertex> prevOfPrev = prevVertexNode.Previous ?? vertsInClippedPolygon.Last;

								prevVertexNode.Value.isConvex = IsConvex(prevOfPrev.Value.position, prevVertexNode.Value.position, nextVertexNode.Value.position);
							}
							if (!nextVertexNode.Value.isConvex)
							{
								LinkedListNode<Vertex> nextOfNext = nextVertexNode.Next ?? vertsInClippedPolygon.First;
								nextVertexNode.Value.isConvex = IsConvex(prevVertexNode.Value.position, nextVertexNode.Value.position, nextOfNext.Value.position);
							}

							tris[triIndex * 3 + 2] = prevVertexNode.Value.index;
							tris[triIndex * 3 + 1] = vertexNode.Value.index;
							tris[triIndex * 3] = nextVertexNode.Value.index;
							triIndex++;

							hasRemovedEarThisIteration = true;
							vertsInClippedPolygon.Remove(vertexNode);
							break;
						}
					}


					vertexNode = nextVertexNode;
				}

				if (!hasRemovedEarThisIteration)
				{
					Debug.LogError("Error triangulating mesh. Aborted.");
					return (null, null);
				}
			}
			return (polygon.points, tris);
		}

		static LinkedList<Vertex> GenerateVertexList(Polygon polygon)
		{
			LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
			LinkedListNode<Vertex> currentNode = null;

			for (int i = 0; i < polygon.numHullPoints; i++)
			{
				int prevPointIndex = (i - 1 + polygon.numHullPoints) % polygon.numHullPoints;
				int nextPointIndex = (i + 1) % polygon.numHullPoints;

				bool vertexIsConvex = IsConvex(polygon.points[prevPointIndex], polygon.points[i], polygon.points[nextPointIndex]);
				Vertex currentHullVertex = new Vertex(polygon.points[i], i, vertexIsConvex);

				if (currentNode == null)
					currentNode = vertexList.AddFirst(currentHullVertex);
				else
					currentNode = vertexList.AddAfter(currentNode, currentHullVertex);
			}

			List<HoleData> sortedHoleData = new List<HoleData>();

			for (int holeIndex = 0; holeIndex < polygon.numHoles; holeIndex++)
			{
				Vector2 holeBridgePoint = new Vector2(float.MinValue, 0);
				int holeBridgeIndex = 0;
				for (int i = 0; i < polygon.numPointsPerHole[holeIndex]; i++)
				{
					if (polygon.GetHolePoint(i, holeIndex).x > holeBridgePoint.x)
					{
						holeBridgePoint = polygon.GetHolePoint(i, holeIndex);
						holeBridgeIndex = i;

					}
				}
				sortedHoleData.Add(new HoleData(holeIndex, holeBridgeIndex, holeBridgePoint));
			}
			sortedHoleData.Sort((x, y) => (x.bridgePoint.x > y.bridgePoint.x) ? -1 : 1);

			foreach (HoleData holeData in sortedHoleData)
			{

				Vector2 rayIntersectPoint = new Vector2(float.MaxValue, holeData.bridgePoint.y);
				List<LinkedListNode<Vertex>> hullNodesPotentiallyInBridgeTriangle = new List<LinkedListNode<Vertex>>();
				LinkedListNode<Vertex> initialBridgeNodeOnHull = null;
				currentNode = vertexList.First;
				while (currentNode != null)
				{
					LinkedListNode<Vertex> nextNode = (currentNode.Next == null) ? vertexList.First : currentNode.Next;
					Vector2 p0 = currentNode.Value.position;
					Vector2 p1 = nextNode.Value.position;

					if (p0.x > holeData.bridgePoint.x || p1.x > holeData.bridgePoint.x)
					{
						if (p0.y > holeData.bridgePoint.y != p1.y > holeData.bridgePoint.y)
						{
							float rayIntersectX = p1.x;
							if (!Mathf.Approximately(p0.x, p1.x))
							{
								float intersectY = holeData.bridgePoint.y;
								float gradient = (p0.y - p1.y) / (p0.x - p1.x);
								float c = p1.y - gradient * p1.x;
								rayIntersectX = (intersectY - c) / gradient;
							}

							if (rayIntersectX > holeData.bridgePoint.x)
							{
								LinkedListNode<Vertex> potentialNewBridgeNode = (p0.x > p1.x) ? currentNode : nextNode;

								bool isDuplicateEdge = Mathf.Approximately(rayIntersectX, rayIntersectPoint.x);

								bool connectToThisDuplicateEdge = holeData.bridgePoint.y > potentialNewBridgeNode.Previous.Value.position.y;

								if (!isDuplicateEdge || connectToThisDuplicateEdge)
								{
									if (rayIntersectX < rayIntersectPoint.x || isDuplicateEdge)
									{
										rayIntersectPoint.x = rayIntersectX;
										initialBridgeNodeOnHull = potentialNewBridgeNode;
									}
								}
							}
						}
					}

					if (currentNode != initialBridgeNodeOnHull)
					{
						if (!currentNode.Value.isConvex && p0.x > holeData.bridgePoint.x)
						{
							hullNodesPotentiallyInBridgeTriangle.Add(currentNode);
						}
					}
					currentNode = currentNode.Next;
				}


				LinkedListNode<Vertex> validBridgeNodeOnHull = initialBridgeNodeOnHull;
				foreach (LinkedListNode<Vertex> nodePotentiallyInTriangle in hullNodesPotentiallyInBridgeTriangle)
				{
					if (nodePotentiallyInTriangle.Value.index == initialBridgeNodeOnHull.Value.index)
					{
						continue;
					}

					if (TriangleContainsPoint(holeData.bridgePoint, rayIntersectPoint, initialBridgeNodeOnHull.Value.position, nodePotentiallyInTriangle.Value.position))
					{
						bool isDuplicatePoint = validBridgeNodeOnHull.Value.position == nodePotentiallyInTriangle.Value.position;

						float currentDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - validBridgeNodeOnHull.Value.position.y);
						float pointInTriDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - nodePotentiallyInTriangle.Value.position.y);

						if (pointInTriDstFromHoleBridgeY < currentDstFromHoleBridgeY || isDuplicatePoint)
						{
							validBridgeNodeOnHull = nodePotentiallyInTriangle;

						}
					}
				}

				currentNode = validBridgeNodeOnHull;
				for (int i = holeData.bridgeIndex; i <= polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex; i++)
				{
					int previousIndex = currentNode.Value.index;
					int currentIndex = polygon.IndexOfPointInHole(i % polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);
					int nextIndex = polygon.IndexOfPointInHole((i + 1) % polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);

					if (i == polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex)
					{
						nextIndex = validBridgeNodeOnHull.Value.index;
					}

					bool vertexIsConvex = IsConvex(polygon.points[previousIndex], polygon.points[currentIndex], polygon.points[nextIndex]);
					Vertex holeVertex = new Vertex(polygon.points[currentIndex], currentIndex, vertexIsConvex);
					currentNode = vertexList.AddAfter(currentNode, holeVertex);
				}

				Vector2 nextVertexPos = (currentNode.Next == null) ? vertexList.First.Value.position : currentNode.Next.Value.position;
				bool isConvex = IsConvex(holeData.bridgePoint, validBridgeNodeOnHull.Value.position, nextVertexPos);
				Vertex repeatStartHullVert = new Vertex(validBridgeNodeOnHull.Value.position, validBridgeNodeOnHull.Value.index, isConvex);
				vertexList.AddAfter(currentNode, repeatStartHullVert);

				LinkedListNode<Vertex> nodeBeforeStartBridgeNodeOnHull = (validBridgeNodeOnHull.Previous == null) ? vertexList.Last : validBridgeNodeOnHull.Previous;
				LinkedListNode<Vertex> nodeAfterStartBridgeNodeOnHull = (validBridgeNodeOnHull.Next == null) ? vertexList.First : validBridgeNodeOnHull.Next;
				validBridgeNodeOnHull.Value.isConvex = IsConvex(nodeBeforeStartBridgeNodeOnHull.Value.position, validBridgeNodeOnHull.Value.position, nodeAfterStartBridgeNodeOnHull.Value.position);
			}
			return vertexList;
		}


		static bool TriangleContainsVertex(Vertex v0, Vertex v1, Vertex v2, LinkedList<Vertex> vertsInClippedPolygon)
		{
			LinkedListNode<Vertex> vertexNode = vertsInClippedPolygon.First;
			for (int i = 0; i < vertsInClippedPolygon.Count; i++)
			{
				if (!vertexNode.Value.isConvex)
				{
					Vertex vertexToCheck = vertexNode.Value;
					if (vertexToCheck.index != v0.index && vertexToCheck.index != v1.index && vertexToCheck.index != v2.index) // dont check verts that make up triangle
					{
						if (TriangleContainsPoint(v0.position, v1.position, v2.position, vertexToCheck.position))
						{
							return true;
						}
					}
				}
				vertexNode = vertexNode.Next;
			}

			return false;
		}


		static bool IsConvex(Vector2 v0, Vector2 v1, Vector2 v2)
		{
			return !IsClockwise(v0, v1, v2);
		}

		public static bool IsClockwise(Vector2 a, Vector2 b, Vector2 c)
		{
			return (c.x - a.x) * (-b.y + a.y) + (c.y - a.y) * (b.x - a.x) < 0;
		}

		static bool TriangleContainsPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
		{
			float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
			float s = 1 / (2 * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
			float t = 1 / (2 * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
			return s >= 0 && t >= 0 && (s + t) <= 1;
		}

		public struct HoleData
		{
			public readonly int holeIndex;
			public readonly int bridgeIndex;
			public readonly Vector2 bridgePoint;

			public HoleData(int holeIndex, int bridgeIndex, Vector2 bridgePoint)
			{
				this.holeIndex = holeIndex;
				this.bridgeIndex = bridgeIndex;
				this.bridgePoint = bridgePoint;
			}
		}

		public class Vertex
		{
			public readonly Vector2 position;
			public readonly int index;
			public bool isConvex;

			public Vertex(Vector2 position, int index, bool isConvex)
			{
				this.position = position;
				this.index = index;
				this.isConvex = isConvex;
			}
		}
	}

}