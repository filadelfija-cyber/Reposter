using System;
using System.Collections.Generic;

namespace odnoklassniki_selenium;

internal class Statistics
{
	public List<ProcessedAccount> ProcessedAccounts { get; set; }

	public DateTime PromotionStartedDateTime { get; set; }

	public DateTime PromotionEndedDateTime { get; set; }

	public string SermonID { get; set; }

	public string SermonTitle { get; set; }

	public string SermonSource { get; set; }

	public List<string> TotalAccountsForSharing { get; set; }

	public KeyValuePair<string, string> CurrentlyProcessedGroup { get; set; }

	public string CurrentlyProcessedAccount { get; set; }

	public Dictionary<string, string> GroupsNotForPosting { get; set; }

	public int ProcessedAccountCount { get; set; } = 1;
}
