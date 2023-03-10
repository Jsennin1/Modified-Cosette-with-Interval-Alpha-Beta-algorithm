namespace Cosette.Engine.Board
{
    public static class BoardConstants
    {
        public const ulong Full = ulong.MaxValue;
        public const ulong Empty = 0;

        public const ulong AFile = 0x8080808080808080;
        public const ulong BFile = 0x4040404040404040;
        public const ulong CFile = 0x2020202020202020;
        public const ulong DFile = 0x1010101010101010;
        public const ulong EFile = 0x0808080808080808;
        public const ulong FFile = 0x0404040404040404;
        public const ulong GFile = 0x0202020202020202;
        public const ulong HFile = 0x0101010101010101;

        public const ulong ARank = 0x00000000000000FF;
        public const ulong BRank = 0x000000000000FF00;
        public const ulong CRank = 0x0000000000FF0000;
        public const ulong DRank = 0x00000000FF000000;
        public const ulong ERank = 0x000000FF00000000;
        public const ulong FRank = 0x0000FF0000000000;
        public const ulong GRank = 0x00FF000000000000;
        public const ulong HRank = 0xFF00000000000000;

        public const ulong Edges = AFile | HFile | ARank | HRank;
        public const ulong BoardWithoutEdges = Full & ~Edges;
        public const ulong RightLeftEdge = AFile | HFile;
        public const ulong TopBottomEdge = ARank | HRank;
        public const ulong Corners = 0x8100000000000081;

        public const ulong NearPromotionAreaWhite = 0xffff0000000000;
        public const ulong NearPromotionAreaBlack = 0xffff00;

        public static int[] DistanceFromCenter =
        {
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 2, 2, 2, 2, 2, 2, 3,
            3, 2, 1, 1, 1, 1, 2, 3,
            3, 2, 1, 0, 0, 1, 2, 3,
            3, 2, 1, 0, 0, 1, 2, 3,
            3, 2, 1, 1, 1, 1, 2, 3,
            3, 2, 2, 2, 2, 2, 2, 3,
            3, 3, 3, 3, 3, 3, 3, 3
        };

        public const int PhaseResolution = 1024;
    }
}
