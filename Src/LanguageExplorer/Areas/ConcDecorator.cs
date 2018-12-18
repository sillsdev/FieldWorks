// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// This class exists to decorate the main SDA for concordance views and browse views that show occurrence counts.
	/// It implements the Occurrences and OccurrencesCount properties for WfiWordform, WfiAnalysis, and/or WfiGloss.
	/// Both occurrences and OccurrencesCount are decorator rather than virtual properties because the results depend
	/// on per-window choices of which texts to include.
	/// It also implements the property ConcOccurrences, which is a decorator-only object as well as a decorator-only property.
	/// ConcOccurrences is the top-level list of occurrences of whatever the user asked to search for in Concordance view.
	/// Occurrences are 'fake' HVOs that don't correspond to any real object in the database. They also have properties only recognized
	/// by the decorator, such as a reference, begin and end character offsets (in the original paragraph string...used to bold the
	/// keyword), the object (typically StTxtPara) and segment they belong to, and several other derived properties which can
	/// be displayed in optional columns of the concordance views.
	/// </summary>
	internal class ConcDecorator : DomainDataByFlidDecoratorBase, IAnalysisOccurrenceFromHvo, IFlexComponent
	{
		/// <summary>
		/// Maps from wf hvo to array of dummy HVOs generated to represent occurrences.
		/// </summary>
		private Dictionary<int, int[]> m_values = new Dictionary<int, int[]>();
		private Dictionary<int, IParaFragment> m_occurrences = new Dictionary<int, IParaFragment>();
		private ILcmServiceLocator m_services;
		// This variable supports kflidConcOccurrences, the root list for the Concordance view (as opposed to the various word list views).
		// The value is determined by the concordance control and inserted into this class.
		private int[] m_concValues = new int[0];
		private int m_notifieeCount; // How many things are we notifying?
		private InterestingTextList m_interestingTexts;
		private bool m_fRefreshSuspended;

		public ConcDecorator(ILcmServiceLocator services)
			: base(services.GetInstance<ISilDataAccessManaged>())
		{
			m_services = services;
			SetOverrideMdc(new ConcMdc(MetaDataCache as IFwMetaDataCacheManaged));
		}

		internal const int kflidWfOccurrences = 899923; // occurrences of a wordform
		internal const int kflidOccurrenceCount = 899924;
		internal const int kflidReference = 899925; // 'Reference' of an occurrence.
		public const int kflidBeginOffset = 899926; // 'BeginOffset' of an occurrence.
		public const int kflidEndOffset = 899927; // 'EndOffset' of an occurrence.
		public const int kflidTextObject = 899928; // The object that has the text (from an occurrence).
		internal const int kflidSenseOccurrences = 899929; // top-level property for occurrences of a sense.
		public const int kflidSegment = 899930; // segment from occurrence.
		public const int kflidConcOccurrences = 899931; // occurrences in Concordance view, supposedly of LangProject.
		public const int kflidAnalysis = 899932; // from fake concordance object to Analysis.
		public const int kflidWaOccurrences = 899933; // occurrences of a WfiAnalysis
		internal const int kflidWgOccurrences = 899934; // occurrences of a WfiGloss.
		internal const int kflidTextTitle = 899935; // of a FakeOccurrence
		internal const int kflidTextGenres = 899936; // of a FakeOccurrence
		internal const int kflidTextIsTranslation = 899937; // of a FakeOccurrence
		internal const int kflidTextSource = 899938; // of a FakeOccurrence
		internal const int kflidTextComment = 899939; // of a FakeOccurrence
		public const int kclidFakeOccurrence = 899940;
		internal const int kflidWfExactOccurrences = 899941; // occurrences of a wordform, but nothing it owns.
		internal const int kflidWaExactOccurrences = 899942; // occurrences of a WfiAnalysis, but nothing it owns and nothing that owns it.
		internal const int kflidWgExactOccurrences = 899943; // occurrences of a WfiGloss, but nothing it owns and nothing that owns it.
		// the paragraph containing the occurrence. Usually the owner of the segment and the same
		// as the TextObject, but occurrences in picture captions are an exception.
		public const int kflidParagraph = 899944;

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			m_interestingTexts = InterestingTextsDecorator.GetInterestingTextList(PropertyTable, m_services);
		}

		#endregion

		public override void RemoveNotification(IVwNotifyChange nchng)
		{
			base.RemoveNotification(nchng);
			m_notifieeCount--;
			if (m_notifieeCount > 0 || m_interestingTexts == null)
			{
				return;
			}
			m_interestingTexts.InterestingTextsChanged -= m_interestingTexts_InterestingTextsChanged;
			// Also we need to make sure the InterestingTextsList doesn't do propchanges for us anymore
			// N.B. This avoids LT-12437, but we are assuming that this only gets triggered during Refresh or
			// shutting down the main window, when all the record lists are being disposed.
			// If a record list were to be disposed some other time when another record list was still using the ITL,
			// this would be a bad thing to do.
			base.RemoveNotification(m_interestingTexts);
		}

		/// <summary>
		/// Count the things that are interested in us so that, when there are no more, we can indicate that we no longer care about
		/// changes to the collection of interesting texts.
		/// Review JohnT: could it ever happen that we regain our interest in those changes, getting a new notifiee after we lost our last one?
		/// </summary>
		public override void AddNotification(IVwNotifyChange nchng)
		{
			m_interestingTexts.InterestingTextsChanged += m_interestingTexts_InterestingTextsChanged;
			base.AddNotification(nchng);
			m_notifieeCount++;
		}

		private const int kBaseDummyId = -200001;
		private int m_nextId = kBaseDummyId;

		private int[] GetAnalysisOccurrences(int hvo)
		{
			return GetAnalysisOccurrences(hvo, true);
		}

		public IEnumerable<IStText> InterestingTexts => m_interestingTexts.InterestingTexts;

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			int[] values;
			if (!m_values.TryGetValue(hvo, out values))
			{
				return;
			}
			// We are mainly looking for changes to FullConcordanceCount; but rather than taking the
			// time to look up that flid, just invalidate our cache on any change to the wordform.
			// occurrences is by far the most likely thing to change, and will probably be affected if
			// anything else does change.
			// All we want to do for now is forget that we 'know' the number of occurrences in the text.
			// This will at least allow us to regenerate properly in a new window.
			// At some point we could consider recomputing the values at once and issuing a PropChanged.
			// I'm slightly worried about performance implications if we do that.
			m_values.Remove(hvo);
		}

		/// <summary>
		/// Get the occurrences of a particular analysis in the currently interesting texts.
		/// </summary>
		private int[] GetAnalysisOccurrences(int hvo, bool includeChildren)
		{
			int[] values;
			if (m_values.TryGetValue(hvo, out values))
			{
				return values;
			}
			var analysis = (IAnalysis)m_services.GetObject(hvo);
			var wf = analysis.Wordform;
			var bag = wf.OccurrencesBag;
			var valuesList = new List<int>(bag.Count);
			foreach (var seg in from item in bag.Items where BelongsToInterestingText(item) select item)
			{
				foreach (var occurrence in seg.GetOccurrencesOfAnalysis(analysis, bag.Occurrences(seg), includeChildren))
				{
					var hvoOcc = m_nextId--;
					valuesList.Add(hvoOcc);
					m_occurrences[hvoOcc] = occurrence;
				}
			}
			AddAdditionalOccurrences(hvo, m_occurrences, ref m_nextId, valuesList);
			values = valuesList.ToArray();
			m_values[hvo] = values;
			return values;
		}

		/// <summary>
		/// Overridden in RespellingSda to add caption occurrences.
		/// </summary>
		protected virtual void AddAdditionalOccurrences(int hvoWf, Dictionary<int, IParaFragment> occurrences, ref int nextId, List<int> valuesList)
		{
		}

		/// <summary>
		/// This is invoked by reflection when the Respeller dialog changes the frequency of a wordform.
		/// </summary>
		public void OnItemDataModified(object argument)
		{
			if (!(argument is IAnalysis))
			{
				return;
			}
			UpdateAnalysisOccurrences((IAnalysis)argument, true);
		}

		/// <summary>
		/// Replace the occurrence values for a WfiWordform.  Assume that all of the
		/// values are already registered in m_occurrences through previous calls to
		/// GetAnalysisOccurrences().
		/// </summary>
		/// <remarks>This is used by the Respelling subclass of this decorator.</remarks>
		protected void ReplaceAnalysisOccurrences(int hvo, int[] values)
		{
			m_values[hvo] = values;
		}

		/// <summary>
		/// Make sure we have the right list of occurrences for the specified analysis (assumes we are using ExactOccurrences).
		/// </summary>
		public void UpdateExactAnalysisOccurrences(IAnalysis obj)
		{
			UpdateAnalysisOccurrences(obj, false);
		}

		// Make sure we have the right list of occurrences for the specified analysis.
		// If we've ever been asked for it, send a PropChanged notification to indicate that it has changed.
		public void UpdateAnalysisOccurrences(IAnalysis obj, bool includeChildren)
		{
			var hvo = obj.Hvo;
			int[] values;
			if (!m_values.TryGetValue(hvo, out values))
			{
				return; // never loaded it, don't unless we get asked for it.
			}
			m_values.Remove(hvo); // get rid of dubious value.
			int[] newvalues = GetAnalysisOccurrences(hvo, includeChildren);
			int flidExact, flidAll;
			switch (obj.ClassID)
			{
				case WfiWordformTags.kClassId:
					flidExact = kflidWfExactOccurrences;
					flidAll = kflidWfOccurrences;
					break;
				case WfiAnalysisTags.kClassId:
					flidExact = kflidWaExactOccurrences;
					flidAll = kflidWaOccurrences;
					break;
				case WfiGlossTags.kClassId:
					flidExact = kflidWgExactOccurrences;
					flidAll = kflidWgOccurrences;
					break;
				default:
					return;
			}
			SendPropChanged(hvo, includeChildren ? flidAll : flidExact, 0, newvalues.Length, values.Length);
			SendPropChanged(hvo, kflidOccurrenceCount, 0, 0, 0);
		}

		/// <summary>
		/// This is used by the ConcordanceControl (in LanguageExplorer), which in various ways comes up with a list of
		/// occurrences to display in a concordance browse view. Often the occurrences made in this way do
		/// not have a meaningful Index, but we are not using that here. We make a dummy HVO for the root
		/// as well as the items, if we don't already have one.
		/// </summary>
		public void SetOccurrences(int hvo, IEnumerable<IParaFragment> occurrences)
		{
			var oldCount = m_concValues.Length;
			var values = new int[occurrences.Count()];
			var i = 0;
			foreach (var occurrence in occurrences)
			{
				var hvoOcc = m_nextId--;
				values[i++] = hvoOcc;
				m_occurrences[hvoOcc] = occurrence;
			}
			UpdateOccurrences(values);
			SendPropChanged(hvo, kflidConcOccurrences, 0, values.Length, oldCount);
		}

		/// <summary>
		/// Update the occurrences list (kflidConcOccurrences of LangProject) to the specified array,
		/// without sending PropChanged. (Typically used during ReloadList.)
		/// </summary>
		public void UpdateOccurrences(int[] values)
		{
			m_concValues = values;
		}

		/// <summary>
		/// Get the values we want for the occurrences of the specified LexSE HVO.
		/// </summary>
		private int[] GetSenseOccurrences(int hvo)
		{
			int[] values;
			if (m_values.TryGetValue(hvo, out values))
			{
				return values;
			}
			var sense = m_services.GetInstance<ILexSenseRepository>().GetObject(hvo);
			var bundles = m_services.GetInstance<IWfiMorphBundleRepository>().InstancesWithSense(sense);
			var valuesList = new List<int>();
			foreach (IWfiAnalysis wa in (bundles.Select(bundle => bundle.Owner)).Distinct())
			{
				var bag = ((IWfiWordform)wa.Owner).OccurrencesBag;
				foreach (var seg in from item in bag.Items where BelongsToInterestingText(item) select item)
				{
					foreach (var occurrence in seg.GetOccurrencesOfAnalysis(wa, bag.Occurrences(seg), true))
					{
						var hvoOcc = m_nextId--;
						valuesList.Add(hvoOcc);
						m_occurrences[hvoOcc] = occurrence;
					}
				}
			}
			values = valuesList.ToArray();
			m_values[hvo] = values;
			return values;
		}

		private bool BelongsToInterestingText(ISegment seg)
		{
			if (m_interestingTexts == null)
			{
				return true; // no filtering
			}
			var text = seg.Paragraph?.Owner as IStText;
			return (text != null && m_interestingTexts.IsInterestingText(text));

		}

		/// <summary>
		/// Makes more acessible the means of testing for interesting texts.
		/// </summary>
		public bool IsInterestingText(IStText text)
		{
			return m_interestingTexts == null || m_interestingTexts.IsInterestingText(text);
		}

		public override int[] VecProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidWfExactOccurrences:
				case kflidWaExactOccurrences:
				case kflidWgExactOccurrences:
					return GetAnalysisOccurrences(hvo, false); // Do not include children.
				case kflidWfOccurrences:
				case kflidWaOccurrences:
				case kflidWgOccurrences:
					return GetAnalysisOccurrences(hvo);
				case kflidConcOccurrences:
					return m_concValues;
				case kflidSenseOccurrences:
					return GetSenseOccurrences(hvo);
				case kflidTextGenres:
					var text = GetStText(hvo);
					if (text != null)
					{
						return (from poss in text.GenreCategories select poss.Hvo).ToArray();
					}
					return new int[0];
			}
			return base.VecProp(hvo, tag);
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			switch (tag)
			{
				case kflidWfExactOccurrences:
				case kflidWaExactOccurrences:
				case kflidWgExactOccurrences:
					return GetAnalysisOccurrences(hvo, false)[index];
				case kflidWfOccurrences:
				case kflidWaOccurrences:
				case kflidWgOccurrences:
					return GetAnalysisOccurrences(hvo)[index];
				case kflidConcOccurrences:
					return m_concValues[index];
				case kflidSenseOccurrences:
					return GetSenseOccurrences(hvo)[index];
				case kflidTextGenres:
					var text = GetStText(hvo);
					if (text != null)
					{
						return text.GenreCategories[index].Hvo;
					}
					return 0;
			}
			return base.get_VecItem(hvo, tag, index);
		}

		public override int get_VecSize(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidWfExactOccurrences:
				case kflidWaExactOccurrences:
				case kflidWgExactOccurrences:
					return GetAnalysisOccurrences(hvo, false).Length;
				case kflidWfOccurrences:
				case kflidWaOccurrences:
				case kflidWgOccurrences:
					return GetAnalysisOccurrences(hvo).Length;
				case kflidConcOccurrences:
					return m_concValues.Length;
				case kflidSenseOccurrences:
					return GetSenseOccurrences(hvo).Length;
				case kflidTextGenres:
					var text = GetStText(hvo);
					if (text != null)
					{
						return text.GenreCategories.Count;
					}
					return 0;
			}
			return base.get_VecSize(hvo, tag);
		}

		public override bool get_BooleanProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidTextIsTranslation:
					var text = GetStText(hvo);
					if (text != null)
					{
						return text.IsTranslation;
					}
					return false;
			}
			return base.get_BooleanProp(hvo, tag);
		}

		public override int get_IntProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidOccurrenceCount:
					return GetAnalysisOccurrences(hvo).Length;
				case kflidBeginOffset:
					{
						IParaFragment occurrence;
						if (m_occurrences.TryGetValue(hvo, out occurrence) && occurrence.IsValid)
						{
							return occurrence.GetMyBeginOffsetInPara();
						}
						return 0;
					}
				case kflidEndOffset:
					{
						IParaFragment occurrence;
						if (m_occurrences.TryGetValue(hvo, out occurrence) && occurrence.IsValid)
						{
							return occurrence.GetMyEndOffsetInPara();
						}
						return 0;
					}
				case CmObjectTags.kflidClass:
					if (hvo < 0)
					{
						return kclidFakeOccurrence;
					}
					break;
			}
			return base.get_IntProp(hvo, tag);
		}

		private ITsString EmptyUserString()
		{
			return TsStringUtils.EmptyString(BaseSda.WritingSystemFactory.UserWs);
		}
		public override ITsString get_StringProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidReference:
					return m_occurrences.ContainsKey(hvo) ? m_occurrences[hvo].Reference ?? EmptyUserString() : EmptyUserString();
			}
			return base.get_StringProp(hvo, tag);
		}

		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			switch (tag)
			{
				case kflidTextTitle:
					{
						var text = GetStText(hvo);
						return text != null ? text.Title.get_String(ws) : TsStringUtils.EmptyString(ws);
					}
				case kflidTextSource:
					{
						var text = GetStText(hvo);
						return text != null ? text.Source.get_String(ws) : TsStringUtils.EmptyString(ws);
					}
				case kflidTextComment:
					{
						var text = GetStText(hvo);
						return text != null ? text.Comment.get_String(ws) : TsStringUtils.EmptyString(ws);
					}
			}
			return base.get_MultiStringAlt(hvo, tag, ws);
		}

		IStText GetStText(int hvoOccurrence)
		{
			return m_occurrences[hvoOccurrence].Paragraph.Owner as IStText;
		}

		public override int get_ObjectProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidTextObject:
					{
						IParaFragment occurrence;
						if (m_occurrences.TryGetValue(hvo, out occurrence) && occurrence.IsValid)
						{
							return occurrence.TextObject.Hvo;
						}
						return 0;
					}
				case kflidSegment:
					{
						IParaFragment occurrence;
						if (m_occurrences.TryGetValue(hvo, out occurrence) && occurrence.IsValid && occurrence.Segment != null)
						{
							return occurrence.Segment.Hvo;
						}
						return 0;
					}
				case kflidAnalysis:
					{
						IParaFragment occurrence;
						if (m_occurrences.TryGetValue(hvo, out occurrence) && occurrence.IsValid && occurrence.Analysis != null)
						{
							return occurrence.Analysis.Hvo;
						}
						return 0;
					}
				case kflidParagraph:
					{
						IParaFragment occurrence;
						if (m_occurrences.TryGetValue(hvo, out occurrence) && occurrence.IsValid)
						{
							return occurrence.Paragraph.Hvo;
						}
						return 0;
					}
			}
			return base.get_ObjectProp(hvo, tag);
		}

		/// <summary>
		/// Makes the actual analysis occurrence available (e.g., for configuring the appropriate interlinear view).
		/// </summary>
		public IParaFragment OccurrenceFromHvo(int hvo)
		{
			Debug.Assert(m_occurrences.ContainsKey(hvo), "Attempting to retrieve an item from m_occurrences which isn't there.");
			return m_occurrences.ContainsKey(hvo) ? m_occurrences[hvo] : (IParaFragment)null;
		}

		void m_interestingTexts_InterestingTextsChanged(object sender, InterestingTextsChangedArgs e)
		{
			m_values.Clear(); // Forget all we know about occurrences, since the texts they are based on have changed.
			const int flid = ObjectListPublisher.OwningFlid;
			var langProj = m_services.GetInstance<ILangProjectRepository>().AllInstances().First();
			var oldSize = m_services.GetInstance<IWfiWordformRepository>().AllInstances().Count();
			// Force everything to be redisplayed by pretending all the wordforms were replaced.
			SendPropChanged(langProj.Hvo, flid, 0, oldSize, oldSize);
		}

		/// <summary>
		/// Refresh your cached properties.
		/// </summary>
		public override void Refresh()
		{
			if (!m_fRefreshSuspended)
			{
				m_values.Clear();
				m_occurrences.Clear();
			}
			base.Refresh();
		}

		public override void SuspendRefresh()
		{
			m_fRefreshSuspended = true;
			base.SuspendRefresh();
		}

		public override void ResumeRefresh()
		{
			m_fRefreshSuspended = false;
			base.ResumeRefresh();
		}
	}
}