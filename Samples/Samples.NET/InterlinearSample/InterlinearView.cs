// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
namespace SIL.FieldWorks.Samples.InterlinearSample
{
	/// <summary>
	/// Summary description for InterlinearView.
	/// </summary>
	public class InterlinearView : SIL.FieldWorks.Common.Framework.RootSite
	{
		public const int ktagWord_Form = 99;
		public const int ktagWord_Type = 98;
		public const int ktagText_Words = 97;
		public const int khvoText = 1000;
		public const int kfrText = 1;
		public const int kfrWord = 2;

		// Words have hvo's from  1 upwards to the number of words.

		/// <summary>
		/// Required designer variable. Review JohnT: Why is this required? How does this class interact with designer?
		/// </summary>
		private System.ComponentModel.Container components = null;
		private InterlinearVc m_iVc;
		// Combo box appears when annotation clicked.
		private System.Windows.Forms.ComboBox typeComboBox;
		private int hvoObjSelected = 0; // object selected for combo box.

		#region Constructor, Dispose and Component Designer generated code
		public InterlinearView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// We don't really use a database, but for some reason RootSite
			// requires to have one, so we take the first database that we
			// find. In real code you would create the FdoCache in the calling
			// class (usually the Form).
			Cache = FDO.FdoCache.Create();
		}
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
		#region Windows message handling methods
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (DesignMode)
				return;

			// Recompute your layout and redraw completely, unless the width has not actually changed.
			InitGraphics();
			if (DoLayout())
				Invalidate();
			UninitGraphics();
		}
		#endregion
		public override void MakeSelectionVisible1(IVwSelection sel)
		{
		}

		public override bool IsSelectionVisible1(IVwSelection sel)
		{
			return false;
		}

		public override void MakeRoot(IVwGraphics vg, ILgEncodingFactory encfParam, out IVwRootBox rootbParam)
		{
			rootbParam = null;

			// JohnT: this is a convenient though somewhat unconventional place to create and partly
			// initialize the combo box. We can't set its position yet, as that depends on what the user
			// clicks. Nor do we yet add it to our list of windows, nor make it visible.
			typeComboBox = new System.Windows.Forms.ComboBox();
			typeComboBox.Items.AddRange(new object[] {"sal", "noun", "verb", "det", "adj", "adv"});
			typeComboBox.DropDownWidth = 280; // Todo JohnT: make right for showing all options
			typeComboBox.SelectedValueChanged += new EventHandler(this.HandleComboSelChange);

			base.MakeRoot(vg, encfParam, out rootbParam);

			if (m_fdoCache == null || DesignMode)
				return;

			IVwRootBox rootb = (IVwRootBox)new FwViews.VwRootBoxClass();
			rootb.SetSite(this);

			int hvoRoot = khvoText;

			// Set up sample data (not in the database for now)
			ITsStrFactory tsf = (ITsStrFactory)new FwKernelLib.TsStrFactoryClass();
			int encEng = 0; // TODO: Implement StrUtil::ParseEnc
			string[] words = {"Hello","world!","This","is","an","interlinear","view"};
			string[] wordtypes = {"sal","noun","det","verb","det","adj","noun"};
			int[] rghvoWords = new int[(words.Length)];
			for (int i = 0; i < words.Length; ++i)
			{
				ITsString tss = tsf.MakeString(words[i], encEng);
				// Use i+1 as the HVO for the word objects. Avoid using 0 as an HVO
				m_fdoCache.VwCacheDaAccessor.CacheStringProp(i + 1, ktagWord_Form, tss);
				tss = tsf.MakeString(wordtypes[i], encEng);
				// Use i+1 as the HVO for the word objects. Avoid using 0 as an HVO
				m_fdoCache.VwCacheDaAccessor.CacheStringProp(i + 1, ktagWord_Type, tss);
				rghvoWords[i] = i + 1;
			}
			m_fdoCache.VwCacheDaAccessor.CacheVecProp(khvoText, ktagText_Words, rghvoWords, rghvoWords.Length);

			int frag = kfrText;
			m_iVc = new InterlinearVc();

			if (encfParam != null)
				m_fdoCache.MainCacheAccessor.set_EncodingFactory(encfParam);

			rootb.set_DataAccess(m_fdoCache.MainCacheAccessor);

			rootb.SetRootObject(hvoRoot, m_iVc, frag, null);
			rootbParam = rootb;
		}
		// Handles a change in the item selected in the combo box
		void HandleComboSelChange(object sender, EventArgs ea)
		{
			ITsString tss = m_fdoCache.GetTsStringProperty(hvoObjSelected, ktagWord_Type);
			string str = tss.get_Text();
			if (str != typeComboBox.SelectedItem.ToString())
			{
				ITsStrFactory tsf = (ITsStrFactory)new FwKernelLib.TsStrFactoryClass();
				int encEng = 0; // TODO: Implement StrUtil::ParseEnc
				tss = tsf.MakeString(typeComboBox.SelectedItem.ToString(), encEng);
				// Enhance JohnT: for a real property, we would use another method that really sets
				// it in the database.
				m_fdoCache.VwCacheDaAccessor.CacheStringProp(hvoObjSelected, ktagWord_Type, tss);
				m_fdoCache.PropChanged(null, FwViews.PropChangeType.kpctNotifyAll, hvoObjSelected, ktagWord_Type, 0, 0, 0);
			}
		}

		// Handles a change in the view selection.
		protected override void HandleSelectionChange(IVwSelection vwselNew)
		{
			base.HandleSelectionChange(vwselNew);
			// Figure what property is selected and display combo only if relevant.
			SIL.FieldWorks.Common.COMInterfaces.ITsString tss;
			int ich, tag, enc;
			bool fAssocPrev;
			vwselNew.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObjSelected, out tag, out enc);
			string str = tss.get_Text();
			if (tag == ktagWord_Type)
			{
				// Display combo at selection
				SIL.FieldWorks.Common.COMInterfaces.Rect loc;
				vwselNew.GetParaLocation(out loc);
				typeComboBox.Location = new System.Drawing.Point(loc.left, loc.top);
				// 60 is an arbitrary minimum size to make the current contents visible.
				// Enhance JohnT: figure the width needed by the widest string, add width of arrow, use that
				// as minimum
				typeComboBox.Size = new System.Drawing.Size(Math.Max(loc.right - loc.left, 60), loc.bottom - loc.top);
				typeComboBox.Text = str;
				// This also makes it visible.
				this.Controls.Add(typeComboBox);
			}
			else
			{
				// Hide combo if visible.
				// Enhance JohnT: possibly also remove on loss of focus?
				if (this.Controls.Contains(typeComboBox))
				{
					this.Controls.Remove(typeComboBox);
				}
			}
			// Todo JohnT: make something interesting happen when a selection is made.
		}
	}
}
