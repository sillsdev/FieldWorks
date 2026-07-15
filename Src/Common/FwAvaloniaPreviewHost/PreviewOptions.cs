// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.PreviewHost
{
	internal sealed class PreviewOptions
	{
		public static PreviewOptions Current { get; set; } = new PreviewOptions(null, "empty");

		public PreviewOptions(string moduleId, string dataMode)
		{
			ModuleId = moduleId;
			DataMode = string.IsNullOrWhiteSpace(dataMode) ? "empty" : dataMode;
		}

		public string ModuleId { get; }
		public string DataMode { get; }

		public static PreviewOptions Parse(string[] args)
		{
			string module = null;
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
				}
			}

			return new PreviewOptions(module, data);
		}
	}
}
