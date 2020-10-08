// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.XMLViews
{
	internal sealed class TargetFeatureEventArgs : EventArgs
	{
		internal TargetFeatureEventArgs(bool enable)
		{
			Enable = enable;
		}

		internal bool Enable { get; }
	}
}