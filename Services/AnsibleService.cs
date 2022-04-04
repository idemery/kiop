using System.Diagnostics;

namespace kiop.Services
{
    public class AnsibleService
    {
        public async Task ExecuteAsync(string cmd, Action<object, DataReceivedEventArgs> outputHandler)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WorkingDirectory = "/app/ansible";
            psi.FileName = $"/bin/bash";
            psi.Arguments = $"-c \"{cmd}\"";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Process proc = new Process
            {
                StartInfo = psi
            };

            proc.OutputDataReceived += new DataReceivedEventHandler(outputHandler);
            proc.ErrorDataReceived += new DataReceivedEventHandler(outputHandler);
            
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }
    }
}
