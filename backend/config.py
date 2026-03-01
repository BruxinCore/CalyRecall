import os
import winreg
import json

def get_steam_path():
    try:
        key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam")
        path, _ = winreg.QueryValueEx(key, "SteamPath")
        winreg.CloseKey(key)
        return path.replace("/", "\\")
    except:
        return os.getcwd()

PLUGIN_DIR = os.path.dirname(os.path.abspath(__file__))
user_config_file = os.path.join(PLUGIN_DIR, "user_config.json")
pending_file = os.path.join(PLUGIN_DIR, "pending.json")

SERVER_PORT = 9999
STEAM_PATH = get_steam_path()

def load_user_config():
    if os.path.exists(user_config_file):
        try:
            with open(user_config_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                if isinstance(data, dict):
                    return data
        except:
            return {}
    return {}

def get_backup_root():
    cfg = load_user_config()
    backup_path = cfg.get("backup_path")
    if isinstance(backup_path, str) and backup_path.strip():
        return backup_path
    return os.path.join(STEAM_PATH, "millennium", "backups")

BACKUP_ROOT = get_backup_root()

BACKUP_TARGETS = [
    {"src": os.path.join(STEAM_PATH, "userdata"), "name": "userdata"},
    
    {"src": os.path.join(STEAM_PATH, "appcache", "stats"), "name": "appcache_stats"},
    
    {"src": os.path.join(STEAM_PATH, "depotcache"), "name": "depotcache"},

    {"src": os.path.join(STEAM_PATH, "config", "stplug-in"), "name": "stplug-in"}
]

UI_THEME = {
    "title": "CalyRecall",
    "bg": "#101014",
    "accent": "#8b5cf6"
}
