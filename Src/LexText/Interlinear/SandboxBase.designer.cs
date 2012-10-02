using System;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.IText
{
	partial class SandboxBase
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing && m_mediator != null && m_mediator.PropertyTable != null)
			{
				m_mediator.PropertyTable.SetProperty("FirstControlToHandleMessages", null);
			}

			base.Dispose(disposing);

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_morphManager != null)
					m_morphManager.Dispose();
				if (m_vc != null)
					m_vc.Dispose();
				if (m_caches != null)
					m_caches.Dispose();
				DisposeComboHandler();
				if (m_caHandler != null)
				{
					m_caHandler.AnalysisChosen -= new EventHandler(Handle_AnalysisChosen);
					m_caHandler.Dispose();
				}
			}
			m_stylesheet = null;
			m_caches = null;
			// StringCaseStatus m_case; // Enum, which is a value type, and value types can't be set to null.
			m_ComboHandler = null; // handles most kinds of combo box.
			m_caHandler = null; // handles the one on the base line.

			m_morphManager = null;
			m_vc = null;
			if (m_rawWordform != null)
			{
				Marshal.ReleaseComObject(m_rawWordform);
				m_rawWordform = null;
			}
			if (m_tssWordform != null)
			{
				Marshal.ReleaseComObject(m_tssWordform);
				m_tssWordform = null;
			}
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		}

		#endregion
	}
}
