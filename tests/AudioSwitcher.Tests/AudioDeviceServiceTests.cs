using System.Reflection;
using System.Runtime.InteropServices;
using AudioSwitcher.Services;

namespace AudioSwitcher.Tests;

public sealed class AudioDeviceServiceTests
{
    [Fact]
    public void PolicyConfigInterop_UsesMatchingWindows7InterfaceAndClass()
    {
        var serviceType = typeof(AudioDeviceService);
        var clientType = serviceType.GetNestedType("PolicyConfigClient", BindingFlags.NonPublic)!;
        var interfaceType = serviceType.GetNestedType("IPolicyConfig", BindingFlags.NonPublic)!;

        Assert.Equal("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9", clientType.GetCustomAttribute<GuidAttribute>()?.Value);
        Assert.Equal("F8679F50-850A-41CF-9C72-430F290290C8", interfaceType.GetCustomAttribute<GuidAttribute>()?.Value);
        Assert.Contains(interfaceType.GetMethods(), method => method.Name == "ResetDeviceFormat");
    }
}
