using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
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
		/// <param name="propertyTable"></param>
		/// <param name="ss">The stylesheet.</param>
		/// <param name="choices">The choices.</param>
		/// <param name="hvoAnalysis">The hvo analysis.</param>
		public OneAnalysisSandbox(FdoCache cache, Mediator mediator, IPropertyTable propertyTable, IVwStylesheet ss, InterlinLineChoices choices, int hvoAnalysis)
			: base(cache, mediator, propertyTable, ss, choices, hvoAnalysis)
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
		public bool UpdateAnalysis(IWfiAnalysis anal)
		{
			CheckDisposed();

			var uram = new UpdateRealAnalysisMethod(this, m_caches, m_choices, anal);
			var result = uram.UpdateRealAnalysis();
			m_caches.DataAccess.ClearDirty();
			return result;
		}

		public class UpdateRealAnalysisMethod : GetRealAnalysisMethod
		{
			private readonly IWfiAnalysis m_anal;
			private readonly IMoFormRepository m_moFormRepos;
			private readonly ILexSenseRepository m_senseRepos;
			private readonly IMoMorphSynAnalysisRepository m_msaRepos;
			/// <summary>
			/// This contructor is only to be used by the
			/// </summary>
			/// <param name="owner"></param>
			/// <param name="caches"></param>
			/// <param name="choices"></param>
			/// <param name="anal"></param>
			public UpdateRealAnalysisMethod(SandboxBase owner, CachePair caches, InterlinLineChoices choices,
				IWfiAnalysis anal)
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
				/* Sets these three variables up.
				m_analysisMorphs = new int[m_cmorphs];
				m_analysisMsas = new int[m_cmorphs];
				m_analysisSenses = new int[m_cmorphs];
				*/
				m_sdaMain.BeginUndoTask(MEStrings.ksUndoEditAnalysis, MEStrings.ksRedoEditAnalysis);

				try
				{
					Debug.Assert(m_anal.MorphBundlesOS.Count == m_cmorphs); // Better be the same.
					for (var imorph = 0; imorph < m_cmorphs; imorph++)
					{
						var mb = m_anal.MorphBundlesOS[imorph];

						// Process Morph.
						var oldHvo = mb.MorphRA == null ? 0 : mb.MorphRA.Hvo;
						var newHvo = m_analysisMorphs[imorph];
						if (oldHvo != newHvo)
						{
							if (newHvo == 0)
							{
								// Change to 'unknown'. Will no longer be able to use the morph property to get
								// at the actual form, so reinstate it in the bundle.
								foreach (IWritingSystem ws in mb.Cache.ServiceLocator.WritingSystems.VernacularWritingSystems)
									mb.Form.set_String(ws.Handle, mb.Cache.MainCacheAccessor.get_MultiStringAlt(oldHvo, MoFormTags.kflidForm, ws.Handle));
								mb.MorphRA = null; // See LT-13878 for 'unrelated' crash reported by Santhosh
							}
							else
							{
								mb.MorphRA = m_moFormRepos.GetObject(newHvo);
							}
							isDirty = true;
						}

						// Process Sense.
						oldHvo = mb.SenseRA == null ? 0 : mb.SenseRA.Hvo;
						newHvo = m_analysisSenses[imorph];
						if (oldHvo != newHvo)
						{
							mb.SenseRA = newHvo == 0 ? null : m_senseRepos.GetObject(newHvo);
							isDirty = true;
						}

						// Process MSA.
						oldHvo = mb.MsaRA == null ? 0 : mb.MsaRA.Hvo;
						newHvo = m_analysisMsas[imorph];
						if (oldHvo == newHvo)
							continue;
						mb.MsaRA = newHvo == 0 ? null : m_msaRepos.GetObject(newHvo);
						isDirty = true;
					}
				}
				finally
				{
					if (isDirty)
						m_sdaMain.EndUndoTask();
					else
						m_sdaMain.Rollback();
				}
				return isDirty;
			}
		}
	}
}
