using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cosette.Engine.Ai.Ordering;
using Cosette.Engine.Ai.Score;
using Cosette.Engine.Ai.Transposition;
using Cosette.Engine.Board;
using Cosette.Engine.Common;
using Cosette.Engine.Moves;

namespace Cosette.Engine.Ai.Search
{
    public static class IterativeDeepening
    {
        public static event EventHandler<SearchStatistics> OnSearchUpdate;
        public static void SetPlayerPowerAndIntervals(int playerEvaluation)
        {

            NegaMax.playerPower = playerEvaluation;
            QuiescenceSearch.playerPower = playerEvaluation;
            int interval= playerEvaluation + Math.Abs(playerEvaluation);
            if (playerEvaluation < 0)
            {

                interval = playerEvaluation + Math.Abs(playerEvaluation) / 2;

            }
            else if (playerEvaluation == 0)
                interval = 100;
            NegaMax.intervalY = interval;
            QuiescenceSearch.intervalY = interval;

        }
        public static void DoubleTheInterval()
        {
            NegaMax.intervalY += Math.Abs(NegaMax.intervalY);
            QuiescenceSearch.intervalY += Math.Abs(QuiescenceSearch.intervalY);

        }
        public static Move FindBestMove(SearchContext context)
        {
            HistoryHeuristic.AgeValues();
            KillerHeuristic.AgeKillers();

            var expectedExecutionTime = 0;
            var alpha = SearchConstants.MinValue;
            var beta = SearchConstants.MaxValue;
            var lastSearchTime = 0ul;
            var bestMove = Move.Empty;
            var stopwatch = Stopwatch.StartNew();

            context.Statistics = new SearchStatistics();
            var evaluationStatistics = new EvaluationStatistics();
            var playerEvaluation = -Evaluation.Evaluate(context.BoardState, false, evaluationStatistics);
            Console.WriteLine("PLAYER EVALUATION " + playerEvaluation);

            for (var depth = 1; ShouldContinueDeepening(context, depth, expectedExecutionTime); depth++)
            {
                if (depth > 3 && context.Statistics.PrincipalVariationMovesCount == 0) {
                    DoubleTheInterval();
                    Console.WriteLine("CALISTI");
                }
                context.Statistics.Board = context.BoardState;
                context.Statistics.Depth = depth;
                context.Statistics.Score = NegaMax.FindBestMove(context, depth, 0, alpha, beta);
                context.Statistics.SearchTime = (ulong)stopwatch.ElapsedMilliseconds;

                if (context.AbortSearch)
                {
                    break;
                }
                
                context.Statistics.PrincipalVariationMovesCount = GetPrincipalVariation(context.BoardState, context.Statistics.PrincipalVariation, 0);
                bestMove = context.Statistics.PrincipalVariation[0];

                OnSearchUpdate?.Invoke(null, context.Statistics);

                if (lastSearchTime != 0)
                {
                    var ratio = (float)context.Statistics.SearchTime / lastSearchTime;
                    expectedExecutionTime = (int)(context.Statistics.SearchTime * ratio);
                }

                lastSearchTime = context.Statistics.SearchTime;
            }

            while (context.WaitForStopCommand)
            {
                Thread.Sleep(1);
            }

            context.AbortSearch = false;
            context.HelperTasksCancellationTokenSource?.Cancel();
            context.BoardState.MakeMove(bestMove);
            var AIEvolation = -Evaluation.Evaluate(context.BoardState, false, evaluationStatistics);
            Console.WriteLine("AI EVALUATION " + AIEvolation);
            context.BoardState.UndoMove(bestMove);

            return bestMove;
        }

        public static bool ShouldContinueDeepening(SearchContext context, int depth, int expectedExecutionTime)
        {
            if (IsScoreNearCheckmate(context.Statistics.Score))
            {
                if (depth - 1 >= GetMovesToCheckmate(context.Statistics.Score) * 2)
                {
                    return false;
                }
            }

            return depth < context.MaxDepth && expectedExecutionTime <= context.MaxTime;
        }

        public static bool IsScoreNearCheckmate(int score)
        {
            var scoreAbs = Math.Abs(score);
            return scoreAbs >= EvaluationConstants.Checkmate - SearchConstants.MaxDepth &&
                   scoreAbs <= EvaluationConstants.Checkmate + SearchConstants.MaxDepth;
        }

        public static int GetMovesToCheckmate(int score)
        {
            return (int)Math.Ceiling((float)Math.Abs(Math.Abs(score) - EvaluationConstants.Checkmate) / 2);
        }

        private static int GetPrincipalVariation(BoardState board, Move[] moves, int movesCount)
        {
            var entry = TranspositionTable.Get(board.Hash);
            if (entry.Flags == TranspositionTableEntryFlags.ExactScore && entry.IsKeyValid(board.Hash) && movesCount < SearchConstants.MaxDepth)
            {
                if (!board.IsMoveLegal(entry.BestMove))
                {
                    return movesCount;
                }

                moves[movesCount] = entry.BestMove;
                board.MakeMove(entry.BestMove);

                var enemyColor = ColorOperations.Invert(board.ColorToMove);
                var king = board.Pieces[enemyColor][Piece.King];
                var kingField = BitOperations.BitScan(king);

                if (board.IsFieldAttacked(enemyColor, (byte)kingField))
                {
                    board.UndoMove(entry.BestMove);
                    return movesCount;
                }

                movesCount = GetPrincipalVariation(board, moves, movesCount + 1);
                board.UndoMove(entry.BestMove);
            }

            return movesCount;
        }
    }
}
