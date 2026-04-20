using System.Collections.Generic;

namespace odnoklassniki_selenium;

internal class Settings
{
	public List<Account> Accounts { get; set; }

	public bool ShareInGroups { get; set; }

	public bool ShareInFriends { get; set; }

	public bool ShareInGroupsDirectly { get; set; }

	public bool AddComments { get; set; }

	public string PageForSharing { get; set; }

	public bool Headless { get; set; }

	public bool TakeScreenShotsOnErrors { get; set; }

	public bool ShowLogInConsole { get; set; }

	public int MinWaitTime { get; set; }

	public int MaxWaitTime { get; set; }

	public bool ShutdownAfterFinish { get; set; }

	public bool ExitAfterFinish { get; set; }

	public bool DisableWebDriverLogging { get; set; }

	public bool RetryOnFaultyRun { get; set; } = true;

	public bool PageLoadStrategyEager { get; set; }

	public bool KioskMode { get; set; }

	public int Parallelism { get; set; } = 1;

	public bool UseBot { get; set; }
}
