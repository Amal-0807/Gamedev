using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlockBlaster.Editor
{
    public static class GameSelfCheck
    {
        [MenuItem("Block Blaster/Check game rules")]
        public static void RunRules()
        {
            foreach (GameRules.Shape shape in Enum.GetValues(typeof(GameRules.Shape)))
            {
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    var layout = GameRules.Layout(shape, rotation);
                    Require(layout.Length == 4, "Every tetromino has four cubes");
                    var unique = new HashSet<(int, int)>();
                    int minX = int.MaxValue, minY = int.MaxValue;
                    foreach (var cell in layout)
                    {
                        Require(unique.Add((cell.X, cell.Y)), "Shape cubes cannot overlap");
                        minX = Math.Min(minX, cell.X); minY = Math.Min(minY, cell.Y);
                    }
                    Require(minX == 0 && minY == 0, "Rotated shapes must be normalized");
                    // All rotations must stay connected by cube edges.
                    var reached = new HashSet<(int, int)> { (layout[0].X, layout[0].Y) };
                    bool changed;
                    do
                    {
                        changed = false;
                        foreach (var a in layout)
                            foreach (var b in layout)
                                if (reached.Contains((a.X, a.Y)) && Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1)
                                    changed |= reached.Add((b.X, b.Y));
                    } while (changed);
                    Require(reached.Count == 4, "A tetromino must be connected");
                    int width = GameRules.Width(layout);
                    Require(width >= 1 && width <= 4, "Shape width must fit four cells");
                    for (int column = 0; column <= GameRules.Columns - width; column++)
                        foreach (var cell in layout)
                        {
                            float x = (column + cell.X - (GameRules.Columns - 1) * 0.5f) * GameRules.CellSize;
                            Require(Math.Abs(x) + GameRules.CubeSize * 0.5f < GameRules.ArenaWidth * 0.5f, "Spawn must fit inside the rails");
                        }
                }
            }
            Require(4 * GameRules.CubePoints == 100, "A whole tetromino scores 100");
            Require(GameRules.Miss(5) == 0 && GameRules.Miss(25) == 15, "Miss penalties cannot make score negative");
            Require(GameRules.AddLife(2) == 3 && GameRules.AddLife(3) == 3, "Lives cap at three");
            Require(GameRules.Wave(0) == 1 && GameRules.Wave(29.99f) == 1 && GameRules.Wave(30) == 2, "Wave boundary is 30 seconds");
            Require(Math.Abs(GameRules.FallSpeed(2) / GameRules.FallSpeed(1) - 1.1f) < 0.001f, "Fall speed increases by ten percent");
            Require(Math.Abs(GameRules.SpawnInterval(1) / GameRules.SpawnInterval(2) - 1.15f) < 0.001f, "Spawn rate increases by fifteen percent");
            Require(GameRules.FallSpeed(10000) < 1 && GameRules.SpawnInterval(10000) > 0.1f, "Late waves stay bounded");
            Debug.Log("Game rules passed: 28 shape rotations, connectivity, spawn bounds, score, lives, and waves.");
        }

        public static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Self-check failed: " + message); }

        [MenuItem("Block Blaster/Run gameplay smoke check")]
        static void Smoke()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before running the check.");
            RunRules();
            ProjectSetup.OpenGame();
            SessionState.SetBool("BlockBlaster.Smoke", true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void WatchPlayMode()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BlockBlaster.Smoke", false)) return;
                SessionState.SetBool("BlockBlaster.Smoke", false);
                new GameObject("Smoke check").AddComponent<GameSmokeCheck>();
            };
        }
    }
}
