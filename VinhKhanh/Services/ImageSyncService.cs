namespace VinhKhanh.Services;

public class ImageSyncService
{
    private readonly HttpClient _httpClient;

    private static string ImagesDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "images");

    public ImageSyncService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task DownloadAsync(string? imageUrl, string? imageFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(imageFileName))
        {
            return;
        }

        var safeFileName = Path.GetFileName(imageFileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return;
        }

        var finalPath = Path.Combine(ImagesDirectory, safeFileName);
        var tempPath = finalPath + ".tmp";

        try
        {
            Directory.CreateDirectory(ImagesDirectory);

            using var response = await _httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (var tempFile = File.Create(tempPath))
            {
                await responseStream.CopyToAsync(tempFile, cancellationToken);
            }

            File.Move(tempPath, finalPath, overwrite: true);
        }
        catch
        {
            // Gi? nguyên file c? khi offline/l?i.
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    public string GetLocalPath(string? imageFileName)
    {
        if (string.IsNullOrWhiteSpace(imageFileName))
        {
            return "placeholder.png";
        }

        var safeFileName = Path.GetFileName(imageFileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return "placeholder.png";
        }

        var fullPath = Path.Combine(ImagesDirectory, safeFileName);
        return File.Exists(fullPath) ? fullPath : "placeholder.png";
    }
}
