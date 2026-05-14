using NUnit.Framework;
using System;

namespace DotChess2.Tests
{
	[TestFixture]
	[ChatGPTGenerated]
	public sealed class WalkerTests
	{


		

		[Test]
		[ChatGPTGenerated]
		public void WhitePawnDoublePush_ShouldCreateEnPassantState()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			Move move = new Move(
				new Coordinate(4, 1),
				new Coordinate(4, 3));

			BoardState next =
				Walker.ApplyMoveUnsafe(board, move);

			uint piece =
				next.ReadRawUnsafe((uint)(4 | (3 << 3)));


			Assert.That(
				(piece & (uint)GamePiece.EnPassantCapturable) != 0,
				Is.True);

			Assert.That(
				(next.enPassantOffset - 1),
				Is.EqualTo(4 | (3 << 3)));
		}

		[Test]
		[ChatGPTGenerated]
		public void KingsideCastle_ShouldMoveRook()
		{
			BoardState board =
				new BoardState(BoardStateNoEnPassant.Initial());

			// Remove knight + bishop + queen-side clutter manually

			board = board.DeleteUnsafe(5); // bishop
			board = board.DeleteUnsafe(6); // knight

			Move castle = new Move(
				new Coordinate(4, 0),
				new Coordinate(6, 0));

			BoardState next =
				Walker.ApplyMoveUnsafe(board, castle);

			uint king =
				next.ReadRawUnsafe((uint)(6 | (0 << 3)));

			uint rook =
				next.ReadRawUnsafe((uint)(5 | (0 << 3)));

			Assert.That(
				(king & 7),
				Is.EqualTo((uint)GamePiece.King));

			Assert.That(
				(rook & 7),
				Is.EqualTo((uint)GamePiece.Rook));
		}

		[Test]
		[ChatGPTGenerated]
		public void KnightPromotion_ShouldProduceKnight()
		{
			BoardState board =
				new BoardState(
					new BoardStateNoEnPassant(
						System.Runtime.Intrinsics.Vector256<ulong>.Zero),
					0);

			// White pawn on F7

			board = board.WriteRawUnsafe(
				(5 | (6 << 3)),
				(uint)GamePiece.Pawn);

			// White king
			board = board.WriteRawUnsafe(
				(4 | (0 << 3)),
				(uint)GamePiece.King);

			// Black king
			board = board.WriteRawUnsafe(
				(4 | (7 << 3)),
				(uint)(GamePiece.King | GamePiece.Black));

			Move promote = new Move(
				new Coordinate(5, 6),
				new Coordinate(5, 6));

			BoardState next =
				Walker.ApplyMoveUnsafe(board, promote);

			uint piece =
				next.ReadRawUnsafe((uint)(5 | (7 << 3)));
			Assert.That(
			(piece & 7),
			Is.EqualTo((uint)GamePiece.Knight));
			

			
		}

		

		[Test]
		[ChatGPTGenerated]
		public void EnPassantCapture_ShouldRemoveCapturedPawn()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// E2E4
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 1), new Coordinate(4, 3)));

			// D7D5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(3, 6), new Coordinate(3, 4)));

			// E4D5 EP
			BoardState next =
				Walker.ApplyMoveUnsafe(
					board,
					new Move(
						new Coordinate(4, 3),
						new Coordinate(3, 4)));

			uint pawn =
				next.ReadRawUnsafe((uint)(3 | (4 << 3)));

			uint captured =
				next.ReadRawUnsafe((uint)(3 | (3 << 3)));


			Assert.That(
				(pawn & 7),
				Is.EqualTo((uint)GamePiece.Pawn));

			Assert.That(captured, Is.EqualTo(0));
		}
		[Test]
		[ChatGPTGenerated]
		public void RuyLopezOpening_ShouldProduceExpectedPiecePlacement()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// 1. e4
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 1), new Coordinate(4, 3)));

			// ... e5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 6), new Coordinate(4, 4)));

			// 2. Nf3
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(6, 0), new Coordinate(5, 2)));

			// ... Nc6
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(1, 7), new Coordinate(2, 5)));

			// 3. Bb5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(5, 0), new Coordinate(1, 4)));

			uint bishop =
				board.ReadRawUnsafe((uint)(1 | (4 << 3)));

			uint knight =
				board.ReadRawUnsafe((uint)(2 | (5 << 3)));

			Assert.That(
				(bishop & 7),
				Is.EqualTo((uint)GamePiece.Bishop));

			Assert.That(
				(knight & 7),
				Is.EqualTo((uint)GamePiece.Knight));
		}

		[Test]
		[ChatGPTGenerated]
		public void ScholarsMateSequence_ShouldPlaceQueenOnF7()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// 1. e4
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 1), new Coordinate(4, 3)));

			// ... e5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 6), new Coordinate(4, 4)));

			// 2. Qh5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(3, 0), new Coordinate(7, 4)));

			// ... Nc6
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(1, 7), new Coordinate(2, 5)));

			// 3. Bc4
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(5, 0), new Coordinate(2, 3)));

			// ... Nf6
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(6, 7), new Coordinate(5, 5)));

			// 4. Qxf7#
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(7, 4), new Coordinate(5, 6)));

			uint queen =
				board.ReadRawUnsafe((uint)(5 | (6 << 3)));

			uint blackPawn =
				board.ReadRawUnsafe((uint)(5 | (6 << 3)));

			Assert.That(
				(queen & 7),
				Is.EqualTo((uint)GamePiece.Queen));

			Assert.That(
				(queen & 8),
				Is.EqualTo(0ul));

			Assert.That(
				(blackPawn & 7),
				Is.Not.EqualTo((uint)GamePiece.Pawn));
		}

		[Test]
		[ChatGPTGenerated]
		public void SicilianDefense_ShouldPlaceC5PawnCorrectly()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// 1. e4
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 1), new Coordinate(4, 3)));

			// ... c5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(2, 6), new Coordinate(2, 4)));

			uint whitePawn =
				board.ReadRawUnsafe((uint)(4 | (3 << 3)));

			uint blackPawn =
				board.ReadRawUnsafe((uint)(2 | (4 << 3)));

			Assert.That(
				(whitePawn & 7),
				Is.EqualTo((uint)GamePiece.Pawn));

			Assert.That(
				(blackPawn & 7),
				Is.EqualTo((uint)GamePiece.Pawn));

			Assert.That(
				(blackPawn & 8),
				Is.EqualTo(8ul));
		}

		[Test]
		[ChatGPTGenerated]
		public void KingsideCastleInItalianGame_ShouldRelocateKingAndRook()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// 1. e4
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 1), new Coordinate(4, 3)));

			// ... e5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(4, 6), new Coordinate(4, 4)));

			// 2. Nf3
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(6, 0), new Coordinate(5, 2)));

			// ... Nc6
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(1, 7), new Coordinate(2, 5)));

			// 3. Bc4
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(5, 0), new Coordinate(2, 3)));

			// ... Bc5
			board = Walker.ApplyMoveUnsafe(board, new Move(new Coordinate(5, 7), new Coordinate(2, 4)));

			// 4. O-O
			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(4, 0),
					new Coordinate(6, 0)));

			uint king =
				board.ReadRawUnsafe((uint)(6 | (0 << 3)));

			uint rook =
				board.ReadRawUnsafe((uint)(5 | (0 << 3)));

			Assert.That(
				(king & 7),
				Is.EqualTo((uint)GamePiece.King));

			Assert.That(
				(rook & 7),
				Is.EqualTo((uint)GamePiece.Rook));
		}

		[Test]
		[ChatGPTGenerated]
		public void QueensGambitAccepted_ShouldRemoveCapturedPawn()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// 1. d4
			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(3, 1),
					new Coordinate(3, 3)));

			// ... d5
			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(3, 6),
					new Coordinate(3, 4)));

			// 2. c4
			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(2, 1),
					new Coordinate(2, 3)));

			// ... dxc4
			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(3, 4),
					new Coordinate(2, 3)));

			uint blackPawn =
				board.ReadRawUnsafe((uint)(2 | (3 << 3)));

			uint originalSquare =
				board.ReadRawUnsafe((uint)(3 | (4 << 3)));

			Assert.That(
				(blackPawn & 7),
				Is.EqualTo((uint)GamePiece.Pawn));

			Assert.That(
				(blackPawn & 8),
				Is.EqualTo(8ul));

			Assert.That(originalSquare, Is.EqualTo(0ul));
		}
		[Test]
		[ChatGPTGenerated]
		public void DoublePawnPush_ShouldExpirePreviousEnPassantRights()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// E2E4
			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(4, 1),
					new Coordinate(4, 3)));

			Assert.That(
				board.enPassantOffset,
				Is.EqualTo((4 | (3 << 3)) + 1));

			// A7A6
			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(0, 6),
					new Coordinate(0, 5)));

			// EP rights should vanish after any board modification
			Assert.That(board.enPassantOffset, Is.EqualTo(0));
		}

		[Test]
		[ChatGPTGenerated]
		public void CastleableRook_ShouldBecomeNormalRookAfterMovement()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(0, 0),
					new Coordinate(0, 1)));

			uint rook =
				board.ReadRawUnsafe((uint)(0 | (1 << 3)));

			Assert.That(
				rook & 7,
				Is.EqualTo((uint)GamePiece.Rook));

			Assert.That(
				rook & 7,
				Is.Not.EqualTo((uint)GamePiece.RookCastleable));
		}

		[Test]
		[ChatGPTGenerated]
		public void KingsideCastle_ShouldConsumeCastleableRook()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// Clear path manually
			board = board.DeleteUnsafe(5);
			board = board.DeleteUnsafe(6);

			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(4, 0),
					new Coordinate(6, 0)));

			uint rook =
				board.ReadRawUnsafe((uint)(5 | (0 << 3)));

			Assert.That(
				rook & 7,
				Is.EqualTo((uint)GamePiece.Rook));

			Assert.That(
				rook & 7,
				Is.Not.EqualTo((uint)GamePiece.RookCastleable));
		}

		[Test]
		[ChatGPTGenerated]
		public void MovingKing_ShouldDisableBothCastleRights()
		{
			BoardState board = new BoardState(BoardStateNoEnPassant.Initial());

			// Free E2 for king
			board = board.DeleteUnsafe(12);

			board = Walker.ApplyMoveUnsafe(
				board,
				new Move(
					new Coordinate(4, 0),
					new Coordinate(4, 1)));

			uint leftRook =
				board.ReadRawUnsafe((uint)(0 | (0 << 3)));

			uint rightRook =
				board.ReadRawUnsafe((uint)(7 | (0 << 3)));

			Assert.That(
				leftRook & 7,
				Is.EqualTo((uint)GamePiece.Rook));

			Assert.That(
				rightRook & 7,
				Is.EqualTo((uint)GamePiece.Rook));
		}

		[Test]
		[ChatGPTGenerated]
		public void QueenPromotion_ShouldBecomeQueen()
		{
			BoardState board =
				new BoardState(
					new BoardStateNoEnPassant(
						System.Runtime.Intrinsics.Vector256<ulong>.Zero),
					0);

			// White pawn on A7
			board = board.WriteRawUnsafe(
				(0 | (6 << 3)),
				(uint)GamePiece.Pawn);

			// Kings
			board = board.WriteRawUnsafe(
				(4 | (0 << 3)),
				(uint)GamePiece.King);

			board = board.WriteRawUnsafe(
				(4 | (7 << 3)),
				(uint)(GamePiece.King | GamePiece.Black));

			BoardState next =
				Walker.ApplyMoveUnsafe(
					board,
					new Move(
						new Coordinate(0, 6),
						new Coordinate(0, 7)));

			uint promoted =
				next.ReadRawUnsafe((uint)(0 | (7 << 3)));


			Assert.That(
				promoted & 7,
				Is.EqualTo((uint)GamePiece.Queen));
		}

		[Test]
		[ChatGPTGenerated]
		public void QueenPromotionCapture_ShouldAwardCapturePlusPromotion()
		{
			BoardState board =
				new BoardState(
					new BoardStateNoEnPassant(
						System.Runtime.Intrinsics.Vector256<ulong>.Zero),
					0);

			// White pawn on B7
			board = board.WriteRawUnsafe(
				(1 | (6 << 3)),
				(uint)GamePiece.Pawn);

			// Black rook on C8
			board = board.WriteRawUnsafe(
				(2 | (7 << 3)),
				(uint)(GamePiece.Rook | GamePiece.Black));

			// Kings
			board = board.WriteRawUnsafe(
				(4 | (0 << 3)),
				(uint)GamePiece.King);

			board = board.WriteRawUnsafe(
				(4 | (7 << 3)),
				(uint)(GamePiece.King | GamePiece.Black));

			BoardState next =
				Walker.ApplyMoveUnsafe(
					board,
					new Move(
						new Coordinate(1, 6),
						new Coordinate(2, 7)));

			uint promoted =
				next.ReadRawUnsafe((uint)(2 | (7 << 3)));


			Assert.That(
				promoted & 7,
				Is.EqualTo((uint)GamePiece.Queen));
		}

		

		[Test]
		[ChatGPTGenerated]
		public void EnPassantCapture_ShouldRemoveCorrectPawn()
		{
			BoardState board =
				new BoardState(
					new BoardStateNoEnPassant(
						System.Runtime.Intrinsics.Vector256<ulong>.Zero),
					0);

			// White pawn E5
			board = board.WriteRawUnsafe(
				(4 | (4 << 3)),
				(uint)GamePiece.Pawn);

			// Black pawn D5 with EP flag
			board = board.WriteRawUnsafe(
				(3 | (4 << 3)),
				(uint)(GamePiece.Pawn |
				GamePiece.Black |
				GamePiece.EnPassantCapturable));

			// Kings
			board = board.WriteRawUnsafe(
				(4 | (0 << 3)),
				(uint)GamePiece.King);

			board = board.WriteRawUnsafe(
				(4 | (7 << 3)),
				(uint)(GamePiece.King | GamePiece.Black));

			BoardState next =
				Walker.ApplyMoveUnsafe(
					board,
					new Move(
						new Coordinate(4, 4),
						new Coordinate(3, 5)));

			uint whitePawn =
				next.ReadRawUnsafe((uint)(3 | (5 << 3)));

			uint capturedPawn =
				next.ReadRawUnsafe((uint)(3 | (4 << 3)));


			Assert.That(
				whitePawn & 7,
				Is.EqualTo((uint)GamePiece.Pawn));

			Assert.That(capturedPawn, Is.EqualTo(0));
		}

		[Test]
		[ChatGPTGenerated]
		public void BoardStateEquality_ShouldRespectEnPassantOffset()
		{
			BoardState a = new BoardState(BoardStateNoEnPassant.Initial());

			BoardState b =
				Walker.ApplyMoveUnsafe(
					new BoardState(BoardStateNoEnPassant.Initial()),
					new Move(
						new Coordinate(4, 1),
						new Coordinate(4, 3)));

			Assert.That(a.Equals(b), Is.False);
		}

		[Test]
		[ChatGPTGenerated]
		public void NormalizePiece_ShouldConvertCastleableRook()
		{
			GamePiece white =
				Walker.NormalizePiece(
					GamePiece.RookCastleable);

			GamePiece black =
				Walker.NormalizePiece(
					GamePiece.RookCastleable |
					GamePiece.Black);

			Assert.That(
				white,
				Is.EqualTo(GamePiece.Rook));

			Assert.That(
				black,
				Is.EqualTo(
					GamePiece.Rook |
					GamePiece.Black));
		}

		[Test]
		[ChatGPTGenerated]
		public void FastBoundCheck_ShouldRejectOutOfBoundsCoordinates()
		{
			Assert.That(
				Walker.FastBoundCheck(-1, 0),
				Is.False);

			Assert.That(
				Walker.FastBoundCheck(0, -1),
				Is.False);

			Assert.That(
				Walker.FastBoundCheck(8, 0),
				Is.False);

			Assert.That(
				Walker.FastBoundCheck(0, 8),
				Is.False);

			Assert.That(
				Walker.FastBoundCheck(7, 7),
				Is.True);

			Assert.That(
				Walker.FastBoundCheck(0, 0),
				Is.True);
		}
		[Test]
		[ChatGPTGenerated]
		public void InitialPosition_HasLegalMoves()
		{
			SimpleRenderer sr =
				new SimpleRenderer(new BoardState(BoardStateNoEnPassant.Initial()));

			Assert.That(
				Walker.HasAnyLegalMoveUnsafe(
					sr.BoardState,
					false),
				Is.True);

			Assert.That(
				sr.GetConclusion(),
				Is.EqualTo(
					Conclusion.NORMAL));
		}
		[Test]
		[ChatGPTGenerated]
		public void InitialPosition_IsNormalConclusion()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			Conclusion result =
				Walker.GetConclusionFastUnsafe(
					bs,
					false);

			Assert.That(
				result,
				Is.EqualTo(
					Conclusion.NORMAL));
		}
		[Test]
		[ChatGPTGenerated]
		public void InitialPosition_Contains20LegalMoves()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			Span<Coordinate> origins =
				stackalloc Coordinate[256];

			Span<Coordinate> destinations =
				stackalloc Coordinate[256];

			int moves =
				Walker.GetAllLegalMovesUnsafe(
					bs,
					origins,
					destinations,
					false);

			Assert.That(moves, Is.EqualTo(20));
		}
		[Test]
		[ChatGPTGenerated]
		public void E2E4_IsLegal()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			Move move =
				new Move(
					new Coordinate(4, 1),
					new Coordinate(4, 3));

			BoardState next =
				Walker.ApplyMoveUnsafe(
					bs,
					move);

			bool safe =
				Walker.ChkLegalKingPositionUnsafe(
					next.boardStateNoEnPassant,
					new Coordinate(4, 0),
					8u);

			Assert.That(safe, Is.True);
		}
		[Test]
		[ChatGPTGenerated]
		public void ApplyMoveUnsafe_WhiteKingsideCastle_MovesRookCorrectly()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			// Clear F1 + G1
			bs = bs.DeleteUnsafe(5);
			bs = bs.DeleteUnsafe(6);

			// White castles kingside
			BoardState result =
				Walker.ApplyMoveUnsafe(
					bs,
					new Move(
						new Coordinate(4, 0),
						new Coordinate(6, 0)));

			uint king =
				result.GetUnsafe(
					new Coordinate(6, 0));

			uint rook =
				result.GetUnsafe(
					new Coordinate(5, 0));

			Assert.That(
				king & 7,
				Is.EqualTo(
					(uint)GamePiece.King));

			Assert.That(
				rook & 7,
				Is.EqualTo(
					(uint)GamePiece.Rook));

			Assert.That(
				result.GetUnsafe(
					new Coordinate(7, 0)),
				Is.EqualTo(0ul));
		}

		[Test]
		[ChatGPTGenerated]
		public void ApplyMoveUnsafe_BlackQueensideCastle_MovesRookCorrectly()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			// Clear B8/C8/D8
			bs = bs.DeleteUnsafe(57);
			bs = bs.DeleteUnsafe(58);
			bs = bs.DeleteUnsafe(59);

			BoardState result =
				Walker.ApplyMoveUnsafe(
					bs,
					new Move(
						new Coordinate(4, 7),
						new Coordinate(2, 7)));

			uint king =
				result.GetUnsafe(
					new Coordinate(2, 7));

			uint rook =
				result.GetUnsafe(
					new Coordinate(3, 7));

			Assert.That(
				king,
				Is.EqualTo(
					(uint)GamePiece.King |
					(uint)GamePiece.Black));

			Assert.That(
				rook,
				Is.EqualTo(
					(uint)GamePiece.Rook |
					(uint)GamePiece.Black));

			Assert.That(
				result.GetUnsafe(
					new Coordinate(0, 7)),
				Is.EqualTo(0ul));
		}

		[Test]
		[ChatGPTGenerated]
		public void ApplyMoveUnsafe_WhiteQueenPromotion_PromotesCorrectly()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			// Remove blocking black rook
			bs = bs.DeleteUnsafe(56);

			// Place white pawn on A7
			bs = bs.DeleteUnsafe(8);

			bs = bs.WriteRawUnsafe(
				48,
				(uint)GamePiece.Pawn);

			BoardState result =
				Walker.ApplyMoveUnsafe(
					bs,
					new Move(
						new Coordinate(0, 6),
						new Coordinate(0, 7)));

			uint promoted =
				result.GetUnsafe(
					new Coordinate(0, 7));

			Assert.That(
				promoted & 7,
				Is.EqualTo(
					(uint)GamePiece.Queen));
		}

		[Test]
		[ChatGPTGenerated]
		public void ApplyMoveUnsafe_BlackQueenPromotion_PromotesCorrectly()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			// Remove blocking white rook
			bs = bs.DeleteUnsafe(0);

			// Place black pawn on A2
			bs = bs.DeleteUnsafe(48);

			bs = bs.WriteRawUnsafe(
				8,
				(uint)GamePiece.Pawn |
				(uint)GamePiece.Black);

			BoardState result =
				Walker.ApplyMoveUnsafe(
					bs,
					new Move(
						new Coordinate(0, 1),
						new Coordinate(0, 0)));

			uint promoted =
				result.GetUnsafe(
					new Coordinate(0, 0));

			Assert.That(
				promoted,
				Is.EqualTo(
					(uint)GamePiece.Queen |
					(uint)GamePiece.Black));
		}


		[Test]
		[ChatGPTGenerated]
		public void ApplyMoveUnsafe_WhiteEnPassant_RemovesCapturedPawn()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			// White pawn E5
			bs = bs.DeleteUnsafe(12);

			bs = bs.WriteRawUnsafe(
				36,
				(uint)GamePiece.Pawn);

			// Black EP pawn D5
			bs = bs.DeleteUnsafe(51);

			bs = bs.WriteRawUnsafe(
				35,
				(uint)(GamePiece.Pawn |
				GamePiece.Black |
				GamePiece.EnPassantCapturable));

			BoardState result =
				Walker.ApplyMoveUnsafe(
					bs,
					new Move(
						new Coordinate(4, 4),
						new Coordinate(3, 5)));

			Assert.That(
				result.GetUnsafe(
					new Coordinate(3, 4)),
				Is.EqualTo(0ul));

			uint pawn =
				result.GetUnsafe(
					new Coordinate(3, 5));

			Assert.That(
				pawn & 7,
				Is.EqualTo(
					(uint)GamePiece.Pawn));
		}

		[Test]
		[ChatGPTGenerated]
		public void GetEffectiveCaptureTarget_KnightUnderpromotion_ReturnsBackRankSquare()
		{
			BoardState bs =
				new BoardState(
					BoardStateNoEnPassant.Initial());

			Coordinate target =
				Walker.GetEffectiveCaptureTarget(
					bs,
					new Move(
						new Coordinate(5, 6),
						new Coordinate(6, 6)),
					GamePiece.Pawn);

			Assert.That(
				target,
				Is.EqualTo(
					new Coordinate(6, 7)));
		}
	}


}