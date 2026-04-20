using System;
using System.Collections.Generic;

namespace odnoklassniki_selenium;

internal class ProcessedAccount
{
	public string Name { get; set; }

	public string Id { get; set; }

	public List<ProcessedGroup> Groups { get; set; }

	public List<ProcessedGroup> FaultyGroups { get; set; }

	public int TotalGroupCount { get; set; }

	public bool IsFullyProcessed { get; set; }

	public DateTime PromotionStartedDateTime { get; set; }

	public DateTime PromotionEndedDateTime { get; set; }

	public List<KeyValuePair<string, string>> Friends { get; set; }
}
