namespace Chess.Core
{
	public class TranspositionTable
	{

		public const int LookupFailed = -1;

		public const int Exact = 0;

		public const int LowerBound = 1;

		public const int UpperBound = 2;

		public Entry[] entries;

		public readonly ulong count;
		public bool enabled = true;
		Board board;

		public TranspositionTable(Board board, int sizeMB)
		{
			this.board = board;

			int ttEntrySizeBytes = System.Runtime.InteropServices.Marshal.SizeOf<TranspositionTable.Entry>();
			int desiredTableSizeInBytes = sizeMB * 1024 * 1024;
			int numEntries = desiredTableSizeInBytes / ttEntrySizeBytes;

			count = (ulong)(numEntries);
			entries = new Entry[numEntries];
		}

		public void Clear()
		{
			for (int i = 0; i < entries.Length; i++)
			{
				entries[i] = new Entry();
			}
		}

		public ulong Index
		{
			get
			{
				return board.currentGameState.zobristKey % count;
			}
		}

		public Move TryGetStoredMove()
		{
			return entries[Index].move;
		}





		public bool TryLookupEvaluation(int depth, int plyFromRoot, int alpha, int beta, out int eval)
		{
			eval = 0;
			return false;
		}

		public int LookupEvaluation(int depth, int plyFromRoot, int alpha, int beta)
		{
			if (!enabled)
			{
				return LookupFailed;
			}
			Entry entry = entries[Index];

			if (entry.key == board.currentGameState.zobristKey)
			{

				if (entry.depth >= depth)
				{
					int correctedScore = CorrectRetrievedMateScore(entry.value, plyFromRoot);

					if (entry.nodeType == Exact)
					{
						return correctedScore;
					}
					
					if (entry.nodeType == UpperBound && correctedScore <= alpha)
					{
						return correctedScore;
					}

					if (entry.nodeType == LowerBound && correctedScore >= beta)
					{
						return correctedScore;
					}
				}
			}
			return LookupFailed;
		}

		public void StoreEvaluation(int depth, int numPlySearched, int eval, int evalType, Move move)
		{
			if (!enabled)
			{
				return;
			}
			ulong index = Index;

			Entry entry = new Entry(board.currentGameState.zobristKey, CorrectMateScoreForStorage(eval, numPlySearched), (byte)depth, (byte)evalType, move);
			entries[Index] = entry;
		}

		int CorrectMateScoreForStorage(int score, int numPlySearched)
		{
			if (Searcher.IsMateScore(score))
			{
				int sign = System.Math.Sign(score);
				return (score * sign + numPlySearched) * sign;
			}
			return score;
		}

		int CorrectRetrievedMateScore(int score, int numPlySearched)
		{
			if (Searcher.IsMateScore(score))
			{
				int sign = System.Math.Sign(score);
				return (score * sign - numPlySearched) * sign;
			}
			return score;
		}

		public Entry GetEntry(ulong zobristKey)
		{
			return entries[zobristKey % (ulong)entries.Length];
		}

		public struct Entry
		{

			public readonly ulong key;
			public readonly int value;
			public readonly Move move;
			public readonly byte depth;
			public readonly byte nodeType;

			public Entry(ulong key, int value, byte depth, byte nodeType, Move move)
			{
				this.key = key;
				this.value = value;
				this.depth = depth;
				this.nodeType = nodeType;
				this.move = move;
			}

			public static int GetSize()
			{
				return System.Runtime.InteropServices.Marshal.SizeOf<Entry>();
			}
		}
	}
}