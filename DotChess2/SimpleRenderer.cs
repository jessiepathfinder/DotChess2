using System;
using System.Runtime.CompilerServices;

namespace DotChess2
{
	public sealed class SimpleRenderer
	{
		private int primaryCursorX;
		private int primaryCursorY;

		private int secondaryCursorX;
		private int secondaryCursorY;

		private BoardState boardState;

		private bool destinationSelectMode;
		private bool isBlackTurn;

		private int drawcounter;

		public bool IsBlackTurn => isBlackTurn;

		public BoardState BoardState => boardState;

		public SimpleRenderer(BoardState boardState)
		{
			this.boardState = boardState;
		}

		// =========================================
		// Centralized legality checker
		// =========================================

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsLegalMove(
			Coordinate src,
			Coordinate dst,
			ulong piece)
		{
			BoardState next =
				Walker.ApplyMoveUnsafe(
					boardState,
					new Move(src, dst));

			Coordinate kingCoord;

			// King moves relocate king square
			if ((piece & 7) ==
				(ulong)GamePiece.King)
			{
				kingCoord = dst;
			}
			else
			{
				// Find current king position
				ulong king =
					(ulong)GamePiece.King |
					(isBlackTurn ? 8ul : 0ul);

				uint kingcoord = 0;

				while (next.ReadRawUnsafe(kingcoord) != king)
				{
					++kingcoord;
				}

				kingCoord =
					new Coordinate(
						(int)(kingcoord & 7),
						(int)(kingcoord >> 3));
			}

			return Walker.ChkLegalKingPositionUnsafe(
				next.boardStateNoEnPassant,
				kingCoord,
				isBlackTurn ? 0u : 8u);
		}

		public Conclusion GetConclusion()
		{

			bool isBlackTurn = this.isBlackTurn;


			if (Walker.HasAnyLegalMoveUnsafe(boardState, isBlackTurn))
			{
				if (drawcounter > 100)
				{
					return Conclusion.FIFTY_MOVE_RULE_VIOLATION;
				}

				return Conclusion.NORMAL;
			}

			// =========================================
			// No legal moves
			// =========================================

			ulong king =
				(ulong)GamePiece.King |
				(isBlackTurn ? 8ul : 0ul);

			uint kingcoord = 0;

			while (boardState.ReadRawUnsafe(kingcoord) != king)
			{
				++kingcoord;
			}

			bool inCheck =
				!Walker.ChkLegalKingPositionUnsafe(
					boardState.boardStateNoEnPassant,
					new Coordinate(
						(int)(kingcoord & 7),
						(int)(kingcoord >> 3)),
					isBlackTurn ? 0u : 8u);

			return inCheck
				? Conclusion.CHECKMATE
				: Conclusion.STALEMATE;
		}

		public string Render(bool enable_cursor)
		{
			Span<char> span = stackalloc char[72];

			int wcr = 0;

			for (int y = 7; y >= 0; --y)
			{
				for (int x = 0; x < 8; ++x)
				{
					ulong piece =
						boardState.ReadRawUnsafe(
							(ulong)(x | (y << 3)));

					span[wcr++] =
						PieceToChar(piece);
				}

				span[wcr++] = '\n';
			}

			Conclusion conclusion =
				GetConclusion();

			if (conclusion == Conclusion.NORMAL & enable_cursor)
			{
				if (destinationSelectMode)
				{
					Coordinate src =
						new Coordinate(
							secondaryCursorX,
							secondaryCursorY);

					ulong piece =
						boardState.GetUnsafe(src);

					Span<Coordinate> legalMoves =
						stackalloc Coordinate[64];

					int ctr =
						Walker.GetPieceTargetsUnsafe(
							legalMoves,
							boardState,
							src,
							(GamePiece)piece);

					for (int i = 0; i < ctr; ++i)
					{
						Coordinate coordinate =
							legalMoves[i];

						// =====================================
						// Correct legality check:
						// POST-MOVE BOARD
						// =====================================

						if (!IsLegalMove(
							src,
							coordinate,
							piece))
						{
							continue;
						}

						span[
							((7 - coordinate.y) * 9) +
							coordinate.x] = 'X';
					}
				}

				span[
					((7 - primaryCursorY) * 9) +
					primaryCursorX] = '+';
			}

			string str = new string(span);

			switch (conclusion)
			{
				case Conclusion.CHECKMATE:
					return str +
						(isBlackTurn
							? "White wins!\n"
							: "Black wins!\n");

				case Conclusion.STALEMATE:
					return str +
						"Draw (stalemate)\n";

				case Conclusion.TOO_WEAK:
					return str +
						"Draw (insufficient material)\n";

				case Conclusion.FIFTY_MOVE_RULE_VIOLATION:
					return str +
						"Draw (fifty-move rule violation)\n";
			}

			return str;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char PieceToChar(ulong piece)
		{
			bool black = (piece & 8) != 0;

			char c =
				(piece & 7) switch
				{
					(ulong)GamePiece.Pawn => 'P',
					(ulong)GamePiece.Knight => 'N',
					(ulong)GamePiece.Bishop => 'B',
					(ulong)GamePiece.Rook => 'R',
					(ulong)GamePiece.RookCastleable => 'R',
					(ulong)GamePiece.Queen => 'Q',
					(ulong)GamePiece.King => 'K',
					_ => '.'
				};

			return black
				? char.ToLowerInvariant(c)
				: c;
		}

		public void LeftKey()
		{
			primaryCursorX =
				Math.Clamp(
					primaryCursorX - 1,
					0,
					7);
		}

		public void RightKey()
		{
			primaryCursorX =
				Math.Clamp(
					primaryCursorX + 1,
					0,
					7);
		}

		public void UpKey()
		{
			primaryCursorY =
				Math.Clamp(
					primaryCursorY + 1,
					0,
					7);
		}

		public void DownKey()
		{
			primaryCursorY =
				Math.Clamp(
					primaryCursorY - 1,
					0,
					7);
		}

		public void Click()
		{
			if (GetConclusion() != Conclusion.NORMAL)
			{
				return;
			}

			if (destinationSelectMode)
			{
				Coordinate dst =
					new Coordinate(
						primaryCursorX,
						primaryCursorY);

				Coordinate src =
					new Coordinate(
						secondaryCursorX,
						secondaryCursorY);

				ulong piece =
					boardState.GetUnsafe(src);

				Span<Coordinate> legalMoves =
					stackalloc Coordinate[64];

				int ctr =
					Walker.GetPieceTargetsUnsafe(
						legalMoves,
						boardState,
						src,
						(GamePiece)piece);

				for (int i = 0; i < ctr; ++i)
				{
					Coordinate coordinate =
						legalMoves[i];

					if (!coordinate.Equals(dst))
					{
						continue;
					}

					// =====================================
					// Correct legality check
					// =====================================

					if (!IsLegalMove(
						src,
						dst,
						piece))
					{
						continue;
					}

					ApplyMoveUnsafe(
						new Move(src, dst));

					isBlackTurn = !isBlackTurn;

					break;
				}

				destinationSelectMode = false;
			}
			else
			{
				int px = primaryCursorX;
				int py = primaryCursorY;

				ulong piece =
					boardState.ReadRawUnsafe(
						(ulong)(px | (py << 3)));

				if (
					(piece != 0) &
					(((piece & 8) != 0) == isBlackTurn)
				)
				{
					secondaryCursorX = px;
					secondaryCursorY = py;

					destinationSelectMode = true;
				}
			}
		}

		public void ApplyMoveUnsafe(Move move)
		{
			Coordinate ect =
				Walker.GetEffectiveCaptureTarget(
					boardState,
					move,
					(GamePiece)
					boardState.GetUnsafe(move.source));

			bool resets =
				(boardState.GetUnsafe(ect) != 0);

			ulong movingPiece =
				boardState.GetUnsafe(move.source);

			if ((movingPiece & 7) ==
				(ulong)GamePiece.Pawn)
			{
				resets = true;
			}

			if (resets)
			{
				drawcounter = 0;
			}
			else
			{
				++drawcounter;
			}

			boardState =
				Walker.ApplyMoveUnsafe(
					boardState,
					move);
		}

		public void InvertTurn()
		{
			isBlackTurn = !isBlackTurn;
		}
	}
}