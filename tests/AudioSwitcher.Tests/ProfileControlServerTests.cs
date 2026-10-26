using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using AudioSwitcher.Services;

namespace AudioSwitcher.Tests;

public sealed class ProfileControlServerTests
{
    private static readonly Guid ProfileId = Guid.NewGuid();

    [Fact]
    public async Task Requests_AreAnsweredLineByLineOnOneConnection()
    {
        var received = new List<ControlRequest>();
        var pipeName = $"AudioSwitcher.Tests.{Guid.NewGuid():N}";
        using var server = new ProfileControlServer(request =>
        {
            received.Add(request);
            return Task.FromResult(new ControlResponse(true, null, [new ControlProfile(ProfileId, "Headset", true)], ProfileId, true));
        }, pipeName);
        server.Start();

        await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);
        using var reader = new StreamReader(client, new UTF8Encoding(false), false, 1024, leaveOpen: true);
        await using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };

        await writer.WriteAsync("{\"command\":\"state\"}\n");
        var state = JsonDocument.Parse((await reader.ReadLineAsync())!).RootElement;
        await writer.WriteAsync($"{{\"command\":\"activate\",\"profileId\":\"{ProfileId}\"}}\r\n");
        var activated = JsonDocument.Parse((await reader.ReadLineAsync())!).RootElement;

        Assert.True(state.GetProperty("ok").GetBoolean());
        Assert.Equal("Headset", state.GetProperty("profiles")[0].GetProperty("name").GetString());
        Assert.Equal(ProfileId, activated.GetProperty("activeProfileId").GetGuid());
        Assert.True(state.GetProperty("microphoneMuted").GetBoolean());
        Assert.Equal(["state", "activate"], received.Select(request => request.Command));
        Assert.Equal(ProfileId, received[1].ProfileId);
    }

    [Fact]
    public async Task InvalidRequest_IsRejectedWithoutCallingTheHandler()
    {
        var called = false;
        var pipeName = $"AudioSwitcher.Tests.{Guid.NewGuid():N}";
        using var server = new ProfileControlServer(_ =>
        {
            called = true;
            return Task.FromResult(new ControlResponse(true, null, [], null));
        }, pipeName);
        server.Start();

        await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);
        using var reader = new StreamReader(client, new UTF8Encoding(false), false, 1024, leaveOpen: true);
        await using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };

        await writer.WriteAsync("not json\n");
        var response = JsonDocument.Parse((await reader.ReadLineAsync())!).RootElement;

        Assert.False(response.GetProperty("ok").GetBoolean());
        Assert.False(called);
    }
}
