using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace WiitarThing
{
    public class UserPrefs
    {
        private static readonly object FileLock = new object();
        private static UserPrefs _instance;

        public static UserPrefs Instance
        {
            get
            {
                if (_instance == null)
                {
                    LoadOrCreate();
                }
                return _instance;
            }
        }

        public static string DataPath { get; private set; }

        public static bool AutoStart
        {
            get { return Instance.autoStartup; }
            set
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (value)
                        {
                            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                            key?.SetValue("WiitarThing", $"\"{exePath}\"");
                        }
                        else
                        {
                            key?.DeleteValue("WiitarThing", false);
                        }
                    }
                }
                catch { }

                Instance.autoStartup = value;
            }
        }

        public List<Property> devicePrefs = new List<Property>();
        public Profile defaultProfile = new Profile();
        public Property defaultProperty;
        public bool autoStartup = false;
        public bool startMinimized = false;
        public bool greedyMode = false;
        public bool toshibaMode = false;
        public bool autoRefresh = true;

        // Notification Controls
        public bool enableNotifications = true;
        public bool notifyBatteryLow = true;
        public bool notifyDisconnect = true;

        public UserPrefs() { }

        private static void LoadOrCreate()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prefs.config");
            string appDataConfig = Path.Combine(appData, "WiitarThing_prefs.config");

            DataPath = File.Exists(localConfig) ? localConfig : appDataConfig;

            if (!LoadPrefs())
            {
                _instance = new UserPrefs();
                SavePrefs();
            }
        }

        public static bool LoadPrefs()
        {
            lock (FileLock)
            {
                if (!File.Exists(DataPath)) return false;

                try
                {
                    var serializer = new XmlSerializer(typeof(UserPrefs));
                    using (FileStream stream = File.OpenRead(DataPath))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        _instance = serializer.Deserialize(reader) as UserPrefs;
                        return _instance != null;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool SavePrefs()
        {
            lock (FileLock)
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(UserPrefs));
                    using (FileStream stream = File.Create(DataPath))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        serializer.Serialize(writer, _instance);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public Property GetDevicePref(string hid)
        {
            return devicePrefs.Find(p => p.hid == hid) ?? defaultProperty;
        }

        public void AddDevicePref(Property property)
        {
            int index = devicePrefs.FindIndex(p => p.hid == property.hid);
            if (index >= 0)
            {
                devicePrefs[index] = property;
            }
            else
            {
                devicePrefs.Add(property);
            }
        }

        public void UpdateDeviceIcon(string path, string icon)
        {
            var prop = devicePrefs.Find(p => p.hid == path);
            if (prop != null)
            {
                prop.lastIcon = icon;
                SavePrefs();
            }
        }
    }

    public class Property
    {
        public enum ProfHolderType { XInput = 0, DInput = 1 }
        public enum CalibrationPreference { Raw = -2, Minimal = -1, Default = 0, More = 1, Extra = 2, Custom = 3 }
        public enum PointerOffScreenMode { Center = 0, SnapX = 1, SnapY = 2, SnapXY = 3 }

        public string hid = "";
        public string name = "";
        public string lastIcon = "";
        public bool autoConnect = false;
        public bool useRumble = true;
        public int autoNum = 0;
        public int rumbleIntensity = 2;
        public ProfHolderType connType = ProfHolderType.XInput;
        public string profile = "";
        public CalibrationPreference calPref = CalibrationPreference.Default;
        public string calString = "";
        public PointerOffScreenMode pointerMode = PointerOffScreenMode.Center;

        public Property() { }
        public Property(string id) { hid = id; }
        public Property(Property copy)
        {
            hid = copy.hid;
            name = copy.name;
            lastIcon = copy.lastIcon;
            autoConnect = copy.autoConnect;
            useRumble = copy.useRumble;
            autoNum = copy.autoNum;
            rumbleIntensity = copy.rumbleIntensity;
            connType = copy.connType;
            profile = copy.profile;
            calPref = copy.calPref;
            calString = copy.calString;
            pointerMode = copy.pointerMode;
        }
    }

    public class Profile
    {
        public enum HolderType { XInput = 0, DInput = 1 }

        public NintrollerLib.ControllerType profileType = NintrollerLib.ControllerType.Guitar;
        public HolderType connType = HolderType.XInput;
        public List<string> controllerMapKeys = new List<string>();
        public List<string> controllerMapValues = new List<string>();

        public Profile() { }
        public Profile(NintrollerLib.ControllerType type) { profileType = type; }
    }
}