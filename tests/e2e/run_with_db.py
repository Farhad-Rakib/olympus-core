#!/usr/bin/env python3
"""Runs an end-to-end suite against a throwaway PostgreSQL database.

  E2E_PG="Host=localhost;Port=5432;Username=postgres;Password=postgres" \
  python3 run_with_db.py --api <Api project dir or glob> --data <Persistence project dir or glob> -- <suite command>

Creates a uniquely named database (the API's startup migration creates it), starts the API on a free port,
waits for /health, runs the suite with E2E_API_URL set, then stops the API and drops the database,
whether the suite passed or not. `{url}` in the suite command is replaced with the API base URL.
Added by the SDD stack profile; project-owned after setup. Standard library only.
"""
import argparse
import datetime
import glob
import os
import secrets
import signal
import socket
import subprocess
import sys
import time
import urllib.request

STARTUP_SECONDS = 180


def one(pattern, what):
    hits = sorted(p for p in glob.glob(pattern) if os.path.isdir(p))
    if len(hits) != 1:
        sys.exit(f"run_with_db: --{what} '{pattern}' must match exactly one folder, found {hits or 'none'}")
    return hits[0]


def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def healthy(url):
    try:
        with urllib.request.urlopen(f"{url}/health", timeout=3) as r:
            return r.status == 200
    except Exception:
        return False


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--api", required=True)
    ap.add_argument("--data", required=True)
    ap.add_argument("--keep", action="store_true", help="keep the database for debugging")
    ap.add_argument("suite", nargs=argparse.REMAINDER)
    args = ap.parse_args()
    suite = args.suite[1:] if args.suite[:1] == ["--"] else args.suite
    server = os.environ.get("E2E_PG", "").strip().rstrip(";")
    if not server or not suite:
        sys.exit("run_with_db: set E2E_PG to a PostgreSQL server connection string without Database= "
                 "(such as Host=localhost;Port=5432;Username=postgres;Password=postgres) and pass the suite after --")
    if "database=" in server.lower():
        sys.exit("run_with_db: E2E_PG must not name a database; a fresh one is created per run")
    api, data = one(args.api, "api"), one(args.data, "data")
    stamp = datetime.datetime.now().strftime("%Y%m%d%H%M%S")
    name = f"e2e_{os.path.basename(os.getcwd()).lower()}_{stamp}_{secrets.token_hex(3)}".replace("-", "_")
    port = free_port()
    url = f"http://127.0.0.1:{port}"
    env = {**os.environ, "ASPNETCORE_ENVIRONMENT": os.environ.get("ASPNETCORE_ENVIRONMENT", "Development"),
           "ASPNETCORE_URLS": url, "Database__Provider": "postgres",
           "ConnectionStrings__PostgresConnection": f"{server};Database={name}", "E2E_API_URL": url}
    print(f"run_with_db: database {name}, API {url}", flush=True)
    log = open(f"e2e-api-{stamp}.log", "w")
    proc = subprocess.Popen(["dotnet", "run", "--no-launch-profile", "--project", api], env=env, stdout=log,
                            stderr=subprocess.STDOUT, start_new_session=True)
    code = 1
    try:
        deadline = time.time() + STARTUP_SECONDS
        while not healthy(url):
            if proc.poll() is not None or time.time() > deadline:
                log.flush()
                tail = open(log.name).read().splitlines()[-20:]
                print("run_with_db: the API did not become healthy:\n" + "\n".join(tail), file=sys.stderr)
                return 1
            time.sleep(2)
        code = subprocess.run([part.replace("{url}", url) for part in suite], env=env).returncode
    finally:
        if proc.poll() is None:
            os.killpg(proc.pid, signal.SIGTERM)
            try:
                proc.wait(timeout=30)
            except subprocess.TimeoutExpired:
                os.killpg(proc.pid, signal.SIGKILL)
        log.close()
        if args.keep:
            print(f"run_with_db: kept database {name}")
        else:
            drop = subprocess.run(["dotnet", "ef", "database", "drop", "--force", "--project", data,
                                   "--startup-project", api], env=env, capture_output=True, text=True)
            if drop.returncode:
                print(f"run_with_db: could not drop {name}; drop it by hand:\n{drop.stderr.strip()[-800:]}", file=sys.stderr)
                code = code or 1
            else:
                print(f"run_with_db: dropped database {name}")
                os.remove(log.name) if code == 0 else None
    return code


if __name__ == "__main__":
    sys.exit(main())
