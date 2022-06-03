using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Index page visited in {DT}",DateTime.UtcNow.ToLongTimeString());
            return View();
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public IActionResult Filter()
        {
            return View();
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> Filter(string newWord)
        {
            await _applicationDbContext.FilterWords.AddAsync(new FilterWord(newWord));
            await _applicationDbContext.SaveChangesAsync();
            ViewBag.ResultFilter = "New word has been added!";
            return View();
        }

        [HttpGet]
        [Route("api/words-list")]
        public async Task<IEnumerable<string>> GetAllFilterWords()
        {
            var result = await _applicationDbContext.FilterWords.Select(_ => _.Name).ToListAsync();
            return result;
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public IActionResult BackUp()
        {
            return View();
        }

        [Authorize(Roles = "Administrator")]
        [ActionName("BackUp"), HttpPost]
        public async Task<IActionResult> BackUpPost()
        {
            await BackupDatabase();
            return View();
        }

        private async Task BackupDatabase()
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
            ViewBag.LastBackUp = timeCreated;
            _logger.LogInformation($"New back up has created {timeCreated}");
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public IActionResult SqlQuery()
        {
            return View();
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> SqlQuery(string query)
        {
            try
            {
                using (var command = _applicationDbContext.Database.GetDbConnection().CreateCommand())
                {
                    //query = "SELECT * FROM public.\"AspNetUsers\"";
                    command.CommandText = query;
                    _applicationDbContext.Database.OpenConnection();
                    using (var result = command.ExecuteReader())
                    {
                        List<string> finall = new();
                        int i = 0;
                        while (result.Read())
                        {
                            while (i != result.VisibleFieldCount)
                            {
                                finall.Add(result[i].ToString());
                                i++;
                            }
                        }
                        string json = JsonSerializer.Serialize(finall);
                        string end = json.Substring(1, json.Length - 2).Replace(",", "<br/>");
                        ViewBag.Result = end;
                    }
                }
                await _applicationDbContext.SaveChangesAsync();
            } catch (Exception e)
            {
                _logger.LogCritical($"SQL Query fail {e}");
                ViewBag.Result = e.ToString();
            }
            ViewBag.Result += "<br/>Operation has ended!";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
