using System;

namespace SIL.FieldWorks.Common.Avalonia.PreviewHost;

internal sealed record PreviewOptions(string? ModuleId, string DataMode)
{
	public static PreviewOptions Current { get; set; } = new(null, "empty");

	public static PreviewOptions Parse(string[] args)
	{
		string? module = null;
		var data = "empty";

		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if (string.Equals(arg, "--module", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
			{
				module = args[++i];
				continue;
			}

			if (string.Equals(arg, "--data", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
			{
				data = args[++i];
				continue;
			}
		}

		return new PreviewOptions(module, data);
	}
}
