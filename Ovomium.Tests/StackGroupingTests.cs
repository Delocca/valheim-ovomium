using System;
using Ovomium.Features.ChestFill;

namespace Ovomium.Tests
{
    /// <summary>
    /// Grilles de <see cref="StackGrouping"/> : une chaîne par ligne (y croissant vers le bas), une lettre par case :
    /// <c>.</c> libre, <c>S</c> même objet, <c>o</c> autre objet, <c>x</c> exclue (barre rapide, cases HotbarSlots).
    /// </summary>
    internal static class StackGroupingTests
    {
        private static int s_failures;

        public static int Run()
        {
            // Prolongement à droite du plus long alignement horizontal.
            Check("horizontal, prolongé à droite", true, 3, 1,
                "........",
                "SSS.....",
                "........",
                "........");
            // Fin prise : juste avant le début.
            Check("horizontal, prolongé à gauche", true, 0, 1,
                "........",
                ".SSSo...",
                "........",
                "........");
            // Deux bouts pris : la plus proche (dist 1 au-dessus / en dessous), ordre de lecture haut-gauche.
            Check("horizontal bloqué, plus proche (haut)", true, 1, 0,
                "........",
                "oSSo....",
                "........",
                "........");
            // Même grille, ordre vanilla bas d'abord (ChestFill désactivé) : la case en dessous.
            Check("horizontal bloqué, plus proche (bas, vanilla)", false, 1, 2,
                "........",
                "oSSo....",
                "........",
                "........");
            // Pas d'alignement horizontal ≥ 2 : vertical, prolongé en dessous.
            Check("vertical, prolongé en dessous", true, 2, 3,
                "..S..S..",
                "..S.....",
                "..S.....",
                "........");
            // Vertical bloqué (bord haut, autre objet en dessous) : à distance 2, (1,3) précède (2,4) en lecture
            // mais la même colonne est préférée.
            Check("vertical bloqué, même colonne", true, 2, 4,
                "ooSoo...",
                "ooSoo...",
                "ooSoo...",
                "..o.....",
                "........");
            // Exemplaires isolés dispersés : celui avec une case libre voisine, prolongé à droite.
            Check("isolés, celui avec une voisine libre", true, 6, 2,
                "So......",
                "o.......",
                ".....S..",
                "........");
            // Le plus long gagne sur l'ordre de lecture.
            Check("plus long avant premier", true, 3, 2,
                "SS......",
                "........",
                "SSS.....",
                "........");
            // Inventaire joueur : ligne 0 exclue (même un alignement y est ignoré), case HotbarSlots (5,1) exclue.
            Check("joueur, exclusions", true, 2, 1,
                "xxxxxxxx",
                "...SSx..",
                "........",
                "........");
            // Aucun exemplaire : pas de regroupement.
            Check("aucun exemplaire", true, -1, -1,
                "oo......",
                "........",
                "........",
                "........");
            // Plus aucune case libre hors exclusions : pas de regroupement.
            Check("plein", true, -1, -1,
                "xxxxxxxx",
                "SSoooooo",
                "oooooooo",
                "oooooooo");
            Console.WriteLine(s_failures == 0 ? "StackGrouping : tous les tests passent" : $"StackGrouping : {s_failures} échec(s)");
            return s_failures;
        }

        private static void Check(string name, bool topFirst, int x, int y, params string[] rows)
        {
            var cell = StackGrouping.Choose(Parse(rows), topFirst, out string trace);
            bool ok = cell.X == x && cell.Y == y;
            Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {name} : {trace}{(ok ? "" : $" (attendu ({x},{y}))")}");
            if (!ok)
                s_failures++;
        }

        private static CellState[,] Parse(string[] rows)
        {
            var grid = new CellState[rows[0].Length, rows.Length];
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    grid[x, y] = rows[y][x] switch
                    {
                        'S' => CellState.Same,
                        'o' => CellState.Other,
                        'x' => CellState.Excluded,
                        _ => CellState.Free,
                    };
            return grid;
        }
    }
}
