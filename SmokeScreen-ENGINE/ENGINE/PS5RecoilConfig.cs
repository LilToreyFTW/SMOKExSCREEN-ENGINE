using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmokeScreenEngine
{
    public class PS5RecoilConfig : Form
    {
        private readonly string _gameType;
        private readonly PS5ControllerManager _ps5Manager;
        
        // UI Controls
        private Label _titleLabel;
        private Label _statusLabel;
        private Label _controllerStatusLabel;
        
        // Recoil Settings
        private Label _recoilStrengthLabel;
        private TrackBar _recoilStrengthSlider;
        private Label _recoilStrengthValue;
        
        private Label _recoilSpeedLabel;
        private TrackBar _recoilSpeedSlider;
        private Label _recoilSpeedValue;
        
        private Label _recoilPatternLabel;
        private ComboBox _recoilPatternCombo;
        
        // PS5 Controller Settings
        private Label _ps5SettingsLabel;
        private CheckBox _enablePS5Controller;
        private Label _triggerSensitivityLabel;
        private TrackBar _triggerSensitivitySlider;
        private Label _triggerSensitivityValue;
        
        private Label _rumbleIntensityLabel;
        private TrackBar _rumbleIntensitySlider;
        private Label _rumbleIntensityValue;
        
        private Label _deadzoneLabel;
        private TrackBar _deadzoneSlider;
        private Label _deadzoneValue;
        
        private Label _gyroControlLabel;
        private CheckBox _enableGyroControl;
        
        private Label _adaptiveTriggerLabel;
        private CheckBox _enableAdaptiveTriggers;
        
        // Control Mode Selection
        private GroupBox _controlModeGroup;
        private RadioButton _mouseKeyboardMode;
        private RadioButton _ps5ControllerMode;
        private RadioButton _hybridMode;
        
        // Action Buttons
        private Button _saveButton;
        private Button _resetButton;
        private Button _calibrateButton;
        private Button _testRumbleButton;
        
        // Settings
        private float _recoilStrength = 50.0f;
        private float _recoilSpeed = 50.0f;
        private string _recoilPattern = "Default";
        private bool _enablePS5 = false;
        private float _triggerSensitivity = 50.0f;
        private float _rumbleIntensity = 50.0f;
        private float _deadzone = 10.0f;
        private bool _enableGyro = false;
        private bool _enableAdaptive = true;
        private string _controlMode = "MouseKeyboard";
        
        public PS5RecoilConfig(string gameType, PS5ControllerManager ps5Manager)
        {
            _gameType = gameType;
            _ps5Manager = ps5Manager;
            InitializeComponent();
            LoadSettings();
            SetupEventHandlers();
        }
        
        private void InitializeComponent()
        {
            Text = $"{_gameType} - PS5 Controller Configuration";
            Size = new Size(600, 700);
            BackColor = Color.FromArgb(32, 36, 44);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            
            // Title
            _titleLabel = new Label
            {
                Text = $"🎮 {_gameType} PS5 Controller Configuration",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(560, 30)
            };
            
            // Status
            _statusLabel = new Label
            {
                Text = "Status: Ready",
                Font = new Font("Consolas", 10),
                ForeColor = Color.Lime,
                Location = new Point(20, 60),
                Size = new Size(200, 20)
            };
            
            _controllerStatusLabel = new Label
            {
                Text = "PS5 Controller: Not Connected",
                Font = new Font("Consolas", 10),
                ForeColor = Color.Red,
                Location = new Point(240, 60),
                Size = new Size(340, 20)
            };
            
            // Control Mode Selection
            _controlModeGroup = new GroupBox
            {
                Text = "Control Mode Selection",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 100),
                Size = new Size(560, 80),
                BackColor = Color.FromArgb(40, 45, 55)
            };
            
            _mouseKeyboardMode = new RadioButton
            {
                Text = "🖱️ Mouse & Keyboard (Default)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(10, 25),
                Size = new Size(250, 20),
                Checked = true
            };
            
            _ps5ControllerMode = new RadioButton
            {
                Text = "🎮 PS5 Controller Only",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(10, 50),
                Size = new Size(250, 20)
            };
            
            _hybridMode = new RadioButton
            {
                Text = "🔄 Hybrid (Mouse + PS5)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(280, 25),
                Size = new Size(250, 20)
            };
            
            _controlModeGroup.Controls.AddRange(new Control[] { _mouseKeyboardMode, _ps5ControllerMode, _hybridMode });
            
            // Recoil Settings
            _recoilStrengthLabel = new Label
            {
                Text = "Recoil Strength:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 200),
                Size = new Size(150, 20)
            };
            
            _recoilStrengthSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Location = new Point(180, 200),
                Size = new Size(200, 45),
                TickFrequency = 10
            };
            
            _recoilStrengthValue = new Label
            {
                Text = "50%",
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(390, 200),
                Size = new Size(50, 20)
            };
            
            _recoilSpeedLabel = new Label
            {
                Text = "Recoil Speed:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 250),
                Size = new Size(150, 20)
            };
            
            _recoilSpeedSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Location = new Point(180, 250),
                Size = new Size(200, 45),
                TickFrequency = 10
            };
            
            _recoilSpeedValue = new Label
            {
                Text = "50%",
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(390, 250),
                Size = new Size(50, 20)
            };
            
            _recoilPatternLabel = new Label
            {
                Text = "Recoil Pattern:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 300),
                Size = new Size(150, 20)
            };
            
            _recoilPatternCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Consolas", 9),
                Location = new Point(180, 300),
                Size = new Size(200, 25)
            };
            
            _recoilPatternCombo.Items.AddRange(new string[] { "Default", "Aggressive", "Smooth", "Burst", "Tap", "Custom" });
            _recoilPatternCombo.SelectedIndex = 0;
            
            // PS5 Controller Settings
            _ps5SettingsLabel = new Label
            {
                Text = "🎮 PS5 Controller Settings",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 350),
                Size = new Size(300, 25)
            };
            
            _enablePS5Controller = new CheckBox
            {
                Text = "Enable PS5 Controller Support",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 380),
                Size = new Size(250, 20)
            };
            
            _triggerSensitivityLabel = new Label
            {
                Text = "RT Trigger Sensitivity:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 410),
                Size = new Size(180, 20)
            };
            
            _triggerSensitivitySlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Location = new Point(210, 410),
                Size = new Size(200, 45),
                TickFrequency = 10
            };
            
            _triggerSensitivityValue = new Label
            {
                Text = "50%",
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(420, 410),
                Size = new Size(50, 20)
            };
            
            _rumbleIntensityLabel = new Label
            {
                Text = "Rumble Intensity:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 460),
                Size = new Size(150, 20)
            };
            
            _rumbleIntensitySlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Location = new Point(180, 460),
                Size = new Size(200, 45),
                TickFrequency = 10
            };
            
            _rumbleIntensityValue = new Label
            {
                Text = "50%",
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(390, 460),
                Size = new Size(50, 20)
            };
            
            _deadzoneLabel = new Label
            {
                Text = "Stick Deadzone:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 510),
                Size = new Size(150, 20)
            };
            
            _deadzoneSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 50,
                Value = 10,
                Location = new Point(180, 510),
                Size = new Size(200, 45),
                TickFrequency = 5
            };
            
            _deadzoneValue = new Label
            {
                Text = "10%",
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.Lime,
                Location = new Point(390, 510),
                Size = new Size(50, 20)
            };
            
            _enableGyroControl = new CheckBox
            {
                Text = "Enable Gyro Aiming",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 560),
                Size = new Size(200, 20)
            };
            
            _enableAdaptiveTriggers = new CheckBox
            {
                Text = "Enable Adaptive Triggers",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 590),
                Size = new Size(200, 20),
                Checked = true
            };
            
            // Action Buttons
            _saveButton = new Button
            {
                Text = "💾 Save Settings",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Location = new Point(20, 630),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            
            _resetButton = new Button
            {
                Text = "🔄 Reset",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                Location = new Point(150, 630),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _resetButton.FlatAppearance.BorderSize = 0;
            
            _calibrateButton = new Button
            {
                Text = "🎯 Calibrate",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                Location = new Point(260, 630),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _calibrateButton.FlatAppearance.BorderSize = 0;
            
            _testRumbleButton = new Button
            {
                Text = "🔊 Test Rumble",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 69, 0),
                ForeColor = Color.White,
                Location = new Point(390, 630),
                Size = new Size(140, 35),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _testRumbleButton.FlatAppearance.BorderSize = 0;
            
            // Add controls to form
            Controls.AddRange(new Control[]
            {
                _titleLabel, _statusLabel, _controllerStatusLabel, _controlModeGroup,
                _recoilStrengthLabel, _recoilStrengthSlider, _recoilStrengthValue,
                _recoilSpeedLabel, _recoilSpeedSlider, _recoilSpeedValue,
                _recoilPatternLabel, _recoilPatternCombo,
                _ps5SettingsLabel, _enablePS5Controller,
                _triggerSensitivityLabel, _triggerSensitivitySlider, _triggerSensitivityValue,
                _rumbleIntensityLabel, _rumbleIntensitySlider, _rumbleIntensityValue,
                _deadzoneLabel, _deadzoneSlider, _deadzoneValue,
                _enableGyroControl, _enableAdaptiveTriggers,
                _saveButton, _resetButton, _calibrateButton, _testRumbleButton
            });
        }
        
        private void SetupEventHandlers()
        {
            // Slider events
            _recoilStrengthSlider.ValueChanged += (s, e) => {
                _recoilStrength = e.Value;
                _recoilStrengthValue.Text = $"{e.Value}%";
            };
            
            _recoilSpeedSlider.ValueChanged += (s, e) => {
                _recoilSpeed = e.Value;
                _recoilSpeedValue.Text = $"{e.Value}%";
            };
            
            _triggerSensitivitySlider.ValueChanged += (s, e) => {
                _triggerSensitivity = e.Value;
                _triggerSensitivityValue.Text = $"{e.Value}%";
            };
            
            _rumbleIntensitySlider.ValueChanged += (s, e) => {
                _rumbleIntensity = e.Value;
                _rumbleIntensityValue.Text = $"{e.Value}%";
            };
            
            _deadzoneSlider.ValueChanged += (s, e) => {
                _deadzone = e.Value;
                _deadzoneValue.Text = $"{e.Value}%";
            };
            
            // Combo box event
            _recoilPatternCombo.SelectedIndexChanged += (s, e) => {
                _recoilPattern = _recoilPatternCombo.SelectedItem?.ToString() ?? "Default";
            };
            
            // Radio button events
            _mouseKeyboardMode.CheckedChanged += (s, e) => {
                if (e.Checked) _controlMode = "MouseKeyboard";
            };
            
            _ps5ControllerMode.CheckedChanged += (s, e) => {
                if (e.Checked) _controlMode = "PS5Controller";
            };
            
            _hybridMode.CheckedChanged += (s, e) => {
                if (e.Checked) _controlMode = "Hybrid";
            };
            
            // Checkbox events
            _enablePS5Controller.CheckedChanged += (s, e) => {
                _enablePS5 = e.Checked;
                UpdatePS5Status();
            };
            
            _enableGyroControl.CheckedChanged += (s, e) => {
                _enableGyro = e.Checked;
            };
            
            _enableAdaptiveTriggers.CheckedChanged += (s, e) => {
                _enableAdaptive = e.Checked;
            };
            
            // Button events
            _saveButton.Click += SaveSettings;
            _resetButton.Click += ResetSettings;
            _calibrateButton.Click += CalibrateController;
            _testRumbleButton.Click += TestRumble;
        }
        
        private void UpdatePS5Status()
        {
            if (_enablePS5)
            {
                if (_ps5Manager.IsConnected())
                {
                    _controllerStatusLabel.Text = "PS5 Controller: Connected ✅";
                    _controllerStatusLabel.ForeColor = Color.Lime;
                    _statusLabel.Text = "Status: PS5 Ready";
                    _statusLabel.ForeColor = Color.Lime;
                }
                else
                {
                    _controllerStatusLabel.Text = "PS5 Controller: Not Connected ❌";
                    _controllerStatusLabel.ForeColor = Color.Red;
                    _statusLabel.Text = "Status: Waiting for PS5";
                    _statusLabel.ForeColor = Color.Yellow;
                }
            }
            else
            {
                _controllerStatusLabel.Text = "PS5 Controller: Disabled";
                _controllerStatusLabel.ForeColor = Color.Gray;
                _statusLabel.Text = "Status: Mouse/Keyboard Mode";
                _statusLabel.ForeColor = Color.White;
            }
        }
        
        private void SaveSettings(object? sender, EventArgs e)
        {
            try
            {
                var settings = new PS5Settings
                {
                    GameType = _gameType,
                    RecoilStrength = _recoilStrength,
                    RecoilSpeed = _recoilSpeed,
                    RecoilPattern = _recoilPattern,
                    ControlMode = _controlMode,
                    EnablePS5 = _enablePS5,
                    TriggerSensitivity = _triggerSensitivity,
                    RumbleIntensity = _rumbleIntensity,
                    Deadzone = _deadzone,
                    EnableGyro = _enableGyro,
                    EnableAdaptive = _enableAdaptive
                };
                
                // Save to file
                string json = System.Text.Json.JsonSerializer.Serialize(settings);
                string filename = $"ps5_config_{_gameType.ToLower().Replace(" ", "_")}.json";
                File.WriteAllText(filename, json);
                
                MessageBox.Show($"PS5 settings saved for {_gameType}!", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine($"[PS5] Settings saved for {_gameType}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[PS5] Error saving settings: {ex.Message}");
            }
        }
        
        private void ResetSettings(object? sender, EventArgs e)
        {
            _recoilStrength = 50.0f;
            _recoilSpeed = 50.0f;
            _recoilPattern = "Default";
            _controlMode = "MouseKeyboard";
            _enablePS5 = false;
            _triggerSensitivity = 50.0f;
            _rumbleIntensity = 50.0f;
            _deadzone = 10.0f;
            _enableGyro = false;
            _enableAdaptive = true;
            
            // Update UI
            _recoilStrengthSlider.Value = 50;
            _recoilSpeedSlider.Value = 50;
            _triggerSensitivitySlider.Value = 50;
            _rumbleIntensitySlider.Value = 50;
            _deadzoneSlider.Value = 10;
            _recoilPatternCombo.SelectedIndex = 0;
            _mouseKeyboardMode.Checked = true;
            _enablePS5Controller.Checked = false;
            _enableGyroControl.Checked = false;
            _enableAdaptiveTriggers.Checked = true;
            
            UpdatePS5Status();
            MessageBox.Show("Settings reset to defaults!", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void CalibrateController(object? sender, EventArgs e)
        {
            if (!_ps5Manager.IsConnected())
            {
                MessageBox.Show("PS5 controller not connected!", "Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                _ps5Manager.CalibrateController();
                MessageBox.Show("PS5 controller calibrated successfully!", "Calibration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Calibration failed: {ex.Message}", "Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void TestRumble(object? sender, EventArgs e)
        {
            if (!_ps5Manager.IsConnected())
            {
                MessageBox.Show("PS5 controller not connected!", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                // Test rumble with current intensity
                int rumbleValue = (int)(_rumbleIntensity * 2.55); // Convert 0-100 to 0-255
                _ps5Manager.SetRumble(rumbleValue, rumbleValue);
                
                // Stop rumble after 1 second
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000;
                timer.Tick += (s, args) => {
                    _ps5Manager.SetRumble(0, 0);
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
                
                MessageBox.Show($"Rumble test complete! Intensity: {_rumbleIntensity}%", "Test Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rumble test failed: {ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadSettings()
        {
            try
            {
                string filename = $"ps5_config_{_gameType.ToLower().Replace(" ", "_")}.json";
                if (File.Exists(filename))
                {
                    string json = File.ReadAllText(filename);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<PS5Settings>(json);
                    
                    if (settings != null)
                    {
                        _recoilStrength = settings.RecoilStrength;
                        _recoilSpeed = settings.RecoilSpeed;
                        _recoilPattern = settings.RecoilPattern ?? "Default";
                        _controlMode = settings.ControlMode ?? "MouseKeyboard";
                        _enablePS5 = settings.EnablePS5;
                        _triggerSensitivity = settings.TriggerSensitivity;
                        _rumbleIntensity = settings.RumbleIntensity;
                        _deadzone = settings.Deadzone;
                        _enableGyro = settings.EnableGyro;
                        _enableAdaptive = settings.EnableAdaptive;
                        
                        // Update UI
                        _recoilStrengthSlider.Value = (int)_recoilStrength;
                        _recoilSpeedSlider.Value = (int)_recoilSpeed;
                        _triggerSensitivitySlider.Value = (int)_triggerSensitivity;
                        _rumbleIntensitySlider.Value = (int)_rumbleIntensity;
                        _deadzoneSlider.Value = (int)_deadzone;
                        _recoilPatternCombo.SelectedItem = _recoilPattern;
                        _enablePS5Controller.Checked = _enablePS5;
                        _enableGyroControl.Checked = _enableGyro;
                        _enableAdaptiveTriggers.Checked = _enableAdaptive;
                        
                        // Set control mode
                        switch (_controlMode)
                        {
                            case "MouseKeyboard":
                                _mouseKeyboardMode.Checked = true;
                                break;
                            case "PS5Controller":
                                _ps5ControllerMode.Checked = true;
                                break;
                            case "Hybrid":
                                _hybridMode.Checked = true;
                                break;
                        }
                        
                        Console.WriteLine($"[PS5] Settings loaded for {_gameType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PS5] Error loading settings: {ex.Message}");
            }
            
            UpdatePS5Status();
        }
        
        // PS5 Settings data structure
        public class PS5Settings
        {
            public string GameType { get; set; } = "";
            public float RecoilStrength { get; set; } = 50.0f;
            public float RecoilSpeed { get; set; } = 50.0f;
            public string? RecoilPattern { get; set; } = "Default";
            public string? ControlMode { get; set; } = "MouseKeyboard";
            public bool EnablePS5 { get; set; } = false;
            public float TriggerSensitivity { get; set; } = 50.0f;
            public float RumbleIntensity { get; set; } = 50.0f;
            public float Deadzone { get; set; } = 10.0f;
            public bool EnableGyro { get; set; } = false;
            public bool EnableAdaptive { get; set; } = true;
        }
        
        public PS5Settings GetSettings()
        {
            return new PS5Settings
            {
                GameType = _gameType,
                RecoilStrength = _recoilStrength,
                RecoilSpeed = _recoilSpeed,
                RecoilPattern = _recoilPattern,
                ControlMode = _controlMode,
                EnablePS5 = _enablePS5,
                TriggerSensitivity = _triggerSensitivity,
                RumbleIntensity = _rumbleIntensity,
                Deadzone = _deadzone,
                EnableGyro = _enableGyro,
                EnableAdaptive = _enableAdaptive
            };
        }
    }
}
