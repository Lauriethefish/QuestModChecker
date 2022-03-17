using System.IO.Compression;
using System.Text.Json;
using QuestPatcher.QMod;

if (args.Length < 1)
{
    Console.Error.WriteLine("No files given to scan!");
    return;
}

foreach (string path in args)
{
    try
    {
        using var modArchive = ZipFile.OpenRead(path);
        Console.WriteLine($"Checking mod {path}");
        await CheckMod(modArchive);
    }
    catch (FileNotFoundException)
    {
        Console.Error.WriteLine($"Could not find {path}");
    }
}

async Task CheckMod(ZipArchive modArchive)
{
    var manifestEntry = modArchive.GetEntry("mod.json");
    if (manifestEntry == null)
    {
        Console.Error.WriteLine("Mod missing manifest!");
        return;
    }

    QModManifest manifest;
    await using (var manifestStream = manifestEntry.Open()) {
        manifest = await QModManifest.ParseAsync(manifestStream);
    }
    
    await using (var manifestStream = manifestEntry.Open())
    {
        var manifestDocument = await JsonDocument.ParseAsync(manifestStream);
        if (manifestDocument.RootElement.TryGetProperty("type", out _))
        {
            Console.Error.WriteLine("Mod contains \"type\" property, but this has not existed in the QMOD format for a long time");
        }
    }

    foreach (var dependency in manifest.Dependencies)
    {
        if (dependency.VersionRangeString.StartsWith("="))
        {
            Console.Error.WriteLine($"Mod uses version range {dependency.VersionRangeString} for dependency {dependency.Id}");
            Console.Error.WriteLine("= version ranges are liable to cause incompatibility with BMBF core mods, please use ^ instead");
        }
    }

    if (manifest.CoverImagePath != null && modArchive.GetEntry(manifest.CoverImagePath) == null)
    {
        Console.Error.WriteLine($"Mod states cover image {manifest.CoverImagePath}, but no such file exists in the archive");
    }

    foreach (var file in manifest.ModFileNames.Where(f => modArchive.GetEntry(f) == null))
    {
        Console.Error.WriteLine($"Mod missing stated mod file {file}");
    }
    
    foreach (var file in manifest.LibraryFileNames.Where(f => modArchive.GetEntry(f) == null))
    {
        Console.Error.WriteLine($"Mod missing stated library file {file}");
    }
    
    foreach (var file in manifest.FileCopies.Where(f => modArchive.GetEntry(f.Name) == null))
    {
        Console.Error.WriteLine($"Mod missing stated file copy {file.Name}");
    }
}