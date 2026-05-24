using System;
using DotChess2;

namespace DotChess2.PVC
{
	public static class Program
	{
		// Placeholder engine
		// Replace with actual implementation later
		private static readonly TruncatedMinimaxChessEngine? engine = new TruncatedMinimaxChessEngine();
		private static void Main()
		{
			Console.CursorVisible = false;

			SimpleRenderer renderer =
				new SimpleRenderer(
					new BoardState(BoardStateNoEnPassant.Initial()));

			ISimpleChessEngine whiteEngine =
				new TruncatedMinimaxChessEngine();

			ISimpleChessEngine blackEngine =
				new TruncatedMinimaxChessEngine();

			while (true)
			{
				Console.Clear();

				Console.Write(
					renderer.Render(false));

				Console.WriteLine(
					renderer.IsBlackTurn
						? "Black to move"
						: "White to move");

				Console.WriteLine(
					"Press ENTER for next engine move.");
				Console.WriteLine(
					"Press ESC to quit.");


				Conclusion conclusion =
					renderer.GetConclusion();

				if (conclusion != Conclusion.NORMAL)
				{
					break;
				}

				bool blackToMove =
					renderer.IsBlackTurn;

				Span<Coordinate> origins =
					stackalloc Coordinate[218];

				Span<Coordinate> destinations =
					stackalloc Coordinate[218];

				int moveCount =
					Walker.GetAllLegalMovesUnsafe(
						renderer.BoardState,
						origins,
						destinations,
						blackToMove);

				if (moveCount <= 0)
				{
					continue;
				}

				Span<Move> permittedMoves =
					stackalloc Move[moveCount];

				for (int i = 0; i < moveCount; ++i)
				{
					permittedMoves[i] =
						new Move(
							origins[i],
							destinations[i]);
				}

				ISimpleChessEngine engine =
					blackToMove
						? blackEngine
						: whiteEngine;

				Move move =
					engine.ComputeMove(
						renderer.BoardState,
						permittedMoves,
						blackToMove);

				renderer.ApplyMoveUnsafe(move);

				renderer.InvertTurn();
			}
			Console.ReadLine();
		}
		public static void Main1()
		{
			Console.CursorVisible = false;

			SimpleRenderer renderer =
				new SimpleRenderer(
					new BoardState(
						BoardStateNoEnPassant.Initial()));

			while (true)
			{
				Console.Clear();

				Console.Write(
					renderer.Render(true));

				Console.WriteLine();
				Console.WriteLine(
					renderer.IsBlackTurn
						? "Black to move"
						: "White to move");

				Console.WriteLine();
				Console.WriteLine(
					"Arrow keys = move cursor");
				Console.WriteLine(
					"Enter = select / move");
				Console.WriteLine(
					"Esc = quit");

				ConsoleKeyInfo key =
					Console.ReadKey(true);

				switch (key.Key)
				{
					case ConsoleKey.LeftArrow:
						renderer.LeftKey();
						break;

					case ConsoleKey.RightArrow:
						renderer.RightKey();
						break;

					case ConsoleKey.UpArrow:
						renderer.UpKey();
						break;

					case ConsoleKey.DownArrow:
						renderer.DownKey();
						break;

					case ConsoleKey.Enter:

						// =====================================
						// Human move
						// =====================================

						renderer.Click();

						// =====================================
						// Engine move
						// =====================================

						if (
							renderer.IsBlackTurn &&
							renderer.GetConclusion() ==
							Conclusion.NORMAL &&
							engine != null)
						{
							Span<Coordinate> origins =
								stackalloc Coordinate[256];

							Span<Coordinate> destinations =
								stackalloc Coordinate[256];

							int ctr =
								Walker.GetAllLegalMovesUnsafe(
									renderer.BoardState,
									origins,
									destinations,
									true);

							Move[] permittedMoves =
								new Move[ctr];

							for (int i = 0; i < ctr; ++i)
							{
								permittedMoves[i] =
									new Move(
										origins[i],
										destinations[i]);
							}

							Move bestMove =
								engine.ComputeMove(
									renderer.BoardState,
									permittedMoves,true);

							renderer.ApplyMoveUnsafe(
								bestMove);

							renderer.InvertTurn();
						}

						break;

					case ConsoleKey.Escape:
						return;
				}
			}
		}
	}
}