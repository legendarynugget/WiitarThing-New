using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;
using NintrollerLib;

namespace WiitarThing
{
    public enum DeviceState
    {
        None = 0,
        Discovered,
        Connected_XInput
    }

    public delegate void ConnectStateChange(DeviceControl sender, DeviceState oldState, DeviceState newState);
    public delegate void ConnectionLost(DeviceControl sender);

    public partial class DeviceControl : UserControl
    {
        #region Members
        private string devicePath;
        private Nintroller device;
        private DeviceState state = DeviceState.None;
        private float rumbleAmount = 0;
        private int rumbleStepCount = 0;
        private readonly int rumbleStepPeriod = 10;
        private readonly float rumbleSlowMult = 0.5f;
        private bool isInitializing = true;
        private int lastBatteryPercentage = -1;
        private DateTime lastBatteryNotificationTime = DateTime.MinValue;

        internal Holders.Holder holder;
        internal Property properties;
        internal int targetXDevice = -1;
        internal bool lowBatteryFired = false;
        internal bool identifying = false;
        internal string dName = "";
        internal Timer updateTimer;

        internal const int UPDATE_SPEED = 15;

        public event ConnectStateChange OnConnectStateChange;
        public event ConnectionLost OnConnectionLost;
        #endregion

        #region Properties
        internal Nintroller Device
        {
            get { return device; }
            set
            {
                if (device != null)
                {
                    device.ExtensionChange -= device_ExtensionChange;
                    device.StateUpdate -= device_StateChange;
                    device.LowBattery -= device_LowBattery;
                }

                device = value;

                if (device != null)
                {
                    device.ExtensionChange += device_ExtensionChange;
                    device.StateUpdate += device_StateChange;
                    device.LowBattery += device_LowBattery;
                }
            }
        }

        internal ControllerType DeviceType { get; private set; }
        internal string DevicePath => devicePath;
        internal bool Connected => device != null && device.Connected;

        internal DeviceState ConnectionState
        {
            get { return state; }
            set
            {
                if (value != state)
                {
                    DeviceState previous = state;
                    SetState(value);
                    OnConnectStateChange?.Invoke(this, previous, value);
                }
            }
        }
        #endregion

        public DeviceControl()
        {
            InitializeComponent();
        }

        public DeviceControl(Nintroller nintroller, string path) : this()
        {
            Device = nintroller;
            devicePath = path;
            Device.Disconnected += device_Disconnected;

            // Probe the controller immediately for attached extensions and battery status
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(150);
                    if (Device != null && Device.DataStream != null && Device.DataStream.Open())
                    {
                        Device.BeginReading();
                        Device.GetStatus();
                        Device.SetPlayerLED(1);
                    }
                }
                catch { }
                finally
                {
                    Task.Delay(2500).ContinueWith(t => isInitializing = false);
                }
            });

            RefreshState();
        }

        public void RefreshState()
        {
            if (state != DeviceState.Connected_XInput)
                ConnectionState = DeviceState.Discovered;

            properties = UserPrefs.Instance.GetDevicePref(devicePath) ?? new Property(devicePath);
            SetName(string.IsNullOrWhiteSpace(properties.name) ? device.Type.ToString() : properties.name);
            ApplyCalibration(properties.calPref, properties.calString ?? "");
            UpdateIcon(device.Type);
            UpdateBatteryUI();
        }

        public void SetName(string newName)
        {
            dName = newName;
            labelName.Content = newName;
        }

        public void Detatch()
        {
            device?.StopReading();
            holder?.Close();
            holder = null;
            targetXDevice = -1;
            lowBatteryFired = false;
            ConnectionState = DeviceState.Discovered;
        }

        public void SetState(DeviceState newState)
        {
            state = newState;
            updateTimer?.Dispose();
            updateTimer = null;

            switch (newState)
            {
                case DeviceState.None:
                case DeviceState.Discovered:
                    btnIdentify.IsEnabled = true;
                    btnProperties.IsEnabled = true;
                    btnXinput.Visibility = Visibility.Visible;
                    btnDetatch.Visibility = Visibility.Collapsed;
                    statusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
                    statusText.Text = "Ready to connect";
                    cardBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4E5058"));
                    break;

                case DeviceState.Connected_XInput:
                    btnIdentify.IsEnabled = true;
                    btnProperties.IsEnabled = true;
                    btnXinput.Visibility = Visibility.Collapsed;
                    btnDetatch.Visibility = Visibility.Visible;
                    statusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    statusText.Text = string.Format("Player {0}", targetXDevice + 1);
                    cardBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    if (!device.Connected)
                    {
                        device.BeginReading();
                    }
                    device.GetStatus();

                    var xHolder = new Holders.XInputHolder(device.Type);
                    LoadProfile(properties.profile, xHolder);
                    xHolder.ConnectXInput(targetXDevice);
                    holder = xHolder;

                    device.SetPlayerLED(targetXDevice + 1);
                    updateTimer = new Timer(HolderUpdate, device, 50, UPDATE_SPEED);
                    break;
            }
        }

        private void device_ExtensionChange(object sender, NintrollerExtensionEventArgs e)
        {
            DeviceType = e.controllerType;
            if (holder != null)
            {
                holder.ClearAllValues();
                holder.ClearAllMappings();
                holder.AddMapping(DeviceType);
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshState();
            }));
        }

        private void device_LowBattery(object sender, LowBatteryEventArgs e)
        {
            SetBatteryStatus(e.batteryLevel == BatteryStatus.Low || e.batteryLevel == BatteryStatus.VeryLow);
        }

        private void device_StateChange(object sender, NintrollerStateEventArgs e)
        {
            if (holder == null) return;
            RumbleStep();

            UpdateBatteryUI();

            switch (e.controllerType)
            {
                case ControllerType.Guitar:
                    var gtr = (Guitar)e.state;
                    holder.SetValue(Guitar.InputNames.G, gtr.G);
                    holder.SetValue(Guitar.InputNames.R, gtr.R);
                    holder.SetValue(Guitar.InputNames.Y, gtr.Y);
                    holder.SetValue(Guitar.InputNames.B, gtr.B);
                    holder.SetValue(Guitar.InputNames.O, gtr.O);
                    holder.SetValue(Guitar.InputNames.UP, gtr.Up);
                    holder.SetValue(Guitar.InputNames.DOWN, gtr.Down);
                    holder.SetValue(Guitar.InputNames.LEFT, gtr.Left);
                    holder.SetValue(Guitar.InputNames.RIGHT, gtr.Right);
                    holder.SetValue(Guitar.InputNames.WHAMMYHIGH, gtr.WhammyHigh);
                    holder.SetValue(Guitar.InputNames.WHAMMYLOW, gtr.WhammyLow);
                    holder.SetValue(Guitar.InputNames.TILTHIGH, gtr.TiltHigh);
                    holder.SetValue(Guitar.InputNames.TILTLOW, gtr.TiltLow);
                    holder.SetValue(Guitar.InputNames.START, gtr.Start);
                    holder.SetValue(Guitar.InputNames.SELECT, gtr.Select);
                    break;

                case ControllerType.Drums:
                    var drm = (Drums)e.state;
                    holder.SetValue(Drums.InputNames.G, drm.G);
                    holder.SetValue(Drums.InputNames.R, drm.R);
                    holder.SetValue(Drums.InputNames.Y, drm.Y);
                    holder.SetValue(Drums.InputNames.B, drm.B);
                    holder.SetValue(Drums.InputNames.O, drm.O);
                    holder.SetValue(Drums.InputNames.BASS, drm.Bass);
                    holder.SetValue(Drums.InputNames.UP, drm.Up);
                    holder.SetValue(Drums.InputNames.DOWN, drm.Down);
                    holder.SetValue(Drums.InputNames.LEFT, drm.Left);
                    holder.SetValue(Drums.InputNames.RIGHT, drm.Right);
                    holder.SetValue(Drums.InputNames.START, drm.Start);
                    holder.SetValue(Drums.InputNames.SELECT, drm.Select);
                    break;

                case ControllerType.ProController:
                    var pro = (ProController)e.state;
                    holder.SetValue(ProController.InputNames.A, pro.A);
                    holder.SetValue(ProController.InputNames.B, pro.B);
                    holder.SetValue(ProController.InputNames.X, pro.X);
                    holder.SetValue(ProController.InputNames.Y, pro.Y);
                    holder.SetValue(ProController.InputNames.UP, pro.Up);
                    holder.SetValue(ProController.InputNames.DOWN, pro.Down);
                    holder.SetValue(ProController.InputNames.LEFT, pro.Left);
                    holder.SetValue(ProController.InputNames.RIGHT, pro.Right);
                    holder.SetValue(ProController.InputNames.L, pro.L);
                    holder.SetValue(ProController.InputNames.R, pro.R);
                    holder.SetValue(ProController.InputNames.ZL, pro.ZL);
                    holder.SetValue(ProController.InputNames.ZR, pro.ZR);
                    holder.SetValue(ProController.InputNames.START, pro.Plus);
                    holder.SetValue(ProController.InputNames.SELECT, pro.Minus);
                    holder.SetValue(ProController.InputNames.HOME, pro.Home);
                    holder.SetValue(ProController.InputNames.LS, pro.LStick);
                    holder.SetValue(ProController.InputNames.RS, pro.RStick);
                    holder.SetValue(ProController.InputNames.LRIGHT, pro.LJoy.X > 0 ? pro.LJoy.X : 0f);
                    holder.SetValue(ProController.InputNames.LLEFT, pro.LJoy.X < 0 ? -pro.LJoy.X : 0f);
                    holder.SetValue(ProController.InputNames.LUP, pro.LJoy.Y > 0 ? pro.LJoy.Y : 0f);
                    holder.SetValue(ProController.InputNames.LDOWN, pro.LJoy.Y < 0 ? -pro.LJoy.Y : 0f);
                    holder.SetValue(ProController.InputNames.RRIGHT, pro.RJoy.X > 0 ? pro.RJoy.X : 0f);
                    holder.SetValue(ProController.InputNames.RLEFT, pro.RJoy.X < 0 ? -pro.RJoy.X : 0f);
                    holder.SetValue(ProController.InputNames.RUP, pro.RJoy.Y > 0 ? pro.RJoy.Y : 0f);
                    holder.SetValue(ProController.InputNames.RDOWN, pro.RJoy.Y < 0 ? -pro.RJoy.Y : 0f);
                    break;

                case ControllerType.Wiimote:
                    var wm = (Wiimote)e.state;
                    SetWiimoteInputs(wm);
                    break;

                case ControllerType.ClassicController:
                    var cc = (ClassicController)e.state;
                    SetWiimoteInputs(cc.wiimote);
                    holder.SetValue(ClassicController.InputNames.A, cc.A);
                    holder.SetValue(ClassicController.InputNames.B, cc.B);
                    holder.SetValue(ClassicController.InputNames.X, cc.X);
                    holder.SetValue(ClassicController.InputNames.Y, cc.Y);
                    holder.SetValue(ClassicController.InputNames.UP, cc.Up);
                    holder.SetValue(ClassicController.InputNames.DOWN, cc.Down);
                    holder.SetValue(ClassicController.InputNames.LEFT, cc.Left);
                    holder.SetValue(ClassicController.InputNames.RIGHT, cc.Right);
                    holder.SetValue(ClassicController.InputNames.ZL, cc.ZL);
                    holder.SetValue(ClassicController.InputNames.ZR, cc.ZR);
                    holder.SetValue(ClassicController.InputNames.LT, cc.L.value);
                    holder.SetValue(ClassicController.InputNames.RT, cc.R.value);
                    holder.SetValue(ClassicController.InputNames.START, cc.Start);
                    holder.SetValue(ClassicController.InputNames.SELECT, cc.Select);
                    holder.SetValue(ClassicController.InputNames.HOME, cc.Home);
                    break;
            }

            holder.Update();
        }

        private void device_Disconnected(object sender, DisconnectedEventArgs e)
        {
            if (isInitializing) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                bool wasActive = (ConnectionState == DeviceState.Connected_XInput);
                Detatch();
                OnConnectionLost?.Invoke(this);

                // Show balloon only if enabled and controller was actively connected
                if (wasActive && UserPrefs.Instance.notifyDisconnect)
                {
                    MainWindow.Instance.ShowBalloon("Connection Lost", string.Format("{0} was disconnected.", dName), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                }
            }));
        }

        private void SetWiimoteInputs(Wiimote wm)
        {
            holder.SetValue(Wiimote.InputNames.A, wm.buttons.A);
            holder.SetValue(Wiimote.InputNames.B, wm.buttons.B);
            holder.SetValue(Wiimote.InputNames.ONE, wm.buttons.One);
            holder.SetValue(Wiimote.InputNames.TWO, wm.buttons.Two);
            holder.SetValue(Wiimote.InputNames.UP, wm.buttons.Up);
            holder.SetValue(Wiimote.InputNames.DOWN, wm.buttons.Down);
            holder.SetValue(Wiimote.InputNames.LEFT, wm.buttons.Left);
            holder.SetValue(Wiimote.InputNames.RIGHT, wm.buttons.Right);
            holder.SetValue(Wiimote.InputNames.MINUS, wm.buttons.Minus);
            holder.SetValue(Wiimote.InputNames.PLUS, wm.buttons.Plus);
            holder.SetValue(Wiimote.InputNames.HOME, wm.buttons.Home);
        }

        private void HolderUpdate(object holderState)
        {
            if (holder == null) return;
            holder.Update();
            RumbleStep();
        }

        private void UpdateBatteryUI()
        {
            if (device == null || !device.Connected) return;

            int percentage = 100;
            switch (device.BatteryLevel)
            {
                case BatteryStatus.VeryHigh:
                    percentage = 100;
                    break;
                case BatteryStatus.High:
                    percentage = 75;
                    break;
                case BatteryStatus.Medium:
                    percentage = 50;
                    break;
                case BatteryStatus.Low:
                    percentage = 25;
                    break;
                case BatteryStatus.VeryLow:
                    percentage = 10;
                    break;
                default:
                    percentage = 100;
                    break;
            }

            if (percentage != lastBatteryPercentage)
            {
                lastBatteryPercentage = percentage;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    batteryPanel.Visibility = Visibility.Visible;
                    batteryText.Text = string.Format("{0}%", percentage);

                    string colorHex = percentage > 50 ? "#10B981" : (percentage > 20 ? "#F59E0B" : "#EF4444");
                    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
                    batteryIcon.Fill = brush;
                    batteryText.Foreground = brush;
                }));
            }
        }

        private void RumbleStep()
        {
            if (identifying || device == null) return;
            bool currentRumbleState = device.RumbleEnabled;

            if (!properties.useRumble || device.Type == ControllerType.Turntable)
            {
                if (currentRumbleState) device.RumbleEnabled = false;
                return;
            }

            rumbleAmount = holder != null ? holder.RumbleAmount : 0;
            float dutyCycle = rumbleAmount < 256 ? rumbleSlowMult * (rumbleAmount / 256f) : (rumbleAmount / 65535f);
            int stopStep = (int)Math.Round(properties.rumbleIntensity * 0.5f * dutyCycle * rumbleStepPeriod);

            device.RumbleEnabled = rumbleStepCount < stopStep;
            rumbleStepCount = (rumbleStepCount + 1) % rumbleStepPeriod;
        }

        private void SetBatteryStatus(bool isLow)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isLow)
                {
                    statusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                    // Trigger toast only if battery notifications are enabled and throttled
                    if (UserPrefs.Instance.notifyBatteryLow && (DateTime.Now - lastBatteryNotificationTime).TotalMinutes > 15)
                    {
                        lastBatteryNotificationTime = DateTime.Now;
                        int pct = lastBatteryPercentage > 0 ? lastBatteryPercentage : 20;
                        MainWindow.Instance.ShowBalloon("Battery Low", string.Format("{0} is running low on battery ({1}%).", dName, pct), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                    }
                }
                else
                {
                    statusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                }
            }));
        }

        private void LoadProfile(string profilePath, Holders.Holder h)
        {
            Profile loadedProfile = null;
            if (!string.IsNullOrWhiteSpace(profilePath) && File.Exists(profilePath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(Profile));
                    using (var stream = File.OpenRead(profilePath))
                    using (var reader = new StreamReader(stream))
                    {
                        loadedProfile = serializer.Deserialize(reader) as Profile;
                    }
                }
                catch { }
            }

            if (loadedProfile == null)
            {
                loadedProfile = UserPrefs.Instance.defaultProfile;
            }

            if (loadedProfile != null)
            {
                for (int i = 0; i < Math.Min(loadedProfile.controllerMapKeys.Count, loadedProfile.controllerMapValues.Count); i++)
                {
                    h.SetMapping(loadedProfile.controllerMapKeys[i], loadedProfile.controllerMapValues[i]);
                }
            }
        }

        private void UpdateIcon(ControllerType cType)
        {
            string key = "WIcon";

            switch (cType)
            {
                case ControllerType.ProController:
                    key = "ProIcon";
                    break;
                case ControllerType.ClassicControllerPro:
                    key = "CCPIcon";
                    break;
                case ControllerType.ClassicController:
                    key = "CCIcon";
                    break;
                case ControllerType.Guitar:
                    key = "GTRIcon";
                    break;
                case ControllerType.Drums:
                    key = "DRMIcon";
                    break;
                case ControllerType.Turntable:
                    key = "TTBIcon";
                    break;
                default:
                    key = "WIcon";
                    break;
            }

            if (Application.Current != null && Application.Current.Resources.Contains(key))
            {
                ImageSource src = Application.Current.Resources[key] as ImageSource;
                if (src != null)
                {
                    icon.Source = src;
                    UserPrefs.Instance.UpdateDeviceIcon(devicePath, key);
                }
            }
        }

        private void ApplyCalibration(Property.CalibrationPreference calPref, string calString)
        {
            switch (calPref)
            {
                case Property.CalibrationPreference.Default:
                    device.SetCalibration(Calibrations.CalibrationPreset.Default);
                    break;
                case Property.CalibrationPreference.More:
                    device.SetCalibration(Calibrations.CalibrationPreset.Modest);
                    break;
                case Property.CalibrationPreference.Extra:
                    device.SetCalibration(Calibrations.CalibrationPreset.Extra);
                    break;
                case Property.CalibrationPreference.Minimal:
                    device.SetCalibration(Calibrations.CalibrationPreset.Minimum);
                    break;
                case Property.CalibrationPreference.Raw:
                    device.SetCalibration(Calibrations.CalibrationPreset.None);
                    break;
            }
        }

        #region UI Events
        private void btnXinput_Click(object sender, RoutedEventArgs e)
        {
            int slot = Holders.XInputHolder.GetFirstAvailableSlot();
            if (slot >= 0)
            {
                AssignToXinputPlayer(slot);
            }
            else
            {
                MessageBox.Show("All 4 Xbox 360 controller slots are currently in use.", "No Slots Available", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AssignToXinputPlayer(int player)
        {
            targetXDevice = player;

            if (!device.Connected)
            {
                device.BeginReading();
            }
            device.GetStatus();

            ConnectionState = DeviceState.Connected_XInput;
            device.SetPlayerLED(player + 1);
            RefreshState();
        }

        private void XOption_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null && int.TryParse(item.Name.Replace("XOption", ""), out int tmp))
            {
                AssignToXinputPlayer(tmp - 1);
            }
        }

        private void btnDetatch_Click(object sender, RoutedEventArgs e)
        {
            Detatch();
        }

        private void btnIdentify_Click(object sender, RoutedEventArgs e)
        {
            if (identifying) return;
            identifying = true;

            Task.Run(() =>
            {
                bool wasConnected = Connected;
                if (wasConnected || (device.DataStream.Open() && device.DataStream.CanRead))
                {
                    if (!wasConnected) device.BeginReading();
                    device.RumbleEnabled = true;

                    for (int i = -3; i < 4; i++)
                    {
                        int led = 4 - Math.Abs(i);
                        device.SetPlayerLED(led);
                        Thread.Sleep(75);
                    }

                    device.RumbleEnabled = false;
                    device.SetPlayerLED(targetXDevice > -1 ? targetXDevice + 1 : 1);

                    if (!wasConnected) device.StopReading();
                }
                identifying = false;
            });
        }

        private void btnProperties_Click(object sender, RoutedEventArgs e)
        {
            var win = new PropWindow(properties, device.Type.ToString());
            win.Owner = Application.Current.MainWindow;

            bool? result = win.ShowDialog();
            if (result == true && win.doSave)
            {
                properties = new Property(win.props);
                SetName(properties.name);
                UserPrefs.Instance.AddDevicePref(properties);
                UserPrefs.SavePrefs();
                RefreshState();
            }
        }
        #endregion
    }
}