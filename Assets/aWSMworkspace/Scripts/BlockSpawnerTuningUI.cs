using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional top bar with a single "regenerate" button to reload blocks.json after the first stroke.
/// </summary>
[DisallowMultipleComponent]
public class BlockSpawnerTuningUI : MonoBehaviour
{
    public BlockSpawner blockSpawner;

    [Tooltip("If true, creates the regenerate button at Start.")]
    public bool buildUiAtRuntime = true;

    [Range(48f, 160f)]
    public float topBarHeightPixels = 72f;

    [Range(0f, 120f)]
    public float topBarOffsetFromTop = 36f;

    public Font uiFont;

    private Font _resolvedFont;

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

        RectTransform parentRt = GetComponent<RectTransform>();
        if (parentRt == null)
        {
            Debug.LogError("[BlockSpawnerTuningUI] Need RectTransform under Canvas.");
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        var bar = new GameObject("TopRegenerateBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(transform, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -topBarOffsetFromTop);
        barRt.sizeDelta = new Vector2(0f, topBarHeightPixels);

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
}
