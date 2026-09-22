using System.Collections.Generic;

namespace Ovomium.Features.ChestFill
{
    /// <summary>État d'une case de la grille vu par le regroupement.</summary>
    internal enum CellState { Free, Same, Other, Excluded }

    /// <summary>Case de grille (x colonne, y ligne) ; -1,-1 = aucune.</summary>
    internal readonly struct GridCell
    {
        public readonly int X;
        public readonly int Y;
        public GridCell(int x, int y) { X = x; Y = y; }
        public bool IsNone => X < 0;
        public static readonly GridCell None = new GridCell(-1, -1);
        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>
    /// Choix de la case d'une nouvelle pile à côté des piles existantes du même objet. Logique pure (aucune dépendance
    /// Unity/Valheim), testée par <c>Ovomium.Tests</c>. Grille indexée <c>[x, y]</c>. Règles :
    /// 1. le plus long alignement horizontal (≥ 2 cases contiguës du même objet), sinon le plus long vertical, sinon
    ///    un exemplaire isolé (de préférence avec une case libre voisine) ; égalité : premier dans l'ordre de lecture
    ///    (haut-gauche si <c>topFirst</c>, sinon bas-gauche comme le vanilla) ;
    /// 2. prolonger l'alignement : juste après sa fin (droite / dessous), sinon juste avant son début (gauche /
    ///    dessus), sinon la case libre la plus proche (distance de Manhattan, puis même ligne / colonne, puis ordre
    ///    de lecture). Les cases <see cref="CellState.Excluded"/> ne sont ni alignement ni candidates.
    /// </summary>
    internal static class StackGrouping
    {
        private sealed class Run
        {
            public readonly List<GridCell> Cells = new List<GridCell>();
            public bool Horizontal;
            public GridCell First => Cells[0];
            public GridCell Last => Cells[Cells.Count - 1];
        }

        public static GridCell Choose(CellState[,] grid, bool topFirst, out string trace)
        {
            var run = BestRun(FindRuns(grid, true), grid, topFirst)
                ?? BestRun(FindRuns(grid, false), grid, topFirst)
                ?? BestSingle(grid, topFirst);
            if (run == null)
            {
                trace = "aucun exemplaire";
                return GridCell.None;
            }
            string kind = run.Cells.Count == 1 ? "isolé" : run.Horizontal ? "horizontal" : "vertical";
            var cell = Extend(run, grid);
            string how = "prolongé";
            if (cell.IsNone)
            {
                cell = Nearest(run, grid, topFirst);
                how = "plus proche";
            }
            trace = $"alignement {kind} {run.First}-{run.Last} x{run.Cells.Count} → {cell} ({(cell.IsNone ? "aucune case libre" : how)})";
            return cell;
        }

        /// <summary>Alignements ≥ 2 cases, horizontaux (par ligne) ou verticaux (par colonne).</summary>
        private static List<Run> FindRuns(CellState[,] grid, bool horizontal)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            var runs = new List<Run>();
            int lines = horizontal ? h : w, length = horizontal ? w : h;
            for (int line = 0; line < lines; line++)
            {
                Run current = null;
                for (int i = 0; i <= length; i++)
                {
                    var cell = horizontal ? new GridCell(i, line) : new GridCell(line, i);
                    if (i < length && grid[cell.X, cell.Y] == CellState.Same)
                    {
                        current ??= new Run { Horizontal = horizontal };
                        current.Cells.Add(cell);
                        continue;
                    }
                    if (current != null && current.Cells.Count >= 2)
                        runs.Add(current);
                    current = null;
                }
            }
            return runs;
        }

        /// <summary>Le plus long, à égalité le premier dans l'ordre de lecture.</summary>
        private static Run BestRun(List<Run> runs, CellState[,] grid, bool topFirst)
        {
            Run best = null;
            foreach (var run in runs)
                if (best == null || run.Cells.Count > best.Cells.Count
                    || (run.Cells.Count == best.Cells.Count && Rank(run, grid, topFirst) < Rank(best, grid, topFirst)))
                    best = run;
            return best;
        }

        /// <summary>Exemplaire isolé : d'abord ceux avec une case libre voisine, puis l'ordre de lecture.</summary>
        private static Run BestSingle(CellState[,] grid, bool topFirst)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            Run best = null;
            bool bestHasFree = false;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (grid[x, y] != CellState.Same)
                        continue;
                    var run = new Run { Horizontal = true };
                    run.Cells.Add(new GridCell(x, y));
                    bool hasFree = HasFreeNeighbour(grid, x, y);
                    if (best == null || (hasFree && !bestHasFree)
                        || (hasFree == bestHasFree && Rank(run, grid, topFirst) < Rank(best, grid, topFirst)))
                    {
                        best = run;
                        bestHasFree = hasFree;
                    }
                }
            return best;
        }

        private static bool HasFreeNeighbour(CellState[,] grid, int x, int y) =>
            IsFree(grid, x + 1, y) || IsFree(grid, x - 1, y) || IsFree(grid, x, y + 1) || IsFree(grid, x, y - 1);

        private static bool IsFree(CellState[,] grid, int x, int y) =>
            x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1) && grid[x, y] == CellState.Free;

        /// <summary>Position dans l'ordre de lecture (lignes de haut en bas si <paramref name="topFirst"/>, sinon de bas en haut).</summary>
        private static int Rank(GridCell cell, CellState[,] grid, bool topFirst)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            int row = topFirst ? cell.Y : h - 1 - cell.Y;
            return row * w + cell.X;
        }

        private static int Rank(Run run, CellState[,] grid, bool topFirst)
        {
            int best = int.MaxValue;
            foreach (var cell in run.Cells)
                best = System.Math.Min(best, Rank(cell, grid, topFirst));
            return best;
        }

        /// <summary>Case juste après la fin de l'alignement, sinon juste avant son début ; None si les deux sont prises.</summary>
        private static GridCell Extend(Run run, CellState[,] grid)
        {
            int dx = run.Horizontal ? 1 : 0, dy = run.Horizontal ? 0 : 1;
            var after = new GridCell(run.Last.X + dx, run.Last.Y + dy);
            if (IsFree(grid, after.X, after.Y))
                return after;
            var before = new GridCell(run.First.X - dx, run.First.Y - dy);
            return IsFree(grid, before.X, before.Y) ? before : GridCell.None;
        }

        /// <summary>Case libre la plus proche de l'alignement (Manhattan), puis même ligne/colonne, puis ordre de lecture.</summary>
        private static GridCell Nearest(Run run, CellState[,] grid, bool topFirst)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            var best = GridCell.None;
            int bestDist = int.MaxValue, bestRank = int.MaxValue;
            bool bestAligned = false;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (grid[x, y] != CellState.Free)
                        continue;
                    int dist = Distance(run, x, y);
                    bool aligned = run.Horizontal ? y == run.First.Y : x == run.First.X;
                    int rank = Rank(new GridCell(x, y), grid, topFirst);
                    if (dist < bestDist || (dist == bestDist && (aligned && !bestAligned
                        || (aligned == bestAligned && rank < bestRank))))
                    {
                        best = new GridCell(x, y);
                        bestDist = dist;
                        bestAligned = aligned;
                        bestRank = rank;
                    }
                }
            return best;
        }

        private static int Distance(Run run, int x, int y)
        {
            int best = int.MaxValue;
            foreach (var cell in run.Cells)
                best = System.Math.Min(best, System.Math.Abs(cell.X - x) + System.Math.Abs(cell.Y - y));
            return best;
        }
    }
}
