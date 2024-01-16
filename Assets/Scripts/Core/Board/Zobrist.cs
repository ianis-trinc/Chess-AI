namespace Chess.Core
{

	public static class Zobrist
	{
		
		public static readonly ulong[,] piecesArray = new ulong[Piece.MaxPieceIndex + 1, 64];
	
		public static readonly ulong[] castlingRights = new ulong[16];
		
		public static readonly ulong[] enPassantFile = new ulong[9];
		public static readonly ulong sideToMove;


		static Zobrist()
		{

			const int seed = 29426028;
			System.Random rng = new System.Random(seed);

			for (int squareIndex = 0; squareIndex < 64; squareIndex++)
			{
				foreach (int piece in Piece.PieceIndices)
				{
					piecesArray[piece, squareIndex] = RandomUnsigned64BitNumber(rng);
				}
			}


			for (int i = 0; i < castlingRights.Length; i++)
			{
				castlingRights[i] = RandomUnsigned64BitNumber(rng);
			}

			for (int i = 0; i < enPassantFile.Length; i++)
			{
				enPassantFile[i] = i == 0 ? 0 : RandomUnsigned64BitNumber(rng);
			}

			sideToMove = RandomUnsigned64BitNumber(rng);
		}


		public static ulong CalculateZobristKey(Board board)
		{
			ulong zobristKey = 0;

			for (int squareIndex = 0; squareIndex < 64; squareIndex++)
			{
				int piece = board.Square[squareIndex];

				if (Piece.PieceType(piece) != Piece.None)
				{
					zobristKey ^= piecesArray[piece, squareIndex];
				}
			}

			zobristKey ^= enPassantFile[board.currentGameState.enPassantFile];

			if (board.MoveColour == Piece.Black)
			{
				zobristKey ^= sideToMove;
			}

			zobristKey ^= castlingRights[board.currentGameState.castlingRights];

			return zobristKey;
		}

		static ulong RandomUnsigned64BitNumber(System.Random rng)
		{
			byte[] buffer = new byte[8];
			rng.NextBytes(buffer);
			return System.BitConverter.ToUInt64(buffer, 0);
		}
	}
}