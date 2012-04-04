// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2008' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LiftMerger.cs
// Responsibility: SteveMc (original version by John Hatton as extension)
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using LiftIO;
using LiftIO.Parsing;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	#region LIFT model classes
	/// <summary>
	/// This class implements "annotation" from the LIFT standard.
	/// </summary>
	public class LiftAnnotation
	{
		string m_name;
		string m_value;
		string m_who;
		DateTime m_when;
		LiftMultiText m_mtComment;

		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}
		public string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}
		public string Who
		{
			get { return m_who; }
			set { m_who = value; }
		}
		public DateTime When
		{
			get { return m_when; }
			set { m_when = value; }
		}
		public LiftMultiText Comment
		{
			get { return m_mtComment; }
			set { m_mtComment = value; }
		}
	}
	/// <summary>
	/// This class implements "trait" from the LIFT standard.
	/// </summary>
	public class LiftTrait
	{
		string m_name;
		string m_value;
		string m_id;
		readonly List<LiftAnnotation> m_rgAnnotations = new List<LiftAnnotation>();

		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}
		public string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}
		public string Id
		{
			get { return m_id; }
			set { m_id = value; }
		}
		public List<LiftAnnotation> Annotations
		{
			get { return m_rgAnnotations; }		// no set needed.
		}
	}
	/// <summary>
	/// This class implements "field" from the LIFT standard.
	/// </summary>
	public class LiftField
	{
		string m_type;
		DateTime m_dateCreated;
		DateTime m_dateModified;
		readonly List<LiftTrait> m_rgTraits = new List<LiftTrait>();
		readonly List<LiftAnnotation> m_rgAnnotations = new List<LiftAnnotation>();
		LiftMultiText m_mtContent;

		public LiftField()
		{
		}
		public LiftField(string type, LiftMultiText contents)
		{
			m_type = type;
			m_mtContent = contents;
		}
		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public DateTime DateCreated
		{
			get { return m_dateCreated; }
			set { m_dateCreated = value; }
		}
		public DateTime DateModified
		{
			get { return m_dateModified; }
			set { m_dateModified = value; }
		}
		public List<LiftTrait> Traits
		{
			get { return m_rgTraits; }			// no set needed.
		}
		public List<LiftAnnotation> Annotations
		{
			get { return m_rgAnnotations; }		// no set needed.
		}
		public LiftMultiText Content
		{
			get { return m_mtContent; }
			set { m_mtContent = value; }
		}
	}
	/// <summary>
	/// This class implements "extensible" from the LIFT standard.
	/// It also corresponds (roughly) to CmObject in the FieldWorks model.
	/// </summary>
	public abstract class LiftObject
	{
		string m_id;
		ICmObject m_cmo;
		Guid m_guid;
		DateTime m_dateCreated;
		DateTime m_dateModified;
		readonly List<LiftField> m_rgFields = new List<LiftField>();
		readonly List<LiftTrait> m_rgTraits = new List<LiftTrait>();
		readonly List<LiftAnnotation> m_rgAnnotations = new List<LiftAnnotation>();

		protected LiftObject()
		{
			m_cmo = null;
			m_dateCreated = DateTime.MinValue;
			m_dateModified = DateTime.MinValue;
		}
		public string Id
		{
			get { return m_id; }
			set { m_id = value; }
		}
		public ICmObject CmObject
		{
			get { return m_cmo; }
			set { m_cmo = value; }
		}
		public Guid Guid
		{
			get { return m_guid; }
			set { m_guid = value; }
		}
		public DateTime DateCreated
		{
			get { return m_dateCreated; }
			set { m_dateCreated = value; }
		}
		public DateTime DateModified
		{
			get { return m_dateModified; }
			set { m_dateModified = value; }
		}
		public List<LiftField> Fields
		{
			get { return m_rgFields; }			// no set needed.
		}
		public List<LiftTrait> Traits
		{
			get { return m_rgTraits; }			// no set needed.
		}
		public List<LiftAnnotation> Annotations
		{
			get { return m_rgAnnotations; }		// no set needed.
		}
		public abstract string XmlTag
		{
			get;
		}
	}
	/// <summary>
	/// This class implements "translation" from the LIFT standard.
	/// </summary>
	public class LiftTranslation
	{
		string m_type;
		LiftMultiText m_mtContent;

		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public LiftMultiText Content
		{
			get { return m_mtContent; }
			set { m_mtContent = value; }
		}
	}
	/// <summary>
	/// This class implements "example" from the LIFT standard.
	/// It also corresponds to LexExampleSentence in the FieldWorks model.
	/// </summary>
	public class LiftExample : LiftObject
	{
		string m_source;
		LiftMultiText m_mtContent;
		readonly List<LiftTranslation> m_rgTranslations = new List<LiftTranslation>();
		readonly List<LiftNote> m_rgNotes = new List<LiftNote>();	// not really in LIFT 0.12?

		public string Source
		{
			get { return m_source; }
			set { m_source = value; }
		}
		public LiftMultiText Content
		{
			get { return m_mtContent; }
			set { m_mtContent = value; }
		}
		public List<LiftTranslation> Translations
		{
			get { return m_rgTranslations; }		// no set needed.
		}
		public List<LiftNote> Notes
		{
			get { return m_rgNotes; }				// no set needed.
		}
		public override string XmlTag
		{
			get { return "example"; }
		}
	}
	/// <summary>
	/// This class implements "grammatical-info" from the LIFT standard.
	/// It also roughly corresponds to MoMorphSynAnalysis in the FieldWorks model.
	/// </summary>
	public class LiftGrammaticalInfo
	{
		string m_value;
		readonly List<LiftTrait> m_rgTraits = new List<LiftTrait>();

		public string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}
		public List<LiftTrait> Traits
		{
			get { return m_rgTraits; }		// no set needed.
		}
	}
	/// <summary>
	/// This class implements "reversal" from the LIFT standard.
	/// It also roughly corresponds to ReversalIndexEntry in the FieldWorks model.
	/// </summary>
	public class LiftReversal : LiftObject
	{
		string m_type;
		LiftMultiText m_mtForm;
		LiftReversal m_main;
		LiftGrammaticalInfo m_graminfo;

		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public LiftMultiText Form
		{
			get { return m_mtForm; }
			set { m_mtForm = value; }
		}
		public LiftReversal Main
		{
			get { return m_main; }
			set { m_main = value; }
		}
		public LiftGrammaticalInfo GramInfo
		{
			get { return m_graminfo; }
			set { m_graminfo = value; }
		}
		public override string XmlTag
		{
			get { return "reversal"; }
		}
	}
	/// <summary>
	/// This class implements "Sense" from the LIFT standard.
	/// It also corresponds to LexSense from the FieldWorks model.
	/// </summary>
	public class LiftSense : LiftObject
	{
		int m_order;
		LiftGrammaticalInfo m_graminfo;
		LiftMultiText m_mtGloss;
		LiftMultiText m_mtDefinition;
		readonly List<LiftRelation> m_rgRelations = new List<LiftRelation>();
		readonly List<LiftNote> m_rgNotes = new List<LiftNote>();
		readonly List<LiftExample> m_rgExamples = new List<LiftExample>();
		readonly List<LiftReversal> m_rgReversals = new List<LiftReversal>();
		readonly List<LiftUrlRef> m_rgPictures = new List<LiftUrlRef>();
		readonly List<LiftSense> m_rgSenses = new List<LiftSense>();
		LiftObject m_owner;

		public LiftSense()
		{
		}
		public LiftSense(Extensible info, Guid guid, FdoCache cache, LiftObject owner, FlexLiftMerger merger)
		{
			Id = info.Id;
			Guid = guid;
			if (guid == Guid.Empty)
				CmObject = null;
			else
				CmObject = merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			m_owner = owner;
		}
		public int Order
		{
			get { return m_order; }
			set { m_order = value; }
		}
		public LiftGrammaticalInfo GramInfo
		{
			get { return m_graminfo; }
			set { m_graminfo = value; }
		}
		public LiftMultiText Gloss
		{
			get { return m_mtGloss; }
			set { m_mtGloss = value; }
		}
		public LiftMultiText Definition
		{
			get { return m_mtDefinition; }
			set { m_mtDefinition = value; }
		}
		public List<LiftRelation> Relations
		{
			get { return m_rgRelations; }		// no set needed.
		}
		public List<LiftNote> Notes
		{
			get { return m_rgNotes; }			// no set needed.
		}
		public List<LiftExample> Examples
		{
			get { return m_rgExamples; }		// no set needed.
		}
		public List<LiftReversal> Reversals
		{
			get { return m_rgReversals; }		// no set needed.
		}
		public List<LiftUrlRef> Illustrations
		{
			get { return m_rgPictures; }		// no set needed.
		}
		public List<LiftSense> Subsenses
		{
			get { return m_rgSenses; }			// no set needed.
		}
		public LiftEntry OwningEntry
		{
			get
			{
				LiftObject owner;
				for (owner = m_owner; owner is LiftSense; owner = (owner as LiftSense).Owner)
				{
				}
				return owner as LiftEntry;
			}
		}
		public LiftObject Owner
		{
			get { return m_owner; }
		}
		public override string XmlTag
		{
			get { return "sense/subsense"; }
		}
	}
	/// <summary>
	/// This class implements "URLRef" from the LIFT standard.
	/// </summary>
	public class LiftUrlRef
	{
		string m_href;
		LiftMultiText m_mtLabel;

		public string Url
		{
			get { return m_href; }
			set { m_href = value; }
		}
		public LiftMultiText Label
		{
			get { return m_mtLabel; }
			set { m_mtLabel = value; }
		}
	}
	/// <summary>
	/// This class implements "note" from the LIFT standard.
	/// </summary>
	public class LiftNote : LiftObject
	{
		string m_type;
		LiftMultiText m_mtContent;

		public LiftNote()
		{
		}
		public LiftNote(string type, LiftMultiText contents)
		{
			m_type = type;
			m_mtContent = contents;
		}
		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public LiftMultiText Content
		{
			get { return m_mtContent; }
			set { m_mtContent = value; }
		}
		public override string XmlTag
		{
			get { return "note"; }
		}
	}
	/// <summary>
	/// This class implements "phonetic" from the LIFT standard.
	/// </summary>
	public class LiftPhonetic : LiftObject
	{
		LiftMultiText m_mtForm;
		readonly List<LiftUrlRef> m_rgMedia = new List<LiftUrlRef>();

		public LiftMultiText Form
		{
			get { return m_mtForm; }
			set { m_mtForm = value; }
		}
		public List<LiftUrlRef> Media
		{
			get { return m_rgMedia; }		// no set needed.
		}
		public override string XmlTag
		{
			get { return "pronunciation"; }
		}
	}
	/// <summary>
	/// This class implements "etymology" from the LIFT standard.
	/// </summary>
	public class LiftEtymology : LiftObject
	{
		string m_type;
		string m_source;
		LiftMultiText m_mtGloss;
		LiftMultiText m_mtForm;

		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public string Source
		{
			get { return m_source; }
			set { m_source = value; }
		}
		public LiftMultiText Gloss
		{
			get { return m_mtGloss; }
			set { m_mtGloss = value; }
		}
		public LiftMultiText Form
		{
			get { return m_mtForm; }
			set { m_mtForm = value; }
		}
		public override string XmlTag
		{
			get { return "etymology"; }
		}
	}
	/// <summary>
	/// This class implements "relation" from the LIFT standard.
	/// </summary>
	public class LiftRelation : LiftObject
	{
		string m_type;
		string m_ref;
		int m_order;
		LiftMultiText m_mtUsage;

		public LiftRelation()
		{
			m_order = -1;
		}
		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public string Ref
		{
			get { return m_ref; }
			set { m_ref = value; }
		}
		public int Order
		{
			get { return m_order; }
			set { m_order = value; }
		}
		public LiftMultiText Usage
		{
			get { return m_mtUsage; }
			set { m_mtUsage = value; }
		}
		public override string XmlTag
		{
			get { return "relation"; }
		}
	}
	/// <summary>
	/// This class implements "variant" from the LIFT standard.  (It represents an allomorph, not what
	/// FieldWorks understands to be a Variant.)
	/// </summary>
	public class LiftVariant : LiftObject
	{
		string m_ref;
		LiftMultiText m_mtVariantForm;
		readonly List<LiftPhonetic> m_rgPronunciations = new List<LiftPhonetic>();
		readonly List<LiftRelation> m_rgRelations = new List<LiftRelation>();
		string m_sRawXml;

		public string Ref
		{
			get { return m_ref; }
			set { m_ref = value; }
		}
		public LiftMultiText Form
		{
			get { return m_mtVariantForm; }
			set { m_mtVariantForm = value; }
		}
		public List<LiftPhonetic> Pronunciations
		{
			get { return m_rgPronunciations; }		// no set needed.
		}
		public List<LiftRelation> Relations
		{
			get { return m_rgRelations; }			// no set needed.
		}
		public string RawXml
		{
			get { return m_sRawXml; }
			set { m_sRawXml = value; }
		}
		public override string XmlTag
		{
			get { return "variant"; }
		}
	}
	/// <summary>
	/// This class implements "Entry" from the LIFT standard.
	/// It also corresponds to LexEntry in the FieldWorks model.
	/// </summary>
	public class LiftEntry : LiftObject
	{
		int m_order;
		DateTime m_dateDeleted;
		LiftMultiText m_mtLexicalForm;
		LiftMultiText m_mtCitation;
		readonly List<LiftPhonetic> m_rgPronunciations = new List<LiftPhonetic>();
		readonly List<LiftVariant> m_rgVariants = new List<LiftVariant>();
		readonly List<LiftSense> m_rgSenses = new List<LiftSense>();
		readonly List<LiftNote> m_rgNotes = new List<LiftNote>();
		readonly List<LiftRelation> m_rgRelations = new List<LiftRelation>();
		readonly List<LiftEtymology> m_rgEtymologies = new List<LiftEtymology>();
		/// <summary>preserve trait value from older LIFT files based on old FieldWorks model</summary>
		string m_sEntryType;
		string m_sMinorEntryCondition;
		bool m_fExcludeAsHeadword;

		public LiftEntry()
		{
			m_order = 0;
			m_dateDeleted = DateTime.MinValue;
		}
		public LiftEntry(Extensible info, Guid guid, int order, FdoCache cache, FlexLiftMerger merger)
		{
			Id = info.Id;
			Guid = guid;
			if (guid == Guid.Empty)
				CmObject = null;
			else
				CmObject = merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			m_dateDeleted = DateTime.MinValue;
			Order = order;
		}
		public int Order
		{
			get { return m_order; }
			set { m_order = value; }
		}
		public DateTime DateDeleted
		{
			get { return m_dateDeleted; }
			set { m_dateDeleted = value; }
		}
		public LiftMultiText LexicalForm
		{
			get { return m_mtLexicalForm; }
			set { m_mtLexicalForm = value; }
		}
		public LiftMultiText CitationForm
		{
			get { return m_mtCitation; }
			set { m_mtCitation = value; }
		}
		public List<LiftPhonetic> Pronunciations
		{
			get { return m_rgPronunciations; }	// no set needed.
		}
		public List<LiftVariant> Variants
		{
			get { return m_rgVariants; }		// no set needed.
		}
		public List<LiftSense> Senses
		{
			get { return m_rgSenses; }			// no set needed.
		}
		public List<LiftNote> Notes
		{
			get { return m_rgNotes; }			// no set needed.
		}
		public List<LiftRelation> Relations
		{
			get { return m_rgRelations; }		// no set needed.
		}
		public List<LiftEtymology> Etymologies
		{
			get { return m_rgEtymologies; }		// no set needed.
		}
		public override string XmlTag
		{
			get { return "entry"; }
		}
		public string EntryType
		{
			get { return m_sEntryType; }
			set { m_sEntryType = value; }
		}
		public string MinorEntryCondition
		{
			get { return m_sMinorEntryCondition; }
			set { m_sMinorEntryCondition = value; }
		}
		public bool ExcludeAsHeadword
		{
			get { return m_fExcludeAsHeadword; }
			set { m_fExcludeAsHeadword = value; }
		}
	}
	#endregion // LIFT model classes

	#region Category catalog class
	public class EticCategory
	{
		string m_id;
		string m_parent;
		readonly Dictionary<string, string> m_dictName = new Dictionary<string, string>();	// term
		readonly Dictionary<string, string> m_dictAbbrev = new Dictionary<string, string>();	// abbrev
		readonly Dictionary<string, string> m_dictDesc = new Dictionary<string, string>();	// def

		public string Id
		{
			get { return m_id; }
			set { m_id = value; }
		}
		public string ParentId
		{
			get { return m_parent; }
			set { m_parent = value; }
		}
		public Dictionary<string, string> MultilingualName
		{
			get { return m_dictName; }
		}
		public void SetName(string lang, string name)
		{
			if (m_dictName.ContainsKey(lang))
				m_dictName[lang] = name;
			else
				m_dictName.Add(lang, name);
		}
		public Dictionary<string, string> MultilingualAbbrev
		{
			get { return m_dictAbbrev; }
		}
		public void SetAbbrev(string lang, string abbrev)
		{
			if (m_dictAbbrev.ContainsKey(lang))
				m_dictAbbrev[lang] = abbrev;
			else
				m_dictAbbrev.Add(lang, abbrev);
		}
		public Dictionary<string, string> MultilingualDesc
		{
			get { return m_dictDesc; }
		}
		public void SetDesc(string lang, string desc)
		{
			if (m_dictDesc.ContainsKey(lang))
				m_dictDesc[lang] = desc;
			else
				m_dictDesc.Add(lang, desc);
		}
	}
	#endregion // Category catalog class

	/// <summary>
	/// This class is called by the LiftParser, as it encounters each element of a lift file.
	/// There is at least one other ILexiconMerger implementation, used in WeSay.
	/// </summary>
	public class FlexLiftMerger : ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>
	{
		readonly FdoCache m_cache;
		readonly ITsPropsFactory m_tpf = TsPropsFactoryClass.Create();
		ITsString m_tssEmpty;
		readonly GuidConverter m_gconv = (GuidConverter)TypeDescriptor.GetConverter(typeof(Guid));
		public const string LiftDateTimeFormat = "yyyy-MM-ddTHH:mm:ssK";	// wants UTC, but works with Local

		readonly Regex m_regexGuid = new Regex(
			"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// save field specification information from the header.
		readonly Dictionary<string, LiftMultiText> m_dictFieldDef = new Dictionary<string, LiftMultiText>();

		// maps for quick lookup of list items
		readonly Dictionary<string, ICmPossibility> m_dictPos = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictMmt = new Dictionary<string, ICmPossibility>(19);
		readonly Dictionary<string, ICmPossibility> m_dictComplexFormType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictVariantType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictSemDom = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictTransType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictAnthroCode = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictDomType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictSenseType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictStatus = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictUsageType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictLocation = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, List<IPhEnvironment>> m_dictEnvirons = new Dictionary<string, List<IPhEnvironment>>();
		readonly Dictionary<string, ICmPossibility> m_dictLexRefTypes = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictRevLexRefTypes = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictExceptFeats = new Dictionary<string, ICmPossibility>();

		readonly Dictionary<string, IFsFeatDefn> m_mapIdFeatDefn = new Dictionary<string, IFsFeatDefn>();
		readonly Dictionary<string, IFsFeatStrucType> m_mapIdFeatStrucType = new Dictionary<string, IFsFeatStrucType>();
		readonly Dictionary<string, IFsSymFeatVal> m_mapIdAbbrSymFeatVal = new Dictionary<string, IFsSymFeatVal>();
		readonly Dictionary<Guid, IFsFeatDefn> m_mapLiftGuidFeatDefn = new Dictionary<Guid, IFsFeatDefn>();
		readonly Dictionary<IFsComplexFeature, string> m_mapComplexFeatMissingTypeAbbr = new Dictionary<IFsComplexFeature, string>();
		readonly Dictionary<IFsClosedFeature, List<string>> m_mapClosedFeatMissingValueAbbrs = new Dictionary<IFsClosedFeature, List<string>>();
		readonly Dictionary<IFsFeatStrucType, List<string>> m_mapFeatStrucTypeMissingFeatureAbbrs = new Dictionary<IFsFeatStrucType, List<string>>();
		readonly Dictionary<string, IMoStemName> m_dictStemName = new Dictionary<string, IMoStemName>();

		// lists of new items added to lists
		readonly List<ICmPossibility> m_rgnewPos = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewMmt = new List<ICmPossibility>();
		readonly List<ILexEntryType> m_rgnewComplexFormType = new List<ILexEntryType>();
		readonly List<ILexEntryType> m_rgnewVariantType = new List<ILexEntryType>();
		readonly List<ICmPossibility> m_rgnewSemDom = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewTransType = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewCondition = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewAnthroCode = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewDomType = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewSenseType = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewStatus = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewUsageType = new List<ICmPossibility>();
		readonly List<ICmLocation> m_rgnewLocation = new List<ICmLocation>();
		readonly List<IPhEnvironment> m_rgnewEnvirons = new List<IPhEnvironment>();
		readonly List<ICmPossibility> m_rgnewLexRefTypes = new List<ICmPossibility>();
		readonly List<IMoInflClass> m_rgnewInflClasses = new List<IMoInflClass>();
		readonly List<IMoInflAffixSlot> m_rgnewSlots = new List<IMoInflAffixSlot>();
		readonly List<ICmPossibility> m_rgnewExceptFeat = new List<ICmPossibility>();
		readonly List<IMoStemName> m_rgnewStemName = new List<IMoStemName>();

		readonly List<IFsFeatDefn> m_rgnewFeatDefn = new List<IFsFeatDefn>();
		readonly List<IFsFeatStrucType> m_rgnewFeatStrucType = new List<IFsFeatStrucType>();

		readonly List<FieldDescription> m_rgnewCustomFields = new List<FieldDescription>();
		readonly Dictionary<string, int> m_mapMorphTypeUnknownCount = new Dictionary<string, int>();

		// map from id strings to database objects (for entries and senses).
		readonly Dictionary<string, ICmObject> m_mapIdObject = new Dictionary<string, ICmObject>();
		// list of errors encountered
		readonly List<string> m_rgErrorMsgs = new List<string>();

		// map from custom field tags to flids (for custom fields)
		readonly Dictionary<string, int> m_dictCustomFlid = new Dictionary<string, int>();

		// map from slot range name to slot map.
		readonly Dictionary<string, Dictionary<string, IMoInflClass>> m_dictDictSlots = new Dictionary<string, Dictionary<string, IMoInflClass>>();

		// map from (reversal's) writing system to reversal PartOfSpeech map.
		readonly Dictionary<int, Dictionary<string, ICmPossibility>> m_dictWsReversalPos = new Dictionary<int, Dictionary<string, ICmPossibility>>();

		// Remember the guids of deleted objects so that we don't try to reuse them.
		HashSet<Guid> m_deletedGuids = new HashSet<Guid>();

		/// <summary>
		/// MuElement = Multi-Unicode Element = one writing systemn and string of a multilingual
		/// unicode string.
		/// </summary>
		struct MuElement
		{
			readonly string m_text;
			readonly int m_ws;
			public MuElement(int ws, string text)
			{
				m_text = text;
				m_ws = ws;
			}

			public override bool Equals(object obj)
			{
				if (obj is MuElement)
				{
					var that = (MuElement)obj;
					return m_text == that.m_text && m_ws == that.m_ws;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_text.GetHashCode() + m_ws.GetHashCode();
			}
		}
		readonly Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>> m_mapToMapToRie =
			new Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>>();

		/// <summary>Set of guids for elements/senses that were found in the LIFT file.</summary>
		readonly Set<Guid> m_setUnchangedEntry = new Set<Guid>();

		readonly Set<Guid> m_setChangedEntry = new Set<Guid>();
		readonly Set<int> m_deletedObjects = new Set<int>();

		public enum MergeStyle
		{
			/// <summary>When there's a conflict, keep the existing data.</summary>
			MsKeepOld = 1,
			/// <summary>When there's a conflict, keep the data in the LIFT file.</summary>
			MsKeepNew = 2,
			/// <summary>When there's a conflict, keep both the existing data and the data in the LIFT file.</summary>
			MsKeepBoth = 3,
			/// <summary>Throw away any existing entries/senses/... that are not in the LIFT file.</summary>
			MsKeepOnlyNew = 4
		}
		MergeStyle m_msImport = MergeStyle.MsKeepOld;

		bool m_fTrustModTimes;

		readonly List<IWritingSystem> m_addedWss = new List<IWritingSystem>();

		// Repositories and factories for interacting with the project.

		ICmObjectRepository m_repoCmObject;
		IMoMorphTypeRepository m_repoMoMorphType;

		IFsFeatDefnRepository m_repoFsFeatDefn;
		IFsFeatStrucTypeRepository m_repoFsFeatStrucType;
		IFsSymFeatValRepository m_repoFsSymFeatVal;

		ICmAnthroItemFactory m_factCmAnthroItem;
		IMoStemAllomorphFactory m_factMoStemAllomorph;
		IMoAffixAllomorphFactory m_factMoAffixAllomorph;
		ILexPronunciationFactory m_factLexPronunciation;
		ICmMediaFactory m_factCmMedia;
		ILexEtymologyFactory m_factLexEtymology;
		ILexSenseFactory m_factLexSense;
		ILexEntryFactory m_factLexEntry;
		IMoInflClassFactory m_factMoInflClass;
		IMoInflAffixSlotFactory m_factMoInflAffixSlot;
		ILexExampleSentenceFactory m_factLexExampleSentence;
		ICmTranslationFactory m_factCmTranslation;
		ILexEntryTypeFactory m_factLexEntryType;
		ILexRefTypeFactory m_factLexRefType;
		ICmSemanticDomainFactory m_factCmSemanticDomain;
		ICmPossibilityFactory m_factCmPossibility;
		ICmLocationFactory m_factCmLocation;
		IMoStemMsaFactory m_factMoStemMsa;
		IMoUnclassifiedAffixMsaFactory m_factMoUnclassifiedAffixMsa;
		IMoDerivStepMsaFactory m_factMoDerivStepMsa;
		IMoDerivAffMsaFactory m_factMoDerivAffMsa;
		IMoInflAffMsaFactory m_factMoInflAffMsa;
		ICmPictureFactory m_factCmPicture;
		IReversalIndexEntryFactory m_factReversalIndexEntry;
		IReversalIndexFactory m_factReversalIndex;
		IPartOfSpeechFactory m_factPartOfSpeech;
		IMoMorphTypeFactory m_factMoMorphType;
		IPhEnvironmentFactory m_factPhEnvironment;
		ILexReferenceFactory m_factLexReference;
		ILexEntryRefFactory m_factLexEntryRef;

		IFsComplexFeatureFactory m_factFsComplexFeature;
		IFsOpenFeatureFactory m_factFsOpenFeature;
		IFsClosedFeatureFactory m_factFsClosedFeature;
		IFsFeatStrucTypeFactory m_factFsFeatStrucType;
		IFsSymFeatValFactory m_factFsSymFeatVal;
		IFsFeatStrucFactory m_factFsFeatStruc;
		IFsClosedValueFactory m_factFsClosedValue;
		IFsComplexValueFactory m_factFsComplexValue;

		/// <summary>
		/// This class stores the information for one range element from a *-feature-value range.
		/// This is used only if the corresponding IFsClosedFeature object cannot be found.
		/// </summary>
		internal class PendingFeatureValue
		{
			readonly string m_featId;
			readonly string m_id;
			readonly string m_catalogId;
			readonly bool m_fShowInGloss;
			readonly LiftMultiText m_abbrev;
			readonly LiftMultiText m_label;
			readonly LiftMultiText m_description;
			readonly Guid m_guidLift;

			internal PendingFeatureValue(string featId, string id, LiftMultiText description,
				LiftMultiText label, LiftMultiText abbrev, string catalogId, bool fShowInGloss,
				Guid guidLift)
			{
				m_featId = featId;
				m_id = id;
				m_catalogId = catalogId;
				m_fShowInGloss = fShowInGloss;
				m_abbrev = abbrev;
				m_label = label;
				m_description = description;
				m_guidLift = guidLift;
			}
			internal string FeatureId
			{
				get { return m_featId; }
			}
			internal string Id
			{
				get { return m_id; }
			}
			internal string CatalogId
			{
				get { return m_catalogId; }
			}
			internal bool ShowInGloss
			{
				get { return m_fShowInGloss; }
			}
			internal LiftMultiText Abbrev
			{
				get { return m_abbrev; }
			}
			internal LiftMultiText Label
			{
				get { return m_label; }
			}
			internal LiftMultiText Description
			{
				get { return m_description; }
			}
			internal Guid LiftGuid
			{
				get { return m_guidLift; }
			}
		}

		readonly List<PendingFeatureValue> m_rgPendingSymFeatVal = new List<PendingFeatureValue>();

		/// <summary>
		/// This stores information for a relation that will be set later because the
		/// target object may not have been imported yet.
		/// </summary>
		internal class PendingRelation
		{
			ICmObject m_cmo;
			ICmObject m_cmoTarget;
			readonly LiftRelation m_rel;
			string m_sResidue;

			public PendingRelation(ICmObject obj, LiftRelation rel, string sResidue)
			{
				m_cmo = obj;
				m_cmoTarget = null;
				m_rel = rel;
				m_sResidue = sResidue;
			}

			public ICmObject CmObject
			{
				get { return m_cmo; }
			}

			public int ObjectHvo
			{
				get { return m_cmo == null ? 0 : m_cmo.Hvo; }
			}

			public string RelationType
			{
				get { return m_rel.Type; }
			}

			public string TargetId
			{
				get { return m_rel.Ref; }
			}

			public ICmObject Target
			{
				get { return m_cmoTarget; }
				set { m_cmoTarget = value; }
			}

			public int TargetHvo
			{
				get { return m_cmoTarget == null ? 0 : m_cmoTarget.Hvo; }
			}

			public string Residue
			{
				get { return m_sResidue; }
			}

			public DateTime DateCreated
			{
				get { return m_rel.DateCreated; }
			}

			public DateTime DateModified
			{
				get { return m_rel.DateModified; }
			}

			internal bool IsSameOrMirror(PendingRelation rel)
			{
				if (rel == this)
					return true;
				if (rel.RelationType != RelationType)
					return false;
				if (rel.ObjectHvo == ObjectHvo && rel.Target == Target)
					return true;
				if (rel.ObjectHvo == TargetHvo && rel.Target == CmObject)
					return true;
				return false;
			}

			internal void MarkAsProcessed()
			{
				m_cmo = null;
			}

			internal bool HasBeenProcessed()
			{
				return m_cmo == null;
			}

			internal bool IsSequence
			{
				get { return m_rel.Order >= 0; }
			}

			internal int Order
			{
				get { return m_rel.Order; }
			}

			public override string ToString()
			{
				return String.Format("PendingRelation: type=\"{0}\", order={1}, target={2}, objHvo={3}",
					m_rel.Type, m_rel.Order, (m_cmoTarget == null ? 0 : m_cmoTarget.Hvo),
					(m_cmo == null ? 0 : m_cmo.Hvo));
			}

			internal string AsResidueString()
			{
				if (m_sResidue == null)
					m_sResidue = String.Empty;
				if (IsSequence)
				{
					return String.Format("<relation type=\"{0}\" ref=\"{1}\" order=\"{2}\"/>{3}",
						XmlUtils.MakeSafeXmlAttribute(m_rel.Type),
						XmlUtils.MakeSafeXmlAttribute(m_rel.Ref),
						m_rel.Order, Environment.NewLine);
				}
				return String.Format("<relation type=\"{0}\" ref=\"{1}\"/>{2}",
					XmlUtils.MakeSafeXmlAttribute(m_rel.Type),
					XmlUtils.MakeSafeXmlAttribute(m_rel.Ref), Environment.NewLine);
			}
		}
		readonly List<PendingRelation> m_rgPendingRelation = new List<PendingRelation>();
		readonly List<PendingRelation> m_rgPendingTreeTargets = new List<PendingRelation>();
		readonly LinkedList<PendingRelation> m_rgPendingCollectionRelations = new LinkedList<PendingRelation>();

		/// <summary>
		///
		/// </summary>
		internal class PendingLexEntryRef
		{
			readonly ICmObject m_cmo;
			ICmObject m_cmoTarget;
			readonly LiftRelation m_rel;
			readonly List<string> m_rgsComplexFormTypes = new List<string>();
			readonly List<string> m_rgsVariantTypes = new List<string>();
			bool m_fIsPrimary;
			int m_nHideMinorEntry;

			string m_sResidue;
			// preserve trait values from older LIFT files based on old FieldWorks model
			readonly string m_sEntryType;
			readonly string m_sMinorEntryCondition;
			bool m_fExcludeAsHeadword;
			LiftField m_summary;

			public PendingLexEntryRef(ICmObject obj, LiftRelation rel, LiftEntry entry)
			{
				m_cmo = obj;
				m_rel = rel;
				m_sResidue = null;
				m_sEntryType = null;
				m_sMinorEntryCondition = null;
				m_fExcludeAsHeadword = false;
				m_summary = null;
				if (entry != null)
				{
					m_sEntryType = entry.EntryType;
					m_sMinorEntryCondition = entry.MinorEntryCondition;
					m_fExcludeAsHeadword = entry.ExcludeAsHeadword;
					ProcessRelationData();
				}
			}

			private void ProcessRelationData()
			{
				List<LiftTrait> knownTraits = new List<LiftTrait>();
				foreach (LiftTrait trait in m_rel.Traits)
				{
					switch (trait.Name)
					{
						case "complex-form-type":
							m_rgsComplexFormTypes.Add(trait.Value);
							knownTraits.Add(trait);
							break;
						case "variant-type":
							m_rgsVariantTypes.Add(trait.Value);
							knownTraits.Add(trait);
							break;
						case "hide-minor-entry":
							Int32.TryParse(trait.Value, out m_nHideMinorEntry);
							knownTraits.Add(trait);
							break;
						case "is-primary":
							m_fIsPrimary = (trait.Value.ToLowerInvariant() == "true");
							m_fExcludeAsHeadword = m_fIsPrimary;
							knownTraits.Add(trait);
							break;
					}
				}
				foreach (LiftTrait trait in knownTraits)
					m_rel.Traits.Remove(trait);
				List<LiftField> knownFields = new List<LiftField>();
				foreach (LiftField field in m_rel.Fields)
				{
					if (field.Type == "summary")
					{
						m_summary = field;
						knownFields.Add(field);
					}
				}
				foreach (LiftField field in knownFields)
					m_rel.Fields.Remove(field);
			}

			public ICmObject CmObject
			{
				get { return m_cmo; }
			}

			public int ObjectHvo
			{
				get { return m_cmo == null ? 0 : m_cmo.Hvo; }
			}

			public string RelationType
			{
				get { return m_rel.Type; }
			}

			public string TargetId
			{
				get { return m_rel.Ref; }
			}

			public int TargetHvo
			{
				get { return m_cmoTarget == null ? 0 : m_cmoTarget.Hvo; }
			}

			public ICmObject Target
			{
				get { return m_cmoTarget; }
				set { m_cmoTarget = value; }
			}

			public string Residue
			{
				get { return m_sResidue; }
				set { m_sResidue = value; }
			}

			public DateTime DateCreated
			{
				get { return m_rel.DateCreated; }
			}

			public DateTime DateModified
			{
				get { return m_rel.DateModified; }
			}

			internal int Order
			{
				get { return m_rel.Order; }
			}

			public string EntryType
			{
				get { return m_sEntryType; }
			}

			public string MinorEntryCondition
			{
				get { return m_sMinorEntryCondition; }
			}

			public bool ExcludeAsHeadword
			{
				get { return m_fExcludeAsHeadword; }
			}

			public List<string> ComplexFormTypes
			{
				get { return m_rgsComplexFormTypes; }
			}

			public List<string> VariantTypes
			{
				get { return m_rgsVariantTypes; }
			}

			public bool IsPrimary
			{
				get { return m_fIsPrimary; }
				set { m_fIsPrimary = value; }
			}

			public int HideMinorEntry
			{
				get { return m_nHideMinorEntry; }
				set { m_nHideMinorEntry = value; }
			}

			public LiftField Summary
			{
				get { return m_summary; }
			}
		}
		readonly List<PendingLexEntryRef> m_rgPendingLexEntryRefs = new List<PendingLexEntryRef>();

		internal class PendingModifyTime
		{
			readonly ILexEntry m_le;
			readonly DateTime m_dt;

			public PendingModifyTime(ILexEntry le, DateTime dt)
			{
				m_le = le;
				m_dt = dt;
			}

			public void SetModifyTime()
			{
				m_le.DateModified = m_dt;
			}
		}
		readonly List<PendingModifyTime> m_rgPendingModifyTimes = new List<PendingModifyTime>();

		private int m_cEntriesAdded;
		private int m_cSensesAdded;
		private int m_cEntriesDeleted;
		private DateTime m_dtStart;		// when import started
		/// <summary>
		/// This stores the information for one object's LIFT import residue.
		/// </summary>
		class LiftResidue
		{
			readonly int m_flid;
			readonly XmlDocument m_xdoc;

			public LiftResidue(int flid, XmlDocument xdoc)
			{
				m_flid = flid;
				m_xdoc = xdoc;
			}

			public int Flid
			{
				get { return m_flid; }
			}

			public XmlDocument Document
			{
				get { return m_xdoc; }
			}
		}
		readonly Dictionary<int, LiftResidue> m_dictResidue = new Dictionary<int, LiftResidue>();

		internal abstract class ConflictingData
		{
			protected string m_sType;
			protected string m_sField;
			protected FlexLiftMerger m_merger;

			protected ConflictingData(string sType, string sField, FlexLiftMerger merger)
			{
				m_sType = sType;
				m_sField = sField;
				m_merger = merger;
			}
			public string ConflictType
			{
				get { return m_sType; }
			}
			public string ConflictField
			{
				get { return m_sField; }
			}
			public abstract string OrigHtmlReference();
			public abstract string DupHtmlReference();

		}
		internal static string LinkRef(ILexEntry le)
		{
			FwLinkArgs link = new FwLinkArgs("lexiconEdit", le.Guid);
			return XmlUtils.MakeSafeXmlAttribute(link.ToString());
		}
		internal class ConflictingEntry : ConflictingData
		{
			private ILexEntry m_leOrig;
			private ILexEntry m_leNew;

			public ConflictingEntry(string sField, ILexEntry leOrig, FlexLiftMerger merger)
				: base(LexTextControls.ksEntry, sField, merger)
			{
				m_leOrig = leOrig;
			}
			public override string OrigHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leOrig), HtmlString(m_leOrig.Headword)
				return String.Format("<a href=\"{0}\">{1}</a>", LinkRef(m_leOrig),
					m_merger.TsStringAsHtml(m_leOrig.HeadWord, m_leOrig.Cache));
			}

			public override string DupHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leNew), HtmlString(m_leNew.Headword)
				return String.Format("<a href=\"{0}\">{1}</a>", LinkRef(m_leNew),
					m_merger.TsStringAsHtml(m_leNew.HeadWord, m_leNew.Cache));
			}
			public ILexEntry DupEntry
			{
				set { m_leNew = value; }
			}
		}
		internal class ConflictingSense : ConflictingData
		{
			private ILexSense m_lsOrig;
			private ILexSense m_lsNew;
			public ConflictingSense(string sField, ILexSense lsOrig, FlexLiftMerger merger)
				: base(LexTextControls.ksSense, sField, merger)
			{
				m_lsOrig = lsOrig;
			}
			public override string OrigHtmlReference()
			{
				return String.Format("<a href=\"{0}\">{1}</a>",
					LinkRef(m_lsOrig.Entry),
					m_merger.TsStringAsHtml(OwnerOutlineName(m_lsOrig), m_lsOrig.Cache));
			}

			public override string DupHtmlReference()
			{
				return String.Format("<a href=\"{0}\">{1}</a>",
					LinkRef(m_lsNew.Entry),
					m_merger.TsStringAsHtml(OwnerOutlineName(m_lsNew), m_lsNew.Cache));
			}
			public ILexSense DupSense
			{
				set { m_lsNew = value; }
			}

			private ITsString OwnerOutlineName(ILexSense m_lsOrig)
			{
				return m_lsOrig.OwnerOutlineNameForWs(m_lsOrig.Cache.DefaultVernWs);
			}
		}
		private ConflictingData m_cdConflict = null;
		private List<ConflictingData> m_rgcdConflicts = new List<ConflictingData>();

		private List<EticCategory> m_rgcats = new List<EticCategory>();

		private string m_sLiftFile;
		// TODO WS: how should this be used in the new world?
		private string m_sLiftDir = null;
		private string m_sLiftProducer = null;		// the producer attribute in the lift element.
		private DateTime m_defaultDateTime = default(DateTime);
		private bool m_fCreatingNewEntry = false;
		private bool m_fCreatingNewSense = false;

		/// <summary>
		/// This is the base class for pending error reports.
		/// </summary>
		class PendingErrorReport
		{
			protected Guid m_guid;
			protected int m_flid;
			int m_ws;
			protected FdoCache m_cache;
			private FlexLiftMerger m_merger;

			internal PendingErrorReport(Guid guid, int flid, int ws, FdoCache cache, FlexLiftMerger merger)
			{
				m_guid = guid;
				m_flid = flid;
				m_ws = ws;
				m_cache = cache;
				m_merger = merger;
			}

			internal virtual string FieldName
			{
				get
				{
					// TODO: make this more informative and user-friendly.
					return m_cache.MetaDataCacheAccessor.GetFieldName((int)m_flid);
				}
			}

			private ILexEntry Entry()
			{
				ICmObject cmo = m_merger.GetObjectForGuid(m_guid);
				if (cmo is ILexEntry)
				{
					return cmo as ILexEntry;
				}
				else
				{
					return cmo.OwnerOfClass<ILexEntry>();
				}
			}

			internal string EntryHtmlReference()
			{
				ILexEntry le = Entry();
				if (le == null)
					return String.Empty;
				else
					return String.Format("<a href=\"{0}\">{1}</a>",
						LinkRef(le),
						m_merger.TsStringAsHtml(le.HeadWord, m_cache));

			}

			internal string WritingSystem
			{
				get
				{
					if (m_ws > 0)
					{
						IWritingSystem ws = m_merger.GetExistingWritingSystem(m_ws);
						return ws.DisplayLabel;
					}
					else
					{
						return null;
					}
				}
			}

			public override bool Equals(object obj)
			{
				PendingErrorReport that = obj as PendingErrorReport;
				if (that != null && m_flid == that.m_flid && m_guid == that.m_guid &&
					m_ws == that.m_ws)
				{
					if (m_cache != null && that.m_cache != null)
					{
						return m_cache == that.m_cache;
					}
					else
					{
						return m_cache == null && that.m_cache == null;
					}
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_flid + m_ws + m_guid.GetHashCode() + (m_cache == null ? 0 : m_cache.GetHashCode());
			}
		}

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that some imported data is actually invalid.
		/// </summary>
		class InvalidData : PendingErrorReport
		{
			string m_sMsg;
			string m_sValue;

			public InvalidData(string sMsg, Guid guid, int flid, string val, int ws, FdoCache cache, FlexLiftMerger merger)
				: base(guid, flid, ws, cache, merger)
			{
				m_sMsg = sMsg;
				m_sValue = val;
			}

			internal string ErrorMessage
			{
				get { return m_sMsg; }
			}

			internal string BadValue
			{
				get { return m_sValue; }
			}

			public override bool Equals(object obj)
			{
				InvalidData that = obj as InvalidData;
				return that != null && m_sMsg == that.m_sMsg && m_sValue == that.m_sValue &&
					base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() + (m_sMsg == null ? 0 : m_sMsg.GetHashCode()) +
					(m_sValue == null ? 0 : m_sValue.GetHashCode());
			}
		}
		List<InvalidData> m_rgInvalidData = new List<InvalidData>();

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that a relation element in the imported file is invalid.
		/// </summary>
		class InvalidRelation : PendingErrorReport
		{
			PendingLexEntryRef m_pendRef;

			public InvalidRelation(PendingLexEntryRef pend, FdoCache cache, FlexLiftMerger merger)
				: base(pend.CmObject.Guid, 0, 0, cache, merger)
			{
				m_pendRef = pend;
			}

			internal string TypeName
			{
				get { return m_pendRef.RelationType; }
			}

			internal string BadValue
			{
				get { return m_pendRef.TargetId; }
			}

			internal string ErrorMessage
			{
				get
				{
					if (m_pendRef.CmObject is ILexEntry)
						return LexTextControls.ksEntryInvalidRef;

					Debug.Assert(m_pendRef is ILexSense);
					return String.Format(LexTextControls.ksSenseInvalidRef,
						((ILexSense)m_pendRef.CmObject).OwnerOutlineNameForWs(
						m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle).Text);
				}
			}

			internal override string FieldName
			{
				get { return String.Empty; }
			}
		}
		List<InvalidRelation> m_rgInvalidRelation = new List<InvalidRelation>();

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that data has been truncated (lost) on import.
		/// </summary>
		class TruncatedData : PendingErrorReport
		{
			string m_sText;
			int m_cchMax;

			public TruncatedData(string sText, int cchMax, Guid guid, int flid, int ws, FdoCache cache, FlexLiftMerger merger)
				: base(guid, flid, ws, cache, merger)
			{
				m_sText = sText;
				m_cchMax = cchMax;
			}

			internal int StoredLength
			{
				get { return m_cchMax; }
			}

			internal string OriginalText
			{
				get { return m_sText; }
			}
		}
		List<TruncatedData> m_rgTruncated = new List<TruncatedData>();

		#region Constructors and other initialization methods
		public FlexLiftMerger(FdoCache cache, MergeStyle msImport, bool fTrustModTimes)
		{
			m_cSensesAdded = 0;
			m_cache = cache;
			m_tssEmpty = cache.TsStrFactory.EmptyString(cache.DefaultUserWs);
			m_msImport = msImport;
			m_fTrustModTimes = fTrustModTimes;

			// remember initial conditions.
			m_dtStart = DateTime.Now;

			if (m_cache.LangProject.PartsOfSpeechOA != null)
				InitializePossibilityMap(m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			InitializeMorphTypes();
			if (m_cache.LangProject.LexDbOA.ComplexEntryTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS,
					m_dictComplexFormType);
			if (m_cache.LangProject.LexDbOA.VariantEntryTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS,
					m_dictVariantType);
			if (m_cache.LangProject.SemanticDomainListOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS,
					m_dictSemDom);
				EnhancePossibilityMapForWeSay(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS,
					m_dictSemDom);
			}
			if (m_cache.LangProject.TranslationTagsOA != null)
				InitializePossibilityMap(m_cache.LangProject.TranslationTagsOA.PossibilitiesOS,
					m_dictTransType);
			if (m_cache.LangProject.AnthroListOA != null)
				InitializePossibilityMap(m_cache.LangProject.AnthroListOA.PossibilitiesOS,
					m_dictAnthroCode);
			if (m_cache.LangProject.MorphologicalDataOA != null)
			{
				if (m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA != null)
				{
					InitializePossibilityMap(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS,
						m_dictExceptFeats);
				}
			}
			if (m_cache.LangProject.LexDbOA.DomainTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS,
					m_dictDomType);
			if (m_cache.LangProject.LexDbOA.SenseTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.SenseTypesOA.PossibilitiesOS,
					m_dictSenseType);
			if (m_cache.LangProject.StatusOA != null)
				InitializePossibilityMap(m_cache.LangProject.StatusOA.PossibilitiesOS,
					m_dictStatus);
			if (m_cache.LangProject.LexDbOA.UsageTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS,
					m_dictUsageType);
			if (m_cache.LangProject.LocationsOA != null)
				InitializePossibilityMap(m_cache.LangProject.LocationsOA.PossibilitiesOS,
					m_dictLocation);
			if (m_cache.LangProject.PhonologicalDataOA != null)
			{
				foreach (IPhEnvironment env in m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS)
				{
					// More than one environment may have the same string representation.  This
					// is unfortunate, but it does happen.
					string s = env.StringRepresentation.Text;
					if (!String.IsNullOrEmpty(s))
					{
						List<IPhEnvironment> rgenv;
						if (m_dictEnvirons.TryGetValue(s, out rgenv))
						{
							rgenv.Add(env);
						}
						else
						{
							rgenv = new List<IPhEnvironment>();
							rgenv.Add(env);
							m_dictEnvirons.Add(s, rgenv);
						}
					}
				}
			}
			if (m_cache.LangProject.LexDbOA.ReferencesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS,
					m_dictLexRefTypes);
			InitializeReverseLexRefTypesMap();
			InitializeStemNameMap();
			InitializeReversalMaps();
			InitializeReversalPOSMaps();
			LoadCategoryCatalog();
		}

		/// <summary>
		/// Get or set the LIFT file being imported (merged from).
		/// </summary>
		public string LiftFile
		{
			get { return m_sLiftFile; }
			set
			{
				m_sLiftFile = value;
				if (!String.IsNullOrEmpty(m_sLiftFile))
				{
					m_sLiftDir = Path.GetDirectoryName(m_sLiftFile);
					m_defaultDateTime = File.GetLastWriteTimeUtc(m_sLiftFile);
					StoreLiftProducer();
				}
			}
		}

		private void StoreLiftProducer()
		{
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.ValidationType = ValidationType.None;
			readerSettings.IgnoreComments = true;
			using (XmlReader reader = XmlReader.Create(m_sLiftFile, readerSettings))
			{
				if (reader.IsStartElement("lift"))
					m_sLiftProducer = reader.GetAttribute("producer");
			}
		}

		private void InitializePossibilityMap(IFdoOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, ICmPossibility> dict)
		{
			if (possibilities == null)
				return;
			int ws;
			foreach (ICmPossibility poss in possibilities)
			{
				for (int i = 0; i < poss.Abbreviation.StringCount; ++i)
				{
					ITsString tss = poss.Abbreviation.GetStringFromIndex(i, out ws);
					AddToPossibilityMap(tss, poss, dict);
				}
				for (int i = 0; i < poss.Name.StringCount; ++i)
				{
					ITsString tss = poss.Name.GetStringFromIndex(i, out ws);
					AddToPossibilityMap(tss, poss, dict);
				}
				InitializePossibilityMap(poss.SubPossibilitiesOS, dict);
			}
		}

		private static void AddToPossibilityMap(ITsString tss, ICmPossibility poss, Dictionary<string, ICmPossibility> dict)
		{
			if (tss.Length > 0)
			{
				string s = tss.Text;
				if (!dict.ContainsKey(s))
					dict.Add(s, poss);
				s = s.ToLowerInvariant();
				if (!dict.ContainsKey(s))
					dict.Add(s, poss);
			}
		}

		/// <summary>
		/// WeSay stores Semantic Domain values as "abbr name", so fill in keys like that
		/// for lookup during import.
		/// </summary>
		/// <param name="possibilities"></param>
		/// <param name="dict"></param>
		private void EnhancePossibilityMapForWeSay(IFdoOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, ICmPossibility> dict)
		{
			foreach (ICmPossibility poss in possibilities)
			{
				for (int i = 0; i < poss.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tssAbbr = poss.Abbreviation.GetStringFromIndex(i, out ws);
					if (tssAbbr.Length > 0)
					{
						ITsString tssName = poss.Name.get_String(ws);
						if (tssName.Length > 0)
						{
							string sAbbr = tssAbbr.Text;
							string sName = tssName.Text;
							string sKey = String.Format("{0} {1}", sAbbr, sName);
							if (!dict.ContainsKey(sKey))
								dict.Add(sKey, poss);
							sKey = sKey.ToLowerInvariant();
							if (!dict.ContainsKey(sKey))
								dict.Add(sKey, poss);
						}
					}
				}
				EnhancePossibilityMapForWeSay(poss.SubPossibilitiesOS, dict);
			}
		}

		private void InitializeMorphTypes()
		{
			foreach (var poss in m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				for (var i = 0; i < poss.Name.StringCount; ++i)
				{
					int ws;
					var s = poss.Name.GetStringFromIndex(i, out ws).Text;
					if (String.IsNullOrEmpty(s) || m_dictMmt.ContainsKey(s))
						continue;
					m_dictMmt.Add(s, poss);
				}
			}
		}

		private void InitializeStemNameMap()
		{
			m_dictStemName.Clear();
			var posNames = new List<string>();
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
			{
				posNames.Clear();
				int ws;
				for (var i = 0; i < pos.Name.StringCount; ++i)
				{
					string name = pos.Name.GetStringFromIndex(i, out ws).Text;
					if (!String.IsNullOrEmpty(name))
						posNames.Add(name);
				}
				if (posNames.Count == 0)
					posNames.Add(String.Empty);		// should never happen!
				foreach (var stem in pos.AllStemNames)
				{
					for (var i = 0; i < stem.Name.StringCount; ++i)
					{
						var name = stem.Name.GetStringFromIndex(i, out ws).Text;
						if (String.IsNullOrEmpty(name))
							continue;
						foreach (var posName in posNames)
						{
							var key = String.Format("{0}:{1}", posName, name);
							if (!m_dictStemName.ContainsKey(key))
								m_dictStemName.Add(key, stem);
						}
					}
				}
			}
		}

		private void InitializeReversalMaps()
		{
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs =
					new Dictionary<MuElement, List<IReversalIndexEntry>>();
				m_mapToMapToRie.Add(ri, mapToRIEs);
				InitializeReversalMap(ri.EntriesOC, mapToRIEs);
			}
		}

		private void InitializeReversalMap(IFdoOwningCollection<IReversalIndexEntry> entries,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs)
		{
			foreach (IReversalIndexEntry rie in entries)
			{
				for (int i = 0; i < rie.ReversalForm.StringCount; ++i)
				{
					int ws;
					ITsString tss = rie.ReversalForm.GetStringFromIndex(i, out ws);
					if (tss.Length > 0)
					{
						MuElement mue = new MuElement(ws, tss.Text);
						AddToReversalMap(mue, rie, mapToRIEs);
					}
				}
				if (rie.SubentriesOC.Count > 0)
				{
					Dictionary<MuElement, List<IReversalIndexEntry>> submapToRIEs =
						new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(rie, submapToRIEs);
					InitializeReversalMap(rie.SubentriesOC, submapToRIEs);
				}
			}
		}

		private void AddToReversalMap(MuElement mue, IReversalIndexEntry rie,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs)
		{
			List<IReversalIndexEntry> rgrie;
			if (!mapToRIEs.TryGetValue(mue, out rgrie))
			{
				rgrie = new List<IReversalIndexEntry>();
				mapToRIEs.Add(mue, rgrie);
			}
			if (!rgrie.Contains(rie))
				rgrie.Add(rie);
		}

		private void InitializeReversalPOSMaps()
		{
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				var dict = new Dictionary<string, ICmPossibility>();
				if (ri.PartsOfSpeechOA != null)
					InitializePossibilityMap(ri.PartsOfSpeechOA.PossibilitiesOS, dict);
				Debug.Assert(!string.IsNullOrEmpty(ri.WritingSystem));
				int handle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
				if (m_dictWsReversalPos.ContainsKey(handle))
				{
					// REVIEW: SHOULD WE LOG A WARNING HERE?  THIS SHOULD NEVER HAPPEN!
					// (BUT IT HAS AT LEAST ONCE IN A 5.4.1 PROJECT)
				}
				else
				{
					m_dictWsReversalPos.Add(handle, dict);
				}
			}
		}

		private void InitializeReverseLexRefTypesMap()
		{
			int ws;
			foreach (ILexRefType lrt in m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS)
			{
				for (int i = 0; i < lrt.ReverseAbbreviation.StringCount; ++i)
				{
					ITsString tss = lrt.ReverseAbbreviation.GetStringFromIndex(i, out ws);
					AddToReverseLexRefTypesMap(tss, lrt);
				}
				for (int i = 0; i < lrt.ReverseName.StringCount; ++i)
				{
					ITsString tss = lrt.ReverseName.GetStringFromIndex(i, out ws);
					AddToReverseLexRefTypesMap(tss, lrt);
				}
			}
		}

		private void AddToReverseLexRefTypesMap(ITsString tss, ILexRefType lrt)
		{
			if (tss.Length > 0)
			{
				string s = tss.Text;
				if (!m_dictRevLexRefTypes.ContainsKey(s))
					m_dictRevLexRefTypes.Add(s, lrt);
				s = s.ToLowerInvariant();
				if (!m_dictRevLexRefTypes.ContainsKey(s))
					m_dictRevLexRefTypes.Add(s, lrt);
			}
		}

		private void LoadCategoryCatalog()
		{
			string sPath = System.IO.Path.Combine(DirectoryFinder.FWCodeDirectory,
				"Templates/GOLDEtic.xml");
			XmlDocument xd = new XmlDocument();
			xd.Load(sPath);
			XmlNode xnTop = xd.SelectSingleNode("eticPOSList");
			if (xnTop != null && xnTop.ChildNodes != null)
			{
				foreach (XmlNode node in xnTop.SelectNodes("item"))
				{
					string sType = XmlUtils.GetAttributeValue(node, "type");
					string sId = XmlUtils.GetAttributeValue(node, "id");
					if (sType == "category" && !String.IsNullOrEmpty(sId))
						LoadCategoryNode(node, sId, null);
				}
			}
		}

		private void LoadCategoryNode(XmlNode node, string id, string parent)
		{
			var cat = new EticCategory {Id = id, ParentId = parent};
			if (node != null)
			{
				foreach (XmlNode xn in node.SelectNodes("abbrev"))
				{
					var sWs = XmlUtils.GetAttributeValue(xn, "ws");
					var sAbbrev = xn.InnerText;
					if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sAbbrev))
						cat.SetAbbrev(sWs, sAbbrev);
				}
				foreach (XmlNode xn in node.SelectNodes("term"))
				{
					var sWs = XmlUtils.GetAttributeValue(xn, "ws");
					var sName = xn.InnerText;
					if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sName))
						cat.SetName(sWs, sName);
				}
				foreach (XmlNode xn in node.SelectNodes("def"))
				{
					var sWs = XmlUtils.GetAttributeValue(xn, "ws");
					var sDesc = xn.InnerText;
					if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sDesc))
						cat.SetDesc(sWs, sDesc);
				}
			}
			m_rgcats.Add(cat);
			if (node != null)
			{
				foreach (XmlNode xn in node.SelectNodes("item"))
				{
					var sType = XmlUtils.GetAttributeValue(xn, "type");
					var sChildId = XmlUtils.GetAttributeValue(xn, "id");
					if (sType == "category" && !String.IsNullOrEmpty(sChildId))
						LoadCategoryNode(xn, sChildId, id);
				}
			}
		}
		#endregion // Constructors and other initialization methods

		#region String matching, merging, extracting, etc.
		/// <summary>
		/// Merge in a form that may need to have morphtype markers stripped from it.
		/// </summary>
		private void MergeInAllomorphForms(LiftMultiText forms, ITsMultiString tsm,
			int clsidForm, Guid guidEntry, int flid)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, string> multi;
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				multi = GetAllUnicodeAlternatives(tsm);
			else
				multi = new Dictionary<int, string>();
			foreach (string key in forms.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				string form = forms[key].Text;
				if (wsHvo > 0 && !String.IsNullOrEmpty(form))
				{
					multi.Remove(wsHvo);
					bool fUpdate = false;
					if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld)
					{
						ITsString tssOld = tsm.get_String(wsHvo);
						if (tssOld == null || tssOld.Length == 0)
							fUpdate = true;
					}
					else
					{
						fUpdate = true;
					}
					if (fUpdate)
					{
						string sAllo = StripAlloForm(form, clsidForm, guidEntry, flid);
						tsm.set_String(wsHvo, m_cache.TsStrFactory.MakeString(sAllo, wsHvo));
					}
				}
			}
			foreach (int ws in multi.Keys)
				tsm.set_String(ws, (ITsString)null);
		}

		private string StripAlloForm(string form, int clsidForm, Guid guidEntry, int flid)
		{
			int clsid;
			// Strip any affix/clitic markers from the form before storing it.
			FindMorphType(ref form, out clsid, guidEntry, flid);
			if (clsidForm != 0 && clsid != clsidForm)
			{
				// complain about varying morph types??
			}
			return form;
		}

		/// <summary>
		/// Merge in a Multi(Ts)String type value.
		/// </summary>
		private void MergeInMultiString(ITsMultiString tsm, int flid, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, ITsString> multi;
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				multi = GetAllTsStringAlternatives(tsm);
			else
				multi = new Dictionary<int, ITsString>();
			if (forms != null && forms.Keys != null)
			{
				int cchMax = m_cache.MaxFieldLength(flid);
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						if (!m_fCreatingNewEntry &&
							!m_fCreatingNewSense &&
							m_msImport == MergeStyle.MsKeepOld)
						{
							ITsString tss = tsm.get_String(wsHvo);
							if (tss == null || tss.Length == 0)
							{
								tss = CreateTsStringFromLiftString(forms[key], wsHvo,
									flid, guidObj, cchMax);
								tsm.set_String(wsHvo, tss);
							}
						}
						else
						{
							ITsString tss = CreateTsStringFromLiftString(forms[key], wsHvo,
								flid, guidObj, cchMax);
							tsm.set_String(wsHvo, tss);
						}
					}
				}
			}
			foreach (int ws in multi.Keys)
				tsm.set_String(ws, null);
		}

		private ITsString CreateTsStringFromLiftString(LiftString form, int wsHvo, int flid,
			Guid guidObj, int cchMax)
		{
			ITsString tss = CreateTsStringFromLiftString(form, wsHvo);
			if (tss.Length > cchMax)
			{
				StoreTruncatedDataInfo(tss.Text, cchMax, guidObj, flid, wsHvo);
				ITsStrBldr tsb = tss.GetBldr();
				tsb.Replace(cchMax, tss.Length, null, null);
				tss = tsb.GetString();
			}
			return tss;
		}

		private void StoreTruncatedDataInfo(string sText, int cchMax, Guid guid, int flid, int ws)
		{
			m_rgTruncated.Add(new TruncatedData(sText, cchMax, guid, flid, ws, m_cache, this));
		}

		private ITsString CreateTsStringFromLiftString(LiftString liftstr, int wsHvo)
		{
			ITsStrBldr tsb = m_cache.TsStrFactory.GetBldr();
			tsb.Replace(0, tsb.Length, liftstr.Text, m_tpf.MakeProps(null, wsHvo, 0));
			int wsSpan;
			// TODO: handle nested spans.
			foreach (LiftSpan span in liftstr.Spans)
			{
				ITsPropsBldr tpb = m_tpf.GetPropsBldr();
				if (String.IsNullOrEmpty(span.Lang))
					wsSpan = wsHvo;
				else
					wsSpan = GetWsFromLiftLang(span.Lang);
				tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsSpan);
				if (!String.IsNullOrEmpty(span.Class))
					tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, span.Class);
				if (!String.IsNullOrEmpty(span.LinkURL))
				{
					string linkPath = FileUtils.StripFilePrefix(span.LinkURL);
					char chOdt = Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName);
					string sRef = chOdt.ToString() + linkPath;
					tpb.SetStrPropValue((int)FwTextPropType.ktptObjData, sRef);
				}
				tsb.SetProperties(span.Index, span.Index + span.Length, tpb.GetTextProps());
			}
			return tsb.GetString();
		}

		/// <summary>
		/// Merge in a MultiUnicode type value.
		/// </summary>
		private void MergeInMultiUnicode(ITsMultiString tsm, int flid, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, string> multi;
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				multi = GetAllUnicodeAlternatives(tsm);
			else
				multi = new Dictionary<int, string>();
			if (forms != null && forms.Keys != null)
			{
				int cchMax = m_cache.MaxFieldLength(flid);
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						string sText = forms[key].Text;
						if (sText.Length > cchMax)
						{
							StoreTruncatedDataInfo(sText, cchMax, guidObj, flid, wsHvo);
							sText = sText.Substring(0, cchMax);
						}
						if (!m_fCreatingNewEntry && !m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
						{
							ITsString tss = tsm.get_String(wsHvo);
							if (tss == null || tss.Length == 0)
								tsm.set_String(wsHvo, m_cache.TsStrFactory.MakeString(sText, wsHvo));
						}
						else
						{
							tsm.set_String(wsHvo, m_cache.TsStrFactory.MakeString(sText, wsHvo));
						}
					}
				}
			}
			foreach (int ws in multi.Keys)
				tsm.set_String(ws, null);
		}

		Dictionary<string, int> m_mapLangWs = new Dictionary<string, int>();

		public int GetWsFromLiftLang(string key)
		{
			int hvo;
			if (m_mapLangWs.TryGetValue(key, out hvo))
				return hvo;
			IWritingSystem ws;
			try
			{
				if (!WritingSystemServices.FindOrCreateWritingSystem(m_cache, key, true, true, out ws))
					m_addedWss.Add(ws);
				m_mapLangWs.Add(key, ws.Handle);
				return ws.Handle;
			}
			catch
			{
				// We may have an ICU Locale identifier that's snuck through somehow.
				// Convert it to RFC 5686 and try again.
				string newkey = LangTagUtils.ToLangTag(key);
				if (newkey == key)
					throw;
				if (!WritingSystemServices.FindOrCreateWritingSystem(m_cache, newkey, true, true, out ws))
					m_addedWss.Add(ws);
				m_mapLangWs.Add(key, ws.Handle);
				if (!m_mapLangWs.ContainsKey(newkey))
					m_mapLangWs.Add(newkey, ws.Handle);
				return ws.Handle;
			}
		}

		private void MergeLiftMultiTexts(LiftMultiText mtCurrent, LiftMultiText mtNew)
		{
			foreach (string key in mtNew.Keys)
			{
				if (mtCurrent.ContainsKey(key))
				{
					if (m_fCreatingNewEntry || m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld)
						mtCurrent.Add(key, mtNew[key]);
				}
				else
				{
					mtCurrent.Add(key, mtNew[key]);
				}
			}
		}

		private ITsString GetFirstLiftTsString(LiftMultiText contents)
		{
			if (contents != null && !contents.IsEmpty)
			{
				int ws = GetWsFromLiftLang(contents.FirstValue.Key);
				return CreateTsStringFromLiftString(contents.FirstValue.Value, ws);
			}
			else
			{
				return null;
			}
		}
		public bool TsStringIsNullOrEmpty(ITsString tss)
		{
			return tss == null || tss.Length == 0;
		}

		private bool StringsConflict(string sOld, string sNew)
		{
			if (String.IsNullOrEmpty(sOld))
				return false;
			if (sNew == null)
				return false;
			string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
			string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
			return sNewNorm != sOldNorm;
		}

		private bool StringsConflict(ITsString tssOld, ITsString tssNew)
		{
			if (TsStringIsNullOrEmpty(tssOld))
				return false;
			if (tssNew == null)
				return false;
			ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
			ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
			return !tssOldNorm.Equals(tssNewNorm);
		}

		private bool MultiUnicodeStringsConflict(ITsMultiString tsm, LiftMultiText lmt, bool fStripMarkers,
			Guid guidEntry, int flid)
		{
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return false;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				string sNew = lmt[key].Text;
				if (fStripMarkers)
					sNew = StripAlloForm(sNew, 0, guidEntry, flid);
				ITsString tssOld = tsm.get_String(wsHvo);
				if (tssOld == null || tssOld.Length == 0)
					continue;
				string sOld = tssOld.Text;
				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
				if (sNewNorm != sOldNorm)
					return true;
			}
			return false;
		}

		private bool MultiTsStringsConflict(ITsMultiString tsm, LiftMultiText lmt)
		{
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return false;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				ITsString tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				ITsString tss = tsm.get_String(wsHvo);
				if (tss == null || tss.Length == 0)
					continue;
				ITsString tssOld = tss;
				ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (!tssOldNorm.Equals(tssNewNorm))
					return true;
			}
			return false;
		}

		private int MultiUnicodeStringMatches(ITsMultiString tsm, LiftMultiText lmt, bool fStripMarkers,
			Guid guidEntry, int flid)
		{
			if (tsm == null && (lmt == null || lmt.IsEmpty))
				return 1;
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return 0;
			int cMatches = 0;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				ITsString tssOld = tsm.get_String(wsHvo);
				if (tssOld == null || tssOld.Length == 0)
					continue;
				string sOld = tssOld.Text;
				string sNew = lmt[key].Text;
				if (fStripMarkers)
					sNew = StripAlloForm(sNew, 0, guidEntry, flid);
				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
				if (sNewNorm == sOldNorm)
					++cMatches;
			}
			return cMatches;
		}

		private int MultiTsStringMatches(ITsMultiString tsm, LiftMultiText lmt)
		{
			if (tsm == null && (lmt == null || lmt.IsEmpty))
				return 1;
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return 0;
			int cMatches = 0;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;
				ITsString tss = tsm.get_String(wsHvo);
				if (tss == null || tss.Length == 0)
					continue;
				ITsString tssOld = tss;
				ITsString tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (tssOldNorm.Equals(tssNewNorm))
					++cMatches;
			}
			return cMatches;
		}

		private bool SameMultiUnicodeContent(LiftMultiText contents, ITsMultiString tsm)
		{
			foreach (string key in contents.Keys)
			{
				int ws = GetWsFromLiftLang(key);
				string sNew = contents[key].Text;
				ITsString tssOld = tsm.get_String(ws);
				if (String.IsNullOrEmpty(sNew) && (tssOld == null || tssOld.Length == 0))
					continue;
				if (String.IsNullOrEmpty(sNew) || (tssOld == null || tssOld.Length == 0))
					return false;
				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				string sOldNorm = Icu.Normalize(tssOld.Text, Icu.UNormalizationMode.UNORM_NFD);
				if (sNewNorm != sOldNorm)
					return false;
			}
			// TODO: check whether all strings in mua are found in contents?
			return true;
		}

		/// <summary>
		/// Check whether any of the given unicode values match in any of the writing
		/// systems.
		/// </summary>
		/// <param name="sVal">value to match against</param>
		/// <param name="tsmAbbr">accessor for abbreviation (or null)</param>
		/// <param name="tsmName">accessor for name</param>
		/// <returns></returns>
		private bool HasMatchingUnicodeAlternative(string sVal, ITsMultiString tsmAbbr,
			ITsMultiString tsmName)
		{
			int ws;
			if (tsmAbbr != null)
			{
				for (int i = 0; i < tsmAbbr.StringCount; ++i)
				{
					ITsString tss = tsmAbbr.GetStringFromIndex(i, out ws);
					// TODO: try tss.Text.Equals(sVal, StringComparison.InvariantCultureIgnoreCase)
					if (tss.Length > 0 && tss.Text.ToLowerInvariant() == sVal)
						return true;
				}
			}
			if (tsmName != null)
			{
				for (int i = 0; i < tsmName.StringCount; ++i)
				{
					ITsString tss = tsmName.GetStringFromIndex(i, out ws);
					// TODO: try tss.Text.Equals(sVal, StringComparison.InvariantCultureIgnoreCase)
					if (tss.Length > 0 && tss.Text.ToLowerInvariant() == sVal)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Write the string as HTML, interpreting the string properties as best we can.
		/// </summary>
		/// <param name="tss">The string.</param>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		public string TsStringAsHtml(ITsString tss, FdoCache cache)
		{
			StringBuilder sb = new StringBuilder();
			int crun = tss.RunCount;
			for (int irun = 0; irun < crun; ++irun)
			{
				int iMin = tss.get_MinOfRun(irun);
				int iLim = tss.get_LimOfRun(irun);
				string sLang = null;
				string sDir = null;
				string sFont = null;
				ITsTextProps ttp = tss.get_Properties(irun);
				int nVar;
				int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				if (ws > 0)
				{
					IWritingSystem wsObj = GetExistingWritingSystem(ws);
					sLang = wsObj.Id;
					sDir = wsObj.RightToLeftScript ? "RTL" : "LTR";
					sFont = wsObj.DefaultFontName;
				}
				int nSuperscript = ttp.GetIntPropValues((int)FwTextPropType.ktptSuperscript, out nVar);
				switch (nSuperscript)
				{
					case (int)FwSuperscriptVal.kssvSuper:
						sb.Append("<sup");
						break;
					case (int)FwSuperscriptVal.kssvSub:
						sb.Append("<sub");
						break;
					default:
						sb.Append("<span");
						break;
				}
				if (!String.IsNullOrEmpty(sLang))
					sb.AppendFormat(" lang=\"{0}\"", sLang);
				if (!String.IsNullOrEmpty(sDir))
					sb.AppendFormat(" dir=\"{0}\"", sDir);
				if (!String.IsNullOrEmpty(sFont))
					sb.AppendFormat(" style=\"font-family: '{0}', serif\"", sFont);
				sb.Append(">");
				sb.Append(tss.Text.Substring(iMin, iLim - iMin));
				switch (nSuperscript)
				{
					case (int)FwSuperscriptVal.kssvSuper:
						sb.Append("</sup>");
						break;
					case (int)FwSuperscriptVal.kssvSub:
						sb.Append("</sub>");
						break;
					default:
						sb.Append("</span>");
						break;
				}
			}
			return sb.ToString();
		}
		#endregion // String matching, merging, extracting, etc.

		#region ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample> Members

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.EntryWasDeleted(
			Extensible info, DateTime dateDeleted)
		{
			Guid guid = info.Guid;
			if (guid == Guid.Empty)
				return;
			ICmObject cmo = GetObjectForGuid(guid);
			if (cmo == null)
				return;
			if (cmo is ILexEntry)	// make sure it's a LexEntry!
			{
				// We need to collect the deleted objects' guids so that they won't be
				// reused.  See FWR-3290 for what can happen if we don't do this.
				CollectGuidsFromDeletedEntry(cmo as ILexEntry);
				// TODO: Compare mod times? or our mod time against import's delete time?
				cmo.Delete();
				++m_cEntriesDeleted;
			}
		}

		private void CollectGuidsFromDeletedEntry(ILexEntry le)
		{
			m_deletedGuids.Add(le.Guid);
			if (le.LexemeFormOA != null)
				m_deletedGuids.Add(le.LexemeFormOA.Guid);
			if (le.EtymologyOA != null)
				m_deletedGuids.Add(le.EtymologyOA.Guid);
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
				m_deletedGuids.Add(msa.Guid);
			foreach (var er in le.EntryRefsOS)
				m_deletedGuids.Add(er.Guid);
			foreach (var form in le.AlternateFormsOS)
				m_deletedGuids.Add(form.Guid);
			foreach (var pron in le.PronunciationsOS)
				m_deletedGuids.Add(pron.Guid);
			CollectGuidsFromDeletedSenses(le.SensesOS);
		}

		private void CollectGuidsFromDeletedSenses(IFdoOwningSequence<ILexSense> senses)
		{
			foreach (var ls in senses)
			{
				m_deletedGuids.Add(ls.Guid);
				foreach (var pict in ls.PicturesOS)
					m_deletedGuids.Add(pict.Guid);
				foreach (var ex in ls.ExamplesOS)
					m_deletedGuids.Add(ex.Guid);
				CollectGuidsFromDeletedSenses(ls.SensesOS);
			}
		}

		/// <summary>
		/// This method does all the real work of importing an entry into the lexicon.  Up to
		/// this point, we've just been building up a memory structure based on the LIFT data.
		/// </summary>
		/// <param name="entry"></param>
		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.FinishEntry(
			LiftEntry entry)
		{
			bool fCreateNew = entry.CmObject == null;
			if (!fCreateNew && m_msImport == MergeStyle.MsKeepBoth)
				fCreateNew = EntryHasConflictingData(entry);
			if (fCreateNew)
				CreateNewEntry(entry);
			else
				MergeIntoExistingEntry(entry);
		}

		LiftEntry ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.GetOrMakeEntry(
			Extensible info, int order)
		{
			Guid guid = GetGuidInExtensible(info);
			if (m_fTrustModTimes && SameEntryModTimes(info))
			{
				// If we're keeping only the imported data, remember this was imported!
				if (m_msImport == MergeStyle.MsKeepOnlyNew)
					m_setUnchangedEntry.Add(guid);
				return null;	// assume nothing has changed.
			}
			LiftEntry entry = new LiftEntry(info, guid, order, m_cache, this);
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				m_setChangedEntry.Add(entry.Guid);
			return entry;
		}

		LiftExample ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.GetOrMakeExample(
			LiftSense sense, Extensible info)
		{
			LiftExample example = new LiftExample();
			example.Id = info.Id;
			example.Guid = GetGuidInExtensible(info);
			example.CmObject = GetObjectForGuid(example.Guid);
			example.DateCreated = info.CreationTime;
			example.DateModified = info.ModificationTime;
			sense.Examples.Add(example);
			return example;
		}

		LiftSense ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.GetOrMakeSense(
			LiftEntry entry, Extensible info, string rawXml)
		{
			LiftSense sense = CreateLiftSenseFromInfo(info, entry);
			entry.Senses.Add(sense);
			return sense;
		}

		LiftSense ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.GetOrMakeSubsense(
			LiftSense sense, Extensible info, string rawXml)
		{
			LiftSense sub = CreateLiftSenseFromInfo(info, sense);
			sense.Subsenses.Add(sub);
			return sub;
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInCitationForm(
			LiftEntry entry, LiftMultiText contents)
		{
			if (entry.CitationForm == null)
				entry.CitationForm = contents;
			else
				MergeLiftMultiTexts(entry.CitationForm, contents);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInDefinition(
			LiftSense sense, LiftMultiText contents)
		{
			if (sense.Definition == null)
				sense.Definition = contents;
			else
				MergeLiftMultiTexts(sense.Definition, contents);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInExampleForm(
			LiftExample example, LiftMultiText contents)
		{
			if (example.Content == null)
				example.Content = contents;
			else
				MergeLiftMultiTexts(example.Content, contents);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInField(
			LiftObject extensible, string tagAttribute, DateTime dateCreated, DateTime dateModified,
			LiftMultiText contents, List<Trait> traits)
		{
			LiftField field = new LiftField();
			field.Type = tagAttribute;
			field.DateCreated = dateCreated;
			field.DateModified = dateModified;
			field.Content = contents;
			foreach (Trait t in traits)
			{
				LiftTrait lt = new LiftTrait();
				lt.Name = t.Name;
				lt.Value = t.Value;
				field.Traits.Add(lt);
			}
			extensible.Fields.Add(field);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInGloss(
			LiftSense sense, LiftMultiText contents)
		{
			if (sense.Gloss == null)
				sense.Gloss = contents;
			else
				MergeLiftMultiTexts(sense.Gloss, contents);
		}

		/// <summary>
		/// Only Sense and Reversal have grammatical information.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="val"></param>
		/// <param name="traits"></param>
		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInGrammaticalInfo(
			LiftObject obj, string val, List<Trait> traits)
		{
			LiftGrammaticalInfo graminfo = new LiftGrammaticalInfo();
			graminfo.Value = val;
			foreach (Trait t in traits)
			{
				LiftTrait lt = new LiftTrait();
				lt.Name = t.Name;
				lt.Value = t.Value;
				graminfo.Traits.Add(lt);
			}
			if (obj is LiftSense)
				(obj as LiftSense).GramInfo = graminfo;
			else if (obj is LiftReversal)
				(obj as LiftReversal).GramInfo = graminfo;
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInLexemeForm(
			LiftEntry entry, LiftMultiText contents)
		{
			if (entry.LexicalForm == null)
				entry.LexicalForm = contents;
			else
				MergeLiftMultiTexts(entry.LexicalForm, contents);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInNote(
			LiftObject extensible, string type, LiftMultiText contents, string rawXml)
		{
			LiftNote note = new LiftNote(type, contents);
			// There may be <trait>, <field>, or <annotation> elements hidden in the
			// raw XML string.  Perhaps these should be arguments, but they aren't.
			FillInExtensibleElementsFromRawXml(note, rawXml);
			if (extensible is LiftEntry)
				(extensible as LiftEntry).Notes.Add(note);
			else if (extensible is LiftSense)
				(extensible as LiftSense).Notes.Add(note);
			else if (extensible is LiftExample)
				(extensible as LiftExample).Notes.Add(note);
			else
				Debug.WriteLine(String.Format(
					"<note type='{1}'> (first content = '{2}') found in bad context: {0}",
					extensible.GetType().Name,
					type,
					GetFirstLiftTsString(contents) == null ? "<null>" : GetFirstLiftTsString(contents).Text));
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInPicture(
			LiftSense sense, string href, LiftMultiText caption)
		{
			LiftUrlRef pict = new LiftUrlRef();
			pict.Url = href;
			pict.Label = caption;
			sense.Illustrations.Add(pict);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInMedia(
			LiftObject obj, string href, LiftMultiText caption)
		{
			LiftPhonetic phon = obj as LiftPhonetic;
			if (phon != null)
			{
				LiftUrlRef url = new LiftUrlRef();
				url.Url = href;
				url.Label = caption;
				phon.Media.Add(url);
			}
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInRelation(
			LiftObject extensible, string relationTypeName, string targetId, string rawXml)
		{
			LiftRelation rel = new LiftRelation();
			rel.Type = relationTypeName;
			rel.Ref = targetId.Normalize();	// I've seen data with this as NFD!
			// order should be an argument of this method, but since it isn't (yet),
			// calculate the order whenever it appears to be relevant.
			// There may also be <trait>, <field>, or <annotation> elements hidden in the
			// raw XML string.  These should also be arguments, but aren't.
			FillInExtensibleElementsFromRawXml(rel, rawXml.Normalize());
			if (extensible is LiftEntry)
				(extensible as LiftEntry).Relations.Add(rel);
			else if (extensible is LiftSense)
				(extensible as LiftSense).Relations.Add(rel);
			else if (extensible is LiftVariant)
				(extensible as LiftVariant).Relations.Add(rel);
			else
				Debug.WriteLine(String.Format("<relation type='{0}' ref='{1}> found in bad context: {2}",
					relationTypeName, targetId, extensible.GetType().Name));
		}

		private static void FillInExtensibleElementsFromRawXml(LiftObject obj, string rawXml)
		{
			if (rawXml.IndexOf("<trait") > 0 ||
				rawXml.IndexOf("<field") > 0 ||
				rawXml.IndexOf("<annotation") > 0 ||
				(obj is LiftRelation && rawXml.IndexOf("order=") > 0))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.LoadXml(rawXml);
				XmlNode node = xdoc.FirstChild;
				LiftRelation rel = obj as LiftRelation;
				if (rel != null)
				{
					string sOrder = XmlUtils.GetAttributeValue(node, "order", null);
					if (!String.IsNullOrEmpty(sOrder))
					{
						int order;
						if (Int32.TryParse(sOrder, out order))
							rel.Order = order;
						else
							rel.Order = 0;
					}
				}
				foreach (XmlNode xn in node.SelectNodes("field"))
				{
					LiftField field = CreateLiftFieldFromXml(xn);
					obj.Fields.Add(field);
				}
				foreach (XmlNode xn in node.SelectNodes("trait"))
				{
					LiftTrait trait = CreateLiftTraitFromXml(xn);
					obj.Traits.Add(trait);
				}
				foreach (XmlNode xn in node.SelectNodes("annotation"))
				{
					LiftAnnotation ann = CreateLiftAnnotationFromXml(xn);
					obj.Annotations.Add(ann);
				}
			}
		}

		/// <summary>
		/// Adapted from LiftParser.ReadExtensibleElementDetails()
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftField CreateLiftFieldFromXml(XmlNode node)
		{
			string fieldType = XmlUtils.GetManditoryAttributeValue(node, "type");
			string priorFieldWithSameTag = String.Format("preceding-sibling::field[@type='{0}']", fieldType);
			if (node.SelectSingleNode(priorFieldWithSameTag) != null)
			{
				// a fatal error
				throw new LiftFormatException(String.Format("Field with same type ({0}) as sibling not allowed. Context:{1}", fieldType, node.ParentNode.OuterXml));
			}
			LiftField field = new LiftField();
			field.Type = fieldType;
			field.DateCreated = GetOptionalDateTime(node, "dateCreated");
			field.DateModified = GetOptionalDateTime(node, "dateModified");
			field.Content = CreateLiftMultiTextFromXml(node);
			foreach (XmlNode xn in node.SelectNodes("trait"))
			{
				LiftTrait trait = CreateLiftTraitFromXml(xn);
				field.Traits.Add(trait);
			}
			foreach (XmlNode xn in node.SelectNodes("annotation"))
			{
				LiftAnnotation ann = CreateLiftAnnotationFromXml(xn);
				field.Annotations.Add(ann);
			}
			return field;
		}

		/// <summary>
		/// Adapted from LiftParser.ReadFormNodes()
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftMultiText CreateLiftMultiTextFromXml(XmlNode node)
		{
			LiftMultiText text = new LiftMultiText();
			foreach (XmlNode xnForm in node.SelectNodes("form"))
			{
				try
				{
					string lang = XmlUtils.GetAttributeValue(xnForm, "lang");
					XmlNode xnText = xnForm.SelectSingleNode("text");
					if (xnText != null)
					{
						// Add the separator if we need it.
						if (xnText.InnerText.Length > 0)
							text.AddOrAppend(lang, "", "; ");
						foreach (XmlNode xn in xnText.ChildNodes)
						{
							if (xn.Name == "span")
							{
								text.AddSpan(lang,
											 XmlUtils.GetOptionalAttributeValue(xn, "lang"),
											 XmlUtils.GetOptionalAttributeValue(xn, "class"),
											 XmlUtils.GetOptionalAttributeValue(xn, "href"),
											 xn.InnerText.Length);
							}
							text.AddOrAppend(lang, xn.InnerText, "");
						}
					}
					// Skip annotations for now...
				}
				catch (Exception)
				{
				}
			}
			return text;
		}

		/// <summary>
		/// Adapted from LiftParser.GetTrait()
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftTrait CreateLiftTraitFromXml(XmlNode node)
		{
			LiftTrait trait = new LiftTrait();
			trait.Name = XmlUtils.GetAttributeValue(node, "name");
			trait.Value = XmlUtils.GetAttributeValue(node, "value");
			foreach (XmlNode n in node.SelectNodes("annotation"))
			{
				LiftAnnotation ann = CreateLiftAnnotationFromXml(n);
				trait.Annotations.Add(ann);
			}
			return trait;
		}

		/// <summary>
		/// Adapted from LiftParser.GetAnnotation()
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftAnnotation CreateLiftAnnotationFromXml(XmlNode node)
		{
			LiftAnnotation ann = new LiftAnnotation();
			ann.Name = XmlUtils.GetOptionalAttributeValue(node, "name");
			ann.Value = XmlUtils.GetOptionalAttributeValue(node, "value");
			ann.When = GetOptionalDateTime(node, "when");
			ann.Who = XmlUtils.GetOptionalAttributeValue(node, "who");
			ann.Comment = CreateLiftMultiTextFromXml(node);
			return ann;
		}

		/// <summary>
		/// Adapted from LiftParser.GetOptionalDate()
		/// </summary>
		/// <param name="node"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		private static DateTime GetOptionalDateTime(XmlNode node, string tag)
		{
			string sWhen = XmlUtils.GetAttributeValue(node, tag);
			if (String.IsNullOrEmpty(sWhen))
			{
				return default(DateTime);
			}
			else
			{
				try
				{
					return Extensible.ParseDateTimeCorrectly(sWhen);
				}
				catch (FormatException)
				{
					return default(DateTime);
				}
			}
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInSource(
			LiftExample example, string source)
		{
			example.Source = source;
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInTrait(
			LiftObject extensible, Trait trait)
		{
			LiftTrait lt = new LiftTrait();
			lt.Value = trait.Value;
			lt.Name = trait.Name;
			foreach (Annotation t in trait.Annotations)
			{
				LiftAnnotation ann = new LiftAnnotation();
				ann.Name = t.Name;
				ann.Value = t.Value;
				ann.When = t.When;
				ann.Who = t.Who;
				lt.Annotations.Add(ann);
			}
			extensible.Traits.Add(lt);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInTranslationForm(
			LiftExample example, string type, LiftMultiText contents, string rawXml)
		{
			LiftTranslation trans = new LiftTranslation();
			trans.Type = type;
			trans.Content = contents;
			example.Translations.Add(trans);
		}

		LiftObject ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInPronunciation(
			LiftEntry entry, LiftMultiText contents, string rawXml)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			LiftPhonetic phon = new LiftPhonetic();
			phon.Form = contents;
			entry.Pronunciations.Add(phon);
			return phon;
		}

		LiftObject ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInVariant(
			LiftEntry entry, LiftMultiText contents, string rawXml)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			LiftVariant var = new LiftVariant();
			var.Form = contents;
			// LiftIO handles "extensible", but not <pronunciation> or <relation>, so store the
			// raw XML for now.
			var.RawXml = rawXml;
			entry.Variants.Add(var);
			return var;
		}

		LiftObject ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.GetOrMakeParentReversal(
			LiftObject parent, LiftMultiText contents, string type)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			LiftReversal rev = new LiftReversal();
			rev.Type = type;
			rev.Form = contents;
			rev.Main = parent as LiftReversal;
			return rev;
		}

		LiftObject ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInReversal(
			LiftSense sense, LiftObject parent, LiftMultiText contents, string type, string rawXml)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			LiftReversal rev = new LiftReversal();
			rev.Type = type;
			rev.Form = contents;
			rev.Main = parent as LiftReversal;
			sense.Reversals.Add(rev);
			return rev;
		}

		LiftObject ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInEtymology(
			LiftEntry entry, string source, string type, LiftMultiText form, LiftMultiText gloss,
			string rawXml)
		{
			LiftEtymology ety = new LiftEtymology();
			ety.Source = source;
			ety.Type = type;
			ety.Form = form;
			ety.Gloss = gloss;
			entry.Etymologies.Add(ety);
			return ety;
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.ProcessRangeElement(
			string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			string rawXml)
		{
			switch (range)
			{
				case "dialect":		// translate into writing systems
					VerifyOrCreateWritingSystem(id, label, abbrev, description);
					break;
				case "etymology":	// I think we can ignore these.
					break;
				case "lexical-relations":	// original FLEX export
				case "lexical-relation":	// lexical relation types (?)
					// TODO: Handle these here instead of where they're encountered in processing!
					break;
				case "note-type":	// I think we can ignore these.
					break;
				case "paradigm":	// I think we can ignore these.
					break;
				case "semanticdomainddp4":	// for WeSay 0.4 compatibility
				case "semantic_domain":
				case "semantic-domain":
				case "semantic-domain-ddp4":	// initialize map, adding to existing list if needed.
					ProcessSemanticDomain(id, guidAttr, parent, description, label, abbrev);
					break;
				case "anthro_codes":	// original FLEX export
				case "anthro-code":		// initialize map, adding to existing list if needed.
					ProcessAnthroItem(id, guidAttr, parent, description, label, abbrev);
					break;
				case "status":
					ProcessPossibility(id, guidAttr, parent, description, label, abbrev,
						m_dictStatus, m_rgnewStatus, m_cache.LangProject.StatusOA);
					break;
				case "users":
					break;
				case "translation-types":	// original FLEX export
				case "translation-type":
					ProcessPossibility(id, guidAttr, parent, description, label, abbrev,
						m_dictTransType, m_rgnewTransType, m_cache.LangProject.TranslationTagsOA);
					break;
				case "grammatical-info":	// map onto parts of speech?  extend as needed.
				case "FromPartOfSpeech":	// original FLEX export
				case "from-part-of-speech":	// map onto parts of speech?  extend as needed.
					ProcessPartOfSpeech(id, guidAttr, parent, description, label, abbrev);
					break;
				case "MorphType":	// original FLEX export
				case "morph-type":
					ProcessMorphType(id, guidAttr, parent, description, label, abbrev);
					break;
				case "exception-feature":
					EnsureProdRestrictListExists();
					ProcessPossibility(id, guidAttr, parent, description, label, abbrev,
						m_dictExceptFeats, m_rgnewExceptFeat, m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA);
					break;
				case "inflection-feature":
					if (m_cache.LangProject.MsFeatureSystemOA == null)
					{
						IFsFeatureSystemFactory fact = m_cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>();
						m_cache.LangProject.MsFeatureSystemOA = fact.Create();
					}
					ProcessFeatureDefinition(id, guidAttr, parent, description, label, abbrev, rawXml,
						m_cache.LangProject.MsFeatureSystemOA);
					break;
				case "inflection-feature-type":
					if (m_cache.LangProject.MsFeatureSystemOA == null)
					{
						IFsFeatureSystemFactory fact = m_cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>();
						m_cache.LangProject.MsFeatureSystemOA = fact.Create();
					}
					ProcessFeatureStrucType(id, guidAttr, parent, description, label, abbrev, rawXml,
						m_cache.LangProject.MsFeatureSystemOA);
					break;
				default:
					if (range.EndsWith("-slot") || range.EndsWith("-Slots"))
						ProcessSlotDefinition(range, id, guidAttr, parent, description, label, abbrev);
					else if (range.EndsWith("-infl-class") || range.EndsWith("-InflClasses"))
						ProcessInflectionClassDefinition(range, id, guidAttr, parent, description, label, abbrev);
					else if (range.EndsWith("-feature-value"))
						ProcessFeatureValue(range, id, guidAttr, parent, description, label, abbrev, rawXml);
					else if (range.EndsWith("-stem-name"))
						ProcessStemName(range, id, guidAttr, parent, description, label, abbrev, rawXml);
#if DEBUG
					else
						Debug.WriteLine(String.Format("Unknown range '{0}' has element '{1}'", range, id));
#endif
						break;
			}
		}

		private void EnsureProdRestrictListExists()
		{
			if (m_cache.LangProject.MorphologicalDataOA == null)
			{
				IMoMorphDataFactory fact = m_cache.ServiceLocator.GetInstance<IMoMorphDataFactory>();
				m_cache.LangProject.MorphologicalDataOA = fact.Create();
			}
			if (m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA == null)
			{
				ICmPossibilityListFactory fact = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
				m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA = fact.Create();
			}
		}

		private void ProcessFeatureDefinition(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml,
			IFsFeatureSystem featSystem)
		{
			if (m_factFsComplexFeature == null)
				m_factFsComplexFeature = m_cache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>();
			if (m_factFsOpenFeature == null)
				m_factFsOpenFeature = m_cache.ServiceLocator.GetInstance<IFsOpenFeatureFactory>();
			if (m_factFsClosedFeature == null)
				m_factFsClosedFeature = m_cache.ServiceLocator.GetInstance <IFsClosedFeatureFactory>();
			if (m_repoFsFeatDefn == null)
				m_repoFsFeatDefn = m_cache.ServiceLocator.GetInstance<IFsFeatDefnRepository>();
			FillFeatureMapsIfNeeded();
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			XmlNodeList fields = xdoc.FirstChild.SelectNodes("field");
			string sCatalogId = null;
			bool fDisplayToRight = false;
			bool fShowInGloss = false;
			string sSubclassType = null;
			string sComplexType = null;
			int nWsSelector = 0;
			string sWs = null;
			List<string> rgsValues = new List<string>();
			XmlNode xnGlossAbbrev = null;
			XmlNode xnRightGlossSep = null;
			foreach (XmlNode xn in traits)
			{
				string name = XmlUtils.GetAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "display-to-right":
						fDisplayToRight = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "show-in-gloss":
						fShowInGloss = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "feature-definition-type":
						sSubclassType = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "type":
						sComplexType = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "ws-selector":
						nWsSelector = XmlUtils.GetMandatoryIntegerAttributeValue(xn, "value");
						break;
					case "writing-system":
						sWs = XmlUtils.GetAttributeValue(xn, "value");
						break;
					default:
						if (name.EndsWith("-feature-value"))
						{
							string sVal = XmlUtils.GetAttributeValue(xn, "value");
							if (!String.IsNullOrEmpty(sVal))
								rgsValues.Add(sVal);
						}
						break;
				}
			}
			foreach (XmlNode xn in fields)
			{
				string type = XmlUtils.GetAttributeValue(xn, "type");
				switch (type)
				{
					case "gloss-abbrev":
						xnGlossAbbrev = xn;
						break;
					case "right-gloss-sep":
						xnRightGlossSep = xn;
						break;
				}
			}
			IFsFeatDefn feat;
			bool fNew = false;
			if (m_mapIdFeatDefn.TryGetValue(id, out feat))
				feat = ValidateFeatDefnType(sSubclassType, feat);
			if (feat == null)
			{
				feat = CreateDesiredFeatDefn(sSubclassType, feat);
				if (feat == null)
					return;
				m_rgnewFeatDefn.Add(feat);
				m_mapIdFeatDefn[id] = feat;
				fNew = true;
			}
			MergeInMultiUnicode(feat.Abbreviation, FsFeatDefnTags.kflidAbbreviation, abbrev, feat.Guid);
			MergeInMultiUnicode(feat.Name, FsFeatDefnTags.kflidName, label, feat.Guid);
			MergeInMultiString(feat.Description, FsFeatDefnTags.kflidDescription, description, feat.Guid);
			if (fNew || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!String.IsNullOrEmpty(sCatalogId))
					feat.CatalogSourceId = sCatalogId;
				if (fDisplayToRight)
					feat.DisplayToRightOfValues = fDisplayToRight;
				if (fShowInGloss)
					feat.ShowInGloss = fShowInGloss;
			}
			if (xnGlossAbbrev != null)
				MergeInMultiUnicode(feat.GlossAbbreviation, xnGlossAbbrev);
			if (xnRightGlossSep != null)
				MergeInMultiUnicode(feat.RightGlossSep, xnRightGlossSep);
			switch (sSubclassType)
			{
				case "complex":
					FinishMergingComplexFeatDefn(feat as IFsComplexFeature, sComplexType);
					break;
				case "open":
					FinishMergingOpenFeatDefn(feat as IFsOpenFeature, nWsSelector, sWs);
					break;
				case "closed":
					FinishMergingClosedFeatDefn(feat as IFsClosedFeature, rgsValues, id);
					break;
			}
			Guid guid = ConvertStringToGuid(guidAttr);
			if (guid != Guid.Empty)
				m_mapLiftGuidFeatDefn.Add(guid, feat);
		}

		/// <summary>
		/// Either set the Type for the complex feature, or remember it to set later after the
		/// appropriate type has been defined.
		/// </summary>
		private void FinishMergingComplexFeatDefn(IFsComplexFeature featComplex, string sComplexType)
		{
			if (featComplex.TypeRA == null ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				IFsFeatStrucType featType;
				if (m_mapIdFeatStrucType.TryGetValue(sComplexType, out featType))
				{
					featComplex.TypeRA = featType;
				}
				else
				{
					m_mapComplexFeatMissingTypeAbbr.Add(featComplex, sComplexType);
				}
			}
		}

		/// <summary>
		/// Set the WsSelector and WritingSystem for the open feature.
		/// </summary>
		private void FinishMergingOpenFeatDefn(IFsOpenFeature featOpen, int nWsSelector, string sWs)
		{
			if (featOpen.WsSelector == 0 ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (nWsSelector != 0)
					featOpen.WsSelector = nWsSelector;
			}
			if (string.IsNullOrEmpty(featOpen.WritingSystem) ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!string.IsNullOrEmpty(sWs))
					featOpen.WritingSystem = sWs;
			}
		}

		/// <summary>
		/// Either set the Values for the closed feature, or remember them to set later after
		/// they have been defined.
		/// </summary>
		private void FinishMergingClosedFeatDefn(IFsClosedFeature featClosed, List<string> rgsValues,
			string id)
		{
			if (featClosed.ValuesOC.Count == 0 ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				IFsSymFeatVal featValue;
				List<string> rgsMissing = new List<string>(rgsValues.Count);
				foreach (string sAbbr in rgsValues)
				{
					string key = String.Format("{0}:{1}", id, sAbbr);
					if (m_mapIdAbbrSymFeatVal.TryGetValue(key, out featValue))
					{
						featClosed.ValuesOC.Add(featValue);
						continue;
					}
					else if (m_rgPendingSymFeatVal.Count > 0)
					{
						PendingFeatureValue val = null;
						foreach (PendingFeatureValue pfv in m_rgPendingSymFeatVal)
						{
							if (pfv.FeatureId == id && pfv.Id == sAbbr)
							{
								val = pfv;
								break;
							}
						}
						if (val != null)
						{
							StoreSymFeatValInClosedFeature(val.Id, val.Description, val.Label,
								val.Abbrev, val.CatalogId, val.ShowInGloss, featClosed, val.FeatureId);
							m_rgPendingSymFeatVal.Remove(val);
							continue;
						}
					}
					rgsMissing.Add(sAbbr);
				}
				if (rgsMissing.Count > 0)
					m_mapClosedFeatMissingValueAbbrs.Add(featClosed, rgsMissing);
			}
		}

		private void MergeInMultiUnicode(IMultiUnicode mu, XmlNode xnField)
		{
			int ws = 0;
			string val = null;
			foreach (XmlNode xn in xnField.SelectNodes("form"))
			{
				string sLang = XmlUtils.GetManditoryAttributeValue(xn, "lang");
				ws = GetWsFromLiftLang(sLang);
				XmlNode xnText = xnField.SelectSingleNode("text");
				if (xnText != null)
				{
					val = xnText.InnerText;
					if (!String.IsNullOrEmpty(val))
					{
						ITsString tssOld = mu.get_String(ws);
						if (tssOld.Length == 0 || m_msImport != MergeStyle.MsKeepOld)
							mu.set_String(ws, val);
					}
				}
			}
		}

		private IFsFeatDefn CreateDesiredFeatDefn(string sSubclassType, IFsFeatDefn feat)
		{
			switch (sSubclassType)
			{
				case "complex":
					feat = m_factFsComplexFeature.Create();
					break;
				case "open":
					feat = m_factFsOpenFeature.Create();
					break;
				case "closed":
					feat = m_factFsClosedFeature.Create();
					break;
				default:
					feat = null;
					break;
			}
			if (feat != null)
				m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(feat);
			return feat;
		}

		private static IFsFeatDefn ValidateFeatDefnType(string sSubclassType, IFsFeatDefn feat)
		{
			if (feat != null)
			{
				switch (sSubclassType)
				{
					case "complex":
						if (feat.ClassID != FsComplexFeatureTags.kClassId)
							feat = null;
						break;
					case "open":
						if (feat.ClassID != FsOpenFeatureTags.kClassId)
							feat = null;
						break;
					case "closed":
						if (feat.ClassID != FsClosedFeatureTags.kClassId)
							feat = null;
						break;
					default:
						feat = null;
						break;
				}
			}
			return feat;
		}

		private static Guid ConvertStringToGuid(string guidAttr)
		{
			if (!String.IsNullOrEmpty(guidAttr))
			{
				try
				{
					return new Guid(guidAttr);
				}
				catch
				{
				}
			}
			return Guid.Empty;
		}

		private void ProcessFeatureStrucType(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml,
			IFsFeatureSystem featSystem)
		{
			if (m_factFsFeatStrucType == null)
				m_factFsFeatStrucType = m_cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>();
			if (m_repoFsFeatStrucType == null)
				m_repoFsFeatStrucType = m_cache.ServiceLocator.GetInstance<IFsFeatStrucTypeRepository>();
			FillFeatureMapsIfNeeded();
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			string sCatalogId = null;
			List<string> rgsFeatures = new List<string>();
			foreach (XmlNode xn in traits)
			{
				string name = XmlUtils.GetAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetManditoryAttributeValue(xn, "value");
						break;
					case "feature":
						rgsFeatures.Add(XmlUtils.GetManditoryAttributeValue(xn, "value"));
						break;
				}
			}
			IFsFeatStrucType featType = null;
			if (!m_mapIdFeatStrucType.TryGetValue(id, out featType))
			{
				featType = m_factFsFeatStrucType.Create();
				m_cache.LangProject.MsFeatureSystemOA.TypesOC.Add(featType);
				m_mapIdFeatStrucType.Add(id, featType);
				m_rgnewFeatStrucType.Add(featType);
			}
			Guid guid = ConvertStringToGuid(guidAttr);
			MergeInMultiUnicode(featType.Abbreviation, FsFeatDefnTags.kflidAbbreviation, abbrev, featType.Guid);
			MergeInMultiUnicode(featType.Name, FsFeatDefnTags.kflidName, label, featType.Guid);
			MergeInMultiString(featType.Description, FsFeatDefnTags.kflidDescription, description, featType.Guid);
			if (featType.CatalogSourceId == null ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!String.IsNullOrEmpty(sCatalogId))
					featType.CatalogSourceId = sCatalogId;
			}
			if (featType.FeaturesRS.Count == 0 ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				IFsFeatDefn feat;
				featType.FeaturesRS.Clear();
				foreach (string sVal in rgsFeatures)
				{
					if (m_mapIdFeatDefn.TryGetValue(sVal, out feat))
						featType.FeaturesRS.Add(feat);
				}
				if (rgsFeatures.Count != featType.FeaturesRS.Count)
				{
					featType.FeaturesRS.Clear();
					m_mapFeatStrucTypeMissingFeatureAbbrs.Add(featType, rgsFeatures);
				}
			}
			// Now try to link up with missing type references.  Note that more than one complex
			// feature may be linked to the same type.
			List<IFsComplexFeature> rgfeatHandled = new List<IFsComplexFeature>();
			foreach (KeyValuePair<IFsComplexFeature, string> kv in m_mapComplexFeatMissingTypeAbbr)
			{
				if (kv.Value == id)
				{
					rgfeatHandled.Add(kv.Key);
					break;
				}
			}
			foreach (IFsComplexFeature feat in rgfeatHandled)
			{
				feat.TypeRA = featType;
				m_mapComplexFeatMissingTypeAbbr.Remove(feat);
			}
		}

		private void ProcessFeatureValue(string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml)
		{
			if (m_repoFsFeatDefn == null)
				m_repoFsFeatDefn = m_cache.ServiceLocator.GetInstance<IFsFeatDefnRepository>();
			if (m_factFsSymFeatVal == null)
				m_factFsSymFeatVal = m_cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>();
			if (m_repoFsSymFeatVal == null)
				m_repoFsSymFeatVal = m_cache.ServiceLocator.GetInstance<IFsSymFeatValRepository>();
			FillFeatureMapsIfNeeded();
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			string sCatalogId = null;
			bool fShowInGloss = false;
			foreach (XmlNode xn in traits)
			{
				string name = XmlUtils.GetAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "show-in-gloss":
						fShowInGloss = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
				}
			}
			string sFeatId = null;
			int idxSuffix = range.IndexOf("-feature-value");
			if (idxSuffix > 0)
				sFeatId = range.Substring(0, idxSuffix);
			Guid guid = ConvertStringToGuid(guidAttr);
			IFsClosedFeature featClosed = FindRelevantClosedFeature(sFeatId, id, guid);
			if (featClosed == null)
			{
				// Save the information for later in hopes something comes up.
				PendingFeatureValue pfv = new PendingFeatureValue(sFeatId, id, description,
					label, abbrev, sCatalogId, fShowInGloss, guid);
				m_rgPendingSymFeatVal.Add(pfv);
				return;
			}
			StoreSymFeatValInClosedFeature(id, description, label, abbrev,
				sCatalogId, fShowInGloss, featClosed, sFeatId);
		}

		private void StoreSymFeatValInClosedFeature(string id, LiftMultiText description,
			LiftMultiText label, LiftMultiText abbrev, string sCatalogId, bool fShowInGloss,
			IFsClosedFeature featClosed, string featId)
		{
			bool fNew = false;
			IFsSymFeatVal val = FindMatchingFeatValue(featClosed, id);
			if (val == null)
			{
				val = m_factFsSymFeatVal.Create();
				featClosed.ValuesOC.Add(val);
				fNew = true;
			}
			MergeInMultiUnicode(val.Abbreviation, FsSymFeatValTags.kflidAbbreviation, abbrev, val.Guid);
			MergeInMultiUnicode(val.Name, FsSymFeatValTags.kflidName, label, val.Guid);
			MergeInMultiString(val.Description, FsSymFeatValTags.kflidDescription, description, val.Guid);
			if (fNew || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!String.IsNullOrEmpty(sCatalogId))
					val.CatalogSourceId = sCatalogId;
				if (fShowInGloss)
					val.ShowInGloss = fShowInGloss;
			}
			// update the map to find this later.
			string key = String.Format("{0}:{1}", featId, id);
			m_mapIdAbbrSymFeatVal[key] = val;
		}

		private IFsSymFeatVal FindMatchingFeatValue(IFsClosedFeature featClosed, string id)
		{
			IFsSymFeatVal val = null;
			foreach (IFsSymFeatVal sfv in featClosed.ValuesOC)
			{
				for (int i = 0; i < sfv.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tss = sfv.Abbreviation.GetStringFromIndex(i, out ws);
					if (tss.Text == id)
						return sfv;
				}
			}
			return val;
		}

		private IFsClosedFeature FindRelevantClosedFeature(string sFeatId, string id, Guid guid)
		{
			IFsClosedFeature featClosed = null;
			if (guid != Guid.Empty)
			{
				IFsFeatDefn feat;
				if (m_mapLiftGuidFeatDefn.TryGetValue(guid, out feat))
					featClosed = feat as IFsClosedFeature;
			}
			if (featClosed == null && !String.IsNullOrEmpty(sFeatId))
			{
				IFsFeatDefn feat;
				if (m_mapIdFeatDefn.TryGetValue(sFeatId, out feat))
					featClosed = feat as IFsClosedFeature;
			}
			if (featClosed == null)
			{
				foreach (KeyValuePair<IFsClosedFeature, List<string>> kv in m_mapClosedFeatMissingValueAbbrs)
				{
					if (kv.Value.Contains(id))
					{
						kv.Value.Remove(id);
						if (kv.Value.Count == 0)
							m_mapClosedFeatMissingValueAbbrs.Remove(kv.Key);
						return kv.Key;
					}
				}
			}
			return featClosed;
		}

		private void FillFeatureMapsIfNeeded()
		{
			if (m_mapIdFeatDefn.Count == 0 && m_mapIdFeatStrucType.Count == 0 && m_mapIdAbbrSymFeatVal.Count == 0)
			{
				FillIdFeatDefnMap();
				FillIdFeatStrucTypeMap();
				FillIdAbbrSymFeatValMap();
			}
		}

		private void FillIdFeatDefnMap()
		{
			foreach (IFsFeatDefn feat in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				for (int i = 0; i < feat.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tssAbbr = feat.Abbreviation.GetStringFromIndex(i, out ws);
					string sAbbr = tssAbbr.Text;
					if (!String.IsNullOrEmpty(sAbbr) && !m_mapIdFeatDefn.ContainsKey(sAbbr))
						m_mapIdFeatDefn.Add(sAbbr, feat);
				}
			}
		}

		private void FillIdFeatStrucTypeMap()
		{
			foreach (IFsFeatStrucType featType in m_cache.LangProject.MsFeatureSystemOA.TypesOC)
			{
				for (int i = 0; i < featType.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tssAbbr = featType.Abbreviation.GetStringFromIndex(i, out ws);
					string sAbbr = tssAbbr.Text;
					if (!String.IsNullOrEmpty(sAbbr) && !m_mapIdFeatStrucType.ContainsKey(sAbbr))
						m_mapIdFeatStrucType.Add(sAbbr, featType);
				}
			}
		}

		private void FillIdAbbrSymFeatValMap()
		{
			foreach (IFsFeatDefn feat in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				IFsClosedFeature featClosed = feat as IFsClosedFeature;
				if (featClosed != null)
				{
					Set<string> setIds = new Set<string>();
					for (int i = 0; i < featClosed.Abbreviation.StringCount; ++i)
					{
						int ws;
						ITsString tssAbbr = featClosed.Abbreviation.GetStringFromIndex(i, out ws);
						string sAbbr = tssAbbr.Text;
						if (!String.IsNullOrEmpty(sAbbr))
							setIds.Add(sAbbr);
					}
					foreach (IFsSymFeatVal featVal in featClosed.ValuesOC)
					{
						for (int i = 0; i < featVal.Abbreviation.StringCount; ++i)
						{
							int ws;
							ITsString tssAbbr = featVal.Abbreviation.GetStringFromIndex(i, out ws);
							string sAbbr = tssAbbr.Text;
							if (!String.IsNullOrEmpty(sAbbr))
							{
								foreach (string sId in setIds)
								{
									string key = String.Format("{0}:{1}", sId, sAbbr);
									if (!m_mapIdAbbrSymFeatVal.ContainsKey(key))
										m_mapIdAbbrSymFeatVal.Add(key, featVal);
								}
							}
						}
					}
				}
			}
		}

		private void ProcessStemName(string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml)
		{
			var idx = range.LastIndexOf("-stem-name");
			if (idx <= 0)
				return;
			string sPosName = range.Substring(0, idx);
			ICmPossibility poss;
			if (!m_dictPos.TryGetValue(sPosName, out poss))
				return;
			IPartOfSpeech pos = poss as IPartOfSpeech;
			if (pos == null)
				return;
			IMoStemName stem;
			var key = String.Format("{0}:{1}", sPosName, id);
			if (!m_dictStemName.TryGetValue(key, out stem))
			{
				stem = m_cache.ServiceLocator.GetInstance<IMoStemNameFactory>().Create();
				pos.StemNamesOC.Add(stem);
				m_dictStemName.Add(key, stem);
				m_rgnewStemName.Add(stem);
			}
			MergeInMultiUnicode(stem.Abbreviation, MoStemNameTags.kflidAbbreviation, abbrev, stem.Guid);
			MergeInMultiUnicode(stem.Name, MoStemNameTags.kflidName, label, stem.Guid);
			MergeInMultiString(stem.Description, MoStemNameTags.kflidDescription, description, stem.Guid);

			HashSet<string> setFeats = new HashSet<string>();
			foreach (IFsFeatStruc ffs in stem.RegionsOC)
				setFeats.Add(ffs.LiftName);
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			foreach (XmlNode xn in traits)
			{
				var name = XmlUtils.GetAttributeValue(xn, "name");
				if (name == "feature-set")
				{
					var value = XmlUtils.GetAttributeValue(xn, "value");
					if (setFeats.Contains(value))
						continue;
					IFsFeatStruc ffs = ParseFeatureString(value, stem);
					if (ffs == null)
						continue;
					setFeats.Add(value);
					var liftName = ffs.LiftName;
					if (liftName != value)
						setFeats.Add(liftName);
				}
			}
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.ProcessFieldDefinition(
			string tag, LiftMultiText description)
		{
			// We may need this information later, but don't do anything for now except save it.
			m_dictFieldDef.Add(tag, description);
		}

		#endregion // ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample> Members

		#region Storing LIFT import residue...
		private XmlDocument FindOrCreateResidue(ICmObject cmo, string sId, int flid)
		{
			LiftResidue res;
			if (!m_dictResidue.TryGetValue(cmo.Hvo, out res))
			{
				res = CreateLiftResidue(cmo.Hvo, flid, sId);
				m_dictResidue.Add(cmo.Hvo, res);
			}
			else if (!String.IsNullOrEmpty(sId))
			{
				EnsureIdSet(res.Document.FirstChild, sId);
			}
			return res.Document;
		}

		/// <summary>
		/// This creates a new LiftResidue object with an empty XML document (empty except for
		/// the enclosing &lt;lift-residue&gt; element, that is).
		/// As a side-effect, it moves any existing LIFT residue for LexEntry or LexSense from
		/// ImportResidue to LiftResidue.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="sId"></param>
		/// <returns></returns>
		private LiftResidue CreateLiftResidue(int hvo, int flid, string sId)
		{
			string sResidue = null;
			// The next four lines move any existing LIFT residue from ImportResidue to LiftResidue.
			if (flid == LexEntryTags.kflidLiftResidue)
				ExtractLIFTResidue(m_cache, hvo, LexEntryTags.kflidImportResidue, flid);
			else if (flid == LexSenseTags.kflidLiftResidue)
				ExtractLIFTResidue(m_cache, hvo, LexSenseTags.kflidImportResidue, flid);
			if (String.IsNullOrEmpty(sId))
				sResidue = "<lift-residue></lift-residue>";
			else
				sResidue = String.Format("<lift-residue id=\"{0}\"></lift-residue>", XmlUtils.MakeSafeXmlAttribute(sId));
			XmlDocument xd = new XmlDocument();
			xd.PreserveWhitespace = true;
			xd.LoadXml(sResidue);
			return new LiftResidue(flid, xd);
		}

		private void EnsureIdSet(XmlNode xn, string sId)
		{
			XmlAttribute xa = xn.Attributes["id"];
			if (xa == null)
			{
				xa = xn.OwnerDocument.CreateAttribute("id");
				xa.Value = XmlUtils.MakeSafeXmlAttribute(sId);
				xn.Attributes.Append(xa);
			}
			else if (String.IsNullOrEmpty(xa.Value))
			{
				xa.Value = XmlUtils.MakeSafeXmlAttribute(sId);
			}
		}

		/// <summary>
		/// Scan ImportResidue for XML looking string inserted by LIFT import.  If any is found,
		/// move it from ImportResidue to LiftResidue.
		/// </summary>
		/// <returns>string containing any LIFT import residue found in ImportResidue</returns>
		private  static string ExtractLIFTResidue(FdoCache cache, int hvo, int flidImportResidue,
			int flidLiftResidue)
		{
			Debug.Assert(flidLiftResidue != 0);
			ITsString tssImportResidue = cache.MainCacheAccessor.get_StringProp(hvo, flidImportResidue);
			string sImportResidue = tssImportResidue == null ? null : tssImportResidue.Text;
			if (String.IsNullOrEmpty(sImportResidue))
				return null;
			if (sImportResidue.Length < 13)
				return null;
			int idx = sImportResidue.IndexOf("<lift-residue");
			if (idx >= 0)
			{
				string sLiftResidue = sImportResidue.Substring(idx);
				int idx2 = sLiftResidue.IndexOf("</lift-residue>");
				if (idx2 >= 0)
				{
					idx2 += 15;
					if (sLiftResidue.Length > idx2)
						sLiftResidue = sLiftResidue.Substring(0, idx2);
				}
				int cch = sLiftResidue.Length;
				cache.MainCacheAccessor.set_UnicodeProp(hvo, flidImportResidue, sImportResidue.Remove(idx, cch));
				cache.MainCacheAccessor.set_UnicodeProp(hvo, flidLiftResidue, sLiftResidue);
				return sLiftResidue;
			}
			else
			{
				return null;
			}
		}

		private void StoreFieldAsResidue(ICmObject extensible, LiftField field)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForField(field);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <field type='{1}'>",
					extensible.GetType().Name, field.Type));
			}
		}

		private XmlDocument FindOrCreateResidue(ICmObject extensible)
		{
			// chaining if..else if instead of switch deals easier with matching superclasses.
			if (extensible is ILexEntry)
				return FindOrCreateResidue(extensible, null, LexEntryTags.kflidLiftResidue);
			else if (extensible is ILexSense)
				return FindOrCreateResidue(extensible, null, LexSenseTags.kflidLiftResidue);
			else if (extensible is ILexEtymology)
				return FindOrCreateResidue(extensible, null, LexEtymologyTags.kflidLiftResidue);
			else if (extensible is ILexExampleSentence)
				return FindOrCreateResidue(extensible, null, LexExampleSentenceTags.kflidLiftResidue);
			else if (extensible is ILexPronunciation)
				return FindOrCreateResidue(extensible, null, LexPronunciationTags.kflidLiftResidue);
			else if (extensible is ILexReference)
				return FindOrCreateResidue(extensible, null, LexReferenceTags.kflidLiftResidue);
			else if (extensible is IMoForm)
				return FindOrCreateResidue(extensible, null, MoFormTags.kflidLiftResidue);
			else if (extensible is IMoMorphSynAnalysis)
				return FindOrCreateResidue(extensible, null, MoMorphSynAnalysisTags.kflidLiftResidue);
			else
				return null;
		}

		private string CreateXmlForField(LiftField field)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<field type=\"{0}\"", field.Type);
			AppendXmlDateAttributes(bldr, field.DateCreated, field.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, field.Content, "form");
			foreach (LiftTrait trait in field.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			bldr.AppendLine("</field>");
			return bldr.ToString();
		}

		private void AppendXmlForMultiText(StringBuilder bldr, LiftMultiText content, string tagXml)
		{
			if (content == null)
				return;		// probably shouldn't happen in a fully functional system, but...
			foreach (string lang in content.Keys)
			{
				LiftString str = content[lang];
				bldr.AppendFormat("<{0} lang=\"{1}\"><text>", tagXml, lang);
				int idxPrev = 0;
				foreach (LiftSpan span in str.Spans)
				{
					if (idxPrev < span.Index)
						bldr.Append(XmlUtils.MakeSafeXml(str.Text.Substring(idxPrev, span.Index - idxPrev)));
					// TODO: handle nested spans.
					bool fSpan = AppendSpanElementIfNeeded(bldr, span, lang);
					bldr.Append(XmlUtils.MakeSafeXml(str.Text.Substring(span.Index, span.Length)));
					if (fSpan)
						bldr.Append("</span>");
					idxPrev = span.Index + span.Length;
				}
				if (idxPrev < str.Text.Length)
					bldr.Append(XmlUtils.MakeSafeXml(str.Text.Substring(idxPrev, str.Text.Length - idxPrev)));
				bldr.AppendFormat("</text></{0}>", tagXml);
				bldr.AppendLine();
			}
		}

		private bool AppendSpanElementIfNeeded(StringBuilder bldr, LiftSpan span, string lang)
		{
			bool fSpan = false;
			if (!String.IsNullOrEmpty(span.Class))
			{
				bldr.AppendFormat("<span class=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.Class));
				fSpan = true;
			}
			if (!String.IsNullOrEmpty(span.LinkURL))
			{
				if (!fSpan)
				{
					bldr.Append("<span");
					fSpan = true;
				}
				bldr.AppendFormat(" href=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.LinkURL));
			}
			if (!String.IsNullOrEmpty(span.Lang) && span.Lang != lang)
			{
				if (!fSpan)
				{
					bldr.Append("<span");
					fSpan = true;
				}
				bldr.AppendFormat(" lang=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.Lang));
			}
			if (fSpan)
				bldr.Append(">");
			return fSpan;
		}

		private void StoreNoteAsResidue(ICmObject extensible, LiftNote note)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForNote(note);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <note type='{1}'>",
					extensible.GetType().Name, note.Type));
			}
		}

		private string CreateXmlForNote(LiftNote note)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.Append("<note");
			if (!String.IsNullOrEmpty(note.Type))
				bldr.AppendFormat(" type=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(note.Type));
			AppendXmlDateAttributes(bldr, note.DateCreated, note.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, note.Content, "form");
			foreach (LiftField field in note.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in note.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			bldr.AppendLine("</note>");
			return bldr.ToString();
		}

		private string CreateXmlForTrait(LiftTrait trait)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<trait name=\"{0}\" value=\"{1}\"",
				XmlUtils.MakeSafeXmlAttribute(trait.Name),
				XmlUtils.MakeSafeXmlAttribute(trait.Value));
			if (!String.IsNullOrEmpty(trait.Id))
				bldr.AppendFormat(" id=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(trait.Id));
			if (trait.Annotations != null && trait.Annotations.Count > 0)
			{
				bldr.AppendLine(">");
				foreach (LiftAnnotation ann in trait.Annotations)
					bldr.Append(CreateXmlForAnnotation(ann));
				bldr.AppendLine("</trait>");
			}
			else
			{
				bldr.AppendLine("/>");
			}
			return bldr.ToString();
		}

		private string CreateXmlForAnnotation(LiftAnnotation ann)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<annotation name=\"{0}\" value=\"{1}\"",
				XmlUtils.MakeSafeXmlAttribute(ann.Name),
				XmlUtils.MakeSafeXmlAttribute(ann.Value));
			if (!String.IsNullOrEmpty(ann.Who))
				bldr.AppendFormat(" who=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(ann.Who));
			DateTime when = ann.When;
			if (IsDateSet(when))
				bldr.AppendFormat(" when=\"{0}\"", when.ToUniversalTime().ToString(LiftDateTimeFormat));
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, ann.Comment, "form");
			bldr.AppendLine("</annotation>");
			return bldr.ToString();
		}

		private string CreateXmlForPhonetic(LiftPhonetic phon)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.Append("<pronunciation");
			AppendXmlDateAttributes(bldr, phon.DateCreated, phon.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, phon.Form, "form");
			foreach (LiftUrlRef url in phon.Media)
				bldr.Append(CreateXmlForUrlRef(url, "media"));
			foreach (LiftField field in phon.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in phon.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			foreach (LiftAnnotation ann in phon.Annotations)
				bldr.Append(CreateXmlForAnnotation(ann));
			bldr.AppendLine("</pronunciation>");
			return bldr.ToString();
		}

		private void AppendXmlDateAttributes(StringBuilder bldr, DateTime created, DateTime modified)
		{
			if (IsDateSet(created))
				bldr.AppendFormat(" dateCreated=\"{0}\"", created.ToUniversalTime().ToString(LiftDateTimeFormat));
			if (IsDateSet(modified))
				bldr.AppendFormat(" dateModified=\"{0}\"", modified.ToUniversalTime().ToString(LiftDateTimeFormat));
		}

		private string CreateXmlForUrlRef(LiftUrlRef url, string tag)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<{0} href=\"{1}\">", tag, url.Url);
			bldr.AppendLine();
			AppendXmlForMultiText(bldr, url.Label, "form");
			bldr.AppendFormat("</{0}>", tag);
			bldr.AppendLine();
			return bldr.ToString();
		}

		private string CreateXmlForRelation(LiftRelation rel)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<relation type=\"{0}\" ref=\"{1}\"", rel.Type, rel.Ref);
			if (rel.Order >= 0)
				bldr.AppendFormat(" order=\"{0}\"", rel.Order);
			AppendXmlDateAttributes(bldr, rel.DateCreated, rel.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, rel.Usage, "usage");
			foreach (LiftField field in rel.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in rel.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			foreach (LiftAnnotation ann in rel.Annotations)
				bldr.Append(CreateXmlForAnnotation(ann));
			bldr.AppendLine("</relation>");
			return bldr.ToString();
		}

		private string CreateRelationResidue(LiftRelation rel)
		{
			if (rel.Usage != null || rel.Fields.Count > 0 || rel.Traits.Count > 0 ||
				rel.Annotations.Count > 0)
			{
				StringBuilder bldr = new StringBuilder();
				AppendXmlForMultiText(bldr, rel.Usage, "usage");
				foreach (LiftField field in rel.Fields)
					bldr.Append(CreateXmlForField(field));
				foreach (LiftTrait trait in rel.Traits)
					bldr.Append(CreateXmlForTrait(trait));
				foreach (LiftAnnotation ann in rel.Annotations)
					bldr.Append(CreateXmlForAnnotation(ann));
				return bldr.ToString();
			}
			else
			{
				return null;
			}
		}

		private void StoreTraitAsResidue(ICmObject extensible, LiftTrait trait)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForTrait(trait);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <trait name='{1}' value='{2}'>",
					extensible.GetType().Name, trait.Name, trait.Value));
			}
		}

		private void StoreResidue(ICmObject extensible, List<string> rgsResidue)
		{
			if (rgsResidue.Count == 0)
				return;
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				foreach (string sXml in rgsResidue)
					InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: {1}...",
					extensible.GetType().Name, rgsResidue[0]));
			}
		}

		private void StoreResidue(ICmObject extensible, string sResidueXml)
		{
			if (String.IsNullOrEmpty(sResidueXml))
				return;
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				InsertResidueContent(xdResidue, sResidueXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: {1}",
					extensible.GetType().Name, sResidueXml));
			}
		}

		private void StoreResidueFromVariant(ICmObject extensible, LiftVariant var)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				// traits have already been handled.
				InsertResidueAttribute(xdResidue, "ref", var.Ref);
				StoreDatesInResidue(extensible, var);
				foreach (LiftAnnotation ann in var.Annotations)
				{
					string sXml = CreateXmlForAnnotation(ann);
					InsertResidueContent(xdResidue, sXml);
				}
				foreach (LiftPhonetic phon in var.Pronunciations)
				{
					string sXml = CreateXmlForPhonetic(phon);
					InsertResidueContent(xdResidue, sXml);
				}
				foreach (LiftRelation rel in var.Relations)
				{
					string sXml = CreateXmlForRelation(rel);
					InsertResidueContent(xdResidue, sXml);
				}
				if (!String.IsNullOrEmpty(var.RawXml) &&
					String.IsNullOrEmpty(var.Ref) &&
					var.Pronunciations.Count == 0 &&
					var.Relations.Count == 0)
				{
					XmlDocument xdoc = new XmlDocument();
					xdoc.PreserveWhitespace = true;
					xdoc.LoadXml(var.RawXml);
					string sRef = XmlUtils.GetOptionalAttributeValue(xdoc.FirstChild, "ref");
					InsertResidueAttribute(xdResidue, "ref", sRef);
					foreach (XmlNode node in xdoc.FirstChild.SelectNodes("pronunciation"))
						InsertResidueContent(xdResidue, node.OuterXml + Environment.NewLine);
					foreach (XmlNode node in xdoc.FirstChild.SelectNodes("relation"))
						InsertResidueContent(xdResidue, node.OuterXml + Environment.NewLine);
				}
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <variant...>",
					extensible.GetType().Name));
			}
		}

		private void StoreEtymologyAsResidue(ICmObject extensible, LiftEtymology ety)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForEtymology(ety);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <etymology...>",
					extensible.GetType().Name));
			}
		}

		private string CreateXmlForEtymology(LiftEtymology ety)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<etymology source=\"{0}\" type=\"{1}\"", ety.Source, ety.Type);
			AppendXmlDateAttributes(bldr, ety.DateCreated, ety.DateModified);
			bldr.AppendLine(">");
			Debug.Assert(ety.Form.Count < 2);
			AppendXmlForMultiText(bldr, ety.Form, "form");
			AppendXmlForMultiText(bldr, ety.Gloss, "gloss");
			foreach (LiftField field in ety.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in ety.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			foreach (LiftAnnotation ann in ety.Annotations)
				bldr.Append(CreateXmlForAnnotation(ann));
			bldr.AppendLine("</etymology>");
			return bldr.ToString();
		}

		private void StoreDatesInResidue(ICmObject extensible, LiftObject obj)
		{
			if (IsDateSet(obj.DateCreated) || IsDateSet(obj.DateModified))
			{
				XmlDocument xdResidue = FindOrCreateResidue(extensible);
				if (xdResidue != null)
				{
					InsertResidueDate(xdResidue, "dateCreated", obj.DateCreated);
					InsertResidueDate(xdResidue, "dateModified", obj.DateModified);
				}
				else
				{
					Debug.WriteLine(String.Format("Need LiftResidue for {0}: <etymology...>",
						extensible.GetType().Name));
				}
			}
		}

		private void InsertResidueAttribute(XmlDocument xdResidue, string sName, string sValue)
		{
			if (!String.IsNullOrEmpty(sValue))
			{
				XmlAttribute xa = xdResidue.FirstChild.Attributes[sName];
				if (xa == null)
				{
					xa = xdResidue.CreateAttribute(sName);
					xdResidue.FirstChild.Attributes.Append(xa);
				}
				xa.Value = sValue;
			}
		}

		private void InsertResidueDate(XmlDocument xdResidue, string sAttrName, DateTime dt)
		{
			if (IsDateSet(dt))
			{
				InsertResidueAttribute(xdResidue, sAttrName,
					dt.ToUniversalTime().ToString(LiftDateTimeFormat));
			}
		}

		private static void InsertResidueContent(XmlDocument xdResidue, string sXml)
		{
			XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.None);
			using (XmlReader reader = new XmlTextReader(sXml, XmlNodeType.Element, context))
			{
				XmlNode xn = xdResidue.ReadNode(reader);
				if (xn != null)
				{
					xdResidue.FirstChild.AppendChild(xn);
					xn = xdResidue.ReadNode(reader); // add trailing newline
					if (xn != null)
						xdResidue.FirstChild.AppendChild(xn);
				}
			}
		}

		public bool IsDateSet(DateTime dt)
		{
			return dt != default(DateTime) && dt != m_defaultDateTime;
		}

		private void StoreAnnotationsAndDatesInResidue(ICmObject extensible, LiftObject obj)
		{
			// unknown fields and traits have already been stored as residue.
			if (obj.Annotations.Count > 0 || IsDateSet(obj.DateCreated) || IsDateSet(obj.DateModified))
			{
				XmlDocument xdResidue = FindOrCreateResidue(extensible);
				if (xdResidue != null)
				{
					StoreDatesInResidue(extensible, obj);
					foreach (LiftAnnotation ann in obj.Annotations)
					{
						string sXml = CreateXmlForAnnotation(ann);
						InsertResidueContent(xdResidue, sXml);
					}
				}
				else
				{
					Debug.WriteLine(String.Format("Need LiftResidue for {0}: <{1}...>",
						extensible.GetType().Name), obj.XmlTag);
				}
			}
		}

		private void StoreExampleResidue(ICmObject extensible, LiftExample expl)
		{
			// unknown notes have already been stored as residue.
			if (expl.Fields.Count + expl.Traits.Count + expl.Annotations.Count == 0)
				return;
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				StoreDatesInResidue(extensible, expl);
				//foreach (LiftField field in expl.Fields)
				//{
				//    string sXml = CreateXmlForField(field);
				//    InsertResidueContent(xdResidue, sXml);
				//}
				foreach (LiftTrait trait in expl.Traits)
				{
					string sXml = CreateXmlForTrait(trait);
					InsertResidueContent(xdResidue, sXml);
				}
				foreach (LiftAnnotation ann in expl.Annotations)
				{
					string sXml = CreateXmlForAnnotation(ann);
					InsertResidueContent(xdResidue, sXml);
				}
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <example...>",
					extensible.GetType().Name));
			}
		}
		#endregion // Storing LIFT import residue...

		#region Methods for processing LIFT header elements
		private int FindOrCreateCustomField(string sLabel, LiftMultiText lmtDesc, int clid)
		{
			var sClass = m_cache.MetaDataCacheAccessor.GetClassName(clid);
			var sTag = String.Format("{0}-{1}", sClass, sLabel);
			var flid = 0;
			if (m_dictCustomFlid.TryGetValue(sTag, out flid))
				return flid;
			var sDesc = String.Empty;
			string sSpec = null;
			if (lmtDesc != null)
			{
				LiftString lstr;
				if (lmtDesc.TryGetValue("en", out lstr))
					sDesc = lstr.Text;
				if (lmtDesc.TryGetValue("x-spec", out lstr))
					sSpec = lstr.Text;
				if (String.IsNullOrEmpty(sSpec) && !String.IsNullOrEmpty(sDesc) && sDesc.StartsWith("Type=kcpt"))
				{
					sSpec = sDesc;
					sDesc = String.Empty;
				}
			}
			var type = CellarPropertyType.MultiBigString;
			var wsSelector = WritingSystemServices.kwsAnalVerns;
			var clidDst = 0;
			if (!String.IsNullOrEmpty(sSpec))
			{
				string sDstCls;
				var rgsDef = sSpec.Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
				type = GetCustomFieldType(rgsDef);

				if (type == CellarPropertyType.Nil)
					type = CellarPropertyType.MultiBigString;
				wsSelector = GetCustomFieldWsSelector(rgsDef);
				clidDst = GetCustomFieldDstCls(rgsDef, out sDstCls);
			}
			foreach (var fd in FieldDescription.FieldDescriptors(m_cache))
			{
				if (fd.Custom != 0 && fd.Userlabel == sLabel && fd.Class == clid)
				{
					var fOk = CheckForCompatibleTypes(type, fd);
					if (!fOk)
					{
						// log error.
						return 0;
					}
					m_dictCustomFlid.Add(sTag, fd.Id);
					return fd.Id;			// field with same label and type information exists already.
				}
			}
			switch (type)
			{
				case CellarPropertyType.Boolean:
				case CellarPropertyType.Integer:
				case CellarPropertyType.Numeric:
				case CellarPropertyType.Float:
				case CellarPropertyType.Time:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Image:
				case CellarPropertyType.GenDate:
				case CellarPropertyType.Binary:
					clidDst = -1;
					break;
				case CellarPropertyType.String:
				case CellarPropertyType.Unicode:
				case CellarPropertyType.BigString:
				case CellarPropertyType.BigUnicode:
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiBigString:
				case CellarPropertyType.MultiBigUnicode:
					if (wsSelector == 0)
						wsSelector = WritingSystemServices.kwsAnalVerns;		// we need a WsSelector value!
					clidDst = -1;
					break;
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceSequence:
					break;
				default:
					type = CellarPropertyType.MultiBigString;
					if (wsSelector == 0)
						wsSelector = WritingSystemServices.kwsAnalVerns;
					clidDst = -1;
					break;
			}
			var fdNew = new FieldDescription(m_cache)
							{
								Type = type,
								Class = clid,
								Name = sLabel,
								Userlabel = sLabel,
								HelpString = sDesc,
								WsSelector = wsSelector,
								DstCls = clidDst
							};
			fdNew.UpdateCustomField();
			m_dictCustomFlid.Add(sTag, fdNew.Id);
			m_rgnewCustomFields.Add(fdNew);
			return fdNew.Id;
		}

		private static bool CheckForCompatibleTypes(CellarPropertyType type, FieldDescription fd)
		{
			if (fd.Type == type)
				return true;
			if (fd.Type == CellarPropertyType.MultiString && type == CellarPropertyType.MultiBigString)
				return true;
			if (fd.Type == CellarPropertyType.MultiBigString &&	type == CellarPropertyType.MultiString)
				return true;
			if (fd.Type == CellarPropertyType.MultiUnicode && type == CellarPropertyType.MultiBigUnicode)
				return true;
			if (fd.Type == CellarPropertyType.MultiBigUnicode && type == CellarPropertyType.MultiUnicode)
				return true;
			if (fd.Type == CellarPropertyType.String && type == CellarPropertyType.BigString)
				return true;
			if (fd.Type == CellarPropertyType.BigString && type == CellarPropertyType.String)
				return true;
			if (fd.Type == CellarPropertyType.Unicode && type == CellarPropertyType.BigUnicode)
				return true;
			if (fd.Type == CellarPropertyType.BigUnicode && type == CellarPropertyType.Unicode)
				return true;
			if (fd.Type == CellarPropertyType.Binary && type == CellarPropertyType.Image)
				return true;
			if (fd.Type == CellarPropertyType.Image && type == CellarPropertyType.Binary)
				return true;
			if (fd.Type == CellarPropertyType.OwningCollection && type == CellarPropertyType.OwningSequence)
				return true;
			if (fd.Type == CellarPropertyType.OwningSequence && type == CellarPropertyType.OwningCollection)
				return true;
			if (fd.Type == CellarPropertyType.ReferenceCollection && type == CellarPropertyType.ReferenceSequence)
				return true;
			if (fd.Type == CellarPropertyType.ReferenceSequence && type == CellarPropertyType.ReferenceCollection)
				return true;
			return false;
		}

		private CellarPropertyType GetCustomFieldType(string[] rgsDef)
		{
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("Type="))
				{
					var sValue = sDef.Substring(5);
					if (sValue.StartsWith("kcpt"))
						sValue = sValue.Substring(4);
					return (CellarPropertyType)Enum.Parse(typeof(CellarPropertyType), sValue, true);
				}
			}
			return CellarPropertyType.Nil;
		}

		private int GetCustomFieldWsSelector(string[] rgsDef)
		{
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("WsSelector="))
				{
					string sValue = sDef.Substring(11);
					int ws = WritingSystemServices.GetMagicWsIdFromName(sValue);
					if (ws == 0)
						ws = GetWsFromStr(sValue);
					return ws;
				}
			}
			return 0;
		}

		private int GetCustomFieldDstCls(string[] rgsDef, out string sValue)
		{
			sValue = null;
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("DstCls="))
				{
					sValue = sDef.Substring(7);
					return (int)m_cache.MetaDataCacheAccessor.GetClassId(sValue);
				}
			}
			return 0;
		}

		private void ProcessAnthroItem(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			int hvo = FindAbbevOrLabelInDict(abbrev, label, m_dictAnthroCode);
			if (hvo <= 0)
			{
				ICmObject caiParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictAnthroCode.ContainsKey(parent))
					caiParent = m_dictAnthroCode[parent];
				else
					caiParent = m_cache.LangProject.AnthroListOA;
				ICmAnthroItem cai = CreateNewCmAnthroItem(guidAttr, caiParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, cai);
				m_dictAnthroCode[id] = cai;
				m_rgnewAnthroCode.Add(cai);
			}
		}

		private static int FindAbbevOrLabelInDict(LiftMultiText abbrev, LiftMultiText label,
			Dictionary<string, ICmPossibility> dict)
		{
			if (abbrev != null && abbrev.Keys != null)
			{
				foreach (string key in abbrev.Keys)
				{
					if (dict.ContainsKey(abbrev[key].Text))
						return dict[abbrev[key].Text].Hvo;
				}
			}
			if (label != null && label.Keys != null)
			{
				foreach (string key in label.Keys)
				{
					if (dict.ContainsKey(label[key].Text))
						return dict[label[key].Text].Hvo;
				}
			}
			return 0;
		}

		private void ProcessSemanticDomain(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			ICmPossibility poss = GetPossibilityForGuidIfExisting(id, guidAttr, m_dictSemDom);
			if (poss == null)
			{
				ICmObject csdParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictSemDom.ContainsKey(parent))
					csdParent = m_dictSemDom[parent];
				else
					csdParent = m_cache.LangProject.SemanticDomainListOA;
				ICmSemanticDomain csd = CreateNewCmSemanticDomain(guidAttr, csdParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, csd);
				m_dictSemDom[id] = csd;
				m_rgnewSemDom.Add(csd);
			}
		}

		private void ProcessPossibility(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, ICmPossibility> dict, List<ICmPossibility> rgNew, ICmPossibilityList list)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (poss == null)
			{
				ICmObject possParent = null;
				if (!String.IsNullOrEmpty(parent) && dict.ContainsKey(parent))
					possParent = dict[parent];
				else
					possParent = list;
				poss = CreateNewCmPossibility(guidAttr, possParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
				dict[id] = poss;
				rgNew.Add(poss);
			}
		}

		private void SetNewPossibilityAttributes(string id, LiftMultiText description, LiftMultiText label,
			LiftMultiText abbrev, ICmPossibility poss)
		{
			if (label.Count > 0)
				MergeInMultiUnicode(poss.Name, CmPossibilityTags.kflidName, label, poss.Guid);
			else
				poss.Name.AnalysisDefaultWritingSystem = m_cache.TsStrFactory.MakeString(id, m_cache.DefaultAnalWs);
			MergeInMultiUnicode(poss.Abbreviation, CmPossibilityTags.kflidAbbreviation, abbrev, poss.Guid);
			MergeInMultiString(poss.Description, CmPossibilityTags.kflidDescription, description, poss.Guid);
		}

		private void ProcessPartOfSpeech(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictPos,
				m_cache.LangProject.PartsOfSpeechOA);
			if (poss == null)
			{
				ICmObject posParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictPos.ContainsKey(parent))
					posParent = m_dictPos[parent];
				else
					posParent = m_cache.LangProject.PartsOfSpeechOA;
				IPartOfSpeech pos = CreateNewPartOfSpeech(guidAttr, posParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, pos);
				m_dictPos[id] = pos;
				// Try to find this in the category catalog list, so we can add in more information.
				EticCategory cat = FindMatchingEticCategory(label);
				if (cat != null)
					AddEticCategoryInfo(cat, pos);
				m_rgnewPos.Add(pos);
			}
		}

		private void AddEticCategoryInfo(EticCategory cat, IPartOfSpeech pos)
		{
			if (cat != null)
			{
				pos.CatalogSourceId = cat.Id;
				foreach (string lang in cat.MultilingualName.Keys)
				{
					int ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						ITsString tssName = pos.Name.get_String(ws);
						if (tssName == null || tssName.Length == 0)
							pos.Name.set_String(ws, cat.MultilingualName[lang]);
					}
				}
				foreach (string lang in cat.MultilingualAbbrev.Keys)
				{
					int ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						ITsString tssAbbrev = pos.Abbreviation.get_String(ws);
						if (tssAbbrev == null || tssAbbrev.Length == 0)
							pos.Abbreviation.set_String(ws, cat.MultilingualAbbrev[lang]);
					}
				}
				foreach (string lang in cat.MultilingualDesc.Keys)
				{
					int ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						ITsString tss = pos.Description.get_String(ws);
						if (tss == null || tss.Length == 0)
							pos.Description.set_String(ws, cat.MultilingualDesc[lang]);
					}
				}
			}
		}

		private EticCategory FindMatchingEticCategory(LiftMultiText label)
		{
			foreach (EticCategory cat in m_rgcats)
			{
				int cMatch = 0;
				int cDiffer = 0;
				foreach (string lang in label.Keys)
				{
					string sName = label[lang].Text;
					string sCatName;
					if (cat.MultilingualName.TryGetValue(lang, out sCatName))
					{
						if (sName.ToLowerInvariant() == sCatName.ToLowerInvariant())
							++cMatch;
						else
							++cDiffer;
					}
				}
				if (cMatch > 0 && cDiffer == 0)
					return cat;
			}
			return null;
		}

		private void ProcessMorphType(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictMmt,
				m_cache.LangProject.LexDbOA.MorphTypesOA);
			if (poss == null)
			{
				ICmObject mmtParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictPos.ContainsKey(parent))
					mmtParent = m_dictMmt[parent];
				else
					mmtParent = m_cache.LangProject.LexDbOA.MorphTypesOA;
				IMoMorphType mmt = CreateNewMoMorphType(guidAttr, mmtParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, mmt);
				m_dictMmt[id] = mmt;
				m_rgnewMmt.Add(mmt);
			}
		}

		private ICmPossibility FindExistingPossibility(string id, string guidAttr, LiftMultiText label,
			LiftMultiText abbrev, Dictionary<string, ICmPossibility> dict, ICmPossibilityList list)
		{
			ICmPossibility poss = GetPossibilityForGuidIfExisting(id, guidAttr, dict);
			if (poss == null)
			{
				poss = FindMatchingPossibility(list.PossibilitiesOS, label, abbrev);
				if (poss != null)
				{
					dict[id] = poss;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return poss;
		}

		private ICmPossibility GetPossibilityForGuidIfExisting(string id, string guidAttr, Dictionary<string, ICmPossibility> dict)
		{
			ICmPossibility poss = null;
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				ICmObject cmo = GetObjectForGuid(guid);
				if (cmo != null && cmo is ICmPossibility)
				{
					poss = cmo as ICmPossibility;
					dict[id] = poss;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return poss;
		}

		ICmPossibility FindMatchingPossibility(IFdoOwningSequence<ICmPossibility> possibilities,
			LiftMultiText label, LiftMultiText abbrev)
		{
			foreach (ICmPossibility item in possibilities)
			{
				if (HasMatchingUnicodeAlternative(item.Name, label) &&
					HasMatchingUnicodeAlternative(item.Abbreviation, abbrev))
				{
					return item;
				}
				ICmPossibility poss = FindMatchingPossibility(item.SubPossibilitiesOS, label, abbrev);
				if (poss != null)
					return poss;
			}
			return null;
		}

		private bool HasMatchingUnicodeAlternative(ITsMultiString tsm, LiftMultiText text)
		{
			if (text != null && text.Keys != null)
			{
				foreach (string key in text.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					string sValue = text[key].Text;
					ITsString tssAlt = tsm.get_String(wsHvo);
					if (String.IsNullOrEmpty(sValue) || (tssAlt == null || tssAlt.Length == 0))
						continue;
					if (sValue.ToLowerInvariant() == tssAlt.Text.ToLowerInvariant())
						return true;
				}
				return false;
			}
			return true;		// no data at all -- assume match (!!??)
		}


		private void VerifyOrCreateWritingSystem(string id, LiftMultiText label,
			LiftMultiText abbrev, LiftMultiText description)
		{
			// This finds or creates a writing system for the given key.
			int handle = GetWsFromLiftLang(id);
			Debug.Assert(handle >= 1);
			IWritingSystem ws = GetExistingWritingSystem(handle);

			if (m_msImport != MergeStyle.MsKeepOld || string.IsNullOrEmpty(ws.Abbreviation))
			{
				if (abbrev.Count > 0)
					ws.Abbreviation = abbrev.FirstValue.Value.Text;
			}
			LanguageSubtag languageSubtag = ws.LanguageSubtag;
			if (m_msImport != MergeStyle.MsKeepOld || string.IsNullOrEmpty(languageSubtag.Name))
			{
				if (label.Count > 0)
					ws.LanguageSubtag = new LanguageSubtag(languageSubtag, label.FirstValue.Value.Text);
			}
		}

		private void ProcessSlotDefinition(string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			int idx = range.IndexOf("-slot");
			if (idx < 0)
				idx = range.IndexOf("-Slots");
			string sOwner = range.Substring(0, idx);
			ICmPossibility owner = null;
			if (m_dictPos.ContainsKey(sOwner))
				owner = m_dictPos[sOwner];
			if (owner == null)
				owner = FindMatchingPossibility(sOwner.ToLowerInvariant(),
					m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			if (owner == null)
				return;
			IPartOfSpeech posOwner = owner as IPartOfSpeech;
			IMoInflAffixSlot slot = null;
			foreach (IMoInflAffixSlot slotT in posOwner.AffixSlotsOC)
			{

				if (HasMatchingUnicodeAlternative(slotT.Name, label))
				{
					slot = slotT;
					break;
				}
			}
			if (slot == null)
			{
				slot = CreateNewMoInflAffixSlot();
				posOwner.AffixSlotsOC.Add(slot);
				MergeInMultiUnicode(slot.Name, MoInflAffixSlotTags.kflidName, label, slot.Guid);
				MergeInMultiString(slot.Description, MoInflAffixSlotTags.kflidDescription, description, slot.Guid);
				m_rgnewSlots.Add(slot);
				// TODO: How to handle "Optional" field.
			}
		}

		private void ProcessInflectionClassDefinition(string range, string id, string guidAttr,
			string sParent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			int idx = range.IndexOf("-infl-class");
			if (idx < 0)
				idx = range.IndexOf("-InflClasses");
			string sOwner = range.Substring(0, idx);
			ICmPossibility owner = null;
			if (m_dictPos.ContainsKey(sOwner))
				owner = m_dictPos[sOwner];
			if (owner == null)
				owner = FindMatchingPossibility(sOwner.ToLowerInvariant(),
					m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			if (owner == null)
				return;
			Dictionary<string, IMoInflClass> dictSlots = null;
			if (!m_dictDictSlots.TryGetValue(sOwner, out dictSlots))
			{
				dictSlots = new Dictionary<string, IMoInflClass>();
				m_dictDictSlots[sOwner] = dictSlots;
			}
			IPartOfSpeech posOwner = owner as IPartOfSpeech;
			IMoInflClass infl = null;
			IMoInflClass inflParent = null;
			if (!String.IsNullOrEmpty(sParent))
			{
				if (dictSlots.ContainsKey(sParent))
				{
					inflParent = dictSlots[sParent];
				}
				else
				{
					inflParent = FindMatchingInflectionClass(sParent, posOwner.InflectionClassesOC, dictSlots);
				}
			}
			else
			{
				foreach (IMoInflClass inflT in posOwner.InflectionClassesOC)
				{
					if (HasMatchingUnicodeAlternative(inflT.Name, label) &&
						HasMatchingUnicodeAlternative(inflT.Abbreviation, abbrev))
					{
						infl = inflT;
						break;
					}
				}
			}
			if (infl == null)
			{
				infl = CreateNewMoInflClass();
				if (inflParent == null)
					posOwner.InflectionClassesOC.Add(infl);
				else
					inflParent.SubclassesOC.Add(infl);
				MergeInMultiUnicode(infl.Abbreviation, MoInflClassTags.kflidAbbreviation, abbrev, infl.Guid);
				MergeInMultiUnicode(infl.Name, MoInflClassTags.kflidName, label, infl.Guid);
				MergeInMultiString(infl.Description, MoInflClassTags.kflidDescription, description, infl.Guid);
				dictSlots[id] = infl;
			}
		}

		private IMoInflClass FindMatchingInflectionClass(string parent,
			IFdoOwningCollection<IMoInflClass> collection, Dictionary<string, IMoInflClass> dict)
		{
			foreach (IMoInflClass infl in collection)
			{
				if (HasMatchingUnicodeAlternative(parent.ToLowerInvariant(), infl.Abbreviation, infl.Name))
				{
					dict[parent] = infl;
					return infl;
				}
				IMoInflClass inflT = FindMatchingInflectionClass(parent, infl.SubclassesOC, dict);
				if (inflT != null)
					return inflT;
			}
			return null;
		}

		private ICmPossibility FindMatchingPossibility(string sVal,
			IFdoOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, ICmPossibility> dict)
		{
			foreach (ICmPossibility poss in possibilities)
			{
				if (HasMatchingUnicodeAlternative(sVal, poss.Abbreviation, poss.Name))
				{
					if (dict != null)
						dict.Add(sVal, poss);
					return poss;
				}
				ICmPossibility possT = FindMatchingPossibility(sVal, poss.SubPossibilitiesOS, dict);
				if (possT != null)
					return possT;
			}
			return null;
		}
		#endregion // Methods for processing LIFT header elements

		#region Process Guids in import data

		/// <summary>
		/// As sense elements often don't have explict guid attributes in LIFT files,
		/// the parser generates new Guid values for them.  We want to always use the
		/// old guid values if we can, so we try to get a guid from the id value if
		/// one exists.  (In fact, WeSay appears to put out only the guid as the id
		/// value.  Flex puts out the default analysis gloss followed by the guid.)
		/// See LT-8840 for what happens if we depend of the Guid value provided by
		/// the parser.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		private LiftSense CreateLiftSenseFromInfo(Extensible info, LiftObject owner)
		{
			Guid guidInfo = info.Guid;
			info.Guid = Guid.Empty;
			Guid guid = GetGuidInExtensible(info);
			if (guid == Guid.Empty)
				guid = guidInfo;
			return new LiftSense(info, guid, m_cache, owner, this);
		}

		private GuidConverter GuidConv
		{
			get { return m_gconv; }
		}

		private Guid GetGuidInExtensible(Extensible info)
		{
			if (info.Guid == Guid.Empty)
			{
				string sGuid = FindGuidInString(info.Id);
				if (!String.IsNullOrEmpty(sGuid))
					return (Guid)GuidConv.ConvertFrom(sGuid);
				else
					return Guid.NewGuid();
			}
			else
			{
				return info.Guid;
			}
		}

		/// <summary>
		/// Find and return a substring like "ebc06013-3cf8-4091-9436-35aa2c4ffc34", or null
		/// if nothing looks like a guid.
		/// </summary>
		/// <param name="sId"></param>
		/// <returns></returns>
		private string FindGuidInString(string sId)
		{
			if (String.IsNullOrEmpty(sId) || sId.Length < 36)
				return null;
			Match matchGuid = m_regexGuid.Match(sId);
			if (matchGuid.Success)
				return sId.Substring(matchGuid.Index, matchGuid.Length);
			else
				return null;
		}

		private ICmObject GetObjectFromTargetIdString(string targetId)
		{
			if (m_mapIdObject.ContainsKey(targetId))
				return m_mapIdObject[targetId];
			string sGuid = FindGuidInString(targetId);
			if (!String.IsNullOrEmpty(sGuid))
			{
				Guid guidTarget = (Guid)GuidConv.ConvertFrom(sGuid);
				return GetObjectForGuid(guidTarget);
			}
			return null;
		}
		#endregion // Process Guids in import data

		#region Methods to find or create list items
		private IPartOfSpeech FindOrCreatePartOfSpeech(string val)
		{
			ICmPossibility poss;
			if (m_dictPos.TryGetValue(val, out poss) ||
				m_dictPos.TryGetValue(val.ToLowerInvariant(), out poss))
			{
				return poss as IPartOfSpeech;
			}
			IPartOfSpeech pos = CreateNewPartOfSpeech();
			m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			// Try to find this in the category catalog list, so we can add in more information.
			EticCategory cat = FindMatchingEticCategory(val);
			if (cat != null)
				AddEticCategoryInfo(cat, pos as IPartOfSpeech);
			if (pos.Name.AnalysisDefaultWritingSystem.Length == 0)
				pos.Name.set_String(m_cache.DefaultAnalWs, val);
			if (pos.Abbreviation.AnalysisDefaultWritingSystem.Length == 0)
				pos.Abbreviation.set_String(m_cache.DefaultAnalWs, val);
			m_dictPos.Add(val, pos);
			m_rgnewPos.Add(pos);
			return pos;
		}

		private EticCategory FindMatchingEticCategory(string val)
		{
			string sVal = val.ToLowerInvariant();
			foreach (EticCategory cat in m_rgcats)
			{
				foreach (string lang in cat.MultilingualName.Keys)
				{
					string sName = cat.MultilingualName[lang];
					if (sName.ToLowerInvariant() == sVal)
						return cat;
				}
				foreach (string lang in cat.MultilingualAbbrev.Keys)
				{
					string sAbbrev = cat.MultilingualAbbrev[lang];
					if (sAbbrev.ToLowerInvariant() == sVal)
						return cat;
				}
			}
			return null;
		}

		private List<IPhEnvironment> FindOrCreateEnvironment(string sEnv)
		{
			List<IPhEnvironment> rghvo;
			if (!m_dictEnvirons.TryGetValue(sEnv, out rghvo))
			{
				IPhEnvironment envNew = CreateNewPhEnvironment();
				m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(envNew);
				envNew.StringRepresentation = m_cache.TsStrFactory.MakeString(sEnv, m_cache.DefaultAnalWs);
				rghvo = new List<IPhEnvironment>();
				rghvo.Add(envNew);
				m_dictEnvirons.Add(sEnv, rghvo);
				m_rgnewEnvirons.Add(envNew);
			}
			return rghvo;
		}

		private IMoMorphType FindMorphType(string sTypeName)
		{
			ICmPossibility mmt;
			if (m_dictMmt.TryGetValue(sTypeName, out mmt) ||
				m_dictMmt.TryGetValue(sTypeName.ToLowerInvariant(), out mmt))
			{
				return mmt as IMoMorphType;
			}
			// This seems the most suitable default value.  Returning null causes crashes.
			// (See FWR-3869.)
			int count;
			if (!m_mapMorphTypeUnknownCount.TryGetValue(sTypeName, out count))
			{
				count = 0;
				m_mapMorphTypeUnknownCount.Add(sTypeName, count);
			}
			++count;
			m_mapMorphTypeUnknownCount[sTypeName] = count;
			return GetExistingMoMorphType(MoMorphTypeTags.kguidMorphStem);
		}

		private ILexRefType FindOrCreateLexRefType(string relationTypeName, bool fIsSequence)
		{
			ICmPossibility poss;
			if (m_dictLexRefTypes.TryGetValue(relationTypeName, out poss) ||
				m_dictLexRefTypes.TryGetValue(relationTypeName.ToLowerInvariant(), out poss))
			{
				return poss as ILexRefType;
			}
			if (m_dictRevLexRefTypes.TryGetValue(relationTypeName, out poss) ||
				m_dictRevLexRefTypes.TryGetValue(relationTypeName.ToLowerInvariant(), out poss))
			{
				return poss as ILexRefType;
			}
			ILexRefType lrt = CreateNewLexRefType();
			m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			lrt.Name.set_String(m_cache.DefaultAnalWs, relationTypeName);
			if ((String.IsNullOrEmpty(m_sLiftProducer) || m_sLiftProducer.StartsWith("WeSay")) &&
				(relationTypeName == "BaseForm"))
			{
				lrt.Abbreviation.set_String(m_cache.DefaultAnalWs, "base");
				lrt.ReverseName.set_String(m_cache.DefaultAnalWs, "Derived Forms");
				lrt.ReverseAbbreviation.set_String(m_cache.DefaultAnalWs, "deriv");
				lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryTree;
			}
			else
			{
				lrt.Abbreviation.set_String(m_cache.DefaultAnalWs, relationTypeName);
				if (fIsSequence)
					lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence;
				else
					lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection;
			}
			m_dictLexRefTypes.Add(relationTypeName, lrt);
			m_rgnewLexRefTypes.Add(lrt);
			return lrt;
		}

		private ILexEntryType FindComplexFormType(string sOldEntryType)
		{
			ICmPossibility poss;
			if (m_dictComplexFormType.TryGetValue(sOldEntryType, out poss) ||
				m_dictComplexFormType.TryGetValue(sOldEntryType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			return null;
		}

		private ILexEntryType FindVariantType(string sOldEntryType)
		{
			ICmPossibility poss;
			if (m_dictVariantType.TryGetValue(sOldEntryType, out poss) ||
				m_dictVariantType.TryGetValue(sOldEntryType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			return null;
		}

		private ILexEntryType FindOrCreateComplexFormType(string sType)
		{
			ICmPossibility poss;
			if (m_dictComplexFormType.TryGetValue(sType, out poss) ||
				m_dictComplexFormType.TryGetValue(sType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			ILexEntryType let = CreateNewLexEntryType();
			m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Add(let);
			let.Abbreviation.set_String(m_cache.DefaultAnalWs, sType);
			let.Name.set_String(m_cache.DefaultAnalWs, sType);
			let.ReverseAbbr.set_String(m_cache.DefaultAnalWs, sType);
			m_dictComplexFormType.Add(sType, let);
			m_rgnewComplexFormType.Add(let);
			return let;
		}

		private ILexEntryType FindOrCreateVariantType(string sType)
		{
			ICmPossibility poss;
			if (m_dictVariantType.TryGetValue(sType, out poss) ||
				m_dictVariantType.TryGetValue(sType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			ILexEntryType let = CreateNewLexEntryType();
			m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(let);
			let.Abbreviation.set_String(m_cache.DefaultAnalWs, sType);
			let.Name.set_String(m_cache.DefaultAnalWs, sType);
			let.ReverseAbbr.set_String(m_cache.DefaultAnalWs, sType);
			m_dictVariantType.Add(sType, let);
			m_rgnewVariantType.Add(let);
			return let;
		}

		private ICmAnthroItem FindOrCreateAnthroCode(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictAnthroCode.TryGetValue(traitValue, out poss) ||
				m_dictAnthroCode.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss as ICmAnthroItem;
			}
			ICmAnthroItem ant = CreateNewCmAnthroItem();
			m_cache.LangProject.AnthroListOA.PossibilitiesOS.Add(ant);
			ant.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			ant.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictAnthroCode.Add(traitValue, ant);
			m_rgnewAnthroCode.Add(ant);
			return ant;
		}

		private ICmSemanticDomain FindOrCreateSemanticDomain(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictSemDom.TryGetValue(traitValue, out poss) ||
				m_dictSemDom.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss as ICmSemanticDomain;
			}
			ICmSemanticDomain sem = CreateNewCmSemanticDomain();
			m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(sem);
			sem.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			sem.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictSemDom.Add(traitValue, sem);
			m_rgnewSemDom.Add(sem);
			return sem;
		}

		private ICmPossibility FindOrCreateDomainType(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictDomType.TryGetValue(traitValue, out poss) ||
				m_dictDomType.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictDomType.Add(traitValue, poss);
			m_rgnewDomType.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateSenseType(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictSenseType.TryGetValue(traitValue, out poss) ||
				m_dictSenseType.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.LexDbOA.SenseTypesOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictSenseType.Add(traitValue, poss);
			m_rgnewSenseType.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateStatus(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictStatus.TryGetValue(traitValue, out poss) ||
				m_dictStatus.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.StatusOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictStatus.Add(traitValue, poss);
			m_rgnewStatus.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateTranslationType(string sType)
		{
			ICmPossibility poss;
			if (m_dictTransType.TryGetValue(sType, out poss) ||
				m_dictTransType.TryGetValue(sType.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.TranslationTagsOA.PossibilitiesOS.Add(poss);
			poss.Name.set_String(m_cache.DefaultAnalWs, sType);
			m_dictTransType.Add(sType, poss);
			m_rgnewTransType.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateUsageType(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictUsageType.TryGetValue(traitValue, out poss) ||
				m_dictUsageType.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictUsageType.Add(traitValue, poss);
			m_rgnewUsageType.Add(poss);
			return poss;
		}

		private ICmLocation FindOrCreateLocation(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictLocation.TryGetValue(traitValue, out poss) ||
				m_dictLocation.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss as ICmLocation;
			}
			ICmLocation loc = CreateNewCmLocation();
			m_cache.LangProject.LocationsOA.PossibilitiesOS.Add(loc);
			loc.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			loc.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictLocation.Add(traitValue, loc);
			m_rgnewLocation.Add(loc);
			return loc;
		}
		#endregion // Methods to find or create list items

		#region Methods for handling relation links
		/// <summary>
		/// This isn't really a relation link, but it needs to be done at the end of the
		/// import process.
		/// </summary>
		private void ProcessMissingFeatStrucTypeFeatures()
		{
			foreach (IFsFeatStrucType type in m_mapFeatStrucTypeMissingFeatureAbbrs.Keys)
			{
				List<string> rgsAbbr = m_mapFeatStrucTypeMissingFeatureAbbrs[type];
				List<IFsFeatDefn> rgfeat = new List<IFsFeatDefn>(rgsAbbr.Count);
				for (int i = 0; i < rgsAbbr.Count; ++i)
				{
					string sAbbr = rgsAbbr[i];
					IFsFeatDefn feat;
					if (m_mapIdFeatDefn.TryGetValue(sAbbr, out feat))
						rgfeat.Add(feat);
					else
						break;
				}
				if (rgfeat.Count == rgsAbbr.Count)
				{
					type.FeaturesRS.Clear();
					for (int i = 0; i < rgfeat.Count; ++i)
						type.FeaturesRS.Add(rgfeat[i]);
				}
			}
			m_mapFeatStrucTypeMissingFeatureAbbrs.Clear();
		}

		/// <summary>
		/// After all the entries (and senses) have been imported, then the relations among
		/// them can be set since all the target ids can be resolved.
		/// This is also an opportunity to delete unwanted objects if we're keeping only
		/// the imported data.
		/// </summary>
		public void ProcessPendingRelations(IProgress progress)
		{
			if (m_mapFeatStrucTypeMissingFeatureAbbrs.Count > 0)
				ProcessMissingFeatStrucTypeFeatures();
			if (m_rgPendingRelation.Count > 0)
			{
				progress.Message = String.Format(LexTextControls.ksProcessingRelationLinks, m_rgPendingRelation.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingRelation.Count;
				// First pass, ignore "minorentry" and "subentry", since those should be
				// installed by "main".  (The first two are backreferences to the third.)
				// Also ignore reverse tree relation references on the first pass.
				// Also collect more information about collection type relations without
				// storing anything in the database yet.
				m_rgPendingTreeTargets.Clear();
				for (int i = 0; i < m_rgPendingRelation.Count; )
				{
					List<PendingRelation> rgRelation = CollectRelationMembers(i);
					if (rgRelation == null || rgRelation.Count == 0)
					{
						++i;
					}
					else
					{
						i += rgRelation.Count;
						ProcessRelation(rgRelation);
					}
					progress.Position = i;
				}
			}
			StorePendingCollectionRelations(progress);
			StorePendingTreeRelations(progress);
			StorePendingLexEntryRefs(progress);
			// We can now store residue everywhere since any bogus relations have been added
			// to residue.
			progress.Message = LexTextControls.ksWritingAccumulatedResidue;
			WriteAccumulatedResidue();

			// If we're keeping only the imported data, erase any unused entries or senses.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				progress.Message = LexTextControls.ksDeletingUnwantedEntries;
				GatherUnwantedEntries();
				DeleteUnwantedObjects();
			}
			// Now that the relations have all been set, it's safe to set the entry
			// modification times.
			progress.Message = LexTextControls.ksSettingEntryModificationTimes;
			foreach (PendingModifyTime pmt in m_rgPendingModifyTimes)
				pmt.SetModifyTime();
		}

		private void StorePendingLexEntryRefs(IProgress progress)
		{
			// Now create the LexEntryRef type links.
			if (m_rgPendingLexEntryRefs.Count > 0)
			{
				progress.Message = String.Format(LexTextControls.ksStoringLexicalEntryReferences,
					m_rgPendingLexEntryRefs.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingLexEntryRefs.Count;
				for (int i = 0; i < m_rgPendingLexEntryRefs.Count; )
				{
					List<PendingLexEntryRef> rgRefs = CollectLexEntryRefMembers(i);
					if (rgRefs == null || rgRefs.Count == 0)
					{
						++i;
					}
					else
					{
						ProcessLexEntryRefs(rgRefs);
						i += rgRefs.Count;
					}
					progress.Position = i;
				}
			}
		}

		private void StorePendingTreeRelations(IProgress progress)
		{
			if (m_rgPendingTreeTargets.Count > 0)
			{
				progress.Message = String.Format(LexTextControls.ksSettingTreeRelationLinks,
					m_rgPendingTreeTargets.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingTreeTargets.Count;
				for (int i = 0; i < m_rgPendingTreeTargets.Count; ++i)
				{
					ProcessRemainingTreeRelation(m_rgPendingTreeTargets[i]);
					progress.Position = i + 1;
				}
			}
		}

		private void ProcessRemainingTreeRelation(PendingRelation rel)
		{
			Debug.Assert(rel.Target != null);
			if (rel.Target == null)
				return;
			string sType = rel.RelationType;
			Debug.Assert(!rel.IsSequence);
			ILexRefType lrt = FindOrCreateLexRefType(sType, false);
			if (!TreeRelationAlreadyExists(lrt, rel))
			{
				ILexReference lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Add(rel.Target);
				lr.TargetsRS.Add(rel.CmObject);
				StoreRelationResidue(lr, rel);
			}
		}

		private void ProcessRelation(List<PendingRelation> rgRelation)
		{
			if (rgRelation == null || rgRelation.Count == 0 || rgRelation[0] == null)
				return;
			switch (rgRelation[0].RelationType)
			{
				case "main":
				case "minorentry":
				case "subentry":
				case "_component-lexeme":
					// These should never get this far...
					Debug.Assert(rgRelation[0].RelationType == "Something else...");
					break;
				default:
					StoreLexReference(rgRelation);
					break;
			}
		}

		private List<PendingLexEntryRef> CollectLexEntryRefMembers(int i)
		{
			if (i < 0 || i >= m_rgPendingLexEntryRefs.Count)
				return null;
			List<PendingLexEntryRef> rgRefs = new List<PendingLexEntryRef>();
			PendingLexEntryRef prev = null;
			int hvo = m_rgPendingLexEntryRefs[i].ObjectHvo;
			string sEntryType = m_rgPendingLexEntryRefs[i].EntryType;
			string sMinorEntryCondition = m_rgPendingLexEntryRefs[i].MinorEntryCondition;
			DateTime dateCreated = m_rgPendingLexEntryRefs[i].DateCreated;
			DateTime dateModified = m_rgPendingLexEntryRefs[i].DateModified;
			//string sResidue = m_rgPendingLexEntryRefs[i].Residue; // cs 219
			while (i < m_rgPendingLexEntryRefs.Count)
			{
				PendingLexEntryRef pend = m_rgPendingLexEntryRefs[i];
				// If the object, entry type (in an old LIFT file), or minor entry condition
				// (in an old LIFT file) has changed, we're into another LexEntryRef.
				if (pend.ObjectHvo != hvo || pend.EntryType != sEntryType ||
					pend.MinorEntryCondition != sMinorEntryCondition ||
					pend.DateCreated != dateCreated || pend.DateModified != dateModified)
				{
					break;
				}
				// The end of the components of a LexEntryRef may be marked only by a sudden
				// drop in the order value (which starts at 0 and increments by 1 steadily, or
				// is set to -1 when there's only one).
				if (prev != null && pend.Order < prev.Order)
					break;
				pend.Target = GetObjectFromTargetIdString(m_rgPendingLexEntryRefs[i].TargetId);
				rgRefs.Add(pend);
				if (pend.Order == -1 && pend.RelationType != "main")
					break;
				prev = pend;
				++i;
			}
			return rgRefs;
		}

		private void ProcessLexEntryRefs(List<PendingLexEntryRef> rgRefs)
		{
			if (rgRefs.Count == 0)
				return;
			ILexEntry le = null;
			ICmObject target = null;
			if (rgRefs.Count == 1 && rgRefs[0].RelationType == "main")
			{
				target = rgRefs[0].CmObject;
				string sRef = rgRefs[0].TargetId;
				ICmObject cmo;
				if (!String.IsNullOrEmpty(sRef) && m_mapIdObject.TryGetValue(sRef, out cmo))
				{
					Debug.Assert(cmo is ILexEntry);
					le = cmo as ILexEntry;
				}
				else
				{
					// log error message about invalid link in <relation type="main" ref="...">.
					InvalidRelation bad = new InvalidRelation(rgRefs[0], m_cache, this);
					if (!m_rgInvalidRelation.Contains(bad))
						m_rgInvalidRelation.Add(bad);
				}
			}
			else
			{
				Debug.Assert(rgRefs[0].CmObject is ILexEntry);
				le = rgRefs[0].CmObject as ILexEntry;
			}
			if (le == null)
				return;
			ILexEntryRef ler = CreateNewLexEntryRef();
			le.EntryRefsOS.Add(ler);
			SetLexEntryTypes(rgRefs, ler);
			ler.HideMinorEntry = rgRefs[0].HideMinorEntry;
			if (rgRefs[0].Summary != null && rgRefs[0].Summary.Content != null)
				MergeInMultiString(ler.Summary, LexEntryRefTags.kflidSummary, rgRefs[0].Summary.Content, ler.Guid);
			for (int i = 0; i < rgRefs.Count; ++i)
			{
				PendingLexEntryRef pend = rgRefs[i];
				if (pend.RelationType == "main" && i == 0 && target != null)
				{
					ler.ComponentLexemesRS.Add(target);
					ler.PrimaryLexemesRS.Add(target);
				}
				else if (pend.Target != null)
				{
					ler.ComponentLexemesRS.Add(pend.Target);
					if (pend.IsPrimary || pend.RelationType == "main")
						ler.PrimaryLexemesRS.Add(pend.Target);
				}
				else
				{
					Debug.Assert(rgRefs.Count == 1);
					Debug.Assert(!pend.IsPrimary);
				}
			}
			// Create an empty sense if a complex form came in without a sense.  See LT-9153.
			if (le.SensesOS.Count == 0 &&
				(ler.ComplexEntryTypesRS.Count > 0 || ler.PrimaryLexemesRS.Count > 0))
			{
				bool fNeedNewId;
				CreateNewLexSense(Guid.Empty, le, out fNeedNewId);
				EnsureValidMSAsForSenses(le);
			}
		}

		/// <summary>
		/// Set the ler.ComplexEntryTypes and ler.VariantEntryTypes as best we can.
		/// </summary>
		/// <param name="rgRefs"></param>
		/// <param name="ler"></param>
		private void SetLexEntryTypes(List<PendingLexEntryRef> rgRefs, ILexEntryRef ler)
		{
			List<string> rgsComplexFormTypes = rgRefs[0].ComplexFormTypes;
			List<string> rgsVariantTypes = rgRefs[0].VariantTypes;
			string sOldEntryType = rgRefs[0].EntryType;
			string sOldCondition = rgRefs[0].MinorEntryCondition;
			// A trait name complex-form-type or variant-type can be used with an unspecified value
			// to indicate that this reference type is either complex or variant (more options in future).
			if (rgsComplexFormTypes.Count > 0)
				ler.RefType = LexEntryRefTags.krtComplexForm;
			if (rgsVariantTypes.Count > 0)
				ler.RefType = LexEntryRefTags.krtVariant;
			if (rgsComplexFormTypes.Count > 0 && rgsVariantTypes.Count > 0)
			{
				// TODO: Complain to the user that he's getting ahead of the programmers!
			}
			foreach (string sType in rgsComplexFormTypes)
			{
				if (!String.IsNullOrEmpty(sType))
				{
					ILexEntryType let = FindOrCreateComplexFormType(sType);
					ler.ComplexEntryTypesRS.Add(let);
				}
			}
			foreach (string sType in rgsVariantTypes)
			{
				if (!String.IsNullOrEmpty(sType))
				{
					ILexEntryType let = FindOrCreateVariantType(sType);
					ler.VariantEntryTypesRS.Add(let);
				}
			}
			if (ler.ComplexEntryTypesRS.Count == 0 &&
				ler.VariantEntryTypesRS.Count == 0 &&
				!String.IsNullOrEmpty(sOldEntryType))
			{
				if (sOldEntryType == "Derivation")
					sOldEntryType = "Derivative";
				else if (sOldEntryType == "derivation")
					sOldEntryType = "derivative";
				else if (sOldEntryType == "Inflectional Variant")
					sOldEntryType = "Irregularly Inflected Form";
				else if (sOldEntryType == "inflectional variant")
					sOldEntryType = "irregularly inflected form";
				ILexEntryType letComplex = FindComplexFormType(sOldEntryType);
				if (letComplex == null)
				{
					ILexEntryType letVar = FindVariantType(sOldEntryType);
					if (letVar == null && sOldEntryType.ToLowerInvariant() != "main entry")
					{
						if (String.IsNullOrEmpty(sOldCondition))
						{
							letComplex = FindOrCreateComplexFormType(sOldEntryType);
							ler.ComplexEntryTypesRS.Add(letComplex);
							ler.RefType = LexEntryRefTags.krtComplexForm;
						}
						else
						{
							letVar = FindOrCreateVariantType(sOldEntryType);
						}
					}
					if (letVar != null)
					{
						if (String.IsNullOrEmpty(sOldCondition))
						{
							ler.VariantEntryTypesRS.Add(letVar);
						}
						else
						{
							ILexEntryType subtype = null;
							foreach (ICmPossibility poss in letVar.SubPossibilitiesOS)
							{
								ILexEntryType sub = poss as ILexEntryType;
								if (sub != null &&
									(sub.Name.AnalysisDefaultWritingSystem.Text == sOldCondition ||
									 sub.Abbreviation.AnalysisDefaultWritingSystem.Text == sOldCondition ||
									 sub.ReverseAbbr.AnalysisDefaultWritingSystem.Text == sOldCondition))
								{
									subtype = sub;
									break;
								}
							}
							if (subtype == null)
							{
								subtype = CreateNewLexEntryType();
								letVar.SubPossibilitiesOS.Add(subtype as ICmPossibility);
								subtype.Name.set_String(m_cache.DefaultAnalWs, sOldCondition);
								subtype.Abbreviation.set_String(m_cache.DefaultAnalWs, sOldCondition);
								subtype.ReverseAbbr.set_String(m_cache.DefaultAnalWs, sOldCondition);
								m_rgnewVariantType.Add(subtype);
							}
							ler.VariantEntryTypesRS.Add(subtype);
						}
						ler.RefType = LexEntryRefTags.krtVariant;
					}
				}
				else
				{
					ler.ComplexEntryTypesRS.Add(letComplex);
					ler.RefType = LexEntryRefTags.krtComplexForm;
				}
				// Adjust HideMinorEntry for using old LIFT file.
				if (rgRefs[0].HideMinorEntry == 0 && rgRefs[0].ExcludeAsHeadword)
					rgRefs[0].HideMinorEntry = 1;
			}
		}

		private void StoreLexReference(List<PendingRelation> rgRelation)
		{
			// Store any relations with unrecognized targets in residue, removing them from the
			// list.
			for (int i = 0; i < rgRelation.Count; ++i)
			{
				if (rgRelation[i].Target == null)
					StoreResidue(rgRelation[i].CmObject, rgRelation[i].AsResidueString());
			}
			for (int i = rgRelation.Count - 1; i >= 0; --i)
			{
				if (rgRelation[i].Target == null)
					rgRelation.RemoveAt(i);
			}
			if (rgRelation.Count == 0)
				return;
			// Store the list of relations appropriately as a LexReference with a proper type.
			string sType = rgRelation[0].RelationType;
			ILexRefType lrt = FindOrCreateLexRefType(sType, rgRelation[0].IsSequence);
			switch (lrt.MappingType)
			{
				case (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					StoreAsymmetricPairRelations(lrt, rgRelation,
						ObjectIsFirstInRelation(rgRelation[0].RelationType, lrt));
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case (int)LexRefTypeTags.MappingTypes.kmtSensePair:
					StorePairRelations(lrt, rgRelation);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseCollection:
					CollapseCollectionRelationPairs(rgRelation);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
				case (int)LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseSequence:
					StoreSequenceRelation(lrt, rgRelation);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryTree:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseTree:
					StoreTreeRelation(lrt, rgRelation,
						ObjectIsFirstInRelation(rgRelation[0].RelationType, lrt));
					break;
			}
		}

		private void StoreAsymmetricPairRelations(ILexRefType lrt, List<PendingRelation> rgRelation,
			bool fFirst)
		{
			for (int i = 0; i < rgRelation.Count; ++i)
			{
				Debug.Assert(rgRelation[i].Target != null);
				if (rgRelation[i].Target == null)
					continue;
				if (AsymmetricPairRelationAlreadyExists(lrt, rgRelation[i], fFirst))
					continue;
				ILexReference lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				if (fFirst)
				{
					lr.TargetsRS.Add(rgRelation[i].CmObject);
					lr.TargetsRS.Add(rgRelation[i].Target);
				}
				else
				{
					lr.TargetsRS.Add(rgRelation[i].Target);
					lr.TargetsRS.Add(rgRelation[i].CmObject);
				}
				StoreRelationResidue(lr, rgRelation[i]);
			}
		}

		private bool AsymmetricPairRelationAlreadyExists(ILexRefType lrt, PendingRelation rel,
			bool fFirst)
		{
			int hvo1 = rel.CmObject == null ? 0 : rel.ObjectHvo;
			int hvo2 = rel.TargetHvo;
			foreach (ILexReference lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != 2)
					continue;		// SHOULD NEVER HAPPEN!!
				int hvoA = lr.TargetsRS[0].Hvo;
				int hvoB = lr.TargetsRS[1].Hvo;
				if (fFirst)
				{
					if (hvoA == hvo1 && hvoB == hvo2)
						return true;
				}
				else
				{
					if (hvoA == hvo2 && hvoB == hvo1)
						return true;
				}
			}
			return false;
		}

		private void StorePairRelations(ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			for (int i = 0; i < rgRelation.Count; ++i)
			{
				Debug.Assert(rgRelation[i].Target != null);
				if (rgRelation[i].Target == null)
					continue;
				if (PairRelationAlreadyExists(lrt, rgRelation[i]))
					continue;
				ILexReference lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Add(rgRelation[i].CmObject);
				lr.TargetsRS.Add(rgRelation[i].Target);
				StoreRelationResidue(lr, rgRelation[i]);
			}
		}

		private bool PairRelationAlreadyExists(ILexRefType lrt, PendingRelation rel)
		{
			int hvo1 = rel.CmObject == null ? 0 : rel.ObjectHvo;
			int hvo2 = rel.TargetHvo;
			foreach (ILexReference lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != 2)
					continue;		// SHOULD NEVER HAPPEN!!
				int hvoA = lr.TargetsRS[0].Hvo;
				int hvoB = lr.TargetsRS[1].Hvo;
				if (hvoA == hvo1 && hvoB == hvo2)
					return true;
				else if (hvoA == hvo2 && hvoB == hvo1)
					return true;
			}
			return false;
		}


		private void CollapseCollectionRelationPairs(List<PendingRelation> rgRelation)
		{
			foreach (PendingRelation rel in rgRelation)
			{
				Debug.Assert(rel.Target != null);
				if (rel.Target == null)
					continue;
				bool fAdd = true;
				foreach (PendingRelation pend in m_rgPendingCollectionRelations)
				{
					if (pend.IsSameOrMirror(rel))
					{
						fAdd = false;
						break;
					}
				}
				if (fAdd)
					m_rgPendingCollectionRelations.AddLast(rel);
			}
		}

		private void StorePendingCollectionRelations(IProgress progress)
		{
			int cOrig = m_rgPendingCollectionRelations.Count;
			if (cOrig == 0)
				return;
			progress.Message = String.Format(LexTextControls.ksSettingCollectionRelationLinks,
				m_rgPendingCollectionRelations.Count);
			progress.Minimum = 0;
			progress.Maximum = cOrig;
			progress.Position = 0;
			while (m_rgPendingCollectionRelations.Count > 0)
			{
				PendingRelation rel = m_rgPendingCollectionRelations.First.Value;
				m_rgPendingCollectionRelations.RemoveFirst();
				StoreCollectionRelation(rel);
				progress.Position = cOrig - m_rgPendingCollectionRelations.Count;
			}
		}

		private void StoreCollectionRelation(PendingRelation relMain)
		{
			string sType = relMain.RelationType;
			ILexRefType lrt = FindOrCreateLexRefType(sType, relMain.IsSequence);
			Set<int> currentRel = new Set<int>();
			currentRel.Add(relMain.ObjectHvo);
			currentRel.Add(relMain.TargetHvo);
			int cAdded;
			do
			{
				cAdded = 0;
				LinkedListNode<PendingRelation> nodeNext = null;
				for (LinkedListNode<PendingRelation> node = m_rgPendingCollectionRelations.First;
					node != null;
					node = nodeNext)
				{
					nodeNext = node.Next;
					PendingRelation rel = node.Value;
					if (rel.RelationType != sType)
						break;
					int hvoNew = 0;
					if (currentRel.Contains(rel.ObjectHvo))
						hvoNew = rel.TargetHvo;
					else if (currentRel.Contains(rel.TargetHvo))
						hvoNew = rel.ObjectHvo;
					else
						continue;
					bool fDelNode = false;
					LinkedListNode<PendingRelation> node2Next = null;
					for (LinkedListNode<PendingRelation> node2 = node.Next;
						node2 != null;
						node2 = node2Next)
					{
						node2Next = node2.Next;
						PendingRelation rel2 = node2.Value;
						if (rel2.RelationType != sType)
							break;
						if ((rel2.ObjectHvo == hvoNew && currentRel.Contains(rel2.TargetHvo)) ||
							(rel2.TargetHvo == hvoNew && currentRel.Contains(rel2.ObjectHvo)))
						{
							// Two pairs have the new item, with their other items already in the
							// collection.  We can add the new item to the collection.
							currentRel.Add(hvoNew);
							++cAdded;
							// Remove nodes that have been used from the linked list.
							fDelNode = true;
							m_rgPendingCollectionRelations.Remove(node2);
						}
					}
					nodeNext = node.Next;
					if (fDelNode)
					{
						nodeNext = node.Next;	// may have changed in inner loop
						m_rgPendingCollectionRelations.Remove(node);
					}
				}
			} while (cAdded > 0);
			if (CollectionRelationAlreadyExists(lrt, currentRel))
				return;
			ILexReference lr = CreateNewLexReference();
			lrt.MembersOC.Add(lr);
			foreach (int hvo in currentRel)
				lr.TargetsRS.Add(GetObjectForId(hvo));
			StoreRelationResidue(lr, relMain);
		}

		private bool CollectionRelationAlreadyExists(ILexRefType lrt, Set<int> setRelation)
		{
			foreach (ILexReference lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != setRelation.Count)
					continue;
				bool fSame = true;
				foreach (ICmObject cmo in lr.TargetsRS)
				{
					if (!setRelation.Contains(cmo.Hvo))
					{
						fSame = false;
						break;
					}
				}
				if (fSame)
					return true;
			}
			return false;
		}

		private bool IsMember(int[] targets, int hvo)
		{
			for (int i = targets.Length - 1; i >= 0; i--)
			{
				if (targets[i] == hvo)
					return true;
			}
			return false;

		}

		private void StoreSequenceRelation(ILexRefType lrt, List<
			PendingRelation> rgRelation)
		{
			if (SequenceRelationAlreadyExists(lrt, rgRelation))
				return;
			ILexReference lr = CreateNewLexReference();
			lrt.MembersOC.Add(lr);
			for (int i = 0; i < rgRelation.Count; ++i)
				lr.TargetsRS.Add(GetObjectForId(rgRelation[i].TargetHvo));
			StoreRelationResidue(lr, rgRelation[0]);
		}

		private bool SequenceRelationAlreadyExists(ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			foreach (ILexReference lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != rgRelation.Count)
					continue;
				bool fSame = true;
				for (int i = 0; i < rgRelation.Count; ++i)
				{
					if (lr.TargetsRS[i].Hvo != rgRelation[i].TargetHvo)
					{
						fSame = false;
						break;
					}
				}
				if (fSame)
					return true;
			}
			return false;
		}

		private void StoreTreeRelation(ILexRefType lrt, List<PendingRelation> rgRelation,
			bool fFirst)
		{
			if (fFirst)
			{
				if (TreeRelationAlreadyExists(lrt, rgRelation))
					return;
				ILexReference lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Add(rgRelation[0].CmObject);
				for (int i = 0; i < rgRelation.Count; ++i)
					lr.TargetsRS.Add(GetObjectForId(rgRelation[i].TargetHvo));
				StoreRelationResidue(lr, rgRelation[0]);
			}
			else
			{
				for (int i = 0; i < rgRelation.Count; ++i)
				{
					if (TreeRelationAlreadyExists(lrt, rgRelation[i]))
						continue;
					m_rgPendingTreeTargets.Add(rgRelation[i]);
				}
			}
		}

		private void StoreRelationResidue(ILexReference lr, PendingRelation pend)
		{
			string sResidue = pend.Residue;
			if (!String.IsNullOrEmpty(sResidue) ||
				IsDateSet(pend.DateCreated) || IsDateSet(pend.DateModified))
			{
				StringBuilder bldr = new StringBuilder();
				bldr.Append("<lift-residue");
				AppendXmlDateAttributes(bldr, pend.DateCreated, pend.DateModified);
				bldr.AppendLine(">");
				if (!String.IsNullOrEmpty(sResidue))
					bldr.Append(sResidue);
				bldr.Append("</lift-residue>");
				lr.LiftResidue = bldr.ToString();
			}
		}

		private bool TreeRelationAlreadyExists(ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			foreach (ILexReference lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != rgRelation.Count + 1)
					continue;
				if (lr.TargetsRS[0].Hvo != rgRelation[0].ObjectHvo)
					continue;
				int[] rghvoRef = new int[rgRelation.Count];
				for (int i = 0; i < rghvoRef.Length; ++i)
					rghvoRef[i] = lr.TargetsRS[i + 1].Hvo;
				int[] rghvoNew = new int[rgRelation.Count];
				for (int i = 0; i < rghvoNew.Length; ++i)
					rghvoNew[i] = rgRelation[i].TargetHvo;
				Array.Sort(rghvoRef);
				Array.Sort(rghvoNew);
				bool fSame = true;
				for (int i = 0; i < rghvoRef.Length; ++i)
				{
					if (rghvoRef[i] != rghvoNew[i])
					{
						fSame = false;
						break;
					}
				}
				if (fSame)
					return true;
			}
			return false;
		}

		private bool TreeRelationAlreadyExists(ILexRefType lrt, PendingRelation rel)
		{
			foreach (ILexReference lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count == 0 || lr.TargetsRS[0].Hvo != rel.TargetHvo)
					continue;
				if (IsMember(lr.TargetsRS.ToHvoArray(), rel.ObjectHvo))
					return true;
			}
			return false;
		}

		private bool ObjectIsFirstInRelation(string sType, ILexRefType lrt)
		{
			if (HasMatchingUnicodeAlternative(sType.ToLowerInvariant(), lrt.Abbreviation, lrt.Name))
				return true;
			else
				return false;
		}

		private List<PendingRelation> CollectRelationMembers(int i)
		{
			if (i < 0 || i >= m_rgPendingRelation.Count)
				return null;
			List<PendingRelation> rgRelation = new List<PendingRelation>();
			PendingRelation prev = null;
			int hvo = m_rgPendingRelation[i].ObjectHvo;
			string sType = m_rgPendingRelation[i].RelationType;
			DateTime dateCreated = m_rgPendingRelation[i].DateCreated;
			DateTime dateModified = m_rgPendingRelation[i].DateModified;
			string sResidue = m_rgPendingRelation[i].Residue;
			while (i < m_rgPendingRelation.Count)
			{
				PendingRelation pend = m_rgPendingRelation[i];
				// If the object or relation type (or residue) has changed, we're into another
				// lexical relation.
				if (pend.ObjectHvo != hvo || pend.RelationType != sType ||
					pend.DateCreated != dateCreated || pend.DateModified != dateModified ||
					pend.Residue != sResidue)
				{
					break;
				}
				// The end of a sequence relation may be marked only by a sudden drop in
				// the order value (which starts at 1 and increments by 1 steadily, or is
				// set to -1 for non-sequence relation).
				if (prev != null && pend.Order < prev.Order)
					break;
				pend.Target = GetObjectFromTargetIdString(m_rgPendingRelation[i].TargetId);
				rgRelation.Add(pend);	// We handle missing/unrecognized targets later.
				prev = pend;
				++i;
			}
			return rgRelation;
		}

		private void GatherUnwantedEntries()
		{
			foreach (ILexEntry le in m_cache.LangProject.LexDbOA.Entries)
			{
				if (!m_setUnchangedEntry.Contains(le.Guid) &&
					!m_setChangedEntry.Contains(le.Guid))
				{
					m_deletedObjects.Add(le.Hvo);
				}
			}
		}

		private void DeleteUnwantedObjects()
		{
			if (m_deletedObjects.Count > 0)
			{
				DeleteObjects(m_deletedObjects);
				DeleteOrphans();
			}
		}

		/// <summary>
		/// This pretends to replace CmObject.DeleteObjects() in the old system.
		/// </summary>
		/// <param name="deletedObjects"></param>
		private void DeleteObjects(Set<int> deletedObjects)
		{
			foreach (int hvo in deletedObjects)
			{
				try
				{
					ICmObject cmo = GetObjectForId(hvo);
					int hvoOwner = cmo.Owner == null ? 0 : cmo.Owner.Hvo;
					int flid = cmo.OwningFlid;
					m_cache.MainCacheAccessor.DeleteObjOwner(hvoOwner, hvo, flid, -1);
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// This replaces CmObject.DeleteOrphanedObjects(m_cache, false, null); in the
		/// old system, which used SQL extensively.  I'm not sure where this should go in
		/// the new system, or if it was used anywhere else.
		/// </summary>
		private void DeleteOrphans()
		{
			Set<int> orphans = new Set<int>();
			// Look for LexReference objects that have lost all their targets.
			ILexReferenceRepository repoLR = m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			foreach (ILexReference lr in repoLR.AllInstances())
			{
				if (lr.TargetsRS.Count == 0)
					orphans.Add(lr.Hvo);
			}
			DeleteObjects(orphans);
			orphans.Clear();
			// Look for MSAs that are not used by any senses.
			IMoMorphSynAnalysisRepository repoMsa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
			foreach (IMoMorphSynAnalysis msa in repoMsa.AllInstances())
			{
				ILexEntry le = msa.Owner as ILexEntry;
				if (le == null)
					continue;
				bool fUsed = false;
				foreach (ILexSense ls in le.AllSenses)
				{
					if (ls.MorphoSyntaxAnalysisRA == msa)
					{
						fUsed = true;
						break;
					}
				}
				if (!fUsed)
					orphans.Add(msa.Hvo);
			}
			DeleteObjects(orphans);
			orphans.Clear();
			// Look for WfiAnalysis objects that are not targeted by a human CmAgentEvaluation
			// and which do not own a WfiMorphBundle with a set Msa value.
			IWfiAnalysisRepository repoWA = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>();
			ICmAgent cmaHuman = GetObjectForGuid(CmAgentTags.kguidAgentDefUser) as ICmAgent;
			Debug.Assert(cmaHuman != null);
			foreach (IWfiAnalysis wa in repoWA.AllInstances())
			{
				if (wa.GetAgentOpinion(cmaHuman as ICmAgent) == Opinions.noopinion)
				{
					bool fOk = false;
					foreach (IWfiMorphBundle wmb in wa.MorphBundlesOS)
					{
						if (wmb.MsaRA != null)
						{
							fOk = true;
							break;
						}
					}
					if (!fOk)
						orphans.Add(wa.Hvo);
				}
			}
			DeleteObjects(orphans);
			orphans.Clear();

			// Update WfiMorphBundle.Form and WfiMorphBundle.Msa as needed.
			IWfiMorphBundleRepository repoWMB = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>();
			foreach (IWfiMorphBundle mb in repoWMB.AllInstances())
			{
				if (mb.Form.StringCount == 0 && mb.MorphRA == null && mb.MsaRA == null && mb.SenseRA == null)
				{
					IWfiAnalysis wa = mb.Owner as IWfiAnalysis;
					IWfiWordform wf = wa.Owner as IWfiWordform;
					ITsString tssWordForm = wf.Form.get_String(m_cache.DefaultVernWs);
					if (tssWordForm != null && tssWordForm.Length > 0)
						mb.Form.set_String(m_cache.DefaultVernWs, tssWordForm.Text);
				}
				if (mb.MsaRA == null && mb.SenseRA != null)
				{
					mb.MsaRA = mb.SenseRA.MorphoSyntaxAnalysisRA;
				}
			}
			// Look for MoMorphAdhocProhib objects that don't have any Morphemes (MSA targets)
			IMoMorphAdhocProhibRepository repoMAP = m_cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibRepository>();
			foreach (IMoMorphAdhocProhib map in repoMAP.AllInstances())
			{
				if (map.MorphemesRS.Count == 0)
					orphans.Add(map.Hvo);
			}
			DeleteObjects(orphans);
			orphans.Clear();
		}
		#endregion // Methods for handling relation links

		#region Methods for displaying list items created during import
		/// <summary>
		/// Summarize what we added to the known lists.
		/// </summary>
		public string DisplayNewListItems(string sLIFTFile, int cEntriesRead)
		{
			string sDir = System.IO.Path.GetDirectoryName(sLIFTFile);
			string sLogFile = String.Format("{0}-ImportLog.htm",
				System.IO.Path.GetFileNameWithoutExtension(sLIFTFile));
			string sHtmlFile = System.IO.Path.Combine(sDir, sLogFile);
			DateTime dtEnd = DateTime.Now;

			using(var writer = new StreamWriter(sHtmlFile, false, System.Text.Encoding.UTF8))
			{
				string sTitle = String.Format(LexTextControls.ksImportLogFor0, sLIFTFile);
				writer.WriteLine("<html>");
				writer.WriteLine("<head>");
				writer.WriteLine("<title>{0}</title>", sTitle);
				writer.WriteLine("</head>");
				writer.WriteLine("<body>");
				writer.WriteLine("<h2>{0}</h2>", sTitle);
				long deltaTicks = dtEnd.Ticks - m_dtStart.Ticks;	// number of 100-nanosecond intervals
				int deltaMsec = (int)((deltaTicks + 5000L) / 10000L);	// round off to milliseconds
				int deltaSec = deltaMsec / 1000;
				string sDeltaTime = String.Format(LexTextControls.ksImportingTookTime,
					System.IO.Path.GetFileName(sLIFTFile), deltaSec, deltaMsec % 1000);
				writer.WriteLine("<p>{0}</p>", sDeltaTime);
				string sEntryCounts = String.Format(LexTextControls.ksEntriesImportCounts,
					cEntriesRead, m_cEntriesAdded, m_cSensesAdded, m_cEntriesDeleted);
				writer.WriteLine("<p><h3>{0}</h3></p>", sEntryCounts);
				ListNewPossibilities(writer, LexTextControls.ksPartsOfSpeechAdded, m_rgnewPos);
				ListNewPossibilities(writer, LexTextControls.ksMorphTypesAdded, m_rgnewMmt);
				ListNewLexEntryTypes(writer, LexTextControls.ksComplexFormTypesAdded, m_rgnewComplexFormType);
				ListNewLexEntryTypes(writer, LexTextControls.ksVariantTypesAdded, m_rgnewVariantType);
				ListNewPossibilities(writer, LexTextControls.ksSemanticDomainsAdded, m_rgnewSemDom);
				ListNewPossibilities(writer, LexTextControls.ksTranslationTypesAdded, m_rgnewTransType);
				ListNewPossibilities(writer, LexTextControls.ksConditionsAdded, m_rgnewCondition);
				ListNewPossibilities(writer, LexTextControls.ksAnthropologyCodesAdded, m_rgnewAnthroCode);
				ListNewPossibilities(writer, LexTextControls.ksDomainTypesAdded, m_rgnewDomType);
				ListNewPossibilities(writer, LexTextControls.ksSenseTypesAdded, m_rgnewSenseType);
				ListNewPossibilities(writer, LexTextControls.ksStatusValuesAdded, m_rgnewStatus);
				ListNewPossibilities(writer, LexTextControls.ksUsageTypesAdded, m_rgnewUsageType);
				ListNewEnvironments(writer, LexTextControls.ksEnvironmentsAdded, m_rgnewEnvirons);
				ListNewPossibilities(writer, LexTextControls.ksLexicalReferenceTypesAdded, m_rgnewLexRefTypes);
				ListNewWritingSystems(writer, LexTextControls.ksWritingSystemsAdded, m_addedWss);
				ListNewInflectionClasses(writer, LexTextControls.ksInflectionClassesAdded, m_rgnewInflClasses);
				ListNewSlots(writer, LexTextControls.ksInflectionalAffixSlotsAdded, m_rgnewSlots);
				ListNewPossibilities(writer, LexTextControls.ksExceptionFeaturesAdded, m_rgnewExceptFeat);
				ListNewInflectionalFeatures(writer, LexTextControls.ksInflectionFeaturesAdded, m_rgnewFeatDefn);
				ListNewFeatureTypes(writer, LexTextControls.ksFeatureTypesAdded, m_rgnewFeatStrucType);
				ListNewStemNames(writer, LexTextControls.ksStemNamesAdded, m_rgnewStemName);
				ListNewCustomFields(writer, LexTextControls.ksCustomFieldsAdded, m_rgnewCustomFields);
				ListConflictsFound(writer, LexTextControls.ksConflictsResultedInDup, m_rgcdConflicts);
				ListInvalidData(writer);
				ListTruncatedData(writer);
				ListInvalidRelations(writer);
				ListInvalidMorphTypes(writer);
				ListErrorMessages(writer);
				writer.WriteLine("</body>");
				writer.WriteLine("</html>");
				writer.Close();
				return sHtmlFile;
			}
		}

		private void ListConflictsFound(StreamWriter writer, string sMsg,
			List<ConflictingData> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<table border=\"1\" width=\"100%\">");
				writer.WriteLine("<tbody>");
				writer.WriteLine("<caption><h3>{0}</h3></caption>", sMsg);
				writer.WriteLine("<tr>");
				writer.WriteLine("<th width=\"16%\">{0}</th>", LexTextControls.ksType);
				writer.WriteLine("<th width=\"28%\">{0}</th>", LexTextControls.ksConflictingField);
				writer.WriteLine("<th width=\"28%\">{0}</th>", LexTextControls.ksOriginal);
				writer.WriteLine("<th width=\"28%\">{0}</th>", LexTextControls.ksNewDuplicate);
				writer.WriteLine("</tr>");
				foreach (ConflictingData cd in list)
				{
					writer.WriteLine("<tr>");
					writer.WriteLine("<td width=\"16%\">{0}</td>", cd.ConflictType);
					writer.WriteLine("<td width=\"28%\">{0}</td>", cd.ConflictField);
					writer.WriteLine("<td width=\"28%\">{0}</td>", cd.OrigHtmlReference());
					writer.WriteLine("<td width=\"28%\">{0}</td>", cd.DupHtmlReference());
					writer.WriteLine("</tr>");
				}
				writer.WriteLine("</tbody>");
				writer.WriteLine("</table>");
				writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
			}

		}

		private void ListTruncatedData(StreamWriter writer)
		{
			if (m_rgTruncated.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksTruncatedOnImport);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"15%\">{0}</th>", LexTextControls.ksTruncatedField);
			writer.WriteLine("<th width=\"10%\">{0}</th>", LexTextControls.ksStoredLength);
			writer.WriteLine("<th width=\"15%\">{0}</th>", LexTextControls.ksWritingSystem);
			writer.WriteLine("<th width=\"20%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"40%\">{0}</th>", LexTextControls.ksOriginalValue);
			writer.WriteLine("</tr>");
			foreach (TruncatedData td in m_rgTruncated)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"15%\">{0}</td>", td.FieldName);
				writer.WriteLine("<td width=\"10%\">{0}</td>", td.StoredLength);
				writer.WriteLine("<td width=\"15%\">{0}</td>", td.WritingSystem);
				writer.WriteLine("<td width=\"20%\">{0}</td>", td.EntryHtmlReference());
				writer.WriteLine("<td width=\"40%\">{0}</td>", td.OriginalText);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
			writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
		}

		private void ListInvalidData(StreamWriter writer)
		{
			if (m_rgInvalidData.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksInvalidDataImported);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksField);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksInvalidValue);
			writer.WriteLine("<th width=\"49%\">{0}</th>", LexTextControls.ksErrorMessage);
			writer.WriteLine("</tr>");
			foreach (InvalidData bad in m_rgInvalidData)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.EntryHtmlReference());
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.FieldName);
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.BadValue);
				writer.WriteLine("<td width=\"49%\">{0}</td>", bad.ErrorMessage);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
			writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
		}

		private void ListInvalidRelations(StreamWriter writer)
		{
			if (m_rgInvalidRelation.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksInvalidRelationsHeader);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksRelationType);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksInvalidReference);
			writer.WriteLine("<th width=\"49%\">{0}</th>", LexTextControls.ksErrorMessage);
			writer.WriteLine("</tr>");
			foreach (InvalidRelation bad in m_rgInvalidRelation)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.EntryHtmlReference());
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.TypeName);
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.BadValue);
				writer.WriteLine("<td width=\"49%\">{0}</td>", bad.ErrorMessage);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
			writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
		}

		private void ListInvalidMorphTypes(StreamWriter writer)
		{
			if (m_mapMorphTypeUnknownCount.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksUnknownMorphTypes);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"83%\">{0}</th>", LexTextControls.ksName);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksReferenceCount);
			writer.WriteLine("</tr>");
			foreach (var bad in m_mapMorphTypeUnknownCount)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"83%\">{0}</td>", bad.Key);
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.Value);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
		}

		private void ListErrorMessages(StreamWriter writer)
		{
			if (m_rgErrorMsgs.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", LexTextControls.ksErrorsEncounteredHeader);
				writer.WriteLine("<ul>");
				foreach (string msg in m_rgErrorMsgs)
					writer.WriteLine("<li>{0}</li>", msg);
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewLexEntryTypes(StreamWriter writer, string sMsg, List<ILexEntryType> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (ILexEntryType type in list)
					writer.WriteLine("<li>{0} / {1}</li>", type.AbbrAndName, type.ReverseAbbr.BestAnalysisAlternative.Text);
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewPossibilities(System.IO.StreamWriter writer, string sMsg,
			List<ICmPossibility> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (ICmPossibility poss in list)
					writer.WriteLine("<li>{0}</li>", poss.AbbrAndName);
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewEnvironments(System.IO.StreamWriter writer, string sMsg,
			List<IPhEnvironment> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IPhEnvironment env in list)
					writer.WriteLine("<li>{0}</li>", env.StringRepresentation.Text);
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewWritingSystems(System.IO.StreamWriter writer, string sMsg,
			List<IWritingSystem> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IWritingSystem ws in list)
					writer.WriteLine("<li>{0} ({1})</li>", ws.DisplayLabel, ws.Id);
				writer.WriteLine("</ul>");
			}
		}
		private void ListNewInflectionClasses(System.IO.StreamWriter writer, string sMsg, List<IMoInflClass> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IMoInflClass infl in list)
				{
					string sPos = String.Empty;
					ICmObject cmo = infl.Owner;
					while (cmo != null && cmo.ClassID != PartOfSpeechTags.kClassId)
					{
						Debug.Assert(cmo.ClassID == MoInflClassTags.kClassId);
						if (cmo.ClassID == MoInflClassTags.kClassId)
						{
							IMoInflClass owner = cmo as IMoInflClass;
							sPos.Insert(0, String.Format(": {0}", owner.Name.BestAnalysisVernacularAlternative.Text));
						}
						cmo = cmo.Owner;
					}
					if (cmo != null)
					{
						IPartOfSpeech pos = cmo as IPartOfSpeech;
						sPos = sPos.Insert(0, pos.Name.BestAnalysisVernacularAlternative.Text);
					}
					writer.WriteLine("<li>{0}: {1}</li>", sPos, infl.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewSlots(System.IO.StreamWriter writer, string sMsg, List<IMoInflAffixSlot> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IMoInflAffixSlot slot in list)
				{
					string sPos = String.Empty;
					ICmObject cmo = slot.Owner;
					if (cmo != null && cmo is IPartOfSpeech)
					{
						IPartOfSpeech pos = cmo as IPartOfSpeech;
						sPos = pos.Name.BestAnalysisVernacularAlternative.Text;
					}
					writer.WriteLine("<li>{0}: {1}</li>", sPos, slot.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewInflectionalFeatures(StreamWriter writer, string sMsg, List<IFsFeatDefn> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IFsFeatDefn feat in list)
				{
					writer.WriteLine("<li>{0} - {1}</li>",
						feat.Abbreviation.BestAnalysisVernacularAlternative.Text,
						feat.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewFeatureTypes(StreamWriter writer, string sMsg, List<IFsFeatStrucType> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IFsFeatStrucType type in list)
				{
					writer.WriteLine("<li>{0} - {1}</li>",
						type.Abbreviation.BestAnalysisVernacularAlternative.Text,
						type.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewStemNames(StreamWriter writer, string sMsg, List<IMoStemName> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IMoStemName stem in list)
				{
					if (stem.Owner is IPartOfSpeech)
					{
						writer.WriteLine("<li>{0} ({1})</li>",
							stem.Name.BestAnalysisVernacularAlternative.Text,
							(stem.Owner as IPartOfSpeech).Name.BestAnalysisVernacularAlternative.Text);
					}
					else if (stem.Owner is IMoInflClass)
					{
						// YAGNI: This isn't (yet) supported by the UI.
					}
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewCustomFields(StreamWriter writer, string sMsg, List<FieldDescription> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (FieldDescription fd in list)
				{
					string sClass = m_cache.MetaDataCacheAccessor.GetClassName(fd.Class);
					writer.WriteLine("<li>{0}: {1}</li>", sClass, fd.Name);
				}
				writer.WriteLine("</ul>");
			}
		}

		#endregion // Methods for displaying list items created during import

		#region Methods for storing entry data

		/// <summary>
		/// Check whether we have an entry with the same id and the same modification time.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		private bool SameEntryModTimes(Extensible info)
		{
			Guid guid = GetGuidInExtensible(info);
			ICmObject obj = GetObjectForGuid(guid);
			if (obj != null && obj is ILexEntry)
			{
				DateTime dtMod = (obj as ILexEntry).DateModified;
				DateTime dtMod2 = dtMod.ToUniversalTime();
				DateTime dtModNew = info.ModificationTime.ToUniversalTime();
				// Only go down to the second -- ignore any millisecond or microsecond granularity.
				return (dtMod2.Date == dtModNew.Date &&
					dtMod2.Hour == dtModNew.Hour &&
					dtMod2.Minute == dtModNew.Minute &&
					dtMod2.Second == dtModNew.Second);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Create a new lexicon entry from the provided data.
		/// </summary>
		/// <param name="entry"></param>
		private void CreateNewEntry(LiftEntry entry)
		{
			try
			{
				m_fCreatingNewEntry = true;
				bool fNeedNewId;
				ILexEntry le = CreateNewLexEntry(entry.Guid, out fNeedNewId);
				if (m_cdConflict != null && m_cdConflict is ConflictingEntry)
				{
					(m_cdConflict as ConflictingEntry).DupEntry = le;
					m_rgcdConflicts.Add(m_cdConflict);
					m_cdConflict = null;
				}
				StoreEntryId(le, entry);
				le.HomographNumber = entry.Order;
				CreateLexemeForm(le, entry);		// also sets CitationForm if it exists.
				if (fNeedNewId)
				{
					XmlDocument xdEntryResidue = FindOrCreateResidue(le, entry.Id, LexEntryTags.kflidLiftResidue);
					XmlAttribute xa = xdEntryResidue.FirstChild.Attributes["id"];
					if (xa == null)
					{
						xa = xdEntryResidue.CreateAttribute("id");
						xdEntryResidue.FirstChild.Attributes.Append(xa);
					}
					xa.Value = le.LIFTid;
				}
				ProcessEntryTraits(le, entry);
				ProcessEntryNotes(le, entry);
				ProcessEntryFields(le, entry);
				CreateEntryVariants(le, entry);
				CreateEntryPronunciations(le, entry);
				ProcessEntryEtymologies(le, entry);
				ProcessEntryRelations(le, entry);
				foreach (LiftSense sense in entry.Senses)
					CreateEntrySense(le, sense);
				if (entry.DateCreated != default(DateTime))
					le.DateCreated = entry.DateCreated;
				if (entry.DateModified != default(DateTime))
					m_rgPendingModifyTimes.Add(new PendingModifyTime(le, entry.DateModified));
				StoreAnnotationsAndDatesInResidue(le, entry);
				FinishProcessingEntry(le);
				++m_cEntriesAdded;
			}
			finally
			{
				m_fCreatingNewEntry = false;
			}
		}

		private void StoreEntryId(ILexEntry le, LiftEntry entry)
		{
			if (!String.IsNullOrEmpty(entry.Id))
			{
				FindOrCreateResidue(le, entry.Id, LexEntryTags.kflidLiftResidue);
				MapIdToObject(entry.Id, le);
			}
		}

		private void MapIdToObject(string id, ICmObject cmo)
		{
			try
			{
				m_mapIdObject.Add(id, cmo);
			}
			catch (ArgumentException ex)
			{
				// presumably duplicate id.
				ICmObject cmo2;
				string msg = null;
				if (m_mapIdObject.TryGetValue(id, out cmo2))
				{
					if (cmo != cmo2)
					{
						msg = String.Format(LexTextControls.ksDuplicateIdValue,
							cmo.ClassName, id);
					}
				}
				if (String.IsNullOrEmpty(msg))
					msg = String.Format(LexTextControls.ksProblemId, cmo.ClassName, id);
				m_rgErrorMsgs.Add(msg);
			}
		}

		/// <summary>
		/// Store accumulated import residue for the entry (and its senses), and ensure
		/// that all senses have an MSA, and that duplicate MSAs are merged together.
		/// </summary>
		/// <param name="le"></param>
		private void FinishProcessingEntry(ILexEntry le)
		{
			// We don't create/assign MSAs to senses if <grammatical-info> doesn't exist.
			EnsureValidMSAsForSenses(le);
			// The next line of code is commented out in 6.0 because it may be finding and
			// fixing lots of redundancies that we didn't create.  In fact, we shouldn't be
			// creating any redundancies any more -- there's lots of code looking for matching
			// MSAs to reuse!
			//(le as LexEntry).MergeRedundantMSAs();
		}

		private void WriteAccumulatedResidue()
		{
			foreach (int hvo in m_dictResidue.Keys)
			{
				LiftResidue res = m_dictResidue[hvo];
				string sLiftResidue = res.Document.OuterXml;
				int flid = res.Flid;
				if (!String.IsNullOrEmpty(sLiftResidue) && flid != 0)
					m_cache.MainCacheAccessor.set_UnicodeProp(hvo, flid, sLiftResidue);
			}
			m_dictResidue.Clear();
		}

		/// <summary>
		/// Check whether an existing entry has data that conflicts with an imported entry that
		/// has the same identity (guid).  Senses are not checked, since they can be added to
		/// the existing entry instead of creating an entirely new entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns>true if a conflict exists, otherwise false</returns>
		private bool EntryHasConflictingData(LiftEntry entry)
		{
			m_cdConflict = null;
			ILexEntry le = entry.CmObject as ILexEntry;
			if (LexemeFormsConflict(le, entry))
				return true;
			if (EntryEtymologiesConflict(le.EtymologyOA, entry.Etymologies))
			{
				m_cdConflict = new ConflictingEntry("Etymology", le, this);
				return true;
			}
			if (EntryFieldsConflict(le, entry.Fields))
				return true;
			if (EntryNotesConflict(le, entry.Notes))
				return true;
			if (EntryPronunciationsConflict(le, entry.Pronunciations))
				return true;
			if (EntryTraitsConflict(le, entry.Traits))
				return true;
			if (EntryVariantsConflict(le, entry.Variants))
				return true;
			//entry.DateCreated;
			//entry.DateModified;
			//entry.Order;
			//entry.Relations;
			return false;
		}

		/// <summary>
		/// Add the imported data to an existing lexical entry.
		/// </summary>
		/// <param name="entry"></param>
		private void MergeIntoExistingEntry(LiftEntry entry)
		{

			ILexEntry le = entry.CmObject as ILexEntry;
			StoreEntryId(le, entry);
			le.HomographNumber = entry.Order;
			MergeLexemeForm(le, entry);		// also sets CitationForm if it exists.
			ProcessEntryTraits(le, entry);
			ProcessEntryNotes(le, entry);
			ProcessEntryFields(le, entry);
			MergeEntryVariants(le, entry);
			MergeEntryPronunciations(le, entry);
			ProcessEntryEtymologies(le, entry);
			ProcessEntryRelations(le, entry);
			Dictionary<LiftSense, ILexSense> map = new Dictionary<LiftSense, ILexSense>();
			Set<int> setUsed = new Set<int>();
			foreach (LiftSense sense in entry.Senses)
			{
				ILexSense ls = FindExistingSense(le.SensesOS, sense);
				map.Add(sense, ls);
				if (ls != null)
					setUsed.Add(ls.Hvo);
			}
			// If we're keeping only the imported data, delete any unused senses.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in le.SensesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftSense sense in entry.Senses)
			{
				ILexSense ls;
				map.TryGetValue(sense, out ls);
				if (ls == null || (m_msImport == MergeStyle.MsKeepBoth && SenseHasConflictingData(ls, sense)))
					CreateEntrySense(le, sense);
				else
					MergeIntoExistingSense(ls, sense);
			}
			if (entry.DateCreated != default(DateTime))
				le.DateCreated = entry.DateCreated;
			if (entry.DateModified != default(DateTime))
				m_rgPendingModifyTimes.Add(new PendingModifyTime(le, entry.DateModified));
			StoreAnnotationsAndDatesInResidue(le, entry);
			FinishProcessingEntry(le);
		}

		private ILexSense FindExistingSense(IFdoOwningSequence<ILexSense> rgsenses, LiftSense sense)
		{
			if (sense.CmObject == null)
				return null;
			foreach (ILexSense ls in rgsenses)
			{
				if (ls.Hvo == sense.CmObject.Hvo)
					return ls;
			}
			return null;
		}

		private bool SenseHasConflictingData(ILexSense ls, LiftSense sense)
		{
			m_cdConflict = null;
			//sense.Order;
			if (MultiUnicodeStringsConflict(ls.Gloss, sense.Gloss, false, Guid.Empty, 0))
			{
				m_cdConflict = new ConflictingSense("Gloss", ls, this);
				return true;
			}
			if (MultiTsStringsConflict(ls.Definition, sense.Definition))
			{
				m_cdConflict = new ConflictingSense("Definition", ls, this);
				return true;
			}
			if (SenseExamplesConflict(ls, sense.Examples))
				return true;
			if (SenseGramInfoConflicts(ls, sense.GramInfo))
				return true;
			if (SenseIllustrationsConflict(ls, sense.Illustrations))
				return true;
			if (SenseNotesConflict(ls, sense.Notes))
				return true;
			if (SenseRelationsConflict(ls, sense.Relations))
				return true;
			if (SenseReversalsConflict(ls, sense.Reversals))
				return true;
			if (SenseTraitsConflict(ls, sense.Traits))
				return true;
			if (SenseFieldsConflict(ls, sense.Fields))
				return true;
			return false;
		}

		private void CreateLexemeForm(ILexEntry le, LiftEntry entry)
		{
			if (entry.LexicalForm != null && !entry.LexicalForm.IsEmpty)
			{
				IMoMorphType mmt;
				string realForm;
				ITsString tssForm = GetFirstLiftTsString(entry.LexicalForm);
				IMoForm mf = CreateMoForm(entry.Traits, tssForm, out mmt, out realForm,
					le.Guid, LexEntryTags.kflidLexemeForm);
				le.LexemeFormOA = mf;
				FinishMoForm(mf, entry.LexicalForm, tssForm, mmt, realForm,
					le.Guid, LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm,
					le.LexemeFormOA == null ? MoStemAllomorphTags.kClassId : le.LexemeFormOA.ClassID,
					le.Guid, LexEntryTags.kflidCitationForm);
			}
		}

		private IMoMorphType FindMorphType(ref string form, out int clsid, Guid guidEntry, int flid)
		{
			string fullForm = form;
			try
			{
				return MorphServices.FindMorphType(m_cache, ref form, out clsid);
			}
			catch (Exception error)
			{
				InvalidData bad = new InvalidData(error.Message, guidEntry, flid, fullForm, 0, m_cache, this);
				if (!m_rgInvalidData.Contains(bad))
					m_rgInvalidData.Add(bad);
				form = fullForm;
				clsid = MoStemAllomorphTags.kClassId;
				return GetExistingMoMorphType(MoMorphTypeTags.kguidMorphStem);
			}
		}

		private void MergeLexemeForm(ILexEntry le, LiftEntry entry)
		{
			if (entry.LexicalForm != null && !entry.LexicalForm.IsEmpty)
			{
				IMoForm mf = le.LexemeFormOA;
				int clsid = 0;
				if (mf == null)
				{
					string form = entry.LexicalForm.FirstValue.Value.Text;
					IMoMorphType mmt = FindMorphType(ref form, out clsid,
						le.Guid, LexEntryTags.kflidLexemeForm);
					if (mmt.IsAffixType)
						mf = CreateNewMoAffixAllomorph();
					else
						mf = CreateNewMoStemAllomorph();
					le.LexemeFormOA = mf;
					mf.MorphTypeRA = mmt;
				}
				else
				{
					clsid = mf.ClassID;
				}
				MergeInAllomorphForms(entry.LexicalForm, mf.Form, clsid, le.Guid,
					LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm,
					le.LexemeFormOA == null ? MoStemAllomorphTags.kClassId : le.LexemeFormOA.ClassID,
					le.Guid, LexEntryTags.kflidCitationForm);
			}
		}

		private bool LexemeFormsConflict(ILexEntry le, LiftEntry entry)
		{
			if (MultiUnicodeStringsConflict(le.CitationForm, entry.CitationForm, true,
				le.Guid, LexEntryTags.kflidCitationForm))
			{
				m_cdConflict = new ConflictingEntry("Citation Form", le, this);
				return true;
			}
			if (le.LexemeFormOA != null)
			{
				if (MultiUnicodeStringsConflict(le.LexemeFormOA.Form, entry.LexicalForm, true,
					le.Guid, LexEntryTags.kflidLexemeForm))
				{
					m_cdConflict = new ConflictingEntry("Lexeme Form", le, this);
					return true;
				}
			}
			return false;
		}

		private void ProcessEntryTraits(ILexEntry le, LiftEntry entry)
		{
			foreach (LiftTrait lt in entry.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "entrytype":	// original FLEX export = EntryType
					case "entry-type":
						// Save this for use with a <relation type="main" ...> to create LexEntryRef later.
						entry.EntryType = lt.Value;
						break;
					case "morphtype":	// original FLEX export = MorphType
					case "morph-type":
						ProcessEntryMorphType(le, lt.Value);
						break;
					case "minorentrycondition":		// original FLEX export = MinorEntryCondition
					case "minor-entry-condition":
						// Save this for use with a <relation type="main" ...> to create LexEntryRef later.
						entry.MinorEntryCondition = lt.Value;
						break;
					case "excludeasheadword":	// original FLEX export = ExcludeAsHeadword
					case "exclude-as-headword":
						bool fExclude = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.ExcludeAsHeadword != fExclude && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld))
							le.ExcludeAsHeadword = fExclude;
						// if EntryType is set, this may be used to initialize HideMinorEntry in a LexEntryRef.
						entry.ExcludeAsHeadword = fExclude;
						break;
					case "donotuseforparsing":	// original FLEX export = DoNotUseForParsing
					case "do-not-use-for-parsing":
						bool fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.DoNotUseForParsing != fDontUse && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld))
							le.DoNotUseForParsing = fDontUse;
						break;
					default:
						StoreTraitAsResidue(le, lt);
						break;
				}
			}
		}

		private void ProcessEntryMorphType(ILexEntry le, string traitValue)
		{
			IMoMorphType mmt = FindMorphType(traitValue);
			if (le.LexemeFormOA == null)
			{
				if (mmt.IsAffixType)
					le.LexemeFormOA = CreateNewMoAffixAllomorph();
				else
					le.LexemeFormOA = CreateNewMoStemAllomorph();
				le.LexemeFormOA.MorphTypeRA = mmt;
			}
			else if (le.LexemeFormOA.MorphTypeRA != mmt &&
				(m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld || le.LexemeFormOA.MorphTypeRA == null))
			{
				if (mmt.IsAffixType)
				{
					if (le.LexemeFormOA is IMoStemAllomorph)
						le.ReplaceMoForm(le.LexemeFormOA, CreateNewMoAffixAllomorph());
				}
				else
				{
					if (!(le.LexemeFormOA is IMoStemAllomorph))
						le.ReplaceMoForm(le.LexemeFormOA, CreateNewMoStemAllomorph());
				}
				le.LexemeFormOA.MorphTypeRA = mmt;
			}
		}

		private bool EntryTraitsConflict(ILexEntry le, List<LiftTrait> list)
		{
			foreach (LiftTrait lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "entrytype":	// original FLEX export = EntryType
					case "entry-type":
						// This trait is no longer used by FLEX.
						break;
					case "morphtype":	// original FLEX export = MorphType
					case "morph-type":
						if (le.LexemeFormOA != null && le.LexemeFormOA.MorphTypeRA != null)
						{
							IMoMorphType mmt = FindMorphType(lt.Value);
							if (le.LexemeFormOA.MorphTypeRA != mmt)
							{
								m_cdConflict = new ConflictingEntry("Morph Type", le, this);
								return true;
							}
						}
						break;
					case "minorentrycondition":		// original FLEX export = MinorEntryCondition
					case "minor-entry-condition":
						// This trait is no longer used by FLEX.
						break;
					case "excludeasheadword":	// original FLEX export = ExcludeAsHeadword
					case "exclude-as-headword":
						bool fExclude = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.ExcludeAsHeadword != fExclude)
						{
							m_cdConflict = new ConflictingEntry("Exclude As Headword", le, this);
							return true;
						}
						break;
					case "donotuseforparsing":	// original FLEX export = DoNotUseForParsing
					case "do-not-use-for-parsing":
						bool fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.DoNotUseForParsing != fDontUse)
						{
							m_cdConflict = new ConflictingEntry("Do Not Use For Parsing", le, this);
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessEntryNotes(ILexEntry le, LiftEntry entry)
		{
			foreach (LiftNote note in entry.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "bibliography":
						MergeInMultiString(le.Bibliography, LexEntryTags.kflidBibliography, note.Content, le.Guid);
						break;
					case "":		// WeSay uses untyped notes in entries; LIFT now exports like this.
					case "comment":	// older Flex exported LIFT files have this type value.
						MergeInMultiString(le.Comment, LexEntryTags.kflidComment, note.Content, le.Guid);
						break;
					case "restrictions":
						MergeInMultiUnicode(le.Restrictions, LexEntryTags.kflidRestrictions, note.Content, le.Guid);
						break;
					default:
						StoreNoteAsResidue(le, note);
						break;
				}
			}
		}

		private bool EntryNotesConflict(ILexEntry le, List<LiftNote> list)
		{
			foreach (LiftNote note in list)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "bibliography":
						if (MultiTsStringsConflict(le.Bibliography, note.Content))
						{
							m_cdConflict = new ConflictingEntry("Bibliography", le, this);
							return true;
						}
						break;
					case "comment":
						if (MultiTsStringsConflict(le.Comment, note.Content))
						{
							m_cdConflict = new ConflictingEntry("Note", le, this);
							return true;
						}
						break;
					case "restrictions":
						if (MultiUnicodeStringsConflict(le.Restrictions, note.Content, false, Guid.Empty, 0))
						{
							m_cdConflict = new ConflictingEntry("Restrictions", le, this);
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessEntryFields(ILexEntry le, LiftEntry entry)
		{
			foreach (LiftField lf in entry.Fields)
			{
				switch (lf.Type.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						le.ImportResidue = StoreTsStringValue(m_fCreatingNewEntry, le.ImportResidue, lf.Content);
						break;
					case "literal_meaning":	// original FLEX export
					case "literal-meaning":
						MergeInMultiString(le.LiteralMeaning, LexEntryTags.kflidLiteralMeaning, lf.Content, le.Guid);
						break;
					case "summary_definition":	// original FLEX export
					case "summary-definition":
						MergeInMultiString(le.SummaryDefinition, LexEntryTags.kflidSummaryDefinition, lf.Content, le.Guid);
						break;
					default:
						ProcessUnknownField(le, entry, lf,
							"LexEntry", "custom-entry-", LexEntryTags.kClassId);
						break;
				}
			}
		}

		private bool EntryFieldsConflict(ILexEntry le, List<LiftField> list)
		{
			foreach (LiftField lf in list)
			{
				switch (lf.Type.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						if (le.ImportResidue != null && le.ImportResidue.Length != 0)
						{
							ITsStrBldr tsb = le.ImportResidue.GetBldr();
							int idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
								tsb.Replace(idx, tsb.Length, null, null);
							if (StringsConflict(tsb.GetString(), GetFirstLiftTsString(lf.Content)))
							{
								m_cdConflict = new ConflictingEntry("Import Residue", le, this);
								return true;
							}
						}
						break;
					case "literal_meaning":	// original FLEX export
					case "literal-meaning":
						if (MultiTsStringsConflict(le.LiteralMeaning, lf.Content))
						{
							m_cdConflict = new ConflictingEntry("Literal Meaning", le, this);
							return true;
						}
						break;
					case "summary_definition":	// original FLEX export
					case "summary-definition":
						if (MultiTsStringsConflict(le.SummaryDefinition, lf.Content))
						{
							m_cdConflict = new ConflictingEntry("Summary Definition", le, this);
							return true;
						}
						break;
					default:
						int flid;
						if (m_dictCustomFlid.TryGetValue("LexEntry-" + lf.Type, out flid))
						{
							if (CustomFieldDataConflicts(le.Hvo, flid, lf.Content))
							{
								m_cdConflict = new ConflictingEntry(String.Format("{0} (custom field)", lf.Type), le, this);
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessCustomFieldData(int hvo, int flid, LiftMultiText contents)
		{
			CellarPropertyType type = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			ICmObject cmo = GetObjectForId(hvo);
			ITsMultiString tsm;
			switch (type)
			{
				case CellarPropertyType.String:
				case CellarPropertyType.BigString:
					ITsString tss = StoreTsStringValue(m_fCreatingNewEntry | m_fCreatingNewSense,
						m_cache.MainCacheAccessor.get_StringProp(hvo, flid), contents);
					m_cache.MainCacheAccessor.SetString(hvo, flid, tss);
					break;
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiBigString:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					MergeInMultiString(tsm, flid, contents, cmo.Guid);
					break;
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiBigUnicode:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					MergeInMultiUnicode(tsm, flid, contents, cmo.Guid);
					break;
				default:
					// TODO: Warn user he's smarter than we are?
					break;
			}
		}

		private bool CustomFieldDataConflicts(int hvo, int flid, LiftMultiText contents)
		{
			CellarPropertyType type = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			ITsMultiString tsm;
			switch (type)
			{
				case CellarPropertyType.String:
				case CellarPropertyType.BigString:
					ITsString tss = m_cache.MainCacheAccessor.get_StringProp(hvo, flid);
					if (StringsConflict(tss, GetFirstLiftTsString(contents)))
						return true;
					break;
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiBigString:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					if (MultiTsStringsConflict(tsm, contents))
						return true;
					break;
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiBigUnicode:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					if (MultiUnicodeStringsConflict(tsm, contents, false, Guid.Empty, 0))
						return true;
					break;
				default:
					break;
			}
			return false;
		}

		private void CreateEntryVariants(ILexEntry le, LiftEntry entry)
		{
			foreach (LiftVariant lv in entry.Variants)
			{
				ITsString tssForm = GetFirstLiftTsString(lv.Form);
				IMoMorphType mmt;
				string realForm;
				IMoForm mf = CreateMoForm(lv.Traits, tssForm, out mmt, out realForm,
					le.Guid, LexEntryTags.kflidAlternateForms);
				le.AlternateFormsOS.Add(mf);
				FinishMoForm(mf, lv.Form, tssForm, mmt, realForm,
					le.Guid, LexEntryTags.kflidAlternateForms);
				bool fTypeSpecified;
				ProcessMoFormTraits(mf, lv, out fTypeSpecified);
				ProcessMoFormFields(mf, lv);
				StoreResidueFromVariant(mf, lv);
				if (!fTypeSpecified)
					mf.MorphTypeRA = null;
			}
		}

		private IMoForm CreateMoForm(List<LiftTrait> traits, ITsString tssForm,
			out IMoMorphType mmt, out string realForm, Guid guidEntry, int flid)
		{
			// Try to create the proper type of allomorph form to begin with.  It takes over
			// 200ms to delete one we just created!  (See LT-9006.)
			int clsidForm;
			if (tssForm == null || tssForm.Text == null)
			{
				clsidForm = MoStemAllomorphTags.kClassId;
				ICmObject cmo = GetObjectForGuid(MoMorphTypeTags.kguidMorphStem);
				mmt = cmo as IMoMorphType;
				realForm = null;
			}
			else
			{
				realForm = tssForm.Text;
				mmt = FindMorphType(ref realForm, out clsidForm, guidEntry, flid);
			}
			IMoMorphType mmt2;
			int clsidForm2 = GetMoFormClassFromTraits(traits, out mmt2);
			if (clsidForm2 != 0 && mmt2 != null)
			{
				if (mmt2 != mmt)
					mmt = mmt2;
				clsidForm = clsidForm2;
			}
			switch (clsidForm)
			{
				case MoStemAllomorphTags.kClassId:
					return CreateNewMoStemAllomorph();
				case MoAffixAllomorphTags.kClassId:
					return CreateNewMoAffixAllomorph();
				default:
					throw new InvalidProgramException(
						"unexpected MoForm subclass returned from FindMorphType or GetMoFormClassFromTraits");
			}
		}

		private void FinishMoForm(IMoForm mf, LiftMultiText forms, ITsString tssForm, IMoMorphType mmt,
			string realForm, Guid guidEntry, int flid)
		{
			mf.MorphTypeRA = mmt; // Has to be done, before the next call.
			ITsString tssRealForm;
			if (tssForm != null)
			{
				if (tssForm.Text != realForm)
				{
					// make a new tsString with the old ws.
					tssRealForm = StringUtils.MakeTss(realForm,
						StringUtils.GetWsAtOffset(tssForm, 0));
				}
				else
				{
					tssRealForm = tssForm;
				}
				mf.FormMinusReservedMarkers = tssRealForm;
			}
			MergeInAllomorphForms(forms, mf.Form, mf.ClassID, guidEntry, flid);
		}

		private void MergeEntryVariants(ILexEntry le, LiftEntry entry)
		{
			Dictionary<int, LiftVariant> dictHvoVariant = new Dictionary<int, LiftVariant>();
			foreach (LiftVariant lv in entry.Variants)
			{
				IMoForm mf = FindMatchingMoForm(le, dictHvoVariant, lv,
					le.Guid, LexEntryTags.kflidAlternateForms);
				if (mf == null)
				{
					ITsString tssForm = GetFirstLiftTsString(lv.Form);
					if (tssForm == null || tssForm.Text == null)
						continue;
					IMoMorphType mmt;
					string realForm = tssForm.Text;
					mf = CreateMoForm(lv.Traits, tssForm, out mmt, out realForm,
						le.Guid, LexEntryTags.kflidAlternateForms);
					le.AlternateFormsOS.Add(mf);
					FinishMoForm(mf, lv.Form, tssForm, mmt, realForm,
						le.Guid, LexEntryTags.kflidAlternateForms);
					dictHvoVariant.Add(mf.Hvo, lv);
				}
				else
				{
					MergeInAllomorphForms(lv.Form, mf.Form, mf.ClassID,
						le.Guid, LexEntryTags.kflidAlternateForms);
				}
				bool fTypeSpecified;
				ProcessMoFormTraits(mf, lv, out fTypeSpecified);
				ProcessMoFormFields(mf, lv);
				StoreResidueFromVariant(mf, lv);
				if (!fTypeSpecified)
					mf.MorphTypeRA = null;
			}
		}

		private bool EntryVariantsConflict(ILexEntry le, List<LiftVariant> list)
		{
			if (le.AlternateFormsOS.Count == 0 || list.Count == 0)
				return false;
			int cCommon = 0;
			Dictionary<int, LiftVariant> dictHvoVariant = new Dictionary<int, LiftVariant>();
			foreach (LiftVariant lv in list)
			{
				IMoForm mf = FindMatchingMoForm(le, dictHvoVariant, lv,
					le.Guid, LexEntryTags.kflidAlternateForms);
				if (mf != null)
				{
					if (MultiUnicodeStringsConflict(mf.Form, lv.Form, true,
						le.Guid, LexEntryTags.kflidAlternateForms))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Alternate Form ({0})",
							TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)), le, this);
						return true;
					}
					if (MoFormTraitsConflict(mf, lv.Traits))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Alternate Form ({0}) details",
							TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)), le, this);
						return true;
					}
					++cCommon;
				}
			}
			if (cCommon < Math.Min(le.AlternateFormsOS.Count, list.Count))
			{
				m_cdConflict = new ConflictingEntry("Alternate Forms", le, this);
				return true;
			}
			else
			{
				return false;
			}
		}

		private IMoForm FindMatchingMoForm(ILexEntry le, Dictionary<int, LiftVariant> dictHvoVariant,
			LiftVariant lv, Guid guidEntry, int flid)
		{
			IMoForm form = null;
			int cMatches = 0;
			foreach (IMoForm mf in le.AlternateFormsOS)
			{
				if (dictHvoVariant.ContainsKey(mf.Hvo))
					continue;
				int cCurrent = MultiUnicodeStringMatches(mf.Form, lv.Form, true, guidEntry, flid);
				if (cCurrent > cMatches)
				{
					form = mf;
					cMatches = cCurrent;
				}
			}
			if (form != null)
				dictHvoVariant.Add(form.Hvo, lv);
			return form;
		}

		private int GetMoFormClassFromTraits(List<LiftTrait> traits, out IMoMorphType mmt)
		{
			mmt = null;
			foreach (LiftTrait lt in traits)
			{
				if (lt.Name.ToLowerInvariant() == "morphtype" ||
					lt.Name.ToLowerInvariant() == "morph-type")
				{
					mmt = FindMorphType(lt.Value);
					bool fAffix = mmt.IsAffixType;
					if (fAffix)
						return MoAffixAllomorphTags.kClassId;
					else
						return MoStemAllomorphTags.kClassId;
				}
			}
			return 0;	// no subclass info in the traits
		}

		private void ProcessMoFormTraits(IMoForm form, LiftVariant variant, out bool fTypeSpecified)
		{
			fTypeSpecified = false;
			foreach (LiftTrait lt in variant.Traits)
			{
				IMoMorphType mmt = null;
				switch (lt.Name.ToLowerInvariant())
				{
					case "morphtype":	// original FLEX export = MorphType
					case "morph-type":
						if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld && form.MorphTypeRA != null)
							continue;
						mmt = FindMorphType(lt.Value);
						bool fAffix = mmt.IsAffixType;
						if (fAffix && form is IMoStemAllomorph)
						{
							IMoStemAllomorph stem = form as IMoStemAllomorph;
							IMoAffixAllomorph affix = CreateNewMoAffixAllomorph();
							ILexEntry entry = form.Owner as ILexEntry;
							Debug.Assert(entry != null);
							entry.ReplaceMoForm(stem, affix);
							form = affix;
						}
						else if (!fAffix && form is IMoAffixAllomorph)
						{
							IMoAffixAllomorph affix = form as IMoAffixAllomorph;
							IMoStemAllomorph stem = CreateNewMoStemAllomorph();
							ILexEntry entry = form.Owner as ILexEntry;
							Debug.Assert(entry != null);
							entry.ReplaceMoForm(affix, stem);
							form = stem;
						}
						if (mmt != form.MorphTypeRA)
							form.MorphTypeRA = mmt;
						fTypeSpecified = true;
						break;
					case "environment":
						List<IPhEnvironment> rgenv = FindOrCreateEnvironment(lt.Value);
						if (form is IMoStemAllomorph)
						{
							AddEnvironmentIfNeeded(rgenv, (form as IMoStemAllomorph).PhoneEnvRC);
						}
						else if (form is IMoAffixAllomorph)
						{
							AddEnvironmentIfNeeded(rgenv, (form as IMoAffixAllomorph).PhoneEnvRC);
						}
						break;
					default:
						StoreTraitAsResidue(form, lt);
						break;
				}
			}
		}

		private static void AddEnvironmentIfNeeded(List<IPhEnvironment> rgnew, IFdoReferenceCollection<IPhEnvironment> rgenv)
		{
			if (rgenv != null && rgnew != null)
			{
				bool fAlready = false;
				foreach (IPhEnvironment env in rgnew)
				{
					if (rgenv.Contains(env))
					{
						fAlready = true;
						break;
					}
				}
				if (!fAlready && rgnew.Count > 0)
					rgenv.Add(rgnew[0]);
			}
		}

		private bool MoFormTraitsConflict(IMoForm mf, List<LiftTrait> list)
		{
			foreach (LiftTrait lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "morphtype":	// original FLEX export = MorphType
					case "morph-type":
						if (mf.MorphTypeRA != null)
						{
							IMoMorphType mmt = FindMorphType(lt.Value);
							if (mf.MorphTypeRA != mmt)
								return true;
						}
						break;
					case "environment":
						if (mf is IMoStemAllomorph)
						{
							if ((mf as IMoStemAllomorph).PhoneEnvRC.Count > 0)
							{
								//int hvo = FindOrCreateEnvironment(lt.Value);
							}
						}
						else if (mf is IMoAffixAllomorph)
						{
							if ((mf as IMoAffixAllomorph).PhoneEnvRC.Count > 0)
							{
								//int hvo = FindOrCreateEnvironment(lt.Value);
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessMoFormFields(IMoForm mf, LiftVariant lv)
		{
			foreach (LiftField field in lv.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					default:
						ProcessUnknownField(mf, lv, field,
							"MoForm", "custom-variant-", MoFormTags.kClassId);
						break;
				}
			}
		}

		private void CreateEntryPronunciations(ILexEntry le, LiftEntry entry)
		{
			foreach (LiftPhonetic phon in entry.Pronunciations)
			{
				ILexPronunciation pron = CreateNewLexPronunciation();
				le.PronunciationsOS.Add(pron);
				MergeInMultiUnicode(pron.Form, LexPronunciationTags.kflidForm, phon.Form, pron.Guid);
				MergePronunciationMedia(pron, phon);
				ProcessPronunciationFieldsAndTraits(pron, phon);
				StoreAnnotationsAndDatesInResidue(pron, phon);
				SavePronunciationWss(phon.Form.Keys);
			}
		}

		private void MergeEntryPronunciations(ILexEntry le, LiftEntry entry)
		{
			Dictionary<int, LiftPhonetic> dictHvoPhon = new Dictionary<int, LiftPhonetic>();
			foreach (LiftPhonetic phon in entry.Pronunciations)
			{
				ILexPronunciation pron = FindMatchingPronunciation(le, dictHvoPhon, phon);
				if (pron == null)
				{
					pron = CreateNewLexPronunciation();
					le.PronunciationsOS.Add(pron);
					dictHvoPhon.Add(pron.Hvo, phon);
				}
				MergeInMultiUnicode(pron.Form, LexPronunciationTags.kflidForm, phon.Form, pron.Guid);
				MergePronunciationMedia(pron, phon);
				ProcessPronunciationFieldsAndTraits(pron, phon);
				StoreAnnotationsAndDatesInResidue(pron, phon);
				SavePronunciationWss(phon.Form.Keys);
			}
		}

		private void SavePronunciationWss(Dictionary<string, LiftString>.KeyCollection langs)
		{
			foreach (string lang in langs)
			{
				int ws = GetWsFromLiftLang(lang);
				if (ws != 0)
				{
					IWritingSystem wsObj = GetExistingWritingSystem(ws);
					if (!m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Contains(wsObj))
						m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Add(wsObj);
				}
			}
		}

		private void MergePronunciationMedia(ILexPronunciation pron, LiftPhonetic phon)
		{
			foreach (LiftUrlRef uref in phon.Media)
			{
				string sFile = uref.Url;
				sFile = sFile.Replace('/', '\\');
				int ws = 0;
				string sLabel;
				if (uref.Label != null && !uref.Label.IsEmpty)
				{
					ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
					sLabel = uref.Label.FirstValue.Value.Text;
				}
				else
				{
					ws = m_cache.DefaultVernWs;
					sLabel = null;
				}
				ICmMedia media = FindMatchingMedia(pron.MediaFilesOS, sFile, uref.Label);
				if (media == null)
				{
					media = CreateNewCmMedia();
					pron.MediaFilesOS.Add(media);
					/*string sLiftDir = */Path.GetDirectoryName(m_sLiftFile);
					// Paths to try for resolving given filename:
					// {directory of LIFT file}/audio/filename
					// {FW LinkedFilesRootDir}/filename
					// {FW LinkedFilesRootDir}/Media/filename
					// {FW DataDir}/filename
					// {FW DataDir}/Media/filename
					// give up and store relative path Pictures/filename (even though it doesn't exist)
					string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
						String.Format("audio{0}{1}", Path.DirectorySeparatorChar, sFile));
					if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
					{
						sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sFile);
						if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
						{
							sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir,
								String.Format("Media{0}{1}", Path.DirectorySeparatorChar, sFile));
							if (!File.Exists(sPath))
							{
								sPath = Path.Combine(DirectoryFinder.FWDataDirectory, sFile);
								if (!File.Exists(sPath))
									sPath = Path.Combine(DirectoryFinder.FWDataDirectory,
										String.Format("Media{0}{1}", Path.DirectorySeparatorChar, sFile));
							}
						}
					}
					try
					{
						if (!String.IsNullOrEmpty(sLabel))
							media.Label.set_String(ws, sLabel);
						if (!String.IsNullOrEmpty(sPath))
						{
							ICmFolder cmfMedia = null;
							foreach (ICmFolder cmf in m_cache.LangProject.MediaOC)
							{
								for (int i = 0; i < cmf.Name.StringCount; ++i)
								{
									int wsT;
									ITsString tss = cmf.Name.GetStringFromIndex(i, out wsT);
									if (tss.Text == StringUtils.LocalMedia)
									{
										cmfMedia = cmf;
										break;
									}
								}
								if (cmfMedia != null)
									break;
							}
							if (cmfMedia == null)
							{
								ICmFolderFactory factFolder = m_cache.ServiceLocator.GetInstance<ICmFolderFactory>();
								cmfMedia = factFolder.Create();
								m_cache.LangProject.MediaOC.Add(cmfMedia);
								cmfMedia.Name.UserDefaultWritingSystem = m_cache.TsStrFactory.MakeString(StringUtils.LocalMedia, m_cache.DefaultUserWs);
							}
							ICmFile file = null;
							foreach (ICmFile cf in cmfMedia.FilesOC)
							{
								if (cf.AbsoluteInternalPath == sPath)
								{
									file = cf;
									break;
								}
							}
							if (file == null)
							{
								ICmFileFactory factFile = m_cache.ServiceLocator.GetInstance<ICmFileFactory>();
								file = factFile.Create();
								cmfMedia.FilesOC.Add(file);
								file.InternalPath = sPath;
							}
							media.MediaFileRA = file;
						}
					}
					catch (ArgumentException ex)
					{
						// If sFile is empty, trying to create the CmFile for the audio/media file will throw.
						// We don't care about this error as the caption will still be set properly.
						Debug.WriteLine("Error initializing media: " + ex.Message);
					}
					if (!File.Exists(sPath))
					{
						media.MediaFileRA.InternalPath =
							String.Format("Media{0}{1}", Path.DirectorySeparatorChar, sFile);
					}
				}
				MergeInMultiString(media.Label, CmMediaTags.kflidLabel, uref.Label, media.Guid);
			}
		}

		private ICmMedia FindMatchingMedia(IFdoOwningSequence<ICmMedia> rgmedia, string sFile,
			LiftMultiText lmtLabel)
		{
			ICmMedia mediaMatching = null;
			int cMatches = 0;
			foreach (ICmMedia media in rgmedia)
			{
				if (media.MediaFileRA == null)
					continue;	// should NEVER happen!
				if (media.MediaFileRA.InternalPath == sFile ||
					Path.GetFileName(media.MediaFileRA.InternalPath) == sFile)
				{
					int cCurrent = MultiTsStringMatches(media.Label, lmtLabel);
					if (cCurrent >= cMatches)
					{
						mediaMatching = media;
						cMatches = cCurrent;
					}
				}
			}
			return mediaMatching;

		}

		private bool EntryPronunciationsConflict(ILexEntry le, List<LiftPhonetic> list)
		{
			if (le.PronunciationsOS.Count == 0 || list.Count == 0)
				return false;
			int cCommon = 0;
			Dictionary<int, LiftPhonetic> dictHvoPhon = new Dictionary<int, LiftPhonetic>();
			foreach (LiftPhonetic phon in list)
			{
				ILexPronunciation pron = FindMatchingPronunciation(le, dictHvoPhon, phon);
				if (pron != null)
				{
					if (MultiUnicodeStringsConflict(pron.Form, phon.Form, false, Guid.Empty, 0))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Pronunciation ({0})",
							TsStringAsHtml(pron.Form.BestVernacularAnalysisAlternative, m_cache)), le, this);
						return true;
					}
					if (PronunciationFieldsOrTraitsConflict(pron, phon))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Pronunciation ({0}) details",
							TsStringAsHtml(pron.Form.BestVernacularAnalysisAlternative, m_cache)), le, this);
						return true;
					}
					// TODO: Compare phon.Media and pron.MediaFilesOS
					++cCommon;
				}
			}
			if (cCommon < Math.Min(le.PronunciationsOS.Count, list.Count))
			{
				m_cdConflict = new ConflictingEntry("Pronunciations", le, this);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Find the best matching pronunciation in the lex entry (if one exists) for the imported LiftPhonetic phon.
		/// If neither has any form, then only the media filenames are compared.  If both have forms, then both forms
		/// and media filenames are compared.  At least one form must match if any forms exist on either side.
		/// If either has a media file, both must have the same number of media files, and at least one filename
		/// must match.
		/// As a side-effect, dictHvoPhon has the matching hvo keyed to the imported data (if one exists).
		/// </summary>
		/// <returns>best match, or null</returns>
		private ILexPronunciation FindMatchingPronunciation(ILexEntry le, Dictionary<int, LiftPhonetic> dictHvoPhon,
			LiftPhonetic phon)
		{
			ILexPronunciation lexpron = null;
			ILexPronunciation lexpronNoMedia = null;
			int cMatches = 0;
			foreach (ILexPronunciation pron in le.PronunciationsOS)
			{
				if (dictHvoPhon.ContainsKey(pron.Hvo))
					continue;
				bool fFormMatches = false;
				int cCurrent = 0;
				if (phon.Form.Count == 0)
				{
					Dictionary<int, string> forms = GetAllUnicodeAlternatives(pron.Form);
					fFormMatches = (forms.Count == 0);
				}
				else
				{
					cCurrent = MultiUnicodeStringMatches(pron.Form, phon.Form, false, Guid.Empty, 0);
					fFormMatches = (cCurrent > cMatches);
				}
				if (fFormMatches)
				{
					cMatches = cCurrent;
					if (phon.Media.Count == pron.MediaFilesOS.Count)
					{
						int cFilesMatch = 0;
						for (int i = 0; i < phon.Media.Count; ++i)
						{
							string sURL = phon.Media[i].Url;
							if (sURL == null)
								continue;
							string sFile = Path.GetFileName(sURL);
							for (int j = 0; j < pron.MediaFilesOS.Count; ++j)
							{
								ICmFile cf = pron.MediaFilesOS[i].MediaFileRA;
								if (cf != null)
								{
									string sPath = cf.InternalPath;
									if (sPath == null)
										continue;
									if (sFile.ToLowerInvariant() == Path.GetFileName(sPath).ToLowerInvariant())
										++cFilesMatch;
								}
							}
						}
						if (phon.Media.Count == 0 || cFilesMatch > 0)
							lexpron = pron;
						else
							lexpronNoMedia = pron;
					}
					else
					{
						lexpronNoMedia = pron;
					}
				}
			}
			if (lexpron != null)
			{
				dictHvoPhon.Add(lexpron.Hvo, phon);
				return lexpron;
			}
			else if (lexpronNoMedia != null)
			{
				dictHvoPhon.Add(lexpronNoMedia.Hvo, phon);
				return lexpronNoMedia;
			}
			else
			{
				return null;
			}
		}

		private Dictionary<int, string> GetAllUnicodeAlternatives(ITsMultiString tsm)
		{
			Dictionary<int, string> dict = new Dictionary<int, string>();
			for (int i = 0; i < tsm.StringCount; ++i)
			{
				int ws;
				ITsString tss = tsm.GetStringFromIndex(i, out ws);
				if (tss.Text != null && ws != 0)
					dict.Add(ws, tss.Text);
			}
			return dict;
		}

		private Dictionary<int, ITsString> GetAllTsStringAlternatives(ITsMultiString tsm)
		{
			Dictionary<int, ITsString> dict = new Dictionary<int, ITsString>();
			for (int i = 0; i < tsm.StringCount; ++i)
			{
				int ws;
				ITsString tss = tsm.GetStringFromIndex(i, out ws);
				if (tss.Text != null && ws != 0)
					dict.Add(ws, tss);
			}
			return dict;
		}

		private void ProcessPronunciationFieldsAndTraits(ILexPronunciation pron, LiftPhonetic phon)
		{
			foreach (LiftField field in phon.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "cvpattern":
					case "cv-pattern":
						pron.CVPattern = StoreTsStringValue(m_fCreatingNewEntry, pron.CVPattern, field.Content);
						break;
					case "tone":
						pron.Tone = StoreTsStringValue(m_fCreatingNewEntry, pron.Tone, field.Content);
						break;
					default:
						ProcessUnknownField(pron, phon, field,
							"LexPronunciation", "custom-pronunciation-", LexPronunciationTags.kClassId);
						break;
				}
			}
			foreach (LiftTrait trait in phon.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case "location":
						ICmLocation loc = FindOrCreateLocation(trait.Value);
						if (pron.LocationRA != loc && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld ||
							pron.LocationRA == null))
						{
							pron.LocationRA = loc;
						}
						break;
					default:
						StoreTraitAsResidue(pron, trait);
						break;
				}
			}
		}

		private bool PronunciationFieldsOrTraitsConflict(ILexPronunciation pron, LiftPhonetic phon)
		{
			foreach (LiftField field in phon.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "cvpattern":
					case "cv-pattern":
						if (StringsConflict(pron.CVPattern, GetFirstLiftTsString(field.Content)))
							return true;
						break;
					case "tone":
						if (StringsConflict(pron.Tone, GetFirstLiftTsString(field.Content)))
							return true;
						break;
				}
			}
			foreach (LiftTrait trait in phon.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case "location":
						ICmLocation loc = FindOrCreateLocation(trait.Value);
						if (pron.LocationRA != null && pron.LocationRA != loc)
							return true;
						break;
					default:
						break;
				}
			}
			return false;
		}

		private void ProcessEntryEtymologies(ILexEntry le, LiftEntry entry)
		{
			bool fFirst = true;
			foreach (LiftEtymology let in entry.Etymologies)
			{
				if (fFirst)
				{
					if (le.EtymologyOA == null)
					{
						ILexEtymology ety = CreateNewLexEtymology();
						le.EtymologyOA = ety;
					}
					MergeInMultiUnicode(le.EtymologyOA.Form, LexEtymologyTags.kflidForm, let.Form, le.EtymologyOA.Guid);
					MergeInMultiUnicode(le.EtymologyOA.Gloss, LexEtymologyTags.kflidGloss, let.Gloss, le.EtymologyOA.Guid);
					if (let.Source != null)
					{
						if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld)
						{
							if (String.IsNullOrEmpty(le.EtymologyOA.Source))
								le.EtymologyOA.Source = let.Source;
						}
						else
						{
							le.EtymologyOA.Source = let.Source;
						}
					}
					ProcessEtymologyFieldsAndTraits(le.EtymologyOA, let);
					StoreDatesInResidue(le.EtymologyOA, let);
					fFirst = false;
				}
				else
				{
					StoreEtymologyAsResidue(le, let);
				}
			}
		}

		private bool EntryEtymologiesConflict(ILexEtymology lexety, List<LiftEtymology> list)
		{
			if (lexety == null || list.Count == 0)
				return false;
			foreach (LiftEtymology ety in list)
			{
				if (MultiUnicodeStringsConflict(lexety.Form, ety.Form, false, Guid.Empty, 0))
					return true;
				if (MultiUnicodeStringsConflict(lexety.Gloss, ety.Gloss, false, Guid.Empty, 0))
					return true;
				if (StringsConflict(lexety.Source, ety.Source))
					return true;
				if (EtymologyFieldsConflict(lexety, ety.Fields))
					return true;
				break;
			}
			return false;
		}

		private void ProcessEtymologyFieldsAndTraits(ILexEtymology ety, LiftEtymology let)
		{
			foreach (LiftField field in let.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "comment":
						MergeInMultiString(ety.Comment, LexEtymologyTags.kflidComment, field.Content, ety.Guid);
						break;
					//case "multiform":		causes problems on round-tripping
					//    MergeIn(ety.Form, field.Content, ety.Guid);
					//    break;
					default:
						ProcessUnknownField(ety, let, field,
							"LexEtymology", "custom-etymology-", LexEtymologyTags.kClassId);
						break;
				}
			}
			foreach (LiftTrait trait in let.Traits)
			{
				StoreTraitAsResidue(ety, trait);
			}
		}

		private bool EtymologyFieldsConflict(ILexEtymology lexety, List<LiftField> list)
		{
			if (lexety == null || lexety.Comment == null)
				return false;
			foreach (LiftField field in list)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "comment":
						if (MultiTsStringsConflict(lexety.Comment, field.Content))
							return true;
						break;
				}
			}
			return false;
		}

		private void ProcessEntryRelations(ILexEntry le, LiftEntry entry)
		{
			// Due to possible forward references, wait until the end to process relations.
			foreach (LiftRelation rel in entry.Relations)
			{
				if (rel.Type != "_component-lexeme" && String.IsNullOrEmpty(rel.Ref))
				{
					XmlDocument xdResidue = FindOrCreateResidue(le);
					InsertResidueContent(xdResidue, CreateXmlForRelation(rel));
				}
				else
				{
					switch (rel.Type)
					{
						case "minorentry":
						case "subentry":
							// We'll just ignore these backreferences.
							break;
						case "main":
						case "_component-lexeme":
							PendingLexEntryRef pend = new PendingLexEntryRef(le, rel, entry);
							pend.Residue = CreateRelationResidue(rel);
							m_rgPendingLexEntryRefs.Add(pend);
							break;
						default:
							string sResidue = CreateRelationResidue(rel);
							m_rgPendingRelation.Add(new PendingRelation(le, rel, sResidue));
							break;
					}
				}
			}
		}

		private void CreateEntrySense(ILexEntry le, LiftSense sense)
		{
			try
			{
				m_fCreatingNewSense = true;
				bool fNeedNewId;
				ILexSense ls = CreateNewLexSense(sense.Guid, le, out fNeedNewId);
				FillInNewSense(ls, sense, fNeedNewId);
			}
			finally
			{
				m_fCreatingNewSense = false;
			}
		}

		private void CreateSubsense(ILexSense ls, LiftSense sub)
		{
			bool fSavedCreatingNew = m_fCreatingNewSense;
			try
			{
				m_fCreatingNewSense = true;
				bool fNeedNewId;
				ILexSense lsSub = CreateNewLexSense(sub.Guid, ls, out fNeedNewId);
				FillInNewSense(lsSub, sub, fNeedNewId);
			}
			finally
			{
				m_fCreatingNewSense = fSavedCreatingNew;
			}
		}

		private void FillInNewSense(ILexSense ls, LiftSense sense, bool fNeedNewId)
		{
			if (m_cdConflict != null && m_cdConflict is ConflictingSense)
			{
				(m_cdConflict as ConflictingSense).DupSense = ls;
				m_rgcdConflicts.Add(m_cdConflict);
				m_cdConflict = null;
			}
			//sense.Order;
			StoreSenseId(ls, sense.Id);
			MergeInMultiUnicode(ls.Gloss, LexSenseTags.kflidGloss, sense.Gloss, ls.Guid);
			MergeInMultiString(ls.Definition, LexSenseTags.kflidDefinition, sense.Definition, ls.Guid);
			if (fNeedNewId)
			{
				XmlDocument xd = FindOrCreateResidue(ls, sense.Id, LexSenseTags.kflidLiftResidue);
				XmlAttribute xa = xd.FirstChild.Attributes["id"];
				if (xa == null)
				{
					xa = xd.CreateAttribute("id");
					xd.FirstChild.Attributes.Append(xa);
				}
				xa.Value = ls.LIFTid;
			}
			CreateSenseExamples(ls, sense);
			ProcessSenseGramInfo(ls, sense);
			CreateSenseIllustrations(ls, sense);
			ProcessSenseRelations(ls, sense);
			ProcessSenseReversals(ls, sense);
			ProcessSenseNotes(ls, sense);
			ProcessSenseFields(ls, sense);
			ProcessSenseTraits(ls, sense);
			foreach (LiftSense sub in sense.Subsenses)
				CreateSubsense(ls, sub);
			StoreAnnotationsAndDatesInResidue(ls, sense);
			++m_cSensesAdded;
		}

		private void MergeIntoExistingSense(ILexSense ls, LiftSense sense)
		{
			//sense.Order;
			StoreSenseId(ls, sense.Id);
			MergeInMultiUnicode(ls.Gloss, LexSenseTags.kflidGloss, sense.Gloss, ls.Guid);
			MergeInMultiString(ls.Definition, LexSenseTags.kflidDefinition, sense.Definition, ls.Guid);
			MergeSenseExamples(ls, sense);
			ProcessSenseGramInfo(ls, sense);
			MergeSenseIllustrations(ls, sense);
			ProcessSenseRelations(ls, sense);
			ProcessSenseReversals(ls, sense);
			ProcessSenseNotes(ls, sense);
			ProcessSenseFields(ls, sense);
			ProcessSenseTraits(ls, sense);

			Dictionary<LiftSense, ILexSense> map = new Dictionary<LiftSense, ILexSense>();
			Set<int> setUsed = new Set<int>();
			foreach (LiftSense sub in sense.Subsenses)
			{
				ILexSense lsSub = FindExistingSense(ls.SensesOS, sub);
				map.Add(sub, lsSub);
				if (lsSub != null)
					setUsed.Add(lsSub.Hvo);
			}
			// If we're keeping only the imported data, delete any unused subsense.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in ls.SensesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftSense sub in sense.Subsenses)
			{
				ILexSense lsSub;
				map.TryGetValue(sub, out lsSub);
				if (lsSub == null || (m_msImport == MergeStyle.MsKeepBoth && SenseHasConflictingData(lsSub, sub)))
					CreateSubsense(ls, sub);
				else
					MergeIntoExistingSense(lsSub, sub);
			}
			StoreAnnotationsAndDatesInResidue(ls, sense);
		}

		private void StoreSenseId(ILexSense ls, string sId)
		{
			if (!String.IsNullOrEmpty(sId))
			{
				FindOrCreateResidue(ls, sId, LexSenseTags.kflidLiftResidue);
				MapIdToObject(sId, ls);
			}
		}

		private void CreateSenseExamples(ILexSense ls, LiftSense sense)
		{
			foreach (LiftExample expl in sense.Examples)
			{
				ILexExampleSentence les = CreateNewLexExampleSentence(expl.Guid, ls);
				if (!String.IsNullOrEmpty(expl.Id))
					MapIdToObject(expl.Id, les);
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				CreateExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference) && !String.IsNullOrEmpty(expl.Source))
					les.Reference = m_cache.TsStrFactory.MakeString(expl.Source, m_cache.DefaultAnalWs);
				ProcessExampleFields(les, expl);
				StoreExampleResidue(les, expl);
			}
		}

		private void MergeSenseExamples(ILexSense ls, LiftSense sense)
		{
			Dictionary<LiftExample, ILexExampleSentence> map = new Dictionary<LiftExample, ILexExampleSentence>();
			Set<int> setUsed = new Set<int>();
			foreach (LiftExample expl in sense.Examples)
			{
				ILexExampleSentence les = FindingMatchingExampleSentence(ls, expl);
				map.Add(expl, les);
				if (les != null)
					setUsed.Add(les.Hvo);
			}
			// If we're keeping only the imported data, delete any unused example.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in ls.ExamplesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftExample expl in sense.Examples)
			{
				ILexExampleSentence les;
				map.TryGetValue(expl, out les);
				if (les == null)
					les = CreateNewLexExampleSentence(expl.Guid, ls);
				if (!String.IsNullOrEmpty(expl.Id))
					MapIdToObject(expl.Id, les);
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				MergeExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference) && !String.IsNullOrEmpty(expl.Source))
					les.Reference = m_cache.TsStrFactory.MakeString(expl.Source, m_cache.DefaultAnalWs);
			}
		}

		private ILexExampleSentence FindingMatchingExampleSentence(ILexSense ls, LiftExample expl)
		{
			ILexExampleSentence les = null;
			if (expl.Guid != Guid.Empty)
			{
				ICmObject cmo = GetObjectForGuid(expl.Guid);
				if (cmo != null && cmo is ILexExampleSentence)
				{
					les = cmo as ILexExampleSentence;
					if (les.Owner != ls)
						les = null;
				}
			}
			if (les == null)
				les = FindExampleSentence(ls.ExamplesOS, expl);
			return les;
		}

		private bool SenseExamplesConflict(ILexSense ls, List<LiftExample> list)
		{
			if (ls.ExamplesOS.Count == 0 || list.Count == 0)
				return false;
			foreach (LiftExample expl in list)
			{
				ILexExampleSentence les = null;
				if (expl.Guid != Guid.Empty)
				{
					ICmObject cmo = GetObjectForGuid(expl.Guid);
					if (cmo != null && cmo is ILexExampleSentence)
					{
						les = cmo as ILexExampleSentence;
						if (les.Owner.Hvo != ls.Hvo)
							les = null;
					}
				}
				if (les == null)
					les = FindExampleSentence(ls.ExamplesOS, expl);
				if (les == null)
					continue;
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				if (MultiTsStringsConflict(les.Example, expl.Content))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0})",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
				if (StringsConflict(les.Reference.Text, expl.Source))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Reference",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
				if (ExampleTranslationsConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Translations",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
				if (ExampleNotesConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Reference",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
			}
			return false;
		}

		private ILexExampleSentence FindExampleSentence(IFdoOwningSequence<ILexExampleSentence> rgexamples, LiftExample expl)
		{
			List<ILexExampleSentence> matches = new List<ILexExampleSentence>();
			int cMatches = 0;
			foreach (ILexExampleSentence les in rgexamples)
			{
				int cCurrent = MultiTsStringMatches(les.Example, expl.Content);
				if (cCurrent > cMatches)
				{
					matches.Clear();
					matches.Add(les);
					cMatches = cCurrent;
				}
				else if (cCurrent == cMatches && cCurrent > 0)
				{
					matches.Add(les);
				}
				else if ((expl.Content == null || expl.Content.IsEmpty) &&
					(les.Example == null || les.Example.BestVernacularAnalysisAlternative.Equals(les.Example.NotFoundTss)))
				{
					matches.Add(les);
				}
			}
			if (matches.Count == 0)
				return null;
			else if (matches.Count == 1)
				return matches[0];
			// Okay, we have more than one example sentence that match equally well in Example.
			// So let's look at the other fields.
			ILexExampleSentence lesMatch = null;
			cMatches = 0;
			foreach (ILexExampleSentence les in matches)
			{
				bool fSameReference = MatchingItemInNotes(les.Reference, "reference", expl.Notes);
				int cCurrent = TranslationsMatch(les.TranslationsOC, expl.Translations);
				if (fSameReference &&
					cCurrent == expl.Translations.Count &&
					cCurrent == les.TranslationsOC.Count)
				{
					return les;
				}
				if (cCurrent > cMatches)
				{
					lesMatch = les;
					cMatches = cCurrent;
				}
				else if (fSameReference)
				{
					lesMatch = les;
				}
			}
			return lesMatch;
		}

		private bool MatchingItemInNotes(ITsString tss, string sType, List<LiftNote> rgnotes)
		{
			string sItem = tss.Text;
			// Review: Should we match on the writing system inside the tss as well?
			bool fTypeFound = false;
			foreach (LiftNote note in rgnotes)
			{
				if (note.Type == sType)
				{
					fTypeFound = true;
					foreach (string sWs in note.Content.Keys)
					{
						if (sItem == note.Content[sWs].Text)
							return true;
					}
				}
			}
			return String.IsNullOrEmpty(sItem) && !fTypeFound;
		}

		private int TranslationsMatch(IFdoOwningCollection<ICmTranslation> oldList, List<LiftTranslation> newList)
		{
			if (oldList.Count == 0 || newList.Count == 0)
				return 0;
			int cMatches = 0;
			foreach (LiftTranslation tran in newList)
			{
				ICmTranslation ct = FindExampleTranslation(oldList, tran);
				if (ct != null)
					++cMatches;
			}
			return cMatches;
		}

//		private static bool StringsMatch(string sOld, string sNew)
//		{
//			if (String.IsNullOrEmpty(sOld) && String.IsNullOrEmpty(sNew))
//			{
//				return true;
//			}
//			else if (String.IsNullOrEmpty(sOld) || String.IsNullOrEmpty(sNew))
//			{
//				return false;
//			}
//			else
//			{
//				string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
//				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
//				return sOldNorm == sNewNorm;
//			}
//		}

		private void CreateExampleTranslations(ILexExampleSentence les, LiftExample expl)
		{
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmPossibility type = null;
				if (!String.IsNullOrEmpty(tran.Type))
					type = FindOrCreateTranslationType(tran.Type);
				ICmTranslation ct = CreateNewCmTranslation(les, type);
				les.TranslationsOC.Add(ct);
				MergeInMultiString(ct.Translation, CmTranslationTags.kflidTranslation, tran.Content, ct.Guid);
			}
		}

		private void MergeExampleTranslations(ILexExampleSentence les, LiftExample expl)
		{
			Dictionary<LiftTranslation, ICmTranslation> map = new Dictionary<LiftTranslation, ICmTranslation>();
			Set<int> setUsed = new Set<int>();
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct = FindExampleTranslation(les.TranslationsOC, tran);
				map.Add(tran, ct);
				if (ct != null)
					setUsed.Add(ct.Hvo);
			}
			// If we're keeping only the imported data, erase any unused existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in les.TranslationsOC.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct;
				map.TryGetValue(tran, out ct);
				ICmPossibility type = null;
				if (!String.IsNullOrEmpty(tran.Type))
					type = FindOrCreateTranslationType(tran.Type);
				if (ct == null)
				{
					ct = CreateNewCmTranslation(les, type);
					les.TranslationsOC.Add(ct);
				}
				MergeInMultiString(ct.Translation, CmTranslationTags.kflidTranslation, tran.Content, ct.Guid);
				if (type != null &&
					ct.TypeRA != type &&
					(m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld))
				{
					ct.TypeRA = type;
				}
			}
		}

		private bool ExampleTranslationsConflict(ILexExampleSentence les, LiftExample expl)
		{
			if (les.TranslationsOC.Count == 0 || expl.Translations.Count == 0)
				return false;
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct = FindExampleTranslation(les.TranslationsOC, tran);
				if (ct == null)
					continue;
				if (MultiTsStringsConflict(ct.Translation, tran.Content))
					return true;
				if (!String.IsNullOrEmpty(tran.Type))
				{
					ICmPossibility type = FindOrCreateTranslationType(tran.Type);
					if (ct.TypeRA != type && ct.TypeRA != null)
						return true;
				}
			}
			return false;
		}

		private ICmTranslation FindExampleTranslation(IFdoOwningCollection<ICmTranslation> rgtranslations,
			LiftTranslation tran)
		{
			ICmTranslation ctMatch = null;
			int cMatches = 0;
			foreach (ICmTranslation ct in rgtranslations)
			{
				int cCurrent = MultiTsStringMatches(ct.Translation, tran.Content);
				if (cCurrent > cMatches)
				{
					ctMatch = ct;
					cMatches = cCurrent;
				}
				else if ((tran.Content == null || tran.Content.IsEmpty) &&
					(ct.Translation == null || ct.Translation.BestAnalysisVernacularAlternative.Equals(ct.Translation.NotFoundTss)))
				{
					return ct;
				}
			}
			return ctMatch;
		}

		private void ProcessExampleNotes(ILexExampleSentence les, LiftExample expl)
		{
			foreach (LiftNote note in expl.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "reference":
						les.Reference = StoreTsStringValue(m_fCreatingNewSense, les.Reference, note.Content);
						break;
					default:
						StoreNoteAsResidue(les, note);
						break;
				}
			}
		}

		private bool ExampleNotesConflict(ILexExampleSentence les, LiftExample expl)
		{
			if (expl.Notes.Count == 0)
				return false;
			foreach (LiftNote note in expl.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "reference":
						if (StringsConflict(les.Reference, GetFirstLiftTsString(note.Content)))
							return true;
						break;
				}
			}
			return false;
		}

		private void ProcessExampleFields(ILexExampleSentence les, LiftExample expl)
		{
			// Note: when/if LexExampleSentence.Reference is written as a <field>
			// instead of a <note>, the next loop will presumably be changed.
			foreach (LiftField field in expl.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					default:
						ProcessUnknownField(les, expl, field,
							"LexExampleSentence", "custom-example-", LexExampleSentenceTags.kClassId);
						break;
				}
			}
		}

		private void ProcessSenseGramInfo(ILexSense ls, LiftSense sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				// except we always need a grammatical info element...
			}
			if (sense.GramInfo == null)
				return;
			if (!m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld && ls.MorphoSyntaxAnalysisRA != null)
				return;
			LiftGrammaticalInfo gram = sense.GramInfo;
			string sTraitPos = gram.Value;
			IPartOfSpeech pos = null;
			if (!String.IsNullOrEmpty(sTraitPos))
				pos = FindOrCreatePartOfSpeech(sTraitPos);
			ls.MorphoSyntaxAnalysisRA = FindOrCreateMSA(ls.Entry, pos, gram.Traits);
		}

		/// <summary>
		/// Creating individual MSAs for every sense, and then merging identical MSAs at the
		/// end is expensive: deleting each redundant MSA takes ~360 msec, which can add up
		/// quickly even for only a few hundred duplications created here.  (See LT-9006.)
		/// </summary>
		private IMoMorphSynAnalysis FindOrCreateMSA(ILexEntry le, IPartOfSpeech pos,
			List<LiftTrait> traits)
		{
			string sType = null;
			string sFromPOS = null;
			Dictionary<string, List<string>> dictPosSlots = new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> dictPosInflClasses = new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> dictPosFromInflClasses = new Dictionary<string, List<string>>();
			List<ICmPossibility> rgpossProdRestrict = new List<ICmPossibility>();
			List<ICmPossibility> rgpossFromProdRestrict = new List<ICmPossibility>();
			string sInflectionFeature = null;
			string sFromInflFeature = null;
			string sFromStemName = null;
			List<string> rgsResidue = new List<string>();
			foreach (LiftTrait trait in traits)
			{
				if (trait.Name == "type")
				{
					sType = trait.Value;
				}
				else if (trait.Name == "from-part-of-speech" || trait.Name == "FromPartOfSpeech")
				{
					sFromPOS = trait.Value;
				}
				else if (trait.Name == "exception-feature")
				{
					ICmPossibility poss = FindOrCreateExceptionFeature(trait.Value);
					rgpossProdRestrict.Add(poss);
				}
				else if (trait.Name == "from-exception-feature")
				{
					ICmPossibility poss = FindOrCreateExceptionFeature(trait.Value);
					rgpossFromProdRestrict.Add(poss);
				}
				else if (trait.Name == "inflection-feature")
				{
					sInflectionFeature = trait.Value;
				}
				else if (trait.Name == "from-inflection-feature")
				{
					sFromInflFeature = trait.Value;
				}
				else if (trait.Name == "from-stem-name")
				{
					sFromStemName = trait.Value;
				}
				else if (trait.Name.EndsWith("-slot") || trait.Name.EndsWith("-Slots"))
				{
					int len = trait.Name.Length - (trait.Name.EndsWith("-slot") ? 5 : 6);
					string sPos = trait.Name.Substring(0, len);
					List<string> rgsSlots;
					if (!dictPosSlots.TryGetValue(sPos, out rgsSlots))
					{
						rgsSlots = new List<string>();
						dictPosSlots.Add(sPos, rgsSlots);
					}
					rgsSlots.Add(trait.Value);
				}
				else if (trait.Name.EndsWith("-infl-class") || trait.Name.EndsWith("-InflectionClass"))
				{
					int len = trait.Name.Length - (trait.Name.EndsWith("-infl-class") ? 11 : 16);
					string sPos = trait.Name.Substring(0, len);
					if (sPos.StartsWith("from-"))
					{
						sPos = sPos.Substring(5);
						Debug.Assert(sPos.Length > 0);
						List<string> rgsInflClasses;
						if (!dictPosFromInflClasses.TryGetValue(sPos, out rgsInflClasses))
						{
							rgsInflClasses = new List<string>();
							dictPosFromInflClasses.Add(sPos, rgsInflClasses);
						}
						rgsInflClasses.Add(trait.Value);
					}
					else
					{
						List<string> rgsInflClasses;
						if (!dictPosInflClasses.TryGetValue(sPos, out rgsInflClasses))
						{
							rgsInflClasses = new List<string>();
							dictPosInflClasses.Add(sPos, rgsInflClasses);
						}
						rgsInflClasses.Add(trait.Value);
					}
				}
				else
				{
					rgsResidue.Add(CreateXmlForTrait(trait));
				}
			}
			IMoMorphSynAnalysis msaSense = null;
			bool fNew;
			switch (sType)
			{
				case "inflAffix":
					fNew = FindOrCreateInflAffixMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgpossProdRestrict, sInflectionFeature, rgsResidue, ref msaSense);
					break;
				case "derivAffix":
					fNew = FindOrCreateDerivAffixMSA(le, pos, sFromPOS, dictPosSlots,
						dictPosInflClasses, dictPosFromInflClasses,
						rgpossProdRestrict, rgpossFromProdRestrict,
						sInflectionFeature, sFromInflFeature, sFromStemName,
						rgsResidue, ref msaSense);
					break;
				case "derivStepAffix":
					fNew = FindOrCreateDerivStepAffixMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgsResidue, ref msaSense);
					break;
				case "affix":
					fNew = FindOrCreateUnclassifiedAffixMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgsResidue, ref msaSense);
					break;
				default:
					fNew = FindOrCreateStemMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgpossProdRestrict, sInflectionFeature, rgsResidue, ref msaSense);
					break;
			}
			if (fNew)
			{
				ProcessMsaSlotInformation(dictPosSlots, msaSense);
				ProcessMsaInflectionClassInfo(dictPosInflClasses, dictPosFromInflClasses, msaSense);
				StoreMsaExceptionFeatures(rgpossProdRestrict, rgpossFromProdRestrict, msaSense);
				if (!ParseFeatureString(sInflectionFeature, msaSense, false))
				{
					int flid = MoStemMsaTags.kflidMsFeatures;
					if (msaSense is IMoInflAffMsa)
						flid = MoInflAffMsaTags.kflidInflFeats;
					else if (msaSense is IMoDerivAffMsa)
						flid = MoDerivAffMsaTags.kflidToMsFeatures;
					LogInvalidFeatureString(le, sInflectionFeature, flid);
				}
				if (msaSense is IMoDerivAffMsa && !ParseFeatureString(sFromInflFeature, msaSense, true))
				{
					LogInvalidFeatureString(le, sFromInflFeature, MoDerivAffMsaTags.kflidFromMsFeatures);
				}
				if (!String.IsNullOrEmpty(sFromStemName))
					ProcessMsaStemName(sFromStemName, sFromPOS, msaSense, rgsResidue);
				StoreResidue(msaSense, rgsResidue);
			}
			return msaSense;
		}

		private void ProcessMsaStemName(string sFromStemName, string sFromPos,
			IMoMorphSynAnalysis msaSense, List<string> rgsResidue)
		{
			if (msaSense is IMoDerivAffMsa)
			{
				var key = String.Format("{0}:{1}", sFromPos, sFromStemName);
				IMoStemName stem;
				if (m_dictStemName.TryGetValue(key, out stem))
				{
					(msaSense as IMoDerivAffMsa).FromStemNameRA = stem;
					return;
				}
				// TODO: Create new IMoStemName object?
			}
			string sResidue = String.Format("<trait name=\"from-stem-name\" value=\"{0}\"/>",
				XmlUtils.MakeSafeXmlAttribute(sFromStemName));
			rgsResidue.Add(sResidue);
		}

		private void LogInvalidFeatureString(ILexEntry le, string sInflectionFeature, int flid)
		{
			InvalidData bad = new InvalidData(LexTextControls.ksCannotParseFeature,
				le.Guid, flid, sInflectionFeature, 0, m_cache, this);
			if (!m_rgInvalidData.Contains(bad))
				m_rgInvalidData.Add(bad);
		}

		private ICmPossibility FindOrCreateExceptionFeature(string sValue)
		{
			ICmPossibility poss;
			if (!m_dictExceptFeats.TryGetValue(sValue, out poss))
			{
				EnsureProdRestrictListExists();
				if (m_factCmPossibility == null)
					m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
				poss = m_factCmPossibility.Create();
				m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Add(poss);
				ITsString tss = m_cache.TsStrFactory.MakeString(sValue, m_cache.DefaultAnalWs);
				poss.Name.AnalysisDefaultWritingSystem = tss;
				poss.Abbreviation.AnalysisDefaultWritingSystem = tss;
				m_rgnewExceptFeat.Add(poss);
				m_dictExceptFeats.Add(sValue, poss);
			}
			return poss;
		}

		/// <summary>
		/// Find or create an IMoStemMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateStemMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<ICmPossibility> rgpossProdRestrict, string sInflectionFeature,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoStemMsa msaStem = msa as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaStem) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaStem) &&
					MsaExceptionFeatsMatch(rgpossProdRestrict, null, msaStem) &&
					MsaInflFeatureMatches(sInflectionFeature, null, msaStem))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
				(msaSense as IMoStemMsa).PartOfSpeechRA = pos;
			return true;
		}

		private void StoreMsaExceptionFeatures(List<ICmPossibility> rgpossProdRestrict,
			List<ICmPossibility> rgpossFromProdRestrict,
			IMoMorphSynAnalysis msaSense)
		{
			IMoStemMsa msaStem = msaSense as IMoStemMsa;
			if (msaStem != null)
			{
				foreach (ICmPossibility poss in rgpossProdRestrict)
					msaStem.ProdRestrictRC.Add(poss);
				return;
			}
			IMoInflAffMsa msaInfl = msaSense as IMoInflAffMsa;
			if (msaInfl != null)
			{
				foreach (ICmPossibility poss in rgpossProdRestrict)
					msaInfl.FromProdRestrictRC.Add(poss);
				return;
			}
			IMoDerivAffMsa msaDeriv = msaSense as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				foreach (ICmPossibility poss in rgpossProdRestrict)
					msaDeriv.ToProdRestrictRC.Add(poss);
				if (rgpossFromProdRestrict != null)
				{
					foreach (ICmPossibility poss in rgpossFromProdRestrict)
						msaDeriv.FromProdRestrictRC.Add(poss);
				}
				return;
			}
		}

		/// <summary>
		/// Find or create an IMoUnclassifiedAffixMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateUnclassifiedAffixMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoUnclassifiedAffixMsa msaAffix = msa as IMoUnclassifiedAffixMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoUnclassifiedAffixMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
				(msaSense as IMoUnclassifiedAffixMsa).PartOfSpeechRA = pos;
			return true;
		}

		/// <summary>
		/// Find or create an IMoDerivStepMsa which matches the given values.
		/// </summary>
		/// <param name="le">The entry.</param>
		/// <param name="pos">The part of speech.</param>
		/// <param name="dictPosSlots">The dict pos slots.</param>
		/// <param name="dictPosInflClasses">The dict pos infl classes.</param>
		/// <param name="rgsResidue">The RGS residue.</param>
		/// <param name="msaSense">The msa sense.</param>
		/// <returns>
		/// true if the desired MSA is newly created, false if it already exists
		/// </returns>
		private bool FindOrCreateDerivStepAffixMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoDerivStepMsa msaAffix = msa as IMoDerivStepMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoDerivStepMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
				(msaSense as IMoDerivStepMsa).PartOfSpeechRA = pos;
			return true;
		}

		/// <summary>
		/// Find or create an IMoDerivAffMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateDerivAffixMSA(ILexEntry le, IPartOfSpeech pos, string sFromPOS,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			Dictionary<string, List<string>> dictPosFromInflClasses,
			List<ICmPossibility> rgpossProdRestrict, List<ICmPossibility> rgpossFromProdRestrict,
			string sInflectionFeature, string sFromInflFeature, string sFromStemName,
			List<string> rgsResidue, ref IMoMorphSynAnalysis msaSense)
		{
			IPartOfSpeech posFrom = null;
			if (!String.IsNullOrEmpty(sFromPOS))
				posFrom = FindOrCreatePartOfSpeech(sFromPOS);
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoDerivAffMsa msaAffix = msa as IMoDerivAffMsa;
				if (msaAffix != null &&
					msaAffix.ToPartOfSpeechRA == pos &&
					msaAffix.FromPartOfSpeechRA == posFrom &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, dictPosFromInflClasses, msaAffix) &&
					MsaExceptionFeatsMatch(rgpossProdRestrict, rgpossFromProdRestrict, msaAffix) &&
					MsaInflFeatureMatches(sInflectionFeature, sFromInflFeature, msaAffix) &&
					MsaStemNameMatches(sFromStemName, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoDerivAffMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
				(msaSense as IMoDerivAffMsa).ToPartOfSpeechRA = pos;
			if (posFrom != null)
				(msaSense as IMoDerivAffMsa).FromPartOfSpeechRA = posFrom;
			return true;
		}

		/// <summary>
		/// Find or create an IMoInflAffMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateInflAffixMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<ICmPossibility> rgpossProdRestrict, string sFeatureString,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoInflAffMsa msaAffix = msa as IMoInflAffMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaAffix) &&
					MsaExceptionFeatsMatch(rgpossProdRestrict, null,  msaAffix) &&
					MsaInflFeatureMatches(sFeatureString, null, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoInflAffMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
				(msaSense as IMoInflAffMsa).PartOfSpeechRA = pos;
			return true;
		}

		//private bool MsaResidueMatches(List<string> rgsResidue, IMoMorphSynAnalysis msa)
		//{
		//    string sResidue = (msa as MoMorphSynAnalysis).LiftResidueContent;
		//    if (String.IsNullOrEmpty(sResidue))
		//        return rgsResidue.Count == 0;
		//    int cch = 0;
		//    foreach (string s in rgsResidue)
		//    {
		//        if (sResidue.IndexOf(s) < 0)
		//            return false;
		//        cch += s.Length;
		//    }
		//    return sResidue.Length == cch;
		//}

		private void ProcessMsaInflectionClassInfo(Dictionary<string, List<string>> dictPosInflClasses,
			Dictionary<string, List<string>> dictPosFromInflClasses, IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
				return;
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
			IMoStemMsa msaStem = msa as IMoStemMsa;
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (msaDeriv != null)
				{
					msaDeriv.ToInflectionClassRA = null;
					msaDeriv.FromInflectionClassRA = null;
				}
				else if (msaStep != null)
				{
					msaStep.InflectionClassRA = null;
				}
				else if (msaStem != null)
				{
					msaStem.InflectionClassRA = null;
				}
			}
			else if (m_msImport == MergeStyle.MsKeepOld && !m_fCreatingNewSense)
			{
				if (msaDeriv != null && (msaDeriv.ToInflectionClassRA != null || msaDeriv.FromInflectionClassRA != null))
					return;
				if (msaStep != null && msaStep.InflectionClassRA != null)
					return;
				if (msaStem != null && msaStem.InflectionClassRA != null)
					return;
			}
			foreach (string sPos in dictPosInflClasses.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && pos != null)
				{
					foreach (string sInflClass in rgsInflClasses)
					{
						IMoInflClass incl = FindOrCreateInflectionClass(pos, sInflClass);
						if (msaDeriv != null)
							msaDeriv.ToInflectionClassRA = incl;
						else if (msaStep != null)
							msaStep.InflectionClassRA = incl;
						else if (msaStem != null)
							msaStem.InflectionClassRA = incl;
					}
				}
			}
			if (msaDeriv != null && dictPosFromInflClasses != null)
			{
				foreach (string sPos in dictPosFromInflClasses.Keys)
				{
					IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
					List<string> rgsInflClasses = dictPosFromInflClasses[sPos];
					if (rgsInflClasses.Count > 0 && pos != null)
					{
						foreach (string sInflClass in rgsInflClasses)
						{
							IMoInflClass incl = FindOrCreateInflectionClass(pos, sInflClass);
							msaDeriv.FromInflectionClassRA = incl;		// last one wins...
						}
					}
				}
			}
		}

		private IMoInflClass FindOrCreateInflectionClass(IPartOfSpeech pos, string sInflClass)
		{
			IMoInflClass incl = null;
			foreach (IMoInflClass inclT in pos.InflectionClassesOC)
			{
				if (HasMatchingUnicodeAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name))
				{
					incl = inclT;
					break;
				}
			}
			if (incl == null)
			{
				incl = CreateNewMoInflClass();
				pos.InflectionClassesOC.Add(incl);
				incl.Name.set_String(m_cache.DefaultAnalWs, sInflClass);
				m_rgnewInflClasses.Add(incl);
			}
			return incl;
		}

		private bool MsaInflClassInfoMatches(Dictionary<string, List<string>> dictPosInflClasses,
			Dictionary<string, List<string>> dictPosFromInflClasses, IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
				return true;
			bool fMatch = MsaMatchesInflClass(dictPosInflClasses, msa, false);
			if (fMatch && msa is IMoDerivAffMsa && dictPosFromInflClasses != null)
				fMatch = MsaMatchesInflClass(dictPosFromInflClasses, msa, true);
			return fMatch;
		}

		private bool MsaMatchesInflClass(Dictionary<string, List<string>> dictPosInflClasses,
			IMoMorphSynAnalysis msa, bool fFrom)
		{
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
			IMoStemMsa msaStem = msa as IMoStemMsa;
			bool fMatch = true;
			foreach (string sPos in dictPosInflClasses.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && pos != null)
				{
					foreach (string sInflClass in rgsInflClasses)
					{
						IMoInflClass incl = null;
						foreach (IMoInflClass inclT in pos.InflectionClassesOC)
						{
							if (HasMatchingUnicodeAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name))
							{
								incl = inclT;
								break;
							}
						}
						if (incl == null)
						{
							// Go ahead and create the new inflection class now.
							incl = CreateNewMoInflClass();
							pos.InflectionClassesOC.Add(incl);
							incl.Name.set_String(m_cache.DefaultAnalWs, sInflClass);
							m_rgnewInflClasses.Add(incl);
						}
						if (fFrom)
						{
							if (msaDeriv != null)
								fMatch = msaDeriv.FromInflectionClassRA == incl;
						}
						else
						{
							if (msaDeriv != null)
								fMatch = msaDeriv.ToInflectionClassRA == incl;
							else if (msaStep != null)
								fMatch = msaStep.InflectionClassRA == incl;
							else if (msaStem != null)
								fMatch = msaStem.InflectionClassRA == incl;
						}

					}
				}
			}
			return fMatch;
		}

		private void ProcessMsaSlotInformation(Dictionary<string, List<string>> dictPosSlots,
			IMoMorphSynAnalysis msa)
		{
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl == null)
				return;
			foreach (string sPos in dictPosSlots.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Count > 0 && pos != null)
				{
					foreach (string sSlot in rgsSlot)
					{
						IMoInflAffixSlot slot = null;
						foreach (IMoInflAffixSlot slotT in pos.AffixSlotsOC)
						{
							if (HasMatchingUnicodeAlternative(sSlot.ToLowerInvariant(), null, slotT.Name))
							{
								slot = slotT;
								break;
							}
						}
						if (slot == null)
						{
							slot = CreateNewMoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.set_String(m_cache.DefaultAnalWs, sSlot);
							m_rgnewSlots.Add(slot);
						}
						if (!msaInfl.SlotsRC.Contains(slot))
							msaInfl.SlotsRC.Add(slot);
					}
				}
			}
		}

		private bool MsaSlotInfoMatches(Dictionary<string, List<string>> dictPosSlots,
			IMoMorphSynAnalysis msa)
		{
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl == null)
				return true;
			foreach (string sPos in dictPosSlots.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Count > 0 && pos != null)
				{
					foreach (string sSlot in rgsSlot)
					{
						IMoInflAffixSlot slot = null;
						foreach (IMoInflAffixSlot slotT in pos.AffixSlotsOC)
						{
							if (HasMatchingUnicodeAlternative(sSlot.ToLowerInvariant(), null, slotT.Name))
							{
								slot = slotT;
								break;
							}
						}
						if (slot == null)
						{
							// Go ahead and create the new slot -- we'll need it shortly.
							slot = CreateNewMoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.set_String(m_cache.DefaultAnalWs, sSlot);
							m_rgnewSlots.Add(slot);
						}
						if (!msaInfl.SlotsRC.Contains(slot))
							return false;
					}
				}
			}
			return true;
		}

		private bool MsaExceptionFeatsMatch(List<ICmPossibility> rgpossProdRestrict,
			List<ICmPossibility> rgpossFromProdRestrict, IMoMorphSynAnalysis msa)
		{
			IMoStemMsa msaStem = msa as IMoStemMsa;
			if (msaStem != null)
			{
				if (msaStem.ProdRestrictRC.Count != rgpossProdRestrict.Count)
					return false;
				foreach (ICmPossibility poss in msaStem.ProdRestrictRC)
				{
					if (!rgpossProdRestrict.Contains(poss))
						return false;
				}
				return true;
			}
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl != null)
			{
				if (msaInfl.FromProdRestrictRC.Count != rgpossProdRestrict.Count)
					return false;
				foreach (ICmPossibility poss in msaInfl.FromProdRestrictRC)
				{
					if (!rgpossProdRestrict.Contains(poss))
						return false;
				}
				return true;
			}
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				if (msaDeriv.ToProdRestrictRC.Count != rgpossProdRestrict.Count)
					return false;
				if (rgpossFromProdRestrict == null && msaDeriv.FromProdRestrictRC.Count > 0)
					return false;
				if (msaDeriv.FromProdRestrictRC.Count != rgpossFromProdRestrict.Count)
					return false;
				foreach (ICmPossibility poss in msaDeriv.ToProdRestrictRC)
				{
					if (!rgpossProdRestrict.Contains(poss))
						return false;
				}
				if (rgpossFromProdRestrict != null)
				{
					foreach (ICmPossibility poss in msaDeriv.FromProdRestrictRC)
					{
						if (!rgpossFromProdRestrict.Contains(poss))
							return false;
					}
				}
				return true;
			}
			return true;
		}

		private bool MsaInflFeatureMatches(string sFeatureString, string sFromInflectionFeature,
			IMoMorphSynAnalysis msa)
		{
			IMoStemMsa msaStem = msa as IMoStemMsa;
			if (msaStem != null)
			{
				if (msaStem.MsFeaturesOA == null)
					return String.IsNullOrEmpty(sFeatureString);
				else if (String.IsNullOrEmpty(sFeatureString))
					return false;
				else
					return sFeatureString == msaStem.MsFeaturesOA.LiftName;
			}
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl != null)
			{
				if (msaInfl.InflFeatsOA == null)
					return String.IsNullOrEmpty(sFeatureString);
				else if (String.IsNullOrEmpty(sFeatureString))
					return false;
				else
					return sFeatureString == msaInfl.InflFeatsOA.LiftName;
			}
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				bool fOk;
				if (msaDeriv.ToMsFeaturesOA == null)
					fOk = String.IsNullOrEmpty(sFeatureString);
				else
					fOk = msaDeriv.ToMsFeaturesOA.LiftName == sFeatureString;
				if (fOk)
				{
					if (msaDeriv.FromMsFeaturesOA == null)
						fOk = String.IsNullOrEmpty(sFromInflectionFeature);
					else
						fOk = msaDeriv.FromMsFeaturesOA.LiftName == sFromInflectionFeature;
				}
				return fOk;
			}
			return true;
		}

		/// <summary>
		/// Parse a feature string that looks like "[nagr:[gen:f num:?]]", and store
		/// the corresponding feature structure.
		/// </summary>
		private bool ParseFeatureString(string sFeatureString, IMoMorphSynAnalysis msa, bool fFrom)
		{
			if (String.IsNullOrEmpty(sFeatureString))
				return true;
			sFeatureString = sFeatureString.Trim();
			if (String.IsNullOrEmpty(sFeatureString))
				return true;
			IMoStemMsa msaStem = msa as IMoStemMsa;
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			if (msaStem == null && msaInfl == null && msaDeriv == null)
				return false;
			string sType = null;
			if (sFeatureString[0] == '{')
			{
				int idx = sFeatureString.IndexOf('}');
				if (idx < 0)
					return false;
				sType = sFeatureString.Substring(1, idx - 1);
				sType = sType.Trim();
				sFeatureString = sFeatureString.Substring(idx + 1);
				sFeatureString = sFeatureString.Trim();
			}
			if (sFeatureString[0] == '[' && sFeatureString.EndsWith("]"))
			{
				// Remove the outermost bracketing
				List<string> rgsName = new List<string>();
				List<string> rgsValue = new List<string>();
				if (SplitFeatureString(sFeatureString.Substring(1, sFeatureString.Length - 2), rgsName, rgsValue))
				{
					if (m_factFsFeatStruc == null)
						m_factFsFeatStruc = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
					IFsFeatStruc feat = m_factFsFeatStruc.Create();
					if (msaStem != null)
					{
						msaStem.MsFeaturesOA = feat;
					}
					else if (msaInfl != null)
					{
						msaInfl.InflFeatsOA = feat;
					}
					else if (msaDeriv != null)
					{
						if (fFrom)
							msaDeriv.FromMsFeaturesOA = feat;
						else if (msaDeriv != null)
							msaDeriv.ToMsFeaturesOA = feat;
					}
					else
					{
						return false;
					}
					if (!String.IsNullOrEmpty(sType))
					{
						IFsFeatStrucType type = null;
						if (m_mapIdFeatStrucType.TryGetValue(sType, out type))
							feat.TypeRA = type;
						else
							return false;
					}
					return ProcessFeatStrucData(rgsName, rgsValue, feat);
				}
				else
				{
					return false;
				}
			}
			return false;
		}

		private IFsFeatStruc ParseFeatureString(string sFeatureString, IMoStemName stem)
		{
			if (String.IsNullOrEmpty(sFeatureString))
				return null;
			sFeatureString = sFeatureString.Trim();
			if (String.IsNullOrEmpty(sFeatureString))
				return null;
			if (stem == null)
				return null;
			IFsFeatStrucType type = null;
			if (sFeatureString[0] == '{')
			{
				int idx = sFeatureString.IndexOf('}');
				if (idx < 0)
					return null;
				string sType = sFeatureString.Substring(1, idx - 1);
				sType = sType.Trim();
				if (!String.IsNullOrEmpty(sType))
				{
					if (!m_mapIdFeatStrucType.TryGetValue(sType, out type))
						return null;
				}
				sFeatureString = sFeatureString.Substring(idx + 1);
				sFeatureString = sFeatureString.Trim();
			}
			if (sFeatureString[0] == '[' && sFeatureString.EndsWith("]"))
			{
				// Remove the outermost bracketing
				List<string> rgsName = new List<string>();
				List<string> rgsValue = new List<string>();
				if (SplitFeatureString(sFeatureString.Substring(1, sFeatureString.Length - 2), rgsName, rgsValue))
				{
					if (m_factFsFeatStruc == null)
						m_factFsFeatStruc = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
					IFsFeatStruc ffs = null;
					ffs = m_factFsFeatStruc.Create();
					stem.RegionsOC.Add(ffs);
					if (type != null)
						ffs.TypeRA = type;
					if (ProcessFeatStrucData(rgsName, rgsValue, ffs))
					{
						int cffs = 0;
						string liftName = ffs.LiftName;
						foreach (IFsFeatStruc fs in stem.RegionsOC)
						{
							if (fs.LiftName == liftName)
								++cffs;
						}
						if (cffs > 1)
						{
							stem.RegionsOC.Remove(ffs);
							return null;
						}
						return ffs;
					}
					else
					{
						stem.RegionsOC.Remove(ffs);
						return null;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// recursively process the inner text of a feature structure.
		/// </summary>
		/// <returns>true if successful, false if a parse error occurs</returns>
		private bool ProcessFeatStrucData(List<string> rgsName, List<string> rgsValue,
			IFsFeatStruc ownerFeatStruc)
		{
			// TODO: figure out how (and when) to set ownerFeatStruc.TypeRA
			Debug.Assert(rgsName.Count == rgsValue.Count);
			for (int i = 0; i < rgsName.Count; ++i)
			{
				string sName = rgsName[i];
				IFsFeatDefn featDefn = null;
				if (!m_mapIdFeatDefn.TryGetValue(sName, out featDefn))
				{
					// REVIEW: SHOULD WE TRY TO CREATE ONE?
					return false;
				}
				string sValue = rgsValue[i];
				if (sValue[0] == '[')
				{
					if (!sValue.EndsWith("]"))
						return false;
					if (m_factFsComplexValue == null)
						m_factFsComplexValue = m_cache.ServiceLocator.GetInstance<IFsComplexValueFactory>();
					List<string> rgsValName = new List<string>();
					List<string> rgsValValue = new List<string>();
					if (SplitFeatureString(sValue.Substring(1, sValue.Length - 2), rgsValName, rgsValValue))
					{
						IFsComplexValue val = m_factFsComplexValue.Create();
						ownerFeatStruc.FeatureSpecsOC.Add(val);
						val.FeatureRA = featDefn;
						IFsFeatStruc featVal = m_factFsFeatStruc.Create();
						val.ValueOA = featVal;
						if (!ProcessFeatStrucData(rgsValName, rgsValValue, featVal))
							return false;
					}
					else
					{
						return false;
					}
				}
				else
				{
					if (m_factFsClosedValue == null)
						m_factFsClosedValue = m_cache.ServiceLocator.GetInstance<IFsClosedValueFactory>();
					IFsSymFeatVal featVal = null;
					string valueKey = String.Format("{0}:{1}", sName, sValue);
					if (m_mapIdAbbrSymFeatVal.TryGetValue(valueKey, out featVal))
					{
						IFsClosedValue val = m_factFsClosedValue.Create();
						ownerFeatStruc.FeatureSpecsOC.Add(val);
						val.FeatureRA = featDefn;
						val.ValueRA = featVal;
					}
					else
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Split the feature string into its parallel names and values.  It may well have only
		/// one of each.  The outermost brackets have been removed before this method is called.
		/// </summary>
		/// <returns>true if successful, false if a parse error occurs</returns>
		private static bool SplitFeatureString(string sFeat, List<string> rgsName, List<string> rgsValue)
		{
			while (!String.IsNullOrEmpty(sFeat))
			{
				int idxVal = sFeat.IndexOf(':');
				if (idxVal > 0)
				{
					string sFeatName = sFeat.Substring(0, idxVal).Trim();
					string sFeatVal = sFeat.Substring(idxVal + 1).Trim();
					if (sFeatName.Length == 0 || sFeatVal.Length == 0)
						return false;
					rgsName.Add(sFeatName);
					int idxSep = -1;
					if (sFeatVal[0] == '[')
					{
						idxSep = FindMatchingCloseBracket(sFeatVal);
						if (idxSep < 0)
							return false;
						++idxSep;
						if (idxSep >= sFeatVal.Length)
							idxSep = -1;
						else if (sFeatVal[idxSep] != ' ')
							return false;
					}
					else
					{
						idxSep = sFeatVal.IndexOf(' ');
					}
					if (idxSep > 0)
					{
						rgsValue.Add(sFeatVal.Substring(0, idxSep));
						sFeat = sFeatVal.Substring(idxSep).Trim();
					}
					else
					{
						rgsValue.Add(sFeatVal);
						sFeat = null;
					}
				}
				else
				{
					return false;
				}
			}
			return rgsName.Count == rgsValue.Count && rgsName.Count > 0;
		}

		/// <summary>
		/// If the string starts with an open bracket ('['), find the matching close bracket
		/// (']').  There may be embedded pairs of open and close brackets inside the string!
		/// </summary>
		/// <returns>index of the matching close bracket, or a negative number if not found</returns>
		private static int FindMatchingCloseBracket(string sFeatVal)
		{
			if (sFeatVal[0] != '[')
				return -1;
			char[] rgBrackets = new char[] { '[', ']' };
			int cOpen = 1;
			int idxBracket = 0;
			while (cOpen > 0)
			{
				idxBracket = sFeatVal.IndexOfAny(rgBrackets, idxBracket + 1);
				if (idxBracket < 0)
					return idxBracket;
				if (sFeatVal[idxBracket] == '[')
					++cOpen;
				else
					--cOpen;
			}
			return idxBracket;
		}

		private bool MsaStemNameMatches(string sFromStemName, IMoDerivAffMsa msaAffix)
		{
			if (String.IsNullOrEmpty(sFromStemName) && msaAffix.FromStemNameRA == null)
				return true;
			IMoStemName msn = msaAffix.FromStemNameRA;
			int ws;
			for (int i = 0; i < msn.Name.StringCount; ++i)
			{
				ITsString tss = msn.Name.GetStringFromIndex(i, out ws);
				if (tss.Text == sFromStemName)
					return true;
			}
			return false;
		}

		private bool SenseGramInfoConflicts(ILexSense ls, LiftGrammaticalInfo gram)
		{
			if (ls.MorphoSyntaxAnalysisRA == null || gram == null)
				return false;
			string sPOS = gram.Value;
			IPartOfSpeech pos = null;
			string sType = null;
			string sFromPOS = null;
			Dictionary<string, List<string>> dictPosSlots = new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> dictPosInflClasses = new Dictionary<string, List<string>>();
			foreach (LiftTrait trait in gram.Traits)
			{
				if (trait.Name == "type")
				{
					sType = trait.Value;
				}
				else if (trait.Name == "from-part-of-speech" || trait.Name == "FromPartOfSpeech")
				{
					sFromPOS = trait.Value;
				}
				else if (trait.Name.EndsWith("-slot") || trait.Name.EndsWith("-Slots"))
				{
					int len = trait.Name.Length - (trait.Name.EndsWith("-slot") ? 5 : 6);
					string sTraitPos = trait.Name.Substring(0, len);
					List<string> rgsSlots;
					if (!dictPosSlots.TryGetValue(sTraitPos, out rgsSlots))
					{
						rgsSlots = new List<string>();
						dictPosSlots.Add(sTraitPos, rgsSlots);
					}
					rgsSlots.Add(trait.Value);
				}
				else if (trait.Name.EndsWith("-infl-class") || trait.Name.EndsWith("-InflectionClass"))
				{
					int len = trait.Name.Length - (trait.Name.EndsWith("-infl-class") ? 11 : 16);
					string sTraitPos = trait.Name.Substring(0, len);
					List<string> rgsInflClasses;
					if (!dictPosInflClasses.TryGetValue(sTraitPos, out rgsInflClasses))
					{
						rgsInflClasses = new List<string>();
						dictPosInflClasses.Add(sTraitPos, rgsInflClasses);
					}
					rgsInflClasses.Add(trait.Value);
				}
			}
			if (!String.IsNullOrEmpty(sPOS))
				pos = FindOrCreatePartOfSpeech(sPOS);
			IMoMorphSynAnalysis msa = ls.MorphoSyntaxAnalysisRA;
			int hvoPosOld = 0;
			switch (sType)
			{
				case "inflAffix":
					IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
					if (msaInfl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaInfl.PartOfSpeechRA == null ? 0 : msaInfl.PartOfSpeechRA.Hvo;
					break;
				case "derivAffix":
					IMoDerivAffMsa msaDerv = msa as IMoDerivAffMsa;
					if (msaDerv == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaDerv.ToPartOfSpeechRA == null ? 0 : msaDerv.ToPartOfSpeechRA.Hvo;
					if (!String.IsNullOrEmpty(sFromPOS))
					{
						IPartOfSpeech posNewFrom = FindOrCreatePartOfSpeech(sFromPOS);
						int hvoOldFrom = msaDerv.FromPartOfSpeechRA == null ? 0 : msaDerv.FromPartOfSpeechRA.Hvo;
						if (posNewFrom != null && hvoOldFrom != 0 && posNewFrom.Hvo != hvoOldFrom)
							return true;
					}
					break;
				case "derivStepAffix":
					IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
					if (msaStep == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaStep.PartOfSpeechRA == null ? 0 : msaStep.PartOfSpeechRA.Hvo;
					break;
				case "affix":
					IMoUnclassifiedAffixMsa msaUncl = msa as IMoUnclassifiedAffixMsa;
					if (msaUncl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaUncl.PartOfSpeechRA == null ? 0 : msaUncl.PartOfSpeechRA.Hvo;
					break;
				default:
					IMoStemMsa msaStem = msa as IMoStemMsa;
					if (msaStem == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaStem.PartOfSpeechRA == null ? 0 : msaStem.PartOfSpeechRA.Hvo;
					break;
			}
			if (hvoPosOld != 0 && pos != null && hvoPosOld != pos.Hvo)
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Part of Speech", ls, this);
				return true;
			}
			if (MsaSlotInformationConflicts(dictPosSlots, msa))
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Slot", ls, this);
				return true;
			}
			if (MsaInflectionClassInfoConflicts(dictPosInflClasses, msa))
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Inflection Class", ls, this);
				return true;
			}
			return false;
		}

		private bool MsaSlotInformationConflicts(Dictionary<string, List<string>> dictPosSlots,
			IMoMorphSynAnalysis msa)
		{
			// how do we determine conflicts in a list?
			return false;
		}

		private bool MsaInflectionClassInfoConflicts(Dictionary<string, List<string>> dictPosInflClasses,
			IMoMorphSynAnalysis msa)
		{
			// How do we determine conflicts in a list?
			return false;
		}

		private void CreateSenseIllustrations(ILexSense ls, LiftSense sense)
		{
			foreach (LiftUrlRef uref in sense.Illustrations)
			{
				string sFile = uref.Url;
				sFile = sFile.Replace('/', '\\');
				int ws = 0;
				if (uref.Label != null && !uref.Label.IsEmpty)
					ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
				else
					ws = m_cache.DefaultVernWs;
				CreatePicture(ls, uref, sFile, ws);
			}
		}

		/// <summary>
		/// Create a picture, adding it to the lex sense.  The filename is used to guess a full path,
		/// and the label from uref is used to set the caption.
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="uref"></param>
		/// <param name="sFile"></param>
		/// <param name="ws"></param>
		private void CreatePicture(ILexSense ls, LiftUrlRef uref, string sFile, int ws)
		{
			string sFolder = StringUtils.LocalPictures;
			// Paths to try for resolving given filename:
			// {directory of LIFT file}/pictures/filename
			// {FW LinkedFilesRootDir}/filename
			// {FW LinkedFilesRootDir}/Pictures/filename
			// {FW DataDir}/filename
			// {FW DataDir}/Pictures/filename
			// give up and store relative path Pictures/filename (even though it doesn't exist)
			string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
				String.Format("pictures{0}{1}", Path.DirectorySeparatorChar, sFile));
			if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
			{
				sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sFile);
				if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
				{
					sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir,
						String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, sFile));
					if (!File.Exists(sPath))
					{
						sPath = Path.Combine(DirectoryFinder.FWDataDirectory, sFile);
						if (!File.Exists(sPath))
							sPath = Path.Combine(DirectoryFinder.FWDataDirectory,
								String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, sFile));
					}
				}
			}

			ICmPicture pict = CreateNewCmPicture();
			ls.PicturesOS.Add(pict);

			try
			{
				pict.UpdatePicture(sPath, GetFirstLiftTsString(uref.Label), sFolder, ws);
			}
			catch (ArgumentException ex)
			{
				// If sPath is empty (which it never can be), trying to create the CmFile
				// for the picture will throw. Even if this could happen, we wouldn't care,
				// as the caption will still be set properly.
				Debug.WriteLine("Error initializing picture: " + ex.Message);
			}
			if (!File.Exists(sPath))
			{
				pict.PictureFileRA.InternalPath =
					String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, sFile);
			}
			MergeInMultiString(pict.Caption, CmPictureTags.kflidCaption, uref.Label, pict.Guid);
		}

		private void MergeSenseIllustrations(ILexSense ls, LiftSense sense)
		{
			Dictionary<LiftUrlRef, ICmPicture> map = new Dictionary<LiftUrlRef, ICmPicture>();
			Set<int> setUsed = new Set<int>();
			foreach (LiftUrlRef uref in sense.Illustrations)
			{
				string sFile = uref.Url.Replace('/', '\\');
				ICmPicture pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				map.Add(uref, pict);
				if (pict != null)
					setUsed.Add(pict.Hvo);
			}
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in ls.PicturesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftUrlRef uref in sense.Illustrations)
			{
				ICmPicture pict;
				map.TryGetValue(uref, out pict);
				if (pict == null)
				{
					string sFile = uref.Url.Replace('/', '\\');
					int ws = 0;
					if (uref.Label != null && !uref.Label.IsEmpty)
						ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
					else
						ws = m_cache.DefaultVernWs;
					CreatePicture(ls, uref, sFile, ws);
				}
				else
				{
					MergeInMultiString(pict.Caption, CmPictureTags.kflidCaption, uref.Label, pict.Guid);
				}
			}
		}

		private ICmPicture FindPicture(IFdoOwningSequence<ICmPicture> rgpictures, string sFile,
			LiftMultiText lmtLabel)
		{
			ICmPicture pictMatching = null;
			int cMatches = 0;
			foreach (ICmPicture pict in rgpictures)
			{
				if (pict.PictureFileRA == null)
					continue;	// should NEVER happen!
				if (pict.PictureFileRA.InternalPath == sFile ||
					Path.GetFileName(pict.PictureFileRA.InternalPath) == sFile)
				{
					int cCurrent = MultiTsStringMatches(pict.Caption, lmtLabel);
					if (cCurrent >= cMatches)
					{
						pictMatching = pict;
						cMatches = cCurrent;
					}
				}
			}
			return pictMatching;
		}

		private bool SenseIllustrationsConflict(ILexSense ls, List<LiftUrlRef> list)
		{
			if (ls.PicturesOS.Count == 0 || list.Count == 0)
				return false;
			foreach (LiftUrlRef uref in list)
			{
				string sFile = FileUtils.StripFilePrefix(uref.Url);
				ICmPicture pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				if (pict == null)
					continue;
				if (MultiTsStringsConflict(pict.Caption, uref.Label))
				{
					m_cdConflict = new ConflictingSense("Picture Caption", ls, this);
					return true;
				}
			}
			return false;
		}

		private void ProcessSenseRelations(ILexSense ls, LiftSense sense)
		{
			// Due to possible forward references, wait until the end to process relations,
			// unless the target is empty.  In which case, add the relation to the residue.
			foreach (LiftRelation rel in sense.Relations)
			{
				if (String.IsNullOrEmpty(rel.Ref) && rel.Type != "_component-lexeme")
				{
					XmlDocument xdResidue = FindOrCreateResidue(ls);
					InsertResidueContent(xdResidue, CreateXmlForRelation(rel));
				}
				else
				{
					switch (rel.Type)
					{
						case "minorentry":
						case "subentry":
							// We'll just ignore these backreferences.
							break;
						case "main":
						case "_component-lexeme":
							// These shouldn't happen at a sense level, but...
							LiftEntry entry = sense.OwningEntry;
							PendingLexEntryRef pend = new PendingLexEntryRef(ls, rel, entry);
							pend.Residue = CreateRelationResidue(rel);
							m_rgPendingLexEntryRefs.Add(pend);
							break;
						default:
							string sResidue = CreateRelationResidue(rel);
							m_rgPendingRelation.Add(new PendingRelation(ls, rel, sResidue));
							break;
					}
				}
			}
		}

		private bool SenseRelationsConflict(ILexSense ls, List<LiftRelation> list)
		{
			// TODO: how do we detect conflicts in a list?
			return false;
		}

		private void ProcessSenseReversals(ILexSense ls, LiftSense sense)
		{
			foreach (LiftReversal rev in sense.Reversals)
			{
				IReversalIndexEntry rie = ProcessReversal(rev);
				if (rie != null && !ls.ReversalEntriesRC.Contains(rie))
					ls.ReversalEntriesRC.Add(rie);
			}
		}

		private IReversalIndexEntry ProcessReversal(LiftReversal rev)
		{
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs;
			IReversalIndexEntry rie = null;
			if (rev.Main == null)
			{
				IReversalIndex riOwning = FindOrCreateReversalIndex(rev.Form, rev.Type);
				if (riOwning == null)
					return null;
				if (!m_mapToMapToRie.TryGetValue(riOwning, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(riOwning, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, riOwning.EntriesOC);
			}
			else
			{
				IReversalIndexEntry rieOwner = ProcessReversal(rev.Main);	// recurse!
				if (!m_mapToMapToRie.TryGetValue(rieOwner, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(rieOwner, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, rieOwner.SubentriesOC);
			}
			MergeInMultiUnicode(rie.ReversalForm, ReversalIndexEntryTags.kflidReversalForm, rev.Form, rie.Guid);
			ProcessReversalGramInfo(rie, rev.GramInfo);
			return rie;
		}

		private IReversalIndexEntry FindOrCreateMatchingReversal(LiftMultiText form,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs,
			IFdoOwningCollection<IReversalIndexEntry> entriesOC)
		{
			IReversalIndexEntry rie = null;
			List<IReversalIndexEntry> rgrie;
			List<MuElement> rgmue = new List<MuElement>();
			foreach (string key in form.Keys)
			{
				int ws = GetWsFromLiftLang(key);
				string sNew = form[key].Text;
				string sNewNorm;
				if (String.IsNullOrEmpty(sNew))
					sNewNorm = sNew;
				else
					sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				MuElement mue = new MuElement(ws, sNewNorm);
				if (rie == null && mapToRIEs.TryGetValue(mue, out rgrie))
				{
					foreach (IReversalIndexEntry rieT in rgrie)
					{
						if (SameMultiUnicodeContent(form, rieT.ReversalForm))
						{
							rie = rieT;
							break;
						}
					}
				}
				rgmue.Add(mue);
			}
			if (rie == null)
			{
				rie = CreateNewReversalIndexEntry();
				entriesOC.Add(rie);
			}
			foreach (MuElement mue in rgmue)
				AddToReversalMap(mue, rie, mapToRIEs);
			return rie;
		}

		private IReversalIndex FindOrCreateReversalIndex(LiftMultiText contents, string type)
		{
			IReversalIndex riOwning = null;
			// For now, fudge "type" as the basic writing system associated with the reversal.
			string sWs = type;
			if (String.IsNullOrEmpty(sWs))
			{
				if (contents == null || contents.Keys.Count == 0)
					return null;
				if (contents.Keys.Count == 1)
					sWs = contents.FirstValue.Key;
				else
					sWs = contents.FirstValue.Key.Split(new char[] { '_', '-' })[0];
			}
			int ws = GetWsFromStr(sWs);
			if (ws == 0)
			{
				ws = GetWsFromLiftLang(sWs);
				if (GetWsFromStr(sWs) == 0)
					sWs = GetExistingWritingSystem(ws).Id;	// Must be old-style ICU Locale.
			}
			// A linear search should be safe here, because we don't expect more than 2 or 3
			// reversal indexes in any given project.
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				if (ri.WritingSystem == sWs)
				{
					riOwning = ri;
					break;
				}
			}
			if (riOwning == null)
			{
				riOwning = CreateNewReversalIndex();
				m_cache.LangProject.LexDbOA.ReversalIndexesOC.Add(riOwning);
				riOwning.WritingSystem = GetExistingWritingSystem(ws).Id;
			}
			return riOwning;
		}

		private void ProcessReversalGramInfo(IReversalIndexEntry rie, LiftGrammaticalInfo gram)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				rie.PartOfSpeechRA = null;
			if (gram == null || String.IsNullOrEmpty(gram.Value))
				return;
			string sPOS = gram.Value;
			IReversalIndex ri = rie.ReversalIndex;
			Dictionary<string, ICmPossibility> dict = null;
			int handle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
			if (m_dictWsReversalPos.ContainsKey(handle))
			{
				dict = m_dictWsReversalPos[handle];
			}
			else
			{
				dict = new Dictionary<string, ICmPossibility>();
				m_dictWsReversalPos.Add(handle, dict);
			}
			if (dict.ContainsKey(sPOS))
			{
				if (!m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
				{
					if (rie.PartOfSpeechRA == null)
						rie.PartOfSpeechRA = dict[sPOS] as IPartOfSpeech;
				}
				else
				{
					rie.PartOfSpeechRA = dict[sPOS] as IPartOfSpeech;
				}
			}
			else
			{
				IPartOfSpeech pos = CreateNewPartOfSpeech();
				if (ri.PartsOfSpeechOA == null)
				{
					ICmPossibilityListFactory fact = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
					ri.PartsOfSpeechOA = fact.Create();
				}
				ri.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
				// Use the name and abbreviation from a regular PartOfSpeech if available, otherwise
				// just use the key and hope the user can sort it out later.
				if (m_dictPos.ContainsKey(sPOS))
				{
					IPartOfSpeech posMain = m_dictPos[sPOS] as IPartOfSpeech;
					pos.Abbreviation.MergeAlternatives(posMain.Abbreviation);
					pos.Name.MergeAlternatives(posMain.Name);
				}
				else
				{
					pos.Abbreviation.set_String(m_cache.DefaultAnalWs, sPOS);
					pos.Name.set_String(m_cache.DefaultAnalWs, sPOS);
				}
				if (!m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
				{
					if (rie.PartOfSpeechRA == null)
						rie.PartOfSpeechRA = pos;
				}
				else
				{
					rie.PartOfSpeechRA = pos;
				}
				dict.Add(sPOS, pos);
			}
		}

		private bool SenseReversalsConflict(ILexSense ls, List<LiftReversal> list)
		{
			// how do we detect conflicts in a list?
			return false;
		}

		private void ProcessSenseNotes(ILexSense ls, LiftSense sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				MergeInMultiString(ls.AnthroNote, LexSenseTags.kflidAnthroNote, null, ls.Guid);
				MergeInMultiString(ls.Bibliography, LexSenseTags.kflidBibliography, null, ls.Guid);
				MergeInMultiString(ls.DiscourseNote, LexSenseTags.kflidDiscourseNote, null, ls.Guid);
				MergeInMultiString(ls.EncyclopedicInfo, LexSenseTags.kflidEncyclopedicInfo, null, ls.Guid);
				MergeInMultiString(ls.GeneralNote, LexSenseTags.kflidGeneralNote, null, ls.Guid);
				MergeInMultiString(ls.GrammarNote, LexSenseTags.kflidGrammarNote, null, ls.Guid);
				MergeInMultiString(ls.PhonologyNote, LexSenseTags.kflidPhonologyNote, null, ls.Guid);
				MergeInMultiUnicode(ls.Restrictions, LexSenseTags.kflidRestrictions, null, ls.Guid);
				MergeInMultiString(ls.SemanticsNote, LexSenseTags.kflidSemanticsNote, null, ls.Guid);
				MergeInMultiString(ls.SocioLinguisticsNote, LexSenseTags.kflidSocioLinguisticsNote, null, ls.Guid);
				ls.Source = null;
			}
			foreach (LiftNote note in sense.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "anthropology":
						MergeInMultiString(ls.AnthroNote, LexSenseTags.kflidAnthroNote, note.Content, ls.Guid);
						break;
					case "bibliography":
						MergeInMultiString(ls.Bibliography, LexSenseTags.kflidBibliography, note.Content, ls.Guid);
						break;
					case "discourse":
						MergeInMultiString(ls.DiscourseNote, LexSenseTags.kflidDiscourseNote, note.Content, ls.Guid);
						break;
					case "encyclopedic":
						MergeInMultiString(ls.EncyclopedicInfo, LexSenseTags.kflidEncyclopedicInfo, note.Content, ls.Guid);
						break;
					case "":		// WeSay uses untyped notes in senses; LIFT now exports like this.
					case "general":	// older Flex exported LIFT files have this type value.
						MergeInMultiString(ls.GeneralNote, LexSenseTags.kflidGeneralNote, note.Content, ls.Guid);
						break;
					case "grammar":
						MergeInMultiString(ls.GrammarNote, LexSenseTags.kflidGrammarNote, note.Content, ls.Guid);
						break;
					case "phonology":
						MergeInMultiString(ls.PhonologyNote, LexSenseTags.kflidPhonologyNote, note.Content, ls.Guid);
						break;
					case "restrictions":
						MergeInMultiUnicode(ls.Restrictions, LexSenseTags.kflidRestrictions, note.Content, ls.Guid);
						break;
					case "semantics":
						MergeInMultiString(ls.SemanticsNote, LexSenseTags.kflidSemanticsNote, note.Content, ls.Guid);
						break;
					case "sociolinguistics":
						MergeInMultiString(ls.SocioLinguisticsNote, LexSenseTags.kflidSocioLinguisticsNote, note.Content, ls.Guid);
						break;
					case "source":
						ls.Source = StoreTsStringValue(m_fCreatingNewSense, ls.Source, note.Content);
						break;
					default:
						StoreNoteAsResidue(ls, note);
						break;
				}
			}
		}

		private bool SenseNotesConflict(ILexSense ls, List<LiftNote> list)
		{
			foreach (LiftNote note in list)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "anthropology":
						if (MultiTsStringsConflict(ls.AnthroNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Anthropology Note", ls, this);
							return true;
						}
						break;
					case "bibliography":
						if (MultiTsStringsConflict(ls.Bibliography, note.Content))
						{
							m_cdConflict = new ConflictingSense("Bibliography", ls, this);
							return true;
						}
						break;
					case "discourse":
						if (MultiTsStringsConflict(ls.DiscourseNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Discourse Note", ls, this);
							return true;
						}
						break;
					case "encyclopedic":
						if (MultiTsStringsConflict(ls.EncyclopedicInfo, note.Content))
						{
							m_cdConflict = new ConflictingSense("Encyclopedic Info", ls, this);
							return true;
						}
						break;
					case "general":
						if (MultiTsStringsConflict(ls.GeneralNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("General Note", ls, this);
							return true;
						}
						break;
					case "grammar":
						if (MultiTsStringsConflict(ls.GrammarNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Grammar Note", ls, this);
							return true;
						}
						break;
					case "phonology":
						if (MultiTsStringsConflict(ls.PhonologyNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Phonology Note", ls, this);
							return true;
						}
						break;
					case "restrictions":
						if (MultiUnicodeStringsConflict(ls.Restrictions, note.Content, false, Guid.Empty, 0))
						{
							m_cdConflict = new ConflictingSense("Restrictions", ls, this);
							return true;
						}
						break;
					case "semantics":
						if (MultiTsStringsConflict(ls.SemanticsNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Semantics Note", ls, this);
							return true;
						}
						break;
					case "sociolinguistics":
						if (MultiTsStringsConflict(ls.SocioLinguisticsNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Sociolinguistics Note", ls, this);
							return true;
						}
						break;
					case "source":
						if (StringsConflict(ls.Source, GetFirstLiftTsString(note.Content)))
						{
							m_cdConflict = new ConflictingSense("Source", ls, this);
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessSenseFields(ILexSense ls, LiftSense sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				ls.ImportResidue = m_tssEmpty;
				ls.ScientificName = m_tssEmpty;
				ClearCustomFields(LexSenseTags.kClassId);
			}
			foreach (LiftField field in sense.Fields)
			{
				string sType = field.Type;
				switch (sType.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						ls.ImportResidue = StoreTsStringValue(m_fCreatingNewSense, ls.ImportResidue, field.Content);
						break;
					case "scientific_name":	// original FLEX export
					case "scientific-name":
						ls.ScientificName = StoreTsStringValue(m_fCreatingNewSense, ls.ScientificName, field.Content);
						break;
					default:
						ProcessUnknownField(ls, sense, field,
							"LexSense", "custom-sense-", LexSenseTags.kClassId);
						break;
				}
			}
		}

		private void ClearCustomFields(int clsid)
		{
			// TODO: Implement this!
		}

		/// <summary>
		/// Try to find find (or create) a custom field to store this data in.  If all else
		/// fails, store it in the LiftResidue field.
		/// </summary>
		/// <param name="co"></param>
		/// <param name="obj"></param>
		/// <param name="field"></param>
		/// <param name="sClass"></param>
		/// <param name="sOldPrefix"></param>
		/// <param name="clid"></param>
		private void ProcessUnknownField(ICmObject co, LiftObject obj, LiftField field,
			string sClass, string sOldPrefix, int clid)
		{
			string sType = field.Type;
			if (sType.StartsWith(sOldPrefix))
				sType = sType.Substring(sOldPrefix.Length);
			Debug.Assert(sType.Length > 0);
			string sTag = String.Format("{0}-{1}", sClass, sType);
			int flid;
			if (m_dictCustomFlid.TryGetValue(sTag, out flid))
			{
				ProcessCustomFieldData(co.Hvo, flid, field.Content);
			}
			else
			{
				LiftMultiText desc = null;
				if (!m_dictFieldDef.TryGetValue(sType, out desc))
				{
					m_dictFieldDef.TryGetValue(sOldPrefix + sType, out desc);
				}
				flid = FindOrCreateCustomField(sType, desc, clid);
				if (flid == 0)
				{
					if (clid == LexSenseTags.kClassId || clid == LexExampleSentenceTags.kClassId)
						FindOrCreateResidue(co, obj.Id, LexSenseTags.kflidLiftResidue);
					else
						FindOrCreateResidue(co, obj.Id, LexEntryTags.kflidLiftResidue);
					StoreFieldAsResidue(co, field);
				}
				else
				{
					ProcessCustomFieldData(co.Hvo, flid, field.Content);
				}
			}
		}

		private ITsString StoreTsStringValue(bool fCreatingNew, ITsString tssOld, LiftMultiText lmt)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				tssOld = m_tssEmpty;
			ITsString tss = GetFirstLiftTsString(lmt);
			if (TsStringIsNullOrEmpty(tss))
				return tssOld;
			if (m_msImport == MergeStyle.MsKeepOld && !fCreatingNew)
			{
				if (TsStringIsNullOrEmpty(tssOld))
					tssOld= tss;
			}
			else
			{
				tssOld = tss;
			}
			return tssOld;
		}

		private bool SenseFieldsConflict(ILexSense ls, List<LiftField> list)
		{
			foreach (LiftField field in list)
			{
				string sType = field.Type;
				switch (sType.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						if (ls.ImportResidue != null && ls.ImportResidue.Length != 0)
						{
							ITsStrBldr tsb = ls.ImportResidue.GetBldr();
							int idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
								tsb.Replace(idx, tsb.Length, null, null);
							if (StringsConflict(tsb.GetString(), GetFirstLiftTsString(field.Content)))
							{
								m_cdConflict = new ConflictingSense("Import Residue", ls, this);
								return true;
							}
						}
						break;
					case "scientific_name":	// original FLEX export
					case "scientific-name":
						if (StringsConflict(ls.ScientificName, GetFirstLiftTsString(field.Content)))
						{
							m_cdConflict = new ConflictingSense("Scientific Name", ls, this);
							return true;
						}
						break;
					default:
						int flid;
						if (m_dictCustomFlid.TryGetValue("LexSense-" + sType, out flid))
						{
							if (CustomFieldDataConflicts(ls.Hvo, flid, field.Content))
							{
								m_cdConflict = new ConflictingSense(String.Format("{0} (custom field)", sType), ls, this);
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessSenseTraits(ILexSense ls, LiftSense sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				ls.AnthroCodesRC.Clear();
				ls.SemanticDomainsRC.Clear();
				ls.DomainTypesRC.Clear();
				ls.SenseTypeRA = null;
				ls.StatusRA = null;
				ls.UsageTypesRC.Clear();
			}
			ICmPossibility poss;
			foreach (LiftTrait lt in sense.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "anthro-code":
						ICmAnthroItem ant = FindOrCreateAnthroCode(lt.Value);
						if (!ls.AnthroCodesRC.Contains(ant))
							ls.AnthroCodesRC.Add(ant);
						break;
					case "semanticdomainddp4":	// for WeSay 0.4 compatibility
					case "semantic_domain":
					case "semantic-domain":
					case "semantic-domain-ddp4":
						ICmSemanticDomain sem = FindOrCreateSemanticDomain(lt.Value);
						if (!ls.SemanticDomainsRC.Contains(sem))
							ls.SemanticDomainsRC.Add(sem);
						break;
					case "domaintype":	// original FLEX export = DomainType
					case "domain-type":
						poss = FindOrCreateDomainType(lt.Value);
						if (!ls.DomainTypesRC.Contains(poss))
							ls.DomainTypesRC.Add(poss);
						break;
					case "sensetype":	// original FLEX export = SenseType
					case "sense-type":
						poss = FindOrCreateSenseType(lt.Value);
						if (ls.SenseTypeRA != poss && (m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld || ls.SenseTypeRA == null))
							ls.SenseTypeRA = poss;
						break;
					case "status":
						poss = FindOrCreateStatus(lt.Value);
						if (ls.StatusRA != poss && (m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld || ls.StatusRA == null))
							ls.StatusRA = poss;
						break;
					case "usagetype":	// original FLEX export = UsageType
					case "usage-type":
						poss = FindOrCreateUsageType(lt.Value);
						if (!ls.UsageTypesRC.Contains(poss))
							ls.UsageTypesRC.Add(poss);
						break;
					default:
						StoreTraitAsResidue(ls, lt);
						break;
				}
			}
		}

		private bool SenseTraitsConflict(ILexSense ls, List<LiftTrait> list)
		{
			ICmPossibility poss;
			foreach (LiftTrait lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "anthro-code":
						ICmAnthroItem ant = FindOrCreateAnthroCode(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case "semanticdomainddp4":	// for WeSay 0.4 compatibility
					case "semantic_domain":
					case "semantic-domain":
					case "semantic-domain-ddp4":
						poss = FindOrCreateSemanticDomain(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case "domaintype":	// original FLEX export = DomainType
					case "domain-type":
						poss = FindOrCreateDomainType(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case "sensetype":	// original FLEX export = SenseType
					case "sense-type":
						poss = FindOrCreateSenseType(lt.Value);
						if (ls.SenseTypeRA != null && ls.SenseTypeRA != poss)
						{
							m_cdConflict = new ConflictingSense("Sense Type", ls, this);
							return true;
						}
						break;
					case "status":
						poss = FindOrCreateStatus(lt.Value);
						if (ls.StatusRA != null && ls.StatusRA != poss)
						{
							m_cdConflict = new ConflictingSense("Status", ls, this);
							return true;
						}
						break;
					case "usagetype":	// original FLEX export = UsageType
					case "usage-type":
						poss = FindOrCreateUsageType(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
				}
			}
			return false;
		}
		#endregion // Methods for storing entry data

		#region Methods for getting or creating model objects

		internal ICmObject GetObjectForId(int hvo)
		{
			if (m_repoCmObject == null)
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			try
			{
				return m_repoCmObject.GetObject(hvo);
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		internal ICmObject GetObjectForGuid(Guid guid)
		{
			if (m_repoCmObject == null)
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (m_repoCmObject.IsValidObjectId(guid))
				return m_repoCmObject.GetObject(guid);
			else
				return null;
		}

		internal IWritingSystem GetExistingWritingSystem(int handle)
		{
			return m_cache.ServiceLocator.WritingSystemManager.Get(handle);
		}


		internal IMoMorphType GetExistingMoMorphType(Guid guid)
		{
			if (m_repoMoMorphType == null)
				m_repoMoMorphType = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			return m_repoMoMorphType.GetObject(guid);
		}

		internal ICmAnthroItem CreateNewCmAnthroItem(string guidAttr, ICmObject owner)
		{
			if (m_factCmAnthroItem == null)
				m_factCmAnthroItem = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is ICmAnthroItem)
					return m_factCmAnthroItem.Create(guid, owner as ICmAnthroItem);
				else
					return m_factCmAnthroItem.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				ICmAnthroItem cai = m_factCmAnthroItem.Create();
				if (owner is ICmAnthroItem)
					(owner as ICmAnthroItem).SubPossibilitiesOS.Add(cai);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(cai);
				return cai;
			}
		}

		internal ICmAnthroItem CreateNewCmAnthroItem()
		{
			if (m_factCmAnthroItem == null)
				m_factCmAnthroItem = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
			return m_factCmAnthroItem.Create();
		}

		internal ICmSemanticDomain CreateNewCmSemanticDomain()
		{
			if (m_factCmSemanticDomain == null)
				m_factCmSemanticDomain = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			return m_factCmSemanticDomain.Create();
		}

		internal ICmSemanticDomain CreateNewCmSemanticDomain(string guidAttr, ICmObject owner)
		{
			if (m_factCmSemanticDomain == null)
				m_factCmSemanticDomain = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is ICmSemanticDomain)
					return m_factCmSemanticDomain.Create(guid, owner as ICmSemanticDomain);
				else
					return m_factCmSemanticDomain.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				ICmSemanticDomain csd = m_factCmSemanticDomain.Create();
				if (owner is ICmSemanticDomain)
					(owner as ICmSemanticDomain).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		internal IMoStemAllomorph CreateNewMoStemAllomorph()
		{
			if (m_factMoStemAllomorph == null)
				m_factMoStemAllomorph = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			return m_factMoStemAllomorph.Create();
		}

		internal IMoAffixAllomorph CreateNewMoAffixAllomorph()
		{
			if (m_factMoAffixAllomorph == null)
				m_factMoAffixAllomorph = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();
			return m_factMoAffixAllomorph.Create();
		}

		internal ILexPronunciation CreateNewLexPronunciation()
		{
			if (m_factLexPronunciation == null)
				m_factLexPronunciation = m_cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			return m_factLexPronunciation.Create();
		}

		internal ICmMedia CreateNewCmMedia()
		{
			if (m_factCmMedia == null)
				m_factCmMedia = m_cache.ServiceLocator.GetInstance<ICmMediaFactory>();
			return m_factCmMedia.Create();
		}

		internal ILexEtymology CreateNewLexEtymology()
		{
			if (m_factLexEtymology == null)
				m_factLexEtymology = m_cache.ServiceLocator.GetInstance<ILexEtymologyFactory>();
			return m_factLexEtymology.Create();
		}

		internal ILexSense CreateNewLexSense(Guid guid, ICmObject owner, out bool fNeedNewId)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ILexEntry || owner is ILexSense);
			if (m_factLexSense == null)
				m_factLexSense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			fNeedNewId = false;
			ILexSense ls = null;
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
			{
				if (owner is ILexEntry)
					ls = m_factLexSense.Create(guid, owner as ILexEntry);
				else
					ls = m_factLexSense.Create(guid, owner as ILexSense);

			}
			if (ls == null)
			{
				ls = m_factLexSense.Create();
				if (owner is ILexEntry)
					(owner as ILexEntry).SensesOS.Add(ls);
				else
					(owner as ILexSense).SensesOS.Add(ls);
				fNeedNewId = guid != Guid.Empty;
			}
			return ls;
		}

		private bool GuidIsNotInUse(Guid guid)
		{
			if (m_deletedGuids.Contains(guid))
				return false;
			if (m_repoCmObject == null)
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			return !m_repoCmObject.IsValidObjectId(guid);
		}

		private ILexEntry CreateNewLexEntry(Guid guid, out bool fNeedNewId)
		{
			if (m_factLexEntry == null)
				m_factLexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			fNeedNewId = false;
			ILexEntry le = null;
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
				le = m_factLexEntry.Create(guid, m_cache.LangProject.LexDbOA);
			if (le == null)
			{
				le = m_factLexEntry.Create();
				fNeedNewId = guid != Guid.Empty;
			}
			return le;
		}

		internal IMoInflClass CreateNewMoInflClass()
		{
			if (m_factMoInflClass == null)
				m_factMoInflClass = m_cache.ServiceLocator.GetInstance<IMoInflClassFactory>();
			return m_factMoInflClass.Create();
		}

		internal IMoInflAffixSlot CreateNewMoInflAffixSlot()
		{
			if (m_factMoInflAffixSlot == null)
				m_factMoInflAffixSlot = m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>();
			return m_factMoInflAffixSlot.Create();
		}

		internal ILexExampleSentence CreateNewLexExampleSentence(Guid guid, ILexSense owner)
		{
			Debug.Assert(owner != null);
			if (m_factLexExampleSentence == null)
				m_factLexExampleSentence = m_cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
			{
				return m_factLexExampleSentence.Create(guid, owner);
			}
			else
			{
				ILexExampleSentence les = m_factLexExampleSentence.Create();
				owner.ExamplesOS.Add(les);
				return les;
			}
		}

		internal ICmTranslation CreateNewCmTranslation(ILexExampleSentence les, ICmPossibility type)
		{
			if (m_factCmTranslation == null)
				m_factCmTranslation = m_cache.ServiceLocator.GetInstance<ICmTranslationFactory>();
			bool fNoType = type == null;
			if (fNoType)
			{
				ICmObject obj;
				if (m_repoCmObject.TryGetObject(LangProjectTags.kguidTranFreeTranslation, out obj))
					type = obj as ICmPossibility;
				if (type == null)
					type = FindOrCreateTranslationType("Free translation");
			}
			ICmTranslation trans = m_factCmTranslation.Create(les, type);
			if (fNoType)
				trans.TypeRA = null;
			return trans;
		}

		internal ILexEntryType CreateNewLexEntryType()
		{
			if (m_factLexEntryType == null)
				m_factLexEntryType = m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
			return m_factLexEntryType.Create();
		}

		internal ILexRefType CreateNewLexRefType()
		{
			if (m_factLexRefType == null)
				m_factLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			return m_factLexRefType.Create();
		}

		internal ICmPossibility CreateNewCmPossibility()
		{
			if (m_factCmPossibility == null)
				m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			return m_factCmPossibility.Create();
		}

		internal ICmPossibility CreateNewCmPossibility(string guidAttr, ICmObject owner)
		{
			if (m_factCmPossibility == null)
				m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is ICmPossibility)
					return m_factCmPossibility.Create(guid, owner as ICmPossibility);
				else
					return m_factCmPossibility.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				ICmPossibility csd = m_factCmPossibility.Create();
				if (owner is ICmPossibility)
					(owner as ICmPossibility).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		internal ICmLocation CreateNewCmLocation()
		{
			if (m_factCmLocation == null)
				m_factCmLocation = m_cache.ServiceLocator.GetInstance<ICmLocationFactory>();
			return m_factCmLocation.Create();
		}

		internal IMoStemMsa CreateNewMoStemMsa()
		{
			if (m_factMoStemMsa == null)
				m_factMoStemMsa = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			return m_factMoStemMsa.Create();
		}

		internal IMoUnclassifiedAffixMsa CreateNewMoUnclassifiedAffixMsa()
		{
			if (m_factMoUnclassifiedAffixMsa == null)
				m_factMoUnclassifiedAffixMsa = m_cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>();
			return m_factMoUnclassifiedAffixMsa.Create();
		}

		internal IMoDerivStepMsa CreateNewMoDerivStepMsa()
		{
			if (m_factMoDerivStepMsa == null)
				m_factMoDerivStepMsa = m_cache.ServiceLocator.GetInstance<IMoDerivStepMsaFactory>();
			return m_factMoDerivStepMsa.Create();
		}

		internal IMoDerivAffMsa CreateNewMoDerivAffMsa()
		{
			if (m_factMoDerivAffMsa == null)
				m_factMoDerivAffMsa = m_cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>();
			return m_factMoDerivAffMsa.Create();
		}

		internal IMoInflAffMsa CreateNewMoInflAffMsa()
		{
			if (m_factMoInflAffMsa == null)
				m_factMoInflAffMsa = m_cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>();
			return m_factMoInflAffMsa.Create();
		}

		internal ICmPicture CreateNewCmPicture()
		{
			if (m_factCmPicture == null)
				m_factCmPicture = m_cache.ServiceLocator.GetInstance<ICmPictureFactory>();
			return m_factCmPicture.Create();
		}

		internal IReversalIndexEntry CreateNewReversalIndexEntry()
		{
			if (m_factReversalIndexEntry == null)
				m_factReversalIndexEntry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			return m_factReversalIndexEntry.Create();
		}

		internal IReversalIndex CreateNewReversalIndex()
		{
			if (m_factReversalIndex == null)
				m_factReversalIndex = m_cache.ServiceLocator.GetInstance<IReversalIndexFactory>();
			return m_factReversalIndex.Create();
		}

		internal IPartOfSpeech CreateNewPartOfSpeech()
		{
			if (m_factPartOfSpeech == null)
				m_factPartOfSpeech = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			return m_factPartOfSpeech.Create();
		}

		internal IPartOfSpeech CreateNewPartOfSpeech(string guidAttr, ICmObject owner)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ICmPossibilityList || owner is IPartOfSpeech);
			if (m_factPartOfSpeech == null)
				m_factPartOfSpeech = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is IPartOfSpeech)
					return m_factPartOfSpeech.Create(guid, owner as IPartOfSpeech);
				else
					return m_factPartOfSpeech.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				IPartOfSpeech csd = m_factPartOfSpeech.Create();
				if (owner is IPartOfSpeech)
					(owner as IPartOfSpeech).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		internal IMoMorphType CreateNewMoMorphType(string guidAttr, ICmObject owner)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ICmPossibilityList || owner is IMoMorphType);
			if (m_factMoMorphType == null)
				m_factMoMorphType = m_cache.ServiceLocator.GetInstance<IMoMorphTypeFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is IMoMorphType)
					return m_factMoMorphType.Create(guid, owner as IMoMorphType);
				else
					return m_factMoMorphType.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				IMoMorphType csd = m_factMoMorphType.Create();
				if (owner is IMoMorphType)
					(owner as IMoMorphType).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		private IPhEnvironment CreateNewPhEnvironment()
		{
			if (m_factPhEnvironment == null)
				m_factPhEnvironment = m_cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>();
			return m_factPhEnvironment.Create();
		}

		private ILexReference CreateNewLexReference()
		{
			if (m_factLexReference == null)
				m_factLexReference = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>();
			return m_factLexReference.Create();
		}

		private ILexEntryRef CreateNewLexEntryRef()
		{
			if (m_factLexEntryRef == null)
				m_factLexEntryRef = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
			return m_factLexEntryRef.Create();
		}

		private int GetWsFromStr(string sWs)
		{
			return m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(sWs);
		}
		#endregion // Methods for getting or creating model objects

		private void EnsureValidMSAsForSenses(ILexEntry le)
		{
			bool fIsAffix = IsAffixType(le);
			foreach (ILexSense ls in GetAllSenses(le))
			{
				if (ls.MorphoSyntaxAnalysisRA != null)
					continue;
				IMoMorphSynAnalysis msa;
				if (fIsAffix)
				{
					msa = FindEmptyAffixMsa(le);
					if (msa == null)
					{
						msa = CreateNewMoUnclassifiedAffixMsa();
						le.MorphoSyntaxAnalysesOC.Add(msa);
					}
				}
				else
				{
					msa = FindEmptyStemMsa(le);
					if (msa == null)
					{
						msa = CreateNewMoStemMsa();
						le.MorphoSyntaxAnalysesOC.Add(msa);
					}
				}
				ls.MorphoSyntaxAnalysisRA = msa;
			}
		}

		private IEnumerable<ILexSense> GetAllSenses(ILexEntry le)
		{
			List<ILexSense> rgls = new List<ILexSense>();
			foreach (ILexSense ls in le.SensesOS)
			{
				rgls.Add(ls);
				GetAllSubsenses(ls, rgls);
			}
			return rgls;
		}

		private void GetAllSubsenses(ILexSense ls, List<ILexSense> rgls)
		{
			foreach (ILexSense lsSub in ls.SensesOS)
			{
				rgls.Add(lsSub);
				GetAllSubsenses(lsSub, rgls);
			}
		}

		/// <summary>
		/// Is this entry an affix type?
		/// </summary>
		public bool IsAffixType(ILexEntry le)
		{
			IMoForm lfForm = le.LexemeFormOA;
			int cTypes = 0;
			if (lfForm != null)
			{
				IMoMorphType mmt = lfForm.MorphTypeRA;
				if (mmt != null)
				{
					if (mmt.IsStemType)
						return false;
					++cTypes;
				}
			}
			foreach (IMoForm form in le.AlternateFormsOS)
			{
				IMoMorphType mmt = form.MorphTypeRA;
				if (mmt != null)
				{
					if (mmt.IsStemType)
						return false;
					++cTypes;
				}
			}
			return cTypes > 0;		// assume stem if no type information.
		}

		private IMoMorphSynAnalysis FindEmptyAffixMsa(ILexEntry le)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoUnclassifiedAffixMsa msaAffix = msa as IMoUnclassifiedAffixMsa;
				if (msaAffix != null && msaAffix.PartOfSpeechRA == null)
					return msa;
			}
			return null;
		}

		private IMoMorphSynAnalysis FindEmptyStemMsa(ILexEntry le)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoStemMsa msaStem = msa as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRA == null &&
					msaStem.FromPartsOfSpeechRC.Count == 0 &&
					msaStem.InflectionClassRA == null &&
					msaStem.ProdRestrictRC.Count == 0 &&
					msaStem.StratumRA == null &&
					msaStem.MsFeaturesOA == null)
				{
					return msaStem;
				}
			}
			return null;
		}

		/// <summary>
		/// This method is a temporary (?) expedient for reading the morph-type information
		/// from a .lift-ranges file.  Someday, the LiftIO.Parsing.LiftParser (or its
		/// Palaso replacement) should handle href values in range elements so that this
		/// method will not be needed.  Without doing this, the user can export morph-type
		/// values in something other than English, and lift import blows up.  See FWR-3869.
		///
		/// Only the morph-type range is handled at present, because the other ranges do not
		/// assume successful matching.
		/// </summary>
		internal void LoadLiftRanges(string sRangesFile)
		{
			try
			{
				if (!File.Exists(sRangesFile))
					return;
				var xdoc = new XmlDocument();
				xdoc.Load(sRangesFile);
				foreach (XmlNode xn in xdoc.ChildNodes)
				{
					if (xn.Name != "lift-ranges")
						continue;
					foreach (XmlNode xnRange in xn.ChildNodes)
					{
						if (xnRange.Name != "range")
							continue;
						var range = XmlUtils.GetAttributeValue(xnRange, "id");
						foreach (XmlNode xnElem in xnRange.ChildNodes)
						{
							if (xnElem.Name != "range-element")
								continue;
							ProcessRangeElement(xnElem, range);
						}
					}
				}
			}
			catch (Exception)
			{
				// swallow any exception...
			}
		}

		private void ProcessRangeElement(XmlNode xnElem, string range)
		{
			switch (range)
			{
				case "semanticdomainddp4":
				case "semantic_domain":
				case "semantic-domain":
				case "semantic-domain-ddp4":
				case "anthro_codes":
				case "anthro-code":
				case "status":
				case "users":
				case "translation-types":
				case "translation-type":
				case "grammatical-info":
				case "FromPartOfSpeech":
				case "from-part-of-speech":
					break;
				case "MorphType": // original FLEX export
				case "morph-type":
					var id = XmlUtils.GetAttributeValue(xnElem, "id");
					var parent = XmlUtils.GetAttributeValue(xnElem, "parent");
					var guidAttr = XmlUtils.GetAttributeValue(xnElem, "guid");
					//var rawXml = xnElem.OuterXml;
					XmlNode xnLabel = null;
					XmlNode xnAbbrev = null;
					XmlNode xnDescription = null;
					foreach (XmlNode xn in xnElem.ChildNodes)
					{
						switch (xn.Name)
						{
							case "label": xnLabel = xn; break;
							case "abbrev": xnAbbrev = xn; break;
							case "description": xnDescription = xn; break;
						}
					}
					LiftMultiText label = null;
					if (xnLabel != null)
						label = new LiftMultiText(xnLabel.OuterXml);
					LiftMultiText abbrev = null;
					if (xnAbbrev != null)
						abbrev = new LiftMultiText(xnAbbrev.OuterXml);
					LiftMultiText description = null;
					if (xnDescription != null)
						description = new LiftMultiText(xnDescription.OuterXml);
					ProcessMorphType(id, guidAttr, parent, description, label, abbrev);
					break;
				case "exception-feature":
				case "inflection-feature":
				case "inflection-feature-type":
					break;
				default:
					break;
			}
		}
	}
}
