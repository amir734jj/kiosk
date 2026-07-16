using System.Diagnostics;
using Serilog;

namespace Kiosk.Agent;

public sealed class ChromiumLauncher(string url)
{
    // Matches our kiosk instance whether it's chromium or google-chrome.
    private const string ProcessMatch = "chrom.*--kiosk";

    // Binary name varies by distro/browser: "chromium" (Bookworm+),
    // "chromium-browser" (Bullseye/older), or Google Chrome on desktops.
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

    public void Kill() => Shell.Run("pkill", "-f", "chrom");

    private static string ResolveBinary()
    {
        foreach (var name in (string[])
            ["chromium", "chromium-browser", "google-chrome-stable", "google-chrome", "chrome"])
        {
            if (!string.IsNullOrWhiteSpace(Shell.Output("which", name)))
            {
                return name;
            }
        }

        Log.Warning("No chromium/google-chrome binary found on PATH — defaulting to 'chromium'");
        return "chromium";
    }
}
