using System.Collections.Generic;
using System.Linq;
using SIL.LCModel.Core.WritingSystems;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public static class StringSliceUtils
	{
		/// <summary>
		/// convert the given writing systems into a property containing comma-delimited icuLocales.
		/// </summary>
		/// <param name="wss"></param>
		/// <returns></returns>
		public static string EncodeWssToDisplayPropertyValue(IEnumerable<CoreWritingSystemDefinition> wss)
		{
			var wsIds = (from ws in wss
				select ws.Id).ToArray();
			return ChoiceGroup.EncodeSinglePropertySequenceValue(wsIds);
		}

		/// <summary>
		/// Pick the writing system for text typed into an empty string field: the ws the slice
		/// was created with (e.g. a custom field's First Vernacular), else the configured
		/// empty-field default (the 'wsempty' attribute), else the first analysis ws (LT-22145).
		/// See LT-22630.
		/// </summary>
		public static int GetWsForEmptyField(int wsSpecified, int wsEmptyDefault, int wsDefaultAnal)
		{
			if (wsSpecified > 0)
				return wsSpecified;
			if (wsEmptyDefault > 0)
				return wsEmptyDefault;
			return wsDefaultAnal;
		}

		/// <summary>
		/// Get the writing systems we should actually display right now. That is, from the ones
		/// that are currently possible, select any we've previously configured to show.
		/// </summary>
		public static IEnumerable<CoreWritingSystemDefinition> GetVisibleWritingSystems(string singlePropertySequenceValue,
			IEnumerable<CoreWritingSystemDefinition> validDefinitions)
		{
			string[] wsIds = ChoiceGroup.DecodeSinglePropertySequenceValue(singlePropertySequenceValue);
			var wsIdSet = new HashSet<string>(wsIds);
			return from ws in validDefinitions
				where wsIdSet.Contains(ws.Id)
				select ws;
		}
	}
}
