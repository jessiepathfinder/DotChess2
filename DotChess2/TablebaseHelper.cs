using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DotChess2
{
	public static class TablebaseHelper
	{
		
        private sealed class Leaker
        {
			public Coordinate whiteKing;
        }

		private static IEnumerable<BoardStateNoEnPassant> GetFirstOrder(){
			for(uint ul = 0; ul < 64; ++ul){
				BoardStateNoEnPassant bs = BoardStateNoEnPassant.WriteRawEmptyUnsafe(ul, 7);
				for (uint ul1 = 0; ul1 < 64; ++ul1)
				{
					if ((((uint)((int)(ul & 7) - (int)(ul1 & 7) + 1) & 7u) < 3u) &
 ((((uint)((int)(ul >> 3) - (int)(ul1 >> 3) + 1) & 7u) < 3u) &
 (ul != ul1)))
					{
						continue;
					}
					yield return bs.WriteRawUnsafe(ul1, 15);

				}
			}
		}
		private static IEnumerable<BoardStateNoEnPassant> GetFirstOrderLeaky(Leaker leaker)
		{
			int uli = 0;
			for (uint ul = 0; ul < 64; ++ul, ++uli)
			{
				leaker.whiteKing = new Coordinate(uli & 7, uli >> 3);
				BoardStateNoEnPassant bs = BoardStateNoEnPassant.WriteRawEmptyUnsafe(ul, 7);
				int uli1 = 0;
				for (uint ul1 = 0; ul1 < 64; ++ul1, ++uli1)
				{
					if((((uint)((int)(ul & 7) - (int)(ul1 & 7) + 1) & 7u) < 3u) &
 ((((uint)((int)(ul >> 3) - (int)(ul1 >> 3) + 1) & 7u) < 3u) &
 (ul != ul1)))
					{
						continue;
					}
					yield return bs.WriteRawUnsafe(ul1, 15);

				}
			}
		}
		public static readonly IEnumerable<BoardStateNoEnPassant> kingOnlyStates = GetFirstOrder();
		public static IEnumerable<BoardStateNoEnPassant> FilterInvalidPieceCount(IEnumerable<BoardStateNoEnPassant> upper){
			foreach(var x in upper){
				bool foundOddWhiteBishop = false;
				bool foundEvenWhiteBishop = false;
				bool foundOddBlackBishop = false;
				bool foundEvenBlackBishop = false;
				uint nWhiteRooks = 0;
				uint nBlackRooks = 0;
				for(ulong i = 0; i < 64; ++i){
					ulong pc = x.ReadRawUnsafe(i);

					if ((pc == 4) | (pc == 5))
					{
						if (nWhiteRooks == 2) goto noret;
						++nWhiteRooks;
					}
					if ((pc == 12) | (pc == 13))
					{
						if (nBlackRooks == 2) goto noret;
						++nBlackRooks;
					}

					//Naming convention doesn't matter
					//And we also want good JIT JNE emitting
					bool odd = ((i + (i >> 3)) & 1) == 0;
					if (pc == 3){
						if (odd){
							if (foundOddWhiteBishop) goto noret;
							foundOddWhiteBishop = true;
						} else{
							if (foundEvenWhiteBishop) goto noret;
							foundEvenWhiteBishop = true;
						}
					}
					if (pc == 11)
					{
						if (odd)
						{
							if (foundOddBlackBishop) goto noret;
							foundOddBlackBishop = true;
						}
						else
						{
							if (foundEvenBlackBishop) goto noret;
							foundEvenBlackBishop = true;
						}
					}
				}

				yield return x;
			noret:;
			}
		}
		public static IEnumerable<BoardStateNoEnPassant> InjectPiece1(IEnumerable<BoardStateNoEnPassant> upper)
		{
			foreach(var x in upper){
				yield return x;
				for (ulong i = 0; i < 8; ++i)
				{
					if (x.ReadRawUnsafe(i) == 0)
					{
						yield return x.WriteRawUnsafe(i, 2);
						yield return x.WriteRawUnsafe(i, 3);
						yield return x.WriteRawUnsafe(i, 4);
						yield return x.WriteRawUnsafe(i, 6);

						yield return x.WriteRawUnsafe(i, 10);
						yield return x.WriteRawUnsafe(i, 11);
						yield return x.WriteRawUnsafe(i, 12);
						yield return x.WriteRawUnsafe(i, 14);
					}
				}
				
				for (ulong i = 8; i < 56; ++i){
					if(x.ReadRawUnsafe(i) == 0){
						yield return x.WriteRawUnsafe(i, 1);
						yield return x.WriteRawUnsafe(i, 2);
						yield return x.WriteRawUnsafe(i, 3);
						yield return x.WriteRawUnsafe(i, 4);
						yield return x.WriteRawUnsafe(i, 6);

						yield return x.WriteRawUnsafe(i, 9);
						yield return x.WriteRawUnsafe(i, 10);
						yield return x.WriteRawUnsafe(i, 11);
						yield return x.WriteRawUnsafe(i, 12);
						yield return x.WriteRawUnsafe(i, 14);
					}
				}
				for (ulong i = 56; i < 64; ++i)
				{
					if (x.ReadRawUnsafe(i) == 0)
					{
						yield return x.WriteRawUnsafe(i, 2);
						yield return x.WriteRawUnsafe(i, 3);
						yield return x.WriteRawUnsafe(i, 4);
						yield return x.WriteRawUnsafe(i, 6);

						yield return x.WriteRawUnsafe(i, 10);
						yield return x.WriteRawUnsafe(i, 11);
						yield return x.WriteRawUnsafe(i, 12);
						yield return x.WriteRawUnsafe(i, 14);
					}
				}
				if (x.ReadRawUnsafe(4) == 7)
				{
					if (x.ReadRawUnsafe(0) == 0) yield return x.WriteRawUnsafe(0, 5);
					if (x.ReadRawUnsafe(7) == 0) yield return x.WriteRawUnsafe(7, 5);
				}
				if (x.ReadRawUnsafe(60) == 15)
				{
					if (x.ReadRawUnsafe(56) == 0) yield return x.WriteRawUnsafe(56, 13);

					if (x.ReadRawUnsafe(63) == 0) yield return x.WriteRawUnsafe(63, 13);
				}
			}
			
		}
		
		public static IEnumerable<BoardState> InjectEnPassant(IEnumerable<BoardStateNoEnPassant> upper){
			foreach(var x in upper){
				yield return new BoardState(x);
				for(ulong i = 24; i < 32; ++i){
					if ((x.ReadRawUnsafe(i) == 1) && (x.ReadRawUnsafe(i-8) == 0))
					{
						if (((i>24)&&(x.ReadRawUnsafe(i - 1) == 9)) || ((i <31) && (x.ReadRawUnsafe(i + 1) == 9))){
							yield return new BoardState(x, (byte)(i + 1));
						}
						
					}
					if ((x.ReadRawUnsafe(i+8) == 9) && (x.ReadRawUnsafe(i + 16) == 0))
					{
						if (((i > 32) && (x.ReadRawUnsafe(i + 7) == 1)) || ((i < 39) && (x.ReadRawUnsafe(i + 9) == 1)))
						{
							yield return new BoardState(x, (byte)(i + 9));
						}
					}
				}
			}
		}
		public static IEnumerable<BoardState> InjectBlackEnPassant(IEnumerable<BoardStateNoEnPassant> upper)
		{
			foreach (var x in upper)
			{
				yield return new BoardState(x);
				for (ulong i = 32; i < 40; ++i)
				{

					if ((x.ReadRawUnsafe(i) == 9) && (x.ReadRawUnsafe(i + 8) == 0))
					{
						if (((i > 32) && (x.ReadRawUnsafe(i - 1) == 1)) || ((i < 39) && (x.ReadRawUnsafe(i + 1) == 1)))
						{
							yield return new BoardState(x, (byte)(i + 9));
						}
					}
				}
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool HasLegalWhiteEnPassantUnsafe(
	BoardState bs,
	Coordinate whiteKing)
		{
			uint ep = bs.enPassantOffset;

			if (ep == 0)
			{
				return false;
			}

			uint idx = ep - 1;

			// Black EP pawn required
			if ((idx < 32) | (idx > 39))
			{
				return false;
			}

			int x = (int)(idx & 7);
			int y = (int)(idx >> 3);

			Coordinate target =
				new Coordinate(x, y - 1);

			// =====================================
			// Left white pawn
			// =====================================

			if (
				(x > 0) &
				(bs.ReadRawUnsafe(idx - 1) == 1)
			)
			{
				Coordinate source =
					new Coordinate(x - 1, y);

				BoardState next =
					Walker.ApplyMoveUnsafe(
						bs,
						new Move(source, target));

				if (
					Walker.ChkLegalKingPositionUnsafe(
						next.boardStateNoEnPassant,
						whiteKing,
						8))
				{
					return true;
				}
			}

			// =====================================
			// Right white pawn
			// =====================================

			if (
				(x < 7) &
				(bs.ReadRawUnsafe(idx + 1) == 1)
			)
			{
				Coordinate source =
					new Coordinate(x + 1, y);

				BoardState next =
					Walker.ApplyMoveUnsafe(
						bs,
						new Move(source, target));

				if (
					Walker.ChkLegalKingPositionUnsafe(
						next.boardStateNoEnPassant,
						whiteKing,
						8))
				{
					return true;
				}
			}

			return false;
		}
		public static IEnumerable<BoardState> WalkWhiteToMove(
	uint nonKingPieceCount)
		{
			Leaker leaker = new();

			IEnumerable<BoardStateNoEnPassant> theThing =
				GetFirstOrderLeaky(leaker);

			for (uint i = 0; i < nonKingPieceCount; ++i)
			{
				theThing = InjectPiece1(theThing);

				if (i > 2)
				{
					theThing =
						FilterInvalidPieceCount(theThing);
				}
			}

			var theThing1 =
				InjectBlackEnPassant(theThing);

			foreach (var x in theThing1)
			{
				uint enPassant =
					x.enPassantOffset;

				if (
					(enPassant != 0) &&
					!HasLegalWhiteEnPassantUnsafe(
						x,
						leaker.whiteKing)
				)
				{
					continue;
				}

				yield return x;
			}
		}

	}
}
