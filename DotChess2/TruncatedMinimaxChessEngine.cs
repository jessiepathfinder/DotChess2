
using System.Runtime.CompilerServices;
using System.Security.Cryptography;


namespace DotChess2
{
	/// <summary>
	/// A chess engine that does not enforce the triple repetition rule
	/// </summary>
	public interface ISimpleChessEngine
	{
		public Move ComputeMove(BoardState boardState, ReadOnlySpan<Move> permittedMoves, bool blackToMove);
	}
	public sealed class RandomChessEngine : ISimpleChessEngine
	{
		private RandomChessEngine() { }
		public static readonly RandomChessEngine instance = new RandomChessEngine();
		public Move ComputeMove(BoardState boardState, ReadOnlySpan<Move> permittedMoves, bool blackToMove)
		{
			int len = permittedMoves.Length;
			if (len == 1) return permittedMoves[0];
			return permittedMoves[RandomNumberGenerator.GetInt32(0, len)];
		}
	}
	public sealed class TruncatedMinimaxChessEngine : ISimpleChessEngine
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Coordinate FindKingUnsafe(BoardStateNoEnPassant bs, uint p)
		{
			for (ulong i = 0; true; ++i)
			{
				if (bs.ReadRawUnsafe(i) == p)
				{
					int i1 = (int)i;
					return new Coordinate(i1 & 7, i1 >> 3);
				}
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Move ComputeMove(BoardState boardState, ReadOnlySpan<Move> permittedMoves, bool blackToMove)
		{
			int lim = permittedMoves.Length;
			//queen's gambit opening
			if (lim == 1) return permittedMoves[0];
			if(boardState.boardStateNoEnPassant == BoardStateNoEnPassant.Initial()){
				Move tm = new Move(new Coordinate(3,1), new Coordinate(3,3));
				for(int i = 0; i < lim; ++i){
					if (permittedMoves[i] == tm) return tm;
				}
			}
			
			var nbs = boardState.boardStateNoEnPassant;
			if (Walker.CheckInsufMaterialDrawFastUnsafe(nbs)) return permittedMoves[RandomNumberGenerator.GetInt32(0, lim)];
			Span<Coordinate> cs = stackalloc Coordinate[436];
			Span<Coordinate> a = cs.Slice(0, 218);
			Span<Coordinate> b = cs.Slice(218, 218);
			BoardStateNoEnPassant identity;
			uint pa = boardState.enPassantOffset;


			
			if (pa == 0)
			{
				identity = Walker.GetIdentity(nbs).identity;
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
				identity = Walker.GetIdentityAssumeNoEP(nbs).identity;
			}
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

			uint eightIfBlack = blackToMove ? 8u : 0u;
			//NOTE: DON'T deduct from max depth remains if king is in check
			Coordinate kingcoord = FindKingUnsafe(nbs, eightIfBlack ^ 15);
			Span<uint> sortkeys = stackalloc uint[lim];
			for (int i = 0; i < lim; ++i)
			{
				Move move = new Move(a[i], b[i]);
				pm1[i] = move;
				BoardState boardState1 = Walker.ApplyMoveUnsafe(boardState, move);
				sortkeys[i] = 256 - Walker.GetKillerMoveScoreUnsafe2(boardState, boardState1.boardStateNoEnPassant, move, kingcoord, eightIfBlack);
			}
			sortkeys.Sort(pm1);


			int depth;
			if (Walker.ChkLegalKingPositionUnsafe(nbs, FindKingUnsafe(nbs, 15 | eightIfBlack), eightIfBlack ^ 8))
			{
				depth = 5;
			}
			else
			{
				depth = 6;
			}
			Span<Move> candid = stackalloc Move[lim];

			uint wp = 0;
			uint bp = 0;
			for(ulong i = 0; i < 64; ++i){
				uint piec = nbs.ReadRawUnsafe(i);
				if (piec == 0) continue;
				uint incr = (piec & 8) >> 3;
				bp += incr;
				wp += 1 ^ incr;
			}
			bool iterative_deepening = (wp < 4) | (bp < 4);

			ulong prevSearchHash = 0;
		restart_search:
			int alpha = -65536;
			int beta = 65536;
			//CREDIT: random.org
			ulong searchHash = 0x62bb204179699ac8u;
			for (int i = 0; i < lim; ++i)
			{
				Move move = permittedMoves[i];
				int score = AlphaBetaPruning(Walker.ApplyMoveUnsafe(boardState, move), a, b, s, cache, alpha, beta, depth, nbpm, 0);
				if (blackToMove)
				{
					if (score < beta)
					{
						beta = score;
					}
				}
				else
				{
					if (score > alpha)
					{
						alpha = score;
					}
				}

				score *= multi;
				score += (int)(sortkeys[i] & 1u);

				if (score > bestScore)
				{
					ndraw = 1;
					bestScore = score;
					candid[0] = move;
				}
				else if (score == bestScore)
				{
					candid[ndraw++] = move;
				}
				if (score == 65537) return move;
				if(iterative_deepening){
					ulong score1;
					if(score == -65536){
						//CREDIT: random.org
						score1 = 0x5ce2292f5281539eu;
					} else{
						score1 = (ulong)(score + 94);
						score1 |= score1 << 8;
						score1 |= score1 << 16;
						score1 |= score1 << 32;
					}
					searchHash += score1;
					searchHash ^= searchHash << 13;
					searchHash ^= searchHash >> 7;
					searchHash ^= searchHash << 17;

				}
			}
			if(iterative_deepening){
				if(prevSearchHash > 0 & prevSearchHash != searchHash){
					goto noidp;
				}
				prevSearchHash = searchHash;
				++depth;

				//FILTER cache: DELETE ALL indeterminate entries
				Dictionary<BoardStateNoEnPassant, (int, int)> newcache = new();
				foreach (KeyValuePair<BoardStateNoEnPassant, (int, int)> kvp in cache)
				{
					(int score, int depth1) = kvp.Value;
					if (depth1 == int.MaxValue)
					{
						newcache.Add(kvp.Key, (score, int.MaxValue));
					}
				}
				cache = newcache;
				goto restart_search;
			}
		noidp:
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

		public static int GetHeuristicAdvantage(BoardStateNoEnPassant boardState)
		{
			int adv = 0;
			for (ulong i = 0; i < 64; ++i)
			{
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
			BoardStateNoEnPassant nbs = boardState.boardStateNoEnPassant;
			
			int delta = Walker.GetAllLegalMovesUnsafe(boardState, a, b, blackToMove);
			
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
				if (Walker.CheckInsufMaterialDrawFastUnsafe(nbs)) return 0;
				//UNLIMITED depth regime
				maxDepthRemains = int.MaxValue;
			}
			uint pa = boardState.enPassantOffset;

			if (pa == 0)
			{
				(identity, multiply) = Walker.GetIdentityAssumeNoEP(nbs);
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
							(identity, multiply) = Walker.GetIdentityAssumePawn(boardState.ToCompressedEPFormUnsafe());
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
			cache[identity] = (res, ((res == 65536) | (res == -65536)) ? int.MaxValue : maxDepthRemains);
			return res;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CheckUnlimitedDepthEligible(BoardStateNoEnPassant boardStateNoEnPassant)
		{
			int tp = 0;
			for (ulong i = 0; i < 64; ++i)
			{
				ulong x = boardStateNoEnPassant.ReadRawUnsafe(i);
				if (x == 0) continue;
				
				if (tp > 3) return false;
				++tp;
			}
			return true;
		}

		

		private sealed class RecursivePruningJob
		{
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
			public void Run()
			{
				var a = array.AsSpan();
				outcome = AlphaBetaPruningImpl3(boardState, a.Slice(0, 218), a.Slice(218, 218), shadowChessStackMoves, reached, cache, alpha, beta, maxDepthRemains, blackToMove, 0);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int AlphaBetaPruningImpl3(BoardState boardState, Span<Coordinate> a, Span<Coordinate> b, int shadowChessStackMoves, HashSet<BoardStateNoEnPassant> reached, Dictionary<BoardStateNoEnPassant, (int score, int depth)> cache, int alpha, int beta, int maxDepthRemains, bool blackToMove, uint ndepth)
		{
			if (ndepth == 64)
			{
				//AVOID stack overflow exceptions
				Coordinate[] clist = GC.AllocateUninitializedArray<Coordinate>(436);
				for (int i = 0; i < shadowChessStackMoves; ++i)
				{
					clist[i] = a[i];
					clist[i + 218] = b[i];
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
			
			uint eightIfBlack = blackToMove ? 8u : 0u;
			//NOTE: DON'T deduct from max depth remains if king is in check
			BoardStateNoEnPassant boardStateNoEnPassant = boardState.boardStateNoEnPassant;
			Coordinate kingcoord = FindKingUnsafe(boardStateNoEnPassant, eightIfBlack ^ 15);
			Span<uint> sortkeys = stackalloc uint[shadowChessStackMoves];
			int walk1;
			if (shadowChessStackMoves > 1){
				
				walk1 = 0;
				int walk2 = 0;
				for(int i = 0; i < shadowChessStackMoves; ++i){
					ref Move ma = ref moves[i];
					Move move = new Move(a[i],b[i]);
					BoardState boardState1 = Walker.ApplyMoveUnsafe(boardState, move);
					if(cache.TryGetValue(Walker.GetIdentity(boardState1).Item1, out var x)){
						if(x.depth >= maxDepthRemains){
							
							ref Move mb = ref moves[walk1++];
							ma = mb;
							mb = move;
							continue;
						}
					}
					ma = move;
					sortkeys[walk2++] = uint.MaxValue - Walker.GetKillerMoveScoreUnsafe2(boardState, boardState1.boardStateNoEnPassant,move,kingcoord,eightIfBlack);
				}
				if(walk2 < 2){
					walk1 = int.MaxValue;
				}

			} else{
				moves[0] = new Move(a[0], b[0]);
				walk1 = int.MaxValue;
			}
			
			uint expectedKing = eightIfBlack | 7;

			for (ulong kindex = 0; true; ++kindex)
			{
				if (boardStateNoEnPassant.ReadRawUnsafe(kindex) == expectedKing)
				{
					int iki = (int)kindex;
					if (Walker.ChkLegalKingPositionUnsafe(boardStateNoEnPassant, new Coordinate(iki & 7, iki >>> 3), (expectedKing & 8) ^ 8))
					{
						break;
					}
					else
					{
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
					
					if(i == walk1){
						int walk2 = shadowChessStackMoves - walk1;
						sortkeys.Slice(0, walk2).Sort(moves.Slice(walk1));
					}
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
							false, ndepth);

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
					if (i == walk1)
					{
						sortkeys.Slice(0, shadowChessStackMoves - walk1).Sort(moves.Slice(walk1));
					}
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
							true,
							ndepth);

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