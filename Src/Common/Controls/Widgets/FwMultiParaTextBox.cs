using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.Common.Widgets
{
	#region FwMultiParaTextBox class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwMultiParaTextBox : Panel
	{
		private InternalFwMultiParaTextBox m_textBox;
		private BorderStyle m_borderStyle = BorderStyle.FixedSingle;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwMultiParaTextBox(IStText stText, IVwStylesheet styleSheet)
		{
			// Because a panel only allows single borders that are black, we'll
			// set it's border to none and manage the border ourselves.
			base.BorderStyle = BorderStyle.None;

			BorderStyle = (Application.RenderWithVisualStyles ?
				BorderStyle.FixedSingle : BorderStyle.Fixed3D);

			m_textBox = new InternalFwMultiParaTextBox(stText, styleSheet);
			m_textBox.Dock = DockStyle.Fill;
			Controls.Add(m_textBox);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.Control"/>
		/// and its child controls and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false
		/// to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_textBox != null)
				{
					m_textBox.CloseRootBox();
					m_textBox.Dispose();
				}
			}

			m_textBox = null;
			base.Dispose(disposing);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text box's back color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override System.Drawing.Color BackColor
		{
			get	{return base.BackColor;	}
			set
			{
				base.BackColor = value;
				if (m_textBox != null)
					m_textBox.BackColor = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new BorderStyle BorderStyle
		{
			get { return m_borderStyle; }
			set
			{
				if (value == BorderStyle.None)
					DockPadding.All = 0;
				else
				{
					DockPadding.All = (Application.RenderWithVisualStyles ?
						SystemInformation.BorderSize.Width :
						SystemInformation.Border3DSize.Width);
				}

				m_borderStyle = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the container enables the user to
		/// scroll to any controls placed outside of its visible boundaries.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool AutoScroll
		{
			get	{ return base.AutoScroll; }
			set
			{
				base.AutoScroll = value;
				m_textBox.AutoScroll = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this text box is read only.
		/// </summary>
		/// <value><c>true</c> if [read only]; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ReadOnly
		{
			get { return m_textBox.ReadOnlyView; }
			set { m_textBox.ReadOnlyView = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentWs
		{
			get { return m_textBox.CurrentWs; }
			set { m_textBox.CurrentWs = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString[] Paragraphs
		{
			get { return (m_textBox == null ?
				new List<ITsString>().ToArray() : m_textBox.Paragraphs); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the estimated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int GetEstimatedHeight()
		{
			return (m_textBox == null ? 0 : m_textBox.GetEstimatedHeight());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Relayouts this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AdjustLayout()
		{
			using (new HoldGraphics(m_textBox))
			{
				m_textBox.RootBox.Layout(m_textBox.VwGraphics, ClientSize.Width);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (m_borderStyle == BorderStyle.None)
				return;

			if (!Application.RenderWithVisualStyles)
				ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle);
			else
			{
				VisualStyleRenderer renderer = new VisualStyleRenderer(Enabled ?
					VisualStyleElement.TextBox.TextEdit.Normal :
					VisualStyleElement.TextBox.TextEdit.Disabled);

				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
			}
		}
	}

	#endregion

	#region InternalFwMultiParaTextBox
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class InternalFwMultiParaTextBox : SimpleRootSite
	{
		private const int kMemTextHvo = 1;
		private const int kDummyParaHvo = 2;
		private ISilDataAccess m_sda;
		private StVc m_vc;
		private int m_ws = -1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InternalFwMultiParaTextBox(IStText stText, IVwStylesheet styleSheet)
		{
			WritingSystemFactory = stText.Cache.LanguageWritingSystemFactoryAccessor;
			CurrentWs = stText.Cache.DefaultAnalWs;
			StyleSheet = styleSheet;
			AutoScroll = true;

			m_sda = VwCacheDaClass.Create() as ISilDataAccess;
			m_sda.WritingSystemFactory = WritingSystemFactory;

			List<int> memHvos = new List<int>();
			foreach (IStTxtPara para in stText.ParagraphsOS)
			{
				memHvos.Add(para.Hvo);
				m_sda.SetString(para.Hvo, (int)StTxtPara.StTxtParaTags.kflidContents,
					para.Contents.UnderlyingTsString);
			}

			// If no paragraphs were passed in, then create one to get the user started off.
			if (memHvos.Count == 0)
			{
				ITsStrFactory strFact = TsStrFactoryClass.Create();
				ITsString paraStr = strFact.MakeString(String.Empty, CurrentWs);
				m_sda.SetString(kDummyParaHvo, (int)StTxtPara.StTxtParaTags.kflidContents, paraStr);
				memHvos.Add(kDummyParaHvo);
			}

			((IVwCacheDa)m_sda).CacheVecProp(kMemTextHvo, (int)StText.StTextTags.kflidParagraphs,
				memHvos.ToArray(), memHvos.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the vw graphics.
		/// </summary>
		/// <value>The vw graphics.</value>
		/// ------------------------------------------------------------------------------------
		internal IVwGraphics VwGraphics
		{
			get {return m_graphicsManager.VwGraphics;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int CurrentWs
		{
			get { return m_ws; }
			set
			{
				m_ws = value;
				if (m_vc != null)
				{
					m_vc.DefaultWs = value;
					if (m_rootb != null)
						m_rootb.Reconstruct();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text box's back color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override System.Drawing.Color BackColor
		{
			get { return base.BackColor; }
			set
			{
				base.BackColor = value;
				if (m_vc != null)
					m_vc.BackColor = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ITsString[] Paragraphs
		{
			get
			{
				List<ITsString> paras = new List<ITsString>();

				if (m_sda != null)
				{
					int count = m_sda.get_VecSize(kMemTextHvo, (int)StText.StTextTags.kflidParagraphs);

					for (int i = 0; i < count; i++)
					{
						int hvoPara = m_sda.get_VecItem(kMemTextHvo, (int)StText.StTextTags.kflidParagraphs, i);
						paras.Add(m_sda.get_StringProp(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents));
					}
				}

				return paras.ToArray();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets extended editing helper for this text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_editingHelper == null)
					m_editingHelper = new MultiParaBoxEditingHelper(this);
				return m_editingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_sda == null || DesignMode)
				return;

			if (m_rootb == null)
				m_rootb = VwRootBoxClass.Create();

			m_rootb.SetSite(this);
			HorizMargin = 5;

			// Set up a new view constructor.
			if (m_vc != null)
				m_vc.Dispose();

			m_vc = new StVc();
			m_vc.Editable = true;
			m_vc.DefaultWs = CurrentWs;
			m_vc.BackColor = BackColor;
			m_rootb.DataAccess = m_sda;
			m_rootb.SetRootObject(kMemTextHvo, m_vc, (int)StTextFrags.kfrText, m_styleSheet);

			base.MakeRoot();
			m_dxdLayoutWidth = kForceLayout;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the estimated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int GetEstimatedHeight()
		{
			return (m_rootb == null ? 0 : m_rootb.Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// watch for keys to do the cut/copy/paste operations
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			//if (Parent is FwTextBox)
			//{
			//    (Parent as FwTextBox).HandleKeyDown(e);
			//    if (e.Handled)
			//        return;
			//}

			if (!EditingHelper.HandleOnKeyDown(e))
				base.OnKeyDown(e);
		}
	}
	#endregion

	#region MultiParaBoxEditingHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class MultiParaBoxEditingHelper : EditingHelper
	{
		private InternalFwMultiParaTextBox m_innerMultiParaFwTextBox;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MultiParaBoxEditingHelper"/> class.
		/// </summary>
		/// <param name="innerFwTextBox">The inner fw text box.</param>
		/// ------------------------------------------------------------------------------------
		public MultiParaBoxEditingHelper(InternalFwMultiParaTextBox innerFwTextBox) :
			base(innerFwTextBox)
		{
			m_innerMultiParaFwTextBox = innerFwTextBox;
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerMultiParaFwTextBox = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a value determining if all writing systems in the pasted string are in this
		/// project. If so, we will keep the writing system formatting. Otherwise, we will
		/// use the destination writing system (at the selection). We don't want to add new
		/// writing systems from a paste into an FwMultiParaTextBox.
		/// </summary>
		/// <param name="wsf">writing system factory containing the writing systems in the
		/// pasted ITsString</param>
		/// <param name="destWs">[out] The destination writing system (writing system used at
		/// the selection).</param>
		/// <returns>
		/// 	an indication of how the paste should be handled.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			// Determine writing system at selection (destination for paste).
			destWs = 0;
			if (CurrentSelection != null)
				destWs = CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.Anchor);
			if (destWs <= 0)
				destWs = m_innerMultiParaFwTextBox.CurrentWs;

			return AllWritingSystemsDefined(wsf) ? PasteStatus.PreserveWs : PasteStatus.UseDestWs;
		}

	}
	#endregion
}
