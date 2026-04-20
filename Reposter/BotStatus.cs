namespace odnoklassniki_selenium;

internal class BotStatus
{
	public bool IsStarted { get; set; }

	public bool ShowStatistics { get; set; } = true;

	public bool IsStopping { get; set; }

	public bool IsSavedIntermediateResult { get; set; }
}
