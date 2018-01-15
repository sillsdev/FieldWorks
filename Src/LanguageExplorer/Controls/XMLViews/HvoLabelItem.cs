// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class allows, for example, the morph type items to be sorted alphabetically in the
	/// listbox by providing the IComparable interface.
	/// </summary>
	internal class HvoLabelItem : IComparable
	{
		private readonly string m_sortString;

		public HvoLabelItem(int hvoChild, ITsString tssLabel)
		{
			Hvo = hvoChild;
			TssLabel = tssLabel;
			m_sortString = tssLabel.Text;
		}

		public int Hvo { get; }

		public ITsString TssLabel { get; }

		public override string ToString()
		{
			return m_sortString;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return m_sortString?.CompareTo(obj.ToString()) ?? string.Empty.CompareTo(obj.ToString());
		}

		#endregion
	}
}