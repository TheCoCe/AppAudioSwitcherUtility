using System;
using System.Runtime.InteropServices;

namespace AppAudioSwitcherUtility.Audio
{
    enum AudioSessionState
    {
        Inactive = 0,
        Active = 1,
        Expired = 2
    }
    
    enum AudioSessionDisconnectReason
    {
        DeviceRemoval = 0,
        ServerShutdown = 1,
        FormatChanged = 2,
        SessionLogoff = 3,
        SessionDisconnected = 4,
        ExclusiveModeOverride = 5
    }
    
    [Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionEvents
    {
        void OnDisplayNameChanged([MarshalAs(UnmanagedType.LPWStr)]string NewDisplayName, ref Guid EventContext);
        void OnIconPathChanged([MarshalAs(UnmanagedType.LPWStr)]string NewIconPath, ref Guid EventContext);
        void OnSimpleVolumeChanged(float NewVolume, int NewMute, ref Guid EventContext);
        void OnChannelVolumeChanged(uint ChannelCount, IntPtr afNewChannelVolume, uint ChangedChannel, ref Guid EventContext);
        void OnGroupingParamChanged(ref Guid NewGroupingParam, ref Guid EventContext);
        void OnStateChanged(AudioSessionState NewState);
        void OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason);
    }
    
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl
    {
        AudioSessionState GetState();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetDisplayName();
        void SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, ref Guid EventContext);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetIconPath();
        void SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string Value, ref Guid EventContext);
        Guid GetGroupingParam();
        void SetGroupingParam(ref Guid Override, ref Guid EventContext);
        void RegisterAudioSessionNotification([MarshalAs(UnmanagedType.Interface)] IAudioSessionEvents NewNotifications);
        void UnregisterAudioSessionNotification([MarshalAs(UnmanagedType.Interface)] IAudioSessionEvents NewNotifications);
    }
    
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionEnumerator
    {
        int GetCount();
        [return: MarshalAs(UnmanagedType.Interface)]
        IAudioSessionControl GetSession(int SessionCount);
    }
    
    [Guid("641DD20B-4D41-49CC-ABA3-174B9477BB08")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionNotification
    {
        void OnSessionCreated([MarshalAs(UnmanagedType.Interface)] IAudioSessionControl NewSession);
    }
    
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISimpleAudioVolume
    {
        void SetMasterVolume(float fLevel, ref Guid EventContext);
        void GetMasterVolume(out float pfLevel);
        void SetMute(int bMute, ref Guid EventContext);
        int GetMute();
    }
    
    [Guid("C3B284D4-6D39-4359-B3CF-B56DDB3BB39C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioVolumeDuckNotification
    {
        void OnVolumeDuckNotification([MarshalAs(UnmanagedType.LPWStr)]string sessionID, uint countCommunicationSessions);
        void OnVolumeUnduckNotification([MarshalAs(UnmanagedType.LPWStr)]string sessionID);
    }
    
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionManager2
    {
        void GetAudioSessionControl(ref Guid AudioSessionGuid, uint StreamFlags, [MarshalAs(UnmanagedType.Interface)] out IAudioSessionControl SessionControl);
        void GetSimpleAudioVolume(ref Guid AudioSessionGuid, uint StreamFlags, [MarshalAs(UnmanagedType.Interface)] out ISimpleAudioVolume AudioVolume);
        [return: MarshalAs(UnmanagedType.Interface)]
        IAudioSessionEnumerator GetSessionEnumerator();
        void RegisterSessionNotification([MarshalAs(UnmanagedType.Interface)] IAudioSessionNotification SessionNotification);
        void UnregisterSessionNotification([MarshalAs(UnmanagedType.Interface)] IAudioSessionNotification SessionNotification);
        void RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)]string sessionID, [MarshalAs(UnmanagedType.Interface)] IAudioVolumeDuckNotification duckNotification);
        void UnregisterDuckNotification([MarshalAs(UnmanagedType.Interface)] IAudioVolumeDuckNotification duckNotification);
    }
    
    [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl2
    {
        void GetState(out AudioSessionState pRetVal);
        void GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string pRetVal);
        void SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, ref Guid EventContext);
        void GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string pRetVal);
        void SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string Value, ref Guid EventContext);
        void GetGroupingParam(out Guid pRetVal);
        void SetGroupingParam(ref Guid Override, ref Guid EventContext);
        void RegisterAudioSessionNotification([MarshalAs(UnmanagedType.Interface)] IAudioSessionEvents NewNotifications);
        void UnregisterAudioSessionNotification([MarshalAs(UnmanagedType.Interface)] IAudioSessionEvents NewNotifications);
        void GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string pRetVal);
        void GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string pRetVal);
        [PreserveSig]
        int GetProcessId(out uint processId);
        [PreserveSig]
        HRESULT IsSystemSoundsSession();
        void SetDuckingPreference(int optOut);
    }
}