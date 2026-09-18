using System.Text;
using SpaceShip.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceShip.UI
{
    [DisallowMultipleComponent]
    public class ShopController : MonoBehaviour
    {
        [Header("Aparencia")]
        [SerializeField] private Color panelColor = new Color(0.02f, 0.04f, 0.09f, 0.94f);
        [SerializeField] private Color titleColor = new Color(0.36f, 0.86f, 1f, 1f);
        [SerializeField] private Color textColor = new Color(0.86f, 0.92f, 1f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private Color affordableColor = new Color(0.55f, 1f, 0.70f, 1f);
        [SerializeField] private Color deniedColor = new Color(1f, 0.48f, 0.45f, 1f);
        [SerializeField] private Color maxedColor = new Color(0.55f, 0.60f, 0.70f, 1f);

        [Header("Tipografia")]
        [SerializeField, Min(8)] private int titleFontSize = 34;
        [SerializeField, Min(8)] private int rowFontSize = 20;
        [SerializeField, Min(8)] private int footerFontSize = 17;

        private GameObject canvasObject;
        private Text titleLabel;
        private Text creditsLabel;
        private Text footerLabel;
        private Text[] rows;

        private GameManager game;
        private RunState run;
        private int selected;
        private readonly StringBuilder builder = new StringBuilder(160);

        private void Start()
        {
            game = GameManager.Instance;
            run = RunState.Instance;

            BuildInterface();
            SetVisible(false);

            if (game != null)
            {
                game.StateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.StateChanged -= HandleStateChanged;
            }

            if (canvasObject != null)
            {
                Destroy(canvasObject);
                canvasObject = null;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            bool open = state == GameState.Shopping;
            SetVisible(open);

            if (open)
            {
                selected = 0;
                Refresh();
            }
        }

        private void Update()
        {
            if (game == null || game.State != GameState.Shopping)
            {
                return;
            }

            int step = PlayerInputReader.MenuVerticalStep;
            if (step != 0)
            {
                selected = (selected - step + ShopCatalog.Count) % ShopCatalog.Count;
                Refresh();
            }

            if (PlayerInputReader.ConfirmPressed)
            {
                TryBuy();
            }

            if (PlayerInputReader.PausePressed)
            {
                game.CloseShop();
            }
        }

        private void TryBuy()
        {
            if (run == null)
            {
                return;
            }

            if (run.TryPurchase(selected))
            {
                SfxLibrary.Instance?.PlayPickup();
            }
            else
            {
                SfxLibrary.Instance?.PlayEnemyHit();
            }

            Refresh();
        }

        private void Refresh()
        {
            if (rows == null || run == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = "LOJA";
            }

            if (creditsLabel != null)
            {
                builder.Length = 0;
                builder.Append("CREDITOS  ").Append(run.Credits.ToString("D5"));
                creditsLabel.text = builder.ToString();
            }

            for (int i = 0; i < rows.Length; i++)
            {
                ShopItem item = ShopCatalog.At(i);
                int level = run.LevelAt(i);
                int price = ShopCatalog.PriceFor(item, level);
                bool maxed = price < 0;
                bool affordable = !maxed && price <= run.Credits;

                builder.Length = 0;
                builder.Append(i == selected ? "> " : "  ");
                builder.Append('[').Append(item.Category).Append("] ");
                builder.Append(item.Name);
                builder.Append("  ").Append(level).Append('/').Append(item.MaxLevel);
                builder.Append("   ");
                builder.Append(maxed ? "MAXIMO" : price.ToString() + " cr");
                builder.Append("   ").Append(item.Description);

                rows[i].text = builder.ToString();
                rows[i].color = i == selected
                    ? selectedColor
                    : (maxed ? maxedColor : (affordable ? affordableColor : deniedColor));
            }

            if (footerLabel != null)
            {
                footerLabel.text =
                    "SETAS escolhem  -  ENTER ou ESPACO compra  -  ESC comeca a proxima onda";
            }
        }

        private void SetVisible(bool value)
        {
            if (canvasObject != null)
            {
                canvasObject.SetActive(value);
            }
        }

        private void BuildInterface()
        {
            canvasObject = new GameObject("Shop Canvas", typeof(Canvas), typeof(CanvasScaler));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasObject.transform;

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = panelColor;
            panelImage.raycastTarget = false;

            Font font = ResolveBuiltinFont();

            titleLabel = CreateLine(root, "Title", font, titleFontSize, titleColor,
                                    TextAnchor.UpperCenter, -46f, 900f);
            creditsLabel = CreateLine(root, "Credits", font, rowFontSize, affordableColor,
                                      TextAnchor.UpperCenter, -92f, 900f);

            rows = new Text[ShopCatalog.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = CreateLine(root, "Row" + i.ToString(), font, rowFontSize, textColor,
                                     TextAnchor.UpperLeft, -150f - i * 34f, 940f);
            }

            footerLabel = CreateLine(root, "Footer", font, footerFontSize, titleColor,
                                     TextAnchor.LowerCenter, -700f, 940f);
        }

        private static Text CreateLine(Transform parent, string name, Font font, int fontSize,
                                       Color color, TextAnchor alignment, float y, float width)
        {
            GameObject holder = new GameObject(name, typeof(RectTransform), typeof(Text));
            holder.transform.SetParent(parent, false);

            RectTransform rect = holder.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, 32f);

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

        private static Font ResolveBuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
