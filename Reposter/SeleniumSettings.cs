namespace odnoklassniki_selenium
{
    public class SeleniumSettings
    {
        /// <summary>
        /// использовать локальную сборку
        /// </summary>
        public bool Offline { get; set; } = false

        public string BrowserVersion  { get; set; }

        public bool EnableVerboseLogging { get; set; } = false

        public bool Debug { get; set; } = false

        /// <summary>
        /// Флаг очистки старых сборок хрома и вебдрайвера
        /// </summary>
        public int ClearOldBinaries { get; set; }
    }
}