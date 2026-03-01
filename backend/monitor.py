import os
import time
import shutil
import winreg
import threading
import urllib.request
import json
from datetime import datetime
from config import BACKUP_ROOT, BACKUP_TARGETS, user_config_file, pending_file
from ui import show_notification

def get_game_name_global(appid):
    if not appid or appid == 0:
        return "Steam Session"
    try:
        url = f"https://store.steampowered.com/api/appdetails?appids={appid}&filters=basic"
        with urllib.request.urlopen(url, timeout=5) as response:
            data = json.loads(response.read().decode())
            if str(appid) in data and data[str(appid)]['success']:
                return data[str(appid)]['data']['name']
    except:
        pass
    return f"AppID {appid}"

def do_backup(appid, game_name=None):
    if not game_name:
        game_name = get_game_name_global(appid)

    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    folder_name = f"CalyBackup-{timestamp}"
    dest_folder = os.path.join(BACKUP_ROOT, folder_name)
    success_count = 0

    print(f"[CalyRecall] Iniciando backup de '{game_name}' em: {dest_folder}")

    if not os.path.exists(BACKUP_ROOT):
        try:
            os.makedirs(BACKUP_ROOT)
        except:
            pass

    for target in BACKUP_TARGETS:
        src = target["src"]
        dst = os.path.join(dest_folder, target["name"])
        try:
            if os.path.exists(src):
                if os.path.isdir(src):
                    shutil.copytree(src, dst, dirs_exist_ok=True)
                else:
                    os.makedirs(os.path.dirname(dst), exist_ok=True)
                    shutil.copy2(src, dst)
                success_count += 1
        except Exception as e:
            print(f"[CalyRecall] Erro ao copiar {target['name']}: {e}")

    if success_count > 0:
        try:
            meta = {
                "appid": appid,
                "game_name": game_name,
                "nickname": None,
                "timestamp": timestamp
            }
            with open(os.path.join(dest_folder, "caly_meta.json"), "w", encoding='utf-8') as f:
                json.dump(meta, f, ensure_ascii=False)
        except:
            pass
        show_notification("CalyRecall", f"Backup de {game_name} concluído.")

class BackupManager(threading.Thread):
    def __init__(self):
        super().__init__(daemon=True)
        self.running = True
        self.last_appid = 0
        self.was_running = False

    def get_running_appid(self):
        try:
            key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam")
            val, _ = winreg.QueryValueEx(key, "RunningAppID")
            winreg.CloseKey(key)
            return int(val)
        except:
            return 0

    def stop(self):
        self.running = False

    def run(self):
        print("[CalyRecall] Monitor ativo (Game Awareness ON).")
        while self.running:
            current_appid = self.get_running_appid()

            if self.was_running and current_appid == 0:
                print("[CalyRecall] Jogo fechado. Verificando protocolo...")
                time.sleep(5)

                game_name = get_game_name_global(self.last_appid)

                semi_auto = False
                if os.path.exists(user_config_file):
                    try:
                        with open(user_config_file, 'r', encoding='utf-8') as f:
                            semi_auto = json.load(f).get("semi_auto", False)
                    except:
                        semi_auto = False

                if semi_auto:
                    print(f"[CalyRecall] Modo Semi-Auto: Aguardando aprovação na UI para {game_name}")
                    try:
                        with open(pending_file, "w", encoding='utf-8') as f:
                            json.dump({"appid": self.last_appid, "game_name": game_name}, f, ensure_ascii=False)
                        show_notification("CalyRecall", f"Aguardando confirmação de backup para {game_name}.")
                    except:
                        pass
                else:
                    do_backup(self.last_appid, game_name)

                self.was_running = False

            elif current_appid > 0:
                self.was_running = True
                self.last_appid = current_appid

            time.sleep(2)
