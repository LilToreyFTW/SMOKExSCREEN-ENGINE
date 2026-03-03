import logging
import threading
import time
import json
import os
import sys
import numpy as np

import win32api
import win32con
import win32gui
from pynput import mouse
from pynput.mouse import Button, Listener

try:
    from scipy.interpolate import CubicSpline

    HAS_SCIPY = True
except:
    HAS_SCIPY = False

KEY_MAP = {
    "TAB": win32con.VK_TAB,
    "ENTER": win32con.VK_RETURN,
    "CTRL": win32con.VK_CONTROL,
    "SHIFT": win32con.VK_SHIFT,
    "ALT": win32con.VK_MENU,
    "ESC": win32con.VK_ESCAPE,
    "SPACE": win32con.VK_SPACE,
    "LEFT": win32con.VK_LEFT,
    "UP": win32con.VK_UP,
    "RIGHT": win32con.VK_RIGHT,
    "DOWN": win32con.VK_DOWN,
    **{f"F{i}": getattr(win32con, f"VK_F{i}") for i in range(1, 13)},
}

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SETTINGS_FILE = os.path.join(SCRIPT_DIR, "settings.json")


class RecoilSettings:
    def __init__(self, game_name):
        self.game_name = game_name
        self.recoil_y = 35.0
        self.recoil_x = 8.0
        self.sensitivity = 1.0
        self.smoothing = 0.5
        self.shoot_delay = 0.01
        self.max_movement = 500
        self.fov = 90
        self.aim_speed = 1.0
        self.trigger_mode = "hold"
        self.load()

    def load(self):
        if os.path.exists(SETTINGS_FILE):
            try:
                with open(SETTINGS_FILE, "r") as f:
                    data = json.load(f)
                    if self.game_name in data:
                        s = data[self.game_name]
                        self.recoil_y = s.get("recoil_y", 35.0)
                        self.recoil_x = s.get("recoil_x", 8.0)
                        self.sensitivity = s.get("sensitivity", 1.0)
                        self.smoothing = s.get("smoothing", 0.5)
                        self.shoot_delay = s.get("shoot_delay", 0.01)
                        self.max_movement = s.get("max_movement", 500)
                        self.fov = s.get("fov", 90)
                        self.aim_speed = s.get("aim_speed", 1.0)
                        self.trigger_mode = s.get("trigger_mode", "hold")
            except:
                pass

    def save(self):
        data = {}
        if os.path.exists(SETTINGS_FILE):
            try:
                with open(SETTINGS_FILE, "r") as f:
                    data = json.load(f)
            except:
                pass
        data[self.game_name] = {
            "recoil_y": self.recoil_y,
            "recoil_x": self.recoil_x,
            "sensitivity": self.sensitivity,
            "smoothing": self.smoothing,
            "shoot_delay": self.shoot_delay,
            "max_movement": self.max_movement,
            "fov": self.fov,
            "aim_speed": self.aim_speed,
            "trigger_mode": self.trigger_mode,
        }
        try:
            with open(SETTINGS_FILE, "w") as f:
                json.dump(data, f)
        except:
            pass

    def to_dict(self):
        return {
            "recoil_y": self.recoil_y,
            "recoil_x": self.recoil_x,
            "sensitivity": self.sensitivity,
            "smoothing": self.smoothing,
            "shoot_delay": self.shoot_delay,
            "max_movement": self.max_movement,
            "fov": self.fov,
            "aim_speed": self.aim_speed,
            "trigger_mode": self.trigger_mode,
        }


class MouseController:
    def __init__(self):
        self.position = (0, 0)
        self._remainder_x = 0.0
        self._remainder_y = 0.0
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


class RecoilController:
    def __init__(self, game_name):
        self.settings = RecoilSettings(game_name)
        self.mouse_controller = MouseController()
        self.game_name = game_name
        self.script_running = False
        self.lbutton_held = False
        self.rbutton_held = False
        self.shot_count = 0
        self._recoil_thread = None
        self._recoil_lock = threading.Lock()
        self.logger = logging.getLogger(f"Recoil.{game_name}")
        self.previous_cumulative_y = 0.0

    def start(self):
        self.script_running = True
        self.logger.info(f"[{self.game_name}] Recoil controller started")
        if not self._recoil_thread or not self._recoil_thread.is_alive():
            self._recoil_thread = threading.Thread(
                target=self._recoil_loop, daemon=True
            )
            self._recoil_thread.start()

    def stop(self):
        self.script_running = False
        self.logger.info(f"[{self.game_name}] Recoil controller stopped")

    def set_recoil(self, x, y):
        with self._recoil_lock:
            self.settings.recoil_x = x
            self.settings.recoil_y = y
        self.logger.info(f"[{self.game_name}] Recoil set to X:{x} Y:{y}")
        self.settings.save()

    def set_sensitivity(self, value):
        self.settings.sensitivity = value
        self.settings.save()

    def set_smoothing(self, value):
        self.settings.smoothing = value
        self.settings.save()

    def _recoil_loop(self):
        self.logger.info(f"[{self.game_name}] Recoil loop started")
        try:
            while self.script_running and self.rbutton_held and self.lbutton_held:
                self.shot_count += 1

                with self._recoil_lock:
                    recoil_y = self.settings.recoil_y
                    recoil_x = self.settings.recoil_x
                    smoothing = self.settings.smoothing
                    sensitivity = self.settings.sensitivity

                if recoil_y > 0 or recoil_x > 0:
                    progress = min(self.shot_count / 60, 1.0)
                    eased_progress = progress ** (1 / max(smoothing, 0.1))

                    dx = recoil_x * sensitivity * eased_progress
                    dy = recoil_y * sensitivity * eased_progress

                    dx = max(
                        -self.settings.max_movement, min(self.settings.max_movement, dx)
                    )
                    dy = max(
                        -self.settings.max_movement, min(self.settings.max_movement, dy)
                    )

                    if abs(dx) > 0.01 or abs(dy) > 0.01:
                        self.mouse_controller.move(dx, dy)
                        self.previous_cumulative_y = dy

                time.sleep(self.settings.shoot_delay)
        except Exception as e:
            self.logger.error(f"[{self.game_name}] Error in recoil loop: {e}")
        finally:
            self.logger.info(f"[{self.game_name}] Recoil loop ended")

    def get_status(self):
        return {
            "game": self.game_name,
            "running": self.script_running,
            "shots": self.shot_count,
            "settings": self.settings.to_dict(),
        }


class GameRecoilApp:
    def __init__(self, game_name):
        self.game_name = game_name
        self.controller = RecoilController(game_name)
        self.listener = None
        self.running = True
        self.command_queue = []
        self.queue_lock = threading.Lock()

    def on_click(self, x, y, button, pressed):
        if button == Button.right:
            self.controller.rbutton_held = pressed
        elif button == Button.left:
            self.controller.lbutton_held = pressed

        if self.controller.rbutton_held and self.controller.lbutton_held:
            if not self.controller.script_running:
                self.controller.start()
        elif not (self.controller.rbutton_held and self.controller.lbutton_held):
            if self.controller.script_running:
                self.controller.stop()
                self.controller.shot_count = 0

    def process_commands(self):
        while self.running:
            time.sleep(0.1)
            with self.queue_lock:
                while self.command_queue:
                    cmd = self.command_queue.pop(0)
                    self.execute_command(cmd)

    def execute_command(self, cmd):
        try:
            action = cmd.get("action")
            if action == "set_recoil":
                self.controller.set_recoil(cmd.get("x", 0), cmd.get("y", 0))
            elif action == "set_sensitivity":
                self.controller.set_sensitivity(cmd.get("value", 1.0))
            elif action == "set_smoothing":
                self.controller.set_smoothing(cmd.get("value", 0.5))
            elif action == "get_status":
                pass
            elif action == "stop":
                self.running = False
                self.controller.stop()
        except Exception as e:
            print(f"[{self.game_name}] Command error: {e}")

    def send_command(self, cmd):
        with self.queue_lock:
            self.command_queue.append(cmd)

    def run(self):
        logging.basicConfig(
            level=logging.INFO,
            format=f"%(asctime)s - [{self.game_name}] - %(levelname)s - %(message)s",
        )

        print(f"[{self.game_name}] Starting Recoil Controller...")

        self.listener = Listener(on_click=self.on_click)
        self.listener.start()

        cmd_thread = threading.Thread(target=self.process_commands, daemon=True)
        cmd_thread.start()

        try:
            while self.running:
                time.sleep(1)
                status = self.controller.get_status()
                if status["running"]:
                    print(f"[{self.game_name}] Active - Shots: {status['shots']}")
        except KeyboardInterrupt:
            pass
        finally:
            self.controller.stop()
            if self.listener:
                self.listener.stop()
            print(f"[{self.game_name}] Controller stopped.")


if __name__ == "__main__":
    if len(sys.argv) > 1:
        game = sys.argv[1]
    else:
        game = "Warzone"
    app = GameRecoilApp(game)
    app.run()
