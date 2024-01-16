namespace Chess.Core
{
	using System.Collections.Generic;
	using static System.Math;

	public class Searcher
	{
		// Constants
		const int transpositionTableSizeMB = 64;
		const int maxExtentions = 16;

		const int immediateMateScore = 100000;
		const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;

		// Callbacks
		public event System.Action<Move> onSearchComplete;

		// Settings
		SearchSettings settings;

		// References
		TranspositionTable transpositionTable;
		RepetitionTable repetitionTable;
		MoveGenerator moveGenerator;
		MoveOrdering moveOrderer;
		Evaluation evaluation;
		Board board;


		// State
		public int CurrentDepth;
		public Move BestMoveSoFar => bestMove;
		public int BestEvalSoFar => bestEval;
		bool aiPlaysWhite;
		Move bestMoveThisIteration;
		int bestEvalThisIteration;
		Move bestMove;
		int bestEval;
		bool hasSearchedAtLeastOneMove;
		bool searchCancelled;

		// Diagnostics
		public SearchDiagnostics searchDiagnostics;
		int currentIterationDepth;
		System.Diagnostics.Stopwatch searchIterationTimer;
		System.Diagnostics.Stopwatch searchTotalTimer;
		public string debugInfo;

		public Searcher(Board board, SearchSettings settings)
		{
			this.board = board;
			this.settings = settings;

			evaluation = new Evaluation();
			moveGenerator = new MoveGenerator();
			transpositionTable = new TranspositionTable(board, transpositionTableSizeMB);
			moveOrderer = new MoveOrdering(moveGenerator, transpositionTable);
			repetitionTable = new RepetitionTable();

			moveGenerator.promotionsToGenerate = MoveGenerator.PromotionMode.QueenAndKnight;

			Search(1, 0, negativeInfinity, positiveInfinity);
		}

		public void StartSearch()
		{
			// Initialize search

			bestEvalThisIteration = bestEval = 0;
			bestMoveThisIteration = bestMove = Move.NullMove;

			aiPlaysWhite = board.IsWhiteToMove;

			moveOrderer.ClearHistory();
			repetitionTable.Init(board.RepetitionPositionHistory.ToArray());



			// Initialize debug info
			CurrentDepth = 0;
			debugInfo = "Starting search with FEN " + FenUtility.CurrentFen(board);
			searchCancelled = false;
			searchDiagnostics = new SearchDiagnostics();
			searchIterationTimer = new System.Diagnostics.Stopwatch();
			searchTotalTimer = System.Diagnostics.Stopwatch.StartNew();

			// Run search
			if (settings.Mode == SearchSettings.SearchMode.IterativeDeepening)
			{
				DoIterativeDeepeningSearch();
			}
			else
			{
				DoFixedDepthSearch();
			}

			if (bestMove.IsNull)
			{
				bestMove = GetRandomMove();
			}
			onSearchComplete?.Invoke(bestMove);
			searchCancelled = false;
		}

		// Run iterative deepening
		void DoIterativeDeepeningSearch()
		{
			for (int searchDepth = 1; searchDepth <= 256; searchDepth++)
			{
				hasSearchedAtLeastOneMove = false;
				debugInfo += "\nStarting Iteration: " + searchDepth;
				searchIterationTimer.Restart();
				currentIterationDepth = searchDepth;
				Search(searchDepth, 0, negativeInfinity, positiveInfinity);

				if (searchCancelled)
				{
					if (hasSearchedAtLeastOneMove)
					{
						bestMove = bestMoveThisIteration;
						bestEval = bestEvalThisIteration;
						searchDiagnostics.move = MoveUtility.NameFromMove(bestMove);
						searchDiagnostics.eval = bestEval;
						searchDiagnostics.moveIsFromPartialSearch = true;
						debugInfo += "\nUsing partial search result: " + MoveUtility.NameFromMove(bestMove) + " Eval: " + bestEval;
					}

					debugInfo += "\nSearch aborted";
					break;
				}
				else
				{
					CurrentDepth = searchDepth;
					bestMove = bestMoveThisIteration;
					bestEval = bestEvalThisIteration;

					debugInfo += "\nIteration result: " + MoveUtility.NameFromMove(bestMove) + " Eval: " + bestEval;
					if (IsMateScore(bestEval))
					{
						debugInfo += " Mate in ply: " + NumPlyToMateFromScore(bestEval);
					}

					bestEvalThisIteration = int.MinValue;
					bestMoveThisIteration = Move.NullMove;

					searchDiagnostics.numCompletedIterations = searchDepth;
					searchDiagnostics.move = MoveUtility.NameFromMove(bestMove);
					searchDiagnostics.eval = bestEval;

					if (IsMateScore(bestEval) && NumPlyToMateFromScore(bestEval) <= searchDepth)
					{
						debugInfo += "\nExitting search due to mate found within search depth";
						break;
					}
				}
			}
		}

		void DoFixedDepthSearch()
		{
			Search(settings.FixedSearchDepth, 0, negativeInfinity, positiveInfinity);
			bestMove = bestMoveThisIteration;
			bestEval = bestEvalThisIteration;
		}

		public (Move move, int eval) GetSearchResult()
		{
			return (bestMove, bestEval);
		}

		public void EndSearch()
		{
			searchCancelled = true;
		}


		int Search(int plyRemaining, int plyFromRoot, int alpha, int beta, int numExtensions = 0, Move prevMove = default, bool prevWasCapture = false)
		{
			if (searchCancelled)
			{
				return 0;
			}

			if (plyFromRoot > 0)
			{

				if (board.currentGameState.fiftyMoveCounter >= 100 || repetitionTable.Contains(board.currentGameState.zobristKey))
				{
					return 0;
				}

				alpha = Max(alpha, -immediateMateScore + plyFromRoot);
				beta = Min(beta, immediateMateScore - plyFromRoot);
				if (alpha >= beta)
				{
					return alpha;
				}
			}

			int ttVal = transpositionTable.LookupEvaluation(plyRemaining, plyFromRoot, alpha, beta);
			if (ttVal != TranspositionTable.LookupFailed)
			{
				if (plyFromRoot == 0)
				{
					bestMoveThisIteration = transpositionTable.TryGetStoredMove();
					bestEvalThisIteration = transpositionTable.entries[transpositionTable.Index].value;
				}
				return ttVal;
			}

			if (plyRemaining == 0)
			{
				int evaluation = QuiescenceSearch(alpha, beta);
				return evaluation;
			}

			System.Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
			moves = moveGenerator.GenerateMoves(board, moves);
			Move prevBestMove = plyFromRoot == 0 ? bestMove : transpositionTable.TryGetStoredMove();
			moveOrderer.OrderMoves(prevBestMove, board, moves, moveGenerator.opponentAttackMap, moveGenerator.opponentPawnAttackMap, false, plyFromRoot);

			if (moves.Length == 0)
			{
				if (moveGenerator.InCheck())
				{
					int mateScore = immediateMateScore - plyFromRoot;
					return -mateScore;
				}
				else
				{
					return 0;
				}
			}

			if (plyFromRoot > 0)
			{
				bool wasPawnMove = Piece.PieceType(board.Square[prevMove.TargetSquare]) == Piece.Pawn;
				repetitionTable.Push(board.currentGameState.zobristKey, prevWasCapture || wasPawnMove);
			}

			int evaluationBound = TranspositionTable.UpperBound;
			Move bestMoveInThisPosition = Move.NullMove;

			for (int i = 0; i < moves.Length; i++)
			{
				Move move = moves[i];
				int capturedPieceType = Piece.PieceType(board.Square[move.TargetSquare]);
				bool isCapture = capturedPieceType != Piece.None;
				board.MakeMove(moves[i], inSearch: true);

				// Extend the depth of the search
				int extension = 0;
				if (numExtensions < maxExtentions)
				{
					int movedPieceType = Piece.PieceType(board.Square[move.TargetSquare]);
					int targetRank = BoardHelper.RankIndex(move.TargetSquare);
					if (board.IsInCheck())
					{
						extension = 1;
					}
					else if (movedPieceType == Piece.Pawn && (targetRank == 1 || targetRank == 6))
					{
						extension = 1;
					}
				}

				bool needsFullSearch = true;
				int eval = 0;

				
				if (i >= 3 && extension == 0 && plyRemaining >= 3 && !isCapture)
				{
					const int reduceDepth = 1;
					eval = -Search(plyRemaining - 1 - reduceDepth, plyFromRoot + 1, -alpha - 1, -alpha, numExtensions, move, isCapture);
					
					needsFullSearch = eval > alpha;
				}

				// Perform a full-depth search
				if (needsFullSearch)
				{
					eval = -Search(plyRemaining - 1 + extension, plyFromRoot + 1, -beta, -alpha, numExtensions + extension, move, isCapture);
				}


				board.UnmakeMove(moves[i], inSearch: true);

				if (searchCancelled)
				{
					return 0;
				}

				if (eval >= beta)
				{
					
					transpositionTable.StoreEvaluation(plyRemaining, plyFromRoot, beta, TranspositionTable.LowerBound, moves[i]);

					if (!isCapture)
					{
						if (plyFromRoot < MoveOrdering.maxKillerMovePly)
						{
							moveOrderer.killerMoves[plyFromRoot].Add(move);
						}
						int historyScore = plyRemaining * plyRemaining;
						moveOrderer.History[board.MoveColourIndex, moves[i].StartSquare, moves[i].TargetSquare] += historyScore;
					}
					if (plyFromRoot > 0)
					{
						repetitionTable.TryPop();
					}

					searchDiagnostics.numCutOffs++;
					return beta;
				}

				if (eval > alpha)
				{
					evaluationBound = TranspositionTable.Exact;
					bestMoveInThisPosition = moves[i];

					alpha = eval;
					if (plyFromRoot == 0)
					{
						bestMoveThisIteration = moves[i];
						bestEvalThisIteration = eval;
						hasSearchedAtLeastOneMove = true;
					}
				}
			}

			if (plyFromRoot > 0)
			{
				repetitionTable.TryPop();
			}

			transpositionTable.StoreEvaluation(plyRemaining, plyFromRoot, alpha, evaluationBound, bestMoveInThisPosition);

			return alpha;

		}

		int QuiescenceSearch(int alpha, int beta)
		{
			if (searchCancelled)
			{
				return 0;
			}
			
			int eval = evaluation.Evaluate(board);
			searchDiagnostics.numPositionsEvaluated++;
			if (eval >= beta)
			{
				searchDiagnostics.numCutOffs++;
				return beta;
			}
			if (eval > alpha)
			{
				alpha = eval;
			}

			System.Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
			moves = moveGenerator.GenerateMoves(board, moves, false);
			moveOrderer.OrderMoves(Move.NullMove, board, moves, moveGenerator.opponentAttackMap, moveGenerator.opponentPawnAttackMap, true, 0);
			for (int i = 0; i < moves.Length; i++)
			{
				board.MakeMove(moves[i], true);
				eval = -QuiescenceSearch(-beta, -alpha);
				board.UnmakeMove(moves[i], true);

				if (eval >= beta)
				{
					searchDiagnostics.numCutOffs++;
					return beta;
				}
				if (eval > alpha)
				{
					alpha = eval;
				}
			}

			return alpha;
		}


		public static bool IsMateScore(int score)
		{
			if (score == int.MinValue)
			{
				return false;
			}
			const int maxMateDepth = 1000;
			return System.Math.Abs(score) > immediateMateScore - maxMateDepth;
		}

		public static int NumPlyToMateFromScore(int score)
		{
			return immediateMateScore - System.Math.Abs(score);

		}

		public string AnnounceMate()
		{
			if (IsMateScore(bestEvalThisIteration))
			{
				int numPlyToMate = NumPlyToMateFromScore(bestEvalThisIteration);
				int numMovesToMate = (int)Ceiling(numPlyToMate / 2f);

				string sideWithMate = (bestEvalThisIteration * ((board.IsWhiteToMove) ? 1 : -1) < 0) ? "Black" : "White";

				return $"{sideWithMate} can mate in {numMovesToMate} move{((numMovesToMate > 1) ? "s" : "")}";
			}
			return "No mate found";
		}

		public void ClearForNewPosition()
		{
			transpositionTable.Clear();
			moveOrderer.ClearKillers();
		}

		public TranspositionTable GetTranspositionTable() => transpositionTable;

		Move GetRandomMove()
		{
			var moves = moveGenerator.GenerateMoves(board);
			if (moves.Length > 0)
			{
				return moves[new System.Random().Next(moves.Length)];
			}
			return Move.NullMove;
		}

		[System.Serializable]
		public struct SearchDiagnostics
		{
			public int numCompletedIterations;
			public int numPositionsEvaluated;
			public ulong numCutOffs;

			public string moveVal;
			public string move;
			public int eval;
			public bool moveIsFromPartialSearch;
			public int NumQChecks;
			public int numQMates;

			public bool isBook;

			public int maxExtentionReachedInSearch;
		}

	}
}