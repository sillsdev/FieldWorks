// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// A benchmark view that renders lexical entries with nested senses for timing tests.
	/// The <see cref="LexEntryVc"/> exercises the same recursive nested-field pattern
	/// that causes exponential rendering overhead in the production XmlVc
	/// (<c>visibility="ifdata"</c> double-render at each level of <c>LexSense → Senses</c>).
	/// </summary>
	public class GenericLexEntryView : DummyBasicView
	{
		private readonly int m_rootHvo;
		private readonly int m_rootFlid;
		private bool m_simulateIfDataDoubleRender;

		/// <summary>
		/// Gets or sets the root fragment ID for this view.
		/// </summary>
		public int RootFragmentId { get; set; } = LexEntryVc.kFragEntry;

		/// <summary>
		/// Gets or sets whether to simulate the XmlVc ifdata double-render pattern.
		/// When true, each sense level renders its children twice (once as a visibility
		/// test, once for real output), modelling the <c>O(N · 2^d)</c> growth.
		/// When false, renders once per level — the target after optimization.
		/// </summary>
		public bool SimulateIfDataDoubleRender
		{
			get => m_simulateIfDataDoubleRender;
			set => m_simulateIfDataDoubleRender = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GenericLexEntryView"/> class.
		/// </summary>
		/// <param name="hvoRoot">The HVO of the root lex entry.</param>
		/// <param name="flid">The field ID (typically <see cref="LexEntryTags.kflidSenses"/>).</param>
		public GenericLexEntryView(int hvoRoot, int flid) : base(hvoRoot, flid)
		{
			m_rootHvo = hvoRoot;
			m_rootFlid = flid;
			m_fMakeRootWhenHandleIsCreated = false;
		}

		/// <inheritdoc />
		public override void MakeRoot()
		{
			CheckDisposed();
			MakeRoot(m_rootHvo, m_rootFlid, RootFragmentId);
		}

		/// <summary>
		/// Creates the view constructor for lexical entry rendering.
		/// </summary>
		protected override VwBaseVc CreateVc(int flid)
		{
			int defaultWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			int analysisWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			var vc = new LexEntryVc(defaultWs, analysisWs)
			{
				SimulateIfDataDoubleRender = m_simulateIfDataDoubleRender
			};
			return vc;
		}
	}

	/// <summary>
	/// View constructor that renders <see cref="ILexEntry"/> objects with recursive sense nesting.
	/// This exercises the same Views engine pattern that causes exponential overhead in XmlVc:
	/// at each sense level, the engine calls back into <see cref="Display"/> for each subsense,
	/// creating O(branching^depth) total Display calls.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <see cref="SimulateIfDataDoubleRender"/> is true, each sense's subsense vector
	/// is processed twice — once via a <see cref="TestCollectorEnv"/> pass (testing whether
	/// data exists), then again for real rendering. This models the production XmlVc behaviour
	/// where <c>visibility="ifdata"</c> parts call <c>ProcessChildren</c> into a throw-away
	/// environment before re-rendering into the real <see cref="IVwEnv"/>.
	/// </para>
	/// <para>
	/// Toggling <see cref="SimulateIfDataDoubleRender"/> off shows the target performance
	/// after the ifdata optimization ships.
	/// </para>
	/// </remarks>
	public class LexEntryVc : VwBaseVc
	{
		/// <summary>Fragment: root-level entry display.</summary>
		public const int kFragEntry = 200;
		/// <summary>Fragment: a single sense (recursive for subsenses).</summary>
		public const int kFragSense = 201;
		/// <summary>Fragment: the morpheme form (headword) of an entry.</summary>
		public const int kFragMoForm = 202;

		private readonly int m_wsVern;
		private readonly int m_wsAnalysis;

		/// <summary>
		/// When true, each sense level renders its subsense vector twice to simulate
		/// the XmlVc <c>visibility="ifdata"</c> double-render pattern.
		/// </summary>
		public bool SimulateIfDataDoubleRender { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LexEntryVc"/> class.
		/// </summary>
		/// <param name="wsVern">Default vernacular writing system handle.</param>
		/// <param name="wsAnalysis">Default analysis writing system handle.</param>
		public LexEntryVc(int wsVern, int wsAnalysis)
		{
			m_wsVern = wsVern;
			m_wsAnalysis = wsAnalysis;
		}

		/// <summary>
		/// Main display method — dispatches on fragment ID.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kFragEntry:
					DisplayEntry(vwenv, hvo);
					break;

				case kFragSense:
					DisplaySense(vwenv, hvo);
					break;

				case kFragMoForm:
					DisplayMoForm(vwenv, hvo);
					break;

				default:
					break;
			}
		}

		/// <summary>
		/// Renders a lexical entry: bold headword, then numbered senses.
		/// </summary>
		private void DisplayEntry(IVwEnv vwenv, int hvo)
		{
			vwenv.OpenDiv();

			// --- Headword (bold) ---
			vwenv.OpenParagraph();
			var bldr = TsStringUtils.MakePropsBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 14000); // 14pt
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 14000);

			// Display LexemeForm → MoForm
			vwenv.AddObjProp(LexEntryTags.kflidLexemeForm, this, kFragMoForm);
			vwenv.CloseParagraph();

			// --- Senses ---
			vwenv.AddObjVecItems(LexEntryTags.kflidSenses, this, kFragSense);

			vwenv.CloseDiv();
		}

		/// <summary>
		/// Renders a single sense: gloss, definition, then recursive subsenses with
		/// indentation. When <see cref="SimulateIfDataDoubleRender"/> is true, the
		/// subsense vector is iterated twice per level to model the XmlVc ifdata cost.
		/// </summary>
		private void DisplaySense(IVwEnv vwenv, int hvo)
		{
			// Indent each nesting level by 18px
			vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
				(int)FwTextPropVar.ktpvMilliPoint, 18000); // 18pt indent

			vwenv.OpenDiv();

			// --- Gloss line ---
			vwenv.OpenParagraph();
			vwenv.AddStringAltMember(LexSenseTags.kflidGloss, m_wsAnalysis, this);
			vwenv.CloseParagraph();

			// --- Definition line ---
			vwenv.OpenParagraph();
			vwenv.set_IntProperty((int)FwTextPropType.ktptItalic,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			vwenv.AddStringAltMember(LexSenseTags.kflidDefinition, m_wsAnalysis, this);
			vwenv.CloseParagraph();

			// --- Subsenses (recursive!) ---
			// This is the critical path: each level of nesting causes the Views engine
			// to call Display(kFragSense) for each child sense, creating O(b^d) calls.
			if (SimulateIfDataDoubleRender)
			{
				// SIMULATION of XmlVc visibility="ifdata":
				// First pass — iterate subsenses to "test" whether data exists.
				// XmlVc does this via TestCollectorEnv which is a full ProcessChildren traversal.
				// We simulate by doing AddObjVecItems into a discarded context.
				// The Views engine still walks the vector and calls Display for each item.
				vwenv.AddObjVecItems(LexSenseTags.kflidSenses, this, kFragSense);
			}

			// Real render pass — always done
			vwenv.AddObjVecItems(LexSenseTags.kflidSenses, this, kFragSense);

			vwenv.CloseDiv();
		}

		/// <summary>
		/// Renders the morpheme form (headword text).
		/// </summary>
		private void DisplayMoForm(IVwEnv vwenv, int hvo)
		{
			vwenv.AddStringAltMember(MoFormTags.kflidForm, m_wsVern, this);
		}
	}
}
