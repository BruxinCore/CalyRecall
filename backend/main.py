import os
import sys
import threading
import Millennium

sys.path.append(os.path.dirname(os.path.realpath(__file__)))

from monitor import BackupManager
from server import start_server
from hotkey import HotkeyManager

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
        self.hotkey = None

    def _load(self):
        print("[CalyRecall] Carregando plugin...")

        # Carregar UI
        plugin_root = find_plugin_root()
        js_path = os.path.join(plugin_root, "public", "index.js")
        if os.path.exists(js_path):
            Millennium.add_browser_js(js_path)

        # Iniciar Monitor
        self.monitor = BackupManager()
        self.monitor.start()

        # Iniciar Hotkey Manager (Passar callback para o monitor)
        def _hk_callback():
            if self.monitor:
                self.monitor.force_backup()
        
        self.hotkey = HotkeyManager(_hk_callback)
        # Configurar hotkey inicial (será atualizada pelo servidor se config existir)
        # O servidor vai ler o user_config e chamar hotkey.update()
        
        # Iniciar Servidor (Passar self para acesso ao hotkey manager)
        self.server_thread = threading.Thread(target=start_server, args=(self,), daemon=True)
        self.server_thread.start()

        Millennium.ready()

    def _unload(self):
        print("[CalyRecall] Descarregando...")
        if self.monitor:
            self.monitor.stop()
        if self.hotkey:
            self.hotkey.stop()

plugin = Plugin()
