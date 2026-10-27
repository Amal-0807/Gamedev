using System;

namespace BlockBlaster
{
    // Engine-independent rules, shared by the game and the editor self-check.
    public static class GameRules
    {
        public const int MaxLives = 3;
        public const int CubePoints = 25;
        public const int MissPenalty = 10;
        public const int MaxBlocks = 20;
        public const int Columns = 12;
        public const float ArenaWidth = 0.60f;
        public const float ArenaLength = 0.80f;
        public const float CellSize = 0.045f;
        public const float CubeSize = 0.040f;
        public const float PlayHeight = 0.030f;
        public const float ShipZ = 0.055f;
        public const float WaveSeconds = 30f;
        public const float FireInterval = 0.25f;
        public const float InvincibleSeconds = 1.5f;

        public enum Shape { I, O, T, S, Z, J, L }
        public enum Power { Shield, ExtraLife, TripleShot, SlowMo }

        public readonly struct Cell
        {
            public readonly int X;
            public readonly int Y;
            public Cell(int x, int y) { X = x; Y = y; }
        }

        static readonly Cell[][] Shapes =
        {
            new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(3, 0) },
            new[] { new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(1, 1) },
            new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(1, 1) },
            new[] { new Cell(0, 0), new Cell(1, 0), new Cell(1, 1), new Cell(2, 1) },
            new[] { new Cell(0, 1), new Cell(1, 1), new Cell(1, 0), new Cell(2, 0) },
            new[] { new Cell(0, 1), new Cell(0, 0), new Cell(1, 0), new Cell(2, 0) },
            new[] { new Cell(2, 1), new Cell(0, 0), new Cell(1, 0), new Cell(2, 0) }
        };

        public static Cell[] Layout(Shape shape, int quarterTurns)
        {
            var cells = (Cell[])Shapes[(int)shape].Clone();
            int turns = ((quarterTurns % 4) + 4) % 4;
            int minX = int.MaxValue, minY = int.MaxValue;
            for (int i = 0; i < cells.Length; i++)
            {
                int x = cells[i].X, y = cells[i].Y;
                for (int r = 0; r < turns; r++) { int oldX = x; x = -y; y = oldX; }
                cells[i] = new Cell(x, y);
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
            }
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new Cell(cells[i].X - minX, cells[i].Y - minY);
            return cells;
        }

        public static int Width(Cell[] cells)
        {
            int max = 0;
            foreach (var cell in cells) max = Math.Max(max, cell.X);
            return max + 1;
        }

        public static int Wave(float elapsed) => 1 + (int)(Math.Max(0, elapsed) / WaveSeconds);
        // ponytail: difficulty caps at wave 16 to keep this student version playable.
        public static float FallSpeed(int wave) => 0.060f * (float)Math.Pow(1.10, Math.Min(15, Math.Max(0, wave - 1)));
        public static float SpawnInterval(int wave) => 1.65f / (float)Math.Pow(1.15, Math.Min(15, Math.Max(0, wave - 1)));
        public static int Miss(int score) => Math.Max(0, score - MissPenalty);
        public static int AddLife(int lives) => Math.Min(MaxLives, lives + 1);
    }
}
