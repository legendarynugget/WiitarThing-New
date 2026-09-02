using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using static WiitarThing.NativeImports;

namespace WiitarThing.Windows
{
    public partial class SyncWindow : Window
    {
        public bool Cancelled { get; private set; }
        public int Count { get; private set; }
        public event EventHandler NewDeviceFound;

        private CancellationTokenSource cts;
        private readonly HashSet<ulong> pairedThisSession = new HashSet<ulong>();

        public SyncWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            Task.Run(() => RunSyncLoop(cts.Token));
        }

        public static void RemoveAllWiimotes()
        {
            WiitarDebug.Log("FUNC BEGIN - RemoveAllWiimotes");

            var btRadios = BluetoothRadio.FindAllRadios();
            if (btRadios != null && btRadios.Count > 0)
            {
                foreach (var radio in btRadios)
                {
                    using (radio)
                    {
                        var devices = radio.FindAllDevices(includeUnknown: true, includeConnected: true, includeRemembered: true);
                        if (devices == null || devices.Count == 0)
                            continue;

                        foreach (var device in devices)
                        {
                            if (device.Name != null && device.Name.StartsWith("Nintendo RVL-CNT-01"))
                            {
                                device.Remove();
                            }
                        }
                    }
                }
            }

            WiitarDebug.Log("FUNC END - RemoveAllWiimotes");
        }

        private void RunSyncLoop(CancellationToken token)
        {
            AppendLog("Scanning for Bluetooth Radios...", Colors.DodgerBlue);
            var radios = BluetoothRadio.FindAllRadios();

            if (radios == null || radios.Count == 0)
            {
                AppendLog("Error: No Bluetooth Radios detected! Please ensure Bluetooth is enabled.", Colors.Salmon);
                Dispatcher.Invoke(new Action(() => scanProgress.Visibility = Visibility.Collapsed));
                return;
            }

            AppendLog("Ready! Press the RED SYNC button or hold 1 + 2 on the Wiimote.", Colors.LightGreen);

            while (!token.IsCancellationRequested && !Cancelled)
            {
                foreach (var radio in radios)
                {
                    if (token.IsCancellationRequested) break;

                    BLUETOOTH_RADIO_INFO radioInfo;
                    if (!radio.TryGetInfo(out radioInfo))
                        continue;

                    var devices = radio.FindAllDevices(
                        includeUnknown: true,
                        includeConnected: true,
                        includeRemembered: false,
                        includeAuthenticated: true,
                        issueInquiry: true,
                        timeoutMultiplier: 2
                    );

                    if (devices == null) continue;

                    foreach (var device in devices)
                    {
                        if (token.IsCancellationRequested) break;

                        string name = device.Name;
                        if (string.IsNullOrEmpty(name) || !name.StartsWith("Nintendo RVL-CNT-01"))
                            continue;

                        if (pairedThisSession.Contains(device.Address))
                            continue;

                        AppendLog(string.Format("Found {0}! Pairing...", name), Colors.Yellow);

                        // 1. Try Device MAC PIN first (1+2 method)
                        string devicePin = GetPinFromAddress(device.Address);
                        uint authRes = device.Authenticate(devicePin);

                        // 2. If rejected, try Host Radio MAC PIN (Red Sync button method)
                        if (authRes != 0)
                        {
                            string radioPin = GetPinFromAddress(radioInfo.address);
                            authRes = device.Authenticate(radioPin);
                        }

                        // 3. Force enable the HID service
                        uint hidRes = device.SetServiceState(NativeImports.HidServiceClassGuid, true);

                        if (authRes == 0 || hidRes == 0 || hidRes == 87) // 87 = already claimed by OS HID
                        {
                            pairedThisSession.Add(device.Address);
                            AppendLog(string.Format("Paired with {0}! Waiting for Windows HID bind...", name), Colors.LightGreen);
                            Count++;

                            // Trigger immediate refresh and wait until the device actually appears in DeviceInfo paths
                            bool verified = false;
                            for (int retry = 0; retry < 10; retry++)
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    NewDeviceFound?.Invoke(this, EventArgs.Empty);
                                    if (MainWindow.Instance != null)
                                    {
                                        MainWindow.Instance.RefreshDevices();
                                    }
                                }));

                                Thread.Sleep(400);

                                var paths = DeviceInfo.GetPaths();
                                if (paths != null && paths.Count > 0)
                                {
                                    verified = true;
                                    break;
                                }
                            }

                            if (verified)
                            {
                                AppendLog(string.Format("Successfully synchronized {0}!", name), Colors.LightGreen);
                                Thread.Sleep(800);
                                Dispatcher.BeginInvoke(new Action(() => Close()));
                                return;
                            }
                            else
                            {
                                AppendLog("Device paired. Please press 1+2 again if LEDs turn off.", Colors.Yellow);
                            }
                        }
                        else
                        {
                            AppendLog(string.Format("Pairing failed (Auth: 0x{0:X8}, HID: 0x{1:X8})", authRes, hidRes), Colors.Salmon);
                        }
                    }
                }

                Thread.Sleep(800);
            }

            foreach (var r in radios)
            {
                r.Dispose();
            }
        }

        private static string GetPinFromAddress(ulong address)
        {
            var sb = new StringBuilder();
            byte[] bytes = BitConverter.GetBytes(address);
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < 6; i++)
                    sb.Append((char)bytes[i]);
            }
            else
            {
                for (int i = 7; i >= 2; i--)
                    sb.Append((char)bytes[i]);
            }
            return sb.ToString();
        }

        private void AppendLog(string message, Color color)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var run = new Run(string.Format("[{0:HH:mm:ss}] {1}\n", DateTime.Now, message))
                {
                    Foreground = new SolidColorBrush(color)
                };
                var paragraph = new Paragraph(run) { Margin = new Thickness(0), Padding = new Thickness(0) };
                prompt.Blocks.Add(paragraph);
                promptBoxContainer.ScrollToEnd();
            }));
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Cancelled = true;
            cts?.Cancel();
        }
    }
}