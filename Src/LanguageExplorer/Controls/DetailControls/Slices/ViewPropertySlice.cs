// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary />
	internal abstract class ViewPropertySlice : ViewSlice
	{
		/// <summary />
		internal ViewPropertySlice()
		{
		}

		/// <summary />
		internal ViewPropertySlice(RootSite ctrlT, ICmObject obj, int flid) : base(ctrlT)
		{
			Reuse(obj, flid);
		}

		/// <summary>
		/// Put the slice in the same state as if just created with these arguments.
		/// </summary>
		internal void Reuse(ICmObject obj, int flid)
		{
			MyCmObject = obj;
			FieldId = flid;

		}

		/// <summary>
		/// Gets the ID of the field we are editing.
		/// </summary>
		internal int FieldId { get; set; }
	}
}