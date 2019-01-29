// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorerTests.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// This class is a 'Test Spy' which implements IVwNotifyChange to record calls for later verification.
	/// </summary>
	public sealed class NotifyChangeSpy : IVwNotifyChange, IDisposable
	{
		private ISilDataAccess m_sda;
		private List<NotifyChangeInfo> m_calls = new List<NotifyChangeInfo>();

		public NotifyChangeSpy(ISilDataAccess sda)
		{
			m_sda = sda;
			//m_sda.AddNotification(this);
		}

		#region IVwNotifyChange Members

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			m_calls.Add(new NotifyChangeInfo(hvo, tag, ivMin, cvIns, cvDel));
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~NotifyChangeSpy()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			if (disposing)
			{
				m_sda.RemoveNotification(this);
			}
		}
		#endregion

		private struct NotifyChangeInfo
		{
			internal NotifyChangeInfo(int hvo1, int tag1, int ivMin1, int cvIns1, int cvDel1)
			{
				hvo = hvo1;
				tag = tag1;
				ivMin = ivMin1;
				cvIns = cvIns1;
				cvDel = cvDel1;
			}

			private readonly int hvo;
			private readonly int tag;
			private readonly int ivMin;
			private readonly int cvIns;
			private readonly int cvDel;

			public override bool Equals(object obj)
			{
				if (!(obj is NotifyChangeInfo))
				{
					return false;
				}
				var other = (NotifyChangeInfo)obj;
				return hvo == other.hvo && tag == other.tag && ivMin == other.ivMin && cvIns == other.cvIns && cvDel == other.cvDel;
			}

			public override int GetHashCode()
			{
				return hvo + tag + ivMin + cvIns + cvDel;
			}
		}
	}
}
