using System;
using System.Runtime.InteropServices;

namespace AppAudioSwitcherUtility.Audio
{
    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        [PreserveSig]
        int GetCount([Out] out uint cProps);

        [PreserveSig]
        int GetAt([In] uint iProp, out PROPERTYKEY pkey);

        PropVariant GetValue([In] ref PROPERTYKEY key);

        [PreserveSig]
        int SetValue([In] ref PROPERTYKEY key, [In] ref PropVariant pv);

        [PreserveSig]
        int Commit();
    }

    static class Ole32
    {
        [DllImport("ole32.dll", PreserveSig = false)]
        public static extern void PropVariantClear(ref PropVariant pvar);
    }

    public static class IPropertyStoreExtensions
    {
        public static T GetValue<T>(this IPropertyStore propStore, PROPERTYKEY key)
        {
            PropVariant pv = default(PropVariant);
            try
            {
                pv = propStore.GetValue(ref key);
                switch (pv.varType)
                {
                    case VarEnum.VT_LPWSTR:
                        return (T)Convert.ChangeType(Marshal.PtrToStringUni(pv.pwszVal), typeof(T));
                    case VarEnum.VT_EMPTY:
                        return default;
                    default: throw new NotImplementedException();
                }
            }
            finally
            {
                Ole32.PropVariantClear(ref pv);
            }
        }
    }
}