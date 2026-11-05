using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public static class FileDownloader
{
    /// <summary>
    /// Скачивает файл по URL с отчётом о прогрессе.
    /// </summary>
    /// <param name="url">Ссылка на файл.</param>
    /// <param name="destinationPath">Путь, куда сохранить файл.</param>
    /// <param name="progress">Прогресс в процентах (0..100). Может быть null.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public static async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL не может быть пустым.", nameof(url));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Путь не может быть пустым.", nameof(destinationPath));

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(30);

        using var response = await httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        var buffer = new byte[81920];

        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // ✅ обычный using вместо await using
        using var contentStream = await response.Content
            .ReadAsStreamAsync().ConfigureAwait(false);

        // ✅ обычный using вместо await using
        using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            buffer.Length,
            useAsync: true);

        long totalRead = 0;
        int bytesRead;
        int lastReportedPercent = -1;

        while ((bytesRead = await contentStream
                   .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                   .ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(
                buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

            totalRead += bytesRead;

            if (progress != null && totalBytes.HasValue && totalBytes.Value > 0)
            {
                double percent = (double)totalRead / totalBytes.Value * 100.0;
                int rounded = (int)Math.Round(percent);

                if (rounded != lastReportedPercent)
                {
                    lastReportedPercent = rounded;
                    progress.Report(Math.Min(percent, 100.0));
                }
            }
        }

        progress?.Report(100.0);
    }
}