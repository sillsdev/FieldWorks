// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
<<<<<<< HEAD:Src/LanguageExplorer/Controls/DetailControls/Slices/RuleFormulaSlice.cs
using SIL.LCModel;
using SIL.PlatformUtilities;
||||||| f013144d5:Src/LexText/Morphology/RuleFormulaSlice.cs
using SIL.LCModel.Utils;
using XCore;
=======
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using XCore;
>>>>>>> develop:Src/LexText/Morphology/RuleFormulaSlice.cs

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary>
	/// This is a view slice that contains a <c>RuleFormulaControl</c>. It is extended by
	/// phonological/morphological rule slices.
	/// </summary>
	internal abstract class RuleFormulaSlice : ViewSlice
	{
		private static uint s_counter;

		public override RootSite RootSite => RuleFormulaControl.RootSite;

		protected ISharedEventHandlers _sharedEventHandlers;

		internal RuleFormulaSlice(ISharedEventHandlers sharedEventHandlers)
		{
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));

			_sharedEventHandlers = sharedEventHandlers;
			if (s_counter++ == 0)
			{
				_sharedEventHandlers.Add(Command.CmdCtxtSetFeatures, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ContextSetFeatures_Clicked, ()=> UiWidgetServices.CanSeeAndDo));
			}
		}

		internal RuleFormulaControl RuleFormulaControl
		{
			get
			{
				var retval = (RuleFormulaControl)Control;
				if (retval.PropertyTable == null)
				{
					retval.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				}
				return retval;
			}
		}

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
				RuleFormulaControl.InsertionControl.SizeChanged -= InsertionControl_SizeChanged;
				if (--s_counter == 0)
				{
					_sharedEventHandlers.Remove(Command.CmdCtxtSetFeatures);
				}
			}

			base.Dispose(disposing);
		}

		private void InsertionControl_SizeChanged(object sender, EventArgs e)
		{
			// resize entire slice when the insertion control changes size
			Height = DesiredHeight(RuleFormulaControl.RootSite);
		}

		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
			// show the insertion control
			var oldHeight = Height;
			RuleFormulaControl.InsertionControl.Show();
			Height = DesiredHeight(RuleFormulaControl.RootSite);
			// FWNX-753 called attention to misbehavior around here.
<<<<<<< HEAD:Src/LanguageExplorer/Controls/DetailControls/Slices/RuleFormulaSlice.cs
			if (Platform.IsMono && RuleFormulaControl.Height != Height && RuleFormulaControl.Height == oldHeight)
||||||| f013144d5:Src/LexText/Morphology/RuleFormulaSlice.cs
			if (MiscUtils.IsMono &&
				RuleFormulaControl.Height != Height &&
				RuleFormulaControl.Height == oldHeight)
=======
			if (Platform.IsMono &&
				RuleFormulaControl.Height != Height &&
				RuleFormulaControl.Height == oldHeight)
>>>>>>> develop:Src/LexText/Morphology/RuleFormulaSlice.cs
			{
				SetSubcontrolHeights(this, oldHeight, Height);
			}
			RuleFormulaControl.PerformLayout();
		}

		/// <summary>
		/// Set the heights of subcontrols to the new value if they're equal to the old value.
		/// (This appears to be done automatically by the Windows .Net implementation during
		/// the call to InsertionControl.Show(), even before the specific setting of Height in
		/// OnEnter()!)
		/// </summary>
		/// <remarks>
		/// Setting only RuleFormulaControl.Height fails, because the process of setting the
		/// height triggers a layout, and the layout code changes the height to be no more
		/// than that of its parent control.  There are two levels of control between
		/// RuleFormulaSlice and RuleFormulaControl, so the heights of those controls must be
		/// adjusted as well.
		/// I suppose something like this could be inserted into the implementation of
		/// Control.Height, but I'm reluctant to do so, partly because even then the behavior
		/// would not really match that of the Windows .Net implementation.
		/// </remarks>
		private static void SetSubcontrolHeights(Control ctrl, int oldHeight, int newHeight)
		{
			if (ctrl.Height == oldHeight)
			{
				ctrl.Height = newHeight;
			}
			foreach (var control in ctrl.Controls)
			{
				if (control is Control ctl)
				{
					SetSubcontrolHeights(ctl, oldHeight, newHeight);
				}
			}
		}

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			// hide the insertion control
			RuleFormulaControl.InsertionControl.Hide();
			if (RuleFormulaControl.RootSite.RootBox != null && ContainingDataTree != null)
			{
				Height = DesiredHeight(RuleFormulaControl.RootSite);
			}
		}

		protected override int DesiredHeight(RootSite rs)
		{
			if (rs != null && !rs.AllowLayout)
			{
				rs.AllowLayout = true; // Fixes LT-13603 where sometimes the slice was constructed by not laid out by now.
			}
			var height = base.DesiredHeight(rs);
			// only include the height of the insertion control when it is visible
			if (RuleFormulaControl.InsertionControl.Visible)
			{
				height += RuleFormulaControl.InsertionControl.Height;
			}
			return height;
		}

		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			RuleFormulaControl.Initialize(PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache), MyCmObject, -1, LanguageExplorerControls.ksRuleEnvChooserName, ContainingDataTree.PersistenceProvder, null, null);
			RuleFormulaControl.InsertionControl.Hide();
			RuleFormulaControl.InsertionControl.SizeChanged += InsertionControl_SizeChanged;
		}

		private void ContextSetFeatures_Clicked(object sender, EventArgs e)
		{
			RuleFormulaControl.SetContextFeatures();
		}
	}
}