using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
{
	public static class TrackingHelper
	{
		public static void TrackImport(string area, string type, ImportExportStep importExportStep, Dictionary<string, string> extraProps = null)
		{
			var eventProps = extraProps ?? new Dictionary<string, string>();
			eventProps["area"] = area;
			eventProps["type"] = type;
			eventProps["step"] = Enum.GetName(typeof(ImportExportStep), importExportStep);
			AnalyticsOutbox.Track("Import", eventProps);
		}

		public static void TrackExport(string area, string type, ImportExportStep importExportStep, Dictionary<string, string> extraProps = null)
		{
			var eventProps = extraProps ?? new Dictionary<string, string>();
			eventProps["area"] = area;
			eventProps["type"] = type;
			eventProps["step"] = Enum.GetName(typeof(ImportExportStep), importExportStep);
			AnalyticsOutbox.Track("Export", eventProps);
		}

		public static void TrackHelpRequest(string helpFile, string helpTopic, Dictionary<string, string> extraProps = null)
		{
			var eventProps = extraProps ?? new Dictionary<string, string>();
			eventProps["helpFile"] = helpFile;
			eventProps["helpTopic"] = helpTopic;
			AnalyticsOutbox.Track("Help", eventProps);
		}
	}
}