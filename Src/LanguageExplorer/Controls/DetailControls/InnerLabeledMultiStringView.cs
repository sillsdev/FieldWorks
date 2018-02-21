// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;		// controls and etc...
using System.Xml.Linq;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// InnerLabeledMultiStringView displays one or more writing system alternatives of a string property.
	/// It simply edits that property.
	/// </summary>
	public class InnerLabeledMultiStringView : RootSiteControl
	{
		bool m_forceIncludeEnglish;
		bool m_editable;

		int m_wsOptions;
		// This is additional writing systems that might possibly be relevant in addition to the one(s) indicated
		// by m_wsMagic. Currently the only example is that on a pronunciation field, vernacular as well as
		// the default pronunciation WSS might be relevant.
		int m_wsAdditionalOptions;
		private string m_textStyle;
		/// <summary>
		/// We may need to set up other controls than what this class itself knows about.
		/// </summary>
		public event EventHandler SetupOtherControls;

		/// <summary>
		/// Return the view constructor.
		/// </summary>
		internal LabeledMultiStringVc VC { get; private set; }

		/// <summary>
		/// Return the relevant writing systems.
		/// </summary>
		internal List<CoreWritingSystemDefinition> WritingSystems { get; private set; }

		/// <summary>
		/// Return the flid.
		/// </summary>
		internal int Flid { get; private set; }

		/// <summary />
		internal int HvoObj { get; private set; }

		/// <summary>
		/// This event is triggered at the start of the Display() method of the VC.
		/// It provides an opportunity to set overall properties (such as read-only) in the view.
		/// </summary>
		public event VwEnvEventHandler Display;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="hvo">The object to be edited</param>
		/// <param name="flid">The multistring property to be edited</param>
		/// <param name="wsMagic">The magic writing system (like LgWritingSystem.kwsAnals)
		/// indicating which writing systems to display.</param>
		/// <param name="forceIncludeEnglish">True, if English is to be included along with others.</param>
		/// <param name="editable">false if we don't want to allow editing of the strings.</param>
		public InnerLabeledMultiStringView(int hvo, int flid, int wsMagic, bool forceIncludeEnglish, bool editable)
			: this(hvo, flid, wsMagic, 0, forceIncludeEnglish, editable, true)
		{
		}
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="hvo">The object to be edited</param>
		/// <param name="flid">The multistring property to be edited</param>
		/// <param name="wsMagic">The magic writing system (like LgWritingSystem.kwsAnals)
		/// indicating which writing systems to display.</param>
		/// <param name="wsOptional">Additional magic WS for more options (e.g., vernacular as well as pronunciation) allowed
		/// when configuring but not shown by default</param>
		/// <param name="forceIncludeEnglish">True, if English is to be included along with others.</param>
		/// <param name="editable">false if we don't want to allow editing of the strings.</param>
		/// <param name="spellCheck">true if you want the view spell-checked.</param>
		public InnerLabeledMultiStringView(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			ConstructReuseCore(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
		}

		/// <summary>
		/// Re-initialize this view as if it had been constructed with the specified arguments.
		/// </summary>
		public void Reuse(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			ConstructReuseCore(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
			if (!editable && RootSiteEditingHelper != null)
			{
				RootSiteEditingHelper.PasteFixTssEvent -= OnPasteFixTssEvent;
			}
			if (m_rootb != null)
			{
				WritingSystems = WritingSystemOptions;
				VC.Reuse(Flid, WritingSystems, m_editable);
			}
		}

		/// <summary>
		/// Return the sound control rectangle.
		/// </summary>
		internal void GetSoundControlRectangle(IVwSelection sel, out Rectangle selRect)
		{
			using (new HoldGraphics(this))
			{
				bool fEndBeforeAnchor;
				SelectionRectangle(sel, out selRect, out fEndBeforeAnchor);
			}
		}

		/// <summary>
		/// Call this on initialization when all properties (e.g., ConfigurationNode) are set.
		/// The purpose is when reusing the slice, when we may have to reconstruct the root box.
		/// </summary>
		public void FinishInit()
		{
			m_rootb?.SetRootObject(HvoObj, VC, 1, m_styleSheet);
		}

		/// <summary>
		/// Get any text styles from configuration node (which is now available; it was not at construction)
		/// </summary>
		public void FinishInit(XElement configurationNode)
		{
			if (configurationNode.HasAttributes)
			{
				var textStyle = configurationNode.Attribute("textStyle");
				if (textStyle != null)
				{
					TextStyle = textStyle.Value;
				}
			}
			FinishInit();
		}

		/// <summary>
		/// Get or set the text style name
		/// </summary>
		public string TextStyle
		{
			get
			{
				if (string.IsNullOrEmpty(m_textStyle))
				{
					m_textStyle = "Default Paragraph Characters";
				}
				return m_textStyle;
			}
			set
			{
				m_textStyle = value;
			}
		}

		/// <summary>
		/// On a major refresh, the writing system list may have changed; update accordingly.
		/// </summary>
		public override bool RefreshDisplay()
		{
			WritingSystems = WritingSystemOptions;
			return base.RefreshDisplay();
		}

		private void ConstructReuseCore(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			HvoObj = hvo;
			Flid = flid;
			m_wsOptions = wsMagic;
			m_wsAdditionalOptions = wsOptional;
			m_forceIncludeEnglish = forceIncludeEnglish;
			m_editable = editable;
			if (editable && RootSiteEditingHelper != null)
			{
				RootSiteEditingHelper.PasteFixTssEvent += OnPasteFixTssEvent;
			}
			DoSpellCheck = spellCheck;
		}

		/// <summary>
		/// If the text for pasting is too long, truncate it and warn the user.
		/// </summary>
		private void OnPasteFixTssEvent(EditingHelper sender, FwPasteFixTssEventArgs e)
		{
			EliminateExtraStyleAndWsInfo(RootBox.DataAccess.MetaDataCache, e, Flid);
		}

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			try
			{
				m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(HvoObj); // Throws an exception, if not valid.
				base.OnKeyPress(e);
			}
			catch
			{
				e.Handled = true;
			}
		}

		/// <summary>
		/// Override to handle KeyUp/KeyDown within a multi-string field -- LT-13334
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!e.Handled && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
			{
				MultiStringSelectionUtils.HandleUpDownArrows(e, m_rootb, RootSiteEditingHelper.CurrentSelection, WritingSystemsToDisplay, Flid);
			}
		}

		static bool s_fProcessingSelectionChanged;
		/// <summary>
		/// Try to keep the selection from including any of the characters in a writing system label.
		/// See LT-8396.
		/// </summary>
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.HandleSelectionChange(prootb, vwselNew);
			// 1) We don't want to recurse into here.
			// 2) If the selection is invalid we can't use it.
			// 3) If the selection is entirely formattable ("IsSelectionInOneFormattableProp"), we don't need to do
			//    anything.
			if (s_fProcessingSelectionChanged || !vwselNew.IsValid || EditingHelper.IsSelectionInOneFormattableProp())
			{
				return;
			}
			try
			{
				s_fProcessingSelectionChanged = true;
				var hlpr = SelectionHelper.Create(vwselNew, this);
				var fRange = hlpr.IsRange;
				var fChangeRange = false;
				if (fRange)
				{
					var fAnchorEditable = vwselNew.IsEditable;
					hlpr.GetIch(SelectionHelper.SelLimitType.Anchor);
					var tagAnchor = hlpr.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
					hlpr.GetIch(SelectionHelper.SelLimitType.End);
					var tagEnd = hlpr.GetTextPropId(SelectionHelper.SelLimitType.End);
					if (vwselNew.EndBeforeAnchor)
					{
						if (fAnchorEditable && tagAnchor > 0 && tagEnd < 0)
						{
							hlpr.SetTextPropId(SelectionHelper.SelLimitType.End, tagAnchor);
							hlpr.SetIch(SelectionHelper.SelLimitType.End, 0);
							fChangeRange = true;
						}
					}
					else
					{
						if (!fAnchorEditable && tagAnchor < 0 && tagEnd > 0)
						{
							hlpr.SetTextPropId(SelectionHelper.SelLimitType.Anchor, tagEnd);
							hlpr.SetIch(SelectionHelper.SelLimitType.Anchor, 0);
							fChangeRange = true;
						}
					}
				}

				if (fChangeRange)
				{
					hlpr.SetSelection(true);
				}
			}
			finally
			{
				s_fProcessingSelectionChanged = false;
			}
		}

		internal static void EliminateExtraStyleAndWsInfo(IFwMetaDataCache mdc, FwPasteFixTssEventArgs e, int flid)
		{
			var type = (CellarPropertyType)mdc.GetFieldType(flid);
			if (type == CellarPropertyType.MultiUnicode || type == CellarPropertyType.Unicode)
			{
				e.TsString = e.TsString.ToWsOnlyString();
			}
		}

		/// <summary />
		public override bool IsSelectionFormattable
		{
			get
			{
				if (!base.IsSelectionFormattable)
				{
					return false;
				}

				// We only want to allow applying styles in this type of control if the whole selection is in
				// the same writing system.
				var wsAnchor = EditingHelper.CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.Anchor);
				var wsEnd = EditingHelper.CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.End);
				return wsAnchor == wsEnd;
			}
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			WritingSystems = null;
			VC = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Make a rootbox. When changing this, give careful consideration to changing Reuse().
		/// </summary>
		public override void MakeRoot()
		{
			m_rootb = null;

			if (m_cache == null || DesignMode)
			{
				return;
			}

			WritingSystems = WritingSystemOptions;
			VC = new InnerLabeledMultiStringViewVc(Flid, WritingSystems, m_cache.WritingSystemFactory.UserWs, m_editable, this);

			base.MakeRoot();

			Debug.Assert(m_rootb != null);
			// And maybe this too, at least by default?
			m_rootb.DataAccess = m_cache.DomainDataByFlid;

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			// arg4 could be used to supply a stylesheet.
			m_rootb.SetRootObject(HvoObj, VC, 1, m_styleSheet);
			SetupOtherControls?.Invoke(this, new EventArgs());
		}

		/// <summary>
		/// This is the list of writing systems that can be enabled for this control. It should be either the Vernacular list
		/// or Analysis list shown in the WritingSystemPropertiesDialog which are checked and unchecked.
		/// </summary>
		public List<CoreWritingSystemDefinition> WritingSystemOptions => GetWritingSystemOptions(true);

		/// <summary>
		/// returns a list of the writing systems available to display for this view
		/// </summary>
		/// <param name="fIncludeUncheckedActiveWss">if false, include only current wss,
		/// if true, includes unchecked active wss.</param>
		public List<CoreWritingSystemDefinition> GetWritingSystemOptions(bool fIncludeUncheckedActiveWss)
		{
			var result = WritingSystemServices.GetWritingSystemList(m_cache, m_wsOptions, HvoObj, m_forceIncludeEnglish, fIncludeUncheckedActiveWss);
			if (fIncludeUncheckedActiveWss && m_wsAdditionalOptions != 0)
			{
				result = new List<CoreWritingSystemDefinition>(result); // just in case caller does not want it modified
				var additionalWss = WritingSystemServices.GetWritingSystemList(m_cache, m_wsAdditionalOptions, HvoObj, m_forceIncludeEnglish, true);
				foreach (var ws in additionalWss)
				{
					if (!result.Contains(ws))
					{
						result.Add(ws);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// if non-null, we'll use this list to determine which writing systems to display. These
		/// are the writing systems the user has checked in the WritingSystemPropertiesDialog.
		/// if null, we'll display every writing system option.
		/// </summary>
		public List<CoreWritingSystemDefinition> WritingSystemsToDisplay { get; set; }

		/// <summary />
		internal void TriggerDisplay(IVwEnv vwenv)
		{
			Display?.Invoke(this, new VwEnvEventArgs(vwenv));
		}

		/// <summary>
		/// Make a selection in the specified writing system at the specified character offset.
		/// </summary>
		public void SelectAt(int ws, int ich)
		{
			var cpropPrevious = 0;
			if (ws != WritingSystems[0].Handle)
			{
				// According to the documentation on RootBox.MakeTextSelection, cpropPrevious
				// needs to be the index into the ws array that matches the ws to be selected.
				cpropPrevious = GetRightPositionInWsArray(ws);
				if (cpropPrevious < 0)
				{
					Debug.Fail("InnerLabeledMultiStringView could not select correct ws");
					cpropPrevious = 0; // safety net to keep from crashing outright.
				}
			}
			try
			{
				RootBox.MakeTextSelection(0, 0, null, Flid, cpropPrevious, ich, ich, ws, true, -1, null, true);
			}
			catch (Exception)
			{
				Debug.Assert(false, "Unexpected failure to make selection in InnerLabeledMultiStringView");
			}
		}

		private int GetRightPositionInWsArray(int ws)
		{
			var i = 0;
			for (; i < WritingSystems.Count; i++)
			{
				if (WritingSystems[i].Handle == ws)
				{
					break;
				}
			}
			return i == WritingSystems.Count ? -1 : i;
		}
	}

	/// <summary>
	/// Delegate defn for an event handler that passes an IVwEnv.
	/// </summary>
	public delegate void VwEnvEventHandler (object sender, VwEnvEventArgs e);

	/// <summary>
	/// Event Args for an event that passes a VwEnv.
	/// </summary>
	public class VwEnvEventArgs : EventArgs
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public VwEnvEventArgs(IVwEnv env)
		{
			Environment = env;
		}

		/// <summary>
		/// Get the environment.
		/// </summary>
		public IVwEnv Environment { get; }
	}
}
