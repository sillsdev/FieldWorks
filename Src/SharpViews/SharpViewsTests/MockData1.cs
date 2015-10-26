// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Utilities;
using SIL.FieldWorks.SharpViews.Builders;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// A class having a variety of properties we would like to be able to view.
	/// </summary>
	public class MockData1 : ICmObject
	{
		public MockData1()
		{
			var temp = new ModifiedMonitoredList<MockData1>();
			ObjSeq2 = temp;
			temp.Changed += (RaiseObjSeq2Changed);
		}

		public MockData1(int vws, int aws) : this()
		{
			VernWs = vws;
			AnalysisWs = aws;
		}

		void RaiseObjSeq2Changed(object sender, EventArgs e)
		{
			if (ObjSeq2Changed != null)
				ObjSeq2Changed(this, e);
		}
		private IViewMultiString m_simpleOneAccessor;
		private ITsString m_simpleTwo;
		private string m_simpleThree = "";
		private MockData1 m_simpleFour;
		public int VernWs;
		public int AnalysisWs;

		private List<MockData1> m_ObjSeq = new List<MockData1>();

		//[Virtual(CellarPropertyType.MultiUnicode, "MultiUnicode")]
		public IViewMultiString MlSimpleOne
		{
			get
			{
				if (m_simpleOneAccessor == null)
				{
					m_simpleOneAccessor = new MultiAccessor(VernWs, AnalysisWs);
				}
				return m_simpleOneAccessor;
			}
			set
			{
				m_simpleOneAccessor = value;
				RaiseMlSimpleOneChanged(((MultiAccessor)value).VernWs);
			}
		}

		public event EventHandler<MlsChangedEventArgs> MlSimpleOneChanged;
		public event EventHandler<EventArgs> SimpleTwoChanged;
		public event EventHandler<EventArgs> SimpleThreeChanged;
		public event EventHandler<EventArgs> SimpleFourChanged;

		//[Virtual(CellarPropertyType.String, "String")]
		public ITsString SimpleTwo
		{
			get { return m_simpleTwo; }
			set
			{
				m_simpleTwo = value;
				RaiseSimpleTwoChanged();
			}
		}

		public const int ktagSimpleOne = 79;

		public const int ktagSimpleTwo = 81;

		public const int ktagSimpleThree = 83;

		public const int ktagSimpleFour = 85;

		public string SimpleThree
		{
			get { return m_simpleThree; }
			set
			{
				m_simpleThree = value;
				RaiseSimpleThreeChanged();
			}
		}

		public MockData1 SimpleFour
		{
			get { return m_simpleFour; }
			set
			{
				m_simpleFour = value;
				RaiseSimpleFourChanged();
			}
		}

		/// <summary>
		/// Eventually a framework will trigger these, typically somewhat later than exactly when it changes.
		/// For test purposes and until MultiUnicodeAccessor is enhanced, we just trigger the event from the test code.
		/// </summary>
		/// <param name="ws"></param>
		public void RaiseMlSimpleOneChanged(int ws)
		{
			if (MlSimpleOneChanged != null)
				MlSimpleOneChanged(this, new MlsChangedEventArgs(ws));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Get the name of the class.</summary>
		/// ------------------------------------------------------------------------------------
		public string ClassName
		{
			get { return "MockData1"; }
		}

		public IFdoServiceLocator Services
		{
			get { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Get the ID of the class.</summary>
		/// ------------------------------------------------------------------------------------
		public uint ClassID
		{
			get
			{
				return 96;
			}
		}

		internal void RaiseSimpleTwoChanged()
		{
			if (SimpleTwoChanged != null)
				SimpleTwoChanged(this, new EventArgs());
		}

		internal void RaiseSimpleThreeChanged()
		{
			if (SimpleThreeChanged != null)
				SimpleThreeChanged(this, new EventArgs());
		}

		internal void RaiseSimpleFourChanged()
		{
			if (SimpleFourChanged != null)
				SimpleFourChanged(this, new EventArgs());
		}

		internal List<MockData1> ObjSeq1
		{
			get
			{
				return m_ObjSeq;
			}
		}

		internal void InsertIntoObjSeq1(int index, MockData1 item)
		{
			m_ObjSeq.Insert(index, item);
			RaiseObjectSeq1Changed();
		}

		internal void ReplaceObjSeq1(MockData1[] newValue)
		{
			m_ObjSeq.Clear();
			m_ObjSeq.AddRange(newValue);
			RaiseObjectSeq1Changed();
		}

		internal void RemoveAtObjSeq1(int index)
		{
			m_ObjSeq.RemoveAt(index);
			RaiseObjectSeq1Changed();
		}

		public event EventHandler<EventArgs> ObjSeq1Changed;

		internal void RaiseObjectSeq1Changed()
		{
			if (ObjSeq1Changed != null)
				ObjSeq1Changed(this, new EventArgs());
		}

		public int ObjSeq1HookupCount
		{
			get { return ObjSeq1Changed.GetInvocationList().Length; }
		}

		public int SimpleThreeHookupCount
		{
			get
			{
				if (SimpleThreeChanged == null)
					return 0;
				return SimpleThreeChanged.GetInvocationList().Length;
			}
		}

		public int SimpleFourHookupCount
		{
			get
			{
				if (SimpleFourChanged == null)
					return 0;
				return SimpleFourChanged.GetInvocationList().Length;
			}
		}

		internal IList<MockData1> ObjSeq2 { get; private set; }

		public event EventHandler<EventArgs> ObjSeq2Changed;

		#region ICmObject Members

		public FdoCache Cache
		{
			get { throw new NotImplementedException(); }
		}

		public void MergeObject(ICmObject objSrc)
		{
			throw new NotImplementedException();
		}

		public void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			throw new NotImplementedException();
		}

		public bool CanDelete
		{
			get { throw new NotImplementedException(); }
		}

		public bool CheckConstraints(int flidToCheck, out ConstraintFailure failure)
		{
			throw new NotImplementedException();
		}

		public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			throw new NotImplementedException();
		}

		public void PostClone(Dictionary<int, ICmObject> copyMap)
		{
		}

		public void AllReferencedObjects(List<ICmObject> collector)
		{
			throw new NotImplementedException();
		}

		public bool IsFieldRelevant(int flid)
		{
			throw new NotImplementedException();
		}

		public bool IsOwnedBy(ICmObject possibleOwner)
		{
			throw new NotImplementedException();
		}

		public ICmObject ReferenceTargetOwner(int flid)
		{
			throw new NotImplementedException();
		}

		public bool IsFieldRequired(int flid)
		{
			throw new NotImplementedException();
		}

		public ITsString ChooserNameTS
		{
			get { throw new NotImplementedException(); }
		}

		public ITsString DeletionTextTSS
		{
			get { throw new NotImplementedException(); }
		}

		int ICmObject.ClassID
		{
			get { throw new NotImplementedException(); }
		}

		public Guid Guid
		{
			get { throw new NotImplementedException(); }
		}

		public ICmObjectId Id
		{
			get { throw new NotImplementedException(); }
		}

		public ICmObject GetObject(ICmObjectRepository repo)
		{
			throw new NotImplementedException();
		}

		public int Hvo
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsFieldRequired(uint flid)
		{
			throw new NotImplementedException();
		}

		public int IndexInOwner
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			throw new NotImplementedException();
		}

		bool ICmObject.IsValidObject
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsValidObject()
		{
			throw new NotImplementedException();
		}

		int ICmObject.OwningFlid
		{
			get { throw new NotImplementedException(); }
		}

		public int OwnOrd
		{
			get { throw new NotImplementedException(); }
		}

		public ICmObject Owner
		{
			get { throw new NotImplementedException(); }
		}

		public uint OwningFlid
		{
			get { throw new NotImplementedException(); }
		}

		public T OwnerOfClass<T>() where T : ICmObject
		{
			throw new NotImplementedException();
		}

		public ICmObject Self
		{
			get { throw new NotImplementedException(); }
		}

		public string ShortName
		{
			get { throw new NotImplementedException(); }
		}

		public ITsString ObjectIdName
		{
			get { throw new NotImplementedException(); }
		}

		public ITsString ShortNameTSS
		{
			get { throw new NotImplementedException(); }
		}

		public string SortKey
		{
			get { throw new NotImplementedException(); }
		}

		public int SortKey2
		{
			get { throw new NotImplementedException(); }
		}

		public string SortKey2Alpha
		{
			get { throw new NotImplementedException(); }
		}

		public HashSet<ICmObject> ReferringObjects
		{
			get { throw new NotImplementedException(); }
		}

		public string SortKeyWs
		{
			get { throw new NotImplementedException(); }
		}

		public string ToXmlString()
		{
			throw new NotImplementedException();
		}

		public DateTime UpdTime
		{
			get { throw new NotImplementedException(); }
		}

		public void Delete()
		{
			throw new NotImplementedException();
		}

		public ICmObject OwnerOfClass(int clsid)
		{
			throw new NotImplementedException();
		}

		public void UpdateTargetTimestamps()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	/// <summary>
	/// We will typically generate a class like this for each main data class
	/// </summary>
	public class MockData1Props
	{
		/// <summary>
		/// Static methods only
		/// </summary>
		private MockData1Props()
		{
		}

		/// <summary>
		/// We will typically generate an adapter-creating method for each property of the main class that we want to access
		/// using SharpViews.
		/// </summary>

		public static MlsHookupAdapter MlSimpleOne(MockData1 target)
		{
			return new MlsHookupAdapter(target, hookup => target.MlSimpleOneChanged += hookup.MlsPropChanged,
										hookup => target.MlSimpleOneChanged -= hookup.MlsPropChanged);
		}

		public static TssHookupAdapter SimpleTwo(MockData1 target)
		{
			return new TssHookupAdapter(target, () => target.SimpleTwo,
										hookup => target.SimpleTwoChanged += hookup.TssPropChanged,
										hookup => target.SimpleTwoChanged -= hookup.TssPropChanged);
		}
	}
}
