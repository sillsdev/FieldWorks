// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.DetailControls
{
	internal delegate void CurrentSliceChangedEventHandler(object sender, CurrentSliceChangedEventArgs e);

	internal class CurrentSliceChangedEventArgs : EventArgs
	{
		public ISlice PreviousSlice { get; }
		public ISlice CurrentSlice { get; }

		internal CurrentSliceChangedEventArgs(ISlice previousSlice, ISlice currentSlice)
		{
			PreviousSlice = previousSlice;
			CurrentSlice = currentSlice;
		}
	}
}