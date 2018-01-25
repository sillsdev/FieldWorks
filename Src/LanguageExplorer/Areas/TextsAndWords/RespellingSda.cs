// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// Entend the ConcDecorator with a few more properties needed for respelling.
	/// </summary>
	internal class RespellingSda : ConcDecorator
	{
		internal class RespellInfo
		{
			public int AdjustedBeginOffset;
			public int AdjustedEndOffset;
			public ITsString SpellingPreview;
		}

		public const int kflidAdjustedBeginOffset = 9909101;	// on occurrence, int
		public const int kflidAdjustedEndOffset = 9909102;		// on occurrence, int
		public const int kflidSpellingPreview = 9909103;		// on occurrence, string
		public const int kflidOccurrencesInCaptions = 9909104;	// on WfiWordform, reference seq

		Dictionary<int, RespellInfo> m_mapRespell = new Dictionary<int, RespellInfo>();

		public RespellingSda(LcmCache cache, ILcmServiceLocator services)
			: base(services)
		{
			Cache = cache;
			SetOverrideMdc(new RespellingMdc(MetaDataCache as IFwMetaDataCacheManaged));
		}

		public override int get_IntProp(int hvo, int tag)
		{
			RespellInfo info;
			switch (tag)
			{
				case kflidAdjustedBeginOffset:
					return m_mapRespell.TryGetValue(hvo, out info) ? info.AdjustedBeginOffset : 0;
				case kflidAdjustedEndOffset:
					return m_mapRespell.TryGetValue(hvo, out info) ? info.AdjustedEndOffset : 0;
			}
			return base.get_IntProp(hvo, tag);
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidSpellingPreview:
					RespellInfo info;
					return m_mapRespell.TryGetValue(hvo, out info) ? info.SpellingPreview : null;
			}
			return base.get_StringProp(hvo, tag);
		}

		public override int[] VecProp(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidOccurrencesInCaptions:
					return new int[0];

			}
			return base.VecProp(hvo, tag);
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			switch (tag)
			{
				case kflidOccurrencesInCaptions:
					return 0;
			}
			return base.get_VecItem(hvo, tag, index);
		}

		public override int get_VecSize(int hvo, int tag)
		{
			switch (tag)
			{
				case kflidOccurrencesInCaptions:
					return 0;
			}
			return base.get_VecSize(hvo, tag);
		}

		public override void SetInt(int hvo, int tag, int n)
		{
			RespellInfo info;
			switch (tag)
			{
				case kflidAdjustedBeginOffset:
					if (m_mapRespell.TryGetValue(hvo, out info))
					{
						info.AdjustedBeginOffset = n;
					}
					else
					{
						info = new RespellInfo
						{
							AdjustedBeginOffset = n
						};
						m_mapRespell.Add(hvo, info);
					}
					break;
				case kflidAdjustedEndOffset:
					if (m_mapRespell.TryGetValue(hvo, out info))
					{
						info.AdjustedEndOffset = n;
					}
					else
					{
						info = new RespellInfo
						{
							AdjustedEndOffset = n
						};
						m_mapRespell.Add(hvo, info);
					}
					break;
				case kflidBeginOffset:
					OccurrenceFromHvo(hvo).SetMyBeginOffsetInPara(n);
					break;
				case kflidEndOffset:
					OccurrenceFromHvo(hvo).SetMyEndOffsetInPara(n);
					break;
				default:
					base.SetInt(hvo, tag, n);
					break;
			}
		}

		public override void SetString(int hvo, int tag, ITsString _tss)
		{
			switch (tag)
			{
				case kflidSpellingPreview:
					RespellInfo info;
					if (m_mapRespell.TryGetValue(hvo, out info))
					{
						info.SpellingPreview = _tss;
					}
					else
					{
						info = new RespellInfo
						{
							SpellingPreview = _tss
						};
						m_mapRespell.Add(hvo, info);
					}
					break;
				default:
					base.SetString(hvo, tag, _tss);
					break;
			}
		}

		/// <summary>
		/// Allow the Occurrences virtual vector property to be updated.  Only hvos obtained
		/// from the Occurrences property earlier are valid for the values array.
		/// </summary>
		internal void ReplaceOccurrences(int hvo, int[] values)
		{
			ReplaceAnalysisOccurrences(hvo, values);
		}

		private LcmCache Cache { get; }

		/// <summary>
		/// Make additional fake occurrences for where the wordform occurs in captions.
		/// </summary>
		protected override void AddAdditionalOccurrences(int hvoWf, Dictionary<int, IParaFragment> occurrences, ref int nextId, List<int> valuesList)
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvoWf);
			var wordform = wf.Form.VernacularDefaultWritingSystem.Text;
			if (string.IsNullOrEmpty(wordform))
			{
				return; // paranoia.
			}
			var desiredType = new HashSet<FwObjDataTypes> { FwObjDataTypes.kodtGuidMoveableObjDisp };
			var cmObjRepos = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			ProgressState state = null;
			if (PropertyTable != null)
			{
				state = PropertyTable.GetValue<ProgressState>("SpellingPrepState");
			}
			var done = 0;
			var total = InterestingTexts.Count();
			foreach (var text in InterestingTexts)
			{
				done++;
				foreach (var stPara in text.ParagraphsOS)
				{
					var para = (IStTxtPara) stPara;
					if (!(para is IScrTxtPara))
					{
						continue; // currently only these have embedded pictures.
					}
					var contents = para.Contents;
					var crun = contents.RunCount;
					for (var irun = 0; irun < crun; irun++)
					{
						// See if the run is a picture ORC
						TsRunInfo tri;
						FwObjDataTypes odt;
						ITsTextProps props;
						var guid = TsStringUtils.GetGuidFromRun(contents, irun, out odt, out tri, out props, desiredType.ToArray());
						if (guid == Guid.Empty)
						{
							continue;
						}
						// See if its caption contains our wordform
						var obj = cmObjRepos.GetObject(guid);
						var clsid = obj.ClassID;
						if (clsid != CmPictureTags.kClassId)
						{
							continue; // bizarre, just for defensiveness.
						}
						var picture = (ICmPicture) obj;
						var caption = picture.Caption.get_String(Cache.DefaultVernWs);
						var wordMaker = new WordMaker(caption, Cache.ServiceLocator.WritingSystemManager);
						for (; ; )
						{
							int ichMin;
							int ichLim;
							var tssTxtWord = wordMaker.NextWord(out ichMin, out ichLim);
							if (tssTxtWord == null)
							{
								break;
							}

							if (tssTxtWord.Text != wordform)
							{
								continue;
							}
							// Make a fake occurrence.
							var hvoFake = nextId--;
							valuesList.Add(hvoFake);
							var occurrence = new CaptionParaFragment();
							occurrence.SetMyBeginOffsetInPara(ichMin);
							occurrence.SetMyEndOffsetInPara(ichLim);
							occurrence.ContainingParaOffset = tri.ichMin;
							occurrence.Paragraph = para;
							occurrence.Picture = picture;
							occurrences[hvoFake] = occurrence;
						}
					}
				}
				if (state != null)
				{
					state.PercentDone = 50 + 50*done/total;
					state.Breath();
				}
			}
		}
	}
}