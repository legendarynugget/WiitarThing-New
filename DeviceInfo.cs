using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NintrollerLib;

namespace WiitarThing
{
    using static NativeImports;

    public class DeviceInfo
    {
        public string DevicePath { get; set; }
        public ControllerType Type { get; set; }

        public static List<DeviceInfo> GetPaths()
        {
            var result = new List<DeviceInfo>();
            Guid guid;
            int index = 0;

            HidD_GetHidGuid(out guid);
            var hDevInfo = SetupDiGetClassDevs(in guid, null, IntPtr.Zero, (uint)(DIGCF.DeviceInterface | DIGCF.Present));
            if (hDevInfo.IsInvalid) return result;

            try
            {
                SP_DEVICE_INTERFACE_DATA diData = SP_DEVICE_INTERFACE_DATA.Create();

                while (SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, in guid, index, out diData))
                {
                    uint size;
                    SetupDiGetDeviceInterfaceDetail(hDevInfo, in diData, IntPtr.Zero, 0, out size, IntPtr.Zero);

                    SP_DEVICE_INTERFACE_DETAIL_DATA diDetail = SP_DEVICE_INTERFACE_DETAIL_DATA.Create();
                    SP_DEVINFO_DATA deviceInfoData = SP_DEVINFO_DATA.Create();

                    if (SetupDiGetDeviceInterfaceDetail(hDevInfo, in diData, ref diDetail, size, out size, out deviceInfoData))
                    {
                        using (SafeFileHandle handle = CreateFile(
                            diDetail.devicePath,
                            FileAccess.ReadWrite,
                            FileShare.ReadWrite,
                            IntPtr.Zero,
                            FileMode.Open,
                            EFileAttributes.Overlapped,
                            IntPtr.Zero))
                        {
                            if (!handle.IsInvalid)
                            {
                                HIDD_ATTRIBUTES attrib = new HIDD_ATTRIBUTES();
                                attrib.Size = Marshal.SizeOf(attrib);

                                if (HidD_GetAttributes(handle, out attrib))
                                {
                                    if (attrib.VendorID == 0x057e && (attrib.ProductID == 0x0306 || attrib.ProductID == 0x0330))
                                    {
                                        result.Add(new DeviceInfo
                                        {
                                            DevicePath = diDetail.devicePath,
                                            Type = attrib.ProductID == 0x0330 ? ControllerType.ProController : ControllerType.Wiimote
                                        });
                                    }
                                }
                            }
                        }
                    }

                    index++;
                }
            }
            finally
            {
                hDevInfo.Dispose();
            }

            return result;
        }
    }
}