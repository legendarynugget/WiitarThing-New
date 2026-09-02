using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using NintrollerLib;

namespace WiitarThing.Holders
{
    public class XInputHolder : Holder
    {
        private struct StateReport
        {
            public float A;
            public float B;
            public float X;
            public float Y;
            public float Up;
            public float Down;
            public float Left;
            public float Right;
            public float LeftBumper;
            public float RightBumper;
            public float LeftStickClick;
            public float RightStickClick;
            public float Start;
            public float Back;
            public float Guide;
            public float LeftStickX;
            public float LeftStickY;
            public float RightStickX;
            public float RightStickY;
            public float LeftTrigger;
            public float RightTrigger;
        }

        private static readonly object SlotLock = new object();
        internal static readonly bool[] Available = { true, true, true, true };

        internal int minRumble = 20;
        private XBus bus;
        private bool connected = false;
        private int ID = -1;
        private ushort vid = 0;
        private ushort pid = 0;

        public static int GetFirstAvailableSlot()
        {
            lock (SlotLock)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Available[i])
                        return i;
                }
                return -1;
            }
        }

        public static void ReleaseSlot(int id)
        {
            if (id >= 0 && id < 4)
            {
                lock (SlotLock)
                {
                    Available[id] = true;
                }
            }
        }

        public static Dictionary<string, string> GetDefaultMapping(ControllerType type)
        {
            var result = new Dictionary<string, string>();

            switch (type)
            {
                case ControllerType.ProController:
                    result.Add(ProController.InputNames.A, Inputs.Xbox360.A);
                    result.Add(ProController.InputNames.B, Inputs.Xbox360.B);
                    result.Add(ProController.InputNames.X, Inputs.Xbox360.X);
                    result.Add(ProController.InputNames.Y, Inputs.Xbox360.Y);
                    result.Add(ProController.InputNames.UP, Inputs.Xbox360.UP);
                    result.Add(ProController.InputNames.DOWN, Inputs.Xbox360.DOWN);
                    result.Add(ProController.InputNames.LEFT, Inputs.Xbox360.LEFT);
                    result.Add(ProController.InputNames.RIGHT, Inputs.Xbox360.RIGHT);
                    result.Add(ProController.InputNames.L, Inputs.Xbox360.LB);
                    result.Add(ProController.InputNames.R, Inputs.Xbox360.RB);
                    result.Add(ProController.InputNames.ZL, Inputs.Xbox360.LT);
                    result.Add(ProController.InputNames.ZR, Inputs.Xbox360.RT);
                    result.Add(ProController.InputNames.LUP, Inputs.Xbox360.LUP);
                    result.Add(ProController.InputNames.LDOWN, Inputs.Xbox360.LDOWN);
                    result.Add(ProController.InputNames.LLEFT, Inputs.Xbox360.LLEFT);
                    result.Add(ProController.InputNames.LRIGHT, Inputs.Xbox360.LRIGHT);
                    result.Add(ProController.InputNames.RUP, Inputs.Xbox360.RUP);
                    result.Add(ProController.InputNames.RDOWN, Inputs.Xbox360.RDOWN);
                    result.Add(ProController.InputNames.RLEFT, Inputs.Xbox360.RLEFT);
                    result.Add(ProController.InputNames.RRIGHT, Inputs.Xbox360.RRIGHT);
                    result.Add(ProController.InputNames.LS, Inputs.Xbox360.LS);
                    result.Add(ProController.InputNames.RS, Inputs.Xbox360.RS);
                    result.Add(ProController.InputNames.SELECT, Inputs.Xbox360.BACK);
                    result.Add(ProController.InputNames.START, Inputs.Xbox360.START);
                    result.Add(ProController.InputNames.HOME, Inputs.Xbox360.GUIDE);
                    break;

                case ControllerType.ClassicControllerPro:
                    result.Add(ClassicControllerPro.InputNames.A, Inputs.Xbox360.A);
                    result.Add(ClassicControllerPro.InputNames.B, Inputs.Xbox360.B);
                    result.Add(ClassicControllerPro.InputNames.X, Inputs.Xbox360.X);
                    result.Add(ClassicControllerPro.InputNames.Y, Inputs.Xbox360.Y);
                    result.Add(ClassicControllerPro.InputNames.UP, Inputs.Xbox360.UP);
                    result.Add(ClassicControllerPro.InputNames.DOWN, Inputs.Xbox360.DOWN);
                    result.Add(ClassicControllerPro.InputNames.LEFT, Inputs.Xbox360.LEFT);
                    result.Add(ClassicControllerPro.InputNames.RIGHT, Inputs.Xbox360.RIGHT);
                    result.Add(ClassicControllerPro.InputNames.L, Inputs.Xbox360.LB);
                    result.Add(ClassicControllerPro.InputNames.R, Inputs.Xbox360.RB);
                    result.Add(ClassicControllerPro.InputNames.ZL, Inputs.Xbox360.LT);
                    result.Add(ClassicControllerPro.InputNames.ZR, Inputs.Xbox360.RT);
                    result.Add(ClassicControllerPro.InputNames.LUP, Inputs.Xbox360.LUP);
                    result.Add(ClassicControllerPro.InputNames.LDOWN, Inputs.Xbox360.LDOWN);
                    result.Add(ClassicControllerPro.InputNames.LLEFT, Inputs.Xbox360.LLEFT);
                    result.Add(ClassicControllerPro.InputNames.LRIGHT, Inputs.Xbox360.LRIGHT);
                    result.Add(ClassicControllerPro.InputNames.RUP, Inputs.Xbox360.RUP);
                    result.Add(ClassicControllerPro.InputNames.RDOWN, Inputs.Xbox360.RDOWN);
                    result.Add(ClassicControllerPro.InputNames.RLEFT, Inputs.Xbox360.RLEFT);
                    result.Add(ClassicControllerPro.InputNames.RRIGHT, Inputs.Xbox360.RRIGHT);
                    result.Add(ClassicControllerPro.InputNames.SELECT, Inputs.Xbox360.BACK);
                    result.Add(ClassicControllerPro.InputNames.START, Inputs.Xbox360.START);
                    result.Add(ClassicControllerPro.InputNames.HOME, Inputs.Xbox360.GUIDE);
                    break;

                case ControllerType.ClassicController:
                    result.Add(ClassicController.InputNames.B, Inputs.Xbox360.B);
                    result.Add(ClassicController.InputNames.A, Inputs.Xbox360.A);
                    result.Add(ClassicController.InputNames.Y, Inputs.Xbox360.X);
                    result.Add(ClassicController.InputNames.X, Inputs.Xbox360.Y);
                    result.Add(ClassicController.InputNames.UP, Inputs.Xbox360.UP);
                    result.Add(ClassicController.InputNames.DOWN, Inputs.Xbox360.DOWN);
                    result.Add(ClassicController.InputNames.LEFT, Inputs.Xbox360.LEFT);
                    result.Add(ClassicController.InputNames.RIGHT, Inputs.Xbox360.RIGHT);
                    result.Add(ClassicController.InputNames.ZL, Inputs.Xbox360.LB);
                    result.Add(ClassicController.InputNames.ZR, Inputs.Xbox360.RB);
                    result.Add(ClassicController.InputNames.LT, Inputs.Xbox360.LT);
                    result.Add(ClassicController.InputNames.RT, Inputs.Xbox360.RT);
                    result.Add(ClassicController.InputNames.LUP, Inputs.Xbox360.LUP);
                    result.Add(ClassicController.InputNames.LDOWN, Inputs.Xbox360.LDOWN);
                    result.Add(ClassicController.InputNames.LLEFT, Inputs.Xbox360.LLEFT);
                    result.Add(ClassicController.InputNames.LRIGHT, Inputs.Xbox360.LRIGHT);
                    result.Add(ClassicController.InputNames.RUP, Inputs.Xbox360.RUP);
                    result.Add(ClassicController.InputNames.RDOWN, Inputs.Xbox360.RDOWN);
                    result.Add(ClassicController.InputNames.RLEFT, Inputs.Xbox360.RLEFT);
                    result.Add(ClassicController.InputNames.RRIGHT, Inputs.Xbox360.RRIGHT);
                    result.Add(ClassicController.InputNames.SELECT, Inputs.Xbox360.BACK);
                    result.Add(ClassicController.InputNames.START, Inputs.Xbox360.START);
                    result.Add(ClassicController.InputNames.HOME, Inputs.Xbox360.GUIDE);
                    break;

                case ControllerType.Guitar:
                    result.Add(Guitar.InputNames.G, Inputs.Xbox360.A);
                    result.Add(Guitar.InputNames.R, Inputs.Xbox360.B);
                    result.Add(Guitar.InputNames.Y, Inputs.Xbox360.Y);
                    result.Add(Guitar.InputNames.B, Inputs.Xbox360.X);
                    result.Add(Guitar.InputNames.O, Inputs.Xbox360.LB);
                    result.Add(Guitar.InputNames.UP, Inputs.Xbox360.UP);
                    result.Add(Guitar.InputNames.DOWN, Inputs.Xbox360.DOWN);
                    result.Add(Guitar.InputNames.LEFT, Inputs.Xbox360.LEFT);
                    result.Add(Guitar.InputNames.RIGHT, Inputs.Xbox360.RIGHT);
                    result.Add(Guitar.InputNames.SELECT, Inputs.Xbox360.BACK);
                    result.Add(Guitar.InputNames.START, Inputs.Xbox360.START);
                    result.Add(Guitar.InputNames.HOME, Inputs.Xbox360.GUIDE);
                    result.Add(Guitar.InputNames.WHAMMYLOW, Inputs.Xbox360.RLEFT);
                    result.Add(Guitar.InputNames.WHAMMYHIGH, Inputs.Xbox360.RRIGHT);
                    result.Add(Guitar.InputNames.TILTLOW, Inputs.Xbox360.RDOWN);
                    result.Add(Guitar.InputNames.TILTHIGH, Inputs.Xbox360.RUP);
                    break;

                case ControllerType.Drums:
                    result.Add(Drums.InputNames.G, Inputs.Xbox360.A);
                    result.Add(Drums.InputNames.R, Inputs.Xbox360.B);
                    result.Add(Drums.InputNames.Y, Inputs.Xbox360.Y);
                    result.Add(Drums.InputNames.B, Inputs.Xbox360.X);
                    result.Add(Drums.InputNames.O, Inputs.Xbox360.RB);
                    result.Add(Drums.InputNames.BASS, Inputs.Xbox360.LB);
                    result.Add(Drums.InputNames.UP, Inputs.Xbox360.UP);
                    result.Add(Drums.InputNames.DOWN, Inputs.Xbox360.DOWN);
                    result.Add(Drums.InputNames.LEFT, Inputs.Xbox360.LEFT);
                    result.Add(Drums.InputNames.RIGHT, Inputs.Xbox360.RIGHT);
                    result.Add(Drums.InputNames.SELECT, Inputs.Xbox360.BACK);
                    result.Add(Drums.InputNames.START, Inputs.Xbox360.START);
                    result.Add(Drums.InputNames.HOME, Inputs.Xbox360.GUIDE);
                    break;

                default:
                    result.Add(Wiimote.InputNames.RIGHT, Inputs.Xbox360.UP);
                    result.Add(Wiimote.InputNames.LEFT, Inputs.Xbox360.DOWN);
                    result.Add(Wiimote.InputNames.B, Inputs.Xbox360.A);
                    result.Add(Wiimote.InputNames.DOWN, Inputs.Xbox360.B);
                    result.Add(Wiimote.InputNames.A, Inputs.Xbox360.Y);
                    result.Add(Wiimote.InputNames.ONE, Inputs.Xbox360.X);
                    result.Add(Wiimote.InputNames.TWO, Inputs.Xbox360.LB);
                    result.Add(Wiimote.InputNames.UP, Inputs.Xbox360.BACK);
                    result.Add(Wiimote.InputNames.PLUS, Inputs.Xbox360.START);
                    result.Add(Wiimote.InputNames.MINUS, Inputs.Xbox360.BACK);
                    result.Add(Wiimote.InputNames.HOME, Inputs.Xbox360.GUIDE);
                    result.Add(Wiimote.InputNames.ACC_SHAKE_X, Inputs.Xbox360.RRIGHT);
                    result.Add(Wiimote.InputNames.ACC_SHAKE_Y, Inputs.Xbox360.RRIGHT);
                    result.Add(Wiimote.InputNames.ACC_SHAKE_Z, Inputs.Xbox360.RRIGHT);
                    break;
            }

            return result;
        }

        public XInputHolder()
        {
            Values = new ConcurrentDictionary<string, float>();
            Mappings = new Dictionary<string, string>();
            Flags = new Dictionary<string, bool>();
        }

        public XInputHolder(ControllerType t) : this()
        {
            Mappings = GetDefaultMapping(t);
            SetType(t);
        }

        public void SetType(ControllerType t)
        {
            switch (t)
            {
                case ControllerType.Guitar:
                    vid = 0x1430;
                    pid = 0x4734;
                    break;
                case ControllerType.Drums:
                    vid = 0x1430;
                    pid = 0x0805;
                    break;
                case ControllerType.Turntable:
                    vid = 0x1430;
                    pid = 0x1705;
                    break;
                default:
                    vid = 0;
                    pid = 0;
                    break;
            }

            if (connected && ID >= 0)
            {
                int currentID = ID;
                RemoveXInput(currentID);
                ConnectXInput(currentID);
            }
        }

        public override void Update()
        {
            if (!connected || ID < 0)
                return;

            var controller = bus != null ? bus.GetController(ID) : null;
            if (controller == null)
                return;

            var report = new StateReport();

            foreach (var entry in Mappings)
            {
                float val;
                if (!Values.TryGetValue(entry.Key, out val) || val == 0f)
                    continue;

                switch (entry.Value)
                {
                    case Inputs.Xbox360.A: report.A += val; break;
                    case Inputs.Xbox360.B: report.B += val; break;
                    case Inputs.Xbox360.X: report.X += val; break;
                    case Inputs.Xbox360.Y: report.Y += val; break;
                    case Inputs.Xbox360.UP: report.Up += val; break;
                    case Inputs.Xbox360.DOWN: report.Down += val; break;
                    case Inputs.Xbox360.LEFT: report.Left += val; break;
                    case Inputs.Xbox360.RIGHT: report.Right += val; break;
                    case Inputs.Xbox360.LB: report.LeftBumper += val; break;
                    case Inputs.Xbox360.RB: report.RightBumper += val; break;
                    case Inputs.Xbox360.LS: report.LeftStickClick += val; break;
                    case Inputs.Xbox360.RS: report.RightStickClick += val; break;
                    case Inputs.Xbox360.START: report.Start += val; break;
                    case Inputs.Xbox360.BACK: report.Back += val; break;
                    case Inputs.Xbox360.GUIDE: report.Guide += val; break;
                    case Inputs.Xbox360.LLEFT: report.LeftStickX -= val; break;
                    case Inputs.Xbox360.LRIGHT: report.LeftStickX += val; break;
                    case Inputs.Xbox360.LUP: report.LeftStickY += val; break;
                    case Inputs.Xbox360.LDOWN: report.LeftStickY -= val; break;
                    case Inputs.Xbox360.RLEFT: report.RightStickX -= val; break;
                    case Inputs.Xbox360.RRIGHT: report.RightStickX += val; break;
                    case Inputs.Xbox360.RUP: report.RightStickY += val; break;
                    case Inputs.Xbox360.RDOWN: report.RightStickY -= val; break;
                    case Inputs.Xbox360.LT: report.LeftTrigger += val; break;
                    case Inputs.Xbox360.RT: report.RightTrigger += val; break;
                }
            }

            try
            {
                controller.SetButtonState(Xbox360Button.A, report.A > 0f);
                controller.SetButtonState(Xbox360Button.B, report.B > 0f);
                controller.SetButtonState(Xbox360Button.X, report.X > 0f);
                controller.SetButtonState(Xbox360Button.Y, report.Y > 0f);
                controller.SetButtonState(Xbox360Button.Up, report.Up > 0f);
                controller.SetButtonState(Xbox360Button.Down, report.Down > 0f);
                controller.SetButtonState(Xbox360Button.Left, report.Left > 0f);
                controller.SetButtonState(Xbox360Button.Right, report.Right > 0f);
                controller.SetButtonState(Xbox360Button.LeftShoulder, report.LeftBumper > 0f);
                controller.SetButtonState(Xbox360Button.RightShoulder, report.RightBumper > 0f);
                controller.SetButtonState(Xbox360Button.LeftThumb, report.LeftStickClick > 0f);
                controller.SetButtonState(Xbox360Button.RightThumb, report.RightStickClick > 0f);
                controller.SetButtonState(Xbox360Button.Start, report.Start > 0f);
                controller.SetButtonState(Xbox360Button.Back, report.Back > 0f);
                controller.SetButtonState(Xbox360Button.Guide, report.Guide > 0f);

                controller.SetAxisValue(Xbox360Axis.LeftThumbX, GetRawAxis(report.LeftStickX));
                controller.SetAxisValue(Xbox360Axis.LeftThumbY, GetRawAxis(report.LeftStickY));
                controller.SetAxisValue(Xbox360Axis.RightThumbX, GetRawAxis(report.RightStickX));
                controller.SetAxisValue(Xbox360Axis.RightThumbY, GetRawAxis(report.RightStickY));

                controller.SetSliderValue(Xbox360Slider.LeftTrigger, GetRawTrigger(report.LeftTrigger));
                controller.SetSliderValue(Xbox360Slider.RightTrigger, GetRawTrigger(report.RightTrigger));

                controller.SubmitReport();
            }
            catch { }
        }

        private void OnRumble(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
            int strength = (args.LargeMotor << 8) | args.SmallMotor;
            Flags[Inputs.Flags.RUMBLE] = strength > minRumble;
            RumbleAmount = strength > minRumble ? strength : 0;
        }

        public override void Close()
        {
            if (ID >= 0)
            {
                RemoveXInput(ID);
            }
        }

        public override void AddMapping(ControllerType controller)
        {
            var additional = GetDefaultMapping(controller);
            foreach (var entry in additional)
            {
                SetMapping(entry.Key, entry.Value);
            }
            SetType(controller);
        }

        public bool ConnectXInput(int id)
        {
            if (id < 0 || id > 3)
            {
                return false;
            }

            lock (SlotLock)
            {
                bus = XBus.Default;
                bus.Unplug(id);
                bus.Plugin(id, vid, pid);

                var controller = bus.GetController(id);
                if (controller == null)
                {
                    Available[id] = true;
                    return false;
                }

                Available[id] = false;
                controller.FeedbackReceived += OnRumble;
                ID = id;
                connected = true;
                return true;
            }
        }

        public bool RemoveXInput(int id)
        {
            if (id < 0 || id > 3)
                return false;

            lock (SlotLock)
            {
                Available[id] = true;
                Flags[Inputs.Flags.RUMBLE] = false;
                RumbleAmount = 0;

                if (bus != null)
                {
                    bus.Unplug(id);
                }

                ID = -1;
                connected = false;
                return true;
            }
        }

        public short GetRawAxis(float axis)
        {
            if (axis >= 1f) return short.MaxValue;
            if (axis <= -1f) return short.MinValue;
            return (short)(axis * short.MaxValue);
        }

        public byte GetRawTrigger(float trigger)
        {
            if (trigger >= 1f) return byte.MaxValue;
            if (trigger <= 0f) return 0;
            return (byte)(trigger * byte.MaxValue);
        }
    }

    public class XBus
    {
        private static readonly object BusLock = new object();
        private static XBus defaultInstance;
        private ViGEmClient viGEmClient;
        private readonly Dictionary<int, IXbox360Controller> targets;
        private readonly List<IXbox360Controller> connected;

        public static XBus Default
        {
            get
            {
                lock (BusLock)
                {
                    if (defaultInstance == null)
                        defaultInstance = new XBus();
                    return defaultInstance;
                }
            }
        }

        public XBus()
        {
            try
            {
                viGEmClient = new ViGEmClient();
            }
            catch { }

            targets = new Dictionary<int, IXbox360Controller>();
            connected = new List<IXbox360Controller>();

            if (App.Current != null)
            {
                App.Current.Exit += StopDevice;
            }
        }

        private void StopDevice(object sender, System.Windows.ExitEventArgs e)
        {
            lock (BusLock)
            {
                foreach (var controller in targets.Values)
                {
                    try { controller.Disconnect(); } catch { }
                }
                connected.Clear();
                targets.Clear();

                try
                {
                    viGEmClient?.Dispose();
                }
                catch { }
                viGEmClient = null;
            }
        }

        public void Plugin(int id, ushort vid, ushort pid)
        {
            lock (BusLock)
            {
                if (viGEmClient == null)
                {
                    try { viGEmClient = new ViGEmClient(); } catch { return; }
                }

                if (targets.ContainsKey(id))
                    return;

                IXbox360Controller controller;
                if (vid != 0 && pid != 0)
                    controller = viGEmClient.CreateXbox360Controller(vid, pid);
                else
                    controller = viGEmClient.CreateXbox360Controller();

                controller.AutoSubmitReport = false;
                controller.Connect();
                targets[id] = controller;
                connected.Add(controller);
            }
        }

        public bool Unplug(int id)
        {
            lock (BusLock)
            {
                IXbox360Controller controller;
                if (targets.TryGetValue(id, out controller))
                {
                    try { controller.Disconnect(); } catch { }
                    connected.Remove(controller);
                    targets.Remove(id);
                    return true;
                }
                return false;
            }
        }

        public IXbox360Controller GetController(int id)
        {
            lock (BusLock)
            {
                IXbox360Controller controller;
                return targets.TryGetValue(id, out controller) ? controller : null;
            }
        }
    }
}