// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// This is a view slice that contains a <c>RuleFormulaControl</c>. It is extended by
	/// phonological/morphological rule slices.
	/// </summary>
	internal class RuleFormulaSlice : ViewSlice
	{
		public override RootSite RootSite => RuleFormulaControl.RootSite;

		public RuleFormulaControl RuleFormulaControl
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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				RuleFormulaControl.InsertionControl.SizeChanged -= InsertionControl_SizeChanged;
			}

			base.Dispose(disposing);
		}

		void InsertionControl_SizeChanged(object sender, EventArgs e)
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
			if (MiscUtils.IsMono && RuleFormulaControl.Height != Height && RuleFormulaControl.Height == oldHeight)
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
		private void SetSubcontrolHeights(Control ctrl, int oldHeight, int newHeight)
		{
			if (ctrl.Height == oldHeight)
			{
				ctrl.Height = newHeight;
			}
			foreach (var c in ctrl.Controls)
			{
				var ctl = c as Control;
				if (ctl != null)
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
			// only include the height of the insertion contorl when it is visible
			if (RuleFormulaControl.InsertionControl.Visible)
			{
				height += RuleFormulaControl.InsertionControl.Height;
			}
			return height;
		}

		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);

			RuleFormulaControl.Initialize(PropertyTable.GetValue<LcmCache>("cache"), MyCmObject, -1, AreaResources.ksRuleEnvChooserName, ContainingDataTree.PersistenceProvder, null, null);
			RuleFormulaControl.InsertionControl.Hide();
			RuleFormulaControl.InsertionControl.SizeChanged += InsertionControl_SizeChanged;
		}

#if RANDYTODO
		public bool OnDisplayContextSetFeatures(object commandObject, ref UIItemDisplayProperties display)
		{
			bool enable = RuleFormulaControl.IsFeatsNCContextCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnContextSetFeatures(object args)
		{
			RuleFormulaControl.SetContextFeatures();
			return true;
		}

#if RANDYTODO
		public bool OnDisplayContextJumpToNaturalClass(object commandObject, ref UIItemDisplayProperties display)
		{
			bool enable = RuleFormulaControl.IsNCContextCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public bool OnContextJumpToNaturalClass(object args)
		{
			JumpToTool(AreaServices.NaturalClassEditMachineName);
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayContextJumpToPhoneme(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			bool enable = RuleFormulaControl.IsPhonemeContextCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}
#endif

		public virtual bool OnContextJumpToPhoneme(object args)
		{
			JumpToTool(AreaServices.PhonemeEditMachineName);
			return true;
		}

		private void JumpToTool(string toolName)
		{
			LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(toolName, ((IPhSimpleContextSeg)RuleFormulaControl.CurrentContext).FeatureStructureRA.Guid));
		}
	}
}