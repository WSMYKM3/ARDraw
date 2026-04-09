#!/usr/bin/env python3
"""
Receives finished drawing strokes from the Unity AR app (world-space polyline).
Separate from debug_server.py — run both if you need logs + strokes.

Usage:
    python3 stroke_server.py [--port 8081]

Unity should POST JSON to http://<this-machine-ip>:<port>/stroke
(Editor on same Mac: http://127.0.0.1:8081/stroke — iPhone: use Mac LAN IP.)
"""

import argparse
import json
import os
import socket
from datetime import datetime
from http.server import HTTPServer, BaseHTTPRequestHandler

RESET = "\033[0m"
BOLD = "\033[1m"

STROKE_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "strokes.jsonl")


def get_local_ip():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return "127.0.0.1"


def validate_points(data):
    """Return list of [x,y,z] floats or None."""
    pts = data.get("points")
    if not isinstance(pts, list) or len(pts) < 1:
        return None
    out = []
    for p in pts:
        if not isinstance(p, list) or len(p) != 3:
            return None
        try:
            out.append([float(p[0]), float(p[1]), float(p[2])])
        except (TypeError, ValueError):
            return None
    return out


class StrokeHandler(BaseHTTPRequestHandler):

    def do_POST(self):
        if self.path == "/stroke":
            self._handle_stroke()
        else:
            self.send_error(404, "Not Found")

    def do_GET(self):
        if self.path == "/health":
            self._send_json(200, {"status": "running", "service": "stroke"})
        else:
            self.send_error(404, "Not Found")

    def _handle_stroke(self):
        try:
            content_length = int(self.headers.get("Content-Length", 0))
            body = self.rfile.read(content_length)
            data = json.loads(body.decode("utf-8"))
        except (json.JSONDecodeError, ValueError) as e:
            self.send_error(400, f"Invalid JSON: {e}")
            return

        points = validate_points(data)
        if points is None:
            self.send_error(400, "Expected JSON with 'points': [[x,y,z], ...] (at least 1 point)")
            return

        client = self.client_address[0] if self.client_address else "?"
        print(f"{BOLD}← STROKE from {client}{RESET} {len(points)} points")

        record = {
            "received_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            "session_id": data.get("sessionId", ""),
            "finger_id": data.get("fingerId", 0),
            "was_smoothed": bool(data.get("wasSmoothed", False)),
            "platform": data.get("platform", ""),
            "created_at": data.get("createdAt", ""),
            "points": points,
        }

        line = json.dumps(record, ensure_ascii=False)
        with open(STROKE_FILE, "a", encoding="utf-8") as f:
            f.write(line + "\n")

        self._send_json(200, {"status": "ok", "saved_points": len(points)})

    def _send_json(self, code, obj):
        response = json.dumps(obj).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(response)))
        self.end_headers()
        self.wfile.write(response)

    def log_message(self, format, *args):
        pass


def main():
    parser = argparse.ArgumentParser(description="Unity stroke capture server")
    parser.add_argument(
        "--port",
        type=int,
        default=int(os.environ.get("STROKE_PORT", "8081")),
        help="Port (default: $STROKE_PORT or 8081)",
    )
    args = parser.parse_args()

    local_ip = get_local_ip()
    server = HTTPServer(("0.0.0.0", args.port), StrokeHandler)

    print(f"\n{'='*60}")
    print("  ARDraw stroke server (JSONL)")
    print(f"{'='*60}")
    print(f"  Listening: 0.0.0.0:{args.port}")
    print(f"  LAN IP:    {local_ip}")
    print("")
    print("  Unity → Stroke Server URL:")
    print(f"    http://127.0.0.1:{args.port}/stroke     (Editor, same Mac)")
    print(f"    http://{local_ip}:{args.port}/stroke     (iPhone build)")
    print("")
    print(f"  Output: {STROKE_FILE}")
    print(f"{'='*60}\n")

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")
        server.server_close()


if __name__ == "__main__":
    main()
