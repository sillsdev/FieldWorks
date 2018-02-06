// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.LcmUi
{
	public class TargetFeatureEventArgs : EventArgs
	{
		public TargetFeatureEventArgs(bool enable)
		{
			Enable = enable;
		}

		public bool Enable { get; }
	}
}