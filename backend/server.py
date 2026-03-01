import os
import json
import threading
import urllib.parse
import subprocess
import shutil
from http.server import BaseHTTPRequestHandler, HTTPServer
from config import BACKUP_ROOT, SERVER_PORT, STEAM_PATH, user_config_file, pending_file

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
            config = {"semi_auto": False}
            if os.path.exists(user_config_file):
                try:
                    with open(user_config_file, 'r', encoding='utf-8') as f:
                        loaded = json.load(f)
                        if isinstance(loaded, dict):
                            config.update(loaded)
                except:
                    pass
            self.wfile.write(json.dumps({"semi_auto": bool(config.get("semi_auto", False))}).encode())

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
        else:
            self.send_response(404)
            self.end_headers()

    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        post_data = self.rfile.read(content_length).decode('utf-8') if content_length > 0 else ""
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
                data = json.loads(post_data)
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
                data = json.loads(post_data) if post_data else {}
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
                data = json.loads(post_data) if post_data else {}
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

def start_server():
    server_address = ('127.0.0.1', SERVER_PORT)
    httpd = HTTPServer(server_address, CalyRequestHandler)
    httpd.serve_forever()
