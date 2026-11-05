using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

/// <summary>Diagnostic-only launch of the installed executable; never opens player stores.</summary>
internal static class PackagedArtworkSmoke
{
    internal static int Run(string reportPath)
    {
        reportPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        try
        {
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "BlackjackCasino.Desktop.dll")))
                throw new InvalidOperationException("This smoke must run the published single-file executable, not a build-directory DLL.");

            string[] shadowed = typeof(App).Assembly.GetCustomAttributesData()
                .Where(a => a.AttributeType.Name == "AssemblyAssociatedContentFileAttribute")
                .Select(a => a.ConstructorArguments[0].Value as string ?? "")
                .Where(path => path.Replace('\\', '/').StartsWith("assets/empire/", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (shadowed.Length != 0)
                throw new InvalidOperationException("WPF Empire resources are shadowed by Content metadata: " + string.Join(", ", shadowed));

            // These are the exact pack-URI paths used by the legacy lobby tile and rooms.
            var embedded = new List<object>();
            for (int index = 0; index < 6; index++)
            {
                BitmapSource image = EmpireRoomTextures.Get(index);
                VerifyImage(image, "Atlas tile " + index);
                embedded.Add(new { Name = "atlas-" + index, image.PixelWidth, image.PixelHeight, image.IsFrozen });
            }
            foreach (var room in EmpireCatalog.Departments.Where(r => r.Floor > 1))
            {
                BitmapSource image = EmpireRoomTextures.ForRoom(room.Id, 0);
                VerifyImage(image, room.Id);
                embedded.Add(new { Name = room.Id, image.PixelWidth, image.PixelHeight, image.IsFrozen });
            }

            string frontend = Path.Combine(AppContext.BaseDirectory, "ModernFrontend");
            if (!File.Exists(Path.Combine(frontend, "empire.html"))) throw new FileNotFoundException("Published Empire entry is missing.");
            string art = Path.Combine(frontend, "empire-art");
            string[] files = Directory.GetFiles(art, "*.png");
            if (files.Length != 11) throw new InvalidOperationException("Expected all 11 browser Empire artwork files, found " + files.Length);
            foreach (string path in files)
            {
                using var stream = File.OpenRead(path);
                BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames[0].PixelWidth < 768 || decoder.Frames[0].PixelHeight < 768)
                    throw new InvalidOperationException("Invalid published browser artwork: " + Path.GetFileName(path));
            }
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new { Success = true, SingleFile = true,
                Executable = Environment.ProcessPath, BaseDirectory = AppContext.BaseDirectory,
                WorkingDirectory = Environment.CurrentDirectory, EmbeddedImages = embedded, BrowserArtwork = files.Select(Path.GetFileName).ToArray() },
                new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new { Success = false, Error = ex.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
            return 1;
        }
    }

    private static void VerifyImage(BitmapSource image, string name)
    {
        if (!image.IsFrozen || image.PixelWidth < 256 || image.PixelHeight < 256)
            throw new InvalidOperationException("Embedded Empire artwork could not be decoded: " + name);
    }
}
