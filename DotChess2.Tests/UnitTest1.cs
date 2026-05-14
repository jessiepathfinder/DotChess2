using NUnit.Framework;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DotChess2.Tests
{
	[TestFixture]
	public class BoardStateTests
	{



		[Test] [ChatGPTGenerated]
		public void RandomWriteRead_NoEnPassant_20Passes()
		{
			for (int pass = 0; pass < 20; pass++)
			{
				BoardState state = new(
					new BoardStateNoEnPassant(
						System.Runtime.Intrinsics.Vector256<ulong>.Zero
					)
				);

				ulong[] expected = new ulong[64];

				for (ulong i = 0; i < 64; i++)
				{
					ulong value = (ulong)RandomNumberGenerator.GetInt32(0, 9);

					expected[i] = value;

					if (value != 0)
					{
						state = state.WriteRawUnsafe(i, value);
					}
				}

				for (ulong i = 0; i < 64; i++)
				{
					ulong actual = state.ReadRawUnsafe(i);

					Assert.That(
						actual,
						Is.EqualTo(expected[i]),
						$"Mismatch at index {i} during pass {pass}"
					);
				}
			}
		}

		[Test]
		[ChatGPTGenerated]
		public void RandomWriteRead_WithEnPassant_20Passes()
		{
			for (int pass = 0; pass < 20; pass++)
			{
				BoardState state = new(
					new BoardStateNoEnPassant(
						System.Runtime.Intrinsics.Vector256<ulong>.Zero
					)
				);

				ulong[] expected = new ulong[64];

				ulong epIndex = (ulong) RandomNumberGenerator.GetInt32(0, 64);


				ulong epPawn =
					(ulong)(
						GamePiece.Pawn |
						GamePiece.EnPassantCapturable
					) | ((ulong)GamePiece.Black * (ulong)RandomNumberGenerator.GetInt32(0, 2));

				for (ulong i = 0; i < 64; i++)
				{

					if (i == epIndex)
					{
						expected[i] = epPawn;
						continue;
					}

					ulong value = (ulong)RandomNumberGenerator.GetInt32(0, 8);
					expected[i] = value;
					state = state.WriteRawUnsafe(i, value);
					
				}
				state = state.WriteRawUnsafe(epIndex, epPawn);

				for (ulong i = 0; i < 64; i++)
				{
					ulong actual = state.ReadRawUnsafe(i);

					Assert.That(
						actual,
						Is.EqualTo(expected[i]),
						$"Mismatch at index {i} during pass {pass}"
					);
				}
			}
		}
	}
}