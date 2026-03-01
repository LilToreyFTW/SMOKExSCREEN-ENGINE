import logging
import threading
import time
import ctypes
from dataclasses import dataclass
from enum import Enum
from typing import Dict, List, Optional, Tuple
import socket
import json
import os

import numpy as np
import win32api
import win32con
import win32gui
from pynput import mouse, keyboard
from pynput.mouse import Button, Listener

script_dir = os.path.dirname(os.path.abspath(__file__))

KEY_MAP = {
    "TAB": win32con.VK_TAB,
    "ENTER": win32con.VK_RETURN,
    "CTRL": win32con.VK_CONTROL,
    "CTRL_L": win32con.VK_LCONTROL,
    "CTRL_R": win32con.VK_RCONTROL,
    "SHIFT": win32con.VK_SHIFT,
    "SHIFT_L": win32con.VK_LSHIFT,
    "SHIFT_R": win32con.VK_RSHIFT,
    "ALT": win32con.VK_MENU,
    "ALT_L": win32con.VK_LMENU,
    "ALT_R": win32con.VK_RMENU,
    "CAPS_LOCK": win32con.VK_CAPITAL,
    "ESC": win32con.VK_ESCAPE,
    "SPACE": win32con.VK_SPACE,
    "LEFT": win32con.VK_LEFT,
    "UP": win32con.VK_UP,
    "RIGHT": win32con.VK_RIGHT,
    "DOWN": win32con.VK_DOWN,
    "DELETE": win32con.VK_DELETE,
    "BACKSPACE": win32con.VK_BACK,
    **{f"F{i}": getattr(win32con, f"VK_F{i}") for i in range(1, 13)},
}


class WarzoneController:
    def __init__(self):
        self.name = "Warzone"
        self.logging = logging.getLogger(__name__)

        self.position = (0, 0)
        self._remainder_x = 0.0
        self._remainder_y = 0.0

        self.script_running = False
        self.lbutton_held = False
        self.rbutton_held = False

        self.recoil_y = 35.0
        self.recoil_x = 8.0
        self.sensitivity = 1.0
        self.smoothing = 0.5
        self.shoot_delay = 0.01

        self._recoil_thread = None
        self.shot_count = 0

        self.update_position()

    def update_position(self):
        self.position = win32gui.GetCursorPos()

    def move(self, dx, dy):
        self._remainder_x += dx
        self._remainder_y += dy

        int_dx = int(self._remainder_x)
        int_dy = int(self._remainder_y)

        if int_dx != 0 or int_dy != 0:
            win32api.mouse_event(win32con.MOUSEEVENTF_MOVE, int_dx, int_dy)
            self._remainder_x -= int_dx
            self._remainder_y -= int_dy
        self.update_position()

    def on_click(self, x, y, button, pressed):
        if button == Button.right:
            self.rbutton_held = pressed
        elif button == Button.left:
            self.lbutton_held = pressed

        if self.rbutton_held and self.lbutton_held:
            if not self.script_running:
                self.start()
        elif not (self.rbutton_held and self.lbutton_held):
            if self.script_running:
                self.stop()

    def start(self):
        self.script_running = True
        self.shot_count = 0
        self.logging.info(f"[{self.name}] Recoil controller started")
        self._recoil_thread = threading.Thread(target=self._recoil_loop, daemon=True)
        self._recoil_thread.start()

    def stop(self):
        self.script_running = False
        self.logging.info(f"[{self.name}] Recoil controller stopped")

    def _recoil_loop(self):
        while self.script_running and self.rbutton_held and self.lbutton_held:
            self.shot_count += 1

            progress = min(self.shot_count / 60, 1.0)
            eased_progress = progress ** (1 / self.smoothing)

            dx = self.recoil_x * self.sensitivity * eased_progress
            dy = self.recoil_y * self.sensitivity * eased_progress

            if abs(dx) > 0.01 or abs(dy) > 0.01:
                self.move(dx, dy)

            time.sleep(self.shoot_delay)

    def set_recoil(self, x, y):
        self.recoil_x = x
        self.recoil_y = y
        self.logging.info(f"[{self.name}] Recoil set to X:{x} Y:{y}")

    def get_status(self):
        return {
            "name": self.name,
            "running": self.script_running,
            "shots": self.shot_count,
            "recoil_x": self.recoil_x,
            "recoil_y": self.recoil_y,
        }


def main():
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    )

    controller = WarzoneController()
    listener = Listener(on_click=controller.on_click)
    listener.start()

    print(f"[Warzone] Controller started. Press ESC to exit.")
    print(f"[Warzone] Hold LEFT + RIGHT mouse button to activate recoil compensation.")

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        controller.stop()
        listener.stop()
        print("[Warzone] Controller stopped.")


if __name__ == "__main__":
    main()
