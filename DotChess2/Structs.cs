using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using System.Text;

namespace DotChess2
{
	public enum GamePiece : uint
	{
		None = 0,Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, RookCastleable = 5, Queen = 6, King = 7, Black = 8, EnPassantCapturable = 16

	}

	
	//[StructLayout(LayoutKind.Explicit,CharSet =CharSet.None,Size = 32)]
	public readonly struct BoardStateNoEnPassant : IEquatable<BoardStateNoEnPassant>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BoardStateNoEnPassant Initial() => new BoardStateNoEnPassant(Vector256.Create(new ulong[] { 13114200639926239157, 9079256848778920062, 11024811887802974361, 18446462598732840960 }));
		public readonly Vector256<ulong> a;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			var c = a;
			c = Avx2.Xor(c, Avx2.ShiftLeftLogical(c, 13));
			c = Avx2.Xor(c, Avx2.ShiftRightLogical(c, 7));
			c = Avx2.Xor(c, Avx2.ShiftLeftLogical(c, 17));
			var d = Sse2.Add(Avx2.ExtractVector128(c, 0).AsInt32(), Avx2.ExtractVector128(c, 1).AsInt32());
			unchecked{
				return Sse41.Extract(d, 0) + Sse41.Extract(d, 1) + Sse41.Extract(d, 2) + Sse41.Extract(d, 3);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetHashCode_PA(byte pa)
		{
			var c = a;
			c = Avx2.Xor(c, Avx2.ShiftLeftLogical(c, 13));
			c = Avx2.Xor(c, Avx2.ShiftRightLogical(c, 7));
			c = Avx2.Xor(c, Avx2.ShiftLeftLogical(c, 17));
			var d = Sse2.Add(Avx2.ExtractVector128(c, 0).AsInt32(), Avx2.ExtractVector128(c, 1).AsInt32());
			unchecked
			{
				int tt = Sse41.Extract(d, 0) + Sse41.Extract(d, 1) + Sse41.Extract(d, 2) + Sse41.Extract(d, 3);
				if(pa > 0){

					uint tt2 = Sse42.Crc32((uint)tt,pa);
					tt2 ^= tt2 << 13;
					tt2 ^= tt2 >> 17;
					tt2 ^= tt2 << 5;
					return (int)tt2;
				}
				return tt;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			if(obj is BoardStateNoEnPassant ts){
				return ts.Equals(this);
			}
			return false;
		}
		

		public static readonly Vector256<ulong> reshift = Vector256.Create(new ulong[] { 0, 1, 2, 3 });
		public static readonly Vector256<ulong> reshift1 = Vector256.Create(new ulong[] { 63, 62, 61, 60 });


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BoardStateNoEnPassant(Vector256<ulong> a)
		{
			this.a = a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint ReadRawUnsafe(ulong index){
			//XY stride
			//if ((index > 63) | (index < 0)) throw new Exception("Index out of bounds (should not reach here)");
			var thevec = Avx2.ShiftRightLogicalVariable(Avx2.ShiftLeftLogical(Avx2.ShiftRightLogicalVariable(a, Vector256.Create(index)), 63), reshift1);
			var thevec2 = Sse2.Or(Avx2.ExtractVector128(thevec, 0), Avx2.ExtractVector128(thevec, 1)).AsUInt32();

			return Sse41.Extract(thevec2,0) | Sse41.Extract(thevec2,2);

		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BoardStateNoEnPassant DeleteUnsafe(int index)
		{
			return new BoardStateNoEnPassant(Avx2.And(a, Vector256.Create(~((1ul) << index))));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BoardStateNoEnPassant WriteRawUnsafe(ulong index, ulong data)
		{
			return new BoardStateNoEnPassant(Avx2.Or(a, Avx2.ShiftRightLogicalVariable(Avx2.ShiftLeftLogical(Avx2.ShiftRightLogicalVariable(Vector256.Create(data), reshift), 63), Vector256.Create(63 - index))));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ChatGPTGenerated]
		public BoardState ToExpandedEPFormUnsafe()
		{
			// =========================================
			// Scan white EP rank
			// =========================================
			//
			// 24..31
			//
			// White EP pawn is encoded as:
			// 5 (white castleable rook)
			//
			// because:
			// 1 | 4 == 5
			//
			// =========================================

			for (uint i = 24; i < 32; ++i)
			{
				if (ReadRawUnsafe(i) == 5u)
				{
					return new BoardState(
						DeleteUnsafe((int)i).WriteRawUnsafe(i, 1),
						(byte)(i + 1));
				}
			}

			// =========================================
			// Scan black EP rank
			// =========================================
			//
			// 32..39
			//
			// Black EP pawn encoded as:
			// 13
			//
			// because:
			// 9 | 4 == 13
			//
			// =========================================

			for (ulong i = 32; i < 40; ++i)
			{
				if (ReadRawUnsafe(i) == 13u)
				{
					return new BoardState(
						DeleteUnsafe((int)i).WriteRawUnsafe(i, 9),
						(byte)(i + 1));
				}
			}

			// No compressed EP marker
			return new BoardState(this);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BoardStateNoEnPassant WriteRawEmptyUnsafe(ulong index, ulong data)
		{
			return new BoardStateNoEnPassant(Avx2.ShiftRightLogicalVariable(Avx2.ShiftLeftLogical(Avx2.ShiftRightLogicalVariable(Vector256.Create(data), reshift), 63), Vector256.Create(63 - index)));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(BoardStateNoEnPassant other)
		{
			var c = Avx2.Xor(a, other.a);
			var d = Sse2.Or(Avx2.ExtractVector128(c, 0), Avx2.ExtractVector128(c, 1)).AsInt32();
			return (Sse41.Extract(d, 0) | Sse41.Extract(d, 1) | Sse41.Extract(d, 2) | Sse41.Extract(d, 3)) == 0;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetOrDefault(Coordinate coordinate, uint defaul){
			int cxy = coordinate.x | coordinate.y;
			if ((cxy < 0) | cxy > 7) return defaul;
			return ReadRawUnsafe((ulong)(coordinate.x | (coordinate.y << 3)));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetUnsafe(Coordinate coordinate)
		{
			return ReadRawUnsafe((ulong)(coordinate.x | (coordinate.y << 3)));
		}
		public BoardStateNoEnPassant FlipColorUnsafe(){
			return new BoardStateNoEnPassant(Avx2.Xor(a, Vector256.Create(0ul,0ul,0ul,ulong.MaxValue)));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(BoardStateNoEnPassant left, BoardStateNoEnPassant right)
		{
			return left.Equals(right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(BoardStateNoEnPassant left, BoardStateNoEnPassant right)
		{
			return !(left == right);
		}
	}
	public readonly struct BoardState : IEquatable<BoardState>
	{
		public readonly BoardStateNoEnPassant boardStateNoEnPassant;
		public readonly byte enPassantOffset;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BoardState(BoardStateNoEnPassant tableStateNoEnPassant, byte enPassantOffset)
		{
			this.boardStateNoEnPassant = tableStateNoEnPassant;
			this.enPassantOffset = enPassantOffset;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BoardState(BoardStateNoEnPassant tableStateNoEnPassant)
		{
			this.boardStateNoEnPassant = tableStateNoEnPassant;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(BoardState other)
		{
			if (other.enPassantOffset != enPassantOffset) return false;
			return other.boardStateNoEnPassant.Equals(boardStateNoEnPassant);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return boardStateNoEnPassant.GetHashCode_PA(enPassantOffset);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BoardState WriteRawUnsafe(ulong index, ulong value){
			//INTENTIONAL DESIGN CHOICE: En passant capturability does not survive board modification
			return (value > 16) ? new BoardState(boardStateNoEnPassant.WriteRawUnsafe(index, value & 15), (byte)(index + 1)) : new BoardState(boardStateNoEnPassant.WriteRawUnsafe(index, value));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BoardState DeleteUnsafe(int index){
			//INTENTIONAL DESIGN CHOICE: En passant capturability does not survive board modification
			return new BoardState(boardStateNoEnPassant.DeleteUnsafe(index));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			if(obj is BoardState table){
				return table.Equals(this);
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint ReadRawUnsafe(ulong index){
			uint read = boardStateNoEnPassant.ReadRawUnsafe(index);
			if((index + 1) == enPassantOffset) return read | 16;
			return read;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetOrDefault(Coordinate coordinate, uint defaul)
		{
			int cxy = coordinate.x | coordinate.y;
			if ((cxy < 0) | cxy > 7) return defaul;
			return ReadRawUnsafe((ulong)(coordinate.x | (coordinate.y << 3)));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetUnsafe(Coordinate coordinate)
		{
			return ReadRawUnsafe((ulong)(coordinate.x | (coordinate.y << 3)));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(BoardState left, BoardState right)
		{
			return left.Equals(right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(BoardState left, BoardState right)
		{
			return !(left == right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//HACK: We repurpose the "castleable rook" piece
		//As "en passant capturable pawn"
		//Should be safe under legal play
		public BoardStateNoEnPassant ToCompressedEPFormUnsafe(){
			ulong epa = enPassantOffset;
			if (epa == 0) return boardStateNoEnPassant;
			//HACK: Efficient or without deleting first
			return boardStateNoEnPassant.WriteRawUnsafe(epa - 1, 4);
		}
	}

	public readonly struct Coordinate : IEquatable<Coordinate>{
		public readonly int x;
		public readonly int y;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Coordinate(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Coordinate other)
		{
			return (other.x == x) && (other.y == y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			if (obj is Coordinate coordinate){
				return coordinate.Equals(this);
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() {
			return (x | (y << 3)).GetHashCode();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Coordinate AddX(int dx){
			return new Coordinate(x + dx, y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Coordinate AddY(int dy)
		{
			return new Coordinate(x, y+dy);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Coordinate AddXY(int dx,int dy)
		{
			return new Coordinate(x+dx, y + dy);
		}

		public static bool operator ==(Coordinate left, Coordinate right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Coordinate left, Coordinate right)
		{
			return !(left == right);
		}
	}
	public readonly struct Move : IEquatable<Move>
	{
		public readonly Coordinate source;
		public readonly Coordinate destination;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Move(Coordinate source, Coordinate destination)
		{
			this.source = source;
			this.destination = destination;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return (source.x | (source.y << 3) | (destination.x << 6) | (destination.y << 9)).GetHashCode();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			if(obj is Move move){
				return (source == move.source) & (destination == move.destination);
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Move other)
		{
			return (source == other.source) & (destination == other.destination);
		}

		public static bool operator ==(Move left, Move right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Move left, Move right)
		{
			return !(left == right);
		}
	}

	public enum ObstructionType : byte
	{
		NO_OBSTRUCTION = 0,
		INVALID_OBSTRUCTION = 1,
		FRIENDLY_OBSTRUCTION = 2,
		ENEMY_OBSTRUCTION = 128
	}
	public enum Conclusion : byte
	{
		NORMAL, CHECKMATE, STALEMATE, TOO_WEAK, FIFTY_MOVE_RULE_VIOLATION, TRIPLE_REPETITION_DRAW
	}
	public sealed class ChatGPTGeneratedAttribute : Attribute{
		
	}
}
