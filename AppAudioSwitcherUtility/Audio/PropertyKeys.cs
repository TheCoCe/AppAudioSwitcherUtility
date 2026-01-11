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
        public static PROPERTYKEY PKEY_Device_FriendlyName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{0xa45c254e, 0xdf1c, 0x4efd, {0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0}}"),
            pid = new UIntPtr(14)
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