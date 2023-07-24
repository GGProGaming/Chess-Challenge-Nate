using System;
using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{

    // Transposition table
    private Dictionary<ulong, Entry> transpositionTable;

    // Evaluation weights
    const int MATERIAL_WEIGHT = 100;
    const int MOBILITY_WEIGHT = 75;
    const int KING_SAFETY_WEIGHT = 50;

    // Search limits
    const int MAX_DEPTH = 64;
    int currentDepthLimit = 1;

    public Move Think(Board board, Timer timer)
    {
        // Initialize transposition table
        transpositionTable = new Dictionary<ulong, Entry>(10000);

        currentDepthLimit = 1; // Start with 1 ply search

        // Iterative deepening until time runs out
        while (timer.MillisecondsRemaining > 10)
        {
            // Get best move for current depth limit
            Move bestMove = AlphaBetaRoot(board, currentDepthLimit);

            // Make best move on board and update transposition table
            board.MakeMove(bestMove);
            transpositionTable.Clear();
            board.UndoMove(bestMove);

            // Increase depth for next iteration
            currentDepthLimit++;
        }

        // Return best move found
        return Move.bestMove;
    }

    private Move AlphaBetaRoot(Board board, int depth)
    {
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        int score = int.MinValue;
        Move bestMove = Move.NullMove;

        // Order moves to check captures first
        Move[] moves = board.GetLegalMoves();
        MoveOrdering.Sort(board, moves);

        // Search position
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int moveScore = -AlphaBeta(board, depth - 1, -beta, -alpha, true);
            board.UndoMove(move);

            // Update best move
            if (moveScore > score)
            {
                score = moveScore;
                bestMove = move;
            }

            // Pruning
            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
                break;
        }

        return bestMove;
    }

    private int AlphaBeta(Board board, int depth, int alpha, int beta, bool nullMovePruning)
    {
        // Check transposition table
        ulong boardHash = board.ZobristKey;
        if (transpositionTable.ContainsKey(boardHash))
        {
            Entry entry1 = transpositionTable[boardHash]; // Rename to avoid conflict with outer scope

            if (entry1.Depth >= depth)
            {
                if (entry1.Flag == EntryFlag.Exact)
                    return entry1.Score;

                if (entry1.Flag == EntryFlag.Lowerbound)
                {
                    alpha = Math.Max(alpha, entry1.Score);
                }
                else if (entry1.Flag == EntryFlag.Upperbound)
                {
                    beta = Math.Min(beta, entry1.Score);
                }

                if (alpha >= beta)
                    return entry1.Score;
            }
        }


        // Base cases
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            return Quiescence(board, alpha, beta);
        }

        // Null move pruning
        if (nullMovePruning && !board.IsInCheck() && depth >= 2)
        {
            board.TrySkipTurn();
            int score1 = -AlphaBeta(board, depth - 1 - nullMoveReduction(depth), -beta, -beta + 1, false); // Rename to avoid conflict with outer scope
            board.UndoSkipTurn();

            if (score1 >= beta)
                return beta;
        }


        // Recursively search all moves
        Move[] moves = board.GetLegalMoves();
        MoveOrdering.Sort(board, moves);

        int score = int.MinValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int moveScore = -AlphaBeta(board, depth - 1, -beta, -alpha, true);
            board.UndoMove(move);

            score = Math.Max(score, moveScore);
            alpha = Math.Max(alpha, score);

            // Pruning
            if (alpha >= beta)
                break;
        }

        // Update transposition table 
        Entry entry = new Entry(depth, score, score <= alpha ? EntryFlag.Upperbound : score >= beta ? EntryFlag.Lowerbound : EntryFlag.Exact);
        transpositionTable[boardHash] = entry;

        return score;
    }

    // Quiescence search 
    private int Quiescence(Board board, int alpha, int beta)
    {
        // TODO: Implement quiescence search

        return EvaluateBoard(board);
    }

    // Evaluation
    public int EvaluateBoard(Board board)
    {
        int score = 0;

        // Material score
        score += Material(board);

        // Piece square tables
        score += PieceSquareTables(board);

        // Mobility
        score += Mobility(board) * MOBILITY_WEIGHT;

        // King safety
        score += KingSafety(board) * KING_SAFETY_WEIGHT;

        return score;
    }

    // Get value for piece type
    private int GetPieceValue(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return 100;
            case PieceType.Knight:
                return 320;
            case PieceType.Bishop:
                return 330;
            case PieceType.Rook:
                return 500;
            case PieceType.Queen:
                return 900;
            default:
                return 0;
        }
    }

    // Material score
    private int Material(Board board)
    {
        int score = 0;

        score += GetPieceValue(PieceType.Pawn) * (board.GetPieceList(PieceType.Pawn, true).Count - board.GetPieceList(PieceType.Pawn, false).Count);
        score += GetPieceValue(PieceType.Knight) * (board.GetPieceList(PieceType.Knight, true).Count - board.GetPieceList(PieceType.Knight, false).Count);
        score += GetPieceValue(PieceType.Bishop) * (board.GetPieceList(PieceType.Bishop, true).Count - board.GetPieceList(PieceType.Bishop, false).Count);
        score += GetPieceValue(PieceType.Rook) * (board.GetPieceList(PieceType.Rook, true).Count - board.GetPieceList(PieceType.Rook, false).Count);
        score += GetPieceValue(PieceType.Queen) * (board.GetPieceList(PieceType.Queen, true).Count - board.GetPieceList(PieceType.Queen, false).Count);

        return score * MATERIAL_WEIGHT;
    }

    // Piece square tables  
    private int[,] pawnTable = {
  {0,  0,  0,  0,  0,  0,  0,  0},
  {50, 50, 50, 50, 50, 50, 50, 50},
  {10, 10, 20, 30, 30, 20, 10, 10},
  {5,  5, 10, 25, 25, 10,  5,  5},
  {0,  0,  0, 20, 20,  0,  0,  0},
  {5, -5,-10,  0,  0,-10, -5,  5},
  {5, 10, 10,-20,-20, 10, 10,  5},
  {0,  0,  0,  0,  0,  0,  0,  0}
};

    private int PieceSquareTables(Board board)
    {
        int score = 0;

        // Score white pawns
        foreach (Piece p in board.GetPieceList(PieceType.Pawn, true))
        {
            score += pawnTable[p.Square.Rank, p.Square.File];
        }

        // Score black pawns
        foreach (Piece p in board.GetPieceList(PieceType.Pawn, false))
        {
            score -= pawnTable[7 - p.Square.Rank, 7 - p.Square.File];
        }

        return score;
    }

    // Mobility score
    private int Mobility(Board board)
    {
        int score = 0;

        // Get attack bitboards for each side
        ulong whiteAttacks = board.GetPieceBitboard(PieceType.Pawn, true) |
                             board.GetPieceBitboard(PieceType.Knight, true) |
                             board.GetPieceBitboard(PieceType.Bishop, true) |
                             board.GetPieceBitboard(PieceType.Rook, true) |
                             board.GetPieceBitboard(PieceType.Queen, true);

        ulong blackAttacks = board.GetPieceBitboard(PieceType.Pawn, false) |
                             board.GetPieceBitboard(PieceType.Knight, false) |
                             board.GetPieceBitboard(PieceType.Bishop, false) |
                             board.GetPieceBitboard(PieceType.Rook, false) |
                             board.GetPieceBitboard(PieceType.Queen, false);

        // Count number of attacked squares                   
        int whiteMobility = BitboardHelper.GetNumberOfSetBits(whiteAttacks);
        int blackMobility = BitboardHelper.GetNumberOfSetBits(blackAttacks);

        // Reward having more mobility
        score += (whiteMobility - blackMobility) * 5;

        return score;
    }

    // King safety score
    private int KingSafety(Board board)
    {
        Square whiteKingSq = board.GetKingSquare(true);
        Square blackKingSq = board.GetKingSquare(false);

        int score = 0;

        // Penalize undefended king squares
        if (!board.SquareIsAttackedByOpponent(whiteKingSq))
            score -= 50;

        if (!board.SquareIsAttackedByOpponent(blackKingSq))
            score += 50;

        return score;
    }

    private class Entry
    {
        public readonly int Depth;
        public readonly int Score;
        public readonly EntryFlag Flag;

        public Entry(int depth, int score, EntryFlag flag)
        {
            Depth = depth;
            Score = score;
            Flag = flag;
        }
    }

    private enum EntryFlag
    {
        Upperbound,
        Lowerbound,
        Exact
    }

    private static class MoveOrdering
    {
        public static void Sort(Board board, Move[] moves)
        {
            // Sort moves so checking moves come first
            Array.Sort(moves, (m1, m2) =>
            {
                bool b1 = board.SquareIsAttackedByOpponent(board.GetKingSquare(!board.IsWhiteToMove));
                bool b2 = board.MakeMove(m1);
                bool a1 = board.SquareIsAttackedByOpponent(board.GetKingSquare(!board.IsWhiteToMove));
                board.UndoMove(m1);

                bool c1 = board.MakeMove(m2);
                bool a2 = board.SquareIsAttackedByOpponent(board.GetKingSquare(!board.IsWhiteToMove));
                board.UndoMove(m2);

                if (a1) return -1;
                if (a2) return 1;
                if (b1 && !c1) return -1;
                if (b1 && !b2) return 1;
                return 0;
            });
        }
    }

    private int nullMoveReduction(int depth)
    {
        return (depth >= 3 ? 2 : 1);
    }

}