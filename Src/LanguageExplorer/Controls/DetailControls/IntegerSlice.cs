// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class IntegerSlice : FieldSlice
	{
		private TextBox m_tb;
		private int m_previousValue;

		/// <summary />
		public IntegerSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new TextBox(), cache, obj, flid)
		{
			m_tb = (TextBox)Control;
			m_tb.LostFocus += m_tb_LostFocus;
			m_tb.GotFocus += m_tb_GotFocus;
			m_tb.BorderStyle = BorderStyle.None;
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
			m_previousValue = Cache.DomainDataByFlid.get_IntProp(MyCmObject.Hvo, m_flid);
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
				UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoChangeTo, m_fieldName), string.Format(DetailControlsStrings.ksRedoChangeTo, m_fieldName), Cache.ActionHandlerAccessor,
					() => Cache.DomainDataByFlid.SetInt(MyCmObject.Hvo, m_flid, i));
			}
			catch (FormatException error)
			{
				error.ToString(); // JohnT added because compiler complains not used.
				MessageBox.Show(DetailControlsStrings.ksEnterNumber);
			}
			catch (Exception error)
			{
				error.ToString(); // JohnT added because compiler complains not used.
				MessageBox.Show(DetailControlsStrings.ksInvalidNumber);
			}
			// In case the update failed, make sure the box is consistent.
			m_tb.Text = Cache.DomainDataByFlid.get_IntProp(MyCmObject.Hvo, m_flid).ToString();
		}

		private void m_tb_GotFocus(object sender, EventArgs e)
		{
			ContainingDataTree.CurrentSlice = this;
		}
	}
}