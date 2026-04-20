using System;

namespace odnoklassniki_selenium;

internal class ProcessedGroup
{
	public string Id { get; set; }

	public string Name { get; set; }

	public DateTime? ProcessedDate { get; set; }

	public string Message { get; set; }
}
