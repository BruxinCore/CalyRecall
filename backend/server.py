import os
import json
import threading
import urllib.parse
import subprocess
import shutil
import zipfile
import io
import uuid
from http.server import BaseHTTPRequestHandler, HTTPServer
from config import BACKUP_ROOT, SERVER_PORT, STEAM_PATH, user_config_file, pending_file

EXPORT_TASKS = {}
PLUGIN_INSTANCE = None

def _collect_backup_files(scope):
    files = []
    if scope == 'all':
        if os.path.exists(BACKUP_ROOT):
            for d in os.listdir(BACKUP_ROOT):
                full = os.path.join(BACKUP_ROOT, d)
                if os.path.isdir(full) and d.startswith("CalyBackup"):
                    for root, _, fs in os.walk(full):
                        for name in fs:
                            fpath = os.path.join(root, name)
                            arc = os.path.relpath(fpath, BACKUP_ROOT)
                            files.append((fpath, arc))
    else:
        target = os.path.join(BACKUP_ROOT, scope)
        if os.path.isdir(target) and scope.startswith("CalyBackup"):
            for root, _, fs in os.walk(target):
                for name in fs:
                    fpath = os.path.join(root, name)
                    arc = os.path.relpath(fpath, BACKUP_ROOT)
                    files.append((fpath, arc))
    return files

def _start_export_task(scope):
    tid = str(uuid.uuid4())
    files = _collect_backup_files(scope)
    total_bytes = 0
    for fpath, _ in files:
        try:
            total_bytes += os.path.getsize(fpath)
        except:
            pass
    zip_name = f'calyrecall-backups-{scope}.zip' if scope != 'all' else 'calyrecall-backups-all.zip'
    zip_path = os.path.join(os.environ.get("TEMP", BACKUP_ROOT), zip_name)
    task = {
        "id": tid,
        "scope": scope,
        "status": "preparing",
        "total_bytes": total_bytes,
        "done_bytes": 0,
        "total_files": len(files),
        "done_files": 0,
        "zip_path": zip_path,
        "filename": zip_name,
        "cancel": False,
    }
    EXPORT_TASKS[tid] = task

    def _runner():
        try:
            task["status"] = "zipping"
            with zipfile.ZipFile(zip_path, mode='w', compression=zipfile.ZIP_DEFLATED) as zf:
                for fpath, arc in files:
                    if task.get("cancel"):
                        task["status"] = "canceled"
                        return
                    try:
                        zf.write(fpath, arcname=arc)
                        task["done_files"] += 1
                        try:
                            task["done_bytes"] += os.path.getsize(fpath)
                        except:
                            pass
                    except:
                        pass
            task["status"] = "ready"
        except:
            task["status"] = "error"

    threading.Thread(target=_runner, daemon=True).start()
    return tid

class CalyRequestHandler(BaseHTTPRequestHandler):
    def do_OPTIONS(self):
        self.send_response(200, "ok")
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, DELETE, OPTIONS')
        self.send_header("Access-Control-Allow-Headers", "X-Requested-With, Content-Type")
        self.end_headers()

    def do_GET(self):
        if self.path == '/check_restore':
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            flag_file = os.path.join(BACKUP_ROOT, "restore_success.flag")
            was_restored = False
            if os.path.exists(flag_file):
                was_restored = True
                try: os.remove(flag_file) 
                except: pass
            self.wfile.write(json.dumps({"restored": was_restored}).encode())

        elif self.path == '/list':
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            backups = []
            if os.path.exists(BACKUP_ROOT):
                try:
                    dirs = [d for d in os.listdir(BACKUP_ROOT) if os.path.isdir(os.path.join(BACKUP_ROOT, d)) and d.startswith("CalyBackup")]
                    dirs.sort(reverse=True)
                    for d in dirs:
                        meta_path = os.path.join(BACKUP_ROOT, d, "caly_meta.json")
                        meta_data = {}
                        if os.path.exists(meta_path):
                            try:
                                with open(meta_path, 'r', encoding='utf-8') as f:
                                    meta_data = json.load(f)
                            except: pass
                        nickname = meta_data.get("nickname") or meta_data.get("name")
                        backups.append({
                            "folder": d,
                            "nickname": nickname,
                            "game_name": meta_data.get("game_name"),
                            "appid": meta_data.get("appid")
                        })
                except: pass
            self.wfile.write(json.dumps(backups).encode())

        elif self.path == '/settings':
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            config = {
                "backup_mode": "auto", 
                "hotkey_enabled": False, 
                "hotkey_mod": 0, 
                "hotkey_vk": 0, 
                "hotkey_str": ""
            }
            if os.path.exists(user_config_file):
                try:
                    with open(user_config_file, 'r', encoding='utf-8') as f:
                        loaded = json.load(f)
                        if isinstance(loaded, dict):
                            config.update(loaded)
                            # Compatibilidade
                            if "backup_mode" not in loaded and loaded.get("semi_auto"):
                                config["backup_mode"] = "semi"
                except:
                    pass
            self.wfile.write(json.dumps(config).encode())

        elif self.path.startswith('/pending'):
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            pending_data = {"pending": False}
            if os.path.exists(pending_file):
                try:
                    with open(pending_file, 'r', encoding='utf-8') as f:
                        data = json.load(f)
                        if isinstance(data, dict):
                            data["pending"] = True
                            pending_data = data
                except:
                    pass
            self.wfile.write(json.dumps(pending_data).encode())

        elif self.path.startswith('/export/') and not (self.path.startswith('/export/progress/') or self.path.startswith('/export/download/') or self.path.startswith('/export/start')):
            scope = 'all'
            parts = self.path.split('/')
            if len(parts) >= 3 and parts[2]:
                scope = urllib.parse.unquote(parts[2])
            mem = io.BytesIO()
            try:
                with zipfile.ZipFile(mem, mode='w', compression=zipfile.ZIP_DEFLATED) as zf:
                    if scope == 'all':
                        if os.path.exists(BACKUP_ROOT):
                            for d in os.listdir(BACKUP_ROOT):
                                full = os.path.join(BACKUP_ROOT, d)
                                if os.path.isdir(full) and d.startswith("CalyBackup"):
                                    for root, _, files in os.walk(full):
                                        for name in files:
                                            fpath = os.path.join(root, name)
                                            arc = os.path.relpath(fpath, BACKUP_ROOT)
                                            zf.write(fpath, arcname=arc)
                    else:
                        target = os.path.join(BACKUP_ROOT, scope)
                        if os.path.isdir(target) and scope.startswith("CalyBackup"):
                            for root, _, files in os.walk(target):
                                for name in files:
                                    fpath = os.path.join(root, name)
                                    arc = os.path.relpath(fpath, BACKUP_ROOT)
                                    zf.write(fpath, arcname=arc)
                mem.seek(0)
                filename = f'calyrecall-backups-{scope}.zip' if scope != 'all' else 'calyrecall-backups-all.zip'
                self.send_response(200)
                self.send_header('Content-Type', 'application/zip')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.send_header('Content-Disposition', f'attachment; filename="{filename}"')
                self.end_headers()
                self.wfile.write(mem.read())
            except:
                self.send_error(500)
        elif self.path == '/export/start':
            try:
                length = int(self.headers.get('Content-Length', 0))
                data = json.loads(self.rfile.read(length).decode('utf-8')) if length > 0 else {}
                scope = data.get("scope") or "all"
                tid = _start_export_task(scope)
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(json.dumps({"id": tid}).encode())
            except:
                self.send_error(500)
        elif self.path.startswith('/export/progress/'):
            tid = self.path.split('/')[-1]
            task = EXPORT_TASKS.get(tid)
            if not task:
                self.send_error(404)
                return
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(json.dumps({
                "status": task["status"],
                "total_bytes": task["total_bytes"],
                "done_bytes": task["done_bytes"],
                "total_files": task["total_files"],
                "done_files": task["done_files"],
                "filename": task["filename"],
            }).encode())
        elif self.path.startswith('/export/download/'):
            tid = self.path.split('/')[-1]
            task = EXPORT_TASKS.get(tid)
            if not task or task.get("status") != "ready":
                self.send_error(404)
                return
            try:
                self.send_response(200)
                self.send_header('Content-Type', 'application/zip')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.send_header('Content-Disposition', f'attachment; filename="{task["filename"]}"')
                self.end_headers()
                with open(task["zip_path"], 'rb') as f:
                    shutil.copyfileobj(f, self.wfile)
            except:
                self.send_error(500)
        elif self.path == '/stats':
            try:
                backup_bytes = 0
                backup_count = 0
                if os.path.exists(BACKUP_ROOT):
                    try:
                        for d in os.listdir(BACKUP_ROOT):
                            full = os.path.join(BACKUP_ROOT, d)
                            if os.path.isdir(full) and d.startswith("CalyBackup"):
                                backup_count += 1
                                for root, _, files in os.walk(full):
                                    for name in files:
                                        fpath = os.path.join(root, name)
                                        try:
                                            backup_bytes += os.path.getsize(fpath)
                                        except:
                                            pass
                    except:
                        pass
                try:
                    usage = shutil.disk_usage(BACKUP_ROOT if os.path.exists(BACKUP_ROOT) else os.getcwd())
                    disk_total_bytes = int(usage.total)
                    disk_free_bytes = int(usage.free)
                except:
                    disk_total_bytes = 0
                    disk_free_bytes = 0
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(json.dumps({
                    "backup_bytes": backup_bytes,
                    "backup_count": backup_count,
                    "disk_total_bytes": disk_total_bytes,
                    "disk_free_bytes": disk_free_bytes
                }).encode())
            except:
                self.send_error(500)
        else:
            self.send_response(404)
            self.end_headers()

    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        raw_body = self.rfile.read(content_length) if content_length > 0 else b""
        
        if self.path == '/settings':
            try:
                data = json.loads(raw_body.decode('utf-8'))
                
                cfg = {}
                if os.path.exists(user_config_file):
                    try:
                        with open(user_config_file, 'r', encoding='utf-8') as f:
                            cfg = json.load(f)
                    except: pass
                
                # Suporte legado para semi_auto
                if "semi_auto" in data:
                    cfg["backup_mode"] = "semi" if data["semi_auto"] else "auto"
                    cfg["semi_auto"] = data["semi_auto"]
                
                # Novo campo backup_mode
                if "backup_mode" in data:
                    cfg["backup_mode"] = data["backup_mode"]
                    # Atualiza legado também
                    cfg["semi_auto"] = (data["backup_mode"] == "semi")
                
                with open(user_config_file, 'w', encoding='utf-8') as f:
                    json.dump(cfg, f, ensure_ascii=False)
                
                self.send_response(200)
                self.end_headers()
                self.wfile.write(b'{"status": "ok"}')
            except Exception as e:
                self.send_error(500, str(e))
            return
            try:
                data = json.loads(raw_body.decode('utf-8'))
                enabled = data.get("enabled", False)
                mod = data.get("mod", 0)
                vk = data.get("vk", 0)
                hk_str = data.get("str", "")
                
                # Salvar no config
                cfg = {}
                if os.path.exists(user_config_file):
                    try:
                        with open(user_config_file, 'r', encoding='utf-8') as f:
                            cfg = json.load(f)
                    except: pass
                
                cfg["hotkey_enabled"] = enabled
                cfg["hotkey_mod"] = mod
                cfg["hotkey_vk"] = vk
                cfg["hotkey_str"] = hk_str
                
                with open(user_config_file, 'w', encoding='utf-8') as f:
                    json.dump(cfg, f, ensure_ascii=False)
                
                # Atualizar runtime
                if PLUGIN_INSTANCE and PLUGIN_INSTANCE.hotkey:
                    if enabled:
                        PLUGIN_INSTANCE.hotkey.update(mod, vk)
                    else:
                        PLUGIN_INSTANCE.hotkey.stop()
                
                self.send_response(200)
                self.end_headers()
                self.wfile.write(b'{"status": "ok"}')
            except Exception as e:
                self.send_error(500, str(e))
            return

        if self.path.startswith('/restore/'):
            backup_name = self.path.replace('/restore/', '')
            backup_name = urllib.parse.unquote(backup_name)
            self.send_response(200)
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(b'{"status": "accepted"}')
            threading.Thread(target=trigger_external_restore, args=(backup_name,), daemon=True).start()
        elif self.path.startswith('/delete/'):
            backup_name = self.path.replace('/delete/', '')
            backup_name = urllib.parse.unquote(backup_name)
            target_path = os.path.join(BACKUP_ROOT, backup_name)
            if os.path.exists(target_path) and os.path.isdir(target_path):
                try:
                    shutil.rmtree(target_path)
                    self.send_response(200)
                    self.send_header('Access-Control-Allow-Origin', '*')
                    self.end_headers()
                    self.wfile.write(b'{"status": "deleted"}')
                except: self.send_error(500)
            else:
                self.send_error(404)
        elif self.path.startswith('/rename'):
            try:
                data = json.loads(raw_body.decode('utf-8')) if raw_body else {}
                folder = data.get("folder")
                new_nickname = data.get("new_name")
                if folder:
                    meta_path = os.path.join(BACKUP_ROOT, folder, "caly_meta.json")
                    current_meta = {}
                    if os.path.exists(meta_path):
                        try:
                            with open(meta_path, 'r', encoding='utf-8') as f:
                                current_meta = json.load(f)
                        except: pass
                    current_meta["nickname"] = new_nickname
                    with open(meta_path, 'w', encoding='utf-8') as f:
                        json.dump(current_meta, f, ensure_ascii=False)
                    self.send_response(200)
                    self.send_header('Access-Control-Allow-Origin', '*')
                    self.end_headers()
                    self.wfile.write(b'{"status": "renamed"}')
                else:
                    self.send_error(400)
            except: self.send_error(500)

        elif self.path == '/settings':
            try:
                data = json.loads(raw_body.decode('utf-8')) if raw_body else {}
                current_config = {}
                if os.path.exists(user_config_file):
                    try:
                        with open(user_config_file, 'r', encoding='utf-8') as f:
                            loaded = json.load(f)
                            if isinstance(loaded, dict):
                                current_config = loaded
                    except:
                        current_config = {}
                current_config["semi_auto"] = bool(data.get("semi_auto", False))
                with open(user_config_file, 'w', encoding='utf-8') as f:
                    json.dump(current_config, f, ensure_ascii=False)

                self.send_response(200)
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(b'{"status": "saved"}')
            except:
                self.send_error(500)

        elif self.path == '/pending/action':
            try:
                data = json.loads(raw_body.decode('utf-8')) if raw_body else {}
                action = data.get("action")

                if os.path.exists(pending_file):
                    try:
                        os.remove(pending_file)
                    except:
                        pass

                if action == "confirm":
                    appid = data.get("appid")
                    game_name = data.get("game_name")
                    from monitor import do_backup
                    threading.Thread(target=do_backup, args=(appid, game_name), daemon=True).start()

                self.send_response(200)
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(b'{"status": "ok"}')
            except:
                self.send_error(500)

        elif self.path == '/export/start':
            try:
                data = json.loads(raw_body.decode('utf-8')) if raw_body else {}
                scope = data.get("scope") or "all"
                tid = _start_export_task(scope)
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(json.dumps({"id": tid}).encode())
            except:
                self.send_error(500)

        elif self.path == '/import':
            try:
                mem = io.BytesIO(raw_body or b"")
                imported = 0
                with zipfile.ZipFile(mem, 'r') as zf:
                    for member in zf.namelist():
                        parts = member.split('/')
                        if not parts:
                            continue
                        top = parts[0]
                        if top.startswith("CalyBackup"):
                            dest = os.path.join(BACKUP_ROOT, member.replace('/', os.sep))
                            if member.endswith('/'):
                                os.makedirs(dest, exist_ok=True)
                            else:
                                os.makedirs(os.path.dirname(dest), exist_ok=True)
                                with zf.open(member) as src, open(dest, 'wb') as out:
                                    shutil.copyfileobj(src, out)
                            imported += 1
                self.send_response(200)
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(json.dumps({"status": "imported", "files": imported}).encode())
            except:
                self.send_error(500)

    def log_message(self, format, *args): return

def trigger_external_restore(backup_folder_name):
    backup_src = os.path.join(BACKUP_ROOT, backup_folder_name)
    steam_exe = os.path.join(STEAM_PATH, "steam.exe")
    temp_bat = os.path.join(os.environ["TEMP"], "caly_restore.bat")
    flag_file = os.path.join(BACKUP_ROOT, "restore_success.flag")
    bat_content = [
        "@echo off",
        "title CalyRecall - Restaurando...",
        "color 0D",
        "cls",
        "echo CalyRecall Restore Protocol",
        "timeout /t 3 /nobreak >nul",
        "taskkill /F /IM steam.exe >nul 2>&1",
        "timeout /t 2 /nobreak >nul",
        f'set "BACKUP={backup_src}"',
        f'set "STEAM={STEAM_PATH}"',
        'xcopy "%BACKUP%\\userdata\\*" "%STEAM%\\userdata\\" /E /H /C /I /Y /Q >nul 2>&1',
        'xcopy "%BACKUP%\\appcache_stats\\*" "%STEAM%\\appcache\\stats\\" /E /H /C /I /Y /Q >nul 2>&1',
        'xcopy "%BACKUP%\\depotcache\\*" "%STEAM%\\depotcache\\" /E /H /C /I /Y /Q >nul 2>&1',
        'xcopy "%BACKUP%\\stplug-in\\*" "%STEAM%\\config\\stplug-in\\" /E /H /C /I /Y /Q >nul 2>&1',
        f'echo 1 > "{flag_file}"',
        f'start "" "{steam_exe}"',
        '(goto) 2>nul & del "%~f0"'
    ]
    try:
        with open(temp_bat, "w") as f:
            f.write("\n".join(bat_content))
        subprocess.Popen([temp_bat], creationflags=subprocess.CREATE_NEW_CONSOLE)
    except: pass

def start_server(plugin_instance=None):
    global PLUGIN_INSTANCE
    PLUGIN_INSTANCE = plugin_instance
    
    # Carregar hotkey inicial
    try:
        if os.path.exists(user_config_file):
            with open(user_config_file, 'r', encoding='utf-8') as f:
                cfg = json.load(f)
                if cfg.get("hotkey_enabled", False):
                    mod = cfg.get("hotkey_mod", 0)
                    vk = cfg.get("hotkey_vk", 0)
                    if PLUGIN_INSTANCE and PLUGIN_INSTANCE.hotkey:
                        PLUGIN_INSTANCE.hotkey.update(mod, vk)
    except:
        pass

    server_address = ('localhost', SERVER_PORT)
    httpd = HTTPServer(server_address, CalyRequestHandler)
    print(f"[CalyRecall] Servidor rodando na porta {SERVER_PORT}")
    httpd.serve_forever()
