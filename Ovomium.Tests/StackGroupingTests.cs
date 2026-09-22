using System;
using Ovomium.Features.ChestFill;

namespace Ovomium.Tests
{
    /// <summary>
    /// Grilles de <see cref="StackGrouping"/> : une chaîne par ligne (y croissant vers le bas), une lettre par case :
    /// <c>.</c> libre, <c>S</c> l'objet ajouté, <c>x</c> exclue (barre rapide, cases HotbarSlots), toute autre lettre
    /// un autre objet (<c>o</c>, <c>a</c>, <c>b</c>… : même lettre = même objet).
    /// </summary>
    internal static class StackGroupingTests
    {
        private static int s_failures;

        public static int Run()
        {
            RunRows();
            RunFallback();
            RunColumns();
            Console.WriteLine(s_failures == 0 ? "StackGrouping : tous les tests passent" : $"StackGrouping : {s_failures} échec(s)");
            return s_failures;
        }

        /// <summary>Mode lignes : choix et prolongement de l'alignement.</summary>
        private static void RunRows()
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
            // Pas d'alignement horizontal ≥ 2 (paires horizontales des autres objets) : vertical, prolongé en dessous.
            Check("vertical, prolongé en dessous", true, 4, 3,
                "ooooS.S.",
                "....S...",
                "....S...",
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
        }

        /// <summary>Alignement bloqué aux deux bouts : ligne du dessous, puis du dessus, indépendamment de topFirst.</summary>
        private static void RunFallback()
        {
            // Deux bouts pris : la ligne juste en dessous, sous l'alignement.
            Check("horizontal bloqué, ligne du dessous", true, 1, 2,
                "........",
                "oSSo....",
                "........",
                "........");
            // Même grille, ordre vanilla bas d'abord (ChestFill désactivé) : même résultat.
            Check("horizontal bloqué, ligne du dessous (vanilla)", false, 1, 2,
                "........",
                "oSSo....",
                "........",
                "........");
            // Ligne pleine : première case libre de la ligne du dessous sous l'alignement, pas la plus à gauche.
            Check("ligne pleine, sous l'alignement", true, 2, 2,
                "........",
                "ooSSSooo",
                "o.......",
                "........");
            // Ligne du dessous pleine aussi : ligne du dessus.
            Check("ligne pleine, dessous plein, dessus", true, 2, 0,
                "........",
                "ooSSSooo",
                "oooooooo",
                "........");
            // Une seule ligne : la ligne de l'alignement en dernier recours.
            Check("horizontal bloqué, même ligne en dernier", true, 4, 0,
                "oSSo....");
            // Alignement vertical bloqué (mode lignes) : colonne de droite, la case en face étant prise.
            Check("vertical bloqué, colonne de droite", true, 2, 4,
                "oooooooo",
                "oSo.....",
                "oSo.....",
                "ooo.....",
                "........");
        }

        /// <summary>Mode colonnes (plus de paires verticales qu'horizontales sur tout le coffre).</summary>
        private static void RunColumns()
        {
            // Alignement vertical préféré à l'horizontal, prolongé en dessous.
            Check("colonnes, alignement vertical préféré", true, 0, 3,
                "SSa.....",
                "S.a.....",
                "S.a.....",
                "..a.....");
            // Isolé traité comme vertical : en dessous.
            Check("colonnes, isolé en dessous", true, 1, 1,
                "aS......",
                "a.......",
                "a.......",
                "........");
            // Mode détecté par les autres objets seulement (l'objet ajouté n'a qu'une pile).
            Check("colonnes détectées par les autres objets", true, 2, 1,
                "a.S.....",
                "a.......",
                "a.......",
                "........");
            // Isolé bloqué dessus et dessous : colonne de droite, en face.
            Check("colonnes, isolé bloqué, colonne de droite", true, 3, 1,
                "a.o.....",
                "a.S.....",
                "a.o.....",
                "........");
            // Égalité de paires : mode lignes, isolé prolongé à droite.
            Check("égalité de paires, lignes", true, 3, 1,
                "aa......",
                "b.S.....",
                "b.......",
                "........");
        }

        private static void Check(string name, bool topFirst, int x, int y, params string[] rows)
        {
            var cell = StackGrouping.Choose(Parse(rows), topFirst, out string trace);
            bool ok = cell.X == x && cell.Y == y;
            Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {name} : {trace}{(ok ? "" : $" (attendu ({x},{y}))")}");
            if (!ok)
                s_failures++;
        }

        private static int[,] Parse(string[] rows)
        {
            var grid = new int[rows[0].Length, rows.Length];
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    grid[x, y] = rows[y][x] switch
                    {
                        '.' => StackGrouping.Free,
                        'S' => StackGrouping.Self,
                        'x' => StackGrouping.Excluded,
                        char c => StackGrouping.Self + 1 + c,
                    };
            return grid;
        }
    }
}
