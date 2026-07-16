using System.Diagnostics;
using Serilog;

namespace Kiosk.Agent;

public static class Shell
{
    public static int Run(string file, params string[] args)
    {
        try
        {
            using var process = Process.Start(BuildStartInfo(file, args, redirect: true));
            if (process is null)
            {
                return -1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Shell command failed: {File} {Args}", file, string.Join(' ', args));
            return -1;
        }
    }

    public static Process? Start(string file, params string[] args)
    {
        try
        {
            return Process.Start(BuildStartInfo(file, args, redirect: false));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to start: {File} {Args}", file, string.Join(' ', args));
            return null;
        }
    }

    public static string? Output(string file, params string[] args)
    {
        try
        {
            using var process = Process.Start(BuildStartInfo(file, args, redirect: true));
            if (process is null)
            {
                return null;
            }

            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? stdout.Trim() : null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Shell output command failed: {File} {Args}", file, string.Join(' ', args));
            return null;
        }
    }

    private static ProcessStartInfo BuildStartInfo(string file, string[] args, bool redirect)
    {
        var psi = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            RedirectStandardOutput = redirect,
            RedirectStandardError = redirect
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        return psi;
    }
}
