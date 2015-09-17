using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;

namespace LanguageExplorerTests.Discourse
{
	/// <summary>
	/// This class is a 'Test Spy' which implements IVwNotifyChange to record calls for later verification.
	/// </summary>
	public sealed class NotifyChangeSpy : IVwNotifyChange, IDisposable
	{
		private ISilDataAccess m_sda;
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

		List<NotifyChangeInfo> m_calls = new List<NotifyChangeInfo>();

		//public void AssertHasNotification(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		//{
		//    Assert.IsTrue(m_calls.Contains(new NotifyChangeInfo(hvo, tag, ivMin, cvIns, cvDel)));
		//}

		//public void AssertHasExactlyOneNotification(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		//{
		//    Assert.AreEqual(1, m_calls.Count);
		//    AssertHasNotification(hvo, tag, ivMin, cvIns, cvDel);
		//}

		#region IDisposable Members

		public void Dispose()
		{
			m_sda.RemoveNotification(this);
			GC.SuppressFinalize(this);
		}
		#endregion

		//public void AssertHasNoNotification(int p)
		//{
		//    throw new Exception("The method or operation is not implemented.");
		//}

		//internal void AssertHasNotification(int hvo1, int tag1)
		//{
		//    foreach (NotifyChangeInfo info in m_calls)
		//    {
		//        if (info.hvo == hvo1 && info.tag == tag1)
		//            return;
		//    }
		//    Assert.Fail("no notification found for " + hvo1 + " and prop " + tag1);
		//}
	}

	public struct NotifyChangeInfo
	{
		public NotifyChangeInfo(int hvo1, int tag1, int ivMin1, int cvIns1, int cvDel1)
		{
			this.hvo = hvo1;
			this.tag = tag1;
			this.ivMin = ivMin1;
			this.cvIns = cvIns1;
			this.cvDel = cvDel1;
		}
		internal int hvo;
		internal int tag;
		int ivMin;
		int cvIns;
		int cvDel;

		public override bool Equals(object obj)
		{
			if (!(obj is NotifyChangeInfo))
				return false;
			NotifyChangeInfo other = (NotifyChangeInfo)obj;
			return hvo == other.hvo && tag == other.tag && ivMin == other.ivMin
				&& cvIns == other.cvIns && cvDel == other.cvDel;
		}

		public override int GetHashCode()
		{
			return hvo + tag + ivMin + cvIns + cvDel;
		}
	}
}
