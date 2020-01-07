// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.Analyses
{
	internal sealed partial class OneAnalysisSandbox : SandboxBase
	{
		#region Construction

		public OneAnalysisSandbox()
		{
			SizeToContent = true;
			InitializeComponent();
		}

		/// <summary>
		/// Create a new one.
		/// </summary>
		public OneAnalysisSandbox(LcmCache cache, IVwStylesheet ss, InterlinLineChoices choices, int hvoAnalysis)
			: base(cache, ss, choices, hvoAnalysis)
		{
			SizeToContent = true;
			InitializeComponent();
		}

		#endregion Construction

		/// <summary>
		///  Pass through to the VC.
		/// </summary>
		protected override bool IsMorphemeFormEditable => false;

		/// <summary>
		/// Update the analysis to what the sandbox is currently.
		/// </summary>
		/// <returns>'true', if anything changed, otherwise 'false'.</returns>
		public bool UpdateAnalysis(IWfiAnalysis anal)
		{
			var uram = new UpdateRealAnalysisMethod(this, Caches, InterlinLineChoices, anal);
			var result = uram.UpdateRealAnalysis();
			Caches.DataAccess.ClearDirty();
			return result;
		}

		private sealed class UpdateRealAnalysisMethod : GetRealAnalysisMethod
		{
			private readonly IWfiAnalysis m_anal;
			private readonly IMoFormRepository m_moFormRepos;
			private readonly ILexSenseRepository m_senseRepos;
			private readonly IMoMorphSynAnalysisRepository m_msaRepos;

			/// <summary />
			internal UpdateRealAnalysisMethod(SandboxBase owner, CachePair caches, InterlinLineChoices choices, IWfiAnalysis anal)
			{
				m_sandbox = owner;
				m_caches = caches;
				m_hvoSbWord = kSbWord; // kSbWord really is a constant, not a real hvo.
				m_anal = anal;
				m_sda = m_caches.DataAccess;
				m_sdaMain = m_caches.MainCache.MainCacheAccessor;
				m_cmorphs = m_sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				m_choices = choices;
				var servLoc = m_caches.MainCache.ServiceLocator;
				m_moFormRepos = servLoc.GetInstance<IMoFormRepository>();
				m_senseRepos = servLoc.GetInstance<ILexSenseRepository>();
				m_msaRepos = servLoc.GetInstance<IMoMorphSynAnalysisRepository>();
			}

			/// <summary>
			/// Put the sandbox info into the real WfiAnalysis (based on the m_hvoWfiAnalysis id).
			/// </summary>
			/// <returns>'true', if anything changed, otherwise 'false'.</returns>
			internal bool UpdateRealAnalysis()
			{
				var isDirty = false;
				BuildMorphLists();
				m_sdaMain.BeginUndoTask(TextAndWordsResources.ksUndoEditAnalysis, TextAndWordsResources.ksRedoEditAnalysis);
				try
				{
					Debug.Assert(m_anal.MorphBundlesOS.Count == m_cmorphs); // Better be the same.
					for (var imorph = 0; imorph < m_cmorphs; imorph++)
					{
						var mb = m_anal.MorphBundlesOS[imorph];
						// Process Morph.
						var oldHvo = mb.MorphRA?.Hvo ?? 0;
						var newHvo = m_analysisMorphs[imorph];
						if (oldHvo != newHvo)
						{
							if (newHvo == 0)
							{
								// Change to 'unknown'. Will no longer be able to use the morph property to get
								// at the actual form, so reinstate it in the bundle.
								foreach (var ws in mb.Cache.ServiceLocator.WritingSystems.VernacularWritingSystems)
								{
									mb.Form.set_String(ws.Handle, mb.Cache.MainCacheAccessor.get_MultiStringAlt(oldHvo, MoFormTags.kflidForm, ws.Handle));
								}
								mb.MorphRA = null; // See LT-13878 for 'unrelated' crash reported by Santhosh
							}
							else
							{
								mb.MorphRA = m_moFormRepos.GetObject(newHvo);
							}
							isDirty = true;
						}
						// Process Sense.
						oldHvo = mb.SenseRA?.Hvo ?? 0;
						newHvo = m_analysisSenses[imorph];
						if (oldHvo != newHvo)
						{
							mb.SenseRA = newHvo == 0 ? null : m_senseRepos.GetObject(newHvo);
							isDirty = true;
						}
						// Process MSA.
						oldHvo = mb.MsaRA?.Hvo ?? 0;
						newHvo = m_analysisMsas[imorph];
						if (oldHvo == newHvo)
						{
							continue;
						}
						mb.MsaRA = newHvo == 0 ? null : m_msaRepos.GetObject(newHvo);
						isDirty = true;
					}
				}
				finally
				{
					if (isDirty)
					{
						m_sdaMain.EndUndoTask();
					}
					else
					{
						m_sdaMain.Rollback();
					}
				}
				return isDirty;
			}
		}
	}
}