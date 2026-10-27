#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;

namespace BlockBlaster
{
    // Runs only through the editor menu. Never included in an Android player.
    public sealed class GameSmokeCheck : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;
            var game = FindAnyObjectByType<GameManager>();
            bool hadBest = PlayerPrefs.HasKey("BlockBlaster.Best");
            int best = PlayerPrefs.GetInt("BlockBlaster.Best", 0);
            string top = PlayerPrefs.GetString("BlockBlaster.Top", "");
            try
            {
                Require(game != null, "Entry scene must contain a game manager");
                game.Practice();
                Require(game.State == GameState.Place, "Practice must reach placement");
                Require(game.Arena.GetComponentsInChildren<Tetromino>(true).Length == 20, "Pool cap is 20 blocks");
                game.StartGame();
                yield return new WaitForSeconds(3.2f);
                Require(game.IsPlaying && game.Lives == 3 && game.Score == 0, "Countdown must start a fresh game");
                game.Spawner.Clear();
                var block = game.Arena.GetComponentsInChildren<Tetromino>(true)[0];
                block.Spawn(GameRules.Shape.O, GameRules.Layout(GameRules.Shape.O, 0), 0, false);
                game.Spawner.Fire(new Vector3(0, GameRules.PlayHeight, 0.65f), 0);
                yield return new WaitForSeconds(0.25f);
                Require(game.Score == 25, "A physics bullet must damage exactly one cube");
                foreach (var cube in block.GetComponentsInChildren<BlockCube>()) cube.Hit();
                Require(game.Score == 100 && !block.Active, "Chipping four cubes destroys the shape for 100 points");
                game.MissedBlock();
                Require(game.Score == 90, "A missed block costs ten points");
                game.Collect(GameRules.Power.Shield);
                game.ShipHit();
                Require(game.Lives == 3 && !game.Shield, "Shield absorbs one hit");
                game.ShipHit();
                Require(game.Lives == 3, "Hit immunity prevents clustered damage");
                yield return new WaitForSeconds(1.6f);
                game.ShipHit();
                Require(game.Lives == 2, "An unshielded hit costs one life");
                game.Collect(GameRules.Power.ExtraLife);
                game.Collect(GameRules.Power.ExtraLife);
                Require(game.Lives == 3, "Extra lives cap at three");
                game.Collect(GameRules.Power.TripleShot);
                game.Collect(GameRules.Power.SlowMo);
                Require(game.TripleLeft == 8 && game.SlowLeft == 5, "Timed power-ups initialize correctly");
                game.Pause("Smoke check");
                float elapsed = game.Elapsed;
                yield return new WaitForSecondsRealtime(0.25f);
                Require(game.State == GameState.Paused && game.Elapsed == elapsed, "Pause freezes gameplay");
                game.Resume();
                Require(game.IsPlaying, "Resume returns to gameplay");
                for (int i = 0; i < 3; i++)
                { yield return new WaitForSeconds(1.6f); game.Spawner.Clear(); game.ShipHit(); }
                Require(game.State == GameState.GameOver && game.Lives == 0, "Three hits end a game");
                game.StartGame();
                Require(game.State == GameState.Countdown && game.Lives == 3 && game.Score == 0, "Replay resets state and keeps the arena");
                game.Menu();
                Require(game.State == GameState.Menu && game.Arena == null && Time.timeScale == 1, "Quit cleans up the arena");
                Debug.Log("Gameplay smoke check passed: real bullet collision, cube damage, score, shield, immunity, power-ups, pause, game over, and replay.");
            }
            finally
            {
                if (game != null) game.Menu();
                if (hadBest) PlayerPrefs.SetInt("BlockBlaster.Best", best); else PlayerPrefs.DeleteKey("BlockBlaster.Best");
                PlayerPrefs.SetString("BlockBlaster.Top", top);
                PlayerPrefs.Save();
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Gameplay smoke check failed: " + message); }
    }
}
#endif
