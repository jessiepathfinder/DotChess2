using System;
using DotChess2;

namespace DotChess2.PVP
{
	internal static class Program
	{
		private static void Main()
		{
			Console.OutputEncoding =
				System.Text.Encoding.UTF8;

			Console.CursorVisible = false;

			SimpleRenderer renderer =
				new SimpleRenderer(
					new BoardState(
						BoardStateNoEnPassant.Initial(),
						0));

			while (true)
			{
				Console.Clear();

				Console.WriteLine(
					"DotChess2");
				Console.WriteLine(
					"Arrow keys = move cursor");
				Console.WriteLine(
					"Space/Enter = select");
				Console.WriteLine(
					"ESC = quit");
				Console.WriteLine();

				Console.WriteLine(
					renderer.IsBlackTurn
						? "Black to move"
						: "White to move");

				Console.WriteLine();

				Console.Write(
					renderer.Render(true));

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
					case ConsoleKey.Spacebar:
						renderer.Click();
						break;

					case ConsoleKey.Escape:
						return;
				}
			}
		}
	}
}