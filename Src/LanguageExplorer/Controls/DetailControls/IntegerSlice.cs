// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Summary description for IntegerSlice.
	/// </summary>
	internal class IntegerSlice : FieldSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IntegerSlice"/> class.
		/// </summary>
		TextBox m_tb;
		int m_previousValue;

		public IntegerSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new TextBox(), cache, obj, flid)
		{
			m_tb = (TextBox)Control;
			m_tb.LostFocus += m_tb_LostFocus;
			m_tb.GotFocus += m_tb_GotFocus;
			m_tb.BorderStyle = BorderStyle.None;
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
			//Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_tb != null)
				{
					m_tb.LostFocus -= m_tb_LostFocus;
					m_tb.GotFocus -= m_tb_GotFocus;
					if (m_tb.Parent == null)
					{
						m_tb.Dispose();
					}
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_tb = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override void UpdateDisplayFromDatabase()
		{
			m_previousValue = m_cache.DomainDataByFlid.get_IntProp(Object.Hvo, m_flid);
			m_tb.Text = m_previousValue.ToString();
		}

		private void m_tb_LostFocus(object sender, EventArgs e)
		{
			try
			{
				var i = Convert.ToInt32(m_tb.Text);
				if (i == m_previousValue)
				{
					return;
				}
				m_previousValue = i;
				UndoableUnitOfWorkHelper.Do(
					string.Format(DetailControlsStrings.ksUndoChangeTo, m_fieldName),
					string.Format(DetailControlsStrings.ksRedoChangeTo, m_fieldName),
					Cache.ActionHandlerAccessor,
					() =>m_cache.DomainDataByFlid.SetInt(Object.Hvo, m_flid, i));
			}
			catch(FormatException error)
			{
				error.ToString(); // JohnT added because compiler complains not used.
				MessageBox.Show(DetailControlsStrings.ksEnterNumber);
			}
			catch(Exception error)
			{
				error.ToString(); // JohnT added because compiler complains not used.
				MessageBox.Show(DetailControlsStrings.ksInvalidNumber);
			}
			// In case the update failed, make sure the box is consistent.
			m_tb.Text = m_cache.DomainDataByFlid.get_IntProp(Object.Hvo, m_flid).ToString();
		}

		private void m_tb_GotFocus(object sender, EventArgs e)
		{
			ContainingDataTree.CurrentSlice = this;
		}
	}
}