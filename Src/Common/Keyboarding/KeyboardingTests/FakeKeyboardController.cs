// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// 	Copyright (c) 2012, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Keyboarding
{
	/// <summary>
	/// Implements a fake do-nothing keyboard controller.
	/// </summary>
	public sealed class FakeKeyboardController: IKeyboardController, IKeyboardEventHandler, IKeyboardMethods
	{
		/// <summary>
		/// Installs this fake keyboard controller instead of the real one
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FakeKeboardController is a Singleton")]
		public static void Install()
		{
			SingletonsContainer.Add(typeof(IKeyboardController).FullName, new FakeKeyboardController());
		}

		#region IDisposable implementation
		public void Dispose()
		{
		}
		#endregion

		#region IKeyboardController implementation
		public IKeyboardDescription GetKeyboard(int? lcid, string otherImKeyboard)
		{
			return null;
		}

		public IKeyboardDescription GetKeyboard(int lcid)
		{
			return null;
		}

		public IKeyboardDescription GetKeyboard(string otherImKeyboard)
		{
			return null;
		}

		public void SetKeyboard(int lcid, string otherImKeyboard, ref int nActiveLangId,
			ref string activeOtherImKeyboard, ref bool fSelectLangPending)
		{
		}

		public List<IKeyboardDescription> InstalledKeyboards
		{
			get
			{
				return new List<IKeyboardDescription>();
			}
		}

		public List<IKeyboardErrorDescription> ErrorKeyboards
		{
			get
			{
				return new List<IKeyboardErrorDescription>();
			}
		}

		public Dictionary<int, IKeyboardDescription> Keyboards
		{
			get
			{
				return new Dictionary<int, IKeyboardDescription>();
			}
		}

		public IKeyboardEventHandler InternalEventHandler
		{
			get { return this; }
			set { }
		}

		public IKeyboardMethods InternalMethods
		{
			get { return this; }
			set { }
		}
		#endregion

		#region IKeyboardEventHandler implementation
		public bool OnUpdateProp(IKeyboardCallback callback)
		{
			return false;
		}

		public bool OnMouseEvent(IKeyboardCallback callback, int xd, int yd, Rectangle rcSrc,
			Rectangle rcDst, MouseEvent mouseEvent)
		{
			return false;
		}

		public void OnLayoutChange(IKeyboardCallback callback)
		{
		}

		public void OnSelectionChange(IKeyboardCallback callback, SelChangeType how)
		{
		}

		public void OnTextChange(IKeyboardCallback callback)
		{
		}
		#endregion

		#region IKeyboardMethods implementation
		public void TerminateAllCompositions(IKeyboardCallback callback)
		{
		}

		public void SetFocus(IKeyboardCallback callback)
		{
		}

		public void KillFocus(IKeyboardCallback callback)
		{
		}

		public bool IsCompositionActive(IKeyboardCallback callback)
		{
			return false;
		}

		public bool IsEndingComposition(IKeyboardCallback callback)
		{
			return false;
		}

		public void EnableInput(IKeyboardCallback callback)
		{
		}

		public void DisableInput(IKeyboardCallback callback)
		{
		}
		#endregion
	}
}
