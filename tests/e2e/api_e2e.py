#!/usr/bin/env python3
"""API end-to-end suite for END_TO_END_TEST_CASES.md (no third-party dependencies).

Runs against a live API. It starts its own SMTP capture server, so start the API with
Smtp__Host=127.0.0.1 Smtp__Port=<--smtp-port> Smtp__UseSsl=false. Accounts, roles and
other records get a per-run suffix, so the suite can be re-run against the same database.

    python3 tests/e2e/api_e2e.py --api http://localhost:5091
"""
import argparse, html, json, re, socketserver, sys, threading, time, urllib.error, urllib.parse, urllib.request

parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
parser.add_argument("--api", default="http://localhost:5091", help="API root URL (default: %(default)s)")
parser.add_argument("--smtp-port", type=int, default=1025, help="port for the built-in SMTP capture (default: %(default)s)")
parser.add_argument("--mock-oauth-port", type=int, default=5099, help="port for the built-in mock OAuth provider (default: %(default)s)")
parser.add_argument("--superadmin-email", default="superadmin@localhost")
parser.add_argument("--superadmin-password", default="SuperAdmin@123!")
args = parser.parse_args()

sys.path.insert(0, __import__("os").path.dirname(__file__))
import mock_oauth as mock  # noqa: E402

MOCK = mock.start(args.mock_oauth_port)

ROOT = args.api.rstrip("/")
B = ROOT + "/api/v1"
RUN = str(int(time.time()))
MAILS = []


def em(name):
    """Unique per-run test email."""
    return f"{name}-{RUN}@e2e.local"


class _SmtpHandler(socketserver.StreamRequestHandler):
    """Minimal SMTP server that records message bodies in MAILS."""

    def handle(self):
        w = lambda line: self.wfile.write((line + "\r\n").encode())
        w("220 e2e-capture")
        body = None
        while True:
            raw = self.rfile.readline()
            if not raw:
                return
            line = raw.decode(errors="replace").rstrip("\r\n")
            if body is not None:
                if line == ".":
                    MAILS.append("\n".join(body))
                    body = None
                    w("250 OK")
                else:
                    body.append(line)
                continue
            cmd = line.upper()
            if cmd.startswith(("EHLO", "HELO")):
                w("250 e2e-capture")
            elif cmd.startswith("DATA"):
                body = []
                w("354 go ahead")
            elif cmd.startswith("QUIT"):
                w("221 bye")
                return
            else:
                w("250 OK")


socketserver.ThreadingTCPServer.allow_reuse_address = True
_smtp = socketserver.ThreadingTCPServer(("127.0.0.1", args.smtp_port), _SmtpHandler)
threading.Thread(target=_smtp.serve_forever, daemon=True).start()
results = []


def call(method, path, body=None, token=None, raw=None, base=B):
    url = path if path.startswith("http") else base + path
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(url, data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            txt = r.read().decode()
            code = r.status
    except urllib.error.HTTPError as e:
        txt = e.read().decode()
        code = e.code
    try:
        js = json.loads(txt) if txt else None
    except Exception:
        js = None
    return code, js, txt


def check(cid, desc, cond, detail=""):
    results.append((cid, desc, bool(cond), detail))
    print(("PASS" if cond else "FAIL"), cid, "-", desc, ("" if cond else f"  [{detail}]"))


def login(email, pw):
    c, j, t = call("POST", "/auth/login", {"email": email, "password": pw})
    return (j["data"] if c == 200 else None), c


def data(j):
    return (j or {}).get("data")


def no_leak(txt):
    return not re.search(r"   at |StackTrace|Host=|Password=|SecretKey|Npgsql", txt or "")


SA_EMAIL, SA_PW = args.superadmin_email, args.superadmin_password
sa, _ = login(SA_EMAIL, SA_PW)
SA = sa["accessToken"]

# ---------------- SMOKE ----------------
c, _, t = call("GET", ROOT + "/health")
check("SMK-001", "health 200 Healthy", c == 200 and "Healthy" in t, t)
c, j, _ = call("GET", ROOT + "/swagger/v1/swagger.json")
check("SMK-004", "swagger lists versioned operations", c == 200 and any("/api/v1/" in p for p in (j or {}).get("paths", {})), c)

# ---------------- AUTH ----------------
check("AUTH-001", "valid login returns access+refresh", sa and sa.get("accessToken") and sa.get("refreshToken"))
c, j, t = call("POST", "/auth/login", {"email": SA_EMAIL, "password": "wrong-pass-1"})
check("AUTH-002", "invalid password -> 401, no token", c == 401 and "accessToken" not in t and no_leak(t), c)
c, j, t = call("POST", "/auth/login", {"email": "", "password": ""})
check("AUTH-003a", "empty login -> 400 validation", c == 400 and no_leak(t), c)
c, j, t = call("POST", "/auth/login", {"email": "not-an-email", "password": "x"})
check("AUTH-003b", "malformed email -> 400/401", c in (400, 401), c)
c, _, _ = call("GET", "/users/me")
check("AUTH-010", "protected API w/o token -> 401", c == 401, c)
c, _, _ = call("GET", "/users/me", token="garbage.token.value")
check("AUTH-005a", "invalid access token -> 401", c == 401, c)

# refresh rotation
r1, _ = login(SA_EMAIL, SA_PW)
c, j, _ = call("POST", "/auth/refresh", {"refreshToken": r1["refreshToken"]})
check("AUTH-005b", "refresh issues new tokens", c == 200 and data(j)["refreshToken"] != r1["refreshToken"], c)
r2 = data(j)
c, _, _ = call("POST", "/auth/refresh", {"refreshToken": r1["refreshToken"]})
check("SEC-refresh-reuse", "old refresh token cannot be reused", c == 401, c)
c, _, _ = call("POST", "/auth/revoke-refresh", {"refreshToken": r2["refreshToken"]})
check("AUTH-006a", "revoke refresh -> 204", c == 204, c)
c, _, _ = call("POST", "/auth/refresh", {"refreshToken": r2["refreshToken"]})
check("AUTH-006b", "revoked refresh rejected", c == 401, c)

# ---------------- registration (no roles) ----------------
def register(name, email, pw, extra=None):
    body = {"fullName": name, "email": email, "password": pw}
    if extra:
        body.update(extra)
    return call("POST", "/auth/register", body)

PW = "Passw0rd!123"
MGR_ROLE = f"Manager{RUN}"
c, j, t = register("Admin Test", em("admin-test"), PW)
check("REG-001", "register without roles -> 201", c == 201, t[:200])
admin_id = data(j)["user"]["id"] if c == 201 else None
check("REG-002", "registered user has no roles", c == 201 and data(j)["user"]["roles"] == [], t[:200])
c, j, t = register("Evil", em("evil"), PW, {"roles": ["SuperAdmin"]})
evil_id = data(j)["user"]["id"] if c == 201 else None
c2, j2, _ = call("GET", f"/users/{evil_id}/roles", token=SA) if evil_id else (0, None, "")
check("SEC-register-escalation", "roles in register payload are ignored", c == 201 and data(j)["user"]["roles"] == [], t[:200])
c, _, t = register("Dup", em("admin-test"), PW)
check("ADM-010a", "duplicate email rejected 4xx", 400 <= c < 500 and no_leak(t), c)
c, _, t = register("", "bad", "1")
check("ADM-010b", "invalid register fields -> 400", c == 400 and no_leak(t), c)
c, _, t = register("x" * 500, em("long"), PW)
check("ADM-010c", "oversized name -> 400", c == 400, c)

c, j, _ = register("Manager Test", em("manager-test"), PW); mgr_id = data(j)["user"]["id"]
c, j, _ = register("User Test", em("user-test"), PW); usr_id = data(j)["user"]["id"]

roles = {r["name"]: r for r in data(call("GET", "/roles", token=SA)[1])}
perms = {p["name"]: p["id"] for p in data(call("GET", "/permissions", token=SA)[1])}

# ADM-004 role CRUD
c, j, t = call("POST", "/roles", {"name": MGR_ROLE, "description": "users read only"}, token=SA)
check("ADM-004a", "create role", c in (200, 201), t[:200])
mgr_role = data(j)["id"]
c, _, t = call("POST", "/roles", {"name": MGR_ROLE, "description": "dup"}, token=SA)
check("ADM-004b", "duplicate role rejected", 400 <= c < 500, c)
c, _, t = call("POST", "/roles", {"name": "", "description": ""}, token=SA)
check("ADM-004c", "invalid role rejected 400", c == 400, c)
c, j, t = call("PUT", f"/roles/{mgr_role}", {"name": MGR_ROLE, "description": "edited"}, token=SA)
check("ADM-004d", "update role", c == 200, t[:200])
c, j, _ = call("POST", "/roles", {"name": f"TempRole{RUN}", "description": "x"}, token=SA)
c, _, _ = call("DELETE", f"/roles/{data(j)['id']}", token=SA)
check("ADM-004e", "delete role", c in (200, 204), c)

# ADM-006 role permissions
c, _, t = call("PUT", f"/roles/{mgr_role}/permissions", {"permissionIds": [perms["users.read"]]}, token=SA)
check("ADM-006a", "set role permissions", c in (200, 204), t[:200])
c, j, _ = call("GET", f"/roles/{mgr_role}/permissions", token=SA)
names = json.dumps(data(j))
check("ADM-006b", "role permission list reflects change", c == 200 and "users.read" in names, names[:200])

# ADM-007 user roles
c, _, t = call("PUT", f"/users/{admin_id}/roles", {"roleIds": [roles["Admin"]["id"]]}, token=SA)
check("ADM-007a", "assign Admin role", c in (200, 204), t[:200])
call("PUT", f"/users/{mgr_id}/roles", {"roleIds": [mgr_role]}, token=SA)
call("PUT", f"/users/{usr_id}/roles", {"roleIds": [roles["User"]["id"]]}, token=SA)
c, j, _ = call("GET", f"/users/{mgr_id}/roles", token=SA)
check("ADM-007b", "user role list updated", c == 200 and MGR_ROLE in json.dumps(data(j)), json.dumps(data(j))[:200])

ADM = login(em("admin-test"), PW)[0]["accessToken"]
MGR = login(em("manager-test"), PW)[0]["accessToken"]
USR = login(em("user-test"), PW)[0]["accessToken"]

# ---------------- RBAC ----------------
c, j, _ = call("GET", "/menu", token=ADM)
adm_menu = json.dumps(data(j))
check("RBAC-001", "admin sees admin menu + users page", c == 200 and "/users" in adm_menu and call("GET", "/users", token=ADM)[0] == 200, adm_menu[:150])
c, _, _ = call("GET", "/users", token=MGR)
c2, _, _ = call("GET", "/roles", token=MGR)
check("RBAC-002", "manager: users.read ok, roles 403", c == 200 and c2 == 403, (c, c2))
c, _, t = call("GET", "/roles", token=USR)
c2, _, _ = call("GET", "/system/endpoints", token=USR)
check("RBAC-003/008", "user hitting admin APIs -> 403, no data", c == 403 and c2 == 403 and '"data":[' not in t, (c, c2))
mm = json.dumps(data(call("GET", "/menu", token=MGR)[1]))
um = json.dumps(data(call("GET", "/menu", token=USR)[1]))
check("ADM-009", "menus differ by permission", "/roles" not in mm and "/roles" not in um and "/users" in mm, (mm[:120], um[:120]))

# RBAC-004 grant roles.read to Manager -> access
call("POST", f"/roles/{mgr_role}/permissions/{perms['roles.read']}", token=SA)
time.sleep(0.5)
c, _, _ = call("GET", "/roles", token=MGR)
c_fresh, _, _ = call("GET", "/roles", token=login(em("manager-test"), PW)[0]["accessToken"])
check("RBAC-004", "granted permission takes effect (fresh token)", c_fresh == 200, f"existing token={c} fresh={c_fresh}")
call("DELETE", f"/roles/{mgr_role}/permissions/{perms['roles.read']}", token=SA)
time.sleep(0.5)
c_fresh, _, _ = call("GET", "/roles", token=login(em("manager-test"), PW)[0]["accessToken"])
check("RBAC-005", "revoked permission removes access (fresh token)", c_fresh == 403, c_fresh)
c, _, _ = call("DELETE", f"/roles/{roles['User']['id']}/permissions/{perms['users.read']}", token=ADM)
check("RBAC-006a", "Admin lacks role-permissions.delete -> 403", c == 403, c)
c, _, _ = call("GET", "/system/endpoints", token=SA)
check("RBAC-006b", "SuperAdmin bypass works", c == 200, c)

# ---------------- ADMIN / users ----------------
c, j, _ = call("GET", "/users", token=SA)
check("ADM-001", "users list loads", c == 200 and len(data(j)) >= 5, c)
c, j, t = call("PUT", "/users/me", {"fullName": "Manager Renamed", "email": em("manager-test"), "profileImageUrl": None}, token=MGR)
c2, j2, _ = call("GET", "/users/me", token=MGR)
check("ADM-002/CFG-006", "update own profile persists", c == 200 and data(j2)["fullName"] == "Manager Renamed", t[:200])
EVIL = login(em("evil"), PW)[0]["accessToken"]  # no roles at all
c, _, _ = call("GET", f"/users/{mgr_id}", token=EVIL)
c2, _, _ = call("GET", f"/users/{evil_id}", token=EVIL)
check("ADM-003", "self-or-permission on GET /users/{id}", c == 403 and c2 == 200, (c, c2))

# ADM-005 permission CRUD
c, j, t = call("POST", "/permissions", {"name": f"e2e.custom.{RUN}", "description": "x"}, token=SA)
check("ADM-005a", "create permission", c in (200, 201), t[:200])
pid = data(j)["id"] if data(j) else None
c, _, _ = call("POST", "/permissions", {"name": f"e2e.custom.{RUN}", "description": "dup"}, token=SA)
check("ADM-005b", "duplicate permission rejected", 400 <= c < 500, c)
c, _, t = call("PUT", f"/permissions/{pid}", {"name": f"e2e.custom.{RUN}", "description": "edited"}, token=SA)
check("ADM-005c", "update permission", c == 200, t[:200])
c, _, _ = call("DELETE", f"/permissions/{pid}", token=SA)
check("ADM-005d", "delete permission", c in (200, 204), c)

# ADM-008 menu CRUD
c, j, t = call("POST", "/menu", {"title": f"E2E Menu {RUN}", "url": "/e2e", "icon": "settings", "requiredPermission": "users.read"}, token=SA)
check("ADM-008a", "create menu", c in (200, 201), t[:200])
mid = (data(j) or {}).get("id")
c, _, t = call("PUT", f"/menu/{mid}", {"title": f"E2E Menu 2 {RUN}", "url": "/e2e", "icon": "settings", "requiredPermission": "users.read"}, token=SA)
check("ADM-008b", "update menu", c in (200, 204), t[:200])
inmgr = f"E2E Menu 2 {RUN}" in json.dumps(data(call("GET", "/menu", token=login(em("manager-test"), PW)[0]["accessToken"])[1]))
inusr = f"E2E Menu 2 {RUN}" in json.dumps(data(call("GET", "/menu", token=EVIL)[1]))
check("ADM-008c", "new menu shown only to permitted users", inmgr and not inusr, (inmgr, inusr))
c, _, t = call("POST", "/menu", {"title": "Bad", "url": "/bad", "icon": "x", "requiredPermission": "does.not.exist"}, token=SA)
check("RBAC-007", "menu with unknown permission rejected", 400 <= c < 500, f"{c} {t[:120]}")
c, _, _ = call("DELETE", f"/menu/{mid}", token=SA)
check("ADM-008d", "delete menu", c in (200, 204), c)

# ---------------- SETTINGS ----------------
c, j, _ = call("GET", "/SiteSettings", token=USR)
check("CFG-001", "settings load", c == 200 and len(data(j)) > 0, c)
c, j, t = call("POST", "/SiteSettings", {"id": 0, "key": f"E2E.Key.{RUN}", "value": "v1", "description": "e2e"}, token=SA)
check("CFG-003a", "create setting", c == 200, t[:200])
sid = data(j)["id"]
c, j, _ = call("POST", "/SiteSettings", {"id": sid, "key": f"E2E.Key.{RUN}", "value": "v2", "description": "e2e"}, token=SA)
c2, j2, _ = call("GET", f"/SiteSettings/E2E.Key.{RUN}", token=ADM)
check("CFG-002", "updated value visible to another session", c2 == 200 and data(j2)["value"] == "v2", data(j2))
c, _, _ = call("POST", "/SiteSettings", {"id": 0, "key": "", "value": "", "description": ""}, token=SA)
check("CFG-003b", "invalid key rejected", c == 400, c)
c, _, _ = call("POST", "/SiteSettings", {"id": 0, "key": f"E2E.UserWrite.{RUN}", "value": "x", "description": ""}, token=USR)
check("SEC-settings-write", "plain user cannot write settings", c == 403, c)
c, _, _ = call("DELETE", f"/SiteSettings/{sid}", token=SA)
check("CFG-003c", "delete setting", c == 200 and call("GET", f"/SiteSettings/E2E.Key.{RUN}", token=SA)[0] == 404, c)
c, j, t = call("POST", "/SiteSettings/palette", {"id": None, "key": "Palette.E2E", "colors": {"primary": "#123456"}}, token=SA)
c2, j2, _ = call("GET", "/SiteSettings/palette/Palette.E2E", token=SA)
check("CFG-004", "palette save + load", c == 200 and (data(j2) or {}).get("primary") == "#123456", t[:150])

# ---------------- SignalR ----------------
c, _, _ = call("POST", ROOT + "/hubs/notifications/negotiate?negotiateVersion=1", raw=b"", token=SA)
check("CFG-009", "hub negotiate with valid token", c == 200, c)
c, _, _ = call("POST", ROOT + "/hubs/notifications/negotiate?negotiateVersion=1", raw=b"", token="bad")
check("CFG-010", "hub negotiate with invalid token -> 401", c == 401, c)

# ---------------- OPS ----------------
c, j, _ = call("GET", "/Audit?pageSize=50", token=SA)
items = (data(j) or {}).get("items", [])
ts = [i["timestamp"] for i in items]
check("OPS-003", "audit log loads newest first", c == 200 and len(items) > 0 and ts == sorted(ts, reverse=True), c)
leak = any("Bearer " in (i.get("headers") or "") for i in items)
check("SEC-audit-headers", "audit log does not store bearer tokens", not leak)
pw_leak = any(PW in (i.get("requestBody") or "") for i in items)
check("SEC-audit-passwords", "audit log does not store passwords", not pw_leak)
c, _, t = call("GET", "/Audit", token=USR)
check("OPS-004", "audit denied without permission", c == 403 and "items" not in t, c)
c, j, _ = call("GET", "/system/endpoints", token=ADM)
c2, _, _ = call("GET", "/system/endpoints", token=USR)
check("OPS-005", "endpoint inventory admin ok / user denied", c == 200 and len(data(j)) > 10 and c2 == 403, (c, c2))
c, _, t = call("GET", "/system/cache/distributed/keys", token=SA)
c2, _, _ = call("DELETE", "/system/cache/distributed/flush", token=SA)
c3, _, _ = call("DELETE", "/system/cache/distributed/flush", token=USR)
check("OPS-006", "cache keys/flush admin ok, user denied", c == 200 and c2 in (200, 204) and c3 == 403, (c, c2, c3, t[:120]))

# ---------------- NOTIFICATIONS ----------------
def notify(user_id, token=SA, **over):
    body = {"userId": user_id, "type": "info", "title": f"Hello {RUN}", "message": "Test notification"}
    body.update(over)
    return call("POST", "/notifications", body, token=token)

c, j, t = notify(usr_id)
nid = (data(j) or {}).get("id")
check("CFG-007a", "admin creates a notification for a user", c == 201 and nid, t[:200])
c, j, _ = call("GET", "/notifications", token=USR)
mine = data(j) or []
check("CFG-007b", "recipient sees it unread", c == 200 and any(n["id"] == nid and not n["isRead"] for n in mine), c)
c, j, _ = call("GET", "/notifications", token=MGR)
check("NOTIF-isolation", "other users do not see it", c == 200 and all(n["id"] != nid for n in (data(j) or [])), c)
c, _, _ = notify(usr_id, token=MGR)
check("NOTIF-create-403", "create requires notifications.create", c == 403, c)
c, _, _ = notify(usr_id, type="loud")
c2, _, _ = notify(usr_id, title="")
check("NOTIF-validation", "invalid type / empty title -> 400", c == 400 and c2 == 400, (c, c2))
c, _, _ = notify(99999999)
check("NOTIF-unknown-user", "unknown recipient -> 404", c == 404, c)
c, _, _ = call("POST", f"/notifications/{nid}/read", token=MGR)
c2, _, _ = call("DELETE", f"/notifications/{nid}", token=MGR)
check("NOTIF-ownership", "cannot read/delete another user's notification", c == 404 and c2 == 404, (c, c2))
c, _, _ = call("POST", f"/notifications/{nid}/read", token=USR)
read = [n for n in data(call("GET", "/notifications", token=USR)[1]) if n["id"] == nid]
check("CFG-008a", "mark as read persists", c == 204 and read and read[0]["isRead"], c)
notify(usr_id); notify(usr_id)
c, _, _ = call("POST", "/notifications/read-all", token=USR)
check("CFG-008b", "mark all as read", c == 204 and all(n["isRead"] for n in data(call("GET", "/notifications", token=USR)[1])), c)
c, _, _ = call("DELETE", f"/notifications/{nid}", token=USR)
check("CFG-008c", "delete persists", c == 204 and all(n["id"] != nid for n in data(call("GET", "/notifications", token=USR)[1])), c)
c, _, _ = call("POST", ROOT + "/hubs/notifications/negotiate?negotiateVersion=1&access_token=" + USR, raw=b"")
check("CFG-009b", "hub accepts token in query string (WebSocket clients)", c == 200, c)

# ---------------- DASHBOARD ----------------
c, j, t = call("GET", "/dashboard?days=7", token=SA)
d = data(j) or {}
check("OPS-001a", "dashboard returns stats + 7 daily points", c == 200 and len(d.get("activity", [])) == 7
      and d["stats"]["totalUsers"] >= 5 and d["stats"]["requests"] > 0, t[:200])
recent = d.get("recentActivity") or []
check("OPS-001b", "recent activity is newest first and readable",
      recent and [a["timestamp"] for a in recent] == sorted([a["timestamp"] for a in recent], reverse=True)
      and ":" in recent[0]["action"] and all(a["method"] != "GET" for a in recent), recent[:1])
c, j, _ = call("GET", "/dashboard?days=30", token=ADM)
check("OPS-002", "date range changes the series length", c == 200 and len(data(j)["activity"]) == 30, c)
c, _, _ = call("GET", "/dashboard?days=0", token=SA)
check("OPS-001c", "invalid range -> 400", c == 400, c)
c, _, _ = call("GET", "/dashboard", token=USR)
check("OPS-001d", "dashboard requires reports.read", c == 403, c)

# ---------------- AUDIT FILTERS ----------------
c, j, t = call("GET", "/Audit?action=RoleController&pageSize=50", token=SA)
items = (data(j) or {}).get("items", [])
check("OPS-003b", "audit action filter works", c == 200 and items and all("rolecontroller" in (i["action"] + i["path"]).lower() for i in items), t[:200])
c, j, _ = call("GET", "/Audit?success=false&pageSize=50", token=SA)
check("OPS-003c", "audit success filter works", c == 200 and all(not i["success"] for i in data(j)["items"]), c)
check("OPS-003d", "audit entries record the user name", any(i.get("userName") for i in items), items[:1])

# ---------------- EXTERNAL SIGN-IN ----------------
class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


_no_redirect = urllib.request.build_opener(_NoRedirect)


def location(url):
    """GET without following redirects; returns (status, Location header)."""
    try:
        r = _no_redirect.open(url, timeout=30)
        return r.status, r.headers.get("Location")
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location")


def set_flag(display_name, on):
    return call("POST", "/SiteSettings", {"id": 0, "key": f"Auth.{display_name}.Enabled", "value": "true" if on else "false",
                                           "description": f"Show 'Continue with {display_name}' on the login page"}, token=SA)


def external_sign_in(provider, return_url="/users"):
    """Runs start -> provider -> callback; returns the final redirect URL into the frontend."""
    c, to_provider = location(f"{B}/auth/external/{provider}/start?returnUrl={urllib.parse.quote(return_url)}")
    if c != 302:
        return c, None
    c, to_callback = location(to_provider)
    c, to_frontend = location(to_callback)
    return c, to_frontend


def query_of(url):
    return dict(urllib.parse.parse_qsl(urllib.parse.urlparse(url).query))


c, j, _ = call("GET", "/auth/providers")
check("EXT-001", "providers endpoint is public and empty while flags are off", c == 200 and data(j) == [], data(j))
c, _, _ = call("GET", "/auth/providers/status")
c2, _, _ = call("GET", "/auth/providers/status", token=USR)
c3, j3, _ = call("GET", "/auth/providers/status", token=SA)
statuses = {s["id"]: s for s in (data(j3) or [])}
check("EXT-002", "provider status: 401 anon, 403 without permission, lists all 4 for admin",
      c == 401 and c2 == 403 and c3 == 200 and set(statuses) == {"google", "linkedin", "microsoft", "github"}, (c, c2, c3))

if not all(statuses.get(p, {}).get("configured") for p in ("google", "microsoft", "github")):
    print("SKIP external sign-in flow: start the API with the mock provider settings (see README)")
else:
    mock.set_identity(f"g-{RUN}", em("google"), True, "Google Person")
    c, _ = location(f"{B}/auth/external/google/start")
    check("EXT-003", "start is refused while the flag is off", c == 404, c)

    set_flag("Google", True); set_flag("Microsoft", True)
    c, j, _ = call("GET", "/auth/providers")
    check("EXT-004", "enabling the flag shows the provider on the login page",
          c == 200 and {p["id"] for p in data(j)} == {"google", "microsoft"}, data(j))
    c, _ = location(f"{B}/auth/external/linkedin/start")
    check("EXT-005", "enabled but unconfigured provider is unavailable", c == 404, c)

    c, to_provider = location(f"{B}/auth/external/google/start?returnUrl=%2Fusers")
    pq = query_of(to_provider or "")
    check("EXT-006", "start redirects to the provider with state + PKCE (S256)",
          c == 302 and to_provider.startswith(MOCK) and pq.get("state") and pq.get("code_challenge_method") == "S256"
          and pq.get("redirect_uri", "").endswith("/api/v1/auth/external/google/callback"), to_provider)

    c, final = external_sign_in("google", "/users")
    fq = query_of(final or "")
    check("EXT-007", "callback redirects to the app with a one-time code and the return URL",
          c == 302 and final.startswith("http://localhost:5173/auth/callback") and fq.get("code") and fq.get("returnUrl") == "/users", final)
    c, j, _ = call("POST", "/auth/external/exchange", {"code": fq.get("code", "")})
    ext_tokens = data(j) or {}
    c2, j2, _ = call("GET", "/users/me", token=ext_tokens.get("accessToken"))
    me = data(j2) or {}
    check("EXT-008", "code exchange signs in a new user with no roles",
          c == 200 and c2 == 200 and me.get("email") == em("google") and me.get("roles") == [] and me.get("fullName") == "Google Person", me)
    c, _, _ = call("POST", "/auth/external/exchange", {"code": fq.get("code", "")})
    check("EXT-009", "login code is single-use", c == 401, c)
    c, _, _ = call("POST", "/auth/external/exchange", {"code": "made-up"})
    c2, _, _ = call("POST", "/auth/external/exchange", {"code": ""})
    check("EXT-010", "unknown / empty code rejected", c == 401 and c2 == 400, (c, c2))

    c, final = external_sign_in("google")
    c2, j2, _ = call("GET", "/users/me", token=data(call("POST", "/auth/external/exchange", {"code": query_of(final)["code"]})[1])["accessToken"])
    check("EXT-011", "returning user signs in to the same account", data(j2)["id"] == me.get("id"), (me.get("id"), data(j2)["id"]))

    # State replay: run a sign-in, then replay its callback.
    c, to_provider = location(f"{B}/auth/external/google/start")
    c, to_callback = location(to_provider)
    location(to_callback)
    c, replay = location(to_callback)
    check("EXT-012", "replayed callback (reused state) is rejected", c == 302 and replay.endswith("/login?error=external_expired"), replay)
    c, bad = location(f"{B}/auth/external/google/callback?code=x&state=forged")
    check("EXT-013", "forged state is rejected", c == 302 and bad.endswith("/login?error=external_expired"), bad)

    c, to_provider = location(f"{B}/auth/external/google/start")
    st = query_of(to_provider)["state"]
    c, denied = location(f"{B}/auth/external/google/callback?error=access_denied&state={st}")
    check("EXT-014", "user cancelling at the provider is handled", c == 302 and denied.endswith("/login?error=external_cancelled"), denied)

    c, final = external_sign_in("google", "//evil.example.com/steal")
    check("EXT-015", "return URL cannot redirect off-site", query_of(final).get("returnUrl") == "/dashboard", final)

    # Linking rules for an existing password account (user-test).
    usr_email = em("user-test")
    mock.set_identity(f"g-unverified-{RUN}", usr_email, False)
    c, final = external_sign_in("google")
    check("EXT-016", "unverified email cannot take over an existing account", final.endswith("/login?error=external_email_in_use"), final)
    mock.set_identity(f"ms-{RUN}", usr_email, True)
    c, final = external_sign_in("microsoft")
    check("EXT-017", "Microsoft (email not vouched for) cannot link an existing account by email",
          final.endswith("/login?error=external_email_in_use"), final)
    mock.set_identity(f"g-verified-{RUN}", usr_email, True)
    c, final = external_sign_in("google")
    c2, j2, _ = call("GET", "/users/me", token=data(call("POST", "/auth/external/exchange", {"code": query_of(final)["code"]})[1])["accessToken"])
    check("EXT-018", "verified Google email links to the existing account", data(j2)["id"] == usr_id, data(j2))
    mock.set_identity(f"ms-new-{RUN}", em("ms-new"), False)
    c, final = external_sign_in("microsoft")
    c2, j2, _ = call("GET", "/users/me", token=data(call("POST", "/auth/external/exchange", {"code": query_of(final)["code"]})[1])["accessToken"])
    check("EXT-019", "Microsoft creates a new account for a new email", data(j2)["email"] == em("ms-new"), data(j2))
    mock.set_identity(f"g-noemail-{RUN}", None, False)
    c, final = external_sign_in("google")
    check("EXT-020", "provider without an email is refused", final.endswith("/login?error=external_no_email"), final)

    # GitHub: numeric ids, separate emails endpoint, primary *verified* email only.
    set_flag("GitHub", True)
    gh_id = int(RUN) * 10
    mock.set_github_identity(gh_id, f"octo{RUN}", None, [
        {"email": em("gh-secondary"), "primary": False, "verified": True},
        {"email": em("gh-primary"), "primary": True, "verified": True},
    ])
    c, final = external_sign_in("github")
    c2, j2, _ = call("GET", "/users/me", token=data(call("POST", "/auth/external/exchange", {"code": query_of(final).get("code", "")})[1] or {}).get("accessToken"))
    gh_me = data(j2) or {}
    check("EXT-022", "GitHub signs in with the primary verified email; name falls back to the login",
          gh_me.get("email") == em("gh-primary") and gh_me.get("fullName") == f"octo{RUN}" and gh_me.get("roles") == [], (final, gh_me))
    c, final = external_sign_in("github")
    c2, j2, _ = call("GET", "/users/me", token=data(call("POST", "/auth/external/exchange", {"code": query_of(final).get("code", "")})[1] or {}).get("accessToken"))
    check("EXT-023", "returning GitHub user (numeric id) gets the same account", (data(j2) or {}).get("id") == gh_me.get("id"), data(j2))
    mock.set_github_identity(gh_id + 1, f"unverified{RUN}", "Unverified", [
        {"email": em("gh-unverified"), "primary": True, "verified": False},
    ])
    c, final = external_sign_in("github")
    check("EXT-024", "GitHub without a verified primary email is refused", final.endswith("/login?error=external_no_email"), final)
    mock.set_github_identity(gh_id + 2, f"linker{RUN}", "Linker", [
        {"email": em("manager-test"), "primary": True, "verified": True},
    ])
    c, final = external_sign_in("github")
    c2, j2, _ = call("GET", "/users/me", token=data(call("POST", "/auth/external/exchange", {"code": query_of(final).get("code", "")})[1] or {}).get("accessToken"))
    check("EXT-025", "GitHub's verified email links to the existing account", (data(j2) or {}).get("id") == mgr_id, data(j2))

    set_flag("Google", False); set_flag("Microsoft", False); set_flag("GitHub", False)
    c, j, _ = call("GET", "/auth/providers")
    check("EXT-021", "turning flags off hides the providers again", data(j) == [], data(j))

# ---------------- VALIDATION / SECURITY ----------------
c, _, t = call("POST", "/roles", raw=b"{not json", token=SA)
check("SEC-malformed-json", "malformed JSON -> 400 no leak", c == 400 and no_leak(t), c)
c, _, t = call("GET", "/roles/999999", token=SA)
check("SEC-missing-id", "unknown id -> 404", c == 404 and no_leak(t), c)
c, _, t = call("GET", "/roles/abc", token=SA)
check("SEC-invalid-id", "non-numeric id -> 404/400", c in (400, 404), c)
c, _, t = call("PATCH", "/roles", {}, token=SA)
check("SEC-method", "unsupported method -> 405", c == 405, c)

# ---------------- PASSWORDS ----------------
c, j, t = call("POST", "/auth/change-password", {"userId": usr_id, "currentPassword": PW, "newPassword": "NewPassw0rd!9"}, token=USR)
check("AUTH-009a", "change password", c == 200, t[:200])
check("AUTH-009b", "old password fails / new works", login(em("user-test"), PW)[1] == 401 and login(em("user-test"), "NewPassw0rd!9")[0] is not None)
c, _, _ = call("POST", "/auth/change-password", {"userId": mgr_id, "currentPassword": PW, "newPassword": "Hijack3d!pw"}, token=USR)
check("SEC-change-pw-other", "cannot change another user's password", c in (400, 401, 403, 404) and login(em("manager-test"), PW)[0] is not None, c)

MAILS.clear()
c, j, t = call("POST", "/auth/forgot-password", {"email": em("admin-test")})
c2, j2, _ = call("POST", "/auth/forgot-password", {"email": em("nobody")})
time.sleep(1)
mails = list(MAILS)
check("AUTH-007a", "forgot-password generic response for known+unknown", c == 200 and c2 == 200 and data(j)["message"] == data(j2)["message"], (c, c2))
body = html.unescape("\n".join(mails).replace("=\n", "").replace("=3D", "="))
m = re.search(r"token=([^&\"'\s]+)", body)
check("AUTH-007b", "reset email delivered with token", len(mails) == 1 and m, f"{len(mails)} mails")
if m:
    tok = urllib.parse.unquote(m.group(1))
    c, _, _ = call("POST", "/auth/reset-password", {"email": em("admin-test"), "token": "bogus", "newPassword": "Reset3d!pw"})
    check("AUTH-008a", "invalid reset token rejected", c == 400, c)
    c, _, t = call("POST", "/auth/reset-password", {"email": em("admin-test"), "token": tok, "newPassword": "Reset3d!pw"})
    check("AUTH-007c", "valid reset changes password", c == 200 and login(em("admin-test"), "Reset3d!pw")[0] is not None, t[:200])
    c, _, _ = call("POST", "/auth/reset-password", {"email": em("admin-test"), "token": tok, "newPassword": "Again3d!pw"})
    check("AUTH-008b", "reused reset token rejected", c == 400 and login(em("admin-test"), "Reset3d!pw")[0] is not None, c)

c, _, _ = call("POST", "/roles", {"name": "x", "description": "x"})
check("SEC-401-all", "write without token -> 401", c == 401, c)

p = sum(r[2] for r in results)
print(f"\nTOTAL {p}/{len(results)} passed")
sys.exit(0 if p == len(results) else 1)
