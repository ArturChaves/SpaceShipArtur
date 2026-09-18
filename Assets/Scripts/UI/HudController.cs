using System.Text;
using SpaceShip.Core;
using SpaceShip.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceShip.UI
{
    [DisallowMultipleComponent]
    public class HudController : MonoBehaviour
    {
        [Header("Aparencia")]
        [SerializeField] private Color textColor = new Color(0.90f, 0.96f, 1f, 1f);
        [SerializeField] private Color accentColor = new Color(0.36f, 0.86f, 1f, 1f);
        [SerializeField, Min(8)] private int statusFontSize = 26;
        [SerializeField, Min(8)] private int messageFontSize = 46;
        [SerializeField, Min(8)] private int hintFontSize = 18;

        [Header("Tratamento visual do slow motion")]
        [Tooltip("Vinheta esticada em tela cheia durante o efeito.")]
        [SerializeField] private Sprite vignetteSprite;

        [Tooltip("Cor da vinheta no auge do efeito.")]
        [SerializeField] private Color vignetteColor = new Color(0.03f, 0.07f, 0.20f, 0.80f);

        [Tooltip("Tom frio aplicado sobre a tela inteira no auge do efeito.")]
        [SerializeField] private Color slowMotionTint = new Color(0.25f, 0.60f, 1f, 0.13f);

        private Text scoreLabel;
        private Text highScoreLabel;
        private Text livesLabel;
        private Text messageLabel;
        private Text hintLabel;
        private Text warpLabel;
        private RectTransform warpBarFill;
        private RectTransform warpBarRoot;
        private GameObject canvasObject;
        private Image vignetteOverlay;
        private Image tintOverlay;
        private Text powerUpLabel;
        private Text bossLabel;
        private RectTransform bossBarRoot;
        private RectTransform bossBarFill;
        private PlayerShip boundPlayer;
        private Boss boundBoss;
        private Text creditsLabel;
        private Text debuffLabel;
        private Text waveLabel;
        private Text comboLabel;
        private RunState run;
        private WaveDirector waves;

        private GameManager game;
        private TimeWarp warp;

        private float announceTimer;
        private string announceText = string.Empty;
        private readonly StringBuilder builder = new StringBuilder(64);

        private const float WarpBarWidth = 260f;
        private const float BossBarWidth = 420f;

        private void Start()
        {
            BuildInterface();

            game = GameManager.Instance;
            warp = TimeWarp.Instance;
            run = RunState.Instance;
            waves = WaveDirector.Instance;

            if (run != null)
            {
                run.CreditsChanged += HandleCreditsChanged;
                run.DifficultyChanged += HandleDifficultyChanged;
                HandleCreditsChanged(run.Credits);
            }

            if (waves != null)
            {
                waves.ProgressChanged += HandleWaveProgress;
            }

            if (game == null)
            {
                Debug.LogError("[HudController] Nenhum GameManager na cena.", this);
                return;
            }

            game.ScoreChanged += HandleScoreChanged;
            game.LivesChanged += HandleLivesChanged;
            game.StateChanged += HandleStateChanged;
            game.Announced += HandleAnnouncement;
            game.BossSpawned += HandleBossSpawned;
            game.PlayerSpawned += HandlePlayerSpawned;

            HandleScoreChanged(game.Score);
            HandleLivesChanged(game.Lives);
            HandleStateChanged(game.State);
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.ScoreChanged -= HandleScoreChanged;
                game.LivesChanged -= HandleLivesChanged;
                game.StateChanged -= HandleStateChanged;
                game.Announced -= HandleAnnouncement;
                game.BossSpawned -= HandleBossSpawned;
                game.PlayerSpawned -= HandlePlayerSpawned;
            }

            if (boundPlayer != null)
            {
                boundPlayer.PowerUpsChanged -= HandlePowerUpsChanged;
                boundPlayer = null;
            }

            if (boundBoss != null)
            {
                boundBoss.HealthChanged -= HandleBossHealthChanged;
                boundBoss = null;
            }

            if (run != null)
            {
                run.CreditsChanged -= HandleCreditsChanged;
                run.DifficultyChanged -= HandleDifficultyChanged;
            }

            if (waves != null)
            {
                waves.ProgressChanged -= HandleWaveProgress;
            }

            if (canvasObject != null)
            {
                Destroy(canvasObject);
                canvasObject = null;
            }
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;

            UpdateWarpBar();
            UpdateSlowMotionOverlays();
            UpdatePowerUpLabel();
            UpdateDebuffLabel();
            UpdateComboLabel();
            SyncBossBar();

            if (announceTimer > 0f)
            {
                announceTimer -= delta;
                if (announceTimer <= 0f && game != null)
                {
                    HandleStateChanged(game.State);
                }
            }
        }

        private void UpdateWarpBar()
        {
            if (warp == null)
            {
                warp = TimeWarp.Instance;
            }

            bool active = warp != null && warp.IsActive;

            if (warpBarRoot != null)
            {
                warpBarRoot.gameObject.SetActive(active);
            }
            if (warpLabel != null)
            {
                warpLabel.gameObject.SetActive(active);
            }

            if (!active || warpBarFill == null)
            {
                return;
            }

            float fraction = Mathf.Clamp01(warp.Remaining / Mathf.Max(0.01f, warp.DefaultDuration));
            warpBarFill.sizeDelta = new Vector2(WarpBarWidth * fraction, warpBarFill.sizeDelta.y);

            if (warpLabel != null)
            {
                builder.Length = 0;
                builder.Append("SLOW MOTION  ");
                builder.Append(warp.Remaining.ToString("0.0"));
                builder.Append("s");
                warpLabel.text = builder.ToString();
            }
        }

        private void UpdateSlowMotionOverlays()
        {
            if (vignetteOverlay == null && tintOverlay == null)
            {
                return;
            }

            float weight = warp != null ? warp.Weight : 0f;

            if (vignetteOverlay != null)
            {
                Color color = vignetteColor;
                color.a *= weight;
                vignetteOverlay.color = color;

                vignetteOverlay.enabled = weight > 0.001f;
            }

            if (tintOverlay != null)
            {
                Color color = slowMotionTint;
                color.a *= weight;
                tintOverlay.color = color;
                tintOverlay.enabled = weight > 0.001f;
            }
        }

        private void HandleScoreChanged(int value)
        {
            if (scoreLabel != null)
            {
                builder.Length = 0;
                builder.Append("PONTOS  ");
                builder.Append(value.ToString("D5"));
                scoreLabel.text = builder.ToString();
            }

            if (highScoreLabel != null && game != null)
            {
                builder.Length = 0;
                builder.Append("RECORDE  ");
                builder.Append(game.HighScore.ToString("D5"));
                highScoreLabel.text = builder.ToString();
            }
        }

        private void HandleLivesChanged(int value)
        {
            if (livesLabel == null)
            {
                return;
            }

            builder.Length = 0;
            builder.Append("NAVES  ");
            for (int i = 0; i < value; i++)
            {
                builder.Append('▶');
                builder.Append(' ');
            }

            if (value <= 0)
            {
                builder.Append('-');
            }

            livesLabel.text = builder.ToString();
        }

        private void HandleStateChanged(GameState state)
        {
            if (announceTimer > 0f)
            {
                return;
            }

            if (messageLabel == null || hintLabel == null)
            {
                return;
            }

            switch (state)
            {
                case GameState.Ready:
                    messageLabel.text = "SPACESHIP";
                    builder.Length = 0;
                    builder.Append("DIFICULDADE:  < ");
                    builder.Append(run != null ? run.Settings.Label : "NORMAL");
                    builder.Append(" >   (setas esquerda e direita)\n");
                    builder.Append("ESPACO ou ENTER para comecar\n");
                    builder.Append("WASD move  -  ESPACO atira  -  Q slow motion  -  ESC pausa");
                    hintLabel.text = builder.ToString();
                    break;

                case GameState.Playing:
                    messageLabel.text = string.Empty;
                    hintLabel.text = string.Empty;
                    break;

                case GameState.Respawning:
                    messageLabel.text = "NAVE PERDIDA";
                    hintLabel.text = string.Empty;
                    break;

                case GameState.Paused:
                    messageLabel.text = "PAUSA";
                    hintLabel.text = "ESC ou ENTER para continuar";
                    break;

                case GameState.Shopping:

                    messageLabel.text = string.Empty;
                    hintLabel.text = string.Empty;
                    break;

                case GameState.GameOver:
                    messageLabel.text = "FIM DE JOGO";
                    hintLabel.text = "ESPACO ou R para jogar de novo";
                    break;
            }
        }

        private void HandleCreditsChanged(int credits)
        {
            if (creditsLabel == null)
            {
                return;
            }

            builder.Length = 0;
            builder.Append("CREDITOS  ").Append(credits.ToString("D5"));
            creditsLabel.text = builder.ToString();
        }

        private void HandleDifficultyChanged(DifficultyLevel level)
        {
            if (game != null && game.State == GameState.Ready)
            {
                HandleStateChanged(GameState.Ready);
            }
        }

        private void HandleWaveProgress(int wave, int kills, int quota)
        {
            if (waveLabel == null)
            {
                return;
            }

            builder.Length = 0;
            builder.Append("ONDA ").Append(wave);
            if (quota > 0)
            {
                builder.Append("   ").Append(kills).Append('/').Append(quota);
            }
            else
            {
                builder.Append("   CHEFE");
            }

            waveLabel.text = builder.ToString();
        }

        private void UpdateComboLabel()
        {
            if (comboLabel == null)
            {
                return;
            }

            ComboTracker combo = ComboTracker.Instance;
            if (combo == null || !combo.IsActive)
            {
                comboLabel.text = string.Empty;
                return;
            }

            builder.Length = 0;
            builder.Append("COMBO x").Append(combo.Multiplier.ToString("0.0"));
            builder.Append("   ").Append(combo.Chain);
            comboLabel.text = builder.ToString();

            Color color = accentColor;
            color.a = 0.35f + 0.65f * combo.WindowFraction;
            comboLabel.color = color;
        }

        private void UpdateDebuffLabel()
        {
            if (debuffLabel == null)
            {
                return;
            }

            if (boundPlayer == null || boundPlayer.ActiveDebuff == PlayerDebuff.None)
            {
                debuffLabel.text = string.Empty;
                return;
            }

            builder.Length = 0;
            builder.Append(DebuffLabelFor(boundPlayer.ActiveDebuff));
            builder.Append(' ').Append(boundPlayer.DebuffRemaining.ToString("0.0")).Append('s');
            debuffLabel.text = builder.ToString();
        }

        private static string DebuffLabelFor(PlayerDebuff value)
        {
            switch (value)
            {
                case PlayerDebuff.Jam:
                    return "ARMA TRAVADA";
                case PlayerDebuff.Drag:
                    return "MOTOR LENTO";
                case PlayerDebuff.Scramble:
                    return "CONTROLE INVERTIDO";
                default:
                    return string.Empty;
            }
        }

        private void HandlePlayerSpawned(PlayerShip player)
        {
            if (boundPlayer != null)
            {
                boundPlayer.PowerUpsChanged -= HandlePowerUpsChanged;
            }

            boundPlayer = player;
            if (boundPlayer != null)
            {
                boundPlayer.PowerUpsChanged += HandlePowerUpsChanged;
            }

            HandlePowerUpsChanged();
        }

        private void HandlePowerUpsChanged()
        {
            UpdatePowerUpLabel();
        }

        private void UpdatePowerUpLabel()
        {
            if (powerUpLabel == null)
            {
                return;
            }

            if (boundPlayer == null)
            {
                powerUpLabel.text = string.Empty;
                return;
            }

            builder.Length = 0;

            float triple = boundPlayer.TripleShotRemaining;
            if (triple > 0f)
            {
                builder.Append("TRIPLO ").Append(triple.ToString("0.0")).Append("s   ");
            }

            float rapid = boundPlayer.RapidFireRemaining;
            if (rapid > 0f)
            {
                builder.Append("CADENCIA ").Append(rapid.ToString("0.0")).Append("s   ");
            }

            if (boundPlayer.HasShield)
            {
                builder.Append("ESCUDO");
            }

            powerUpLabel.text = builder.ToString();
        }

        private void HandleBossSpawned(Boss boss)
        {
            if (boundBoss != null)
            {
                boundBoss.HealthChanged -= HandleBossHealthChanged;
            }

            boundBoss = boss;
            if (boundBoss == null)
            {
                return;
            }

            boundBoss.HealthChanged += HandleBossHealthChanged;

            SetBossBarVisible(true);
            HandleBossHealthChanged(boundBoss.HealthFraction);
        }

        private void HandleBossHealthChanged(float fraction)
        {
            if (bossBarFill != null)
            {
                bossBarFill.sizeDelta = new Vector2(BossBarWidth * Mathf.Clamp01(fraction),
                                                    bossBarFill.sizeDelta.y);
            }
        }

        private void SyncBossBar()
        {
            bool onScreen = game != null && game.BossActive;
            if (onScreen)
            {
                return;
            }

            if (boundBoss != null)
            {
                boundBoss.HealthChanged -= HandleBossHealthChanged;
                boundBoss = null;
            }

            if (bossBarRoot != null && bossBarRoot.gameObject.activeSelf)
            {
                SetBossBarVisible(false);
            }
        }

        private void SetBossBarVisible(bool visible)
        {
            if (bossBarRoot != null)
            {
                bossBarRoot.gameObject.SetActive(visible);
            }
            if (bossLabel != null)
            {
                bossLabel.gameObject.SetActive(visible);
            }
        }

        private void HandleAnnouncement(string reason)
        {
            announceText = reason;
            announceTimer = 1.6f;

            if (messageLabel != null)
            {
                messageLabel.text = announceText;
            }
            if (hintLabel != null)
            {
                hintLabel.text = string.Empty;
            }
        }

        private void BuildInterface()
        {
            canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(null, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Font font = ResolveBuiltinFont();
            Transform root = canvasObject.transform;

            BuildSlowMotionOverlays(root);

            scoreLabel = CreateLabel(root, "Score", font, statusFontSize, textColor,
                                     TextAnchor.UpperLeft,
                                     new Vector2(0f, 1f), new Vector2(0f, 1f),
                                     new Vector2(22f, -18f), new Vector2(340f, 40f));

            highScoreLabel = CreateLabel(root, "HighScore", font, statusFontSize, accentColor,
                                         TextAnchor.UpperCenter,
                                         new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                         new Vector2(0f, -18f), new Vector2(340f, 40f));

            livesLabel = CreateLabel(root, "Lives", font, statusFontSize, textColor,
                                     TextAnchor.UpperRight,
                                     new Vector2(1f, 1f), new Vector2(1f, 1f),
                                     new Vector2(-22f, -18f), new Vector2(340f, 40f));

            messageLabel = CreateLabel(root, "Message", font, messageFontSize, textColor,
                                       TextAnchor.MiddleCenter,
                                       new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                       new Vector2(0f, 40f), new Vector2(900f, 90f));

            hintLabel = CreateLabel(root, "Hint", font, hintFontSize, accentColor,
                                    TextAnchor.MiddleCenter,
                                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                    new Vector2(0f, -40f), new Vector2(900f, 70f));

            creditsLabel = CreateLabel(root, "Credits", font, hintFontSize, accentColor,
                                       TextAnchor.UpperLeft,
                                       new Vector2(0f, 1f), new Vector2(0f, 1f),
                                       new Vector2(22f, -50f), new Vector2(420f, 26f));

            powerUpLabel = CreateLabel(root, "PowerUps", font, hintFontSize, accentColor,
                                       TextAnchor.UpperLeft,
                                       new Vector2(0f, 1f), new Vector2(0f, 1f),
                                       new Vector2(22f, -76f), new Vector2(520f, 26f));

            debuffLabel = CreateLabel(root, "Debuff", font, hintFontSize,
                                      new Color(1f, 0.52f, 0.42f, 1f),
                                      TextAnchor.UpperLeft,
                                      new Vector2(0f, 1f), new Vector2(0f, 1f),
                                      new Vector2(22f, -102f), new Vector2(520f, 26f));

            waveLabel = CreateLabel(root, "Wave", font, hintFontSize, textColor,
                                    TextAnchor.UpperRight,
                                    new Vector2(1f, 1f), new Vector2(1f, 1f),
                                    new Vector2(-22f, -50f), new Vector2(420f, 26f));

            comboLabel = CreateLabel(root, "Combo", font, hintFontSize, accentColor,
                                     TextAnchor.UpperRight,
                                     new Vector2(1f, 1f), new Vector2(1f, 1f),
                                     new Vector2(-22f, -76f), new Vector2(420f, 26f));

            BuildWarpBar(root, font);
            BuildBossBar(root, font);
        }

        private void BuildBossBar(Transform root, Font font)
        {
            bossLabel = CreateLabel(root, "BossLabel", font, hintFontSize,
                                    new Color(1f, 0.45f, 0.42f, 1f),
                                    TextAnchor.UpperCenter,
                                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                    new Vector2(0f, -54f), new Vector2(520f, 26f));
            bossLabel.text = "CHEFE";

            GameObject barRoot = new GameObject("BossBar", typeof(RectTransform), typeof(Image));
            barRoot.transform.SetParent(root, false);

            bossBarRoot = barRoot.GetComponent<RectTransform>();
            bossBarRoot.anchorMin = new Vector2(0.5f, 1f);
            bossBarRoot.anchorMax = new Vector2(0.5f, 1f);
            bossBarRoot.pivot = new Vector2(0.5f, 1f);
            bossBarRoot.anchoredPosition = new Vector2(0f, -80f);
            bossBarRoot.sizeDelta = new Vector2(BossBarWidth, 12f);
            barRoot.GetComponent<Image>().color = new Color(0.16f, 0.05f, 0.08f, 0.8f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barRoot.transform, false);

            bossBarFill = fill.GetComponent<RectTransform>();
            bossBarFill.anchorMin = new Vector2(0f, 0f);
            bossBarFill.anchorMax = new Vector2(0f, 1f);
            bossBarFill.pivot = new Vector2(0f, 0.5f);
            bossBarFill.anchoredPosition = Vector2.zero;
            bossBarFill.sizeDelta = new Vector2(BossBarWidth, 0f);
            fill.GetComponent<Image>().color = new Color(0.95f, 0.33f, 0.30f, 1f);

            SetBossBarVisible(false);
        }

        private void BuildSlowMotionOverlays(Transform root)
        {
            tintOverlay = CreateFullScreenOverlay(root, "SlowMotionTint", null);
            vignetteOverlay = CreateFullScreenOverlay(root, "SlowMotionVignette", vignetteSprite);
        }

        private static Image CreateFullScreenOverlay(Transform parent, string name, Sprite sprite)
        {
            GameObject holder = new GameObject(name, typeof(RectTransform), typeof(Image));
            holder.transform.SetParent(parent, false);

            RectTransform rect = holder.GetComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = holder.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.clear;
            image.raycastTarget = false;

            return image;
        }

        private void BuildWarpBar(Transform root, Font font)
        {
            warpLabel = CreateLabel(root, "WarpLabel", font, hintFontSize, accentColor,
                                    TextAnchor.LowerCenter,
                                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                    new Vector2(0f, 54f), new Vector2(420f, 28f));

            GameObject barRoot = new GameObject("WarpBar", typeof(RectTransform), typeof(Image));
            barRoot.transform.SetParent(root, false);

            warpBarRoot = barRoot.GetComponent<RectTransform>();
            warpBarRoot.anchorMin = new Vector2(0.5f, 0f);
            warpBarRoot.anchorMax = new Vector2(0.5f, 0f);
            warpBarRoot.pivot = new Vector2(0.5f, 0f);
            warpBarRoot.anchoredPosition = new Vector2(0f, 34f);
            warpBarRoot.sizeDelta = new Vector2(WarpBarWidth, 10f);

            Image barBackground = barRoot.GetComponent<Image>();
            barBackground.color = new Color(0.08f, 0.14f, 0.22f, 0.75f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barRoot.transform, false);

            warpBarFill = fill.GetComponent<RectTransform>();

            warpBarFill.anchorMin = new Vector2(0f, 0f);
            warpBarFill.anchorMax = new Vector2(0f, 1f);
            warpBarFill.pivot = new Vector2(0f, 0.5f);
            warpBarFill.anchoredPosition = Vector2.zero;
            warpBarFill.sizeDelta = new Vector2(WarpBarWidth, 0f);

            fill.GetComponent<Image>().color = accentColor;

            warpBarRoot.gameObject.SetActive(false);
            warpLabel.gameObject.SetActive(false);
        }

        private static Font ResolveBuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        private static Text CreateLabel(Transform parent, string name, Font font, int fontSize,
                                        Color color, TextAnchor alignment,
                                        Vector2 anchorMin, Vector2 anchorMax,
                                        Vector2 anchoredPosition, Vector2 size)
        {
            GameObject holder = new GameObject(name, typeof(RectTransform), typeof(Text));
            holder.transform.SetParent(parent, false);

            RectTransform rect = holder.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = holder.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.text = string.Empty;

            return text;
        }
    }
}
