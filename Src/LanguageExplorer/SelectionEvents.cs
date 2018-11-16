// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	/// <summary />
	public delegate void FwSelectionChangedEventHandler (object sender, FwObjectSelectionEventArgs e);

	#region FwObjectSelectionEventArgs
	/// <remarks>
	/// This event argument class could be used for other user interface lists or trees containing FieldWorks objects
	/// </remarks>
	public class FwObjectSelectionEventArgs : EventArgs
	{
		/// <summary />
		public FwObjectSelectionEventArgs(int hvo)
		{
			Hvo= hvo;
		}

		/// <summary />
		public FwObjectSelectionEventArgs(int hvo, int index)
		{
			Index = index;
			Hvo = hvo;
		}

		/// <summary>
		/// The id of the selected object.
		/// </summary>
		public int Hvo { get; }

		/// <summary>
		/// Index of the selected object in its list.
		/// </summary>
		public int Index { get; } = -1;

		/// <summary />
		public bool HasSelectedItem => Hvo != 0;
	}
	#endregion
}