#!/usr/bin/env python3
"""
Remote Debug Log Server for Unity AR projects.
Receives debug messages via HTTP POST and displays them in the terminal
with color coding. All messages are saved to a JSON log file.

Usage:
    python3 debug_server.py [--port 8080]

The Unity app should POST JSON to http://<this-machine-ip>:<port>/log
Both devices must be on the same WiFi network.
"""

import argparse
import json
import os
import socket
from datetime import datetime
from http.server import HTTPServer, BaseHTTPRequestHandler

# ANSI color codes
COLORS = {
    "info": "\033[37m",      # white
    "warning": "\033[33m",   # yellow
    "error": "\033[31m",     # red
}
RESET = "\033[0m"
BOLD = "\033[1m"

LOG_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "debug_log.json")

# In-memory log store
log_entries = []


def load_existing_logs():
    """Load existing log file if present."""
    global log_entries
    if os.path.exists(LOG_FILE):
        try:
            with open(LOG_FILE, "r") as f:
                log_entries = json.load(f)
            print(f"Loaded {len(log_entries)} existing log entries from {LOG_FILE}")
        except (json.JSONDecodeError, IOError):
            log_entries = []


def save_logs():
    """Write all log entries to the JSON file."""
    with open(LOG_FILE, "w") as f:
        json.dump(log_entries, f, indent=2)


def get_local_ip():
    """Get the machine's LAN IP address."""
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return "127.0.0.1"


class DebugLogHandler(BaseHTTPRequestHandler):

    def do_POST(self):
        if self.path == "/log":
            self._handle_log()
        else:
            self.send_error(404, "Not Found")

    def do_GET(self):
        if self.path == "/health":
            self._send_json(200, {"status": "running"})
        else:
            self.send_error(404, "Not Found")

    def _handle_log(self):
        try:
            content_length = int(self.headers.get("Content-Length", 0))
            body = self.rfile.read(content_length)
            data = json.loads(body.decode("utf-8"))
        except (json.JSONDecodeError, ValueError) as e:
            self.send_error(400, f"Invalid JSON: {e}")
            return

        timestamp = data.get("timestamp", datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
        level = data.get("level", "info").lower()
        message = data.get("message", "")

        client = self.client_address[0] if self.client_address else "?"
        print(f"{BOLD}← POST from {client}{RESET}")

        # Print to terminal with color
        color = COLORS.get(level, COLORS["info"])
        level_tag = f"[{level.upper()}]"
        print(f"{color}{BOLD}{timestamp} {level_tag}{RESET}{color} {message}{RESET}")

        # Store and save
        entry = {
            "timestamp": timestamp,
            "level": level,
            "message": message,
            "received_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        }
        log_entries.append(entry)
        save_logs()

        self._send_json(200, {"status": "ok"})

    def _send_json(self, code, obj):
        response = json.dumps(obj).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(response)))
        self.end_headers()
        self.wfile.write(response)

    def log_message(self, format, *args):
        # Suppress default access log noise
        pass


def main():
    parser = argparse.ArgumentParser(description="Unity Remote Debug Log Server")
    parser.add_argument("--port", type=int, default=int(os.environ.get("PORT", 8080)), help="Port to listen on (default: $PORT or 8080)")
    args = parser.parse_args()

    load_existing_logs()

    local_ip = get_local_ip()
    server = HTTPServer(("0.0.0.0", args.port), DebugLogHandler)

    print(f"\n{'='*60}")
    print(f"  Unity Remote Debug Log Server")
    print(f"{'='*60}")
    print(f"  Listening on: 0.0.0.0:{args.port}")
    print(f"  Your LAN IP:  {local_ip}")
    print(f"")
    print(f"  Set this in Unity Inspector:")
    print(f"  Remote Server URL: http://{local_ip}:{args.port}/log")
    print(f"")
    print(f"  Log file: {LOG_FILE}")
    print(f"{'='*60}\n")

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print(f"\n\nServer stopped. {len(log_entries)} total log entries saved to {LOG_FILE}")
        server.server_close()


if __name__ == "__main__":
    main()
