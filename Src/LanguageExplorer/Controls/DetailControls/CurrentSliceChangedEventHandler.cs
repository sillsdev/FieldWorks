// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.DetailControls
{
	internal delegate void CurrentSliceChangedEventHandler(object sender, CurrentSliceChangedEventArgs e);

	internal class CurrentSliceChangedEventArgs : EventArgs
	{
		public Slice PreviousSlice { get; }
		public Slice CurrentSlice { get; }

		internal CurrentSliceChangedEventArgs(Slice previousSlice, Slice currentSlice)
		{
			PreviousSlice = previousSlice;
			CurrentSlice = currentSlice;
		}
	}
}