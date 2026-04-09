using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Regenerate bar + one slider for uniform scale on SpawnedBlocksRoot.
/// </summary>
[DisallowMultipleComponent]
public class BlockSpawnerTuningUI : MonoBehaviour
{
    public BlockSpawner blockSpawner;

    [Tooltip("If true, creates UI at Start.")]
    public bool buildUiAtRuntime = true;

    public Font uiFont;

    private Font _resolvedFont;
    private Slider _scale;

    const float TopBarHeightPx = 72f;
    const float TopBarOffsetPx = 36f;
    const float BottomPanelHeightFraction = 0.32f;
    const float ScaleMin = 0.05f;
    const float ScaleMax = 4f;

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

        BuildBottomScalePanel();
        BuildTopRegenerateBar();
    }

    void BuildTopRegenerateBar()
    {
        var bar = new GameObject("TopRegenerateBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(transform, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -TopBarOffsetPx);
        barRt.sizeDelta = new Vector2(0f, TopBarHeightPx);

        var barImg = bar.GetComponent<Image>();
        barImg.color = new Color(0.05f, 0.06f, 0.1f, 0.96f);
        barImg.raycastTarget = true;

        var btnGo = new GameObject("RegenerateButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(bar.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0f, 0f);
        btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.offsetMin = new Vector2(16f, 10f);
        btnRt.offsetMax = new Vector2(-16f, -10f);

        var btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.15f, 0.48f, 0.95f, 1f);
        btnImg.raycastTarget = true;
        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = btnImg;

        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(btnGo.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = _resolvedFont;
        txt.fontSize = 24;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = "regenerate";
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        btn.onClick.AddListener(() =>
        {
            if (blockSpawner != null)
                blockSpawner.RespawnBlocks();
        });
    }

    void BuildBottomScalePanel()
    {
        var panel = new GameObject("ScalePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(1f, BottomPanelHeightFraction);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
        panel.GetComponent<Image>().raycastTarget = true;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(panel.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(16f, 14f);
        contentRt.offsetMax = new Vector2(-16f, -14f);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;

        var title = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
        title.transform.SetParent(content.transform, false);
        title.GetComponent<LayoutElement>().minHeight = 26f;
        var tt = title.AddComponent<Text>();
        tt.font = _resolvedFont;
        tt.fontSize = 20;
        tt.fontStyle = FontStyle.Bold;
        tt.color = new Color(0.85f, 0.88f, 0.95f);
        tt.text = "Scale (group)";
        tt.alignment = TextAnchor.MiddleLeft;

        _scale = AddSliderRow(content.transform, "×", ScaleMin, ScaleMax, 1f, _ => ApplyScale());

        SyncScaleFromRoot();
    }

    void ApplyScale()
    {
        if (blockSpawner == null || blockSpawner.SpawnedBlocksRoot == null || _scale == null)
            return;

        float u = _scale.value;
        blockSpawner.SpawnedBlocksRoot.localScale = new Vector3(u, u, u);
    }

    void SyncScaleFromRoot()
    {
        if (blockSpawner == null || blockSpawner.SpawnedBlocksRoot == null || _scale == null)
            return;

        Vector3 s = blockSpawner.SpawnedBlocksRoot.localScale;
        float u = (s.x + s.y + s.z) / 3f;
        if (u < ScaleMin) u = ScaleMin;
        if (u > ScaleMax) u = ScaleMax;
        _scale.SetValueWithoutNotify(u);
    }

    Slider AddSliderRow(Transform parent, string label, float min, float max, float start, Action<float> onChanged)
    {
        var row = new GameObject("Row_Scale", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().minHeight = 64f;
        row.GetComponent<LayoutElement>().preferredHeight = 64f;

        var hl = row.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 12f;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childForceExpandWidth = false;
        hl.childControlHeight = true;
        hl.childForceExpandHeight = false;

        var labGo = new GameObject("Lab", typeof(RectTransform), typeof(LayoutElement));
        labGo.transform.SetParent(row.transform, false);
        labGo.GetComponent<LayoutElement>().minWidth = 36f;
        labGo.GetComponent<LayoutElement>().preferredWidth = 36f;
        var lab = labGo.AddComponent<Text>();
        lab.font = _resolvedFont;
        lab.fontSize = 20;
        lab.color = Color.white;
        lab.text = label;
        lab.alignment = TextAnchor.MiddleLeft;

        var valGo = new GameObject("Val", typeof(RectTransform), typeof(LayoutElement));
        valGo.transform.SetParent(row.transform, false);
        valGo.GetComponent<LayoutElement>().minWidth = 64f;
        valGo.GetComponent<LayoutElement>().preferredWidth = 64f;
        var valText = valGo.AddComponent<Text>();
        valText.font = _resolvedFont;
        valText.fontSize = 18;
        valText.color = new Color(0.7f, 0.9f, 1f);
        valText.alignment = TextAnchor.MiddleRight;

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderGo.transform.SetParent(row.transform, false);
        var sliderLe = sliderGo.GetComponent<LayoutElement>();
        sliderLe.minWidth = 160f;
        sliderLe.minHeight = 56f;
        sliderLe.preferredHeight = 56f;
        sliderLe.flexibleWidth = 1f;

        var sl = sliderGo.GetComponent<Slider>();
        sl.minValue = min;
        sl.maxValue = max;
        sl.wholeNumbers = false;
        sl.value = start;
        sl.direction = Slider.Direction.LeftToRight;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.12f);
        bgRt.anchorMax = new Vector2(1f, 0.88f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.15f, 0.16f, 0.2f);

        var fill = new GameObject("Fill Area", typeof(RectTransform));
        fill.transform.SetParent(sliderGo.transform, false);
        var fillArea = fill.GetComponent<RectTransform>();
        fillArea.anchorMin = new Vector2(0f, 0.12f);
        fillArea.anchorMax = new Vector2(1f, 0.88f);
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        var fillImg = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillImg.transform.SetParent(fill.transform, false);
        var fillRt = fillImg.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;
        fillImg.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.95f);

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
        hkRt.anchorMax = new Vector2(0f, 1f);
        hkRt.pivot = new Vector2(0.5f, 0.5f);
        hkRt.sizeDelta = new Vector2(36f, 0f);
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
}
