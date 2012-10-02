// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ViewProxy.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	#region ViewProxy (abstract)
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Proxy class to use as a placeholder for a view that can be created on demand.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class ViewProxy
	{
		/// <summary>The name of the view.</summary>
		protected readonly string m_name;
		/// <summary><c>true</c> if this view is editable, <c>false</c> if read-only.</summary>
		protected readonly bool m_editable;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ViewProxy"/> class.
		/// </summary>
		/// <param name="name">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if this view is editable, <c>false</c> if
		/// read-only.</param>
		/// ------------------------------------------------------------------------------------
		protected ViewProxy(string name, bool fEditable)
		{
			m_name = name;
			m_editable = fEditable;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be <c>null</c>)
		/// </param>
		/// <returns>The created view</returns>
		/// ----------------------------------------------------------------------------------------
		public abstract Control CreateView(Control host);
	}
	#endregion

	#region FixedControlProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds already created controls. This can be used to insert regular controls like labels
	/// and buttons in a SplitGrid.
	/// </summary>
	/// <example>For an example how this is used, see TE\DiffView\DiffViewWrapper.cs.</example>
	/// ----------------------------------------------------------------------------------------
	public class FixedControlProxy : ViewProxy
	{
		private readonly Control m_control;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FixedControlProxy"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		/// ------------------------------------------------------------------------------------
		public FixedControlProxy(Control control) : base(control.Name, true)
		{
			m_control = control;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since this is a proxy for a control that already exists, we just return it.
		/// </summary>
		/// <param name="host">Ignored.</param>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			return m_control;
		}
	}
	#endregion
}
