using System.Collections.Generic;
using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    private const int SearchDepth = 4;

    public Move Think(Board board, Timer timer)
    {
        if (board.IsInCheckmate()) return Move.NullMove;

        Move[] legalMoves = board.GetLegalMoves();
        if (legalMoves.Length == 0) return Move.NullMove;

        int bestMoveIndex = 0;
        int bestMoveValue = int.MinValue;

        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            int moveValue = -AlphaBeta(board, SearchDepth - 1, int.MinValue, int.MaxValue, false);
            board.UndoMove(legalMoves[i]);

            if (moveValue > bestMoveValue)
            {
                bestMoveIndex = i;
                bestMoveValue = moveValue;
            }
        }

        return legalMoves[bestMoveIndex];
    }

    private int AlphaBeta(Board board, int depth, int alpha, int beta, bool doNull)
    {
        if (board.IsDraw() || board.IsInCheckmate())
        {
            return EvaluateBoard(board);
        }

        if (depth == 0)
        {
            return QuiescenceSearch(board, alpha, beta);
        }

        Move[] legalMoves = board.GetLegalMoves();
        int bestValue = int.MinValue;

        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            int moveValue = -AlphaBeta(board, depth - 1, -beta, -alpha, true);
            board.UndoMove(legalMoves[i]);

            bestValue = Math.Max(bestValue, moveValue);
            alpha = Math.Max(alpha, moveValue);

            if (alpha >= beta)
            {
                break;
            }
        }

        return bestValue;
    }

    private int QuiescenceSearch(Board board, int alpha, int beta)
    {
        int standPat = EvaluateBoard(board);

        if (standPat >= beta)
        {
            return beta;
        }

        if (alpha < standPat)
        {
            alpha = standPat;
        }

        Move[] legalMoves = board.GetLegalMoves(true);

        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            int score = -QuiescenceSearch(board, -beta, -alpha);
            board.UndoMove(legalMoves[i]);

            if (score >= beta)
            {
                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
            }
        }

        return alpha;
    }

    private int Minimax(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            return EvaluateBoard(board);
        }

        Move[] legalMoves = board.GetLegalMoves();
        int bestValue = int.MinValue;

        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            int moveValue = -Minimax(board, depth - 1, -beta, -alpha);
            board.UndoMove(legalMoves[i]);

            bestValue = Math.Max(bestValue, moveValue);
            alpha = Math.Max(alpha, moveValue);

            if (alpha >= beta)
            {
                break;
            }
        }

        return bestValue;
    }
    // Piece Values
    private static readonly Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>
    {
        { PieceType.Pawn, 100 },
        { PieceType.Knight, 320 },
        { PieceType.Bishop, 330 },
        { PieceType.Rook, 500 },
        { PieceType.Queen, 900 },
        { PieceType.King, 20000 } // Arbitrarily high value as losing the king means losing the game.
    };

    public int EvaluateBoard(Board board)
    {
        int score = 0;

        // Evaluate material balance
        foreach (var pieceType in pieceValues.Keys)
        {
            var whitePieceList = board.GetPieceList(pieceType, true);
            var blackPieceList = board.GetPieceList(pieceType, false);

            // Increment score for each white piece
            for (int i = 0; i < whitePieceList.Count; i++)
                score += pieceValues[pieceType];

            // Decrement score for each black piece
            for (int i = 0; i < blackPieceList.Count; i++)
                score -= pieceValues[pieceType];
        }

        return score;
    }
}