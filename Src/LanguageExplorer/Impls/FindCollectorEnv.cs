// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Handles finding text
	/// </summary>
	/// <remarks>The current implementation doesn't work for different styles, tags, and WSs
	/// that are applied by the VC.</remarks>
	public class FindCollectorEnv : CollectorEnv, IDisposable
	{
		#region Data members
		/// <summary>Found match location</summary>
		protected LocationInfo m_LocationFound;
		/// <summary>Location where find next should stop because it has wrapped around</summary>
		protected LocationInfo m_LimitLocation;
		/// <summary>Location to start current find next</summary>
		protected LocationInfo m_StartLocation;

		/// <summary />
		protected IVwViewConstructor m_vc;
		/// <summary />
		protected int m_frag;
		/// <summary />
		protected IVwPattern m_Pattern;
		/// <summary />
		protected IVwTxtSrcInit2 m_textSourceInit;
		/// <summary />
		protected IVwSearchKiller m_searchKiller;
		#endregion

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FindCollectorEnv"/> class.
		/// </summary>
		/// <param name="vc">The view constructor.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display.</param>
		/// <param name="frag">The fragment.</param>
		/// <param name="vwPattern">The find/replace pattern.</param>
		/// <param name="searchKiller">Used to interrupt a find/replace</param>
		/// <remarks>If the base environment is not null, it is used for various things,
		/// such as obtaining 'outer object' information.</remarks>
		public FindCollectorEnv(IVwViewConstructor vc, ISilDataAccess sda, int hvoRoot, int frag, IVwPattern vwPattern, IVwSearchKiller searchKiller)
			: base(null, sda, hvoRoot)
		{
			m_vc = vc;
			m_frag = frag;
			m_Pattern = vwPattern;
			m_searchKiller = searchKiller;
			m_textSourceInit = VwMappedTxtSrcClass.Create();
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Finds the next occurence.
		/// </summary>
		/// <param name="startLocation">The selection level information, property tag and
		/// character offset that represent the location where the search should start.</param>
		/// <returns>
		/// A LocationInfo thingy if a match was found, otherwise <c>null</c>.
		/// </returns>
		public LocationInfo FindNext(LocationInfo startLocation)
		{
			m_StartLocation = startLocation;
			m_LocationFound = null;
			StoppedAtLimit = false;

			Reset(); // Just in case
			// Enhance JohnT: if we need to handle more than one root object, this would
			// be one place to loop over them.
			m_vc.Display(this, m_hvoCurr, m_frag);

			if (m_LocationFound == null && StoppedAtLimit)
			{
				m_LimitLocation = null;
			}
			else if (m_LimitLocation == null)
			{
				m_LimitLocation = new LocationInfo(startLocation);
			}
			return m_LocationFound;
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Determines whether to search this object based on whether we've reached our start
		/// location yet.
		/// </summary>
		protected override bool DisplayThisObject(int hvoItem, int tag)
		{
			// We want to skip the beginning until we reach our start location.
			if (m_StartLocation == null || Finished)
			{
				return true;
			}

			var cPropPrev = CPropPrev(tag);
			return (m_StartLocation.m_location.Where(lev => lev.tag == tag && lev.cpropPrevious == cPropPrev).Select(lev => lev.hvo == hvoItem)).FirstOrDefault();
		}

		/// <summary>
		/// Override to check whether the start location or the limit location is in a literal
		/// string or some other added property.
		/// </summary>
		protected override void CheckForNonPropInfo()
		{
			if (Finished)
			{
				return;
			}

			if (m_fGotNonPropInfo)
			{
				// This should clear the m_fGotNonPropInfo flag and increment the count of
				// props for the ktagGapInAttrs
				base.CheckForNonPropInfo();

				// If our start location was in the object we just added (which isn't checked
				// by the normal find code), we need to set the start location to null so that
				// the find code will start looking for a match.
				CheckForStartLocationAndLimit((int)VwSpecialAttrTags.ktagGapInAttrs);
			}
		}

		/// <summary>
		/// Adds the string. Overridden to check for strings that were added inside of an
		/// open property.
		/// </summary>
		public override void AddString(ITsString tss)
		{
			base.AddString(tss);

			if (!m_fGotNonPropInfo)
			{
				// We actually had a prop open already, but we still need to do the checks for
				// this string. In this case m_tagCurrent should hold the tag that belongs to
				// the open property.
				CheckForStartLocationAndLimit(m_tagCurrent);
			}
		}

		/// <summary>
		/// Member AddStringProp
		/// </summary>
		public override void AddStringProp(int tag, IVwViewConstructor vwvc)
		{
			if (Finished)
			{
				return;
			}

			base.AddStringProp(tag, vwvc);

			if (m_StartLocation != null && !CurrentLocationIsStartLocation(tag))
			{
				return;
			}

			DoFind(m_sda.get_StringProp(m_hvoCurr, tag), tag);

			// We now processed the start location, so continue normally
			m_StartLocation = null;
		}

		/// <summary>
		/// Member AddStringAltMember
		/// </summary>
		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor vwvc)
		{
			if (Finished)
			{
				return;
			}

			base.AddStringAltMember(tag, ws, vwvc);

			if (m_StartLocation != null && !CurrentLocationIsStartLocation(tag))
			{
				return;
			}

			DoFind(m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws), tag);

			// We now processed the start location, so continue normally
			m_StartLocation = null;
		}

		/// <summary>
		/// Return true if we don't need to process any further. Some methods may be able
		/// to truncate operations.
		/// </summary>
		protected override bool Finished
		{
			get
			{
				if (m_LocationFound != null || StoppedAtLimit)
				{
					return true;
				}
				if (m_searchKiller == null)
				{
					return false;
				}

				m_searchKiller.FlushMessages();
				return m_searchKiller.AbortRequest;
			}
		}
		#endregion // Overrides

		#region Public properties
		/// <summary>
		/// Gets a value indicating whether a match was found
		/// </summary>
		public bool FoundMatch => (m_LocationFound != null);

		/// <summary>
		/// Gets a value indicating whether find stopped at limit.
		/// </summary>
		public bool StoppedAtLimit { get; protected set; }

		/// <summary>
		/// Gets or sets a value indicating whether the find has already wrapped around
		/// </summary>
		public bool HasWrapped { set; get; }
		#endregion

		#region Protected methods
		/// <summary>
		/// Returns whether or not the property at the current location is the starting
		/// location.
		/// NOTE: This method will return false if there is no start location
		/// </summary>
		protected bool CurrentLocationIsStartLocation(int tag)
		{
			return (m_StartLocation != null && CurrentStackIsSameAsLocationInfo(m_StartLocation, tag));
		}

		/// <summary>
		/// Determines if the current stack loacation and the specified tag match the location
		/// specified in the given LocationInfo
		/// </summary>
		/// <param name="info">The LocationInfo to check.</param>
		/// <param name="tag">The tag of the current property.</param>
		/// <returns>True if the location is the same, false otherwise</returns>
		protected bool CurrentStackIsSameAsLocationInfo(LocationInfo info, int tag)
		{
			if (info.m_location.Length != m_stack.Count)
			{
				return false;
			}

			// If we haven't gotten to the same occurrence of the same object property, we haven't
			// hit the starting point.
			for (var lev = 0; lev < m_stack.Count; lev++)
			{
				var limInfo = info.m_location[lev];
				// NOTE: the information in our m_stack variable and the information stored in
				// the selection levels are in opposite order.
				var iourStackLev = m_stack.Count - lev - 1;
				var stackInfo = m_stack[iourStackLev];
				var cPrevProps = (iourStackLev > 0 ? m_stack[iourStackLev - 1].m_cpropPrev.GetCount(stackInfo.m_tag) : m_cpropPrev.GetCount(stackInfo.m_tag));

				if (limInfo.tag != stackInfo.m_tag || limInfo.cpropPrevious != cPrevProps || limInfo.hvo != stackInfo.m_hvo)
				{
					return false; // Can't be at the same location
				}
			}

			// ENHANCE: If we ever need to handle multiple root objects, we'd need to check
			// ihvoRoot here.
			return (info.m_tag == tag && info.m_cpropPrev == CPropPrev(tag));
		}

		/// <summary>
		/// Checks to see if the current location is the starting location. It will also check
		/// to see if the current location is the limit (or passed the limit).
		/// NOTE: This method doesn't check any character position as it should only be used for
		/// properties that won't be searched (i.e. that the find will skip over)
		/// </summary>
		protected void CheckForStartLocationAndLimit(int tag)
		{
			if (Finished)
			{
				return;
			}

			if (CurrentLocationIsStartLocation(tag))
			{
				m_StartLocation = null;
			}
			else if (m_StartLocation == null && m_LimitLocation != null)
			{
				// Pass in -1 because we don't care about the character position
				if (PassedLimit(tag, -1))
				{
					StoppedAtLimit = true;
				}
			}
		}

		/// <summary>
		/// Does the find.
		/// </summary>
		protected virtual void DoFind(ITsString tss, int tag)
		{
			m_textSourceInit.SetString(tss, m_vc, m_sda.WritingSystemFactory);

			var textSource = m_textSourceInit as IVwTextSource;
			var ichBegin = 0;
			if (m_StartLocation != null)
			{
				Debug.Assert(m_StartLocation.TopLevelHvo == m_hvoCurr && m_StartLocation.m_tag == tag);
				ichBegin = m_StartLocation.m_ichLim;
			}

			int ichMin, ichLim;
			// When we re-wrote the find stuff to use this FindCollectorEnv, we removed some
			// whacky code from the FwFindReplaceDlg to try to deal with a sporadic failure
			// reported as TE-4085. We're no longer even calling the same method on vwPattern,
			// but if this failure ever recurs, this is probably the place where we'd want to
			// put a try/catch block so we could retry the find.
			m_Pattern.FindIn(textSource, ichBegin, tss.Length, true, out ichMin, out ichLim, null);
			if (PassedLimit(tag, ichMin))
			{
				StoppedAtLimit = true;
				return;
			}
			if (ichMin >= 0)
			{
				m_LocationFound = new LocationInfo(m_stack, CountOfPrevPropAtRoot, tag, ichMin, ichLim);
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
		protected virtual bool PassedLimit(int tag, int testIch)
		{
			Debug.Assert(!StoppedAtLimit);

			// If we don't have a limit, we're still looking for our start position.
			if (m_LimitLocation == null)
			{
				return false;
			}
			// If our start location is after the limit then we haven't hit the limit
			if (m_StartLocation != null && m_StartLocation.m_ichLim >= m_LimitLocation.m_ichMin && !HasWrapped)
			{
				return false;
			}
			if (!CurrentStackIsSameAsLocationInfo(m_LimitLocation, tag))
			{
				return false;
			}

			// We are back in the same string. If we have hit or passed the limit offset, then
			// return true
			return (testIch < 0 || testIch >= m_LimitLocation.m_ichMin);
		}
		#endregion

		#region Implementation of IDisposable

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. IsDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FindCollectorEnv()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the underlying issue.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once
				return;
			}

			if (disposing)
			{
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			if (m_textSourceInit != null)
			{
				Marshal.ReleaseComObject(m_textSourceInit);
				m_textSourceInit = null;
			}

			IsDisposed = true;
		}

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		public bool IsDisposed
		{
			get; private set;
		}

		#endregion
	}
}
