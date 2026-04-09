using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Left: vertical scale slider. Right: Draw toggle.
/// </summary>
[DisallowMultipleComponent]
public class BlockSpawnerTuningUI : MonoBehaviour
{
    public BlockSpawner blockSpawner;

    [Tooltip("If true, creates UI at Start.")]
    public bool buildUiAtRuntime = true;

    public Font uiFont;

    [Header("Scale slider")]
    [Tooltip("Initial uniform scale for SpawnedBlocksRoot when the UI builds (also the slider’s starting value).")]
    [Range(0.05f, 4f)]
    public float defaultGroupScale = 0.3f;

    private Font _resolvedFont;
    private Slider _scale;
    private Image _drawButtonImage;

    static readonly Color DrawEnabledGreen = new Color(0.18f, 0.62f, 0.32f, 1f);
    static readonly Color DrawDisabledGrey = new Color(0.42f, 0.43f, 0.45f, 1f);

    const float ScaleMin = 0.05f;
    const float ScaleMax = 4f;

    const float LeftStripWidthPx = 112f;
    const float RightDrawWidthPx = 88f;
    const float EdgeInsetPx = 12f;
    /// <summary>Vertical span for left/right strips (0–1), avoids overlap with bottom UI.</summary>
    const float StripAnchorYMin = 0.1f;
    const float StripAnchorYMax = 0.82f;

    void Start()
    {
        if (blockSpawner == null)
            blockSpawner = FindObjectOfType<BlockSpawner>();

        if (blockSpawner == null)
        {
            Debug.LogError("[BlockSpawnerTuningUI] No BlockSpawner.");
            return;
        }

        if (buildUiAtRuntime)
            BuildUi();

        StartCoroutine(CoSyncDrawButtonAfterFrame());
    }

    IEnumerator CoSyncDrawButtonAfterFrame()
    {
        yield return null;
        UpdateDrawButtonVisual();
    }

    public void BuildUi()
    {
        _resolvedFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (GetComponent<RectTransform>() == null)
        {
            Debug.LogError("[BlockSpawnerTuningUI] Need RectTransform under Canvas.");
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        BuildLeftVerticalScale();
        BuildDrawButtonRight();
    }

    void BuildDrawButtonRight()
    {
        var drawGo = new GameObject("DrawButton", typeof(RectTransform), typeof(Image), typeof(Button));
        drawGo.transform.SetParent(transform, false);
        var btnRt = drawGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, StripAnchorYMin);
        btnRt.anchorMax = new Vector2(1f, StripAnchorYMax);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-EdgeInsetPx, 0f);
        btnRt.sizeDelta = new Vector2(RightDrawWidthPx, 0f);

        var drawImg = drawGo.GetComponent<Image>();
        _drawButtonImage = drawImg;
        drawImg.raycastTarget = true;
        var drawBtn = drawGo.GetComponent<Button>();
        drawBtn.targetGraphic = drawImg;
        AddButtonText(drawGo.transform, "Draw");
        drawBtn.onClick.AddListener(ToggleDraw);

        UpdateDrawButtonVisual();
    }

    void BuildLeftVerticalScale()
    {
        var column = new GameObject("ScaleColumn", typeof(RectTransform));
        column.transform.SetParent(transform, false);
        var colRt = column.GetComponent<RectTransform>();
        colRt.anchorMin = new Vector2(0f, StripAnchorYMin);
        colRt.anchorMax = new Vector2(0f, StripAnchorYMax);
        colRt.pivot = new Vector2(0f, 0.5f);
        colRt.anchoredPosition = new Vector2(EdgeInsetPx, 0f);
        colRt.sizeDelta = new Vector2(LeftStripWidthPx, 0f);

        var vlg = column.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = true;

        var title = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
        title.transform.SetParent(column.transform, false);
        title.GetComponent<LayoutElement>().minHeight = 22f;
        var tt = title.AddComponent<Text>();
        tt.font = _resolvedFont;
        tt.fontSize = 16;
        tt.fontStyle = FontStyle.Bold;
        tt.color = new Color(0.9f, 0.92f, 0.96f);
        tt.text = "Scale";
        tt.alignment = TextAnchor.MiddleCenter;

        float startScale = Mathf.Clamp(defaultGroupScale, ScaleMin, ScaleMax);
        _scale = AddVerticalScaleSlider(column.transform, startScale, _ => ApplyScale());

        ApplyScale();
    }

    void ApplyScale()
    {
        if (blockSpawner == null || blockSpawner.SpawnedBlocksRoot == null || _scale == null)
            return;

        float u = _scale.value;
        blockSpawner.SpawnedBlocksRoot.localScale = new Vector3(u, u, u);
    }

    /// <summary>Vertical slider (BottomToTop), no extra panel — column has no Image.</summary>
    Slider AddVerticalScaleSlider(Transform parent, float start, Action<float> onChanged)
    {
        var valueGo = new GameObject("ScaleValue", typeof(RectTransform), typeof(LayoutElement));
        valueGo.transform.SetParent(parent, false);
        valueGo.GetComponent<LayoutElement>().minHeight = 22f;
        var valText = valueGo.AddComponent<Text>();
        valText.font = _resolvedFont;
        valText.fontSize = 17;
        valText.color = new Color(0.75f, 0.92f, 1f);
        valText.alignment = TextAnchor.MiddleCenter;

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderGo.transform.SetParent(parent, false);
        var sliderLe = sliderGo.GetComponent<LayoutElement>();
        sliderLe.minWidth = 56f;
        sliderLe.flexibleHeight = 1f;

        var sl = sliderGo.GetComponent<Slider>();
        sl.minValue = ScaleMin;
        sl.maxValue = ScaleMax;
        sl.wholeNumbers = false;
        sl.value = start;
        sl.direction = Slider.Direction.BottomToTop;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0f);
        bgRt.anchorMax = new Vector2(0.5f, 1f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(22f, 0f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        // Thin track only (no panel); keep slight alpha so the slider receives raycasts.
        bg.GetComponent<Image>().color = new Color(0.35f, 0.36f, 0.4f, 0.35f);

        var fill = new GameObject("Fill Area", typeof(RectTransform));
        fill.transform.SetParent(sliderGo.transform, false);
        var fillArea = fill.GetComponent<RectTransform>();
        fillArea.anchorMin = new Vector2(0.5f, 0f);
        fillArea.anchorMax = new Vector2(0.5f, 1f);
        fillArea.pivot = new Vector2(0.5f, 0.5f);
        fillArea.sizeDelta = new Vector2(22f, 0f);
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        var fillImg = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillImg.transform.SetParent(fill.transform, false);
        var fillRt = fillImg.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 0f);
        fillRt.pivot = new Vector2(0.5f, 0f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;
        fillImg.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.95f, 1f);

        var handle = new GameObject("Handle Slide Area", typeof(RectTransform));
        handle.transform.SetParent(sliderGo.transform, false);
        var handleArea = handle.GetComponent<RectTransform>();
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = Vector2.zero;
        handleArea.offsetMax = Vector2.zero;

        var handleKnob = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleKnob.transform.SetParent(handle.transform, false);
        var hkRt = handleKnob.GetComponent<RectTransform>();
        hkRt.anchorMin = new Vector2(0f, 0f);
        hkRt.anchorMax = new Vector2(1f, 0f);
        hkRt.pivot = new Vector2(0.5f, 0.5f);
        hkRt.sizeDelta = new Vector2(0f, 28f);
        handleKnob.GetComponent<Image>().color = Color.white;

        sl.fillRect = fillRt;
        sl.targetGraphic = handleKnob.GetComponent<Image>();
        sl.handleRect = hkRt;

        void UpdateVal(float v)
        {
            valText.text = v.ToString("0.###");
            onChanged?.Invoke(v);
        }

        sl.onValueChanged.AddListener(UpdateVal);
        UpdateVal(sl.value);

        return sl;
    }

    void AddButtonText(Transform parent, string caption)
    {
        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(parent, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = _resolvedFont;
        txt.fontSize = 24;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = caption;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    void ToggleDraw()
    {
        var m = ARDrawManager.Instance;
        if (m == null)
        {
            Debug.LogWarning("[BlockSpawnerTuningUI] ARDrawManager not found.");
            return;
        }

        m.AllowDraw(!m.IsDrawAllowed);
        UpdateDrawButtonVisual();
    }

    void UpdateDrawButtonVisual()
    {
        if (_drawButtonImage == null)
            return;

        var m = ARDrawManager.Instance;
        bool on = m != null && m.IsDrawAllowed;
        _drawButtonImage.color = on ? DrawEnabledGreen : DrawDisabledGrey;
    }
}
