using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SIL.LCModel.Core.WritingSystems;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public static class StringSliceUtils
	{/// <summary>
		/// Get the visible writing systems list in terms of a singlePropertySequenceValue string.
		/// if it hasn't been defined yet, we'll use the WritingSystemOptions for default.
		/// </summary>
		/// <returns></returns>
		public static string GetVisibleWSSPropertyValue(XmlNode partRef, IEnumerable<CoreWritingSystemDefinition> defaultOptions)
		{
			string singlePropertySequenceValue = XmlUtils.GetOptionalAttributeValue(partRef, "visibleWritingSystems", null);
			if (singlePropertySequenceValue == null)
			{
				// Encode a sinqlePropertySequenceValue property value using only current WritingSystemOptions.
				singlePropertySequenceValue = EncodeWssToDisplayPropertyValue(defaultOptions);
			}
			return singlePropertySequenceValue;
		}

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
