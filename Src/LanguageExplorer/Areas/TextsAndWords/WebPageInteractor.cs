// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using Gecko;
using LanguageExplorer.Controls;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.FwCoreDlgs.Controls;

namespace LanguageExplorer.Areas.TextsAndWords
{
	internal sealed class WebPageInteractor
	{
		private readonly HtmlControl m_htmlControl;
		private readonly IPublisher m_publisher;
		private readonly LcmCache m_cache;
		private readonly FwTextBox m_tbWordForm;

		public WebPageInteractor(HtmlControl htmlControl, IPublisher publisher, LcmCache cache, FwTextBox tbWordForm)
		{
			m_htmlControl = htmlControl;
			m_publisher = publisher;
			m_cache = cache;
			m_tbWordForm = tbWordForm;
			m_htmlControl.Browser.DomClick += HandleDomClick;
		}

		private bool TryGetHvo(GeckoElement element, out int hvo)
		{
			while (element != null)
			{
				switch (element.TagName.ToLowerInvariant())
				{
					case "table":
					case "span":
					case "th":
					case "td":
						var id = element.GetAttribute("id");
						if (!string.IsNullOrEmpty(id))
						{
							return int.TryParse(id, out hvo);
						}
						break;
				}
				element = element.ParentElement;
			}

			hvo = 0;
			return false;
		}

		private void HandleDomClick(object sender, DomMouseEventArgs e)
		{
			if (sender == null || e?.Target == null)
			{
				return;
			}

			var elem = e.Target.CastToGeckoElement();
			int hvo;
			if (TryGetHvo(elem, out hvo))
			{
				JumpToToolBasedOnHvo(hvo);
			}

			if (!elem.TagName.Equals("input", StringComparison.InvariantCultureIgnoreCase) || !elem.GetAttribute("type")
				    .Equals("button", StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}
			switch (elem.GetAttribute("name"))
			{
				case "ShowWordGrammarDetail":
					ShowWordGrammarDetail(elem.GetAttribute("id"));
					break;

				case "TryWordGrammarAgain":
					TryWordGrammarAgain(elem.GetAttribute("id"));
					break;

				case "GoToPreviousWordGrammarPage":
					GoToPreviousWordGrammarPage();
					break;
			}
		}

		/// <summary>
		/// Set the current parser to use when tracing
		/// </summary>
		public XAmpleWordGrammarDebugger WordGrammarDebugger { get; set; }

		/// <summary>
		/// Have the main FLEx window jump to the appropriate item
		/// </summary>
		/// <param name="hvo">item whose parent will indcate where to jump to</param>
		public void JumpToToolBasedOnHvo(int hvo)
		{
			if (hvo == 0)
			{
				return;
			}
			string sTool = null;
			var parentClassId = 0;
			var cmo = m_cache.ServiceLocator.GetObject(hvo);
			switch (cmo.ClassID)
			{
				case MoFormTags.kClassId:					// fall through
				case MoAffixAllomorphTags.kClassId:			// fall through
				case MoStemAllomorphTags.kClassId:			// fall through
				case MoInflAffMsaTags.kClassId:				// fall through
				case MoDerivAffMsaTags.kClassId:			// fall through
				case MoUnclassifiedAffixMsaTags.kClassId:	// fall through
				case MoStemMsaTags.kClassId:				// fall through
				case MoMorphSynAnalysisTags.kClassId:		// fall through
				case MoAffixProcessTags.kClassId:
					sTool = AreaServices.LexiconEditMachineName;
					parentClassId = LexEntryTags.kClassId;
					break;
				case MoInflAffixSlotTags.kClassId:		// fall through
				case MoInflAffixTemplateTags.kClassId:	// fall through
				case PartOfSpeechTags.kClassId:
					sTool = AreaServices.PosEditMachineName;
					parentClassId = PartOfSpeechTags.kClassId;
					break;
				// still need to test compound rule ones
				case MoCoordinateCompoundTags.kClassId:	// fall through
				case MoEndoCompoundTags.kClassId:		// fall through
				case MoExoCompoundTags.kClassId:
					sTool = AreaServices.CompoundRuleAdvancedEditMachineName;
					parentClassId = cmo.ClassID;
					break;
				case PhRegularRuleTags.kClassId:		// fall through
				case PhMetathesisRuleTags.kClassId:
					sTool = AreaServices.PhonologicalRuleEditMachineName;
					parentClassId = cmo.ClassID;
					break;
				case PhPhonemeTags.kClassId:
					sTool = AreaServices.PhonemeEditMachineName;
					parentClassId = cmo.ClassID;
					break;
			}
			if (parentClassId <= 0)
			{
				return; // do nothing
			}
			cmo = CmObjectUi.GetSelfOrParentOfClass(cmo, parentClassId);
			if (cmo == null)
			{
				return; // do nothing
			}
			LinkHandler.PublishFollowLinkMessage(m_publisher, new FwLinkArgs(sTool, cmo.Guid));
		}

		/// <summary>
		/// Show the first pass of the Word Grammar Debugger
		/// </summary>
		/// <param name="sNodeId">The node id in the XAmple trace to use</param>
		public void ShowWordGrammarDetail(string sNodeId)
		{
			var sForm = AdjustForm(m_tbWordForm.Text);
			m_htmlControl.URL = WordGrammarDebugger.SetUpWordGrammarDebuggerPage(sNodeId, sForm, m_htmlControl.URL);
		}
		/// <summary>
		/// Try another pass in the Word Grammar Debugger
		/// </summary>
		/// <param name="sNodeId">the node id of the step to try</param>
		public void TryWordGrammarAgain(string sNodeId)
		{
			var sForm = AdjustForm(m_tbWordForm.Text);
			m_htmlControl.URL = WordGrammarDebugger.PerformAnotherWordGrammarDebuggerStepPage(sNodeId, sForm, m_htmlControl.URL);
		}
		/// <summary>
		/// Back up a page in the Word Grammar Debugger
		/// </summary>
		/// <remarks>
		/// We cannot merely use the history mechanism of the html control
		/// because we need to keep track of the xml page source file as well as the html page.
		/// This info is kept in the WordGrammarStack.
		/// </remarks>
		public void GoToPreviousWordGrammarPage()
		{
			m_htmlControl.URL = WordGrammarDebugger.PopWordGrammarStack();
		}
		/// <summary>
		/// Modify the content of the form to use entities when needed
		/// </summary>
		/// <param name="sForm">form to adjust</param>
		/// <returns>adjusted form</returns>
		private static string AdjustForm(string sForm)
		{
			return sForm.Replace("&", "&amp;").Replace("<", "&lt;");
		}
	}
}