// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class DateSlice : FieldSlice, IVwNotifyChange
	{
		/// <summary />
		public DateSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new RichTextBox(), cache, obj, flid)
		{
			Control.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Window);
			Control.ForeColor = System.Drawing.SystemColors.GrayText;
			Control.TabStop = false;
			Control.Size = new System.Drawing.Size(128, 16);
			((RichTextBox)Control).BorderStyle = BorderStyle.None;
			((RichTextBox)Control).ReadOnly = true;
			Control.GotFocus += Control_GotFocus;
			// We need to watch the cache for changes to our property.
			cache.DomainDataByFlid.AddNotification(this);
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				Control.GotFocus -= Control_GotFocus;
				Cache?.DomainDataByFlid?.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override void UpdateDisplayFromDatabase()
		{
			var rtb = (RichTextBox)Control;
			var dt = SilTime.GetTimeProperty(Cache.DomainDataByFlid, MyCmObject.Hvo, m_flid);
			rtb.Text = dt == DateTime.MinValue ? "Date/Time not set" : string.Format(DetailControlsStrings.ksDateAndTime, dt.ToLongDateString(), dt.ToShortTimeString());
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			ContainingDataTree.CurrentSlice = this;
		}

		private void Control_GotFocus(object sender, EventArgs e)
		{
			ContainingDataTree.CurrentSlice = this;
		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == MyCmObject.Hvo && tag == m_flid)
			{
				UpdateDisplayFromDatabase();
			}
		}
	}
}