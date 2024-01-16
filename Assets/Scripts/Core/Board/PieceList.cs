namespace Chess.Core
{
	public class PieceList
	{
		public int[] occupiedSquares;
		
		int[] map;
		int numPieces;

		public PieceList(int maxPieceCount = 16)
		{
			occupiedSquares = new int[maxPieceCount];
			map = new int[64];
			numPieces = 0;
		}

		public int Count
		{
			get
			{
				return numPieces;
			}
		}

		public void AddPieceAtSquare(int square)
		{
			occupiedSquares[numPieces] = square;
			map[square] = numPieces;
			numPieces++;
		}

		public void RemovePieceAtSquare(int square)
		{
			int pieceIndex = map[square];
			occupiedSquares[pieceIndex] = occupiedSquares[numPieces - 1];
			map[occupiedSquares[pieceIndex]] = pieceIndex;
			numPieces--;
		}

		public void MovePiece(int startSquare, int targetSquare)
		{
			int pieceIndex = map[startSquare];
			occupiedSquares[pieceIndex] = targetSquare;
			map[targetSquare] = pieceIndex;
		}

		public int this[int index] => occupiedSquares[index];

	}
}