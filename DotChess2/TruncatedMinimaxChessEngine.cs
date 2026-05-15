using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DotChess2
{
	/// <summary>
	/// A chess engine that does not enforce the triple repetition rule
	/// </summary>
	public interface ISimpleChessEngine{
		public Move ComputeMove(BoardState boardState,ReadOnlySpan<Move> permittedMoves,bool blackToMove);
	}
	public sealed class RandomChessEngine : ISimpleChessEngine
	{
		private RandomChessEngine(){ }
		public static readonly RandomChessEngine instance = new RandomChessEngine();
		public Move ComputeMove(BoardState boardState, ReadOnlySpan<Move> permittedMoves,bool blackToMove)
		{
			int len = permittedMoves.Length;
			if (len == 1) return permittedMoves[0];
			return permittedMoves[RandomNumberGenerator.GetInt32(0, len)];
		}
	}
	public sealed class TruncatedMinimaxChessEngine : ISimpleChessEngine
	{
		private readonly struct EphemeralKillerSorterOld : IComparer<Move> {
			private readonly BoardState boardState;
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public EphemeralKillerSorterOld(BoardState boardState) {
				this.boardState = boardState;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int Compare(Move x, Move y) {
				return Walker.GetKillerMoveScoreUnsafe(boardState, y).CompareTo(Walker.GetKillerMoveScoreUnsafe(boardState, x));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Move ComputeMove(BoardState boardState, ReadOnlySpan<Move> permittedMoves, bool blackToMove)
		{
			int lim = permittedMoves.Length;
			if (lim == 1) return permittedMoves[0];
			
			Span<Coordinate> cs = stackalloc Coordinate[436];
			Span<Coordinate> a = cs.Slice(0, 218);
			Span<Coordinate> b = cs.Slice(218, 218);
			BoardStateNoEnPassant identity;
			uint pa = boardState.enPassantOffset;

			

			if (pa == 0)
			{
				identity = Walker.GetIdentity(boardState.boardStateNoEnPassant).identity;
			}
			else
			{
				ulong pa1 = pa;
				for (int i = 0; i < lim; ++i)
				{
					var des = permittedMoves[i].destination;
					if (boardState.GetUnsafe(des) == 0)
					{
						//AVOID knight underpromotion moves
						if ((des.y != 0 & des.y != 7))
						{
							identity = Walker.GetIdentityAssumePawn(boardState.ToCompressedEPFormUnsafe()).identity;
							goto nopi;
						}
					}
				}
				identity = Walker.GetIdentityAssumeNoEP(boardState.boardStateNoEnPassant).identity;
			}
			identity = Walker.GetIdentityAssumeNoEP(identity).identity;
		nopi:
			HashSet<BoardStateNoEnPassant> s = new();
			s.Add(identity);

			int ndraw = 0;
			int bestScore = -65536;
			int multi = blackToMove ? (-1) : 1;
			bool nbpm = !blackToMove;
			Dictionary<BoardStateNoEnPassant, (int, int)> cache = new();

			Span<Move> pm1 = stackalloc Move[lim];
			permittedMoves.CopyTo(pm1);
			pm1.Sort(new EphemeralKillerSorterOld(boardState));

			Span<Move> candid = stackalloc Move[lim];
			int alpha = -65536;
			int beta = 65536;
			for (int i = 0; i < lim; ++i){
				Move move = permittedMoves[i];

				int score = AlphaBetaPruning(Walker.ApplyMoveUnsafe(boardState, move), a, b, s, cache, alpha, beta, 5, nbpm, 0);
				if(blackToMove){
					if(score < beta){
						beta = score;
					}
				} else{
					if(score > alpha){
						alpha = score;
					}
				}

				score *= multi;

				if(score > bestScore){
					ndraw = 1;
					bestScore = score;
					candid[0] = move;
				} else if(score == bestScore){
					candid[ndraw++] = move;
				}
				if (score == 65536) break;

			}
			if (ndraw == 0) return permittedMoves[RandomNumberGenerator.GetInt32(0, lim)];
			if (ndraw == 1) return candid[0];
			return candid[RandomNumberGenerator.GetInt32(0, ndraw)];
			
			
		}

		//Branch predictor state isolation optimization
		[MethodImpl(MethodImplOptions.NoOptimization)]
		private static int AlphaBetaPruning(BoardState boardState, Span<Coordinate> a, Span<Coordinate> b, HashSet<BoardStateNoEnPassant> reached, Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache, int alpha, int beta, int maxDepthRemains, bool blackToMove, uint ndepth)
		{
			if (blackToMove)
			{
				return AlphaBetaPruningBlack(boardState, a, b, reached, cache, alpha, beta, maxDepthRemains, ndepth);
			}
			else
			{
				return AlphaBetaPruningWhite(boardState, a, b, reached, cache, alpha, beta, maxDepthRemains, ndepth);
			}
		}
		private static int AlphaBetaPruningBlack(BoardState boardState, Span<Coordinate> a, Span<Coordinate> b, HashSet<BoardStateNoEnPassant> reached, Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache, int alpha, int beta, int maxDepthRemains, uint ndepth)
		{
			return AlphaBetaPruningImpl(boardState, a, b, reached, cache, alpha, beta, maxDepthRemains, true, ndepth);
		}
		private static int AlphaBetaPruningWhite(BoardState boardState, Span<Coordinate> a, Span<Coordinate> b, HashSet<BoardStateNoEnPassant> reached, Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache, int alpha, int beta, int maxDepthRemains, uint ndepth)
		{
			return AlphaBetaPruningImpl(boardState, a, b, reached, cache, alpha, beta, maxDepthRemains, false, ndepth);
		}

		public static int GetHeuristicAdvantage(BoardStateNoEnPassant boardState){
			int adv = 0;
			for(ulong i = 0; i < 64; ++i){
				uint p = boardState.ReadRawUnsafe(i);
				adv += ((0x09553310 >> (int)((p & 7u) << 2)) & 15) * (1 - (((int)(p >> 3)) << 1));
			}
			return adv;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int AlphaBetaPruningImpl(BoardState boardState, Span<Coordinate> a, Span<Coordinate> b, HashSet<BoardStateNoEnPassant> reached, Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache, int alpha, int beta, int maxDepthRemains, bool blackToMove, uint ndepth)
		{

			if (maxDepthRemains == 0)
			{
				Conclusion conclusion = Walker.GetConclusionFastUnsafe(boardState, blackToMove);
				if (conclusion == Conclusion.CHECKMATE) return blackToMove ? 65536 : -65536;
				if (conclusion != Conclusion.NORMAL) return 0;
				return GetHeuristicAdvantage(boardState.boardStateNoEnPassant);
			}

			int delta = Walker.GetAllLegalMovesUnsafe(boardState, a, b, blackToMove);
			BoardStateNoEnPassant nbs = boardState.boardStateNoEnPassant;
			if (delta == 0)
			{
				uint expectedKing = blackToMove ? 15u : 7u;
				ulong kingCoord = 0;
				while (true)
				{
					if (nbs.ReadRawUnsafe(kingCoord) == expectedKing)
					{
						break;
					}
					++kingCoord;
				}
				int ikc = (int)kingCoord;
				if (Walker.ChkLegalKingPositionUnsafe(nbs, new Coordinate(ikc & 7, ikc >> 3), (expectedKing & 8) ^ 8))
				{
					return 0;
				}
				return blackToMove ? 65536 : -65536;
			}
			BoardStateNoEnPassant identity;
			int multiply;
			if (CheckUnlimitedDepthEligible(nbs))
			{
				//UNLIMITED depth regime
				maxDepthRemains = int.MaxValue;
			}
			uint pa = boardState.enPassantOffset;

			if (pa == 0)
			{
				(identity,multiply) = Walker.GetIdentityAssumeNoEP(nbs);
			}
			else
			{
				for (int i = 0; i < delta; ++i)
				{
					var des = b[i];
					if (boardState.GetUnsafe(des) == 0)
					{
						//AVOID knight underpromotion moves
						if ((des.y != 0 & des.y != 7))
						{
							(identity,multiply) = Walker.GetIdentityAssumePawn(boardState.ToCompressedEPFormUnsafe());
							goto nopi;
						}
					}
				}
				(identity, multiply) = Walker.GetIdentityAssumeNoEP(nbs);
			}
		nopi:
			
			if (cache.TryGetValue(identity, out var result))
			{
				if (result.depth < maxDepthRemains)
				{
					goto nores;
				}
				return result.score * multiply;
			}
		nores:;
			reached.Add(identity);
			int res = AlphaBetaPruningImpl3(boardState, a, b, delta, reached, cache, alpha, beta, maxDepthRemains, blackToMove, ndepth);
			reached.Remove(identity);
			//NOTE: Cache doesn't care about depth if we have a definite win/lose evaluation
			cache[identity] = (res,  ((res == 65536) | (res == -65536)) ? int.MaxValue : maxDepthRemains);
			return res;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CheckUnlimitedDepthEligible(BoardStateNoEnPassant boardStateNoEnPassant){
			ulong whitePieces = 0;
			ulong blackPieces = 0;
			for(ulong i = 0; i < 64; ++i){
				ulong x = boardStateNoEnPassant.ReadRawUnsafe(i);
				if (x == 0) continue;
				ulong bp = x >> 3;
				blackPieces += bp;
				whitePieces += 1 - bp;
				if ((blackPieces > 3) & (whitePieces > 3)) return false;
			}
			return true;
		}

		private readonly struct EphemeralKillerSorter : IComparer<Move>
		{
			private readonly BoardState boardState;

			private readonly
			Dictionary<
				BoardStateNoEnPassant,
				(int score, int depth)>
			cache;

			private readonly int maxDepthRemains;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public EphemeralKillerSorter(
				BoardState boardState,
				Dictionary<
					BoardStateNoEnPassant,
					(int score, int depth)>
				cache,
				int maxDepthRemains)
			{
				this.boardState = boardState;
				this.cache = cache;
				this.maxDepthRemains =
					maxDepthRemains;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private uint GetCacheAwareKillerScoreUnsafe(
				Move move)
			{
				uint score =
					Walker.GetKillerMoveScoreUnsafe(
						boardState,	move);






				// =====================================
				// Cache hit bonus
				// =====================================

				if (cache.TryGetValue(Walker.GetIdentity(Walker.ApplyMoveUnsafe(boardState,move)).Item1,out var result))
				{

					if (
						(result.depth >=
						maxDepthRemains) | (result.score == 65536) | (result.score == -65536))
					{
						return int.MaxValue;
					}
				}


				return score;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int Compare(Move x, Move y)
			{
				return
					GetCacheAwareKillerScoreUnsafe(y)
					.CompareTo(
					GetCacheAwareKillerScoreUnsafe(x));
			}
		}

		private sealed class RecursivePruningJob{
			private readonly BoardState boardState;
			private readonly Coordinate[] array;
			private readonly int shadowChessStackMoves;
			private readonly HashSet<BoardStateNoEnPassant> reached;
			private readonly Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache;
			private readonly int alpha;
			private readonly int beta;
			private readonly int maxDepthRemains;
			private readonly bool blackToMove;
			public int outcome;

			public RecursivePruningJob(BoardState boardState, Coordinate[] array, int shadowChessStackMoves, HashSet<BoardStateNoEnPassant> reached, Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache, int alpha, int beta, int maxDepthRemains, bool blackToMove)
			{
				this.boardState = boardState;
				this.array = array;
				this.shadowChessStackMoves = shadowChessStackMoves;
				this.reached = reached;
				this.cache = cache;
				this.alpha = alpha;
				this.beta = beta;
				this.maxDepthRemains = maxDepthRemains;
				this.blackToMove = blackToMove;
			}
			public void Run() {
				var a = array.AsSpan();
				outcome = AlphaBetaPruningImpl3(boardState, a.Slice(0, 218), a.Slice(218, 218), shadowChessStackMoves, reached, cache, alpha, beta, maxDepthRemains, blackToMove, 0);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int AlphaBetaPruningImpl3(BoardState boardState, Span<Coordinate> a, Span<Coordinate> b, int shadowChessStackMoves, HashSet<BoardStateNoEnPassant> reached, Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache, int alpha, int beta, int maxDepthRemains, bool blackToMove,uint ndepth)
		{
			if(ndepth == 64){
				//AVOID stack overflow exceptions
				Coordinate[] clist = GC.AllocateUninitializedArray<Coordinate>(436);
				for (int i = 0; i < shadowChessStackMoves; ++i)
				{
					clist[i] = a[i];
					clist[i+218] = b[i];
				}
				RecursivePruningJob recursivePruningJob = new RecursivePruningJob(boardState, clist, shadowChessStackMoves, reached, cache, alpha, beta, maxDepthRemains, blackToMove);
				Thread thread = new Thread(recursivePruningJob.Run);
				thread.Name = "DotChess Ephemeral Stack Extender Thread";
				thread.IsBackground = true;
				thread.Start();
				thread.Join();
				return recursivePruningJob.outcome;
			}
			++ndepth;

			Span<Move> moves = stackalloc Move[shadowChessStackMoves];
			//Killer heuristic
			for(int i = 0; i < shadowChessStackMoves; ++i){
				moves[i] = new Move(a[i], b[i]);
			}
			if(shadowChessStackMoves > 1) moves.Sort(new EphemeralKillerSorter(boardState, cache, maxDepthRemains));

			//NOTE: DON'T deduct from max depth remains if king is in check
			uint expectedKing = blackToMove ? 15u : 7u;
			BoardStateNoEnPassant boardStateNoEnPassant = boardState.boardStateNoEnPassant;
			for(ulong kindex = 0; true;++kindex){
				if(boardStateNoEnPassant.ReadRawUnsafe(kindex) == expectedKing){
					int iki = (int)kindex;
					if(Walker.ChkLegalKingPositionUnsafe(boardStateNoEnPassant, new Coordinate(iki & 7, iki >>> 3),(expectedKing & 8) ^ 8)){
						break;
					} else{
						goto nodeduct;

					}
				}
			}
			
			--maxDepthRemains;
		nodeduct:;
			if (blackToMove)
			{
				int best = 65536;

				for (int i = 0; i < shadowChessStackMoves; ++i)
				{
					Move move = moves[i];

					BoardState child =
						Walker.ApplyMoveUnsafe(
							boardState,
							move);

					BoardStateNoEnPassant childIdentity =
						Walker.GetIdentity(
							child.boardStateNoEnPassant)
						.identity;

					if (reached.Contains(childIdentity))
					{
						continue;
					}

					int score =
						AlphaBetaPruning(
							child,
							a,
							b,
							reached,
							cache,
							alpha,
							beta,
							maxDepthRemains,
							false,ndepth);

					if (score < best)
					{
						best = score;
					}

					if (score < beta)
					{
						beta = score;
					}

					if (beta <= alpha)
					{
						break;
					}
				}

				return best;
			}
			else
			{
				int best = -65536;

				for (int i = 0; i < shadowChessStackMoves; ++i)
				{
					Move move = moves[i];

					BoardState child =
						Walker.ApplyMoveUnsafe(
							boardState,
							move);

					BoardStateNoEnPassant childIdentity =
						Walker.GetIdentity(
							child.boardStateNoEnPassant)
						.identity;

					if (reached.Contains(childIdentity))
					{
						continue;
					}

					int score =
						AlphaBetaPruning(
							child,
							a,
							b,
							reached,
							cache,
							alpha,
							beta,
							maxDepthRemains,
							true,ndepth);

					if (score > best)
					{
						best = score;
					}

					if (score > alpha)
					{
						alpha = score;
					}

					if (beta <= alpha)
					{
						break;
					}
				}

				return best;
			}



		}
	}
}
