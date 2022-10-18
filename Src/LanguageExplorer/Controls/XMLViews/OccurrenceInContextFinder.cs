// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Filters;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// LT-8457
	/// Finds sort strings for the Occurrence column of the Concordance control. The column displays each occurrence of a search term in its context.
	/// However, for sorting, the context should not take precedence over the occurrence itself (as leading context normally would if the string is
	/// considered as a whole). This StringFinder returns the occurrence itself and any trailing context.
	/// </summary>
	public class OccurrenceInContextFinder : LayoutFinder
	{
		// ENHANCE (Hasso) 2022.03: perhaps the better way to get these flids is to call GetFieldId on the decorator, passing these class and field names,
		// but I'm not sure which I like less: trying to figure out how to get all of the pieces the "right way", hard-coding the field names and trying to find the decorator,
		// or simply hard-coding the flids (can't call ConcDecorator.kflidBeginOffset, since that would create a circular project dependency.
		// ENHANCE (Hasso) (update): the colSpec should have the class and field names somewhere

		/// <summary>'BeginOffset' of an occurrence in its context.</summary>
		/// <remarks>Can't use ConcDecorator.kflidBeginOffset directly because of circular references.</remarks>
		private const int kflidBeginOffset = 899926;
		/// <summary>'EndOffset' of an occurrence in its context.</summary>
		private const int kflidEndOffset = 899927;

		/// <inheritdoc/>
		public OccurrenceInContextFinder(LcmCache cache, string layoutName, XmlNode colSpec, IApp app)
			: base(cache, layoutName, colSpec, app)
		{
		}

		/// <summary>Default constructor for persistence</summary>
		public OccurrenceInContextFinder()
		{
		}

		/// <inheritdoc/>
		public override string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			return TrimSortStrings(base.SortStrings(item, sortedFromEnd), item, sortedFromEnd);
		}

		internal string[] TrimSortStrings(string[] strings, IManyOnePathSortItem item, bool sortedFromEnd)
		{
			if (strings.Length == 0)
			{
				return strings;
			}

			if (sortedFromEnd)
			{
				var endOffset = m_sda.get_IntProp(item.RootObjectHvo, kflidEndOffset);
				return new[] { strings[0].Substring(strings[0].Length - endOffset) };
			}

			var beginOffset = m_sda.get_IntProp(item.RootObjectHvo, kflidBeginOffset);
			return new[] { strings[0].Substring(beginOffset) };
		}
	}
}
