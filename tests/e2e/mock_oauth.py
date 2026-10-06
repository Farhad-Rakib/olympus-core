"""Minimal OAuth 2.0 / OIDC provider for exercising external sign-in in tests.

/authorize redirects straight back with a code (no login screen). The identity it signs in
is whatever the test last set with set_identity() or set_github_identity(). /token checks the
client credentials and the PKCE code_verifier. /userinfo returns the identity as OIDC claims;
/github/user and /github/emails return it in the shape of GitHub's REST API.
"""
import base64, hashlib, http.server, json, secrets, threading, urllib.parse

CLIENT_ID = "e2e-client"
CLIENT_SECRET = "e2e-secret"

_state = {"identity": None, "codes": {}, "tokens": {}}


def set_identity(sub, email, email_verified=True, name="External User"):
    _state["identity"] = {"sub": sub, "email": email, "email_verified": email_verified, "name": name}


def set_github_identity(user_id, login, name, emails):
    """emails: list of {"email", "primary", "verified"} dicts, as GitHub returns them."""
    _state["identity"] = {"github_user": {"id": user_id, "login": login, "name": name,
                                          "avatar_url": f"https://avatars.example/{user_id}"},
                          "github_emails": emails}


class _Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def _json(self, status, body):
        payload = json.dumps(body).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def do_GET(self):
        url = urllib.parse.urlparse(self.path)
        q = dict(urllib.parse.parse_qsl(url.query))
        if url.path == "/authorize":
            if q.get("client_id") != CLIENT_ID or q.get("code_challenge_method") != "S256":
                return self._json(400, {"error": "invalid_request"})
            code = secrets.token_urlsafe(16)
            _state["codes"][code] = {"challenge": q["code_challenge"], "redirect_uri": q["redirect_uri"],
                                     "identity": dict(_state["identity"])}
            target = q["redirect_uri"] + "?" + urllib.parse.urlencode({"code": code, "state": q.get("state", "")})
            self.send_response(302)
            self.send_header("Location", target)
            self.end_headers()
        elif url.path in ("/userinfo", "/github/user", "/github/emails"):
            token = self.headers.get("Authorization", "").removeprefix("Bearer ")
            identity = _state["tokens"].get(token)
            if not identity:
                return self._json(401, {"error": "invalid_token"})
            if url.path == "/github/user":
                return self._json(200, identity["github_user"])
            if url.path == "/github/emails":
                return self._json(200, identity["github_emails"])
            return self._json(200, identity)
        else:
            self._json(404, {})

    def do_POST(self):
        if self.path != "/token":
            return self._json(404, {})
        body = dict(urllib.parse.parse_qsl(self.rfile.read(int(self.headers["Content-Length"])).decode()))
        grant = _state["codes"].pop(body.get("code"), None)  # codes are single-use
        if not grant or body.get("client_id") != CLIENT_ID or body.get("client_secret") != CLIENT_SECRET:
            return self._json(400, {"error": "invalid_grant"})
        verifier_hash = base64.urlsafe_b64encode(hashlib.sha256(body.get("code_verifier", "").encode()).digest()).rstrip(b"=").decode()
        if verifier_hash != grant["challenge"] or body.get("redirect_uri") != grant["redirect_uri"]:
            return self._json(400, {"error": "invalid_grant", "error_description": "PKCE or redirect_uri mismatch"})
        token = secrets.token_urlsafe(16)
        _state["tokens"][token] = grant["identity"]
        self._json(200, {"access_token": token, "token_type": "Bearer"})


def start(port):
    server = http.server.ThreadingHTTPServer(("127.0.0.1", port), _Handler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return f"http://127.0.0.1:{port}"
