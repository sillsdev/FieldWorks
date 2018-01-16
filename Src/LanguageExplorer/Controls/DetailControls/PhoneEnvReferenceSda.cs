// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class PhoneEnvReferenceSda : DomainDataByFlidDecoratorBase
	{
		Dictionary<int, List<int>> m_MainObjEnvs = new Dictionary<int, List<int>>();
		Dictionary<int, ITsString> m_EnvStringReps = new Dictionary<int, ITsString>();
		Dictionary<int, string> m_ErrorMsgs = new Dictionary<int, string>();

		int m_NextDummyId = -500000;

		public PhoneEnvReferenceSda(ISilDataAccessManaged domainDataByFlid)
			: base(domainDataByFlid)
		{
			SetOverrideMdc(new PhoneEnvReferenceDataCacheDecorator(MetaDataCache as IFwMetaDataCacheManaged));
		}

		public override int get_VecSize(int hvo, int tag)
		{
			if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
			{
				List<int> objs;
				return m_MainObjEnvs.TryGetValue(hvo, out objs) ? objs.Count : 0;
			}
			return base.get_VecSize(hvo, tag);
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
			{
				List<int> objs;
				return m_MainObjEnvs.TryGetValue(hvo, out objs) ? objs[index] : 0;
			}
			return base.get_VecItem(hvo, tag, index);
		}

		public override void DeleteObj(int hvoObj)
		{
			if (hvoObj < 0)
			{
				if (m_MainObjEnvs.ContainsKey(hvoObj))
				{
					m_MainObjEnvs.Remove(hvoObj);
				}

				if (m_EnvStringReps.ContainsKey(hvoObj))
				{
					m_EnvStringReps.Remove(hvoObj);
				}

				if (m_ErrorMsgs.ContainsKey(hvoObj))
				{
					m_ErrorMsgs.Remove(hvoObj);
				}
				foreach (var x in m_MainObjEnvs)
				{
					if (x.Value.Contains(hvoObj))
					{
						x.Value.Remove(hvoObj);
					}
				}
			}
			else
			{
				base.DeleteObj(hvoObj);
			}
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			if (tag == PhoneEnvReferenceView.kEnvStringRep)
			{
				ITsString tss;
				return m_EnvStringReps.TryGetValue(hvo, out tss) ? tss : null;
			}
			return base.get_StringProp(hvo, tag);
		}

		public override void SetString(int hvo, int tag, ITsString _tss)
		{
			if (tag == PhoneEnvReferenceView.kEnvStringRep)
			{
				m_EnvStringReps[hvo] = _tss;
			}
			else
			{
				base.SetString(hvo, tag, _tss);
			}
		}

		public override string get_UnicodeProp(int hvo, int tag)
		{
			if (tag == PhoneEnvReferenceView.kErrorMessage)
			{
				string sMsg;
				return m_ErrorMsgs.TryGetValue(hvo, out sMsg) ? sMsg : null;
			}
			return base.get_UnicodeProp(hvo, tag);
		}

		public override void SetUnicode(int hvo, int tag, string _rgch, int cch)
		{
			if (tag == PhoneEnvReferenceView.kErrorMessage)
			{
				m_ErrorMsgs[hvo] = _rgch;
			}
			else
			{
				base.SetUnicode(hvo, tag, _rgch, cch);
			}
		}

		public override int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
			{
				var hvo = --m_NextDummyId;
				List<int> objs;
				if (!m_MainObjEnvs.TryGetValue(hvoOwner, out objs))
				{
					objs = new List<int>();
					m_MainObjEnvs.Add(hvoOwner, objs);
				}
				objs.Insert(ord, hvo);
				return hvo;
			}
			return base.MakeNewObject(clid, hvoOwner, tag, ord);
		}

		#region extra methods borrowed from IVwCacheDa

		public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			if (tag != PhoneEnvReferenceView.kMainObjEnvironments)
			{
				return;
			}
			List<int> objs;
			if (m_MainObjEnvs.TryGetValue(hvoObj, out objs))
			{
				var cDel = ihvoLim - ihvoMin;
				if (cDel > 0)
				{
					objs.RemoveRange(ihvoMin, cDel);
				}
				objs.InsertRange(ihvoMin, _rghvo);
			}
			else
			{
				objs = new List<int>();
				objs.AddRange(_rghvo);
				m_MainObjEnvs.Add(hvoObj, objs);
			}
		}

		public void CacheVecProp(int hvoObj, int tag, int[] rghvo, int chvo)
		{
			if (tag != PhoneEnvReferenceView.kMainObjEnvironments)
			{
				return;
			}
			List<int> objs;
			if (m_MainObjEnvs.TryGetValue(hvoObj, out objs))
			{
				objs.Clear();
			}
			else
			{
				objs = new List<int>();
				m_MainObjEnvs.Add(hvoObj, objs);
			}
			objs.AddRange(rghvo);
		}

		#endregion
	}
}