// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Palaso.UI.WindowsForms.Keyboarding;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Connects a view (rootbox) with keyboards. This class gets created by the VwRootBox when ENABLE_TSF is not defined
	/// and MANAGED_KEYBOARDING is, that is, on Mono but not on Windows. Thus, the code here is basically Mono/Linux-only.
	/// </summary>
	[Guid("830BAF1F-6F84-46EF-B63E-3C1BFDF9E83E")]
	public class ViewInputManager: IViewInputMgr
	{
		private IVwRootBox m_rootb;

		#region IViewInputMgr methods
		/// <summary>
		/// Inititialize the input manager
		/// </summary>
		public void Init(IVwRootBox rootb)
		{
			m_rootb = rootb;
		}

		/// <summary/>
		public void Close()
		{
		}

		/// <summary>
		/// End all active compositions. Not applicable on Mono.
		/// </summary>
		public void TerminateAllCompositions()
		{
		}

		/// <summary>
		/// Activate the input method
		/// </summary>
		public void SetFocus()
		{
		}

		/// <summary>
		/// Deactivate the input method
		/// </summary>
		public void KillFocus()
		{
		}

		/// <summary>
		/// Gets a value indicating whether a composition window is active.
		/// </summary>
		public bool IsCompositionActive
		{
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating if the input method is in the process of closing a composition
		/// window.
		/// </summary>
		public bool IsEndingComposition
		{
			get { return false; }
		}

		/// <summary>
		/// Called before a property gets updated.
		/// </summary>
		/// <returns>Returns <c>true</c> if the property should be updated without normalization
		/// (i.e. not updated in the database) in order to avoid messing up compositions;
		/// <c>false</c> if property can be processed regularly.</returns>
		public bool OnUpdateProp()
		{
			return false;
		}

		/// <summary>
		/// Called when a mouse event happened.
		/// </summary>
		/// <returns>Returns <c>false</c>. Returning <c>true</c> would mean that no further
		/// processing of the mouse event should happen.</returns>
		public bool OnMouseEvent(int xd, int yd, Rect rcSrc, Rect rcDst, VwMouseEvent me)
		{
			var mouseEvent = (MouseEvent) me;
			if (mouseEvent == MouseEvent.kmeDown)
			{
				Keyboard.Activate();
			}
			return false;
		}

		/// <summary>
		/// Called when the layout of the view changes.
		/// </summary>
		public void OnLayoutChange()
		{
		}

		/// <summary>
		/// Called when the selection changes.
		/// </summary>
		public void OnSelectionChange(int nHow)
		{
		}

		/// <summary>
		/// Called when the text changes.
		/// </summary>
		public void OnTextChange()
		{
		}
		#endregion /* IViewInputMgr */

		private IWritingSystem CurrentWritingSystem
		{
			get
			{
				IVwSelection sel = m_rootb.Selection;
				int nWs = SelectionHelper.GetFirstWsOfSelection(sel);
				if (nWs == 0)
					return null;

				var wsf = m_rootb.DataAccess.WritingSystemFactory;
				if (wsf == null)
					return null;

				return wsf.get_EngineOrNull(nWs) as IWritingSystem;
			}
		}

		/// <summary>
		/// Gets the keyboard corresponding to the current selection.
		/// </summary>
		/// <returns>The keyboard, or KeyboardDescription.Zero if we can't detect the writing
		/// system based on the current selection (e.g. there is no selection).</returns>
		public IKeyboardDefinition Keyboard
		{
			get
			{
				var manager = m_rootb.DataAccess.WritingSystemFactory as PalasoWritingSystemManager;
				var ws = CurrentWritingSystem;
				if (ws == null)
					return KeyboardDescription.Zero;

				var wsd = manager.Get(ws.Handle) as IWritingSystemDefinition;
				return wsd.LocalKeyboard;
			}
		}
	}
}
