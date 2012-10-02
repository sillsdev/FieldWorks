	// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ToolBarAdapter.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using SIL.Utils;


namespace XCore
{
/*	/// <summary>
	/// Summary description for ToolBarAdapter.
	/// </summary>
	public class ToolBarAdapter : IUIAdapter
	{
		protected Form m_window;
		protected ImageList m_imageList;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolBarAdapter"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ToolBarAdapter()
		{
		}

		public System.Windows.Forms.Control Init (System.Windows.Forms.Form window,  ImageCollection smallImages, ImageCollection largeImages)
		{
			m_window = window;
			//	m_imageList = images;
			//			m_imageLabels = labels;
			return null;//nothing to show yet since we have not created anything yet
		}
		/// <summary>
		/// Do anything that is needed after all of the other widgets have been Initialize.
		/// </summary>
		public void FinishInit()
		{
		}

		public void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			foreach(ChoiceGroup group in groupCollection)
			{
				ToolBar toolbar = new ToolBar();
				toolbar.ImageList=m_imageList;
				toolbar.ImageList = group.GetImageList();
				toolbar.Tag = group;
				group.ReferenceWidget = toolbar;
				m_window.Controls.Add(toolbar);

				//REVIEW: what's the best event to use for this?
				//this is just a hack to get us started.
				toolbar.VisibleChanged  += new System.EventHandler(group.OnDisplay);

				//this is so lame that they don't just let the buttons have a click event
				toolbar.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(OnClick);
			}
		}

		public void OnClick(object something, System.Windows.Forms.ToolBarButtonClickEventArgs args)
		{
			ToolBarButton button = args.Button;
			ChoiceBase control = (ChoiceBase) button.Tag;
			Debug.Assert( control != null);
			control.OnClick(button, null);
		}

		public void CreateUIForChoiceGroup (ChoiceGroup group)
		{

			foreach(ChoiceBase control in group)
			{
				ToolBar toolbar = (ToolBar)group.ReferenceWidget;
				Debug.Assert( toolbar != null);
				UIItemDisplayProperties display = control.GetDisplayProperties();
				display.Text  = display.Text .Replace("_", "");
				ToolBarButton button = new ToolBarButton(display.  Text);
				button.Tag = control;
				button.Enabled = display.Enabled;

				control.ReferenceWidget = button;
				toolbar.Buttons.Add(button);
			}
		}
		public void OnIdle()
		{
		}

	}
*/
}
