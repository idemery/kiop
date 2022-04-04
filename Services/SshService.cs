using Renci.SshNet;

namespace kiop.Services
{
    public class SshService : IDisposable
    {
        private readonly string hostIp;
        private readonly string username;
        private readonly string password;
        private readonly Action<ScriptOutputLine> outputHandler;
        SshClient sshClient;

        public SshService(string hostIp, string username, string password, Action<ScriptOutputLine> outputHandler)
        {
            this.hostIp = hostIp;
            this.username = username;
            this.password = password;
            this.outputHandler = outputHandler;
            sshClient = new SshClient(hostIp, username, password);
        }

        public void Dispose()
        {
            if (sshClient != null)
            {
                if (sshClient.IsConnected)
                {
                    sshClient.Disconnect();
                }

                sshClient.Dispose();
            }
        }

        public async Task ExecuteAsync(string cmd, bool noOutput = false)
        {
            if (!sshClient.IsConnected)
            {
                sshClient.Connect();
            }

            var outputs = new Progress<ScriptOutputLine>(outputHandler);

            using (var command = sshClient.RunCommand(cmd))
            {
                if (noOutput)
                {
                    command.Execute();
                }
                else
                {
                    await command.ExecuteAsync(outputs, CancellationToken.None);
                }
            }
        }

        public void CopySshPubKeyToHost()
        {
            using (ScpClient client = new ScpClient(hostIp, username, password))
            {
                client.Connect();

                using (Stream localFile = File.OpenRead("/root/.ssh/id_rsa.pub"))
                {
                    client.Upload(localFile, "/root/my_id_rsa.pub");
                }
            }
        }
    }

    public static class SshCommandExtensions
    {
        public static async Task ExecuteAsync(
            this SshCommand sshCommand,
            IProgress<ScriptOutputLine> progress,
            CancellationToken cancellationToken)
        {
            var asyncResult = sshCommand.BeginExecute();
            var stdoutReader = new StreamReader(sshCommand.OutputStream);
            var stderrReader = new StreamReader(sshCommand.ExtendedOutputStream);

            var stderrTask = CheckOutputAndReportProgressAsync(sshCommand, asyncResult, stderrReader, progress, true, cancellationToken);
            var stdoutTask = CheckOutputAndReportProgressAsync(sshCommand, asyncResult, stdoutReader, progress, false, cancellationToken);

            await Task.WhenAll(stderrTask, stdoutTask);

            sshCommand.EndExecute(asyncResult);
        }

        private static async Task CheckOutputAndReportProgressAsync(
            SshCommand sshCommand,
            IAsyncResult asyncResult,
            StreamReader streamReader,
            IProgress<ScriptOutputLine> progress,
            bool isError,
            CancellationToken cancellationToken)
        {
            while (!asyncResult.IsCompleted || !streamReader.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    sshCommand.CancelAsync();
                }

                cancellationToken.ThrowIfCancellationRequested();

                var stderrLine = await streamReader.ReadLineAsync();

                if (!string.IsNullOrEmpty(stderrLine))
                {
                    progress.Report(new ScriptOutputLine(
                        line: stderrLine,
                        isErrorLine: isError));
                }

                // wait 10 ms
                await Task.Delay(10, cancellationToken);
            }
        }
    }

    public class ScriptOutputLine
    {
        public ScriptOutputLine(string line, bool isErrorLine)
        {
            Line = line;
            IsErrorLine = isErrorLine;
        }

        public string Line { get; private set; }

        public bool IsErrorLine { get; private set; }
    }
}
