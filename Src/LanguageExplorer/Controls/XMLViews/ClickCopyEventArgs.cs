// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class is the arguments for a ClickCopyEventHandler.
	/// </summary>
	public class ClickCopyEventArgs : EventArgs
	{
		/// <summary />
		public ClickCopyEventArgs(ITsString tssWord, int hvo, ITsString tssSource, int ichStartWord)
		{
			Word = tssWord;
			Hvo = hvo;
			Source = tssSource;
			IchStartWord = ichStartWord;
		}

		/// <summary>
		/// Gets the hvo.
		/// </summary>
		public int Hvo { get; }

		/// <summary>
		/// Gets the word.
		/// </summary>
		public ITsString Word { get; }

		/// <summary>
		/// Gets the ich start word.
		/// </summary>
		public int IchStartWord { get; }

		/// <summary>
		/// Gets the source.
		/// </summary>
		public ITsString Source { get; }
	}

	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	public delegate void ClickCopyEventHandler(object sender, ClickCopyEventArgs e);
}