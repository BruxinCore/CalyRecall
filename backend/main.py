import os
import sys
import threading
import Millennium

sys.path.append(os.path.dirname(os.path.realpath(__file__)))

from monitor import BackupManager
from server import start_server

def find_plugin_root():
    current = os.path.abspath(__file__)
    for _ in range(4):
        current = os.path.dirname(current)
        if os.path.exists(os.path.join(current, "plugin.json")):
            return current
    return os.path.dirname(os.path.abspath(__file__))

class Plugin:
    def __init__(self):
        self.monitor = None

    def _load(self):
        print("[CalyRecall] Carregando plugin...")

        plugin_root = find_plugin_root()
        js_path = os.path.join(plugin_root, "public", "index.js")

        if os.path.exists(js_path):
            print(f"[CalyRecall] Injetando UI: {js_path}")
            Millennium.add_browser_js(js_path)
        else:
            print(f"[CalyRecall] ERRO: UI não encontrada em {js_path}")

        self.monitor = BackupManager()
        self.monitor.start()

        self.server_thread = threading.Thread(target=start_server, daemon=True)
        self.server_thread.start()

        Millennium.ready()

    def _unload(self):
        print("[CalyRecall] Descarregando...")
        if self.monitor:
            self.monitor.stop()

plugin = Plugin()
