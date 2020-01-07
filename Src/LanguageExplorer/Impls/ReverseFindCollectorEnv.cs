// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Impls
{
	/// <summary />
	/// <remarks>The current implementation doesn't work for different styles, tags, and WSs
	/// that are applied by the VC.</remarks>
	internal class ReverseFindCollectorEnv : FindCollectorEnv
	{
		/// <summary />
		/// <param name="vc">The view constructor.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display.</param>
		/// <param name="frag">The fragment.</param>
		/// <param name="vwPattern">The find/replace pattern.</param>
		/// <param name="searchKiller">Used to interrupt a find/replace</param>
		/// <remarks>If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.</remarks>
		public ReverseFindCollectorEnv(IVwViewConstructor vc, ISilDataAccess sda, int hvoRoot, int frag, IVwPattern vwPattern, IVwSearchKiller searchKiller)
			: base(vc, sda, hvoRoot, frag, vwPattern, searchKiller)
		{
		}

		#region Overriden protected methods
		/// <summary>
		/// Does the find.
		/// </summary>
		protected override void DoFind(ITsString tss, int tag)
		{
			m_textSourceInit.SetString(tss, m_vc, DataAccess.WritingSystemFactory);
			var textSource = (IVwTextSource)m_textSourceInit;
			var ichBegin = textSource.LengthSearch;
			if (m_StartLocation != null)
			{
				Debug.Assert(m_StartLocation.TopLevelHvo == OpenObject && m_StartLocation.m_tag == tag);
				ichBegin = m_StartLocation.m_ichMin;
			}
			int ichMin, ichLim;
			// When we re-wrote the find stuff to use this FindCollectorEnv, we removed some
			// whacky code from the FwFindReplaceDlg to try to deal with a sporadic failure
			// reported as TE-4085. We're no longer even calling the same method on vwPattern,
			// but if this failure ever recurs, this is probably the place where we'd want to
			// put a try/catch block so we could retry the find.
			m_Pattern.FindIn(textSource, ichBegin, 0, false, out ichMin, out ichLim, null);
			if (PassedLimit(tag, ichMin))
			{
				StoppedAtLimit = true;
				return;
			}
			if (ichMin >= 0)
			{
				m_LocationFound = new LocationInfo(m_stack, CPropPrev(tag), tag, ichMin, ichLim);
			}
		}

		/// <summary>
		/// Check to see whether we have passed the limit so we can stop searching and not just
		/// go on and on endlessly in an infinite loop forever and ever until the user gets fed
		/// up and throws the computer out the window.
		/// </summary>
		/// <param name="tag">The tag of the property whose string is being searched</param>
		/// <param name="testIch">The character offset position being tested. May be -1 if
		/// no match was found in this string, in which case we treat it as being beyond the
		/// limit if this string is the string that contains the limit.</param>
		/// <returns><c>true</c> if we passed the limit; <c>false</c> otherwise.</returns>
		protected override bool PassedLimit(int tag, int testIch)
		{
			Debug.Assert(!StoppedAtLimit);
			// If we don't have a limit, we're still looking for our start position.
			if (m_LimitLocation == null)
			{
				return false;
			}
			// If our start location is after the limit then we haven't hit the limit
			if (m_StartLocation != null && m_StartLocation.m_ichLim <= m_LimitLocation.m_ichMin && !HasWrapped)
			{
				return false;
			}
			// If we haven't gotten to the same occurrence of the same object property, we haven't
			// hit the limit.
			if (m_LimitLocation.TopLevelHvo != OpenObject || m_LimitLocation.m_tag != tag || m_LimitLocation.m_cpropPrev != CPropPrev(tag))
			{
				return false;
			}
			// We are back in the same string. If we have hit or passed the limit offset, then
			// return true;
			return (testIch < 0 || testIch <= m_LimitLocation.m_ichLim);
		}
		#endregion
	}
}