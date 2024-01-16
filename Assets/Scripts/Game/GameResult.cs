namespace Chess.Game
{
	using Chess.Core;
	using System.Linq;

	public static class GameResult
	{
		public enum Result
		{
			None,
			Playing,
			WhiteIsMated,
			BlackIsMated,
			Stalemate,
			Repetition,
			FiftyMoveRule,
			InsufficientMaterial,
			DrawByArbiter,
			WhiteTimeout,
			BlackTimeout
		}

		public static Result GetGameState(Board board)
		{
			MoveGenerator moveGenerator = new MoveGenerator();
			var moves = moveGenerator.GenerateMoves(board);

			// Look for mate/stalemate
			if (moves.Length == 0)
			{
				if (moveGenerator.InCheck())
				{
					return (board.IsWhiteToMove) ? Result.WhiteIsMated : Result.BlackIsMated;
				}
				return Result.Stalemate;
			}

			// Fifty move rule
			if (board.currentGameState.fiftyMoveCounter >= 100)
			{
				return Result.FiftyMoveRule;
			}

			// Threefold repetition
			int repCount = board.RepetitionPositionHistory.Count((x => x == board.currentGameState.zobristKey));
			if (repCount == 3)
			{
				return Result.Repetition;
			}

			// Look for insufficient material
			if (InsufficentMaterial(board))
			{
				return Result.InsufficientMaterial;
			}
			return Result.Playing;
		}

		static bool InsufficentMaterial(Board board)
		{
			if (board.pawns[Board.WhiteIndex].Count > 0 || board.pawns[Board.BlackIndex].Count > 0)
			{
				return false;
			}

			if (board.FriendlyOrthogonalSliders != 0 || board.EnemyOrthogonalSliders != 0)
			{
				return false;
			}

			int numWhiteBishops = board.bishops[Board.WhiteIndex].Count;
			int numBlackBishops = board.bishops[Board.BlackIndex].Count;
			int numWhiteKnights = board.knights[Board.WhiteIndex].Count;
			int numBlackKnights = board.knights[Board.BlackIndex].Count;
			int numWhiteMinors = numWhiteBishops + numWhiteKnights;
			int numBlackMinors = numBlackBishops + numBlackKnights;

			if (numWhiteMinors == 0 && numBlackMinors == 0)
			{
				return true;
			}

			if ((numWhiteMinors == 1 && numBlackMinors == 0) || (numBlackMinors == 1 && numWhiteMinors == 0))
			{
				return true;
			}


			return false;


		}
	}
}