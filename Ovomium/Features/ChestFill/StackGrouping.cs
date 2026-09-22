using System.Collections.Generic;

namespace Ovomium.Features.ChestFill
{
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
    /// Unity/Valheim), testée par <c>Ovomium.Tests</c>. Grille indexée <c>[x, y]</c> d'identifiants d'objet :
    /// <see cref="Free"/>, <see cref="Excluded"/> (ni alignement ni candidate), <see cref="Self"/> = l'objet ajouté,
    /// tout autre entier = un autre objet (même entier = même objet). Règles :
    /// 0. orientation du rangement : paires de cases adjacentes contenant le même objet (tous objets, hors exclues) ;
    ///    plus de paires verticales qu'horizontales → mode « colonnes » (axe principal vertical), sinon mode « lignes »
    ///    (axe principal horizontal) ;
    /// 1. le plus long alignement (≥ 2 cases contiguës de l'objet) sur l'axe principal, sinon sur l'autre axe, sinon un
    ///    exemplaire isolé (de préférence avec une case libre voisine), traité comme un alignement sur l'axe principal ;
    ///    égalité : premier dans l'ordre de lecture (haut-gauche si <c>topFirst</c>, sinon bas-gauche comme le vanilla) ;
    /// 2. prolonger l'alignement : juste après sa fin (droite / dessous), sinon juste avant son début (gauche / dessus) ;
    /// 3. sinon repli, indépendant de <c>topFirst</c> : les lignes (alignement horizontal) ou colonnes (vertical)
    ///    parallèles à l'alignement, de la plus proche à la plus lointaine, dessous avant dessus (droite avant gauche)
    ///    à distance égale, celle de l'alignement lui-même en dernier ; dans chacune, les cases libres en face de
    ///    l'alignement puis les autres, de gauche à droite (de haut en bas).
    /// </summary>
    internal static class StackGrouping
    {
        public const int Free = 0;
        public const int Excluded = -1;
        public const int Self = 1;

        private sealed class Run
        {
            public readonly List<GridCell> Cells = new List<GridCell>();
            public bool Horizontal;
            public GridCell First => Cells[0];
            public GridCell Last => Cells[Cells.Count - 1];
        }

        public static GridCell Choose(int[,] grid, bool topFirst, out string trace)
        {
            CountPairs(grid, out int rowPairs, out int columnPairs);
            bool rows = columnPairs <= rowPairs;
            string mode = $"mode {(rows ? "lignes" : "colonnes")} ({rowPairs} paires horizontales, {columnPairs} verticales)";
            var run = BestRun(FindRuns(grid, rows), grid, topFirst)
                ?? BestRun(FindRuns(grid, !rows), grid, topFirst)
                ?? BestSingle(grid, rows, topFirst);
            if (run == null)
            {
                trace = $"{mode}, aucun exemplaire";
                return GridCell.None;
            }
            string kind = run.Cells.Count == 1 ? "isolé" : run.Horizontal ? "horizontal" : "vertical";
            var cell = Extend(run, grid);
            string how = "prolongé";
            if (cell.IsNone)
            {
                cell = Fallback(run, grid);
                how = run.Horizontal ? "ligne voisine" : "colonne voisine";
            }
            trace = $"{mode}, alignement {kind} {run.First}-{run.Last} x{run.Cells.Count} → {cell} ({(cell.IsNone ? "aucune case libre" : how)})";
            return cell;
        }

        /// <summary>Paires de cases adjacentes (horizontalement / verticalement) contenant le même objet, tous objets confondus.</summary>
        private static void CountPairs(int[,] grid, out int horizontal, out int vertical)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            horizontal = vertical = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int id = grid[x, y];
                    if (id <= Free)
                        continue;
                    if (x + 1 < w && grid[x + 1, y] == id)
                        horizontal++;
                    if (y + 1 < h && grid[x, y + 1] == id)
                        vertical++;
                }
        }

        /// <summary>Case n° <paramref name="i"/> de la ligne (si horizontal) ou de la colonne n° <paramref name="line"/>.</summary>
        private static GridCell At(bool horizontal, int i, int line) =>
            horizontal ? new GridCell(i, line) : new GridCell(line, i);

        /// <summary>Alignements ≥ 2 cases de l'objet ajouté, horizontaux (par ligne) ou verticaux (par colonne).</summary>
        private static List<Run> FindRuns(int[,] grid, bool horizontal)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            var runs = new List<Run>();
            int lines = horizontal ? h : w, length = horizontal ? w : h;
            for (int line = 0; line < lines; line++)
            {
                Run current = null;
                for (int i = 0; i <= length; i++)
                {
                    var cell = At(horizontal, i, line);
                    if (i < length && grid[cell.X, cell.Y] == Self)
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
        private static Run BestRun(List<Run> runs, int[,] grid, bool topFirst)
        {
            Run best = null;
            foreach (var run in runs)
                if (best == null || run.Cells.Count > best.Cells.Count
                    || (run.Cells.Count == best.Cells.Count && Rank(run, grid, topFirst) < Rank(best, grid, topFirst)))
                    best = run;
            return best;
        }

        /// <summary>Exemplaire isolé : d'abord ceux avec une case libre voisine, puis l'ordre de lecture.</summary>
        private static Run BestSingle(int[,] grid, bool horizontal, bool topFirst)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            Run best = null;
            bool bestHasFree = false;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (grid[x, y] != Self)
                        continue;
                    var run = new Run { Horizontal = horizontal };
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

        private static bool HasFreeNeighbour(int[,] grid, int x, int y) =>
            IsFree(grid, x + 1, y) || IsFree(grid, x - 1, y) || IsFree(grid, x, y + 1) || IsFree(grid, x, y - 1);

        private static bool IsFree(int[,] grid, int x, int y) =>
            x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1) && grid[x, y] == Free;

        /// <summary>Position dans l'ordre de lecture (lignes de haut en bas si <paramref name="topFirst"/>, sinon de bas en haut).</summary>
        private static int Rank(GridCell cell, int[,] grid, bool topFirst)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            int row = topFirst ? cell.Y : h - 1 - cell.Y;
            return row * w + cell.X;
        }

        private static int Rank(Run run, int[,] grid, bool topFirst)
        {
            int best = int.MaxValue;
            foreach (var cell in run.Cells)
                best = System.Math.Min(best, Rank(cell, grid, topFirst));
            return best;
        }

        /// <summary>Case juste après la fin de l'alignement, sinon juste avant son début ; None si les deux sont prises.</summary>
        private static GridCell Extend(Run run, int[,] grid)
        {
            int dx = run.Horizontal ? 1 : 0, dy = run.Horizontal ? 0 : 1;
            var after = new GridCell(run.Last.X + dx, run.Last.Y + dy);
            if (IsFree(grid, after.X, after.Y))
                return after;
            var before = new GridCell(run.First.X - dx, run.First.Y - dy);
            return IsFree(grid, before.X, before.Y) ? before : GridCell.None;
        }

        /// <summary>
        /// Lignes (colonnes) parallèles à l'alignement, de la plus proche à la plus lointaine, dessous (droite) avant
        /// dessus (gauche) à distance égale, celle de l'alignement en dernier.
        /// </summary>
        private static GridCell Fallback(Run run, int[,] grid)
        {
            int lines = run.Horizontal ? grid.GetLength(1) : grid.GetLength(0);
            int own = run.Horizontal ? run.First.Y : run.First.X;
            for (int d = 1; d < lines; d++)
            {
                var cell = FirstFreeInLine(run, grid, own + d);
                if (cell.IsNone)
                    cell = FirstFreeInLine(run, grid, own - d);
                if (!cell.IsNone)
                    return cell;
            }
            return FirstFreeInLine(run, grid, own);
        }

        /// <summary>Première case libre de la ligne (colonne) : en face de l'alignement d'abord, puis les autres, de gauche à droite (de haut en bas).</summary>
        private static GridCell FirstFreeInLine(Run run, int[,] grid, int line)
        {
            int lines = run.Horizontal ? grid.GetLength(1) : grid.GetLength(0);
            if (line < 0 || line >= lines)
                return GridCell.None;
            int length = run.Horizontal ? grid.GetLength(0) : grid.GetLength(1);
            int start = run.Horizontal ? run.First.X : run.First.Y, end = run.Horizontal ? run.Last.X : run.Last.Y;
            for (int i = start; i <= end; i++)
            {
                var cell = At(run.Horizontal, i, line);
                if (IsFree(grid, cell.X, cell.Y))
                    return cell;
            }
            for (int i = 0; i < length; i++)
            {
                var cell = At(run.Horizontal, i, line);
                if (IsFree(grid, cell.X, cell.Y))
                    return cell;
            }
            return GridCell.None;
        }
    }
}
