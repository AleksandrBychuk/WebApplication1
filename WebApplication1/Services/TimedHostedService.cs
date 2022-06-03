using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<TimedHostedService> _logger;
        private Timer? _timer = null;

        public TimedHostedService(ILogger<TimedHostedService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        private void DoWork(object? state) // дубликат кода
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            var currentAppPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
            startInfo.FileName = Path.Combine(currentAppPath, "postgresql-backup.bat");
            var host = "localhost";
            var port = "5432";
            var user = "postgres";
            var database = "pablodb";
            var outputPath = Path.Combine(currentAppPath, $"backup{DateTimeOffset.Now.ToString("yyyy-dd-M--HH-mm-ss")}.sql");

            // use pg_dump, specifying the host, port, user, database to back up, and the output path.
            // the host, port, user, and database must be an exact match with what's inside your pgpass.conf (Windows)
            startInfo.Arguments = $@"{host} {port} {user} {database} ""{outputPath}""";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            process.Close();
            var timeCreated = DateTimeOffset.Now.ToString();
            _logger.LogInformation($"New back up has created {timeCreated}");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
