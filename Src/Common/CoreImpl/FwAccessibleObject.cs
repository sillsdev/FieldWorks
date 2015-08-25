// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This is the base AccessibleObject that is used by controls derived from
	/// MainUserControl.
	/// </summary>
	public class FwAccessibleObject : Control.ControlAccessibleObject
	{
		private readonly IMainUserControl m_mainUserControl;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mainUserControl"></param>
		public FwAccessibleObject(IMainUserControl mainUserControl)
			: base((Control)mainUserControl)
		{
			m_mainUserControl = mainUserControl;
		}

		/// <summary>
		/// Get/Set the FwAccessibleObject's name.
		/// </summary>
		public override string Name
		{
			get	{ return m_mainUserControl.AccName; }
			set { m_mainUserControl.AccName = value; }
		}
	}
}