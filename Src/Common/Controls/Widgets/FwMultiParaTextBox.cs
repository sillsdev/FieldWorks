// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwMultiParaTextBox.cs
// Responsibility: FW Team

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;

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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
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
	/// Simple multi-paragraph rootsite that uses a data access that is not based on an FdoCache
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
			WritingSystemFactory = stText.Cache.WritingSystemFactory;
			CurrentWs = stText.Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			StyleSheet = styleSheet;
			AutoScroll = true;

			// Sandbox cache.
			m_sda = VwCacheDaClass.Create();
			m_sda.WritingSystemFactory = WritingSystemFactory;
			m_sda.SetActionHandler(new SimpleActionHandler());

			List<int> memHvos = new List<int>();
			foreach (IStTxtPara para in stText.ParagraphsOS)
			{
				memHvos.Add(para.Hvo);
				m_sda.SetString(para.Hvo, StTxtParaTags.kflidContents,
					para.Contents);
			}

			// If no paragraphs were passed in, then create one to get the user started off.
			if (memHvos.Count == 0)
			{
				ITsStrFactory strFact = TsStrFactoryClass.Create();
				ITsString paraStr = strFact.MakeString(String.Empty, CurrentWs);
				m_sda.SetString(kDummyParaHvo, StTxtParaTags.kflidContents, paraStr);
				memHvos.Add(kDummyParaHvo);
			}

			((IVwCacheDa)m_sda).CacheVecProp(kMemTextHvo, StTextTags.kflidParagraphs,
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
			get { return m_graphicsManager.VwGraphics; }
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
					int count = m_sda.get_VecSize(kMemTextHvo, StTextTags.kflidParagraphs);

					for (int i = 0; i < count; i++)
					{
						int hvoPara = m_sda.get_VecItem(kMemTextHvo, StTextTags.kflidParagraphs, i);
						paras.Add(m_sda.get_StringProp(hvoPara, StTxtParaTags.kflidContents));
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
		protected override EditingHelper CreateEditingHelper()
		{
			return new MultiParaBoxEditingHelper(this);
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
			m_vc = new StVc(CurrentWs);
			m_vc.Editable = true;
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
		/// Watch for keys to do the cut/copy/paste operations
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			m_sda.GetActionHandler().BeginUndoTask("dummy undo Key Down", "dummy redo Key Down");
			if (!EditingHelper.HandleOnKeyDown(e))
				base.OnKeyDown(e);
			m_sda.GetActionHandler().EndUndoTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles OnKeyPress, allowing for a delayed selection to get set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			m_sda.GetActionHandler().BeginUndoTask("dummy undo Key Press", "dummy redo Key Press");
			base.OnKeyPress(e);
			m_sda.GetActionHandler().EndUndoTask();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in the
		/// view, this method requests creation of a selection after the unit of work is complete.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void RequestSelectionAtEndOfUow(IVwRootBox rootb, int ihvoRoot, int cvlsi,
			SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt,
			bool fAssocPrev, ITsTextProps selProps)
		{
			// Creating one hooks it up; it will free itself when invoked.
			new RequestSelectionHelper((IActionHandlerExtensions)m_sda.GetActionHandler(), rootb,
				ihvoRoot, rgvsli, tagTextProp, cpropPrevious, ich, wsAlt, fAssocPrev, selProps);

			// We don't want to continue using the old, out-of-date selection.
			rootb.DestroySelection();
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
		/// Initializes a new instance of the class.
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
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

	#region SimpleActionHandler class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple ActionHandler implementation for handling fake prop changes needed for
	/// RequestSelectionAtEndOfUOW called from the Views code during multi-paragraph edits.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class SimpleActionHandler : IActionHandler, IActionHandlerExtensions
	{
		#region IActionHandler Members

		public void AddAction(IUndoAction _uact)
		{
			throw new NotImplementedException();
		}

		public void BeginNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			IsUndoTaskActive = true;
		}

		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			if (!IsUndoTaskActive)
				throw new InvalidOperationException();
			DoEndPropChangedTasks();
		}

		List<Action> m_endPropChangedTasks = new List<Action>();
		private void DoEndPropChangedTasks()
		{
			var tasks = m_endPropChangedTasks;
			m_endPropChangedTasks = new List<Action>(); // reset in case a task starts a new one
			foreach (var task in tasks)
				task();
		}

		public bool CanRedo()
		{
			throw new NotImplementedException();
		}

		public bool CanUndo()
		{
			throw new NotImplementedException();
		}

		public void Close()
		{
			throw new NotImplementedException();
		}

		public bool CollapseToMark(int hMark, string bstrUndo, string bstrRedo)
		{
			throw new NotImplementedException();
		}

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public void ContinueUndoTask()
		{
			throw new NotImplementedException();
		}

		public void CreateMarkIfNeeded(bool fCreateMark)
		{
			throw new NotImplementedException();
		}

		public int CurrentDepth
		{
			get { throw new NotImplementedException(); }
		}

		public void DiscardToMark(int hMark)
		{
			throw new NotImplementedException();
		}

		public void EndNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		public void EndOuterUndoTask()
		{
			throw new NotImplementedException();
		}

		public void EndUndoTask()
		{
			if (!IsUndoTaskActive)
				throw new InvalidOperationException();
			DoEndPropChangedTasks();
		}

		public string GetRedoText()
		{
			throw new NotImplementedException();
		}

		public string GetRedoTextN(int iAct)
		{
			throw new NotImplementedException();
		}

		public string GetUndoText()
		{
			throw new NotImplementedException();
		}

		public string GetUndoTextN(int iAct)
		{
			throw new NotImplementedException();
		}

		public bool IsUndoOrRedoInProgress
		{
			get { throw new NotImplementedException(); }
		}

		public int Mark()
		{
			throw new NotImplementedException();
		}

		public UndoResult Redo()
		{
			throw new NotImplementedException();
		}

		public int RedoableSequenceCount
		{
			get { throw new NotImplementedException(); }
		}

		public void Rollback(int nDepth)
		{
			throw new NotImplementedException();
		}

		public void StartSeq(string bstrUndo, string bstrRedo, IUndoAction _uact)
		{
			throw new NotImplementedException();
		}

		public bool SuppressSelections
		{
			get { throw new NotImplementedException(); }
		}

		public int TopMarkHandle
		{
			get { throw new NotImplementedException(); }
		}

		public UndoResult Undo()
		{
			throw new NotImplementedException();
		}

		public IUndoGrouper UndoGrouper
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public int UndoableActionCount
		{
			get { return 0; }
		}

		public int UndoableSequenceCount
		{
			get { throw new NotImplementedException(); }
		}

		public bool get_TasksSinceMark(bool fUndo)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IActionHandlerExtensions Members

		public bool IsUndoTaskActive { get; set; }

		public bool CanStartUow
		{
			get { return !IsUndoTaskActive; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearAllMarks()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MergeLastTwoUnitsOfWork()
		{
		}

		public void DoAtEndOfPropChanged(Action task)
		{
			m_endPropChangedTasks.Add(task);
		}

		public void DoAtEndOfPropChangedAlways(Action task)
		{
			throw new NotImplementedException();
		}
#pragma warning disable 67 // event is required to fulfil interface, but not used, so disable unused warning
		public event DoingUndoOrRedoDelegate DoingUndoOrRedo;
#pragma warning restore 67
		#endregion
	}
	#endregion
}
