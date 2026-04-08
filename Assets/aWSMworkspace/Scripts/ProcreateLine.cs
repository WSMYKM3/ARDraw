using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Non-AR mouse test: drag to draw a LineRenderer stroke; keep the button down and stay still
/// for <see cref="smoothHoldSeconds"/> to smooth the stroke. Release earlier to keep the raw polyline.
/// </summary>
public class ProcreateLine : MonoBehaviour
{
    [SerializeField] private Camera drawCamera;
    [SerializeField] private float distanceFromCamera = 10f;

    [Header("Line appearance")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float startWidth = 0.05f;
    [SerializeField] private float endWidth = 0.05f;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = Color.white;
    [SerializeField] private int cornerVertices = 3;
    [SerializeField] private int capVertices = 3;

    [Header("Sampling")]
    [SerializeField] private float minDistanceNewPoint = 0.02f;

    [Header("Smooth on hold (stationary)")]
    [SerializeField] private float smoothHoldSeconds = 1f;
    [SerializeField] private float stationaryWorldEpsilon = 0.005f;
    [Tooltip("RDP tolerance for LineUtility.Simplify before spline resample")]
    [SerializeField] private float simplifyTolerance = 0.02f;
    [SerializeField] private int catmullSamplesPerSegment = 8;
    [SerializeField] private int minPointsToSmooth = 3;

    private LineRenderer currentLine;
    private readonly List<Vector3> strokePoints = new List<Vector3>();
    private Vector3 lastStationarySamplePos;
    private float lastMovementTime;
    private bool smoothedThisStroke;
    private bool isDrawing;

    void Reset()
    {
        drawCamera = Camera.main;
    }

    void Update()
    {
        Camera cam = drawCamera != null ? drawCamera : Camera.main;
        if (cam == null)
            return;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceFromCamera));

        if (Input.GetMouseButtonDown(0))
            BeginStroke(world);
        else if (Input.GetMouseButton(0) && isDrawing)
            ContinueStroke(world);
        else if (Input.GetMouseButtonUp(0) && isDrawing)
            EndStroke();

        if (isDrawing && Input.GetMouseButton(0) && !smoothedThisStroke && strokePoints.Count >= minPointsToSmooth)
        {
            if (Time.time - lastMovementTime >= smoothHoldSeconds)
                ApplySmoothing();
        }
    }

    void BeginStroke(Vector3 world)
    {
        isDrawing = true;
        smoothedThisStroke = false;
        strokePoints.Clear();

        var go = new GameObject("Stroke");
        go.transform.SetParent(transform, worldPositionStays: true);

        currentLine = go.AddComponent<LineRenderer>();
        currentLine.useWorldSpace = true;
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, world);
        currentLine.startWidth = startWidth;
        currentLine.endWidth = endWidth;
        currentLine.startColor = startColor;
        currentLine.endColor = endColor;
        currentLine.numCornerVertices = cornerVertices;
        currentLine.numCapVertices = capVertices;
        currentLine.material = lineMaterial != null
            ? lineMaterial
            : new Material(Shader.Find("Sprites/Default"));

        strokePoints.Add(world);
        lastStationarySamplePos = world;
        lastMovementTime = Time.time;
    }

    void ContinueStroke(Vector3 world)
    {
        if (Vector3.Distance(world, lastStationarySamplePos) > stationaryWorldEpsilon)
        {
            lastStationarySamplePos = world;
            lastMovementTime = Time.time;
        }

        Vector3 latest = strokePoints[strokePoints.Count - 1];
        if (Vector3.Distance(latest, world) < minDistanceNewPoint)
            return;

        strokePoints.Add(world);
        int c = strokePoints.Count;
        currentLine.positionCount = c;
        currentLine.SetPosition(c - 1, world);
    }

    void EndStroke()
    {
        isDrawing = false;
        currentLine = null;
        strokePoints.Clear();
    }

    void ApplySmoothing()
    {
        if (strokePoints.Count < minPointsToSmooth)
            return;

        var simplified = new List<Vector3>();
        LineUtility.Simplify(strokePoints, simplifyTolerance, simplified);

        if (simplified.Count < 2)
            return;

        var smooth = ResampleCatmullRom(simplified, Mathf.Max(2, catmullSamplesPerSegment));

        currentLine.positionCount = smooth.Count;
        currentLine.SetPositions(smooth.ToArray());
        strokePoints.Clear();
        strokePoints.AddRange(smooth);
        smoothedThisStroke = true;
    }

    static List<Vector3> ResampleCatmullRom(IReadOnlyList<Vector3> pts, int samplesPerSegment)
    {
        var output = new List<Vector3>(pts.Count * samplesPerSegment + 4);
        int n = pts.Count;

        for (int i = 0; i < n - 1; i++)
        {
            Vector3 p0 = pts[Mathf.Max(0, i - 1)];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = pts[Mathf.Min(n - 1, i + 2)];

            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                output.Add(CatmullRom(t, p0, p1, p2, p3));
            }
        }

        output.Add(pts[n - 1]);
        return output;
    }

    static Vector3 CatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
