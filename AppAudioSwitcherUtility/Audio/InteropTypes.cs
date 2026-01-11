using System;

namespace AppAudioSwitcherUtility.Audio
{
    public enum HRESULT : uint
    {
        S_OK = 0x0,
        S_FALSE = 0x1,
        AUDCLNT_E_DEVICE_INVALIDATED = 0x88890004,
        AUDCLNT_S_NO_SINGLE_PROCESS = 0x889000d,
        ERROR_NOT_FOUND = 0x80070490,
        ERROR_INSUFFICIENT_BUFFER = 0x7a
    }

    public enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2,
        EDataFlow_enum_count = 3
    }

    [Flags]
    public enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2,
        ERole_enum_count = 3
    }

    public enum DeviceState : uint
    {
        ACTIVE = 0x00000001,
        DISABLED = 0x00000002,
        NOTPRESENT = 0x00000004,
        UNPLUGGED = 0x00000008,
        MASK_ALL = 0x0000000f
    }

    public enum STGM
    {
        STGM_READ = 0,
        STGM_WRITE = 1,
        STGM_READWRITE = 2,
        // ...
    }

    public static class InteropTypeExtensions
    {
        public static EDataFlow StrToDataFlow(string str)
        {
            str = str.ToLower();

            EDataFlow dataFlow;
            switch (str)
            {
                case "capture":
                case "c":
                    dataFlow = EDataFlow.eCapture;
                    break;
                case "all":
                case "a":
                    dataFlow = EDataFlow.eAll;
                    break;
                case "render":
                case "r":
                default:
                    dataFlow = EDataFlow.eRender;
                    break;
            }

            return dataFlow;
        }

        public static DeviceState StrToDeviceState(string str)
        {
            str = str.ToLower();

            DeviceState deviceState;
            switch (str)
            {
                case "active":
                    deviceState = DeviceState.ACTIVE;
                    break;
                case "disabled":
                    deviceState = DeviceState.DISABLED;
                    break;
                case "notpresent":
                    deviceState = DeviceState.NOTPRESENT;
                    break;
                case "unplugged":
                    deviceState = DeviceState.UNPLUGGED;
                    break;
                default:
                    deviceState = DeviceState.MASK_ALL;
                    break;
            }

            return deviceState;
        }
    }
}