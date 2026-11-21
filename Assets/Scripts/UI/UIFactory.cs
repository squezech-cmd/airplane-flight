// UIFactory.cs — процедурное построение UI (Canvas, панели, кнопки, надписи).
// Если TMP Essentials импортированы — используется TextMeshProUGUI, иначе legacy Text.
// Стиль: «бюджетная авиакомпания» — тёмные панели, оранжевые акценты.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Обёртка над надписью: единый API для TMP и legacy Text.</summary>
public class UiLabel
{
    public GameObject go;
    public Component textComponent;

    public void SetText(string text)
    {
        if (textComponent is TextMeshProUGUI tmp) tmp.text = text;
        else if (textComponent is Text leg) leg.text = text;
    }

    public void SetColor(Color c)
    {
        if (textComponent is TextMeshProUGUI tmp) tmp.color = c;
        else if (textComponent is Text leg) leg.color = c;
    }
}

/// <summary>Фабрика UI-элементов.</summary>
public static class UIFactory
{
    public static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.10f, 0.92f);
    public static readonly Color PanelLight = new Color(0.13f, 0.15f, 0.18f, 0.95f);
    public static readonly Color Accent = new Color(0.95f, 0.45f, 0.13f);
    public static readonly Color AccentHover = new Color(1.0f, 0.58f, 0.25f);
    public static readonly Color TextMain = new Color(0.94f, 0.95f, 0.96f);
    public static readonly Color TextDim = new Color(0.62f, 0.66f, 0.70f);

    private static EventSystem _eventSystem;

    /// <summary>Единая EventSystem на сцену (Input System UI-модуль).</summary>
    public static void EnsureEventSystem()
    {
        if (_eventSystem != null) return;
        var existing = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (existing != null) { _eventSystem = existing; return; }

        var go = new GameObject("EventSystem");
        _eventSystem = go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    /// <summary>Создать полноэкранный overlay-канвас.</summary>
    public static Canvas CreateCanvas(Transform parent, string name, int sortOrder = 100)
    {
        EnsureEventSystem();
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>Создать панель (тёмная, полупрозрачная).</summary>
    public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var img = go.AddComponent<Image>();
        img.color = color ?? PanelColor;
        return rect;
    }

    /// <summary>Простая панель по центру экрана заданного размера.</summary>
    public static RectTransform CreateCenterPanel(Transform parent, string name, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        var img = go.AddComponent<Image>();
        img.color = PanelColor;
        return rect;
    }

    /// <summary>Надпись (TMP при наличии Essentials, иначе legacy Text).</summary>
    public static UiLabel CreateLabel(Transform parent, string name, string text, int fontSize,
        TextAnchor align = TextAnchor.MiddleLeft, Color? color = null, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        UiLabel label = new UiLabel { go = go };
        if (ProceduralAssets.TmpReady)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color ?? TextMain;
            tmp.alignment = ToTmpAlignment(align);
            tmp.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.raycastTarget = false;
            label.textComponent = tmp;
        }
        else
        {
            var leg = go.AddComponent<Text>();
            leg.text = text;
            leg.font = ProceduralAssets.BuiltinFont;
            leg.fontSize = fontSize;
            leg.color = color ?? TextMain;
            leg.alignment = align;
            leg.fontStyle = style;
            leg.raycastTarget = false;
            label.textComponent = leg;
        }
        return label;
    }

    private static TextAlignmentOptions ToTmpAlignment(TextAnchor align) => align switch
    {
        TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
        TextAnchor.UpperCenter => TextAlignmentOptions.TopCenter,
        TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
        TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
        TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
        TextAnchor.MiddleRight => TextAlignmentOptions.Right,
        TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
        TextAnchor.LowerCenter => TextAlignmentOptions.BottomCenter,
        _ => TextAlignmentOptions.BottomRight,
    };

    /// <summary>Кнопка с текстом. Возвращает кнопку (подписка через onClick).</summary>
    public static Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPos,
        Vector2 size, int fontSize = 26, Action onClick = null, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = color ?? Accent;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        // Надпись на кнопке.
        var lbl = CreateLabel(rect, "Text", text, fontSize, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        lbl.go.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return btn;
    }

    /// <summary>Степпер «[-] Значение [+]» для настроек. Возвращает функции обновления.</summary>
    public static (RectTransform root, Action<int> apply, Func<int> get, Action refresh) CreateStepper(
        Transform parent, string name, string title, string[] options, int initial, Action<int> onChange)
    {
        var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(0f, 54f);

        int value = initial;
        var titleLbl = CreateLabel(root, "Title", title, 24, TextAnchor.MiddleLeft, TextDim);
        titleLbl.go.GetComponent<RectTransform>().offsetMin = new Vector2(16f, 0f);
        titleLbl.go.GetComponent<RectTransform>().offsetMax = new Vector2(-360f, 0f);

        var valueLbl = CreateLabel(root, "Value", options[value], 24, TextAnchor.MiddleCenter, TextMain, FontStyle.Bold);
        var valueRect = valueLbl.go.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.anchoredPosition = new Vector2(-190f, 0f);
        valueRect.sizeDelta = new Vector2(300f, 0f);

        void Refresh() => valueLbl.SetText(options[Mathf.Clamp(value, 0, options.Length - 1)]);
        void Change(int delta)
        {
            int count = options.Length;
            value = ((value + delta) % count + count) % count;
            Refresh();
            onChange?.Invoke(value);
        }

        CreateButton(root, "Minus", "<", new Vector2(-370f, 0f), new Vector2(52f, 44f), 28, () => Change(-1),
            new Color(0.2f, 0.22f, 0.26f));
        CreateButton(root, "Plus", ">", new Vector2(-10f, 0f), new Vector2(52f, 44f), 28, () => Change(+1),
            new Color(0.2f, 0.22f, 0.26f));

        Refresh();
        return (root, _ => { }, () => value, Refresh);
    }

    /// <summary>Полоса-индикатор (топливо, настроение салона).</summary>
    public static (Image fill, RectTransform rect) CreateBar(Transform parent, string name, Vector2 anchoredPos,
        Vector2 size, Color fillColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(rect, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
        var fill = fillGo.AddComponent<Image>();
        fill.color = fillColor;

        return (fill, rect);
    }

    /// <summary>Картинка-оверлей (фон меню, трещины стекла).</summary>
    public static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchoredPos,
        Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
