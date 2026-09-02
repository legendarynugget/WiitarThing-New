using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Nefarius.ViGEm.Client;
using NintrollerLib;

namespace WiitarThing
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        private readonly List<DeviceControl> deviceList = new List<DeviceControl>();
        private readonly object syncLock = new object();
        private bool isRefreshing = false;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            // Check for ViGEmBus
            try
            {
                using (var client = new ViGEmClient())
                {
                    // ViGEm initialized
                }
            }
            catch (Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException)
            {
                if (MessageBox.Show(
                    "WiitarThing requires ViGEmBus driver to emulate Xbox 360 controllers.\n\nWould you like to open the ViGEmBus download page now?",
                    "Driver Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("https://github.com/nefarius/ViGEmBus/releases") { UseShellExecute = true });
                }
                Application.Current.Shutdown();
                return;
            }

            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            Title = "WiitarThing New v2.7.1";
            menu_version.Header = string.Format("Version {0}", version);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceListener.Instance.RegisterDeviceNotification(this, DeviceListener.GuidInterfaceHID);
            DeviceListener.Instance.RegisterDeviceNotification(this, DeviceListener.GuidInterfaceBT);
            DeviceListener.Instance.OnDevicesUpdated += () => Dispatcher.BeginInvoke(new Action(RefreshDevices));

            if (UserPrefs.Instance.startMinimized)
            {
                WindowState = WindowState.Minimized;
            }

            menu_AutoStart.IsChecked = UserPrefs.AutoStart;
            menu_StartMinimized.IsChecked = UserPrefs.Instance.startMinimized;
            menu_AutoRefresh.IsChecked = UserPrefs.Instance.autoRefresh;

            // Notification preferences
            menu_NotifyAll.IsChecked = UserPrefs.Instance.enableNotifications;
            menu_NotifyBattery.IsChecked = UserPrefs.Instance.notifyBatteryLow;
            menu_NotifyDisconnect.IsChecked = UserPrefs.Instance.notifyDisconnect;

            RefreshDevices();
        }

        public void ShowWindow()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                trayIcon.Visibility = Visibility.Hidden;
                Show();
                WindowState = WindowState.Normal;
                Topmost = true;
                Topmost = false;
                Focus();
                Activate();
            }));
        }

        public void ShowBalloon(string title, string message, BalloonIcon icon, SystemSound sound = null)
        {
            if (!UserPrefs.Instance.enableNotifications)
                return;

            trayIcon.Visibility = Visibility.Visible;
            trayIcon.ShowBalloonTip(title, message, icon);
            sound?.Play();

            Task.Delay(5000).ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    trayIcon.Visibility = WindowState == WindowState.Minimized ? Visibility.Visible : Visibility.Hidden;
                }));
            });
        }

        public void RefreshDevices()
        {
            lock (syncLock)
            {
                if (isRefreshing) return;
                isRefreshing = true;
            }

            Task.Run(() =>
            {
                try
                {
                    var paths = DeviceInfo.GetPaths();
                    var shareMode = UserPrefs.Instance.greedyMode ? FileShare.None : FileShare.ReadWrite;

                    Dispatcher.Invoke(new Action(() =>
                    {
                        // Clean up inactive devices
                        for (int i = deviceList.Count - 1; i >= 0; i--)
                        {
                            var dev = deviceList[i];
                            bool exists = paths.Exists(p => p.DevicePath == dev.DevicePath);
                            if (!exists && dev.ConnectionState != DeviceState.Connected_XInput)
                            {
                                groupAvailable.Children.Remove(dev);
                                groupXinput.Children.Remove(dev);
                                deviceList.RemoveAt(i);
                            }
                        }

                        // Add new incoming devices
                        foreach (var info in paths)
                        {
                            var existing = deviceList.Find(d => d.DevicePath == info.DevicePath);
                            if (existing == null)
                            {
                                if (HidDeviceStream.TryCreate(info.DevicePath, out var stream, shareMode))
                                {
                                    var nintroller = new Nintroller(stream, info.Type);
                                    var control = new DeviceControl(nintroller, info.DevicePath);
                                    control.OnConnectStateChange += DeviceControl_OnConnectStateChange;
                                    control.OnConnectionLost += DeviceControl_OnConnectionLost;
                                    deviceList.Add(control);

                                    groupAvailable.Children.Add(control);

                                    // Automatic connection handling
                                    if (control.properties != null && control.properties.autoConnect)
                                    {
                                        int slot = control.properties.autoNum > 0 ? control.properties.autoNum - 1 : Holders.XInputHolder.GetFirstAvailableSlot();
                                        if (slot >= 0 && Holders.XInputHolder.Available[slot])
                                        {
                                            control.targetXDevice = slot;
                                            control.ConnectionState = DeviceState.Connected_XInput;
                                        }
                                    }
                                }
                            }
                            else if (!existing.Connected)
                            {
                                existing.RefreshState();
                            }
                        }
                    }));
                }
                finally
                {
                    lock (syncLock)
                    {
                        isRefreshing = false;
                    }
                }
            });
        }

        private void DeviceControl_OnConnectStateChange(DeviceControl sender, DeviceState oldState, DeviceState newState)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (oldState == DeviceState.Discovered)
                    groupAvailable.Children.Remove(sender);
                else if (oldState == DeviceState.Connected_XInput)
                    groupXinput.Children.Remove(sender);

                if (newState == DeviceState.Discovered)
                {
                    if (!groupAvailable.Children.Contains(sender))
                        groupAvailable.Children.Add(sender);
                }
                else if (newState == DeviceState.Connected_XInput)
                {
                    if (!groupXinput.Children.Contains(sender))
                        groupXinput.Children.Add(sender);
                }
            }));
        }

        private void DeviceControl_OnConnectionLost(DeviceControl sender)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                groupAvailable.Children.Remove(sender);
                groupXinput.Children.Remove(sender);
                deviceList.Remove(sender);
            }));
        }

        private void btnSync_Click(object sender, RoutedEventArgs e)
        {
            var sync = new Windows.SyncWindow { Owner = this };
            sync.NewDeviceFound += (s, args) => RefreshDevices();
            sync.ShowDialog();
            RefreshDevices();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private void btnDetatchAllXInput_Click(object sender, RoutedEventArgs e)
        {
            foreach (var d in deviceList.ToArray())
            {
                if (d.ConnectionState == DeviceState.Connected_XInput)
                    d.Detatch();
            }
        }

        private void btnRemoveAllWiimotes_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Unpair and remove all Nintendo controllers from Windows Bluetooth registry?", "Unpair All", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var dlg = new Windows.RemoveAllWiimotesWindow { Owner = this };
                dlg.ShowDialog();
                RefreshDevices();
            }
        }

        private void buttonTestInputs_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("joy.cpl"); } catch { }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (btnSettings.ContextMenu != null)
                btnSettings.ContextMenu.IsOpen = true;
        }

        private void menu_AutoStart_Click(object sender, RoutedEventArgs e)
        {
            menu_AutoStart.IsChecked = !menu_AutoStart.IsChecked;
            UserPrefs.AutoStart = menu_AutoStart.IsChecked;
            UserPrefs.SavePrefs();
        }

        private void menu_StartMinimized_Click(object sender, RoutedEventArgs e)
        {
            menu_StartMinimized.IsChecked = !menu_StartMinimized.IsChecked;
            UserPrefs.Instance.startMinimized = menu_StartMinimized.IsChecked;
            UserPrefs.SavePrefs();
        }

        private void menu_AutoRefresh_Click(object sender, RoutedEventArgs e)
        {
            menu_AutoRefresh.IsChecked = !menu_AutoRefresh.IsChecked;
            UserPrefs.Instance.autoRefresh = menu_AutoRefresh.IsChecked;
            UserPrefs.SavePrefs();
        }

        private void menu_NotifyAll_Click(object sender, RoutedEventArgs e)
        {
            menu_NotifyAll.IsChecked = !menu_NotifyAll.IsChecked;
            UserPrefs.Instance.enableNotifications = menu_NotifyAll.IsChecked;
            UserPrefs.SavePrefs();
        }

        private void menu_NotifyBattery_Click(object sender, RoutedEventArgs e)
        {
            menu_NotifyBattery.IsChecked = !menu_NotifyBattery.IsChecked;
            UserPrefs.Instance.notifyBatteryLow = menu_NotifyBattery.IsChecked;
            UserPrefs.SavePrefs();
        }

        private void menu_NotifyDisconnect_Click(object sender, RoutedEventArgs e)
        {
            menu_NotifyDisconnect.IsChecked = !menu_NotifyDisconnect.IsChecked;
            UserPrefs.Instance.notifyDisconnect = menu_NotifyDisconnect.IsChecked;
            UserPrefs.SavePrefs();
        }

        private void menu_Github_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/legendarynugget/WiitarThing-New") { UseShellExecute = true });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                trayIcon.Visibility = Visibility.Visible;
                Hide();
            }
        }

        private void trayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void MenuItem_Show_Click(object sender, RoutedEventArgs e) => ShowWindow();
        private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e) => RefreshDevices();
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DeviceListener.Instance.UnregisterDeviceNotification();
            foreach (var d in deviceList)
            {
                d.Detatch();
            }
        }
    }

    public class ShowWindowCommand : ICommand
    {
        public void Execute(object parameter)
        {
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.ShowWindow();
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
    }
}