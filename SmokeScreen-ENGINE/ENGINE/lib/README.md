# Fortnite Aimbot Configuration

## Installation Instructions

1. **Install Python 3.8+** with the following packages:
   ```bash
   pip install torch torchvision ultralytics opencv-python pynput mss numpy
   ```

2. **Place the dd40605x64.dll** in the `lib\mouse\` directory

3. **Run fortnite_main.py** to start the aimbot

## Controls

- **F1**: Toggle aimbot on/off
- **F2**: Quit aimbot
- **Mouse**: Automatic aiming when enabled

## Configuration

Edit `lib\config\config.json` to adjust sensitivity settings:

```json
{
    "xy_sens": 6.9,
    "targeting_sens": 1.2,
    "xy_scale": 1.4492753623188406,
    "targeting_scale": 120.77194280276816
}
```

## Features

- **AI-powered target detection** using YOLO neural network
- **Smooth human-like aiming** with customizable sensitivity
- **Adjustable FOV and confidence** settings
- **Trigger bot functionality** for automatic shooting
- **Screen resolution auto-detection**
- **Performance optimized algorithms**

## Troubleshooting

1. **Python not found**: Install Python 3.8+ and add to PATH
2. **DLL not found**: Ensure dd40605x64.dll is in lib\mouse\
3. **CUDA not available**: Install PyTorch with CUDA support
4. **Poor performance**: Check GPU drivers and CUDA installation

## Safety

- Use at your own risk
- This is for educational purposes only
- Follow all game terms of service
