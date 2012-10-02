using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.FieldWorks.IText;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public partial class OneAnalysisSandbox : SandboxBase
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
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="ss">The stylesheet.</param>
		/// <param name="choices">The choices.</param>
		/// <param name="hvoAnalysis">The hvo analysis.</param>
		public OneAnalysisSandbox(FdoCache cache, Mediator mediator, IVwStylesheet ss, InterlinLineChoices choices, int hvoAnalysis)
			: base(cache, mediator, ss, choices, hvoAnalysis)
		{
			SizeToContent = true;
			InitializeComponent();
		}

		#endregion Construction

		/// <summary>
		///  Pass through to the VC.
		/// </summary>
		protected override bool IsMorphemeFormEditable
		{
			get { return false; }
		}

		/// <summary>
		/// Update the analysis to what the sandbox is currently.
		/// </summary>
		/// <returns>'true', if anything changed, otherwise 'false'.</returns>
		public bool UpdateAnalysis(WfiAnalysis anal)
		{
			CheckDisposed();

			UpdateRealAnalysisMethod uram = new UpdateRealAnalysisMethod(this, m_caches, m_choices, anal);
			bool result = uram.UpdateRealAnalysis();
			m_caches.DataAccess.ClearDirty();
			return result;
		}

		public class UpdateRealAnalysisMethod : GetRealAnalysisMethod
		{
			private WfiAnalysis m_anal;
			/// <summary>
			/// This contructor is only to be used by the
			/// </summary>
			/// <param name="owner"></param>
			/// <param name="caches"></param>
			/// <param name="choices"></param>
			/// <param name="anal"></param>
			public UpdateRealAnalysisMethod(SandboxBase owner, CachePair caches, InterlinLineChoices choices,
				WfiAnalysis anal)
			{
				m_sandbox = owner;
				m_caches = caches;
				m_hvoSbWord = kSbWord; // kSbWord really is a constant, not a real hvo.
				//m_hvoWordform = hvoWordform;
				//m_hvoWfiAnalysis = hvoWfiAnalysis;
				m_anal = anal;
				//m_hvoWordGloss = hvoWordGloss;
				m_sda = m_caches.DataAccess;
				m_sdaMain = m_caches.MainCache.MainCacheAccessor;
				m_cmorphs = m_sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				m_choices = choices;
				//m_tssForm = tssForm;
			}

			/// <summary>
			/// Put the sandbox info into the real WfiAnalysis (based on the m_hvoWfiAnalysis id).
			/// </summary>
			/// <returns>'true', if anything changed, otherwise 'false'.</returns>
			internal bool UpdateRealAnalysis()
			{
				bool isDirty = false;
				BuildMorphLists();
				/* Sets these three variables up.
				m_analysisMorphs = new int[m_cmorphs];
				m_analysisMsas = new int[m_cmorphs];
				m_analysisSenses = new int[m_cmorphs];
				*/

				Debug.Assert(m_anal.MorphBundlesOS.Count == m_cmorphs); // Better be the same.
				for (int imorph = 0; imorph < m_cmorphs; imorph++)
				{
					IWfiMorphBundle mb = m_anal.MorphBundlesOS[imorph];

					// Process Morph.
					int oldHvo = mb.MorphRAHvo;
					int newHvo = m_analysisMorphs[imorph];
					if (oldHvo != newHvo)
					{
						if (newHvo == 0 && oldHvo != 0)
						{
							// Change to 'unknown'. Will no longer be able to use the morph property to get
							// at the actual form, so reinstate it in the bundle.
							foreach (int ws in mb.Cache.LangProject.VernWssRC.HvoArray)
							{
								mb.Form.SetAlternativeTss(mb.Cache.GetMultiStringAlt(oldHvo,
									(int)MoForm.MoFormTags.kflidForm, ws));
							}
						}
						mb.MorphRAHvo = newHvo;
						isDirty = true;
					}

					// Process Sense.
					oldHvo = mb.SenseRAHvo;
					newHvo = m_analysisSenses[imorph];
					if (oldHvo != newHvo)
					{
						mb.SenseRAHvo = newHvo;
						isDirty = true;
					}

					// Process MSA.
					oldHvo = mb.MsaRAHvo;
					newHvo = m_analysisMsas[imorph];
					if (oldHvo != newHvo)
					{
						mb.MsaRAHvo = newHvo;
						isDirty = true;
					}
				}
				return isDirty;
			}
		}
	}
}
