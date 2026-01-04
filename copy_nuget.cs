using System;
using System.IO;
using System.Linq;

var root = AppContext.BaseDirectory;
var outputDir = Path.GetFullPath("publish_packages", root);
Directory.CreateDirectory(outputDir);

var source = Path.Combine(root, ".localNuget");

var packages = Directory.GetFiles(source, "*.nupkg", SearchOption.AllDirectories);

packages = packages.Where(p =>
            !Path.GetFileName(p).Contains("dev", StringComparison.OrdinalIgnoreCase)).ToArray();

foreach (var file in packages)
{
    var dest = Path.Combine(outputDir, Path.GetFileName(file));
    Console.WriteLine($"Copying {Path.GetFileName(file)}");
    File.Copy(file, dest, overwrite: true);
}

Console.WriteLine($"Completed. Output: {outputDir}");
Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey(intercept: true);