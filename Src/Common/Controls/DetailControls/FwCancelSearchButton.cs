// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This Button signals that a search is possible by showing a magnifying glass to the right
	/// of a search box. In this state the button is not enabled.
	/// When a search is active, it changes to an X that is enabled and when clicked on will
	/// revert to no search being active.
	/// Most of the work happens in the SearchIsActive setter.
	/// The Button's click event should be hooked up to a handler that clears the search (text)
	/// and (consequently) sets SearchIsActive to false.
	/// </summary>
	public class FwCancelSearchButton : Button
	{
		private readonly Image m_cancelSearchImage; // The X image that cancels a current search
		private readonly Image m_searchInactiveImage; // The magnifying glass that indicates that search is possible
		private bool m_active;

		public FwCancelSearchButton()
		{
			m_cancelSearchImage = FieldWorks.Resources.Images.X;
			m_searchInactiveImage = FieldWorks.Resources.Images.Search;
		}

		public void Init()
		{
			SearchIsActive = false;
			Anchor = (AnchorStyles.Top | AnchorStyles.Right);
			BackColor = SystemColors.Window;
			FlatStyle = FlatStyle.Flat;
			ForeColor = SystemColors.Window;
			Size = new Size(25, 23);
			UseVisualStyleBackColor = false;
		}

		/// <summary>
		/// When disabled, we use a Background image rather than a regular image so that it
		/// does not gray out. When showing the search icon, the button is never enabled,
		/// so it is a shame to have it grey-out our pretty magnifying glass. The X however
		/// can work as a normal button image (which avoids needing to make it larger
		/// than the button etc. in order to avoid repeating it as wallpaper, which is how
		/// BackgroundImage works.)
		/// </summary>
		public bool SearchIsActive
		{
			get { return m_active; }
			set
			{
				m_active = value;
				if (m_active)
				{
					BackgroundImage = null;
					Image = m_cancelSearchImage;
					Enabled = true;
				}
				else
				{
					BackgroundImage = m_searchInactiveImage;
					Image = null;
					Enabled = false;
				}
			}
		}
	}
}
