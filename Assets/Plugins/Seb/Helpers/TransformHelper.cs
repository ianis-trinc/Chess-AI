using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb
{

	public static class TransformHelper
	{

		public static Vector3[] GetAllChildPositions(Transform parent)
		{
			Vector3[] positions = new Vector3[parent.childCount];
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = parent.GetChild(i).position;
			}
			return positions;
		}

		public static Vector2[] GetAllChildPositions2D(Transform parent, float z = 0)
		{
			Vector2[] positions = new Vector2[parent.childCount];
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = parent.GetChild(i).position;
			}
			return positions;
		}


		public static Transform[] GetAllChildTransforms(Transform parent)
		{
			Transform[] children = new Transform[parent.childCount];

			for (int i = 0; i < children.Length; i++)
			{
				children[i] = parent.GetChild(i);
			}
			return children;
		}

	}
}
