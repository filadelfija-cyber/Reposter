using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Reposter.ChromeVersioning
{


    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ZipExtractor
    {
        /// <summary>
        /// Распаковывает ZIP в указанную папку.
        /// </summary>
        /// <param name="zipPath">Путь к архиву.</param>
        /// <param name="destinationDirectory">Куда распаковывать.</param>
        /// <param name="progress">Прогресс в процентах.</param>
        /// <param name="overwrite">Перезаписывать существующие файлы.</param>
        /// <param name="stripTopLevelDirectory">
        /// Отрезать ли общий верхний каталог у записей архива.
        /// <c>null</c> — автоопределение (если в архиве ровно одна корневая папка).
        /// </param>
        /// <param name="cancellationToken">Токен отмены.</param>
        public static async Task ExtractAsync(
            string zipPath,
            string destinationDirectory,
            IProgress<double>? progress = null,
            bool overwrite = true,
            bool? stripTopLevelDirectory = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Архив не найден.", zipPath);

            Directory.CreateDirectory(destinationDirectory);

            using var archive = ZipFile.OpenRead(zipPath);

            // Определяем префикс, который нужно отрезать
            var detectedRoot = DetectCommonRootFolder(archive);
            bool shouldStrip = stripTopLevelDirectory ?? (detectedRoot != null);

            string? rootPrefix = (shouldStrip && detectedRoot != null)
                ? detectedRoot + "/"
                : null;

            // Считаем общий объём только тех файлов, которые реально извлечём
            long totalBytes = 0;
            foreach (var entry in archive.Entries)
            {
                if (TryGetRelativePath(entry, rootPrefix, out var rel) &&
                    !string.IsNullOrEmpty(rel) &&
                    !rel.EndsWith("/"))
                {
                    totalBytes += entry.Length;
                }
            }

            long processedBytes = 0;
            int lastPercent = -1;
            var buffer = new byte[81920];

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryGetRelativePath(entry, rootPrefix, out var relativePath))
                    continue;

                // пустая строка — это сама корневая папка, её пропускаем
                if (string.IsNullOrEmpty(relativePath))
                    continue;

                var destPath = GetSafeDestinationPath(destinationDirectory, relativePath);

                // записи-каталоги (оканчиваются на '/')
                if (relativePath.EndsWith("/"))
                {
                    Directory.CreateDirectory(destPath);
                    continue;
                }

                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                using var entryStream = entry.Open();
                using var outputStream = new FileStream(
                    destPath,
                    overwrite ? FileMode.Create : FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    buffer.Length,
                    useAsync: true);

                int bytesRead;
                while ((bytesRead = await entryStream.ReadAsync(
                           buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await outputStream.WriteAsync(
                        buffer, 0, bytesRead, cancellationToken);

                    processedBytes += bytesRead;

                    if (progress != null && totalBytes > 0)
                    {
                        double percent = (double)processedBytes / totalBytes * 100.0;
                        int rounded = (int)Math.Round(percent);
                        if (rounded != lastPercent)
                        {
                            lastPercent = rounded;
                            progress.Report(Math.Min(percent, 100.0));
                        }
                    }
                }
            }

            progress?.Report(100.0);
        }

        /// <summary>
        /// Ищет единственную общую корневую папку. Если в корне есть файлы
        /// или корней несколько — возвращает <c>null</c>.
        /// </summary>
        private static string? DetectCommonRootFolder(ZipArchive archive)
        {
            string? commonRoot = null;

            foreach (var entry in archive.Entries)
            {
                var fullName = entry.FullName.Replace('\\', '/');
                if (string.IsNullOrEmpty(fullName))
                    continue;

                var slash = fullName.IndexOf('/');
                if (slash < 0)
                    return null; // файл в корне — общего префикса нет

                var root = fullName.Substring(0, slash);
                if (string.IsNullOrEmpty(root))
                    return null;

                if (commonRoot == null)
                    commonRoot = root;
                else if (!string.Equals(commonRoot, root, StringComparison.Ordinal))
                    return null;
            }

            return commonRoot;
        }

        /// <summary>
        /// Возвращает путь внутри архива после отрезания <paramref name="rootPrefix"/>.
        /// Если запись не относится к корню — возвращает <c>false</c>.
        /// </summary>
        private static bool TryGetRelativePath(
            ZipArchiveEntry entry, string? rootPrefix, out string relativePath)
        {
            relativePath = string.Empty;

            var fullName = entry.FullName.Replace('\\', '/');
            if (string.IsNullOrEmpty(fullName))
                return false;

            if (rootPrefix != null)
            {
                if (!fullName.StartsWith(rootPrefix, StringComparison.Ordinal))
                    return false;
                fullName = fullName.Substring(rootPrefix.Length);
            }

            relativePath = fullName;
            return true;
        }

        /// <summary>
        /// Нормализует путь внутри целевой папки и защищает от zip-slip.
        /// </summary>
        private static string GetSafeDestinationPath(
            string destinationDirectory, string relativePath)
        {
            var normalized = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var root = Path.GetFullPath(destinationDirectory);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
                root += Path.DirectorySeparatorChar;

            var fullPath = Path.GetFullPath(Path.Combine(root, normalized));

            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Архив содержит недопустимый путь: {relativePath}");

            return fullPath;
        }
    }
}
