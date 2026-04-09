using System.Collections.Generic;
using DilmerGames.Core.Singletons;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>Fires once per session with the world position of the first stroke start (touch or editor mouse).</summary>
[System.Serializable]
public class FirstStrokeWorldOriginEvent : UnityEvent<Vector3> { }

[RequireComponent(typeof(ARAnchorManager))]
public class ARDrawManager : Singleton<ARDrawManager>
{
    [SerializeField]
    private LineSettings lineSettings = null;

    [SerializeField]
    private UnityEvent OnDraw = null;

    [Tooltip("Invoked once when the first line begins, with the world position of that stroke start.")]
    [SerializeField]
    private FirstStrokeWorldOriginEvent onFirstStrokeWorldOrigin = null;

    [SerializeField]
    private ARAnchorManager anchorManager = null;

    [SerializeField] 
    private Camera arCamera = null;

    private bool _firstStrokeOriginInvoked;

    /// <summary>True after <see cref="onFirstStrokeWorldOrigin"/> has been invoked for this session.</summary>
    public bool HasRecordedFirstStrokeOrigin => _firstStrokeOriginInvoked;

    private List<ARAnchor> anchors = new List<ARAnchor>();

    private Dictionary<int, ARLine> Lines = new Dictionary<int, ARLine>();

    private readonly Dictionary<int, List<Vector3>> pendingPointsWhileAnchoring = new Dictionary<int, List<Vector3>>();

    private readonly HashSet<int> activeTouchFingers = new HashSet<int>();

    private bool CanDraw { get; set; }

    void Update ()
    {
        #if !UNITY_EDITOR    
        DrawOnTouch();
        #else
        DrawOnMouse();
        #endif
	}

    public void AllowDraw(bool isAllow)
    {
        CanDraw = isAllow;
    }


    void DrawOnTouch()
    {
        if(!CanDraw) return;

        int tapCount = Input.touchCount > 1 && lineSettings.allowMultiTouch ? Input.touchCount : 1;

        for(int i = 0; i < tapCount; i++)
        {
            if (i >= Input.touchCount) break;
            Touch touch = Input.GetTouch(i);
            Vector3 touchPosition = arCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, lineSettings.distanceFromCamera));
            
            ARDebugManager.Instance.LogInfo($"{touch.fingerId}");

            if(touch.phase == TouchPhase.Began)
            {
                OnDraw?.Invoke();

                if (!_firstStrokeOriginInvoked)
                {
                    _firstStrokeOriginInvoked = true;
                    onFirstStrokeWorldOrigin?.Invoke(touchPosition);
                }

                activeTouchFingers.Add(touch.fingerId);
                pendingPointsWhileAnchoring[touch.fingerId] = new List<Vector3> { touchPosition };
                _ = TryStartLineWithAnchorAsync(touch.fingerId, touchPosition);
            }
            else if(touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (Lines.TryGetValue(touch.fingerId, out ARLine line))
                    line.SampleStrokeInput(touchPosition, Time.time);
                else if (pendingPointsWhileAnchoring.TryGetValue(touch.fingerId, out List<Vector3> buffer))
                    buffer.Add(touchPosition);
            }
            else if(touch.phase == TouchPhase.Ended)
            {
                if (Lines.TryGetValue(touch.fingerId, out ARLine line))
                    StrokeUploadManager.Instance.TryUploadStroke(touch.fingerId, line);

                activeTouchFingers.Remove(touch.fingerId);
                pendingPointsWhileAnchoring.Remove(touch.fingerId);
                Lines.Remove(touch.fingerId);
            }
        }
    }

    async Awaitable TryStartLineWithAnchorAsync(int fingerId, Vector3 touchPosition)
    {
        var result = await anchorManager.TryAddAnchorAsync(new Pose(touchPosition, Quaternion.identity));
        if (!result.status.IsSuccess())
        {
            Debug.LogError("Error creating reference point");
            pendingPointsWhileAnchoring.Remove(fingerId);
            activeTouchFingers.Remove(fingerId);
            return;
        }

        if (!activeTouchFingers.Contains(fingerId))
        {
            pendingPointsWhileAnchoring.Remove(fingerId);
            return;
        }

        ARAnchor anchor = result.value;
        anchors.Add(anchor);
        ARDebugManager.Instance.LogInfo($"Anchor created & total of {anchors.Count} anchor(s)");

        ARLine line = new ARLine(lineSettings);
        Lines.Add(fingerId, line);
        line.AddNewLineRenderer(transform, anchor, touchPosition);

        if (pendingPointsWhileAnchoring.TryGetValue(fingerId, out List<Vector3> buffered))
        {
            foreach (Vector3 p in buffered)
                line.SampleStrokeInput(p, Time.time);
            pendingPointsWhileAnchoring.Remove(fingerId);
        }
    }

    void DrawOnMouse()
    {
        if(!CanDraw) return;

        Vector3 mousePosition = arCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, lineSettings.distanceFromCamera));

        if(Input.GetMouseButton(0))
        {
            OnDraw?.Invoke();

            if (Input.GetMouseButtonDown(0))
            {
                ARDebugManager.Instance.LogInfo("Editor draw stroke began (mouse)");
                if (!_firstStrokeOriginInvoked)
                {
                    _firstStrokeOriginInvoked = true;
                    onFirstStrokeWorldOrigin?.Invoke(mousePosition);
                }
            }

            if(!Lines.ContainsKey(0))
            {
                ARLine line = new ARLine(lineSettings);
                Lines.Add(0, line);
                line.AddNewLineRenderer(transform, null, mousePosition);
            }

            Lines[0].SampleStrokeInput(mousePosition, Time.time);
        }
        else if(Input.GetMouseButtonUp(0))
        {
            if (Lines.TryGetValue(0, out ARLine line))
                StrokeUploadManager.Instance.TryUploadStroke(0, line);
            Lines.Remove(0);
        }
    }

    GameObject[] GetAllLinesInScene()
    {
        return GameObject.FindGameObjectsWithTag("Line");
    }

    public void ClearLines()
    {
        GameObject[] lines = GetAllLinesInScene();
        foreach (GameObject currentLine in lines)
        {
            LineRenderer line = currentLine.GetComponent<LineRenderer>();
            Destroy(currentLine);
        }
    }
}