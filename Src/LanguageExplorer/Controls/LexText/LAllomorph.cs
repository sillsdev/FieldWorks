// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Cheapo version of the FDO MoForm object.
	/// </summary>
	internal class LAllomorph : LObject, ITssValue
	{
		#region Data members

		private int m_type;
		private ITsString m_form;

		#endregion Data members

		#region Properties

		public int Type
		{
			get { return m_type; }
		}

		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		/// <param name="type"></param>
		public LAllomorph(int hvo, int type) : base(hvo)
		{
			m_type = type;
			m_form = null;
		}

		public LAllomorph(IMoForm allo) : base(allo.Hvo)
		{
			m_type = allo.ClassID;
			m_form = allo.Form.BestVernacularAlternative;
		}

		#endregion Construction & initialization

		public override string ToString()
		{
			return (m_form == null || m_form.Text == null) ? m_hvo.ToString() : m_form.Text;
		}

		#region ITssValue Members

		/// <summary>
		/// Implementing this allows the fw combo box to do a better job of displaying items.
		/// </summary>
		public ITsString AsTss
		{
			get { return m_form; }
		}

		#endregion
	}
}