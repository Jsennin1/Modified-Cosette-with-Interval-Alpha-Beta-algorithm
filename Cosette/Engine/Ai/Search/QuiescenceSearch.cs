using System;
using Cosette.Engine.Ai.Ordering;
using Cosette.Engine.Ai.Score;
using Cosette.Engine.Ai.Transposition;
using Cosette.Engine.Common;
using Cosette.Engine.Moves;

namespace Cosette.Engine.Ai.Search
{

    public static class QuiescenceSearch
    {
        public static int playerPower = 0;
        public static int intervalX = 0, intervalY = 0;
        public static int FindBestMove(SearchContext context, int depth, int ply, int alpha, int beta)
        {
            context.Statistics.QNodes++;

            if (ply > context.Statistics.SelectiveDepth)
            {
                context.Statistics.SelectiveDepth = ply;
            }

            if (context.BoardState.Pieces[context.BoardState.ColorToMove][Piece.King] == 0)
            {
                context.Statistics.QLeafs++;
                return SearchConstants.NoKingValue;
            }

            if (context.BoardState.IsKingChecked(ColorOperations.Invert(context.BoardState.ColorToMove)))
            {
                context.Statistics.QLeafs++;
                return -SearchConstants.NoKingValue;
            }

            var standPat = 0;

            var evaluationEntry = EvaluationHashTable.Get(context.BoardState.Hash);
            if (evaluationEntry.IsKeyValid(context.BoardState.Hash))
            {
                standPat = evaluationEntry.Score;

#if DEBUG
                context.Statistics.EvaluationStatistics.EHTHits++;
#endif
            }
            else
            {
                standPat = Evaluation.Evaluate(context.BoardState, true, context.Statistics.EvaluationStatistics);
                EvaluationHashTable.Add(context.BoardState.Hash, (short)standPat);

#if DEBUG
                context.Statistics.EvaluationStatistics.EHTNonHits++;
                context.Statistics.EvaluationStatistics.EHTAddedEntries++;

                if (evaluationEntry.Key != 0 || evaluationEntry.Score != 0)
                {
                    context.Statistics.EvaluationStatistics.EHTReplacements++;
                }
#endif
            }    
            //EKLEME
            if (standPat >= beta /*&& standPat < intervalY*/)
            {
                context.Statistics.QLeafs++;
                return standPat;
            }
            //EKLEME
            //Console.WriteLine($"quies 69 alpha {alpha} , standPat {standPat}");

            if (standPat > alpha /*&& standPat < intervalY*/)
            {
                alpha = standPat;
            }

            Span<Move> moves = stackalloc Move[SearchConstants.MaxMovesCount];
            Span<short> moveValues = stackalloc short[SearchConstants.MaxMovesCount];

            var movesCount = context.BoardState.GetAvailableCaptureMoves(moves);
            MoveOrdering.AssignQValues(context.BoardState, moves, moveValues, movesCount);

            for (var moveIndex = 0; moveIndex < movesCount; moveIndex++)
            {
                MoveOrdering.SortNextBestMove(moves, moveValues, movesCount, moveIndex);

                if (moveValues[moveIndex] < 0)
                {
#if DEBUG
                    context.Statistics.QSEEPrunes++;
#endif
                    break;
                }

                if (standPat + moveValues[moveIndex] + SearchConstants.QFutilityPruningMargin < alpha)
                {
#if DEBUG
                    context.Statistics.QFutilityPrunes++;
#endif
                    break;
                }

                context.BoardState.MakeMove(moves[moveIndex]);
                var score = -FindBestMove(context, depth - 1, ply + 1, -beta, -alpha);  
                context.BoardState.UndoMove(moves[moveIndex]);
                //EKLEME
                Console.WriteLine($"quiess 106 score {score} , intervalY {intervalY}");
                if (score > intervalY)
                    continue;
                if (score > alpha )
                {
                    alpha = score;

                    if (alpha >= beta)
                    {
#if DEBUG
                        if (moveIndex == 0)
                        {
                            context.Statistics.QBetaCutoffsAtFirstMove++;
                        }
                        else
                        {
                            context.Statistics.QBetaCutoffsNotAtFirstMove++;
                        }
#endif

                        context.Statistics.QBetaCutoffs++;
                        break;
                    }
                }
            }

            return alpha;
        }
    }
}
