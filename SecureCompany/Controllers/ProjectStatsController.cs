using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SecureCompany.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class ProjectStatsController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public ProjectStatsController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("Lines")]
        public IActionResult GetProjectLines()
        {
            string rootPath = _environment.ContentRootPath;

            var excludedDirectories = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "bin",
                "obj",
                ".git",
                ".vs",
                "node_modules"
            };

            var directories = new Stack<string>();
            directories.Push(rootPath);

            var extensionStats = new Dictionary<string, int>();

            int totalFiles = 0;
            int totalLines = 0;
            int skippedFiles = 0;

            while (directories.Count > 0)
            {
                string currentDirectory = directories.Pop();

                try
                {
                    foreach (var directory in
                        Directory.EnumerateDirectories(currentDirectory))
                    {
                        string name = Path.GetFileName(directory);

                        if (!excludedDirectories.Contains(name))
                        {
                            directories.Push(directory);
                        }
                    }

                    foreach (var file in
                        Directory.EnumerateFiles(currentDirectory))
                    {
                        try
                        {
                            if (!IsTextFile(file))
                            {
                                skippedFiles++;
                                continue;
                            }

                            int lines = CountLines(file);

                            string extension =
                                Path.GetExtension(file).ToLowerInvariant();

                            if (string.IsNullOrEmpty(extension))
                                extension = "No Extension";

                            if (!extensionStats.ContainsKey(extension))
                                extensionStats[extension] = 0;

                            extensionStats[extension] += lines;

                            totalLines += lines;
                            totalFiles++;
                        }
                        catch
                        {
                            skippedFiles++;
                        }
                    }
                }
                catch
                {
                    skippedFiles++;
                }
            }

            return Ok(new
            {
                ProjectName = Path.GetFileName(rootPath),

                TotalFiles = totalFiles,

                TotalLines = totalLines,

                SkippedFiles = skippedFiles,

                LinesByExtension = extensionStats
                    .OrderByDescending(x => x.Value)
                    .ToDictionary(x => x.Key, x => x.Value)
            });
        }

        private static int CountLines(string filePath)
        {
            int lines = 0;

            using var reader = new StreamReader(filePath);

            while (reader.ReadLine() != null)
            {
                lines++;
            }

            return lines;
        }

        private static bool IsTextFile(string filePath)
        {
            using var stream = System.IO.File.OpenRead(filePath);

            byte[] buffer = new byte[8192];

            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead == 0)
                return true;

            int controlCharacters = 0;

            for (int i = 0; i < bytesRead; i++)
            {
                byte b = buffer[i];

                if (b == 0)
                    return false;

                if (b < 32 && b != 9 && b != 10 &&
                    b != 12 && b != 13)
                {
                    controlCharacters++;
                }
            }

            return (double)controlCharacters / bytesRead <= 0.01;
        }
    }
}