using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cosette.Engine.Ai.Ordering;
using Cosette.Engine.Ai.Score;
using Cosette.Engine.Ai.Score.Evaluators;
using Cosette.Engine.Ai.Search;
using Cosette.Engine.Ai.Transposition;
using Cosette.Engine.Board;
using Cosette.Engine.Common;
using Cosette.Engine.Moves;
using Cosette.Interactive;
using Cosette.Logs;
using Cosette.Uci.Commands;

namespace Cosette.Uci
{
    public class UciClient
    {
        public BoardState BoardState;
        public SearchContext SearchContext;

        private bool _debugMode;

        private readonly InteractiveConsole _interactiveConsole;
        private readonly Dictionary<string, IUciCommand> _commands;

        public UciClient(InteractiveConsole interactiveConsole)
        {
            BoardState = new BoardState(true);
            BoardState.SetDefaultState();

            _interactiveConsole = interactiveConsole;

#if UCI_DEBUG_OUTPUT
            _debugMode = true;
#endif

            _commands = new Dictionary<string, IUciCommand>();
            _commands["quit"] = new QuitCommand(this);
            _commands["setoption"] = new SetOptionCommand(this);
            _commands["isready"] = new IsReadyCommand(this);
            _commands["ucinewgame"] = new UciNewGameCommand(this);
            _commands["position"] = new PositionCommand(this);
            _commands["debug"] = new DebugCommand(this);
            _commands["go"] = new GoCommand(this);
            _commands["stop"] = new StopCommand(this);

            IterativeDeepening.OnSearchUpdate += OnSearchUpdate;
        }

        public void Run()
        {
            SendName();
            SendAuthor();
            SendOptions();
            RunCommandsLoop();
        }

        public void Send(string command)
        {
            _interactiveConsole.WriteLine(command);
            LogManager.LogInfo("[SEND] " + command);
        }

        public void SendError(string errorMessage)
        {
            Send($"error {errorMessage}");
        }

        public (string Command, string[] parameters) Receive()
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (input == null)
                {
                    Environment.Exit(0);
                }

                var splitInput = input.Split(' ');
                var command = splitInput[0].ToLower();
                var parameters = splitInput.Skip(1).ToArray();

                LogManager.LogInfo("[RECV] " + input);
                return (command, parameters);
            }
        }

        public void SetDebugMode(bool state)
        {
            _debugMode = state;
        }

        private void SendName()
        {
            Send("id name Cosette v5.1");
        }

        private void SendAuthor()
        {
            Send("id author Paweł Osikowski");
        }

        private void SendOptions()
        {
            Send($"option name Hash type spin default {HashTableConstants.DefaultHashTablesSize} min 3 max 65535");

            Send($"option name PawnValue type spin default {EvaluationConstants.Pieces[Piece.Pawn]} min 0 max 65535");
            Send($"option name KnightValue type spin default {EvaluationConstants.Pieces[Piece.Knight]} min 0 max 65535");
            Send($"option name BishopValue type spin default {EvaluationConstants.Pieces[Piece.Bishop]} min 0 max 65535");
            Send($"option name RookValue type spin default {EvaluationConstants.Pieces[Piece.Rook]} min 0 max 65535");
            Send($"option name QueenValue type spin default {EvaluationConstants.Pieces[Piece.Queen]} min 0 max 65535");
            Send($"option name KingValue type spin default {EvaluationConstants.Pieces[Piece.King]} min 0 max 65535");

            Send($"option name DoubledPawnsOpening type spin default {EvaluationConstants.DoubledPawns[GamePhase.Opening]} min -100 max 100");
            Send($"option name DoubledPawnsEnding type spin default {EvaluationConstants.DoubledPawns[GamePhase.Ending]} min -100 max 100");
            Send($"option name IsolatedPawnsOpening type spin default {EvaluationConstants.IsolatedPawns[GamePhase.Opening]} min -100 max 100");
            Send($"option name IsolatedPawnsEnding type spin default {EvaluationConstants.IsolatedPawns[GamePhase.Ending]} min -100 max 100");
            Send($"option name ChainedPawnsOpening type spin default {EvaluationConstants.ChainedPawns[GamePhase.Opening]} min -100 max 100");
            Send($"option name ChainedPawnsEnding type spin default {EvaluationConstants.ChainedPawns[GamePhase.Ending]} min -100 max 100");
            Send($"option name PassingPawnsOpening type spin default {EvaluationConstants.PassingPawns[GamePhase.Opening]} min -100 max 100");
            Send($"option name PassingPawnsEnding type spin default {EvaluationConstants.PassingPawns[GamePhase.Ending]} min -100 max 100");

            Send($"option name CenterMobilityModifier type spin default {EvaluationConstants.CenterMobilityModifier} min -100 max 100");
            Send($"option name OutsideMobilityModifier type spin default {EvaluationConstants.OutsideMobilityModifier} min -100 max 100");
            Send($"option name KingInDanger type spin default {EvaluationConstants.KingInDanger} min -100 max 100");
            Send($"option name PawnShield type spin default {EvaluationConstants.PawnShield} min -100 max 100");
            Send($"option name DoubledRooks type spin default {EvaluationConstants.DoubledRooks} min -100 max 100");
            Send($"option name RookOnOpenFile type spin default {EvaluationConstants.RookOnOpenFile} min -100 max 100");
            Send($"option name PairOfBishops type spin default {EvaluationConstants.PairOfBishops} min -100 max 100");
            Send($"option name Fianchetto type spin default {EvaluationConstants.Fianchetto} min -100 max 100");
            Send($"option name FianchettoWithoutBishop type spin default {EvaluationConstants.FianchettoWithoutBishop} min -100 max 100");
            Send($"option name OpenFileNextToKing type spin default {EvaluationConstants.OpenFileNextToKing} min -100 max 0");

            Send($"option name HashMove type spin default {MoveOrderingConstants.HashMove} min -10000 max 10000");
            Send($"option name Promotion type spin default {MoveOrderingConstants.Promotion} min -10000 max 10000");
            Send($"option name Castling type spin default {MoveOrderingConstants.Castling} min -10000 max 10000");
            Send($"option name PawnNearPromotion type spin default {MoveOrderingConstants.PawnNearPromotion} min -10000 max 10000");
            Send($"option name Capture type spin default {MoveOrderingConstants.Capture} min -10000 max 10000");
            Send($"option name EnPassant type spin default {MoveOrderingConstants.EnPassant} min -10000 max 10000");
            Send($"option name KillerMove type spin default {MoveOrderingConstants.KillerMove} min -10000 max 10000");
            Send($"option name HistoryHeuristicMaxScore type spin default {MoveOrderingConstants.HistoryHeuristicMaxScore} min -10000 max 10000");
            Send($"option name KillerSlots type spin default {MoveOrderingConstants.KillerSlots} min -10000 max 10000");

            Send($"option name IIDMinDepth type spin default {SearchConstants.IIDMinDepth} min 0 max 32");
            Send($"option name IIDDepthReduction type spin default {SearchConstants.IIDDepthReduction} min 0 max 32");

            Send($"option name RazoringMinDepth type spin default {SearchConstants.RazoringMinDepth} min 0 max 32");
            Send($"option name RazoringMaxDepth type spin default {SearchConstants.RazoringMaxDepth} min 0 max 32");
            Send($"option name RazoringMargin type spin default {SearchConstants.RazoringMargin} min 0 max 1000");
            Send($"option name RazoringMarginMultiplier type spin default {SearchConstants.RazoringMarginMultiplier} min 0 max 1000");

            Send($"option name StaticNullMoveMaxDepth type spin default {SearchConstants.StaticNullMoveMaxDepth} min 0 max 32");
            Send($"option name StaticNullMoveMaxDepthDivider type spin default {SearchConstants.StaticNullMoveMaxDepthDivider} min 0 max 32");
            Send($"option name StaticNullMoveMargin type spin default {SearchConstants.StaticNullMoveMargin} min 0 max 1000");
            Send($"option name StaticNullMoveMarginMultiplier type spin default {SearchConstants.StaticNullMoveMarginMultiplier} min 0 max 1000");

            Send($"option name NullMoveMinDepth type spin default {SearchConstants.NullMoveMinDepth} min 0 max 32");
            Send($"option name NullMoveDepthReduction type spin default {SearchConstants.NullMoveDepthReduction} min 0 max 32");
            Send($"option name NullMoveDepthReductionDivider type spin default {SearchConstants.NullMoveDepthReductionDivider} min 0 max 32");

            Send($"option name FutilityPruningMaxDepth type spin default {SearchConstants.FutilityPruningMaxDepth} min 0 max 32");
            Send($"option name FutilityPruningMaxDepthDivisor type spin default {SearchConstants.FutilityPruningMaxDepthDivisor} min 0 max 32");
            Send($"option name FutilityPruningMargin type spin default {SearchConstants.FutilityPruningMargin} min 0 max 1000");
            Send($"option name FutilityPruningMarginMultiplier type spin default {SearchConstants.FutilityPruningMarginMultiplier} min 0 max 1000");
            Send($"option name QFutilityPruningMargin type spin default {SearchConstants.QFutilityPruningMargin} min 0 max 1000");

            Send($"option name LMRMinDepth type spin default {SearchConstants.LMRMinDepth} min 0 max 32");
            Send($"option name LMRMovesWithoutReduction type spin default {SearchConstants.LMRMovesWithoutReduction} min 0 max 32");
            Send($"option name LMRBaseReduction type spin default {SearchConstants.LMRBaseReduction} min 0 max 32");
            Send($"option name LMRMoveIndexDivider type spin default {SearchConstants.LMRMoveIndexDivider} min 0 max 32");
            Send($"option name LMRPvNodeMaxReduction type spin default {SearchConstants.LMRPvNodeMaxReduction} min 0 max 32");
            Send($"option name LMRNonPvNodeMaxReduction type spin default {SearchConstants.LMRNonPvNodeMaxReduction} min 0 max 32");
            Send($"option name LMRMaxHistoryValueDivider type spin default {SearchConstants.LMRMaxHistoryValueDivider} min 0 max 32");

            Send($"option name LMPMaxDepth type spin default {SearchConstants.LMPMaxDepth} min 0 max 32");
            Send($"option name LMPBasePercentMovesToPrune type spin default {SearchConstants.LMPBasePercentMovesToPrune} min 0 max 100");
            Send($"option name LMPPercentIncreasePerDepth type spin default {SearchConstants.LMPPercentIncreasePerDepth} min 0 max 100");

            Send("uciok");
        }

        private void RunCommandsLoop()
        {
            while (true)
            {
                var (command, parameters) = Receive();
                if (command == "leave")
                {
                    break;
                }

                if (_commands.ContainsKey(command))
                {
                    _commands[command].Run(parameters);
                }
            }
        }

        private void OnSearchUpdate(object sender, SearchStatistics stats)
        {
            var score = FormatScore(stats.Score);
            var principalVariation = FormatPrincipalVariation(stats.PrincipalVariation, stats.PrincipalVariationMovesCount);

            Send($"info depth {stats.Depth} seldepth {stats.SelectiveDepth} time {stats.SearchTime} " +
                 $"score {score} nodes {stats.TotalNodes} nps {stats.TotalNodesPerSecond} pv {principalVariation}");

            if (_debugMode && stats.PrincipalVariationMovesCount > 0 && stats.PrincipalVariation[0] != Move.Empty)
            {
                var sign = stats.Board.ColorToMove == Color.White ? 1 : -1;
                stats.Board.MakeMove(stats.PrincipalVariation[0]);

                var evaluationStatistics = new EvaluationStatistics();
                var openingPhase = stats.Board.GetPhaseRatio();
                var endingPhase = BoardConstants.PhaseResolution - openingPhase;

                var fieldsAttackedByWhite = 0ul;
                var fieldsAttackedByBlack = 0ul;

                var materialEvaluation = sign * MaterialEvaluator.Evaluate(stats.Board);
                var positionEvaluation = sign * PositionEvaluator.Evaluate(stats.Board, openingPhase, endingPhase);
                var pawnStructureEvaluation = sign * PawnStructureEvaluator.Evaluate(stats.Board, evaluationStatistics, openingPhase, endingPhase);
                var mobility = sign * MobilityEvaluator.Evaluate(stats.Board, openingPhase, endingPhase, ref fieldsAttackedByWhite, ref fieldsAttackedByBlack);
                var kingSafety = sign * KingSafetyEvaluator.Evaluate(stats.Board, openingPhase, endingPhase, fieldsAttackedByWhite, fieldsAttackedByBlack);
                var rooks = sign * RookEvaluator.Evaluate(stats.Board, openingPhase, endingPhase);
                var bishops = sign * BishopEvaluator.Evaluate(stats.Board, openingPhase, endingPhase);

                var total = materialEvaluation + positionEvaluation + pawnStructureEvaluation + 
                            mobility + kingSafety;

                Send($"info string evaluation {total} phase {openingPhase} material {materialEvaluation} " +
                     $"position {positionEvaluation} pawns {pawnStructureEvaluation} mobility {mobility} ksafety {kingSafety} " +
                     $"rooks {rooks} bishops {bishops} irrmoves {stats.Board.IrreversibleMovesCount}");

                stats.Board.UndoMove(stats.PrincipalVariation[0]);
            }
        }

        private string FormatScore(int score)
        {
            if (IterativeDeepening.IsScoreNearCheckmate(score))
            {
                var movesToCheckmate = IterativeDeepening.GetMovesToCheckmate(score);
                if (score < 0)
                {
                    movesToCheckmate = -movesToCheckmate;
                }

                return "mate " + movesToCheckmate;
            }

            return "cp " + score;
        }

        private string FormatPrincipalVariation(Move[] moves, int movesCount)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < movesCount; i++)
            {
                stringBuilder.Append(moves[i]);
                stringBuilder.Append(' ');
            }

            return stringBuilder.ToString().Trim();
        }
    }
}
