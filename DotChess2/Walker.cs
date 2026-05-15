using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace DotChess2
{
	public static class Walker
	{

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static bool HasAnyLegalMoveUnsafe(
	BoardState boardState,
	bool blackToMove)
		{
			uint kingcoord = 0;

			uint eightIfBlack =
				blackToMove ? 8u : 0u;

			uint expectedKing =
				7u | eightIfBlack;

			// Flip to enemy color for check test
			eightIfBlack ^= 8u;

			// WARNING:
			// kingless boards freeze forever
			while (
				boardState.ReadRawUnsafe(
					kingcoord) != expectedKing)
			{
				++kingcoord;
			}

			Coordinate kingCoord =
				new Coordinate(
					(int)(kingcoord & 7),
					(int)(kingcoord >> 3));

			BoardStateNoEnPassant nopa =
				boardState.boardStateNoEnPassant;

			// Maximum pseudo-legal move count
			// for a single piece is safely below 28
			Span<Coordinate> destinations =
				stackalloc Coordinate[28];

			if (blackToMove)
			{
				for (ulong ctr1 = 0; ctr1 < 64; ++ctr1)
				{
					uint u =
						nopa.ReadRawUnsafe(ctr1);

					if ((u & 8u) == 0)
					{
						continue;
					}

					int ictr1 = (int)ctr1;

					Coordinate origin =
						new Coordinate(
							ictr1 & 7,
							ictr1 >>> 3);

					int delta =
						GetPieceTargetsUnsafe(
							destinations,
							boardState,
							origin,
							(GamePiece)u);

					bool isKingMove =
						origin.Equals(kingCoord);

					for (int i = 0; i < delta; ++i)
					{
						Coordinate dest =
							destinations[i];

						if (
							ChkLegalKingPositionUnsafe(
								ApplyMoveUnsafe(
									boardState,
									new Move(origin, dest))
								
								.boardStateNoEnPassant,
								isKingMove
									? dest
									: kingCoord,
								eightIfBlack))
						{
							return true;
						}
					}
				}
			}
			else
			{
				for (ulong ctr1 = 0; ctr1 < 64; ++ctr1)
				{
					ulong u =
						nopa.ReadRawUnsafe(ctr1);

					if (
						(u == 0) |
						((u & 8u) == 8u))
					{
						continue;
					}

					int ictr1 = (int)ctr1;

					Coordinate origin =
						new Coordinate(
							ictr1 & 7,
							ictr1 >>> 3);

					int delta =
						GetPieceTargetsUnsafe(
							destinations,
							boardState,
							origin,
							(GamePiece)u);

					bool isKingMove =
						origin.Equals(kingCoord);

					for (int i = 0; i < delta; ++i)
					{
						Coordinate dest =
							destinations[i];

						if (
							ChkLegalKingPositionUnsafe(
								ApplyMoveUnsafe(
									boardState,
									new Move(origin, dest))
								
								.boardStateNoEnPassant,
								isKingMove
									? dest
									: kingCoord,
								eightIfBlack))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetAllLegalMovesUnsafe(BoardState boardState, Span<Coordinate> origins, Span<Coordinate> destinations, bool blackToMove)
		{
			uint kingcoord = 0;
			uint eightIfBlack = blackToMove ? 8u : 0u;
			uint expectedKing = 7u | eightIfBlack;
			eightIfBlack ^= 8;

			//WARNING: kingless boards will freeze forever here
			while(boardState.ReadRawUnsafe(kingcoord) != expectedKing){
				++kingcoord;
			}
			Coordinate kingCoord = new Coordinate((int)(kingcoord & 7), (int)(kingcoord >> 3));
			int ctr = 0;
			//OPTIMIZATION: Legal moves are dependent on piece identity
			//NOT en passant status 
			BoardStateNoEnPassant nopa = boardState.boardStateNoEnPassant;
			//HACK: CPU branch predictor state isolation optimization
			//Boosts execution speed by reducing branch mispredictions
			if (blackToMove)
			{
				for (ulong ctr1 = 0; ctr1 < 64; ++ctr1)
				{
					ulong u = nopa.ReadRawUnsafe(ctr1);
					if ((u & 8u) == 0) continue;
					int ictr1 = (int)ctr1;
					Coordinate origin = new Coordinate(ictr1 & 7, ictr1 >>> 3);
					int delta = GetPieceTargetsUnsafe(destinations[ctr..], boardState, origin, (GamePiece)u);

					bool isKingMove = origin.Equals(kingCoord);
					int oldctr = ctr;
					for(int i = ctr, stop = ctr + delta; i < stop; ++i){
						Coordinate dest = destinations[i];
						if(ChkLegalKingPositionUnsafe(ApplyMoveUnsafe(boardState, new Move(origin, dest)).boardStateNoEnPassant, isKingMove ? dest : kingCoord, eightIfBlack)){
							//Smart defragmentation
							if(ctr < i){
								destinations[ctr] = dest;
							}
							++ctr;
						}
					}
					origins.Slice(oldctr, ctr - oldctr).Fill(origin);
				}
			}
			else
			{
				for (ulong ctr1 = 0; ctr1 < 64; ++ctr1)
				{
					ulong u = nopa.ReadRawUnsafe(ctr1);
					if ((u == 0) | ((u & 8u) == 8)) continue;
					int ictr1 = (int)ctr1;
					Coordinate origin = new Coordinate(ictr1 & 7, ictr1 >>> 3);
					int delta = GetPieceTargetsUnsafe(destinations[ctr..], boardState, origin, (GamePiece)u);

					bool isKingMove = origin.Equals(kingCoord);
					int oldctr = ctr;
					for (int i = ctr, stop = ctr + delta; i < stop; ++i)
					{
						Coordinate dest = destinations[i];
						if (ChkLegalKingPositionUnsafe(ApplyMoveUnsafe(boardState, new Move(origin, dest)).boardStateNoEnPassant, isKingMove ? dest : kingCoord, eightIfBlack))
						{
							//Smart defragmentation
							if (ctr < i)
							{
								destinations[ctr] = dest;
							}
							++ctr;
						}
					}
					origins.Slice(oldctr, ctr - oldctr).Fill(origin);
				}
			}
			return ctr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static GamePiece NormalizePiece(GamePiece gamePiece){
			if (gamePiece == GamePiece.RookCastleable) return GamePiece.Rook;
			if (gamePiece == (GamePiece.RookCastleable|GamePiece.Black)) return (GamePiece.Rook | GamePiece.Black);
			return gamePiece & (GamePiece)15;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static GamePiece NormalizePiece2(GamePiece gamePiece)
		{
			if (gamePiece == GamePiece.RookCastleable) return GamePiece.Rook;
			if (gamePiece == (GamePiece.RookCastleable | GamePiece.Black)) return (GamePiece.Rook | GamePiece.Black);
			return gamePiece;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetPieceTargetsUnsafe(Span<Coordinate> destinations,BoardState bs, Coordinate origin, GamePiece cached){
			uint eightIfBlack = (uint)(cached & GamePiece.Black);
			switch (cached & GamePiece.King)
			{
				case GamePiece.Pawn:
					return GetPawnTargets(destinations, bs, origin, cached, eightIfBlack);
				case GamePiece.Rook:
				case GamePiece.RookCastleable:
					return GetRookTargetsUnsafe(destinations, bs.boardStateNoEnPassant, origin, eightIfBlack);
				case GamePiece.Bishop:
					return GetBishopTargetsUnsafe(destinations, bs.boardStateNoEnPassant, origin, eightIfBlack);
				case GamePiece.Knight:
					return GetKnightTargetsUnsafe(destinations, bs.boardStateNoEnPassant, origin, eightIfBlack);
				case GamePiece.Queen:
					int i = GetRookTargetsUnsafe(destinations, bs.boardStateNoEnPassant, origin, eightIfBlack);
					return i + GetBishopTargetsUnsafe(destinations[i..], bs.boardStateNoEnPassant, origin, eightIfBlack);
				case GamePiece.King:
					return GetKingTargetsUnsafe(destinations, bs.boardStateNoEnPassant, origin, eightIfBlack);
				default:
					throw new Exception("Invalid piece (should not reach here)");
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		private static int GetKingTargetsUnsafe(
			Span<Coordinate> destinations,
			BoardStateNoEnPassant bs,
			Coordinate origin,
			uint eightIfBlack)
		{
			int x = origin.x;
			int y = origin.y;
			int off = 0;

			int tx, ty;
			Coordinate coord;
			uint ul;

			tx = x; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x + 1; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x + 1; ty = y;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x + 1; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x - 1; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x - 1; ty = y;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x - 1; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);

				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			// =========================================
			// Castling
			// =========================================
			//
			// NOTE:
			// Final destination legality is filtered
			// later by GetAllLegalMovesUnsafe().
			//
			// Here we only:
			// - require king on home file
			// - require rook castleability
			// - require empty path
			// - require current square not in check
			// - require pass-through square not in check
			//
			// =========================================
			uint eib1 = eightIfBlack ^ 8;
			if (
				(x == 4) &&
				ChkLegalKingPositionUnsafe(
					bs,
					origin,
					eib1))
			{
				uint rookCastleable =
					RookCastleable | eightIfBlack;

				// =====================================
				// Kingside
				// =====================================
				//
				// e -> g
				// rook h -> f
				//
				// Squares f/g must be empty
				// f must not be attacked
				//
				// =====================================

				if (
					(bs.ReadRawUnsafe(
						5u | ((uint)y << 3)) == 0) &
					(bs.ReadRawUnsafe(
						6u | ((uint)y << 3)) == 0) &
					(bs.ReadRawUnsafe(
						7u | ((uint)y << 3))
						== rookCastleable)
				)
				{
					if (
						ChkLegalKingPositionUnsafe(
							bs,
							new Coordinate(5, y),
							eib1))
					{
						destinations[off++] =
							new Coordinate(6, y);
					}
				}

				// =====================================
				// Queenside
				// =====================================
				//
				// e -> c
				// rook a -> d
				//
				// Squares b/c/d empty
				// d must not be attacked
				//
				// =====================================

				if (
					(bs.ReadRawUnsafe(
						1u | ((uint)y << 3)) == 0) &
					(bs.ReadRawUnsafe(
						2u | ((uint)y << 3)) == 0) &
					(bs.ReadRawUnsafe(
						3u | ((uint)y << 3)) == 0) &
					(bs.ReadRawUnsafe(
						0u | ((uint)y << 3))
						== rookCastleable)
				)
				{
					if (
						ChkLegalKingPositionUnsafe(
							bs,
							new Coordinate(3, y),
							eib1))
					{
						destinations[off++] =
							new Coordinate(2, y);
					}
				}
			}

			return off;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static BoardState ApplyMoveUnsafe(BoardState bs, Move move)
		{
			Coordinate source = move.source;
			Coordinate destination = move.destination;

			int sx = source.x;
			int sy = source.y;

			int tx = destination.x;
			int ty = destination.y;

			ulong sindex = (ulong)(sx | (sy << 3));
			ulong tindex = (ulong)(tx | (ty << 3));

			ulong rawPiece = bs.ReadRawUnsafe(sindex);
			//ulong target = bs.ReadRawUnsafe(tindex);

			ulong tp1 = rawPiece & 15;

			bool isPawn =
				(tp1 & 7) == (ulong)GamePiece.Pawn;


			BoardState wbs = bs;

			// =========================================
			// Semantic capture resolution
			// =========================================

			ulong effectiveCaptureIndex = tindex;

			// =========================================
			// Physical landing square
			// =========================================

			ulong effectivePlacementIndex = tindex;

			if (isPawn)
			{
				Coordinate ect =
					GetEffectiveCaptureTarget(
						bs,
						move,
						(GamePiece)tp1);

				effectiveCaptureIndex =
					(ulong)(ect.x | (ect.y << 3));



				// =====================================
				// Knight underpromotion landing square
				// =====================================

				if ((sx != tx) & (sy == ty))
				{
					int pmd =
						1 - (((int)(tp1 &
						(ulong)GamePiece.Black)) >> 2);

					effectivePlacementIndex =
						(ulong)(tx |
						((sy + pmd) << 3));
				}
				else if ((sx == tx) & (sy == ty))
				{
					int pmd =
						1 - (((int)(tp1 &
						(ulong)GamePiece.Black)) >> 2);

					effectivePlacementIndex =
						(ulong)(sx |
						((sy + pmd) << 3));
				}



				// =====================================
				// En passant capture
				// =====================================

				if (
					(sx != tx) &
					(sy != ty)
				)
				{
					wbs = wbs.DeleteUnsafe(
						(int)effectiveCaptureIndex);


					goto skippy;
				}
			}



		skippy:

			// =========================================
			// Pawn handling
			// =========================================

			if (isPawn)
			{
				int temp = ty - sy;

				// Double push
				if ((temp * temp) == 4)
				{
					tp1 |=
						(ulong)GamePiece.EnPassantCapturable;
				}
				// Promotion
				else if (
					(sy == ty) |
					(ty == 7) |
					(ty == 0)
				)
				{
					// =====================================
					// DotChess2 knight underpromotion
					//
					// Same-rank move => knight promotion
					// Otherwise queen promotion
					// =====================================

					if (sy == ty)
					{
						tp1 =
							(tp1 &
							(ulong)GamePiece.Black) |
							(ulong)GamePiece.Knight;

						// Queen -> knight delta
					}
					else
					{
						tp1 =
							(tp1 &
							(ulong)GamePiece.Black) |
							(ulong)GamePiece.Queen;

					}
				}
			}
			// =========================================
			// King handling
			// =========================================
			else if (
				(tp1 & 7) ==
				(ulong)GamePiece.King)
			{
				int temp = tx - sx;

				// =====================================
				// Castling
				// =====================================

				if ((temp * temp) == 4)
				{
					int rookSourceX =
						(temp > 0) ? 7 : 0;

					int rookDestX =
						sx + (temp / 2);

					ulong rookSourceIndex =
						(ulong)(rookSourceX |
						(sy << 3));

					ulong rookDestIndex =
						(ulong)(rookDestX |
						(sy << 3));

					ulong rookPiece =
						bs.ReadRawUnsafe(
							rookSourceIndex);

					if (
						(rookPiece & 7) ==
						(ulong)GamePiece.RookCastleable
					)
					{
						ulong rook =
							((ulong)GamePiece.Rook) |
							(tp1 &
							(ulong)GamePiece.Black);

						wbs = wbs.DeleteUnsafe(
							(int)rookSourceIndex);

						wbs = wbs.WriteRawUnsafe(
							rookDestIndex,
							rook);
					}
				}
				else
				{
					// =================================
					// First king move disables both
					// castleable rooks.
					// =================================

					int homeRank =
						(((int)(tp1 &
						(ulong)GamePiece.Black)) >> 3) * 7;

					if (
						(sx == 4) &
						(sy == homeRank)
					)
					{
						ulong qrookIndex =
							(ulong)(0 |
							(sy << 3));

						ulong krookIndex =
							(ulong)(7 |
							(sy << 3));

						ulong qrook =
							wbs.ReadRawUnsafe(
								qrookIndex);

						ulong krook =
							wbs.ReadRawUnsafe(
								krookIndex);

						if (
							(qrook & 7) == 5ul
						)
						{
							wbs = wbs.DeleteUnsafe(
								(int)qrookIndex);

							wbs = wbs.WriteRawUnsafe(
								qrookIndex,
								(qrook & 8) |
								(ulong)GamePiece.Rook);
						}

						if (
							(krook & 7) == 5ul
						)
						{
							wbs = wbs.DeleteUnsafe(
								(int)krookIndex);

							wbs = wbs.WriteRawUnsafe(
								krookIndex,
								(krook & 8) |
								(ulong)GamePiece.Rook);
						}
					}
				}
			}
			// =========================================
			// Rook movement removes castleability
			// =========================================
			else if (
				(tp1 & 7) ==
				(ulong)GamePiece.RookCastleable)
			{
				tp1 =
					(tp1 &
					(ulong)GamePiece.Black) |
					(ulong)GamePiece.Rook;
			}

			// =========================================
			// Remove captured piece
			// =========================================

			if (
				(effectiveCaptureIndex != tindex) &
				(effectiveCaptureIndex != sindex)
			)
			{
				wbs = wbs.DeleteUnsafe(
					(int)effectiveCaptureIndex);
			}

			// =========================================
			// Apply move
			// =========================================

			wbs = wbs.DeleteUnsafe((int)sindex);

			// Avoid deleting source square twice
			if (effectivePlacementIndex != sindex)
			{
				wbs = wbs.DeleteUnsafe(
					(int)effectivePlacementIndex);
			}

			wbs = wbs.WriteRawUnsafe(
				effectivePlacementIndex,
				tp1);

			return wbs;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool FastBoundCheck(int x, int y)
		{
			x |= y;
			return (x > -1) & (x < 8);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetRookTargetsUnsafe(Span<Coordinate> destinations, BoardStateNoEnPassant bs, Coordinate origin, ulong eightIfBlack)
		{
			int x1 = origin.x;
			int y1 = origin.y;
			int off = 0;
			for(int x = x1 + 1; x < 8; ++x){
				Coordinate coord = new Coordinate(x,y1);
				ulong ul = bs.GetUnsafe(coord);
				if (ul == 0) goto write;
				if((ul & 8) != eightIfBlack){
					destinations[off++] = coord;
				}
				break;
			write:
				destinations[off++] = coord;
			}
			for (int x = x1 - 1; x > -1; --x)
			{
				Coordinate coord = new Coordinate(x, y1);
				ulong ul = bs.GetUnsafe(coord);
				if (ul == 0) goto write;
				if ((ul & 8) != eightIfBlack)
				{
					destinations[off++] = coord;
				}
				break;
			write:
				destinations[off++] = coord;
			}
			for (int y = y1 + 1; y < 8; ++y)
			{
				Coordinate coord = new Coordinate(x1, y);
				ulong ul = bs.GetUnsafe(coord);
				if (ul == 0) goto write;
				if ((ul & 8) != eightIfBlack)
				{
					destinations[off++] = coord;
				}
				break;
			write:
				destinations[off++] = coord;
			}
			for (int y = y1 - 1; y > -1; --y)
			{
				Coordinate coord = new Coordinate(x1, y);
				ulong ul = bs.GetUnsafe(coord);
				if (ul == 0) goto write;
				if ((ul & 8) != eightIfBlack)
				{
					destinations[off++] = coord;
				}
				break;
			write:
				destinations[off++] = coord;
			}
			return off;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		private static int GetKnightTargetsUnsafe(Span<Coordinate> destinations, BoardStateNoEnPassant bs, Coordinate origin, ulong eightIfBlack)
		{
			int x = origin.x;
			int y = origin.y;
			int off = 0;

			int tx, ty;
			Coordinate coord;
			ulong ul;

			tx = x + 1; ty = y + 2;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x + 2; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x + 2; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x + 1; ty = y - 2;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x - 1; ty = y - 2;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x - 2; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x - 2; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			tx = x - 1; ty = y + 2;
			if (FastBoundCheck(tx, ty))
			{
				coord = new Coordinate(tx, ty);
				ul = bs.GetUnsafe(coord);
				if ((ul == 0) | ((ul & 8) != eightIfBlack))
					destinations[off++] = coord;
			}

			return off;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		private static int GetBishopTargetsUnsafe(Span<Coordinate> destinations, BoardStateNoEnPassant bs, Coordinate origin, ulong eightIfBlack)
		{
			int x1 = origin.x;
			int y1 = origin.y;
			int off = 0;

			for (int x = x1 + 1, y = y1 + 1; FastBoundCheck(x, y); ++x, ++y)
			{
				Coordinate coord = new Coordinate(x, y);
				ulong ul = bs.GetUnsafe(coord);

				if (ul == 0) goto write;
				if ((ul & 8) != eightIfBlack)
				{
					destinations[off++] = coord;
				}
				break;

			write:
				destinations[off++] = coord;
			}

			for (int x = x1 - 1, y = y1 + 1; FastBoundCheck(x, y); --x, ++y)
			{
				Coordinate coord = new Coordinate(x, y);
				ulong ul = bs.GetUnsafe(coord);

				if (ul == 0) goto write;
				if ((ul & 8) != eightIfBlack)
				{
					destinations[off++] = coord;
				}
				break;

			write:
				destinations[off++] = coord;
			}

			for (int x = x1 + 1, y = y1 - 1; FastBoundCheck(x, y); ++x, --y)
			{
				Coordinate coord = new Coordinate(x, y);
				ulong ul = bs.GetUnsafe(coord);

				if (ul == 0) goto write;
				if ((ul & 8) != eightIfBlack)
				{
					destinations[off++] = coord;
				}
				break;

			write:
				destinations[off++] = coord;
			}

			for (int x = x1 - 1, y = y1 - 1; FastBoundCheck(x, y); --x, --y)
			{
				Coordinate coord = new Coordinate(x, y);
				ulong ul = bs.GetUnsafe(coord);

				if (ul == 0) goto write;
				if ((ul & 8) != eightIfBlack)
				{
					destinations[off++] = coord;
				}
				break;

			write:
				destinations[off++] = coord;
			}

			return off;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Conclusion GetConclusionFastUnsafe(
	BoardState boardState,
	bool blackToMove)
		{
			// =========================================
			// Insufficient material detection
			// =========================================
			//
			// Supported:
			// - K vs K
			// - K+B vs K
			// - K+N vs K
			//
			// Intentionally does NOT detect:
			// - K+B vs K+B same color
			// - fortress draws
			// - repetition
			// - 50 move rule
			//
			// Fast conservative detector.
			// =========================================

			bool foundMinor = false;

			for (ulong i = 0; i < 64; ++i)
			{
				uint p =
					boardState.ReadRawUnsafe(i);

				switch (p & 7)
				{
					case 0:
					case 7:
						break;

					case 3:
					case 2:

						if (foundMinor)
						{
							goto material_sufficient;
						}
						foundMinor = true;

						break;

					default:
						goto material_sufficient;
				}
			}

			return Conclusion.TOO_WEAK;

		material_sufficient:

			// =========================================
			// Find side-to-move king
			// =========================================

			uint king = 7u | (blackToMove ? 8u : 0u);

			uint kingcoord = 0;

			// WARNING:
			// kingless boards freeze forever
			while (
				boardState.ReadRawUnsafe(
					kingcoord) != king)
			{
				++kingcoord;
			}

			Coordinate kingCoord =
				new Coordinate(
					(int)(kingcoord & 7),
					(int)(kingcoord >> 3));

			// =========================================
			// Check if side has ANY legal move
			// =========================================

			if (HasAnyLegalMoveUnsafe(
				boardState,
				blackToMove))
			{
				return Conclusion.NORMAL;
			}

			// =========================================
			// No legal moves:
			// checkmate or stalemate
			// =========================================

			bool inCheck =
				!ChkLegalKingPositionUnsafe(
					boardState.boardStateNoEnPassant,
					kingCoord,
					blackToMove
						? 0u
						: 8u);

			return inCheck
				? Conclusion.CHECKMATE
				: Conclusion.STALEMATE;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ObstructionType GetObstructionType(BoardStateNoEnPassant bs, Coordinate coordinate, ulong eightifblack)
		{
			int cxy = coordinate.x | coordinate.y;
			if ((cxy < 0) | cxy > 7){
				return ObstructionType.INVALID_OBSTRUCTION;
			}
			ulong piece = bs.ReadRawUnsafe(((uint)coordinate.x) | (((uint)coordinate.y) << 3));
			if (piece == 0) return ObstructionType.NO_OBSTRUCTION;
			return ((piece & 8) == eightifblack) ? ObstructionType.FRIENDLY_OBSTRUCTION : ObstructionType.ENEMY_OBSTRUCTION;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetPawnTargets(Span<Coordinate> destinations,BoardState bs, Coordinate origin, GamePiece cached, uint u8ib)
		{
			int eightIfBlack = (int)u8ib;
			uint defaul = (uint)cached;
			
			int pmd = 1 - (eightIfBlack >> 2);

			uint pa_pawn = (u8ib^8u) | Pawn | 16u;
			Coordinate leftCapture = origin.AddXY(-1, pmd);
			Coordinate rightCapture = origin.AddXY(1, pmd);
			int ictr = 0;
			if(bs.GetOrDefault(origin.AddX(-1),defaul) == pa_pawn){
				destinations[ictr++] = leftCapture;
			} else if (bs.GetOrDefault(origin.AddX(1), defaul) == pa_pawn)
			{
				//OPTIMIZATION: EP captures are mutual-exclusive, meaning that left and right EP captures
				//are never simultaneously possible
				//If left-EP is possible, don't check for right-EP
				destinations[ictr++] = rightCapture;
			}
			int sr = 1 + (eightIfBlack >> 3) * 5;
			bool isPromoteable = origin.y == (7-sr);
			Coordinate fwd = origin.AddY(pmd);
			var nbs = bs.boardStateNoEnPassant;
			if (nbs.GetUnsafe(fwd) == 0){
				destinations[ictr++] = fwd;
				if(isPromoteable){
					//Knight promotion is implied
					//For same source and destination pawn move
					//e.g F2F2, F2F3, F3F1
					destinations[ictr++] =  origin;
				}
				else if(origin.y == sr)
				{
					Coordinate fwd1 = fwd.AddY(pmd);
					if(nbs.GetUnsafe(fwd1) == 0){
						destinations[ictr++] = fwd1;
					}
				}

			}
			
			if (GetObstructionType(nbs, leftCapture, u8ib) == ObstructionType.ENEMY_OBSTRUCTION)
			{
				destinations[ictr++] = leftCapture;
				if (isPromoteable)
				{
					destinations[ictr++] = origin.AddX(-1);
				}
			}
			if (GetObstructionType(nbs, rightCapture, u8ib) == ObstructionType.ENEMY_OBSTRUCTION)
			{
				destinations[ictr++] = rightCapture;
				if (isPromoteable)
				{
					destinations[ictr++] = origin.AddX(1);
				}
			}
			
			return ictr;
		}
		private const uint Pawn = (uint)GamePiece.Pawn;
		private const uint Knight = (uint)GamePiece.Knight;
		private const uint Bishop = (uint)GamePiece.Bishop;
		private const uint Rook = (uint)GamePiece.Rook;
		private const uint RookCastleable = (uint)GamePiece.RookCastleable;
		private const uint Queen = (uint)GamePiece.Queen;
		private const uint King = (uint)GamePiece.King;

		private const uint Black = (uint)GamePiece.Black;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static bool ChkLegalKingPositionUnsafe(BoardStateNoEnPassant bs, Coordinate origin, uint eightIfBlack)
		{
			int x = origin.x;
			int y = origin.y;

			int tx, ty;
			uint ul;
			uint piece;

			//NOTE: We don't check for en passant pawns
			//because BoardStateNoEnPassant naturally strips this metadata
			uint enemy_pawn = Pawn | eightIfBlack;
			uint enemy_knight = Knight | eightIfBlack;
			uint enemy_bishop = Bishop | eightIfBlack;
			uint enemy_rook = Rook | eightIfBlack;
			uint enemy_rook_castleable = RookCastleable | eightIfBlack;
			uint enemy_queen = Queen | eightIfBlack;
			uint enemy_king = King | eightIfBlack;

			// =========================================
			// Adjacent kings + knights fused by quadrant
			// =========================================

			// +x +y
			tx = x + 1; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;

				tx = x + 1; ty = y + 2;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}

				tx = x + 2; ty = y + 1;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}
			}

			// +x -y
			tx = x + 1; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;

				tx = x + 1; ty = y - 2;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}

				tx = x + 2; ty = y - 1;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}
			}

			// -x -y
			tx = x - 1; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;

				tx = x - 1; ty = y - 2;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}

				tx = x - 2; ty = y - 1;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}
			}

			// -x +y
			tx = x - 1; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;

				tx = x - 1; ty = y + 2;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}

				tx = x - 2; ty = y + 1;
				if (FastBoundCheck(tx, ty))
				{
					ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

					if (ul == enemy_knight)
						return false;
				}
			}

			// =========================================
			// Orthogonal king checks
			// =========================================

			tx = x; ty = y + 1;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;
			}

			tx = x + 1; ty = y;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;
			}

			tx = x; ty = y - 1;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;
			}

			tx = x - 1; ty = y;
			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_king)
					return false;
			}

			// =========================================
			// Enemy pawns
			// =========================================

			int pmd = 1 - (((int)eightIfBlack) >> 2);

			tx = x - 1;
			ty = y - pmd;

			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_pawn)
					return false;
			}

			tx = x + 1;
			ty = y - pmd;

			if (FastBoundCheck(tx, ty))
			{
				ul = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (ul == enemy_pawn)
					return false;
			}

			// =========================================
			// Orthogonal sliders
			// =========================================

			for (tx = x + 1; tx < 8; ++tx)
			{
				piece = bs.ReadRawUnsafe((uint)tx | ((uint)y << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_rook) |
					(piece == enemy_rook_castleable) |
					(piece == enemy_queen))
					return false;

				break;
			}

			for (tx = x - 1; tx > -1; --tx)
			{
				piece = bs.ReadRawUnsafe((uint)tx | ((uint)y << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_rook) |
					(piece == enemy_rook_castleable) |
					(piece == enemy_queen))
					return false;

				break;
			}

			for (ty = y + 1; ty < 8; ++ty)
			{
				piece = bs.ReadRawUnsafe((uint)x | ((uint)ty << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_rook) |
					(piece == enemy_rook_castleable) |
					(piece == enemy_queen))
					return false;

				break;
			}

			for (ty = y - 1; ty > -1; --ty)
			{
				piece = bs.ReadRawUnsafe((uint)x | ((uint)ty << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_rook) |
					(piece == enemy_rook_castleable) |
					(piece == enemy_queen))
					return false;

				break;
			}

			// =========================================
			// Diagonal sliders
			// =========================================

			for (tx = x + 1, ty = y + 1; FastBoundCheck(tx, ty); ++tx, ++ty)
			{
				piece = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_bishop) |
					(piece == enemy_queen))
					return false;

				break;
			}

			for (tx = x - 1, ty = y + 1; FastBoundCheck(tx, ty); --tx, ++ty)
			{
				piece = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_bishop) |
					(piece == enemy_queen))
					return false;

				break;
			}

			for (tx = x + 1, ty = y - 1; FastBoundCheck(tx, ty); ++tx, --ty)
			{
				piece = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_bishop) |
					(piece == enemy_queen))
					return false;

				break;
			}

			for (tx = x - 1, ty = y - 1; FastBoundCheck(tx, ty); --tx, --ty)
			{
				piece = bs.ReadRawUnsafe((uint)tx | ((uint)ty << 3));

				if (piece == 0)
					continue;

				if ((piece == enemy_bishop) |
					(piece == enemy_queen))
					return false;

				break;
			}

			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static Coordinate GetEffectiveCaptureTarget(
	BoardState bs,
	Move move,
	GamePiece movingPiece)
		{
			Coordinate source = move.source;
			Coordinate destination = move.destination;

			int sx = source.x;
			int sy = source.y;

			int tx = destination.x;
			int ty = destination.y;

			// =========================================
			// Normal captures
			// =========================================

			// Most moves capture on destination square.
			// This includes:
			// - all non-pawn captures
			// - normal pawn captures
			// - queen promotions
			if ((movingPiece & GamePiece.King) != GamePiece.Pawn)
			{
				return destination;
			}

			// =========================================
			// En passant
			// =========================================

			// Pawn diagonal move onto empty square.
			if ((sx != tx) & (sy != ty))
			{
				if (bs.GetUnsafe(destination) == 0)
				{
					return new Coordinate(tx, sy);
				}

				return destination;
			}

			// =========================================
			// DotChess2 knight underpromotion capture
			// =========================================

			// Same-rank sideways pawn move.
			//
			// Encoding:
			// F7G7 means:
			// - capture on G8 (white)
			// - knight promote
			//
			// or:
			//
			// F2G2 means:
			// - capture on G1 (black)
			// - knight promote

			if ((sx != tx) & (sy == ty))
			{
				int pmd =
					1 - (((int)(movingPiece & GamePiece.Black)) >> 2);

				return new Coordinate(tx, sy + pmd);
			}

			// =========================================
			// Non-capturing pawn move
			// =========================================

			return destination;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static bool HasRelevantEnPassantUnsafe(
	BoardState bs)
		{
			uint ep = bs.enPassantOffset;

			if (ep == 0)
			{
				return false;
			}

			uint idx = ep - 1;

			uint enemyPawn = 1 + (((idx - 24) >> 3) << 3);
			uint idx7 = idx & 7u;

			if ((idx7 != 0) && (bs.ReadRawUnsafe(idx - 1) == enemyPawn)) return true;
			return (idx7 != 7) && (bs.ReadRawUnsafe(idx + 1) == enemyPawn);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BoardStateNoEnPassant VerticalFlipUnsafe(BoardStateNoEnPassant boardStateNoEnPassant)
		{
			BoardStateNoEnPassant newboard = new BoardStateNoEnPassant(Vector256<ulong>.Zero);
			for (ulong i = 0; i < 64; ++i)
			{
				uint p = boardStateNoEnPassant.ReadRawUnsafe(i);
				if (p == 0) continue;
				newboard = newboard.WriteRawUnsafe(i ^ 56, p);
			}
			return newboard;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BoardStateNoEnPassant HorizontalFlipUnsafe(BoardStateNoEnPassant boardStateNoEnPassant)
		{
			BoardStateNoEnPassant newboard = new BoardStateNoEnPassant(Vector256<ulong>.Zero);
			for (ulong i = 0; i < 64; ++i)
			{
				uint p = boardStateNoEnPassant.ReadRawUnsafe(i);
				if (p == 0) continue;
				newboard = newboard.WriteRawUnsafe(i ^ 7, p);
			}
			return newboard;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int InvarCompare(BoardStateNoEnPassant a, BoardStateNoEnPassant b){

			var c = a.a.AsUInt32();
			var d = b.a.AsUInt32();

			var e = Avx2.ExtractVector128(c, 0);
			var f = Avx2.ExtractVector128(d, 0);
			int pc = Sse41.Extract(e, 0).CompareTo(Sse41.Extract(f, 0));
			if (pc != 0) return pc;
			pc = Sse41.Extract(e, 1).CompareTo(Sse41.Extract(f, 1));
			if (pc != 0) return pc;
			pc = Sse41.Extract(e, 2).CompareTo(Sse41.Extract(f, 2));
			if (pc != 0) return pc;
			pc = Sse41.Extract(e, 3).CompareTo(Sse41.Extract(f, 3));
			if (pc != 0) return pc;
			e = Avx2.ExtractVector128(c, 1);
			f = Avx2.ExtractVector128(d, 1);
			pc = Sse41.Extract(e, 0).CompareTo(Sse41.Extract(f, 0));
			if (pc != 0) return pc;
			pc = Sse41.Extract(e, 1).CompareTo(Sse41.Extract(f, 1));
			if (pc != 0) return pc;
			pc = Sse41.Extract(e, 2).CompareTo(Sse41.Extract(f, 2));
			if (pc != 0) return pc;
			return Sse41.Extract(e, 3).CompareTo(Sse41.Extract(f, 3));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static BoardStateNoEnPassant Rotate90Unsafe(
	BoardStateNoEnPassant bs)
		{
			// (x,y) -> (7-y,x)

			BoardStateNoEnPassant nb =
				new(Vector256<ulong>.Zero);

			for (ulong i = 0; i < 64; ++i)
			{
				uint p = bs.ReadRawUnsafe(i);

				if (p == 0) continue;

				ulong x = (i & 7ul);
				ulong y = (i >> 3);

				ulong ni = (7 - y) | (x << 3);

				nb = nb.WriteRawUnsafe(ni, p);
			}

			return nb;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static BoardStateNoEnPassant RotateMinus90Unsafe(
			BoardStateNoEnPassant bs)
		{
			// (x,y) -> (y,7-x)

			BoardStateNoEnPassant nb =
				new(Vector256<ulong>.Zero);

			for (ulong i = 0; i < 64; ++i)
			{
				uint p = bs.ReadRawUnsafe(i);

				if (p == 0) continue;

				ulong x = (i & 7ul);
				ulong y = (i >> 3);

				ulong ni = (y | ((7 - x) << 3));

				nb = nb.WriteRawUnsafe(ni, p);
			}

			return nb;
		}




		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (
			BoardStateNoEnPassant identity,
			int color_flip)
		GetIdentity1(BoardStateNoEnPassant bullshit){

			BoardStateNoEnPassant ivc = bullshit.FlipColorUnsafe();
			if(InvarCompare(ivc,bullshit) < 0){
				return (ivc, -1);
			} else{
				return (bullshit, 1);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (
			BoardStateNoEnPassant identity,
			int color_flip)
		GetIdentity2(BoardStateNoEnPassant bullshit)
		{

			(bullshit, int fli) = GetIdentity1(bullshit);
			BoardStateNoEnPassant ivc = VerticalFlipUnsafe(bullshit);

			if (InvarCompare(ivc, bullshit) < 0)
			{
				return (ivc, fli);
			}
			else
			{
				return (bullshit, fli);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (
			BoardStateNoEnPassant identity,
			int color_flip)
		GetIdentity3(BoardStateNoEnPassant bullshit)
		{

			(bullshit, int fli) = GetIdentity2(bullshit);
			BoardStateNoEnPassant ivc = HorizontalFlipUnsafe(bullshit);

			if (InvarCompare(ivc, bullshit) < 0)
			{
				return (ivc, fli);
			}
			else
			{
				return (bullshit, fli);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (
			BoardStateNoEnPassant identity,
			int color_flip)
		GetIdentity4(BoardStateNoEnPassant bullshit)
		{

			BoardStateNoEnPassant ivc = VerticalFlipUnsafe(bullshit.FlipColorUnsafe());
			if (InvarCompare(ivc, bullshit) < 0)
			{
				return (ivc, -1);
			}
			else
			{
				return (bullshit, 1);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (
			BoardStateNoEnPassant identity,
			int color_flip)
		GetIdentityAssumePawn(BoardStateNoEnPassant bullshit)
		{

			(bullshit, int fli) = GetIdentity4(bullshit);
			BoardStateNoEnPassant ivc = HorizontalFlipUnsafe(bullshit);

			if (InvarCompare(ivc, bullshit) < 0)
			{
				return (ivc, fli);
			}
			else
			{
				return (bullshit, fli);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (
			BoardStateNoEnPassant identity,
			int color_flip)
		GetIdentity(BoardStateNoEnPassant bullshit)
		{
			// =====================================
			// If castleability metadata exists,
			// horizontal symmetry is unsafe because
			// queenside/kingside semantics differ.
			//
			// Allowed:
			// - identity
			// - vertical flip + color inversion
			// =====================================

			if (
				(bullshit.ReadRawUnsafe(0ul) == 5) |
				(bullshit.ReadRawUnsafe(7ul) == 5) |
				(bullshit.ReadRawUnsafe(56ul) == 13) |
				(bullshit.ReadRawUnsafe(63ul) == 13)
			)
			{
				BoardStateNoEnPassant newboard =
					VerticalFlipUnsafe(bullshit)
					.FlipColorUnsafe();

				if (InvarCompare(bullshit, newboard) > 0)
				{
					return (newboard, -1);
				}

				return (bullshit, 1);
			}

			// =====================================
			// Full symmetry reduction allowed
			// =====================================



			for (ulong x = 0; x < 24; ++x)
			{
				ulong p = bullshit.ReadRawUnsafe(x);
				if ((p == 1) | (p == 9))
				{
					return GetIdentityAssumePawn(bullshit);
				}
			}
			for (ulong x = 24; x < 32; ++x)
			{
				ulong p = bullshit.ReadRawUnsafe(x);
				if ((p == 1) | (p == 9) | (p == 5))
				{
					return GetIdentityAssumePawn(bullshit);
				}
			}
			for (ulong x = 32; x < 40; ++x)
			{
				ulong p = bullshit.ReadRawUnsafe(x);
				if ((p == 1) | (p == 9) | (p == 13))
				{
					return GetIdentityAssumePawn(bullshit);
				}
			}
			for (ulong x = 40; x < 64; ++x)
			{
				ulong p = bullshit.ReadRawUnsafe(x);
				if ((p == 1) | (p == 9))
				{
					return GetIdentityAssumePawn(bullshit);
				}
			}
			(BoardStateNoEnPassant best, int bestFlip) = GetIdentity3(bullshit);
			(BoardStateNoEnPassant best1, int bestFlip1) = GetIdentity3(Rotate90Unsafe(bullshit));
			if(InvarCompare(best1,best) < 0){
				return (best1, bestFlip1);
			}

			return (best, bestFlip);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (
	BoardStateNoEnPassant identity,
	int color_flip)
GetIdentityAssumeNoEP(BoardStateNoEnPassant bullshit)
		{
			// =====================================
			// If castleability metadata exists,
			// horizontal symmetry is unsafe because
			// queenside/kingside semantics differ.
			//
			// Allowed:
			// - identity
			// - vertical flip + color inversion
			// =====================================

			if (
				(bullshit.ReadRawUnsafe(0ul) == 5) |
				(bullshit.ReadRawUnsafe(7ul) == 5) |
				(bullshit.ReadRawUnsafe(56ul) == 13) |
				(bullshit.ReadRawUnsafe(63ul) == 13)
			)
			{
				BoardStateNoEnPassant newboard =
					VerticalFlipUnsafe(bullshit)
					.FlipColorUnsafe();

				if (InvarCompare(bullshit, newboard) > 0)
				{
					return (newboard, -1);
				}

				return (bullshit, 1);
			}

			// =====================================
			// Full symmetry reduction allowed
			// =====================================



			for (ulong x = 0; x < 64; ++x)
			{
				ulong p = bullshit.ReadRawUnsafe(x);
				if ((p == 1) | (p == 9))
				{
					return GetIdentityAssumePawn(bullshit);
				}
			}
			
			(BoardStateNoEnPassant best, int bestFlip) = GetIdentity3(bullshit);
			(BoardStateNoEnPassant best1, int bestFlip1) = GetIdentity3(Rotate90Unsafe(bullshit));
			if (InvarCompare(best1, best) < 0)
			{
				return (best1, bestFlip1);
			}

			return (best, bestFlip);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (BoardStateNoEnPassant,int) GetIdentity(BoardState boardState){
			BoardStateNoEnPassant boardStateNoEnPassant;
			if(HasRelevantEnPassantUnsafe(boardState)){
				return GetIdentityAssumePawn(boardState.ToCompressedEPFormUnsafe());
			} else{
				return GetIdentityAssumeNoEP(boardState.boardStateNoEnPassant);
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static uint GetMoveAdvantageDeltaUnsafe(
	BoardState bs,
	Move move)
		{
			Coordinate source = move.source;
			Coordinate destination = move.destination;

			int sx = source.x;
			int sy = source.y;

			int tx = destination.x;
			int ty = destination.y;

			ulong sindex =
				(ulong)(sx | (sy << 3));

			ulong tindex =
				(ulong)(tx | (ty << 3));

			uint movingPiece =
				bs.ReadRawUnsafe(sindex);

			uint tp1 =
				movingPiece & 15u;

			uint target;

			// =====================================
			// Resolve effective capture target
			// =====================================

			if ((tp1 & 7u) == Pawn)
			{
				// En passant
				if (
					(sx != tx) &
					(sy != ty) &
					(bs.ReadRawUnsafe(tindex) == 0)
				)
				{
					target =
						bs.ReadRawUnsafe(
							(ulong)(tx | (sy << 3)));
				}
				// Knight underpromotion capture
				else if ((sx != tx) & (sy == ty))
				{
					int pmd =
						1 - (((int)(tp1 & Black)) >> 2);

					target =
						bs.ReadRawUnsafe(
							(ulong)(tx |
							((sy + pmd) << 3)));
				}
				else
				{
					target =
						bs.ReadRawUnsafe(tindex);
				}
			}
			else
			{
				target =
					bs.ReadRawUnsafe(tindex);
			}

			uint score = (0x9553310u >> (int)((target & 7u) << 2)) &15;

			

			// =====================================
			// Promotion bonus
			// =====================================

			if ((tp1 & 7u) == Pawn)
			{
				if (
					(sy == ty) |
					(ty == 0) |
					(ty == 7)
				)
				{
					// Knight promotion
					if (sy == ty)
					{
						score += 2;
					}
					// Queen promotion
					else
					{
						score += 8;
					}
				}
			}

			return score;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint GetKillerMoveScoreUnsafe2(
		BoardState bs,
		Move move, Coordinate ememyKingCoordinate, uint eightIfBlack){
			uint score = GetKillerMoveScoreUnsafe(bs,move) << 2;
			if(ChkLegalKingPositionUnsafe(bs.boardStateNoEnPassant, ememyKingCoordinate, eightIfBlack)){
				return score;
			}
			return score | 1;


		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public static uint GetKillerMoveScoreUnsafe(
	BoardState bs,
	Move move)
		{
			Coordinate source = move.source;
			Coordinate destination = move.destination;

			int sx = source.x;
			int sy = source.y;

			int tx = destination.x;
			int ty = destination.y;

			ulong sindex =
				(ulong)(sx | (sy << 3));

			ulong tindex =
				(ulong)(tx | (ty << 3));

			uint movingPiece =
				bs.ReadRawUnsafe(sindex);

			uint tp1 =
				movingPiece & 15u;

			uint target;

			// =====================================
			// Resolve effective capture target
			// =====================================

			if ((tp1 & 7u) == Pawn)
			{
				// En passant
				if (
					(sx != tx) &
					(sy != ty) &
					(bs.ReadRawUnsafe(tindex) == 0)
				)
				{
					target =
						bs.ReadRawUnsafe(
							(ulong)(tx | (sy << 3)));
				}
				// Knight underpromotion capture
				else if ((sx != tx) & (sy == ty))
				{
					int pmd =
						1 - (((int)(tp1 & Black)) >> 2);

					target =
						bs.ReadRawUnsafe(
							(ulong)(tx |
							((sy + pmd) << 3)));
				}
				else
				{
					target =
						bs.ReadRawUnsafe(tindex);
				}
			}
			else
			{
				target =
					bs.ReadRawUnsafe(tindex);
			}

			// =====================================
			// Capture score
			// =====================================

			return (0x5443210u >> (int)((target & 7u) << 2)) & 7;
		}
	}
}
