using System;
using System.Runtime.InteropServices;

namespace AppAudioSwitcherUtility.Audio
{
    static class Combase
    {
        [DllImport("combase.dll", PreserveSig = false)]
        public static extern void RoGetActivationFactory(
            [MarshalAs(UnmanagedType.HString)] string activatableClassId,
            [In] ref Guid iid,
            [Out, MarshalAs(UnmanagedType.IInspectable)]
            out Object factory);

        [DllImport("combase.dll", PreserveSig = false)]
        public static extern void WindowsCreateString(
            [MarshalAs(UnmanagedType.LPWStr)] string src,
            [In] uint length,
            [Out] out IntPtr hstring);
    }
    
    public class AudioPolicyConfigFactoryImplForDownlevel : IAudioPolicyConfigFactory
    {
        private readonly IAudioPolicyConfigFactoryVariantForDownlevel _factory;

        internal AudioPolicyConfigFactoryImplForDownlevel()
        {
            var iid = typeof(IAudioPolicyConfigFactoryVariantForDownlevel).GUID;
            Combase.RoGetActivationFactory("Windows.Media.Internal.AudioPolicyConfig", ref iid, out object factory);
            _factory = (IAudioPolicyConfigFactoryVariantForDownlevel)factory;
        }

        public HRESULT ClearAllPersistedApplicationDefaultEndpoints()
        {
            return _factory.ClearAllPersistedApplicationDefaultEndpoints();
        }

        public HRESULT GetPersistedDefaultAudioEndpoint(uint processId, EDataFlow flow, ERole role,
            out string deviceId)
        {
            return _factory.GetPersistedDefaultAudioEndpoint(processId, flow, role, out deviceId);
        }

        public HRESULT SetPersistedDefaultAudioEndpoint(uint processId, EDataFlow flow, ERole role, IntPtr deviceId)
        {
            return _factory.SetPersistedDefaultAudioEndpoint(processId, flow, role, deviceId);
        }
    }
}