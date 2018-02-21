// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// An item in the combo list that groups an object ID with the string to display.
	/// </summary>
	public class HvoTssComboItem : ITssValue
	{
		/// <summary />
		public int Hvo { get; protected set; }

		/// <summary>
		/// Special combo item tag, to further identify otherwise ambiguious combo selections. default = 0;
		/// </summary>
		public int Tag { get; protected set; }

		/// <summary>
		/// Item for the choose-analysis combo box.
		/// Constructed with an ITsString partly because this is convenient for all current
		/// creators, but also because one day we may do this with a FieldWorks combo that
		/// really takes advantage of them.
		/// </summary>
		public HvoTssComboItem(int hvoAnalysis, ITsString text)
		{
			Init(hvoAnalysis, text, 0);
		}

		/// <summary>
		/// Item for the choose-analysis combo box.
		/// </summary>
		/// <param name="hvoAnalysis"></param>
		/// <param name="text"></param>
		/// <param name="tag">special tag, to identify an otherwise ambiguious combo selections.</param>
		public HvoTssComboItem(int hvoAnalysis, ITsString text, int tag)
		{
			Init(hvoAnalysis, text, tag);
		}

		private void Init(int hvoAnalysis, ITsString text, int tag)
		{
			Hvo = hvoAnalysis;
			AsTss = text;
			Tag = tag;
		}

		/// <summary />
		public override string ToString()
		{
			return AsTss.Text;
		}

		#region ITssValue implementation

		/// <summary />
		public ITsString AsTss { get; protected set; }
		#endregion ITssValue implementation
	}
}