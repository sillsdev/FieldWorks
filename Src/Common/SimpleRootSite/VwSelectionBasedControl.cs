// --------------------------------------------------------------------------------------------
// <copyright from='2013' to='2013' company='SIL International'>
// 	Copyright (c) 2013, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Automation.Provider;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Base class for controls that are based upon view selections
	/// </summary>
	public abstract class VwSelectionBasedControl<TSelectionBasedControl> : BaseFragmentProvider<TSelectionBasedControl>
		where TSelectionBasedControl : VwSelectionBasedControl<TSelectionBasedControl>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VwSelectionBasedControl{TSelectionBasedControl}"/> class.
		/// </summary>
		/// <param name="parent">The control parent.</param>
		/// <param name="site">The site.</param>
		/// <param name="selection">The selection.</param>
		protected VwSelectionBasedControl(IChildControlNavigation parent, SimpleRootSite site, IVwSelection selection)
			: this(parent, site, selection, selectionBasedControl => null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="VwSelectionBasedControl{TSelectionBasedControl}"/> class.
		/// </summary>
		/// <param name="parent">The control parent.</param>
		/// <param name="site">The site.</param>
		/// <param name="selection">The selection.</param>
		/// <param name="childControlFactory">The child control factory.</param>
		protected VwSelectionBasedControl(IChildControlNavigation parent, SimpleRootSite site, IVwSelection selection,
			Func<TSelectionBasedControl, Func<IChildControlNavigation, IList<IRawElementProviderFragment>>> childControlFactory)
			: base(parent, site, childControlFactory)
		{
			Selection = selection;
			m_site.Invoke(ComputeScreenBoundingRectangle);
		}

		#region Other protected methods

		#endregion

		/// <summary>
		/// Gets or sets the selection that this control is based upon.
		/// </summary>
		/// <value>The selection.</value>
		protected IVwSelection Selection { get; set; }

		/// <summary>
		/// Computes the screen bounding rectangle.
		/// </summary>
		protected void ComputeScreenBoundingRectangle()
		{
			// probably should extend this to the enclosing (white) edit box;
			using (new HoldGraphics(m_site))
			{
				Rectangle rcPrimary;
				bool fEndBeforeAnchor;
				m_site.SelectionRectangle(Selection, out rcPrimary, out fEndBeforeAnchor);
				var point = m_site.PointToScreen(new Point(rcPrimary.X, rcPrimary.Y));
				var screenPoint = new System.Windows.Point(point.X, point.Y);
				var size = new System.Windows.Size(rcPrimary.Width, rcPrimary.Height);
				BoundingRectangle = new System.Windows.Rect(screenPoint, size);
			}
		}

		#region IRawElementProviderFragment Members

		#endregion

		/// <summary>
		/// Gets the next sibling of this fragment.
		/// </summary>
		/// <returns></returns>
		protected override IRawElementProviderFragment GetNextSibling()
		{
			if (m_parent != null)
				return m_parent.Navigate(this, NavigateDirection.NextSibling);
			return null;
		}

		/// <summary>
		/// Gets the previous sibling.
		/// </summary>
		/// <returns></returns>
		protected override IRawElementProviderFragment GetPreviousSibling()
		{
			if (m_parent != null)
				return m_parent.Navigate(this, NavigateDirection.PreviousSibling);
			return null;
		}

		#region IRawElementProviderSimple Members

		#endregion


		private void InstallTextRangeSelection()
		{
			// create a new selection based off our Editable range and install it.
			var sh = SelectionHelper.Create(Selection, m_site);
			sh.SetSelection(m_site, true, true);
		}

		/// <summary>
		/// Sets the focus to this element.
		/// NOTE: also installs the range selection.
		/// </summary>
		public override void SetFocus()
		{
			m_site.Invoke(() =>
				{
					if (m_site.FindForm() == Form.ActiveForm)
						m_site.Focus();
					if (Selection.SelType == VwSelType.kstText)
					{
						InstallTextRangeSelection();
					}
					else if (Selection.SelType == VwSelType.kstPicture)
					{
						var sh = SelectionHelper.Create(Selection, m_site);
						m_site.RootBox.MakeSelInObj(0, sh.LevelInfo.Length, sh.LevelInfo, sh.TextPropId, true);
					}
				});
		}
	}
}
