using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.Framework.DetailControls;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for InfoPane.
	/// </summary>
	public class InfoPane : UserControl, IFWDisposable
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// Local variables.
		private FdoCache m_cache;
		Mediator m_mediator;
		RecordEditView m_xrev;

		#region Constructors, destructors, and suchlike methods.

		// This constructor is used by the Windows.Forms Form Designer.
		public InfoPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		public InfoPane(FdoCache cache, Mediator mediator, RecordClerk clerk)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_cache = cache;
			m_mediator = mediator;
			InitializeInfoView(clerk);
		}

		private void InitializeInfoView(RecordClerk clerk)
		{
			if (m_mediator == null)
				return;
			XmlNode xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			if (xnWindow == null)
				return;
			XmlNode xnControl = xnWindow.SelectSingleNode(
				"controls/parameters/guicontrol[@id=\"TextInformationPane\"]/control/parameters");
			if (xnControl == null)
				return;
			m_xrev = new InterlinearTextsRecordEditView();
			m_xrev.Clerk = clerk;
			m_xrev.Init(m_mediator, xnControl);
			m_xrev.Dock = DockStyle.Fill;
			this.Controls.Add(m_xrev);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_cache = null;

			base.Dispose( disposing );
		}

		#endregion // Constructors, destructors, and suchlike methods.

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InfoPane));
			this.SuspendLayout();
			//
			// InfoPane
			//
			this.Name = "InfoPane";
			resources.ApplyResources(this, "$this");
			this.Load += new System.EventHandler(this.InfoPane_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void InfoPane_Load(object sender, System.EventArgs e)
		{

		}

		internal class InterlinearTextsRecordEditView : RecordEditView
		{
			protected override DataTree CreateNewDataTree()
			{
				return new StTextDataTree();
			}

			private class StTextDataTree : DataTree
			{

				protected override void SetDefaultCurrentSlice()
				{
					base.SetDefaultCurrentSlice();
					// currently we always want the focus in the first slice by default,
					// since the user cannot control the governing browse view with a cursor.
					if (CurrentSlice == null)
						FocusFirstPossibleSlice();
				}

				public override void ShowObject(int hvoRoot, string layoutName)
				{
					int hvoShowObj = hvoRoot;
					int clsid = Cache.GetClassOfObject(hvoRoot);
					int hvoStText = 0;
					if (clsid == CmBaseAnnotation.kClassId)	// RecordClerk is tracking the annotation
					{
						// This pane, as well as knowing how to work with a record list of Texts, knows
						// how to work with one of CmBaseAnnotations, that is, a list of occurrences of
						// a word.
						int annHvo = hvoRoot;
						int hvoPara = Cache.MainCacheAccessor.get_ObjectProp(annHvo, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
						hvoStText = Cache.GetOwnerOfObject(hvoPara);
						hvoShowObj = hvoStText;
					}
					else
					{
						hvoStText = hvoRoot;
					}
					StText stText = new StText(Cache, hvoStText);
					if (stText.OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents)
						hvoShowObj = stText.OwnerHVO;
					base.ShowObject(hvoShowObj, layoutName);
				}

			}
		}
	}
}
