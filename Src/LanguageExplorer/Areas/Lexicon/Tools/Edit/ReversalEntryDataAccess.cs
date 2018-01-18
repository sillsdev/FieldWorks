// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Decorated ISilDataAccess for accessing temporary, interactively edit reversal index
	/// entry (forms).
	/// </summary>
	internal class ReversalEntryDataAccess : DomainDataByFlidDecoratorBase, IVwCacheDa
	{
		public const int kflidWsAbbr = 89999123;

		private struct HvoWs
		{
			private int hvo;
			private int ws;
			internal HvoWs(int hvoIn, int wsIn)
			{
				this.hvo = hvoIn;
				this.ws = wsIn;
			}
		}
		Dictionary<HvoWs, ITsString> m_mapHvoWsRevForm = new Dictionary<HvoWs, ITsString>();
		Dictionary<int, int[]> m_mapIndexHvoEntryHvos = new Dictionary<int, int[]>();
		Dictionary<int, int[]> m_mapSenseHvoIndexHvos = new Dictionary<int, int[]>();
		Dictionary<int, ITsString> m_mapWsAbbr = new Dictionary<int, ITsString>();
		Dictionary<int, string> m_mapHvoIdxWs = new Dictionary<int, string>();

		/// <summary />
		public ReversalEntryDataAccess(ISilDataAccessManaged sda)
			: base(sda)
		{
		}

		#region ISilDataAccess overrides

		/// <summary />
		public override int get_VecSize(int hvo, int tag)
		{
			switch (tag)
			{
				case ReversalIndexEntrySliceView.kFlidEntries:
				{
					int[] rghvo;
					if (m_mapIndexHvoEntryHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo.Length;
					}
					throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidEntries)");
				}
				case ReversalIndexEntrySliceView.kFlidIndices:
				{
					int[] rghvo;
					if (m_mapSenseHvoIndexHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo.Length;
					}
					throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidIndices)");
				}
				default:
					return base.get_VecSize(hvo, tag);
			}
		}

		/// <summary />
		public override int get_VecItem(int hvo, int tag, int index)
		{
			switch (tag)
			{
				case ReversalIndexEntrySliceView.kFlidEntries:
				{
					int[] rghvo;
					if (m_mapIndexHvoEntryHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo[index];
					}
					throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidEntries)");
				}
				case ReversalIndexEntrySliceView.kFlidIndices:
				{
					int[] rghvo;
					if (m_mapSenseHvoIndexHvos.TryGetValue(hvo, out rghvo))
					{
						return rghvo[index];
					}
					throw new ArgumentException("data not stored for get_VecSize(ReversalIndexEntrySliceView.kFlidIndices)");
				}
				default:
					return base.get_VecItem(hvo, tag, index);
			}
		}

		/// <summary />
		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			if (tag == ReversalIndexEntryTags.kflidReversalForm)
			{
				var key = new HvoWs(hvo, ws);
				ITsString tss;
				return m_mapHvoWsRevForm.TryGetValue(key, out tss) ? tss : TsStrFactory.EmptyString(ws);
			}
			return base.get_MultiStringAlt(hvo, tag, ws);
		}

		/// <summary />
		public override void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			if (tag == ReversalIndexEntryTags.kflidReversalForm)
			{
				var key = new HvoWs(hvo, ws);
				m_mapHvoWsRevForm[key] = _tss;
				// anything negative is just a dummy hvo. Make the base class ignore it for now
				if (hvo < 0)
				{
					return;
				}
			}
			base.SetMultiStringAlt(hvo, tag, ws, _tss);
		}

		/// <summary />
		public override ITsString get_StringProp(int hvo, int tag)
		{
			if (tag != kflidWsAbbr)
			{
				return base.get_StringProp(hvo, tag);
			}
			ITsString tss;
			return m_mapWsAbbr.TryGetValue(hvo, out tss) ? tss : null;
		}

		/// <summary />
		public override string get_UnicodeProp(int hvo, int tag)
		{
			if (tag == ReversalIndexTags.kflidWritingSystem)
			{
				string val;
				return m_mapHvoIdxWs.TryGetValue(hvo, out val) ? val : null;
			}
			return base.get_UnicodeProp(hvo, tag);
		}
		/// <summary />
		public override void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == ReversalIndexEntrySliceView.kFlidEntries)
			{
				// What can we do here??
				base.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
			}
			else
			{
				base.PropChanged(_nchng, _ct, hvo, tag, ivMin, cvIns, cvDel);
			}
		}
		#endregion

		#region IVwCacheDa Members

		/// <summary />
		public void CacheBinaryProp(int obj, int tag, byte[] _rgb, int cb)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void CacheBooleanProp(int obj, int tag, bool val)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void CacheGuidProp(int obj, int tag, Guid uid)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void CacheInt64Prop(int obj, int tag, long val)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void CacheIntProp(int obj, int tag, int val)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public ITsStrFactory TsStrFactory { get; set; }

		/// <summary />
		public void CacheObjProp(int obj, int tag, int val)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			if (tag == ReversalIndexEntrySliceView.kFlidEntries)
			{
				int[] rghvoOld;
				if (m_mapIndexHvoEntryHvos.TryGetValue(hvoObj, out rghvoOld))
				{
					var rghvoNew = new List<int>();
					rghvoNew.AddRange(rghvoOld);
					if (ihvoMin != ihvoLim)
					{
						rghvoNew.RemoveRange(ihvoMin, ihvoLim - ihvoMin);
					}

					if (chvo != 0)
					{
						rghvoNew.InsertRange(ihvoMin, _rghvo);
					}
					m_mapIndexHvoEntryHvos[hvoObj] = rghvoNew.ToArray();
				}
				else
				{
					throw new ArgumentException("data not stored for CacheReplace(ReversalIndexEntrySliceView.kFlidEntries)");
				}
			}
			else
			{
				throw new ArgumentException("we can only handle ReversalIndexEntrySliceView.kFlidEntries here!");
			}
		}

		/// <summary />
		public void CacheStringAlt(int obj, int tag, int ws, ITsString _tss)
		{
			if (tag == ReversalIndexEntryTags.kflidReversalForm)
			{
				var key = new HvoWs(obj, ws);
				m_mapHvoWsRevForm[key] = _tss;
			}
			else
			{
				throw new ArgumentException("we can only handle ReversalIndexEntryTags.kflidReversalForm here!");
			}
		}

		/// <summary />
		public void CacheStringProp(int obj, int tag, ITsString _tss)
		{
			if (tag == kflidWsAbbr)
			{
				m_mapWsAbbr[obj] = _tss;
			}
			else
			{
				throw new ArgumentException("we can only handle LgWritingSystemTags.kflidAbbr here!");
			}
		}

		/// <summary />
		public void CacheTimeProp(int hvo, int tag, long val)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void CacheUnicodeProp(int obj, int tag, string _rgch, int cch)
		{
			if (tag == ReversalIndexTags.kflidWritingSystem)
			{
				m_mapHvoIdxWs[obj] = _rgch;
			}
			else
			{
				throw new ArgumentException("we can only handle ReversalIndexTags.kflidWritingSystem here!");
			}
		}

		/// <summary />
		public void CacheUnknown(int obj, int tag, object _unk)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void CacheVecProp(int obj, int tag, int[] rghvo, int chvo)
		{
			switch (tag)
			{
				case ReversalIndexEntrySliceView.kFlidEntries:
					m_mapIndexHvoEntryHvos[obj] = rghvo;
					break;
				case ReversalIndexEntrySliceView.kFlidIndices:
					m_mapSenseHvoIndexHvos[obj] = rghvo;
					break;
				default:
					throw new ArgumentException("we can only handle ReversalIndexEntrySliceView fake flids here!");
			}
		}

		/// <summary />
		public void ClearAllData()
		{
			m_mapHvoWsRevForm.Clear();
			m_mapIndexHvoEntryHvos.Clear();
			m_mapSenseHvoIndexHvos.Clear();
			m_mapWsAbbr.Clear();
			m_mapHvoIdxWs.Clear();
		}

		/// <summary />
		public void ClearInfoAbout(int hvo, VwClearInfoAction cia)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void ClearInfoAboutAll(int[] _rghvo, int chvo, VwClearInfoAction cia)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void ClearVirtualProperties()
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public IVwVirtualHandler GetVirtualHandlerId(int tag)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public IVwVirtualHandler GetVirtualHandlerName(string bstrClass, string bstrField)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void InstallVirtual(IVwVirtualHandler _vh)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public int get_CachedIntProp(int obj, int tag, out bool _f)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}