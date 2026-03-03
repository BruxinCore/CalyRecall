import ctypes
from ctypes import wintypes
import threading

user32 = ctypes.windll.user32

class HotkeyWorker(threading.Thread):
    def __init__(self, callback, modifiers, vk):
        super().__init__(daemon=True)
        self.callback = callback
        self.modifiers = modifiers
        self.vk = vk
        self.active = False
        self.registered = False

    def run(self):
        if not self.vk: return
        self.active = True
        
        # 1 é o ID da hotkey
        if not user32.RegisterHotKey(None, 1, self.modifiers, self.vk):
            print(f"[CalyRecall] Erro: Nao foi possivel registrar hotkey Mod={self.modifiers} VK={self.vk}")
            self.active = False
            return
            
        self.registered = True
        # print(f"[CalyRecall] Hotkey ativa: Mod={self.modifiers} VK={self.vk}")

        msg = wintypes.MSG()
        while self.active:
            res = user32.GetMessageW(ctypes.byref(msg), None, 0, 0)
            if res != 0:
                if msg.message == 0x0312: # WM_HOTKEY
                    if msg.wParam == 1:
                        self.callback()
                user32.TranslateMessage(ctypes.byref(msg))
                user32.DispatchMessageW(ctypes.byref(msg))
            else:
                break

        if self.registered:
            user32.UnregisterHotKey(None, 1)

    def stop(self):
        if self.active and self.ident:
            self.active = False
            user32.PostThreadMessageW(self.ident, 0x0012, 0, 0) # WM_QUIT

class HotkeyManager:
    def __init__(self, callback):
        self.callback = callback
        self.worker = None
        self.current_mod = 0
        self.current_vk = 0

    def update(self, modifiers, vk):
        if self.worker:
            self.worker.stop()
            self.worker.join(timeout=0.5)
            self.worker = None

        self.current_mod = modifiers
        self.current_vk = vk

        if vk > 0:
            self.worker = HotkeyWorker(self.callback, modifiers, vk)
            self.worker.start()

    def stop(self):
        if self.worker:
            self.worker.stop()
