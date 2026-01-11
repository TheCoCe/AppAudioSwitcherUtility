using System;
using System.Runtime.InteropServices;

namespace AppAudioSwitcherUtility.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public UIntPtr pid;

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var pkey = ((PROPERTYKEY)obj);
            return pkey.fmtid == fmtid && pkey.pid == pid;
        }

        public override int GetHashCode()
        {
            return fmtid.GetHashCode() + pid.GetHashCode();
        }
    }

    public static class PropertyKeys
    {
        public static PROPERTYKEY PKEY_ItemNameDisplay = new PROPERTYKEY
        {
            fmtid = new Guid("{B725F130-47EF-101A-A5F1-02608C9EEBAC}"),
            pid = new UIntPtr(10)
        };

        public static PROPERTYKEY PKEY_AppUserModel_Background = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{86D40B4D-9069-443C-819A-2A54090DCCEC}"),
            pid = new UIntPtr(4)
        };

        public static PROPERTYKEY PKEY_AppUserModel_PackageInstallPath = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}"),
            pid = new UIntPtr(15)
        };

        public static PROPERTYKEY PKEY_Tile_SmallLogoPath = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{86D40B4D-9069-443C-819A-2A54090DCCEC}"),
            pid = new UIntPtr(2)
        };

        public static PROPERTYKEY PKEY_AppUserModel_PackageFullName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}"),
            pid = new UIntPtr(21)
        };

        public static PROPERTYKEY PKEY_Device_FriendlyName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{0xa45c254e, 0xdf1c, 0x4efd, {0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0}}"),
            pid = new UIntPtr(14)
        };

        public static PROPERTYKEY PKEY_AudioEndPoint_Interface = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{a45c254e-df1c-4efd-8020-67d146a850e0}"),
            pid = new UIntPtr(2)
        };

        public static PROPERTYKEY PKEY_AudioEndpoint_PhysicalSpeakers = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{0x1da5d803, 0xd492, 0x4edd, {0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e}}"),
            pid = new UIntPtr(3)
        };

        public static PROPERTYKEY DEVPKEY_Device_DeviceDesc = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{0xa45c254e, 0xdf1c, 0x4efd, {0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0}}"),
            pid = new UIntPtr(2)
        };

        public static PROPERTYKEY DEVPKEY_Device_EnumeratorName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{0xa45c254e, 0xdf1c, 0x4efd, {0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0}}"),
            pid = new UIntPtr(24)
        };

        public static PROPERTYKEY DEVPKEY_DeviceClass_IconPath = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{0x259abffc, 0x50a7, 0x47ce, {0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66}}"),
            pid = new UIntPtr(12)
        };

        public static PROPERTYKEY DEVPKEY_DeviceInterface_FriendlyName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{0x026e516e, 0xb814, 0x414b, {0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22}}"),
            pid = new UIntPtr(2)
        };
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct PropArray
    {
        internal uint cElems;
        internal IntPtr pElems;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct PropVariant
    {
        [FieldOffset(0)] public VarEnum varType;
        [FieldOffset(2)] public ushort wReserved1;
        [FieldOffset(4)] public ushort wReserved2;
        [FieldOffset(6)] public ushort wReserved3;
        [FieldOffset(8)] public byte bVal;
        [FieldOffset(8)] public sbyte cVal;
        [FieldOffset(8)] public ushort uiVal;
        [FieldOffset(8)] public short iVal;
        [FieldOffset(8)] public uint uintVal;
        [FieldOffset(8)] public int intVal;
        [FieldOffset(8)] public ulong ulVal;
        [FieldOffset(8)] public long lVal;
        [FieldOffset(8)] public float fltVal;
        [FieldOffset(8)] public double dblVal;
        [FieldOffset(8)] public short boolVal;
        [FieldOffset(8)] public IntPtr pclsidVal;
        [FieldOffset(8)] public IntPtr pszVal;
        [FieldOffset(8)] public IntPtr pwszVal;
        [FieldOffset(8)] public IntPtr punkVal;
        [FieldOffset(8)] public PropArray ca;
        [FieldOffset(8)] public System.Runtime.InteropServices.ComTypes.FILETIME filetime;
    }
}