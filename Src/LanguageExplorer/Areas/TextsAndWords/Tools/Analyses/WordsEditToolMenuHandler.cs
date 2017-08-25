// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.FwUtils;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.Analyses
{
	/// <summary>
	/// WordsEditToolMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// this class would normally be constructed by the factory method on DTMenuHandler,
	/// when the XML configuration of the RecordEditView specifies this class.
	///
	/// This is an IFlexComponent, so it gets a chance to modify
	/// the display characteristics of the menu just before the menu is displayed.
	/// </summary>
	internal sealed class WordsEditToolMenuHandler : DTMenuHandler
	{
		private XmlNode m_mainWindowNode;

		#region Properties

		private XmlNode MainWindowNode
		{
			get
			{
				if (m_mainWindowNode == null)
					m_mainWindowNode = PropertyTable.GetValue<XmlNode>("WindowConfiguration");
				return m_mainWindowNode;
			}
		}

		/// <summary>
		/// Returns the object of the current slice, or (if no slice is marked current)
		/// the object of the first slice, or (if there are no slices, or no data entry form) null.
		/// </summary>
		private ICmObject CurrentSliceObject
		{
			get
			{
				if (m_dataEntryForm == null)
					return null;
				if (m_dataEntryForm.CurrentSlice != null)
					return m_dataEntryForm.CurrentSlice.Object;
				if (m_dataEntryForm.Slices.Count == 0)
					return null;
				return m_dataEntryForm.FieldAt(0).Object;
			}
		}

		private IWfiWordform Wordform
		{
			get
			{
				// Note that we may get here after the owner (or the owner's owner) of the
				// current object has been deleted: see LT-10124.
				var curObject = CurrentSliceObject;
				if (curObject is IWfiWordform)
					return (IWfiWordform)curObject;

				if (curObject is IWfiAnalysis && curObject.Owner != null)
					return (IWfiWordform)(curObject.Owner);

				if (curObject is IWfiGloss && curObject.Owner != null)
				{
					var anal = curObject.OwnerOfClass<IWfiAnalysis>();
					if (anal.Owner != null)
						return anal.OwnerOfClass<IWfiWordform>();
				}
				return null;
			}
		}

		private IWfiAnalysis Analysis
		{
			get
			{
				var curObject = CurrentSliceObject;
				if (curObject is IWfiAnalysis)
					return (IWfiAnalysis)curObject;
				if (curObject is IWfiGloss)
					return curObject.OwnerOfClass<IWfiAnalysis>();
				return null;
			}
		}

		private IWfiGloss Gloss
		{
			get
			{
				var curObject = CurrentSliceObject;
				if (curObject is IWfiGloss)
					return (IWfiGloss)curObject;
				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		private bool InFriendlyArea
		{
			get
			{
				return (PropertyTable.GetValue<string>("areaChoice") == "textsWords");
			}
		}

		#endregion Properties

		#region Construction

		//need a default constructor for dynamic loading
		public WordsEditToolMenuHandler()
		{
		}

		#endregion Construction

		#region Other methods

		private void SetNewStatus(IWfiAnalysis anal, int newStatus)
		{
			int currentStatus = anal.ApprovalStatusIcon;
			if (currentStatus == newStatus)
				return;

			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoChangingApprovalStatus,
				LanguageExplorerResources.ksRedoChangingApprovalStatus, Cache.ActionHandlerAccessor,
				() =>
				{
					if (currentStatus == 1)
						anal.MoveConcAnnotationsToWordform();
					anal.ApprovalStatusIcon = newStatus;
					if (newStatus == 1)
					{
						// make sure default senses are set to be real values,
						// since the user has seen the defaults, and approved the analysis based on them.
						foreach (var mb in anal.MorphBundlesOS)
						{
							var currentSense = mb.SenseRA;
							if (currentSense == null)
								mb.SenseRA = mb.DefaultSense;
						}
					}
				});

			// Wipe all of the old slices out, so we get new numbers and newly placed objects.
			// This fixes LT-5935. Also removes the need to somehow make the virtual properties like HumanApprovedAnalyses update.
			m_dataEntryForm.RefreshList(true);
		}

		private void ShowConcDlg(ICmObject concordOnObject)
		{
			using (var ctrl = new ConcordanceDlg(concordOnObject))
			{
				ctrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				ctrl.Launch();
			}
		}

		#endregion Other methods

		#region XCore Message handlers

		#region Concordance Message handlers

#if RANDYTODO
		public virtual bool OnDisplayShowWordformConc(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnShowWordformConc(object argument)
		{
			var wf = Wordform;
			if (wf == null)
				throw new InvalidOperationException("Could not find wordform object.");

			ShowConcDlg(wf);

			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayShowWordGlossConc(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnShowWordGlossConc(object argument)
		{
			var gloss = Gloss;
			if (gloss == null)
				throw new InvalidOperationException("Could not find gloss object.");

			ShowConcDlg(gloss);

			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayShowHumanApprovedAnalysisConc(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnShowHumanApprovedAnalysisConc(object argument)
		{
			var anal = Analysis;
			if (anal == null)
				throw new InvalidOperationException("Could not find analysis object.");

			ShowConcDlg(anal);

			return true;
		}

#if RANDYTODO
	/// <summary>
	///
	/// </summary>
	/// <param name="commandObject"></param>
	/// <param name="display"></param>
	/// <returns></returns>
		public virtual bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			var cmd = (Command)commandObject;
			var className = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			var specifiedClsid = 0;
			if ((Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged).ClassExists(className))
				specifiedClsid = Cache.MetaDataCacheAccessor.GetClassId(className);
			var anal = Analysis;
			if (anal != null)
			{
				if (anal.ClassID == specifiedClsid)
				{
					display.Enabled = display.Visible = true;
					return true;
				}

				if (specifiedClsid == WfiGlossTags.kClassId)
				{
					if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null &&
						CurrentSliceObject != null && CurrentSliceObject.ClassID == specifiedClsid)
					{
						display.Enabled = display.Visible = true;
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
			var cmd = (Command)commandObject;
			var className = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			var guid = Guid.Empty;
			switch (className)
			{
				case "WfiAnalysis":
					var anal = Analysis;
					if (anal != null)
						guid = anal.Guid;
					break;
				case "WfiGloss":
					if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null &&
						CurrentSliceObject != null && CurrentSliceObject.ClassID == WfiGlossTags.kClassId)
					{
						guid = CurrentSliceObject.Guid;
					}
					break;
			}
			if (guid != Guid.Empty)
			{
				var tool = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
				var commands = new List<string>
											{
												"AboutToFollowLink",
												"FollowLink"
											};
				var parms = new List<object>
											{
												null,
												new FwLinkArgs(tool, guid)
											};
				Publisher.Publish(commands, parms);
				return true;
			}
			return false;
		}
#endif
		#endregion Concordance Message handlers

		#region Approval Status Message handlers

#if RANDYTODO
		public virtual bool OnDisplayAnalysisApprove(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			display.Checked = Analysis != null && Analysis.ApprovalStatusIcon == 1;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAnalysisApprove(object argument)
		{
			IWfiAnalysis anal = Analysis;
			Debug.Assert(anal != null, "Could not find analysis object.");
			SetNewStatus(anal, 1);

			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayAnalysisUnknown(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			display.Checked = Analysis.ApprovalStatusIcon == 0;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAnalysisUnknown(object argument)
		{
			IWfiAnalysis anal = Analysis;
			Debug.Assert(anal != null, "Could not find analysis object.");
			SetNewStatus(anal, 0);

			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayAnalysisDisapprove(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			display.Checked = Analysis.ApprovalStatusIcon == 2;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAnalysisDisapprove(object argument)
		{
			IWfiAnalysis anal = Analysis;
			Debug.Assert(anal != null, "Could not find analysis object.");
			SetNewStatus(anal, 2);

			return true;
		}

		#endregion Approval Status Message handlers

#if NOTYET
		#region SpellingStatus Message handlers

		public virtual bool OnDisplaySpellingStatusUnknown(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnSpellingStatusUnknown(object argument)
		{
			MessageBox.Show("TODO: Set spelling status to 'Unknown' goes here, when the integer values that get stored in the database get defined.");

			return true;
		}

		public virtual bool OnDisplaySpellingStatusGood(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnSpellingStatusGood(object argument)
		{
			MessageBox.Show("TODO: Set spelling status to 'Good' goes here, when the integer values that get stored in the database get defined.");

			return true;
		}

		public virtual bool OnDisplaySpellingStatusDisapprove(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnSpellingStatusDisapprove(object argument)
		{
			MessageBox.Show("TODO: Set spelling status to 'Disapproved' goes here, when the integer values that get stored in the database get defined.");

			return true;
		}

		#endregion SpellingStatus Message handlers
#endif

		#region Wordform edit Message handlers

#if RANDYTODO
		protected override bool DeleteObject(Command command)
		{
			if (base.DeleteObject(command))
			{
				// Wipe all of the old slices out,
				// so we get new numbers, where needed.
				// This fixes LT-5974.
				m_dataEntryForm.RefreshList(true);
				return true;
			}
			return false;
		}

		public virtual bool OnDisplayWordformEditForm(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnWordformEditForm(object argument)
		{
			MessageBox.Show(LanguageExplorerResources.ksTodo_WordformEditing);

			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayWordformChangeCase(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnWordformChangeCase(object argument)
		{
			MessageBox.Show("TODO: Case changing editing happens here.");

			return true;
		}

		#endregion Wordform edit Message handlers

		#region New analysis message handler

#if RANDYTODO
		public virtual bool OnDisplayAddApprovedAnalysis(object commandObject,
			ref UIItemDisplayProperties display)
		{
			// The null test covers cases where there is no current object because the list (as filtered) is empty.
			if (InFriendlyArea && m_mediator != null && m_dataEntryForm.Root != null)
			{
#pragma warning disable 0219
				display.Visible = true;
				display.Enabled = Wordform != null;
#pragma warning restore 0219
			}
			else
			{
				display.Enabled = display.Visible = false;
			}
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAddApprovedAnalysis(object argument)
		{
			var mainWnd = (IFwMainWnd)m_dataEntryForm.FindForm();
			using (EditMorphBreaksDlg dlg = new EditMorphBreaksDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				IWfiWordform wf = Wordform;
				if (wf == null)
					return true;
				ITsString tssWord = Wordform.Form.BestVernacularAlternative;
				string morphs = tssWord.Text;
				var cache = Cache;
				dlg.Initialize(tssWord, morphs, cache.MainCacheAccessor.WritingSystemFactory,
					cache, m_dataEntryForm.StyleSheet);
				// Making the form active fixes problems like LT-2619.
				// I'm (RandyR) not sure what adverse impact might show up by doing this.
				((Form)mainWnd).Activate();
				if (dlg.ShowDialog(((Form)mainWnd)) == DialogResult.OK)
				{
					morphs = dlg.GetMorphs().Trim();
					if (morphs.Length == 0)
						return true;

					string[] prefixMarkers = MorphServices.PrefixMarkers(cache);
					string[] postfixMarkers = MorphServices.PostfixMarkers(cache);

					List<string> allMarkers = new List<string>();
					foreach (string s in prefixMarkers)
					{
						allMarkers.Add(s);
					}

					foreach (string s in postfixMarkers)
					{
						if (!allMarkers.Contains(s))
							allMarkers.Add(s);
					}
					allMarkers.Add(" ");

					string[] breakMarkers = new string[allMarkers.Count];
					for (int i = 0; i < allMarkers.Count; ++i)
						breakMarkers[i] = allMarkers[i];

					string fullForm = SandboxBase.MorphemeBreaker.DoBasicFinding(morphs, breakMarkers, prefixMarkers, postfixMarkers);

#if RANDYTODO
					var command = (Command) argument;
					UndoableUnitOfWorkHelper.Do(command.UndoText, command.RedoText, cache.ActionHandlerAccessor,
						() =>
							{
								IWfiAnalysis newAnalysis = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
								Wordform.AnalysesOC.Add(newAnalysis);
								newAnalysis.ApprovalStatusIcon = 1; // Make it human approved.
								int vernWS = TsStringUtils.GetWsAtOffset(tssWord, 0);
								foreach (string morph in fullForm.Split(SIL.Utils.Unicode.SpaceChars))
								{
									if (morph != null && morph.Length != 0)
									{
										IWfiMorphBundle mb = cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
										newAnalysis.MorphBundlesOS.Add(mb);
										mb.Form.set_String(vernWS, Cache.TsStrFactory.MakeString(morph, vernWS));
									}
								}
							});
#endif
				}
			}
			return true;
		}

		#endregion New analysis message handler

		#endregion XCore Message handlers
	}
}