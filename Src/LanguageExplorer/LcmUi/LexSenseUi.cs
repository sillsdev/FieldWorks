// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// UI functions for MoMorphSynAnalysis.
	/// </summary>
	internal sealed class LexSenseUi : CmObjectUi
	{
		/// <summary />
		internal LexSenseUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is ILexSense);
		}

		internal LexSenseUi() { }

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
		{
			wp.m_title = LcmUiResources.ksMergeSense;
			wp.m_label = LcmUiResources.ksSenses;
			var sense = (ILexSense)MyCmObject;
			var le = sense.Entry;
			// Exclude subsenses of the chosen sense.  See LT-6107.
			var rghvoExclude = new List<int>();
			foreach (var ls in sense.AllSenses)
			{
				rghvoExclude.Add(ls.Hvo);
			}
			foreach (var senseInner in le.AllSenses)
			{
				if (senseInner == MyCmObject || rghvoExclude.Contains(senseInner.Hvo))
				{
					continue;
				}
				// Make sure we get the actual WS used (best analysis would be the
				// descriptive term) for the ShortName.  See FWR-2812.
				var tssName = senseInner.ShortNameTSS;
				mergeCandidates.Add(new DummyCmObject(senseInner.Hvo, tssName.Text, TsStringUtils.GetWsAtOffset(tssName, 0)));
			}
			guiControlParameters = XElement.Parse(LcmUiResources.MergeSenseListParameters);
			helpTopic = "khtpMergeSense";
			var tss = MyCmObject.ShortNameTSS;
			return new DummyCmObject(m_hvo, tss.Text, TsStringUtils.GetWsAtOffset(tss, 0));
		}

		internal override void MoveUnderlyingObjectToCopyOfOwner()
		{
			var obj = MyCmObject.Owner;
			var clid = obj.ClassID;
			while (clid != LexEntryTags.kClassId)
			{
				obj = obj.Owner;
				clid = obj.ClassID;
			}
			var le = (ILexEntry)obj;
			le.MoveSenseToCopy((ILexSense)MyCmObject);
		}

		/// <summary>
		/// When inserting a LexSense, copy the MSA from the one we are inserting after, or the
		/// first one.  If this is the first one, we may need to create an MSA if the owning entry
		/// does not have an appropriate one.
		/// </summary>
		internal static LexSenseUi MakeLcmModelUiObject(LcmCache cache, int hvoOwner, int insertionPosition = int.MaxValue)
		{
			Guard.AgainstNull(cache, nameof(cache));

			var owner = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoOwner);
			switch (owner)
			{
				case ILexEntry entry:
					return new LexSenseUi(entry.CreateNewLexSense(insertionPosition));
				case ILexSense sense:
					return new LexSenseUi(sense.CreateNewLexSense(insertionPosition));
				default:
					throw new ArgumentOutOfRangeException(nameof(hvoOwner), $"Owner must be an ILexEntry or an ILexSense, but it was: '{owner.ClassName}'.");
			}
		}
	}
}