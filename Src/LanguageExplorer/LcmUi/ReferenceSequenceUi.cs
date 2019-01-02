// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Currently only LexReferenceSequenceView displays a full sequence for lexical relations sequence.
	/// Otherwise we could also manufacture ReferenceSequenceUi from ReferenceBaseUi.MakeLcmModelUiObject().
	/// But since only LexReferenceSequenceView (e.g. Calendar) is handling changing the sequence of items
	/// through the context menu, we'll wait till we really need it to come up with a solution that can
	/// "exclude self" from the list in moving calculations.
	/// </summary>
	public class ReferenceSequenceUi : VectorReferenceUi
	{
		private readonly LcmRefSeq _lcmRefSeq;

		public ReferenceSequenceUi(LcmCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
			: base(cache, rootObj, referenceFlid, targetHvo)
		{
			Debug.Assert(m_iType == CellarPropertyType.ReferenceSequence);
			_lcmRefSeq = new LcmRefSeq(m_cache, m_hvo, m_flid);
			m_iCurrent = ComputeTargetVectorIndex();
		}

		protected int ComputeTargetVectorIndex()
		{
			Debug.Assert(_lcmRefSeq != null);
			Debug.Assert(m_hvoTarget > 0);
			if (_lcmRefSeq == null || m_hvoTarget <= 0)
			{
				return -1;
			}
			var hvos = _lcmRefSeq.ToHvoArray();
			for (var i = 0; i < hvos.Length; i++)
			{
				if (hvos[i] == m_hvoTarget)
				{
					return i;
				}
			}
			return -1;
		}

#if RANDYTODO
		public override bool OnDisplayMoveTargetUpInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			if (m_iCurrent < 0 || _lcmRefSeq == null || _lcmRefSeq.Count == 0)
			{
				display.Visible = display.Enabled = false;
				return true;
			}

			if ((m_iCurrent + 1) == _lcmRefSeq.Count)
			{
				display.Visible = true;
				display.Enabled = false;
			}
			else
			{
				display.Visible = display.Enabled = true;
			}
			return true;
		}

		public override bool OnDisplayMoveTargetDownInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			if (m_iCurrent < 0 || _lcmRefSeq == null || _lcmRefSeq.Count == 0)
			{
				display.Visible = display.Enabled = false;
				return true;
			}

			if (m_iCurrent == 0)
			{
				display.Visible = true;
				display.Enabled = false;
			}
			else
			{
				display.Visible = display.Enabled = true;
			}
			return true;
		}
#endif

		public void OnMoveTargetUpInSequence(object commandObject)
		{
			if (m_cmObject == null || m_iCurrent < 0)
			{
				return;
			}
			// Move currently selected object to the next location
			var iNew = m_iCurrent + 1;
			Debug.Assert(iNew < _lcmRefSeq.Count);
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Undo move down/right/later in sequence", "Redo move down/right/later in sequence", m_cache.ActionHandlerAccessor, () =>
			{
				_lcmRefSeq.RemoveAt(m_iCurrent);
				_lcmRefSeq.Insert(iNew, m_hvoTarget);
			});
		}

		public void OnMoveTargetDownInSequence(object commandObject)
		{
			if (m_cmObject == null || m_iCurrent < 0)
			{
				return;
			}
			// Move currently selected object to the previous location
			var iNew = m_iCurrent - 1;
			Debug.Assert(iNew >= 0);
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Undo move up/left/earlier in sequence", "Redo move up/left/earlier in sequence", m_cache.ActionHandlerAccessor, () =>
			{
				_lcmRefSeq.RemoveAt(m_iCurrent);
				_lcmRefSeq.Insert(iNew, m_hvoTarget);
			});
		}

		/// <summary>
		/// Our own minimal implementation of a reference sequence, since we can't just get what
		/// we want from LCM's internal secret implementation of ILcmReferenceSequence.
		/// </summary>
		private sealed class LcmRefSeq
		{
			private readonly LcmCache m_cache;
			private readonly int m_hvo;
			private readonly int m_flid;

			internal LcmRefSeq(LcmCache cache, int hvo, int flid)
			{
				m_cache = cache;
				m_hvo = hvo;
				m_flid = flid;
			}

			internal int Count => m_cache.DomainDataByFlid.get_VecSize(m_hvo, m_flid);

			internal int[] ToHvoArray()
			{
				var chvo = Count;
				using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
				{
					m_cache.DomainDataByFlid.VecProp(m_hvo, m_flid, chvo, out chvo, arrayPtr);
					return MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				}
			}

			internal void RemoveAt(int ihvo)
			{
				m_cache.DomainDataByFlid.Replace(m_hvo, m_flid, ihvo, ihvo + 1, null, 0);
			}

			internal void Insert(int ihvo, int hvo)
			{
				m_cache.DomainDataByFlid.Replace(m_hvo, m_flid, ihvo, ihvo, new[] { hvo }, 1);
			}
		}
	}
}