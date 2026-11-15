using Reposter.ChromeVersioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Reposter
{
    internal class ChromeVersionManager
    {
        public static readonly string UpdateCheckUrl = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";
        public static readonly string CachePathChrome;
        public static readonly string CachePathChromeDriver;
        public static readonly string CurrentLocalChromeVersion;
        public static readonly string CurrentLocalChromeDriverVersion;
        public static readonly string PathDestinationChrome;
        public static readonly string PathDestinationChromeDriver;
        public static string LoadedJsonFilePath;

        static ChromeVersionManager()
        {
            CachePathChrome =  @"C:\Users\" + Environment.UserName + @"\.cache\selenium\chrome\win64";
            CachePathChromeDriver =  @"C:\Users\" + Environment.UserName + @"\.cache\selenium\chromedriver\win64";
            CurrentLocalChromeVersion = GetCurrentVersionInCache();
            PathDestinationChrome = @"C:\Users\" + Environment.UserName + @"\Downloads\";
            PathDestinationChromeDriver = @"C:\Users\" + Environment.UserName + @"\Downloads\";
        }

        public async Task CheckVersion()
        {

        }

        /// <summary>
        /// Возвращает текущую версию хрома в кэше
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentVersionInCache()
        {
            DirectoryInfo di = new DirectoryInfo(CachePathChrome);
            var folders = di.EnumerateDirectories();
            foreach (var folder in folders)
            {
                Debug.WriteLine(folder.FullName);
                if (folder.EnumerateFiles().Count() < 2)//меньше двух файлов, то удаляем
                {
                    Debug.WriteLine("removing "+folder.FullName + " ...");
                    folder.Delete(true);
                    Debug.WriteLine("folder "+folder.FullName+" removed");
                }
            }
            folders = di.EnumerateDirectories();
            var newestFolder = folders.OrderBy(e => e.CreationTimeUtc).Last();
            foreach (var folder in folders)
            {
                if (folder.CreationTimeUtc != newestFolder.CreationTimeUtc)
                {
                    folder.Delete(true);
                }
            }
            if (newestFolder != null)
            {
                return newestFolder.Name;
            }
            return null;
        }

        /// <summary>
        /// Удаляет старые версии хрома
        /// </summary>
        /// <param name="versions"></param>
        void RemoveOldVersions(List<string> versions)
        {

        }


        public static async Task UpdateVersion()
        {
            var progress = new Progress<double>(p => Console.Write($"\rПрогресс: {p:F1}% "));
            var extractProgress = new Progress<double>(p => Console.Write($"\rРаспаковано: {p:F1}% "));

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                LoadedJsonFilePath = Path.Combine(PathDestinationChrome, @"last-known-good-versions-with-downloads.json");
                await FileDownloader.DownloadFileAsync(UpdateCheckUrl, LoadedJsonFilePath, progress, cts.Token);
                Console.WriteLine($"Файл {UpdateCheckUrl} загружен!");//json file
                //process json file
                var jsonObj = JsonConvert.DeserializeObject<Root>(File.ReadAllText(LoadedJsonFilePath));

                if (CurrentLocalChromeVersion == jsonObj.Channels.Stable.Version)
                {
                    Console.WriteLine("Уже установлена последняя версия Chrome.");
                    cts.Dispose();
                    return;//уже имеем последнюю версию
                }
                Console.WriteLine("Будет установлена последняя версия программ Chrome и Chromedriver. Для отмены нажмите CTRL+C.");

                var latestVersionToDownload = jsonObj.Channels.Stable.Downloads.Chrome.FirstOrDefault(e=>e.Platform == "win64").Url;
                var filename = latestVersionToDownload.Trim('/').Split('/').Last();
                string loadedFilePath = Path.Combine(PathDestinationChrome, filename);

                //debug
                //loadedFilePath = @"J:\chrome-win64.zip";//remove this line after debug
                await FileDownloader.DownloadFileAsync(latestVersionToDownload, loadedFilePath, progress, cts.Token);//загрузка хрома
                Console.WriteLine($"Файл {loadedFilePath} загружен!");

                await ZipExtractor.ExtractAsync(loadedFilePath, Path.Combine(CachePathChrome, jsonObj.Channels.Stable.Version), extractProgress, true,stripTopLevelDirectory: true, cts.Token);

                latestVersionToDownload = jsonObj.Channels.Stable.Downloads.Chromedriver.FirstOrDefault(e => e.Platform == "win64").Url;
                filename = latestVersionToDownload.Trim('/').Split('/').Last();
                loadedFilePath = Path.Combine(PathDestinationChrome, filename);

                if (File.Exists(loadedFilePath))
                {
                    File.Delete(loadedFilePath);
                }

                await FileDownloader.DownloadFileAsync(latestVersionToDownload, loadedFilePath, progress, cts.Token);//загрузка хромдрайвера
                Console.WriteLine($"Файл {loadedFilePath} загружен!");

                await ZipExtractor.ExtractAsync(loadedFilePath, CachePathChromeDriver, extractProgress, true,stripTopLevelDirectory: true, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Загрузка отменена.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка HTTP: {ex.Message}");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
