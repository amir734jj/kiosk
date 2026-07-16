using System.Diagnostics;
using Serilog;

namespace Kiosk.Agent;

public sealed class ChromiumLauncher(string url)
{
    private const string ProcessMatch = "chromium.*--kiosk";

    // Binary name varies across Raspberry Pi OS: "chromium" (Bookworm+) vs
    // "chromium-browser" (Bullseye and older).
    private readonly string _binary = ResolveBinary();

    private static readonly string[] Flags =
    [
        "--noerrdialogs",
        "--disable-infobars",
        "--password-store=basic",
        "--kiosk",
        "--disable-translate",
        "--disable-features=TranslateUI",
        "--disable-session-crashed-bubble",
        "--disable-component-update",
        "--no-first-run",
        "--start-fullscreen",
        "--autoplay-policy=no-user-gesture-required"
    ];

    /// <summary>Kills any existing Chromium and launches a fresh kiosk instance.</summary>
    public void Launch()
    {
        Kill();
        Thread.Sleep(2000);

        var args = new List<string>(Flags) { url };
        var process = Shell.Start(_binary, [.. args]);

        if (process is not null)
        {
            Log.Information("Launched {Binary} (pid {Pid}) -> {Url}", _binary, process.Id, url);
        }
        else
        {
            Log.Error("Failed to launch {Binary} for {Url}", _binary, url);
        }
    }

    public bool IsRunning() => Shell.Run("pgrep", "-f", ProcessMatch) == 0;

    public void Refresh() => Shell.Run("xdotool", "key", "F5");

    public void Kill() => Shell.Run("pkill", "-f", "chromium");

    private static string ResolveBinary()
    {
        foreach (var name in (string[])["chromium", "chromium-browser"])
        {
            if (!string.IsNullOrWhiteSpace(Shell.Output("which", name)))
            {
                return name;
            }
        }

        Log.Warning("Neither 'chromium' nor 'chromium-browser' found on PATH — defaulting to 'chromium'");
        return "chromium";
    }
}
