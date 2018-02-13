// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Summary description for DateSlice.
	/// </summary>
	internal class DateSlice : FieldSlice, IVwNotifyChange
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DateSlice"/> class.
		/// </summary>
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

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
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
			var dt = SilTime.GetTimeProperty(Cache.DomainDataByFlid, Object.Hvo, m_flid);
			rtb.Text = dt == DateTime.MinValue ? "Date/Time not set" : string.Format(DetailControlsStrings.ksDateAndTime, dt.ToLongDateString(), dt.ToShortTimeString());
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus (e);
			ContainingDataTree.CurrentSlice = this;
		}

		private void Control_GotFocus(object sender, EventArgs e)
		{
			ContainingDataTree.CurrentSlice = this;
		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo == Object.Hvo && tag == m_flid)
			{
				UpdateDisplayFromDatabase();
			}
		}
	}
}