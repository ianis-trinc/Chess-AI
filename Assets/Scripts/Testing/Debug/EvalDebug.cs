using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chess.Core;

namespace Chess.Testing
{
	public class EvalDebug : MonoBehaviour
	{
		public string fen;

		void Start()
		{
			Board board = new Board();
			board.LoadPosition(fen);

			Evaluation eval = new Evaluation();
			eval.Evaluate(board);
		}
	}
}