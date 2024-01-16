using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb
{

	public static class ArrayHelper
	{

		public static void ShuffleArray<T>(T[] array, System.Random rng)
		{
			for (int i = 0; i < array.Length - 1; i++)
			{
				int randomIndex = rng.Next(i, array.Length);
				(array[randomIndex], array[i]) = (array[i], array[randomIndex]); // Swap
			}
		}

		public static int[] CreateShuffledIndexArray(int length, System.Random rng)
		{
			int[] indexArray = CreateIndexArray(length);
			ShuffleArray(indexArray, rng);
			return indexArray;
		}

		public static int[] CreateIndexArray(int length)
		{
			int[] array = new int[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = i;
			}

			return array;
		}

		public static void SortByScores<ItemType, ScoreType>(ItemType[] items, ScoreType[] scores, bool ascending) where ScoreType : System.IComparable
		{
			for (int i = 0; i < items.Length - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					int comparison = scores[swapIndex].CompareTo(scores[j]);
					bool swap = ascending ? comparison > 0 : comparison < 0;

					if (swap)
					{
						(items[j], items[swapIndex]) = (items[swapIndex], items[j]);
						(scores[j], scores[swapIndex]) = (scores[swapIndex], scores[j]);
					}
				}
			}
		}

		public static void Sort<T>(T[] items, System.Func<T, T, int> comparisonFunction)
		{
			for (int i = 0; i < items.Length - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					int relativeScore = comparisonFunction.Invoke(items[swapIndex], items[j]);
					if (relativeScore < 0)
					{
						(items[j], items[swapIndex]) = (items[swapIndex], items[j]); // Swap
					}
				}
			}
		}

		public static void AppendArray<T>(ref T[] array, T[] arrayToAppend)
		{
			int originalLength = array.Length;
			System.Array.Resize(ref array, array.Length + arrayToAppend.Length);
			System.Array.Copy(arrayToAppend, 0, array, originalLength, arrayToAppend.Length);
		}

	}
}
