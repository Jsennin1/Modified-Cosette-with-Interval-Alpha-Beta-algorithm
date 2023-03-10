using Cosette.Engine.Board;
using Cosette.Engine.Perft;

namespace Cosette.Interactive.Commands
{
    public class AdvancedPerftCommand : ICommand
    {
        public string Description { get; }
        private readonly InteractiveConsole _interactiveConsole;

        public AdvancedPerftCommand(InteractiveConsole interactiveConsole)
        {
            _interactiveConsole = interactiveConsole;
            Description = "Test performance of the moves generator with advanced statistics";
        }

        public void Run(params string[] parameters)
        {
            if (parameters.Length < 1 || !int.TryParse(parameters[0], out var depth))
            {
                _interactiveConsole.WriteLine("No depth specified");
                return;
            }

            var boardState = new BoardState(true);
            boardState.SetDefaultState();

            for (var i = 0; i <= depth; i++)
            {
                var result = AdvancedPerft.Run(boardState, i);

                _interactiveConsole.WriteLine($"Depth {i}: {result.Leafs} leafs ({result.Time:F} s), Captures: {result.Captures}, " +
                                         $"Checkmates: {result.Checkmates}, Castlings: {result.Castles}, " +
                                         $"En passants: {result.EnPassants}, Checks: {result.Checks}");
            }
        }
    }
}
