using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// ===== 設定 =====
var targetDirs = new[]
{
    Path.GetFullPath("publish_packages", AppContext.BaseDirectory),
    Path.GetFullPath(".localNuget", AppContext.BaseDirectory)
};

// ===== 実行 =====
foreach (var dir in targetDirs)
{
    CleanupOldPackages(dir, keepCount: 2);
}

Console.WriteLine();
Console.WriteLine("All cleanup completed.");
Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey(intercept: true);

// =========================
// パッケージ掃除ロジック
// =========================
static void CleanupOldPackages(string directory, int keepCount)
{
    if (!Directory.Exists(directory))
    {
        Console.WriteLine($"[Skip] Directory not found: {directory}");
        return;
    }

    Console.WriteLine($"Target: {directory}");

    var files = Directory
        .EnumerateFiles(directory, "*.nupkg", SearchOption.TopDirectoryOnly)
        .Where(f => !f.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
        .Select(f => new FileInfo(f))
        .ToList();
    
    // ① プレリリースは即削除
    var packages = new List<PackageFile>();
    foreach (var file in files)
    {
        var pkg = TryParseStablePackage(file);
        if (pkg == null)
        {
            Console.WriteLine($"  Delete (pre-release): {file.Name}");
            file.Delete();
        }
        else
        {
            packages.Add(pkg);
        }
    }

    // ② 安定版のみ抽出
    var stablePackages = packages
        .GroupBy(x => x!.Id);

    foreach (var group in stablePackages)
    {
        var ordered = group
            .OrderByDescending(x => x!.Version)
            .ToList();

        foreach (var old in ordered.Skip(keepCount))
        {
            Console.WriteLine($"  Delete: {old!.File.Name}");
            old.File.Delete();
        }
    }

    Console.WriteLine();
}


static PackageFile? TryParseStablePackage(FileInfo file)
{
    var name = Path.GetFileNameWithoutExtension(file.Name);
    var parts = name.Split('.');

    // x.y.z が最低条件
    if (parts.Length < 4)
        return null;

    // 後ろ3つを version とみなす
    var versionText = string.Join('.', parts[^3..]);

    // プレリリースは即除外
    if (versionText.Contains('-'))
        return null;

    if (!Version.TryParse(versionText, out var version))
        return null;

    // それ以外はすべて ID
    var id = string.Join('.', parts[..^3]);

    return new PackageFile(file, id, version);
}

record PackageFile(FileInfo File, string Id, Version Version);
