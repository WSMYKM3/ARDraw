using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARLine
{
    private int positionCount = 0;

    private Vector3 prevPointDistance = Vector3.zero;

    private LineRenderer LineRenderer { get; set; }

    private LineSettings settings;

    private readonly List<Vector3> strokePoints = new List<Vector3>();
    private Vector3 lastStationarySamplePos;
    private float lastMovementTime;
    private bool smoothedThisStroke;

    public ARLine(LineSettings settings)
    {
        this.settings = settings;
    }

    public void SampleStrokeInput(Vector3 world, float time)
    {
        if (LineRenderer == null)
            return;

        if (settings.enableHoldToSmooth)
        {
            if (Vector3.Distance(world, lastStationarySamplePos) > settings.stationaryWorldEpsilon)
            {
                lastStationarySamplePos = world;
                lastMovementTime = time;
            }
        }

        TryAddVertex(world);

        if (settings.enableHoldToSmooth && !smoothedThisStroke && strokePoints.Count >= settings.minPointsToSmooth)
        {
            if (time - lastMovementTime >= settings.smoothHoldSeconds)
                ApplySmoothing();
        }
    }

    void TryAddVertex(Vector3 position)
    {
        if (Vector3.Distance(prevPointDistance, position) >= settings.minDistanceBeforeNewPoint)
        {
            prevPointDistance = position;
            positionCount++;

            LineRenderer.positionCount = positionCount;
            LineRenderer.SetPosition(positionCount - 1, position);

            if (settings.enableHoldToSmooth)
                strokePoints.Add(position);

            if (!settings.enableHoldToSmooth
                && LineRenderer.positionCount % settings.applySimplifyAfterPoints == 0
                && settings.allowSimplification)
            {
                LineRenderer.Simplify(settings.tolerance);
            }
        }
    }

    void ApplySmoothing()
    {
        if (strokePoints.Count < settings.minPointsToSmooth)
            return;

        var simplified = new List<Vector3>();
        LineUtility.Simplify(strokePoints, settings.procreateSimplifyTolerance, simplified);

        if (simplified.Count < 2)
            return;

        var smooth = ResampleCatmullRom(simplified, Mathf.Max(2, settings.catmullSamplesPerSegment));

        LineRenderer.positionCount = smooth.Count;
        LineRenderer.SetPositions(smooth.ToArray());

        strokePoints.Clear();
        strokePoints.AddRange(smooth);
        positionCount = smooth.Count;
        prevPointDistance = strokePoints[strokePoints.Count - 1];
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

    public void AddNewLineRenderer(Transform parent, ARAnchor anchor, Vector3 position)
    {
        positionCount = 2;
        smoothedThisStroke = false;
        strokePoints.Clear();

        if (settings.enableHoldToSmooth)
        {
            strokePoints.Add(position);
            lastStationarySamplePos = position;
            lastMovementTime = Time.time;
        }

        prevPointDistance = position;

        GameObject go = new GameObject($"LineRenderer");

        go.transform.parent = anchor?.transform ?? parent;
        go.transform.position = position;
        go.tag = settings.lineTagName;

        LineRenderer goLineRenderer = go.AddComponent<LineRenderer>();
        goLineRenderer.startWidth = settings.startWidth;
        goLineRenderer.endWidth = settings.endWidth;

        goLineRenderer.startColor = settings.startColor;
        goLineRenderer.endColor = settings.endColor;

        goLineRenderer.material = settings.defaultMaterial;
        goLineRenderer.useWorldSpace = true;
        goLineRenderer.positionCount = positionCount;

        goLineRenderer.numCornerVertices = settings.cornerVertices;
        goLineRenderer.numCapVertices = settings.endCapVertices;

        goLineRenderer.SetPosition(0, position);
        goLineRenderer.SetPosition(1, position);

        LineRenderer = goLineRenderer;

        ARDebugManager.Instance.LogInfo($"New line renderer created");
    }
}
