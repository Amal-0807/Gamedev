using System.Collections.Generic;
using UnityEngine;

namespace BlockBlaster
{
    public enum GameState { Menu, Scan, Place, Countdown, Playing, Paused, GameOver }

    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Menu;
        public int Lives { get; private set; } = GameRules.MaxLives;
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public IReadOnlyList<int> TopScores => topScores;
        // 1 at the moment the ship is hit, easing to 0 over 0.3 s. Drives the screen flash and camera kick.
        public float HitKick => IsPlaying ? Mathf.Clamp01((InvincibleLeft - (GameRules.InvincibleSeconds - 0.3f)) / 0.3f) : 0;
        public int Wave => GameRules.Wave(Elapsed);
        public float Elapsed { get; private set; }
        public float Countdown { get; private set; }
        public bool Shield { get; private set; }
        public float TripleLeft { get; private set; }
        public float SlowLeft { get; private set; }
        public float InvincibleLeft { get; private set; }
        public string Notice { get; private set; } = "";
        public bool IsPlaying => State == GameState.Playing;
        public ArenaPlacer Placer { get; private set; }
        public ShipController Ship { get; private set; }
        public BlockSpawner Spawner { get; private set; }
        public GameEffects Effects { get; private set; }
        public UIManager UI { get; private set; }
        public Transform Arena { get; private set; }
        readonly List<int> topScores = new();
        GameState resumeState;
        float noticeLeft;
        bool backgrounded;

        void Awake()
        {
            Application.targetFrameRate = 60;
            // Bullets hit cubes only; shapes do not push one another off their columns.
            Physics.IgnoreLayerCollision(8, 8);
            Physics.IgnoreLayerCollision(8, 10);
            Physics.IgnoreLayerCollision(8, 11);
            Physics.IgnoreLayerCollision(9, 9);
            Physics.IgnoreLayerCollision(9, 11);
            Physics.IgnoreLayerCollision(11, 11);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            HighScore = PlayerPrefs.GetInt("BlockBlaster.Best", 0);
            foreach (var entry in PlayerPrefs.GetString("BlockBlaster.Top", "").Split(','))
                if (int.TryParse(entry, out int value)) topScores.Add(value);
            if (topScores.Count == 0 && HighScore > 0) topScores.Add(HighScore);
            Placer = gameObject.AddComponent<ArenaPlacer>();
            Placer.Initialize(this);
            Effects = gameObject.AddComponent<GameEffects>();
            Effects.Initialize();
            UI = gameObject.AddComponent<UIManager>();
            UI.Initialize(this);
        }

        void Update()
        {
            if (Placer.IsAR && !Placer.HasTracking && (State == GameState.Playing || State == GameState.Countdown))
                Pause(Arena == null ? "Arena lost. Quit and place it again." : "Move phone slowly to find the table");
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing || State == GameState.Countdown) Pause("Paused");
                else if (State == GameState.Paused) Resume();
                else if (State != GameState.Menu) Menu();
            }
            if (noticeLeft > 0 && State != GameState.Paused)
            {
                noticeLeft -= Time.unscaledDeltaTime;
                if (noticeLeft <= 0) Notice = "";
            }
            if (State == GameState.Countdown)
            {
                Countdown -= Time.deltaTime;
                if (Countdown <= 0) { State = GameState.Playing; Tell("DRAG TO MOVE  ·  AUTO-FIRE", 3); }
            }
            if (!IsPlaying) return;
            Elapsed += Time.deltaTime;
            TripleLeft = Mathf.Max(0, TripleLeft - Time.deltaTime);
            SlowLeft = Mathf.Max(0, SlowLeft - Time.deltaTime);
            InvincibleLeft = Mathf.Max(0, InvincibleLeft - Time.deltaTime);
        }

        public void Practice()
        {
            if (State != GameState.Menu) return;
            Placer.BeginPractice();
        }

        public void PlayAR()
        {
            if (State != GameState.Menu) return;
            State = GameState.Scan;
            Placer.BeginAR();
        }

        public void SetArena(Transform arena)
        {
            Arena = arena;
            var shipObject = new GameObject("Ship");
            shipObject.transform.SetParent(arena, false);
            Ship = shipObject.AddComponent<ShipController>();
            Ship.Initialize(this);
            Spawner = gameObject.AddComponent<BlockSpawner>();
            Spawner.Initialize(this);
            State = GameState.Place;
        }

        public void StartGame()
        {
            if (State != GameState.Place && State != GameState.GameOver && State != GameState.Paused) return;
            if (Placer.IsAR && !Placer.HasTracking) { Tell("Move phone slowly to find the table"); return; }
            Time.timeScale = 1;
            Spawner.Clear();
            Effects.Clear();
            Lives = GameRules.MaxLives;
            Score = 0;
            Elapsed = TripleLeft = SlowLeft = InvincibleLeft = 0;
            Shield = false;
            Notice = "";
            Countdown = 3;
            Ship.ResetShip();
            Physics.SyncTransforms();
            State = GameState.Countdown;
            Placer.HidePlanes();
        }

        public void AddCubeScore()
        {
            if (!IsPlaying) return;
            Score += GameRules.CubePoints;
            SaveBest();
        }

        public void MissedBlock()
        {
            if (!IsPlaying) return;
            Score = GameRules.Miss(Score);
            Tell("BLOCK MISSED  -10", 1);
        }

        public void ShipHit()
        {
            if (!IsPlaying || InvincibleLeft > 0) return;
            if (Shield)
            {
                Shield = false;
                InvincibleLeft = GameRules.InvincibleSeconds;
                Tell("SHIELD ABSORBED THE HIT");
                Effects.Sound(GameEffects.Cue.Pickup);
                return;
            }
            Lives--;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
            InvincibleLeft = GameRules.InvincibleSeconds;
            Effects.Sound(GameEffects.Cue.Hurt);
            Effects.Burst(Arena, Ship.transform.localPosition, Visuals.Coral, 18);
            if (Lives == 0)
            {
                SaveBest();
                RecordScore();
                State = GameState.GameOver;
                Ship.ReleasePointer();
                PlayerPrefs.Save();
                Time.timeScale = 0;
            }
        }

        public void Collect(GameRules.Power kind)
        {
            if (!IsPlaying) return;
            switch (kind)
            {
                case GameRules.Power.Shield: Shield = true; Tell("SHIELD  ·  NEXT HIT ABSORBED"); break;
                case GameRules.Power.ExtraLife: Lives = GameRules.AddLife(Lives); Tell("EXTRA LIFE  ·  MAXIMUM 3"); break;
                case GameRules.Power.TripleShot: TripleLeft = 8; Tell("TRIPLE SHOT  ·  8 SECONDS"); break;
                case GameRules.Power.SlowMo: SlowLeft = 5; Tell("SLOW-MO  ·  5 SECONDS"); break;
            }
            Effects.Sound(GameEffects.Cue.Pickup);
        }

        public void Pause(string reason)
        {
            if (State != GameState.Playing && State != GameState.Countdown) return;
            resumeState = State;
            State = GameState.Paused;
            Notice = reason;
            Time.timeScale = 0;
            Ship.ReleasePointer();
        }

        public void Resume()
        {
            if (State != GameState.Paused || backgrounded) return;
            if (Placer.IsAR && !Placer.HasTracking)
            { Notice = "Move phone slowly to find the table"; return; }
            Notice = "";
            State = resumeState;
            Time.timeScale = 1;
        }

        public void Menu()
        {
            Time.timeScale = 1;
            if (Spawner != null) { Spawner.Clear(); Destroy(Spawner); Spawner = null; }
            Effects.Clear();
            Placer.ResetPlacement();
            Arena = null;
            Ship = null;
            State = GameState.Menu;
            Notice = "";
        }

        public void Tell(string text, float seconds = 2.5f) { Notice = text; noticeLeft = seconds; }

        void SaveBest()
        {
            if (Score <= HighScore) return;
            HighScore = Score;
            PlayerPrefs.SetInt("BlockBlaster.Best", HighScore);
            // Flush at game-over/background, rather than writing storage for every hit.
            if (State == GameState.GameOver || Lives == 0) PlayerPrefs.Save();
        }

        void RecordScore()
        {
            if (Score <= 0) return;
            topScores.Add(Score);
            topScores.Sort((a, b) => b.CompareTo(a));
            if (topScores.Count > 5) topScores.RemoveAt(5);
            PlayerPrefs.SetString("BlockBlaster.Top", string.Join(",", topScores));
        }

        void OnApplicationPause(bool paused)
        {
            backgrounded = paused;
            if (paused) { Pause("Tap Resume when you are ready"); PlayerPrefs.Save(); }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) { Pause("Tap Resume when you are ready"); PlayerPrefs.Save(); }
        }

        void OnDestroy() { Time.timeScale = 1; PlayerPrefs.Save(); }
    }
}
