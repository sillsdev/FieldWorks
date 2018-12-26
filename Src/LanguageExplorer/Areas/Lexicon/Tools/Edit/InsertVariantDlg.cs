// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// (LT-9283)
	/// "Insert Variant" should look like the GoDlg layout, but we still want some
	/// of the extra logic in LinkVariantToEntryOrSense, (e.g. determine whether
	/// we've already inserted the selected variant.)
	///
	/// TODO: refactor with LinkVariantToEntryOrSense to put all m_fBackRefToVariant logic here,
	/// else allow GoDlg to support additional Variant matching logic.
	/// </summary>
	public class InsertVariantDlg : LinkVariantToEntryOrSense
	{
		public InsertVariantDlg()
		{
			// inherit some layout controls from GoDlg
			InitializeSomeComponentsLikeGoDlg();
		}

		private void InitializeSomeComponentsLikeGoDlg()
		{
			SuspendLayout();
			// first reapply some BaseGoDlg settings
			var resources = new ComponentResourceManager(typeof(BaseGoDlg));
			ApplySomeResources(resources);
			ResumeLayout(false);
			PerformLayout();
		}

		private void ApplySomeResources(ComponentResourceManager resources)
		{
			resources.ApplyResources(m_btnClose, "m_btnClose");
			resources.ApplyResources(m_btnOK, "m_btnOK");
			resources.ApplyResources(m_btnInsert, "m_btnInsert");
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			resources.ApplyResources(m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
			resources.ApplyResources(this, "$this");

			if (MiscUtils.IsUnix)
			{
				// Mono doesn't handle anchoring coming in through these resources for adjusting
				// initial locations and sizes, so let's set those manually.  See FWNX-546.
				var bounds = ClientSize;
				var deltaX = bounds.Width - (m_matchingObjectsBrowser.Location.X + m_matchingObjectsBrowser.Width + 12);
				FixButtonLocation(m_btnClose, bounds, deltaX);
				FixButtonLocation(m_btnOK, bounds, deltaX);
				FixButtonLocation(m_btnInsert, bounds, deltaX);
				FixButtonLocation(m_btnHelp, bounds, deltaX);
				if (deltaX > 0)
				{
					m_matchingObjectsBrowser.Width = m_matchingObjectsBrowser.Width + deltaX;
				}
				var desiredBottom = Math.Min(m_btnClose.Location.Y, m_btnOK.Location.Y);
				desiredBottom = Math.Min(desiredBottom, m_btnInsert.Location.Y);
				desiredBottom = Math.Min(desiredBottom, m_btnHelp.Location.Y);
				desiredBottom -= 30;
				var deltaY = desiredBottom - (m_matchingObjectsBrowser.Location.Y + m_matchingObjectsBrowser.Height);
				if (deltaY > 0)
				{
					m_matchingObjectsBrowser.Height = m_matchingObjectsBrowser.Height + deltaY;
				}
			}
		}

		private static void FixButtonLocation(Button button, Size bounds, int deltaX)
		{
			var xloc = button.Location.X;
			if (deltaX > 0)
			{
				xloc += deltaX;
			}
			var yloc = button.Location.Y;
			var desiredY = bounds.Height - (button.Height + 12);
			var deltaY = desiredY - button.Location.Y;
			if (deltaY > 0)
			{
				yloc = desiredY;
			}
			if (xloc != button.Location.X || yloc != button.Location.Y)
			{
				button.Location = new Point(xloc, yloc);
			};
		}

		/// <summary />
		public void SetDlgInfo(LcmCache cache, IVariantComponentLexeme componentLexeme)
		{
			SetDlgInfoForComponentLexeme(cache, componentLexeme);
		}
	}
}