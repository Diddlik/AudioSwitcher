using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace AudioSwitcher.Services;

/// <summary>Mutes the default microphone and reports mute changes from any source, such as headset buttons.</summary>
public sealed class MicrophoneMuteService : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;

    /// <summary>Raised on a Windows audio thread.</summary>
    public event Action? MuteChanged;

    public bool IsMuted => _device?.AudioEndpointVolume.Mute == true;

    /// <summary>Follows the current default microphone; call again after the default capture device changes.</summary>
    public void Attach()
    {
        Detach();
        try
        {
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            _device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
        }
        catch (COMException)
        {
            Detach();
        }
    }

    /// <returns>The new mute state, or null when no default microphone exists.</returns>
    public bool? ToggleMute()
    {
        if (_device is null) return null;
        var volume = _device.AudioEndpointVolume;
        volume.Mute = !volume.Mute;
        return volume.Mute;
    }

    public void Dispose()
    {
        Detach();
        _enumerator.Dispose();
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data) => MuteChanged?.Invoke();

    private void Detach()
    {
        if (_device is null) return;
        try
        {
            _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
        }
        catch (COMException)
        {
            // The device may already be gone; releasing it below is still required.
        }

        _device.Dispose();
        _device = null;
    }
}
