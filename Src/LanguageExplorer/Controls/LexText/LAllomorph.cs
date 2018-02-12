// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Cheapo version of the LCM MoForm object.
	/// </summary>
	internal class LAllomorph : LObject, ITssValue
	{
		#region Data members
		#endregion Data members

		public int Type { get; }

		#region Construction & initialization

		/// <summary />
		public LAllomorph(int hvo, int type) : base(hvo)
		{
			Type = type;
			AsTss = null;
		}

		public LAllomorph(IMoForm allo) : base(allo.Hvo)
		{
			Type = allo.ClassID;
			AsTss = allo.Form.BestVernacularAlternative;
		}

		#endregion Construction & initialization

		public override string ToString()
		{
			return AsTss?.Text ?? HVO.ToString();
		}

		#region ITssValue Members

		/// <summary>
		/// Implementing this allows the fw combo box to do a better job of displaying items.
		/// </summary>
		public ITsString AsTss { get; }
		#endregion
	}
}