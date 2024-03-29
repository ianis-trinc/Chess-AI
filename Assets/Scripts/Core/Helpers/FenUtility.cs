﻿using System.Collections.Generic;

namespace Chess.Core
{
	public static class FenUtility
	{
		public const string StartPositionFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

		public static PositionInfo PositionFromFen(string fen)
		{

			PositionInfo loadedPositionInfo = new PositionInfo();
			string[] sections = fen.Split(' ');

			int file = 0;
			int rank = 7;

			foreach (char symbol in sections[0])
			{
				if (symbol == '/')
				{
					file = 0;
					rank--;
				}
				else
				{
					if (char.IsDigit(symbol))
					{
						file += (int)char.GetNumericValue(symbol);
					}
					else
					{
						int pieceColour = (char.IsUpper(symbol)) ? Piece.White : Piece.Black;
						int pieceType = char.ToLower(symbol) switch
						{
							'k' => Piece.King,
							'p' => Piece.Pawn,
							'n' => Piece.Knight,
							'b' => Piece.Bishop,
							'r' => Piece.Rook,
							'q' => Piece.Queen,
							_ => Piece.None
						};

						loadedPositionInfo.squares[rank * 8 + file] = pieceType | pieceColour;
						file++;
					}
				}
			}

			loadedPositionInfo.whiteToMove = (sections[1] == "w");

			string castlingRights = sections[2];
			loadedPositionInfo.whiteCastleKingside = castlingRights.Contains("K");
			loadedPositionInfo.whiteCastleQueenside = castlingRights.Contains("Q");
			loadedPositionInfo.blackCastleKingside = castlingRights.Contains("k");
			loadedPositionInfo.blackCastleQueenside = castlingRights.Contains("q");

			if (sections.Length > 3)
			{
				string enPassantFileName = sections[3][0].ToString();
				if (BoardHelper.fileNames.Contains(enPassantFileName))
				{
					loadedPositionInfo.epFile = BoardHelper.fileNames.IndexOf(enPassantFileName) + 1;
				}
			}

			// Half-move clock
			if (sections.Length > 4)
			{
				int.TryParse(sections[4], out loadedPositionInfo.fiftyMovePlyCount);
			}
			// Full move number
			if (sections.Length > 5)
			{
				int.TryParse(sections[5], out loadedPositionInfo.moveCount);
			}
			return loadedPositionInfo;
		}

		// Get the fen string of the current position
		public static string CurrentFen(Board board)
		{
			string fen = "";
			for (int rank = 7; rank >= 0; rank--)
			{
				int numEmptyFiles = 0;
				for (int file = 0; file < 8; file++)
				{
					int i = rank * 8 + file;
					int piece = board.Square[i];
					if (piece != 0)
					{
						if (numEmptyFiles != 0)
						{
							fen += numEmptyFiles;
							numEmptyFiles = 0;
						}
						bool isBlack = Piece.IsColour(piece, Piece.Black);
						int pieceType = Piece.PieceType(piece);
						char pieceChar = ' ';
						switch (pieceType)
						{
							case Piece.Rook:
								pieceChar = 'R';
								break;
							case Piece.Knight:
								pieceChar = 'N';
								break;
							case Piece.Bishop:
								pieceChar = 'B';
								break;
							case Piece.Queen:
								pieceChar = 'Q';
								break;
							case Piece.King:
								pieceChar = 'K';
								break;
							case Piece.Pawn:
								pieceChar = 'P';
								break;
						}
						fen += (isBlack) ? pieceChar.ToString().ToLower() : pieceChar.ToString();
					}
					else
					{
						numEmptyFiles++;
					}

				}
				if (numEmptyFiles != 0)
				{
					fen += numEmptyFiles;
				}
				if (rank != 0)
				{
					fen += '/';
				}
			}

			// Side to move
			fen += ' ';
			fen += (board.IsWhiteToMove) ? 'w' : 'b';

			// Castling
			bool whiteKingside = (board.currentGameState.castlingRights & 1) == 1;
			bool whiteQueenside = (board.currentGameState.castlingRights >> 1 & 1) == 1;
			bool blackKingside = (board.currentGameState.castlingRights >> 2 & 1) == 1;
			bool blackQueenside = (board.currentGameState.castlingRights >> 3 & 1) == 1;
			fen += ' ';
			fen += (whiteKingside) ? "K" : "";
			fen += (whiteQueenside) ? "Q" : "";
			fen += (blackKingside) ? "k" : "";
			fen += (blackQueenside) ? "q" : "";
			fen += ((board.currentGameState.castlingRights) == 0) ? "-" : "";

			// En-passant
			fen += ' ';
			int epFileIndex = board.currentGameState.enPassantFile - 1;
			int epRankIndex = (board.IsWhiteToMove) ? 5 : 2;

			if (epFileIndex == -1 || !EnPassantCanBeCaptured(epFileIndex, epRankIndex, board))
			{
				fen += '-';
			}
			else
			{
				fen += BoardHelper.SquareNameFromCoordinate(epFileIndex, epRankIndex);
			}

			fen += ' ';
			fen += board.currentGameState.fiftyMoveCounter;

			fen += ' ';
			fen += (board.plyCount / 2) + 1;

			return fen;
		}

		static bool EnPassantCanBeCaptured(int epFileIndex, int epRankIndex, Board board)
		{
			Coord captureFromA = new Coord(epFileIndex - 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
			Coord captureFromB = new Coord(epFileIndex + 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
			int epCaptureSquare = new Coord(epFileIndex, epRankIndex).SquareIndex;
			int friendlyPawn = Piece.MakePiece(Piece.Pawn, board.MoveColour);



			return CanCapture(captureFromA) || CanCapture(captureFromB);


			bool CanCapture(Coord from)
			{
				bool isPawnOnSquare = board.Square[from.SquareIndex] == friendlyPawn;
				if (from.IsValidSquare() && isPawnOnSquare)
				{
					Move move = new Move(from.SquareIndex, epCaptureSquare, Move.EnPassantCaptureFlag);
					board.MakeMove(move);
					board.MakeNullMove();
					bool wasLegalMove = !board.CalculateInCheckState();

					board.UnmakeNullMove();
					board.UnmakeMove(move);
					return wasLegalMove;
				}

				return false;
			}
		}

		public static string FlipFen(string fen)
		{
			string flippedFen = "";
			string[] sections = fen.Split(' ');

			List<char> invertedFenChars = new();
			string[] fenRanks = sections[0].Split('/');

			for (int i = fenRanks.Length - 1; i >= 0; i--)
			{
				string rank = fenRanks[i];
				foreach (char c in rank)
				{
					flippedFen += InvertCase(c);
				}
				if (i != 0)
				{
					flippedFen += '/';
				}
			}

			flippedFen += " " + (sections[1][0] == 'w' ? 'b' : 'w');
			string castlingRights = sections[2];
			string flippedRights = "";
			foreach (char c in "kqKQ")
			{
				if (castlingRights.Contains(c))
				{
					flippedRights += InvertCase(c);
				}
			}
			flippedFen += " " + (flippedRights.Length == 0 ? "-" : flippedRights);

			string ep = sections[3];
			string flippedEp = ep[0] + "";
			if (ep.Length > 1)
			{
				flippedEp += ep[1] == '6' ? '3' : '6';
			}
			flippedFen += " " + flippedEp;
			flippedFen += " " + sections[4] + " " + sections[5];


			return flippedFen;

			char InvertCase(char c)
			{
				if (char.IsLower(c))
				{
					return char.ToUpper(c);
				}
				return char.ToLower(c);
			}
		}

		public class PositionInfo
		{
			public int[] squares;
			// Castling rights
			public bool whiteCastleKingside;
			public bool whiteCastleQueenside;
			public bool blackCastleKingside;
			public bool blackCastleQueenside;

			public int epFile;
			public bool whiteToMove;

			public int fiftyMovePlyCount;

			public int moveCount;

			public PositionInfo()
			{
				squares = new int[64];
			}
		}
	}
}