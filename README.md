forked from dilmerv/ARDraw ro make a demo for guide AI camera movement of video generation

# ARDraw

Unity **AR Foundation** app for drawing 3D strokes in augmented reality. This repo extends **[dilmerv/ARDraw](https://github.com/dilmerv/ARDraw)** for experiments such as **recording stroke geometry** (world-space polylines) for downstream tooling—for example guiding AI camera motion or video generation pipelines.

## Requirements

- **Unity Editor:** `6000.0.70f1` (Unity 6) — see `ProjectSettings/ProjectVersion.txt`
- **Targets:** iOS (ARKit) and/or Android (ARCore), per AR Foundation setup
- **Python3** (optional): for local stroke and debug log servers under `tools/`

## Features

- Multi-touch / editor mouse drawing with `LineRenderer`-based strokes anchored in AR space (`ARDrawManager`, `ARLine`).
- Optional **HTTP upload** of finished strokes as JSON (`StrokeUploadManager`) to a small local server.
- **Workspace scene** with JSON-driven prefab spawning after the first stroke anchors the world (`BlockSpawner` + `StreamingAssets` JSON such as `blocks_cuo.json`).
- Optional **remote debug logging** over HTTP (`ARDebugManager` + `debug_server.py`).

## Repository layout

| Path | Purpose |
|------|---------|
| `Assets/Scenes/ARDraw.unity` | Original-style AR draw scene |
| `Assets/aWSMworkspace/` | Extended workspace scenes, jelly FBX assets, `BlockSpawner`, etc. |
| `Assets/StreamingAssets/` | Block layout JSON consumed at runtime |
| `tools/debug_log_server/stroke_server.py` | Receives `POST /stroke`, appends JSONL, archives sessions |
| `tools/debug_log_server/debug_server.py` | Receives `POST /log` for terminal + file logging |
| `tools/strokesHistory/` | Archived per-session stroke JSONL (created by `stroke_server.py`) |

## Quick start (Unity)

1. Clone the repo and open the project folder in **Unity 6** (`6000.0.70f1` recommended).
2. Install platform modules (iOS / Android) and configure **XR Plug-in Management** / AR Foundation as usual for your device.
3. Open the scene you want from **File → Open Scene**. Build settings may point at a specific workspace scene; check **File → Build Settings** for the currently enabled scenes.
4. On device, allow camera / tracking permissions required by ARKit or ARCore.

## Stroke capture (local server)

From the repo root:

```bash
cd tools/debug_log_server
python3 stroke_server.py --port 8081
```

The server listens for strokes at `http://<host>:8081/stroke`. In Unity, enable stroke upload on `StrokeUploadManager` and set the URL (e.g. `http://127.0.0.1:8081/stroke` in the Editor; on a phone, use your development machine’s **LAN IP**). Live strokes are written to `tools/debug_log_server/strokes.jsonl`; when the session changes, they are moved under `tools/strokesHistory/`.

Optional debug log relay (separate port):

```bash
python3 debug_server.py --port 8080
```

## Main dependencies (packages)

Defined in `Packages/manifest.json`, including:

- `com.unity.xr.arfoundation` / `com.unity.xr.arkit` / `com.unity.xr.arcore`
- `com.unity.render-pipelines.universal` (URP)

## Credits

- Based on **[dilmerv/ARDraw](https://github.com/dilmerv/ARDraw)**.
- This fork adds workspace tooling, stroke export, block spawning from JSON, and related utilities.

## License

If the upstream project specifies a license, retain it alongside any original notices. Add or adjust a `LICENSE` file in this repo to match how you distribute your changes.

