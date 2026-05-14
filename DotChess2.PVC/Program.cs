using System;
using DotChess2;

namespace DotChess2.PVC
{
	public static class Program
	{
		// Placeholder engine
		// Replace with actual implementation later
		private static readonly TruncatedMinimaxChessEngine? engine = new TruncatedMinimaxChessEngine();

		public static void Main()
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
					renderer.Render());

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