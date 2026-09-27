using System.Runtime.InteropServices;
using AudioSwitcher.Models;
using NAudio.CoreAudioApi;

namespace AudioSwitcher.Services;

public sealed class AudioDeviceService
{
    public IReadOnlyList<AudioDeviceInfo> GetDevices(DataFlow flow)
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator
            .EnumerateAudioEndPoints(flow, DeviceState.Active)
            .Select(device => new AudioDeviceInfo(device.ID, device.FriendlyName))
            .OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public string? GetDefaultDeviceId(DataFlow flow)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
            return device.ID;
        }
        catch (COMException)
        {
            return null;
        }
    }

    public void ActivateProfile(AudioProfile profile)
    {
        SetDefaultEndpoint(profile.OutputDeviceId);
        SetDefaultEndpoint(profile.InputDeviceId);
    }

    private static void SetDefaultEndpoint(string deviceId)
    {
        var policyConfig = (IPolicyConfig)new PolicyConfigClient();
        try
        {
            foreach (var role in Enum.GetValues<PolicyRole>())
            {
                Marshal.ThrowExceptionForHR(policyConfig.SetDefaultEndpoint(deviceId, role));
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(policyConfig);
        }
    }

    private enum PolicyRole
    {
        Console,
        Multimedia,
        Communications,
    }

    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClient;

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat();
        [PreserveSig] int GetDeviceFormat();
        [PreserveSig] int ResetDeviceFormat();
        [PreserveSig] int SetDeviceFormat();
        [PreserveSig] int GetProcessingPeriod();
        [PreserveSig] int SetProcessingPeriod();
        [PreserveSig] int GetShareMode();
        [PreserveSig] int SetShareMode();
        [PreserveSig] int GetPropertyValue();
        [PreserveSig] int SetPropertyValue();
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, PolicyRole role);
        [PreserveSig] int SetEndpointVisibility();
    }
}
