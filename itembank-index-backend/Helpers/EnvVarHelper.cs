using System.Diagnostics;
using System.Runtime.InteropServices;

namespace itembank_index_backend.Helpers;

// deprecated
public static class EnvVarHelper
{
    public static string? Get(string variableName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);

        Process process = new Process() { StartInfo = new ProcessStartInfo()
        {
            FileName = "bash",
            Arguments = $"-c \"echo ${variableName}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        } };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return String.IsNullOrEmpty(output) ? null : output;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public static void Set(string variableName, string value)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.Machine);
            return;
        }

        Process process = new Process() { StartInfo = new ProcessStartInfo()
        {
            FileName = "bash",
            Arguments = $"-c \"export ${variableName}={value}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        } };

        try
        {
            process.Start();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        catch (Exception ex) {}
    }
}