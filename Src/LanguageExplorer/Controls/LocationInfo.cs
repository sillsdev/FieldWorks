// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Contains information about a location. This is essentially a cheap IVwSelection
	/// that can be used for finding/replacing.
	/// </summary>
	internal sealed class LocationInfo
	{
		/// <summary>The levels that indicate the view constructor
		/// hierarchy leading to the location represented by this object.</summary>
		public SelLevInfo[] m_location;
		/// <summary>The tag of the string property.</summary>
		public int m_tag;
		/// <summary>The min char offset into the string property.</summary>
		public int m_ichMin;
		/// <summary>The limit char offset into the string property.</summary>
		public int m_ichLim;
		/// <summary>Count of previous occurrences of the string property at this same level</summary>
		public int m_cpropPrev;
		/// <summary> ws for multistring </summary>
		public int m_ws;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocationInfo"/> class.
		/// </summary>
		/// <param name="location">The levels that indicate the view constructor hierarchy
		/// leading to the location represented by this object.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ich">The character offset into the string property.</param>
		public LocationInfo(SelLevInfo[] location, int tag, int ich)
		{
			m_location = location;
			m_tag = tag;
			m_ichMin = m_ichLim = ich;
			m_cpropPrev = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocationInfo"/> class.
		/// </summary>
		/// <param name="helper">
		/// The selection helper used to initialize this location.
		/// </param>
		public LocationInfo(SelectionHelper helper)
		{
			m_location = helper.GetLevelInfo(SelLimitType.Bottom);
			m_tag = helper.GetTextPropId(SelLimitType.Bottom);
			m_ichMin = m_ichLim = helper.GetIch(SelLimitType.Bottom);
			m_cpropPrev = helper.GetNumberOfPreviousProps(SelLimitType.Bottom);
			m_ws = SelectionHelper.GetFirstWsOfSelection(helper.Selection);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocationInfo"/> class.
		/// </summary>
		/// <param name="locationStack">The levels that indicate the view constructor
		/// hierarchy leading to the location represented by this object.</param>
		/// <param name="cPropPrev">The count of previous occurrences of the base item
		/// on the stack.</param>
		/// <param name="tag">The tag of the string property.</param>
		/// <param name="ichMin">The min char offset into the string property.</param>
		/// <param name="ichLim">The limit char offset into the string property.</param>
		public LocationInfo(IList<StackItem> locationStack, int cPropPrev, int tag, int ichMin, int ichLim)
		{
			m_tag = tag;
			m_ichMin = ichMin;
			m_ichLim = ichLim;
			m_cpropPrev = locationStack.Count > 0 ? locationStack[locationStack.Count - 1].m_cpropPrev.GetCount(tag) : cPropPrev;
			m_location = CollectorEnv.ConvertVwEnvStackToSelLevInfo(locationStack, cPropPrev);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocationInfo"/> class.
		/// </summary>
		/// <param name="locationStack">The levels that indicate the view constructor
		/// hierarchy leading to the location represented by this object.</param>
		/// <param name="cPropPrev">The count of previous occurrences of the base item
		/// on the stack.</param>
		/// <param name="tag">The tag of the string property.</param>
		/// <param name="ichMin">The min char offset into the string property.</param>
		/// <param name="ichLim">The limit char offset into the string property.</param>
		/// <param name="ws">ws for multistring</param>
		public LocationInfo(IList<StackItem> locationStack, int cPropPrev, int tag, int ichMin, int ichLim, int ws)
			: this(locationStack, cPropPrev, tag, ichMin, ichLim)
		{
			m_ws = ws;
		}

		/// <summary>
		/// Copy constructor for the <see cref="LocationInfo"/> class.
		/// </summary>
		public LocationInfo(LocationInfo copyFrom)
		{
			m_location = copyFrom.m_location;
			m_tag = copyFrom.m_tag;
			m_ichMin = copyFrom.m_ichMin;
			m_ichLim = copyFrom.m_ichLim;
			m_cpropPrev = copyFrom.m_cpropPrev;
			m_ws = copyFrom.m_ws;
		}

		/// <summary>
		/// Gets the top (leaf) level hvo.
		/// </summary>
		public int TopLevelHvo => m_location.Length > 0 ? m_location[0].hvo : 0;
	}
}