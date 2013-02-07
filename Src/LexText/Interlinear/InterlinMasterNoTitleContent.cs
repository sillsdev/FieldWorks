using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is like its superclass, except it reomves the superclass' title pane content control.
	/// It does not remove the titlebar as does the similar class in InterlinMasterNoTitleBar.cs.
	/// </summary>
	public class InterlinMasterNoTitleContent : InterlinMaster
	{
		private System.ComponentModel.IContainer components = null;

		public InterlinMasterNoTitleContent()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (m_styleSheet == null)
				return;		// cannot display properly without style sheet, so don't try.

			// LT-10995: the TitleContentsPane m_tcPane and the TabControl m_tabCtrl used to be
			// docked (definition in InterlinMaster.resx). However, this led to problems if the
			// font size of displayed data got changed. So we are doing the layout ourselves now:
			m_tabCtrl.Width = this.Width; // tab control width = container width
			m_tcPane = null; // don't want this component at all in this case
			// When there is no TitleContentsPane then the TabControl needs to occupy the
			// entire container:
			m_tabCtrl.Location = new Point(0, 0);
			m_tabCtrl.Height = this.Height;
			base.OnLayout(levent);
		}
	}
}
