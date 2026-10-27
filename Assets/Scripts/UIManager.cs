using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace BlockBlaster
{
    public sealed class UIManager : MonoBehaviour
    {
        GameManager game;
        RectTransform safe;
        Font font, titleFont;
        GameObject menu, scan, place, hud, countdown, pause, over;
        Text score, wave, notice, scanHint, count, best, finalScore, finalBest, pauseHint, mode, banner;
        Image flash;
        Image[] hearts, chipBars;
        GameObject[] chips;
        Button resume, restart;
        Rect lastSafe;
        int lastWidth, lastHeight;
        float nextRefresh, punch, bannerLeft;
        int shownScore, shownWave = 1;
        GameState lastState = (GameState)(-1);

        public void Initialize(GameManager manager)
        {
            game = manager;
            font = Resources.Load<Font>("Rajdhani-SemiBold");
            titleFont = Resources.Load<Font>("Orbitron");
            var canvasObject = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            safe = Rect("Safe area", canvasObject.transform, Vector2.zero, Vector2.one);
            flash = Panel(safe, Color.clear, Vector2.zero, Vector2.one, false).GetComponent<Image>();
            new GameObject("Event system", typeof(EventSystem), typeof(StandaloneInputModule));
            MakeMenu(); MakeScan(); MakePlace(); MakeHUD(); MakePause(); MakeOver();
            countdown = Group("Countdown");
            count = Label(countdown.transform, "3", new Vector2(0.2f, 0.43f), new Vector2(0.8f, 0.60f), 150, Color.white);
            notice = Label(safe, "", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.15f), 26, Visuals.Gold);
            Refresh();
        }

        GameObject Group(string name) => Rect(name, safe, Vector2.zero, Vector2.one).gameObject;

        void MakeMenu()
        {
            menu = Group("Start");
            Panel(menu.transform, Color.white, Vector2.zero, Vector2.one).GetComponent<Image>().sprite = UiArt.Gradient;
            Label(menu.transform, "AMAL + BASIL  /  PROJECT 18", new Vector2(0.05f, 0.94f), new Vector2(0.95f, 0.98f), 20, Visuals.Muted);
            var emblem = Rect("Block emblem", menu.transform, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f));
            emblem.sizeDelta = new Vector2(230, 145);
            var cells = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(1, 1) };
            foreach (var cell in cells)
            {
                var block = Panel(emblem, Visuals.Cyan, Vector2.zero, Vector2.zero, true, true);
                block.anchorMin = block.anchorMax = new Vector2(0.5f, 0.5f);
                block.sizeDelta = new Vector2(60, 60);
                block.anchoredPosition = new Vector2((cell.x - 1) * 70, (cell.y - 0.5f) * 70);
            }
            Label(menu.transform, "BLOCK\nBLASTER", new Vector2(0.06f, 0.64f), new Vector2(0.94f, 0.79f), 98, Color.white, FontStyle.Bold);
            Label(menu.transform, "A R   A R C A D E", new Vector2(0.1f, 0.59f), new Vector2(0.9f, 0.64f), 26, Visuals.Cyan);
            Label(menu.transform, "Your table. Your arena.\nHold the line against falling blocks.", new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.58f), 30, Visuals.Muted);
            MakeButton(menu.transform, "PLAY ON MY TABLE", new Vector2(0.10f, 0.40f), new Vector2(0.90f, 0.465f), game.PlayAR, Visuals.Cyan, Visuals.Ink);
            MakeButton(menu.transform, "PRACTICE", new Vector2(0.10f, 0.315f), new Vector2(0.90f, 0.38f), game.Practice, Visuals.Navy, Color.white);
            Label(menu.transform, "Drag to steer. Your ship fires on its own.\n3 lives · 7 shapes · endless waves", new Vector2(0.10f, 0.235f), new Vector2(0.90f, 0.30f), 26, Visuals.Muted);
            Label(menu.transform, "TOP SCORES", new Vector2(0.10f, 0.195f), new Vector2(0.90f, 0.225f), 24, Visuals.Gold, FontStyle.Bold);
            best = Label(menu.transform, "", new Vector2(0.10f, 0.075f), new Vector2(0.90f, 0.19f), 26, Color.white);
            MakeButton(menu.transform, "TOGGLE SOUND", new Vector2(0.27f, 0.02f), new Vector2(0.73f, 0.065f), () =>
            { game.Effects.ToggleSound(); game.Tell(game.Effects.Muted ? "SOUND OFF" : "SOUND ON"); }, Visuals.Navy, Visuals.Muted);
        }

        void MakeScan()
        {
            scan = Group("Scan");
            Label(scan.transform, "FIND YOUR ARENA", new Vector2(0.10f, 0.86f), new Vector2(0.90f, 0.92f), 42, Color.white, FontStyle.Bold);
            Label(scan.transform, "Use a well-lit, textured table", new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.85f), 26, Visuals.Cyan);
            Panel(scan.transform, new Color(0.025f, 0.035f, 0.08f, 0.90f), new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.31f), false, true);
            scanHint = Label(scan.transform, "", new Vector2(0.11f, 0.20f), new Vector2(0.89f, 0.29f), 32, Color.white);
            MakeButton(scan.transform, "BACK", new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.075f), game.Menu, Visuals.Navy, Color.white);
        }

        void MakePlace()
        {
            place = Group("Place");
            Label(place.transform, "ARENA READY", new Vector2(0.10f, 0.85f), new Vector2(0.90f, 0.92f), 48, Color.white, FontStyle.Bold);
            Label(place.transform, "Pinch to resize before starting", new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.85f), 28, Visuals.Cyan);
            MakeButton(place.transform, "START GAME", new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.245f), game.StartGame, Visuals.Cyan, Visuals.Ink);
            MakeButton(place.transform, "BACK", new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.075f), game.Menu, Visuals.Navy, Color.white);
        }

        void MakeHUD()
        {
            hud = Group("HUD");
            Panel(hud.transform, new Color(0.025f, 0.035f, 0.08f, 0.88f), new Vector2(0, 0.86f), Vector2.one, false);
            Label(hud.transform, "SCORE", new Vector2(0.06f, 0.95f), new Vector2(0.48f, 0.98f), 20, Visuals.Muted, alignment: TextAnchor.MiddleLeft);
            score = Label(hud.transform, "000000", new Vector2(0.06f, 0.895f), new Vector2(0.54f, 0.95f), 56, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            hearts = new Image[GameRules.MaxLives];
            for (int i = 0; i < hearts.Length; i++)
                hearts[i] = Icon(hud.transform, UiArt.Icon.Heart, new Vector2(0.57f + i * 0.09f, 0.90f), new Vector2(0.65f + i * 0.09f, 0.945f), Visuals.Coral);
            var pauseButton = MakeButton(hud.transform, "", new Vector2(0.87f, 0.905f), new Vector2(0.98f, 0.965f), () => game.Pause("Paused"), Visuals.Navy, Color.white);
            Icon(pauseButton.transform, UiArt.Icon.Pause, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.78f), Color.white);
            wave = Label(hud.transform, "WAVE 01", new Vector2(0.06f, 0.86f), new Vector2(0.55f, 0.895f), 24, Visuals.Cyan, alignment: TextAnchor.MiddleLeft);
            mode = Label(hud.transform, "PRACTICE", new Vector2(0.55f, 0.86f), new Vector2(0.94f, 0.895f), 20, Visuals.Muted, alignment: TextAnchor.MiddleRight);
            // One chip per lasting power-up: its icon over a bar that drains with the time left.
            var chipIcons = new[] { UiArt.Icon.Shield, UiArt.Icon.Triple, UiArt.Icon.Slow };
            chips = new GameObject[chipIcons.Length];
            chipBars = new Image[chipIcons.Length];
            for (int i = 0; i < chipIcons.Length; i++)
            {
                var chip = Panel(hud.transform, Fade(Visuals.Navy, 0.9f), new Vector2(0.305f + i * 0.14f, 0.795f), new Vector2(0.415f + i * 0.14f, 0.85f), false, true);
                Icon(chip, chipIcons[i], new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.92f), Visuals.Gold);
                chipBars[i] = Panel(chip, Visuals.Gold, new Vector2(0.14f, 0.10f), new Vector2(0.86f, 0.20f), false).GetComponent<Image>();
                chipBars[i].sprite = UiArt.Panel;
                chipBars[i].type = Image.Type.Filled;
                chipBars[i].fillMethod = Image.FillMethod.Horizontal;
                chips[i] = chip.gameObject;
            }
            banner = Label(hud.transform, "", new Vector2(0.10f, 0.60f), new Vector2(0.90f, 0.70f), 90, Visuals.Cyan, FontStyle.Bold);
            Label(hud.transform, "DRAG TO MOVE  ·  AUTO-FIRE", new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.065f), 24, Visuals.Muted);
        }

        void MakePause()
        {
            pause = Group("Pause");
            Panel(pause.transform, new Color(0.025f, 0.035f, 0.08f, 0.94f), Vector2.zero, Vector2.one);
            Label(pause.transform, "TAKE A BREATHER", new Vector2(0.07f, 0.66f), new Vector2(0.93f, 0.74f), 56, Color.white, FontStyle.Bold);
            pauseHint = Label(pause.transform, "Paused", new Vector2(0.10f, 0.55f), new Vector2(0.90f, 0.65f), 30, Visuals.Muted);
            resume = MakeButton(pause.transform, "RESUME", new Vector2(0.10f, 0.44f), new Vector2(0.90f, 0.505f), game.Resume, Visuals.Cyan, Visuals.Ink);
            restart = MakeButton(pause.transform, "RESTART", new Vector2(0.10f, 0.35f), new Vector2(0.90f, 0.415f), game.StartGame, Visuals.Navy, Color.white);
            MakeButton(pause.transform, "QUIT TO MENU", new Vector2(0.10f, 0.26f), new Vector2(0.90f, 0.325f), game.Menu, Visuals.Navy, Visuals.Muted);
        }

        void MakeOver()
        {
            over = Group("Game over");
            Panel(over.transform, new Color(0.025f, 0.035f, 0.08f, 0.95f), Vector2.zero, Vector2.one);
            Label(over.transform, "GAME OVER", new Vector2(0.08f, 0.71f), new Vector2(0.92f, 0.79f), 74, Color.white, FontStyle.Bold);
            Label(over.transform, "FINAL SCORE", new Vector2(0.10f, 0.65f), new Vector2(0.90f, 0.69f), 24, Visuals.Muted);
            finalScore = Label(over.transform, "0", new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.65f), 100, Visuals.Cyan, FontStyle.Bold);
            finalBest = Label(over.transform, "PERSONAL BEST  0", new Vector2(0.10f, 0.48f), new Vector2(0.90f, 0.53f), 28, Visuals.Gold);
            MakeButton(over.transform, "PLAY AGAIN", new Vector2(0.10f, 0.35f), new Vector2(0.90f, 0.415f), game.StartGame, Visuals.Cyan, Visuals.Ink);
            MakeButton(over.transform, "MENU", new Vector2(0.10f, 0.26f), new Vector2(0.90f, 0.325f), game.Menu, Visuals.Navy, Color.white);
        }

        void Update()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastWidth || Screen.height != lastHeight)
            {
                lastSafe = Screen.safeArea; lastWidth = Screen.width; lastHeight = Screen.height;
                safe.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
                safe.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
            }
            if (game.State != lastState || Time.unscaledTime >= nextRefresh) Refresh();
            // Per-frame feedback: score punch, wave banner, and the hit flash.
            float dt = Time.unscaledDeltaTime;
            if (game.Score > shownScore) punch = 1;
            shownScore = game.Score;
            punch = Mathf.Max(0, punch - dt * 5);
            score.transform.localScale = Vector3.one * (1 + 0.22f * punch);
            if (game.IsPlaying && game.Wave > shownWave)
            {
                bannerLeft = 1.6f;
                banner.text = "WAVE " + game.Wave.ToString("D2");
                game.Effects.Sound(GameEffects.Cue.Pickup);
            }
            shownWave = game.Wave;
            bannerLeft = Mathf.Max(0, bannerLeft - dt);
            banner.color = Fade(Visuals.Cyan, Mathf.Clamp01(bannerLeft / 0.5f));
            flash.color = Fade(Visuals.Coral, 0.38f * game.HitKick);
        }

        void Refresh()
        {
            lastState = game.State;
            nextRefresh = Time.unscaledTime + 0.1f;
            menu.SetActive(lastState == GameState.Menu);
            scan.SetActive(lastState == GameState.Scan);
            place.SetActive(lastState == GameState.Place);
            bool inGame = lastState == GameState.Playing || lastState == GameState.Countdown;
            hud.SetActive(inGame);
            countdown.SetActive(lastState == GameState.Countdown);
            pause.SetActive(lastState == GameState.Paused);
            over.SetActive(lastState == GameState.GameOver);
            score.text = game.Score.ToString("D6");
            for (int i = 0; i < hearts.Length; i++) hearts[i].color = Fade(Visuals.Coral, i < game.Lives ? 1 : 0.18f);
            wave.text = "WAVE " + game.Wave.ToString("D2");
            mode.text = game.Placer.IsAR ? "TABLETOP AR" : "PRACTICE";
            chips[0].SetActive(game.Shield);
            chips[1].SetActive(game.TripleLeft > 0);
            chips[2].SetActive(game.SlowLeft > 0);
            chipBars[1].fillAmount = game.TripleLeft / 8;
            chipBars[2].fillAmount = game.SlowLeft / 5;
            notice.text = lastState == GameState.Paused ? "" : game.Notice;
            scanHint.text = game.Placer.ScanMessage;
            count.text = Mathf.Max(1, Mathf.CeilToInt(game.Countdown)).ToString();
            if (lastState == GameState.Menu) best.text = BoardText();
            finalScore.text = game.Score.ToString();
            finalBest.text = game.Score > 0 && game.Score >= game.HighScore ? "NEW PERSONAL BEST!" : "PERSONAL BEST  " + game.HighScore;
            pauseHint.text = game.Notice;
            resume.interactable = restart.interactable = game.Placer.HasTracking;
        }

        string BoardText()
        {
            if (game.TopScores.Count == 0) return "No games yet. Set the first score!";
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < game.TopScores.Count; i++)
                lines.Append(i + 1).Append(".   ").Append(game.TopScores[i].ToString("D6")).Append('\n');
            return lines.ToString().TrimEnd();
        }

        static Color Fade(Color color, float alpha) { color.a = alpha; return color; }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        RectTransform Panel(Transform parent, Color color, Vector2 min, Vector2 max, bool blocksInput = true, bool rounded = false)
        {
            var rect = Rect("Panel", parent, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksInput;
            if (rounded) { image.sprite = UiArt.Panel; image.type = Image.Type.Sliced; }
            return rect;
        }

        Image Icon(Transform parent, UiArt.Icon icon, Vector2 min, Vector2 max, Color color)
        {
            var image = Panel(parent, color, min, max, false).GetComponent<Image>();
            image.sprite = UiArt.Get(icon);
            image.preserveAspect = true;
            return image;
        }

        Text Label(Transform parent, string value, Vector2 min, Vector2 max, int size, Color color,
            FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rect = Rect(value.Length > 0 ? value : "Label", parent, min, max);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = style == FontStyle.Bold ? titleFont : font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, size * 2 / 3);
            text.resizeTextMaxSize = size;
            return text;
        }

        Button MakeButton(Transform parent, string title, Vector2 min, Vector2 max, UnityAction action, Color background, Color foreground)
        {
            var panel = Panel(parent, background, min, max, true, true);
            panel.name = title;
            var rim = Panel(panel, Fade(Visuals.Cyan, 0.55f), Vector2.zero, Vector2.one, false).GetComponent<Image>();
            rim.sprite = UiArt.Outline;
            rim.type = Image.Type.Sliced;
            var button = panel.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.90f, 0.96f, 1f);
            colors.pressedColor = new Color(0.65f, 0.78f, 0.84f);
            colors.disabledColor = new Color(0.35f, 0.40f, 0.45f);
            button.colors = colors;
            button.targetGraphic = panel.GetComponent<Image>();
            button.onClick.AddListener(action);
            Label(panel, title, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), 30, foreground, FontStyle.Bold);
            return button;
        }
    }
}
