using System.Diagnostics;

namespace LogProxy.MakeCertWrapper
{
    public static class CommandLine
    {
        public static int Run(string command, string parameters)
        {
            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = parameters;
                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}
