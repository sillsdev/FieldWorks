// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SingleLexReference.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.Application.ApplicationServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is a "virtual" class that implements one member of a lexical relation in
	/// a form that is tractable for exporting to MDF or LIFT.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SingleLexReference : ICmObject
	{
		/// <summary></summary>
		protected readonly ILexReference m_lexRef;
		/// <summary></summary>
		protected int m_hvoCrossRef;
		/// <summary></summary>
		protected ICmObject m_coRef = null;
		/// <summary></summary>
		protected int m_nMappingType = -1;
		private readonly FdoCache m_cache;
		private readonly ICmObjectRepository m_cmObjectRepository;
		private readonly IWritingSystemManager m_wsManager;

		/// <summary>Constructor.</summary>
		public SingleLexReference(ICmObject lexRef, int hvoCrossRef)
		{
			m_lexRef = lexRef as ILexReference;
			m_hvoCrossRef = hvoCrossRef;
			m_cache = lexRef.Cache;
			m_cmObjectRepository = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			m_wsManager = m_cache.ServiceLocator.WritingSystemManager;
		}

		/// <summary></summary>
		protected ICmObject CrossRefObject
		{
			get
			{
				if (m_coRef == null)
					m_coRef = m_cmObjectRepository.GetObject(m_hvoCrossRef);
				return m_coRef;
			}
		}

		/// <summary></summary>
		public bool IsOwnedBy(ICmObject possibleOwner)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void Delete()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Obtain the type of the current lex reference.
		/// </summary>
		public int MappingType
		{
			get
			{
				if (m_nMappingType < 0)
				{
					var lrt = m_lexRef.Owner as ILexRefType;
					m_nMappingType = lrt.MappingType;
				}
				return m_nMappingType;
			}
		}

		/// <summary></summary>
		public string MappingTypeName
		{
			get
			{
				LexRefTypeTags.MappingTypes maptype = (LexRefTypeTags.MappingTypes)MappingType;
				string s = Enum.Format(typeof(LexRefTypeTags.MappingTypes), maptype, "G");
				if (s.StartsWith("kmt"))
					return s.Substring(3);
				else
					return s;
			}
		}
		/// <summary>
		/// Access the database id of the specific reference element represented by the
		/// object.
		/// </summary>
		public int CrossRefHvo
		{
			get { return m_hvoCrossRef; }
			set
			{
				if (m_hvoCrossRef != value)
				{
					m_hvoCrossRef = value;
					m_coRef = null;
				}
			}
		}

		/// <summary>
		/// Obtain the value for the \lf field for the current LexEntry or LexSense as given
		/// by hvo, in the given writing system.
		/// </summary>
		/// <param name="ws">database id for a writing system</param>
		/// <param name="hvo">database id for a LexEntry or LexSense</param>
		/// <returns></returns>
		public string TypeAbbreviation(int ws, int hvo)
		{
			return m_lexRef.TypeAbbreviation(ws, m_lexRef.Services.GetObject(hvo));
		}

		/// <summary>
		/// Obtain the value for the \lf field for the current LexEntry or LexSense as given
		/// by hvo, in the given writing system.
		/// </summary>
		/// <param name="ws">database id for a writing system</param>
		/// <param name="hvo">database id for a LexEntry or LexSense</param>
		/// <returns></returns>
		public string TypeName(int ws, int hvo)
		{
			ILexRefType lrtOwner;
			SpecialWritingSystemCodes wsCode;
			GetOwnerAndWsCode(ws, out lrtOwner, out wsCode);
			/*
				For all but 2, 6, and 8 the field label for all items would be Name.
				For 2, 6, and 8, the label for the first item would be Name,
				while the label for the other items would be ReverseName.
			 */
			string x = null;
			switch ((LexRefTypeTags.MappingTypes)lrtOwner.MappingType)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtSenseTree:
				case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtEntryTree:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
					if (ws > 0)
					{
						if (m_lexRef.TargetsRS[0].Hvo == hvo)
							x = lrtOwner.Name.get_String(ws).Text;
						else
							x = lrtOwner.ReverseName.get_String(ws).Text;
					}
					else
					{
						if (m_lexRef.TargetsRS[0].Hvo == hvo)
							x = lrtOwner.Name.GetAlternative(wsCode);
						else
							x = lrtOwner.ReverseName.GetAlternative(wsCode);
					}
					break;
				default:
					if (ws > 0)
						x = lrtOwner.Name.get_String(ws).Text;
					else
						x = lrtOwner.Name.GetAlternative(wsCode);
					break;
			}
			return x;
		}

		private void GetOwnerAndWsCode(int ws, out ILexRefType lrtOwner, out SpecialWritingSystemCodes wsCode)
		{
			lrtOwner = m_lexRef.Owner as ILexRefType;
			wsCode = SpecialWritingSystemCodes.DefaultAnalysis;
			if (ws < 0)
			{
				switch (ws)
				{

					case (int)CellarModuleDefns.kwsAnal:
						wsCode = SpecialWritingSystemCodes.DefaultAnalysis;
						break;
					case (int)CellarModuleDefns.kwsVern:
						wsCode = SpecialWritingSystemCodes.DefaultVernacular;
						break;
					default:
						wsCode = (SpecialWritingSystemCodes)ws;
						break;
				}
			}
		}

		/// <summary>
		/// Obtain the value for the \lv field in the given writing system.
		/// </summary>
		/// <param name="ws">database id for a writing system</param>
		/// <returns></returns>
		public string CrossReference(int ws)
		{
			if (CrossRefObject is ILexEntry)
			{
				return (CrossRefObject as ILexEntry).HeadWordForWs(ws).Text;
			}
			else if (CrossRefObject is ILexSense)
			{
				return (CrossRefObject as ILexSense).OwnerOutlineNameForWs(ws).Text;
			}
			else
			{
				return "???";
			}
		}

		/// <summary>
		/// Obtain the value for the \le field in the given writing system.
		/// </summary>
		/// <param name="ws">database id for a writing system</param>
		/// <returns></returns>
		public string CrossReferenceGloss(int ws)
		{
			if (CrossRefObject is ILexEntry)
			{
				var le = CrossRefObject as ILexEntry;
				if (le.SensesOS.Count > 0)
				{
					return le.SensesOS[0].Gloss.get_String(ws).Text;
				}
				else
				{
					return "";
				}
			}
			else if (CrossRefObject is ILexSense)
			{
				return (CrossRefObject as ILexSense).Gloss.get_String(ws).Text;
			}
			else
			{
				return "";
			}
		}

		/// <summary>
		/// Returns the 1-based order number, or an empty string if irrelevant.
		/// </summary>
		public string RefOrder
		{
			get
			{
				int nOrd = m_lexRef.SequenceIndex(CrossRefObject.Hvo);
				if (nOrd > 0)
					return nOrd.ToString();
				else
					return String.Empty;
			}
		}

		/// <summary>
		/// Return the Guid of the cross reference object.
		/// </summary>
		public Guid RefGuid
		{
			get { return CrossRefObject.Guid; }
		}

		/// <summary></summary>
		public string RefLIFTid
		{
			get
			{
				return CrossRefObject is ILexEntry
						? (CrossRefObject as ILexEntry).LIFTid
						: (CrossRefObject is ILexSense ? (CrossRefObject as ILexSense).LIFTid : "???");
			}
		}

		/// <summary>
		/// Return the Guid of the specific LexReference object.
		/// </summary>
		public Guid RelationGuid
		{
			get { return m_lexRef.Guid; }
		}

		/// <summary>
		/// Return the LiftResidueContent of the specific LexReference object.
		/// </summary>
		public string LiftResidueContent
		{
			get { return m_lexRef.LiftResidueContent; }
		}

		/// <summary>
		/// Return the dateCreated attribute value from the LiftResidue.
		/// </summary>
		public string LiftDateCreated
		{
			get { return m_lexRef.LiftDateCreated; }
		}

		/// <summary>
		/// Return the dateModified attribute value from the LiftResidue.
		/// </summary>
		public string LiftDateModified
		{
			get { return m_lexRef.LiftDateModified; }
		}

		/// <summary>
		/// Return the Name of the specific LexReference object.
		/// </summary>
		public IMultiUnicode Name
		{
			get { return m_lexRef.Name; }
		}

		/// <summary>
		/// Return the Comment of the specific LexReference object.
		/// </summary>
		public IMultiString Comment
		{
			get { return m_lexRef.Comment; }
		}

		#region ICmObject Members

		/// <summary></summary>
		public FdoCache Cache
		{
			get { return m_cache; }
		}

		/// <summary></summary>
		public bool CanDelete
		{
			get { return false; }
		}

		/// <summary></summary>
		public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gives an object an opportunity to do any class-specific side-effect work when it has
		/// been cloned with DomainServices.CopyObject. CopyObject will call this method on each
		/// source object it copies after the copy is complete. The copyMap contains the source
		/// object Hvo as the Key and the copied object as the Value.
		/// </summary>
		/// <param name="copyMap"></param>
		/// ------------------------------------------------------------------------------------
		public void PostClone(Dictionary<int, ICmObject> copyMap)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void AllReferencedObjects(List<ICmObject> collector)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public ITsString ChooserNameTS
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public int ClassID
		{
			get { return 123456; }
		}

		/// <summary></summary>
		public string ClassName
		{
			get { return "SingleLexReference"; }
		}

		/// <summary></summary>
		public ITsString DeletionTextTSS
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public Guid Guid
		{
			get { return Guid.Empty; }
		}

		/// <summary></summary>
		public ICmObjectId Id
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public ICmObject GetObject(ICmObjectRepository repo)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public int Hvo
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public int IndexInOwner
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public bool IsFieldRequired(int flid)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public bool IsValidObject
		{
			get { return false; }
		}

		/// <summary></summary>
		public int OwnOrd
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public ICmObject Owner
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public T OwnerOfClass<T>() where T : ICmObject
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public ICmObject OwnerOfClass(int clsid)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public int OwningFlid
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public ICmObject ReferenceTargetOwner(int flid)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public ICmObject Self
		{
			get { return this; }
		}

		/// <summary></summary>
		public IFdoServiceLocator Services
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string ShortName
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public ITsString ObjectIdName
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public ITsString ShortNameTSS
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string SortKey
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public int SortKey2
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string SortKey2Alpha
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public HashSet<ICmObject> ReferringObjects
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string SortKeyWs
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string ToXmlString()
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void MergeObject(ICmObject objSrc)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public bool IsFieldRelevant(int flid)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
