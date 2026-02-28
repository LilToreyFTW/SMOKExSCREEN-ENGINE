
import cv2
import numpy as np
import tkinter as tk
from tkinter import messagebox
from PIL import Image, ImageTk
import threading

# Explicit MediaPipe imports (fix for newer versions)
from mediapipe.python.solutions import selfie_segmentation


class BodyIsolationApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Depstech AI Body Isolation")
        self.root.geometry("1000x750")
        self.root.configure(bg="#111111")

        self.video_label = tk.Label(self.root, bg="black")
        self.video_label.pack(pady=10)

        self.start_btn = tk.Button(
            self.root,
            text="Start Camera",
            font=("Arial", 14),
            width=18,
            command=self.start_camera
        )
        self.start_btn.pack(pady=5)

        self.stop_btn = tk.Button(
            self.root,
            text="Stop Camera",
            font=("Arial", 14),
            width=18,
            command=self.stop_camera,
            state="disabled"
        )
        self.stop_btn.pack(pady=5)

        self.cap = None
        self.running = False

        # Initialize segmentation model
        self.segmentor = selfie_segmentation.SelfieSegmentation(model_selection=1)

    def start_camera(self):
        self.cap = cv2.VideoCapture(0)
        if not self.cap.isOpened():
            messagebox.showerror("Camera Error", "Webcam not detected.")
            return

        self.running = True
        self.start_btn.config(state="disabled")
        self.stop_btn.config(state="normal")

        threading.Thread(target=self.video_loop, daemon=True).start()

    def stop_camera(self):
        self.running = False
        if self.cap:
            self.cap.release()
        self.video_label.config(image="")
        self.start_btn.config(state="normal")
        self.stop_btn.config(state="disabled")

    def video_loop(self):
        while self.running:
            ret, frame = self.cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

            results = self.segmentor.process(rgb)
            mask = results.segmentation_mask

            condition = mask > 0.6
            output = np.zeros_like(frame)
            output[condition] = frame[condition]

            img = Image.fromarray(cv2.cvtColor(output, cv2.COLOR_BGR2RGB))
            imgtk = ImageTk.PhotoImage(image=img)

            self.video_label.imgtk = imgtk
            self.video_label.configure(image=imgtk)

        self.stop_camera()


if __name__ == "__main__":
    root = tk.Tk()
    app = BodyIsolationApp(root)
    root.mainloop()
