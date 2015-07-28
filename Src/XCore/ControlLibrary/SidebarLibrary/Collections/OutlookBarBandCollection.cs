// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SidebarLibrary.WinControls;

namespace SidebarLibrary.Collections
{
	/// <summary>
	/// Summary description for OutlookBarBandCollection.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="parentBar is a reference")]
	public class OutlookBarBandCollection  : System.Collections.CollectionBase, IEnumerable
	{

		#region Events
		public event EventHandler Changed;
		#endregion

		#region Class Variables
		// Back reference to the parent control
		private OutlookBar parentBar;
		#endregion

		#region Constructors
		public OutlookBarBandCollection(OutlookBar bar)
		{
			parentBar = bar;
		}
		#endregion

		#region Methods
		public int Add(OutlookBarBand band)
		{

			if (Contains(band)) return -1;
			int index = InnerList.Add(band);
			RaiseChanged();
			return index;
		}

		public bool Contains(OutlookBarBand band)
		{
			return InnerList.Contains(band);
		}

		public int IndexOf(OutlookBarBand band)
		{
			return InnerList.IndexOf(band);
		}

		public void Remove(OutlookBarBand band)
		{
			// Make sure currentBandIndex is always valid
			int currentBandIndex = parentBar.GetCurrentBand();
			bool updateCurrentIndex = currentBandIndex != -1 && currentBandIndex == Count - 1;
			InnerList.Remove(band);
			if ( updateCurrentIndex )
			{
				// Since we just removed the currently selected band,
				// set the new selected band to last band
				if ( Count > 0 )
					parentBar.SetCurrentBand(Count-1);
			}

			RaiseChanged();
		}

		public OutlookBarBand this[int index]
		{
			get {
				if ( index < 0 || index >= Count)
					return null;
				return (OutlookBarBand) InnerList[index];
			}
		}
		#endregion

		#region Implementation
		private void RaiseChanged()
		{
			if (Changed != null) Changed(this, null);
		}
		#endregion

	}
}
