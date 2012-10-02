// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LiftMerger.cs
// Responsibility: SteveMc (original version by John Hatton as extension)
// Last reviewed:
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

using LiftIO;
using LiftIO.Parsing;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

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
		LiftIO.Parsing.LiftMultiText m_mtComment;

		public LiftAnnotation()
		{
		}
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
		public LiftIO.Parsing.LiftMultiText Comment
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
		List<LiftAnnotation> m_rgAnnotations = new List<LiftAnnotation>();

		public LiftTrait()
		{
		}
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
		List<LiftTrait> m_rgTraits = new List<LiftTrait>();
		List<LiftAnnotation> m_rgAnnotations = new List<LiftAnnotation>();
		LiftIO.Parsing.LiftMultiText m_mtContent;
		public LiftField()
		{
		}
		public LiftField(string type, LiftIO.Parsing.LiftMultiText contents)
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
		public LiftIO.Parsing.LiftMultiText Content
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
		int m_hvo;
		Guid m_guid;
		DateTime m_dateCreated;
		DateTime m_dateModified;
		List<LiftField> m_rgFields = new List<LiftField>();
		List<LiftTrait> m_rgTraits = new List<LiftTrait>();
		List<LiftAnnotation> m_rgAnnotations = new List<LiftAnnotation>();

		public LiftObject()
		{
			m_hvo = 0;
			m_dateCreated = DateTime.MinValue;
			m_dateModified = DateTime.MinValue;
		}
		public string Id
		{
			get { return m_id; }
			set { m_id = value; }
		}
		public int Hvo
		{
			get { return m_hvo; }
			set { m_hvo = value; }
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
		LiftIO.Parsing.LiftMultiText m_mtContent;

		public LiftTranslation()
		{
		}
		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public LiftIO.Parsing.LiftMultiText Content
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
		LiftIO.Parsing.LiftMultiText m_mtContent;
		List<LiftTranslation> m_rgTranslations = new List<LiftTranslation>();
		List<LiftNote> m_rgNotes = new List<LiftNote>();	// not really in LIFT 0.12?

		public LiftExample()
		{
		}
		public string Source
		{
			get { return m_source; }
			set { m_source = value; }
		}
		public LiftIO.Parsing.LiftMultiText Content
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
		List<LiftTrait> m_rgTraits = new List<LiftTrait>();

		public LiftGrammaticalInfo()
		{
		}
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
		LiftIO.Parsing.LiftMultiText m_mtForm;
		LiftReversal m_main;
		LiftGrammaticalInfo m_graminfo;

		public LiftReversal()
		{
		}
		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public LiftIO.Parsing.LiftMultiText Form
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
		LiftIO.Parsing.LiftMultiText m_mtGloss;
		LiftIO.Parsing.LiftMultiText m_mtDefinition;
		List<LiftRelation> m_rgRelations = new List<LiftRelation>();
		List<LiftNote> m_rgNotes = new List<LiftNote>();
		List<LiftExample> m_rgExamples = new List<LiftExample>();
		List<LiftReversal> m_rgReversals = new List<LiftReversal>();
		List<LiftURLRef> m_rgPictures = new List<LiftURLRef>();
		List<LiftSense> m_rgSenses = new List<LiftSense>();
		LiftObject m_owner = null;

		public LiftSense()
		{
		}
		public LiftSense(Extensible info, Guid guid, FdoCache cache, LiftObject owner)
		{
			this.Id = info.Id;
			this.Guid = guid;
			if (guid == Guid.Empty)
				this.Hvo = 0;
			else
				this.Hvo = cache.GetIdFromGuid(guid);
			this.DateCreated = info.CreationTime;
			this.DateModified = info.ModificationTime;
			this.m_owner = owner;
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
		public LiftIO.Parsing.LiftMultiText Gloss
		{
			get { return m_mtGloss; }
			set { m_mtGloss = value; }
		}
		public LiftIO.Parsing.LiftMultiText Definition
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
		public List<LiftURLRef> Illustrations
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
					if (owner == null)
						return null;
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
	public class LiftURLRef
	{
		string m_href;
		LiftIO.Parsing.LiftMultiText m_mtLabel;

		public LiftURLRef()
		{
		}
		public string URL
		{
			get { return m_href; }
			set { m_href = value; }
		}
		public LiftIO.Parsing.LiftMultiText Label
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
		LiftIO.Parsing.LiftMultiText m_mtContent;

		public LiftNote()
		{
		}
		public LiftNote(string type, LiftIO.Parsing.LiftMultiText contents)
		{
			m_type = type;
			m_mtContent = contents;
		}
		public string Type
		{
			get { return m_type; }
			set { m_type = value; }
		}
		public LiftIO.Parsing.LiftMultiText Content
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
		LiftIO.Parsing.LiftMultiText m_mtForm;
		List<LiftURLRef> m_rgMedia = new List<LiftURLRef>();

		public LiftPhonetic()
		{
		}
		public LiftIO.Parsing.LiftMultiText Form
		{
			get { return m_mtForm; }
			set { m_mtForm = value; }
		}
		public List<LiftURLRef> Media
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
		LiftIO.Parsing.LiftMultiText m_mtGloss;
		LiftIO.Parsing.LiftMultiText m_mtForm;

		public LiftEtymology()
		{
		}
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
		public LiftIO.Parsing.LiftMultiText Gloss
		{
			get { return m_mtGloss; }
			set { m_mtGloss = value; }
		}
		public LiftIO.Parsing.LiftMultiText Form
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
		LiftIO.Parsing.LiftMultiText m_mtUsage;

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
		public LiftIO.Parsing.LiftMultiText Usage
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
		LiftIO.Parsing.LiftMultiText m_mtVariantForm;
		List<LiftPhonetic> m_rgPronunciations = new List<LiftPhonetic>();
		List<LiftRelation> m_rgRelations = new List<LiftRelation>();
		string m_sRawXml = null;

		public LiftVariant()
		{
		}
		public string Ref
		{
			get { return m_ref; }
			set { m_ref = value; }
		}
		public LiftIO.Parsing.LiftMultiText Form
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
		LiftIO.Parsing.LiftMultiText m_mtLexicalForm;
		LiftIO.Parsing.LiftMultiText m_mtCitation;
		List<LiftPhonetic> m_rgPronunciations = new List<LiftPhonetic>();
		List<LiftVariant> m_rgVariants = new List<LiftVariant>();
		List<LiftSense> m_rgSenses = new List<LiftSense>();
		List<LiftNote> m_rgNotes = new List<LiftNote>();
		List<LiftRelation> m_rgRelations = new List<LiftRelation>();
		List<LiftEtymology> m_rgEtymologies = new List<LiftEtymology>();
		/// <summary>preserve trait value from older LIFT files based on old FieldWorks model</summary>
		string m_sEntryType = null;
		string m_sMinorEntryCondition = null;
		bool m_fExcludeAsHeadword = false;

		public LiftEntry()
		{
			m_order = 0;
			m_dateDeleted = DateTime.MinValue;
		}
		public LiftEntry(Extensible info, Guid guid, int order, FdoCache cache)
		{
			this.Id = info.Id;
			this.Guid = guid;
			if (guid == Guid.Empty)
				this.Hvo = 0;
			else
				this.Hvo = cache.GetIdFromGuid(guid);	// zero if a LexEntry with the given guid doesn't exist.
			this.DateCreated = info.CreationTime;
			this.DateModified = info.ModificationTime;
			m_dateDeleted = DateTime.MinValue;
			this.Order = order;
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
		public LiftIO.Parsing.LiftMultiText LexicalForm
		{
			get { return m_mtLexicalForm; }
			set { m_mtLexicalForm = value; }
		}
		public LiftIO.Parsing.LiftMultiText CitationForm
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
		Dictionary<string, string> m_dictName = new Dictionary<string, string>();	// term
		Dictionary<string, string> m_dictAbbrev = new Dictionary<string, string>();	// abbrev
		Dictionary<string, string> m_dictDesc = new Dictionary<string, string>();	// def
		public EticCategory()
		{
		}
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
	public class FlexLiftMerger : LiftIO.Parsing.ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>
	{
		private readonly FdoCache m_cache = null;
		private ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		private ITsPropsFactory m_tpf = TsPropsFactoryClass.Create();
		private GuidConverter m_gconv = (GuidConverter)TypeDescriptor.GetConverter(typeof(Guid));
		private MoMorphTypeCollection m_rgmmt;
		public const string LiftDateTimeFormat = "yyyy-MM-ddTHH:mm:ssK";	// wants UTC, but works with Local

		private Regex m_regexGuid = new System.Text.RegularExpressions.Regex(
			"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		RfcWritingSystem m_rfcWs;

		// save field specification information from the header.
		private Dictionary<string, LiftMultiText> m_dictFieldDef = new Dictionary<string, LiftMultiText>();

		// maps for quick lookup of list items
		private Dictionary<string, int> m_dictPOS = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictMMT = new Dictionary<string, int>(19);
		private Dictionary<string, int> m_dictComplexFormType = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictVariantType = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictSemDom = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictTransType = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictAnthroCode = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictDomType = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictSenseType = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictStatus = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictUsageType = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictLocation = new Dictionary<string, int>();
		private Dictionary<string, List<int>> m_dictEnvirons = new Dictionary<string, List<int>>();
		private Dictionary<string, int> m_dictLexRefTypes = new Dictionary<string, int>();
		private Dictionary<string, int> m_dictRevLexRefTypes = new Dictionary<string, int>();

		// lists of new items added to lists
		private List<ICmPossibility> m_rgnewPOS = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewMMT = new List<ICmPossibility>();
		private List<ILexEntryType> m_rgnewComplexFormType = new List<ILexEntryType>();
		private List<ILexEntryType> m_rgnewVariantType = new List<ILexEntryType>();
		private List<ICmPossibility> m_rgnewSemDom = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewTransType = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewCondition = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewAnthroCode = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewDomType = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewSenseType = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewStatus = new List<ICmPossibility>();
		private List<ICmPossibility> m_rgnewUsageType = new List<ICmPossibility>();
		private List<ICmLocation> m_rgnewLocation = new List<ICmLocation>();
		private List<IPhEnvironment> m_rgnewEnvirons = new List<IPhEnvironment>();
		private List<ICmPossibility> m_rgnewLexRefTypes = new List<ICmPossibility>();
		private List<IMoInflClass> m_rgnewInflClasses = new List<IMoInflClass>();
		private List<IMoInflAffixSlot> m_rgnewSlots = new List<IMoInflAffixSlot>();

		// map from id strings to database object id numbers (for entries and senses).
		private Dictionary<string, int> m_mapIdHvo = new Dictionary<string, int>();

		// map from custom field tags to flids (for custom fields)
		private Dictionary<string, int> m_dictCustomFlid = new Dictionary<string, int>();

		// map from slot range name to slot map.
		private Dictionary<string, Dictionary<string, int>> m_dictDictSlots = new Dictionary<string, Dictionary<string, int>>();

		// map from (reversal's) writing system to reversal PartOfSpeech map.
		private Dictionary<int, Dictionary<string, int>> m_dictWsReversalPOS = new Dictionary<int, Dictionary<string, int>>();

		struct MuElement
		{
			string m_text;
			int m_ws;
			public MuElement(int ws, string text)
			{
				m_text = text;
				m_ws = ws;
			}

			public override bool Equals(object obj)
			{
				if (obj is MuElement)
				{
					MuElement that = (MuElement)obj;
					return this.m_text == that.m_text && this.m_ws == that.m_ws;
				}
				else
				{
					return false;
				}
			}

			public override int GetHashCode()
			{
				return m_text.GetHashCode() + m_ws.GetHashCode();
			}
		}
		Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>> m_mapToMapToRIE =
			new Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>>();

		/// <summary>Set of guids for elements/senses that were found in the LIFT file.</summary>
		Set<Guid> m_setUnchangedEntry = new Set<Guid>();
		Set<Guid> m_setChangedEntry = new Set<Guid>();
		Set<int> m_deletedObjects = new Set<int>();

		public enum MergeStyle
		{
			/// <summary>When there's a conflict, keep the existing data.</summary>
			msKeepOld = 1,
			/// <summary>When there's a conflict, keep the data in the LIFT file.</summary>
			msKeepNew = 2,
			/// <summary>When there's a conflict, keep both the existing data and the data in the LIFT file.</summary>
			msKeepBoth = 3,
			/// <summary>Throw away any existing entries/senses/... that are not in the LIFT file.</summary>
			msKeepOnlyNew = 4
		}
		private MergeStyle m_msImport = MergeStyle.msKeepOld;

		private bool m_fTrustModTimes = false;
		/// <summary>
		/// This stores information for a relation that will be set later because the
		/// target object may not have been imported yet.
		/// </summary>
		internal class PendingRelation
		{
			ICmObject m_obj;
			int m_hvoTarget;
			LiftRelation m_rel;
			string m_sResidue;

			public PendingRelation(ICmObject obj, LiftRelation rel, string sResidue)
			{
				m_obj = obj;
				m_hvoTarget = 0;
				m_rel = rel;
				m_sResidue = sResidue;
			}

			public ICmObject CmObject
			{
				get { return m_obj; }
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
				get { return m_hvoTarget; }
				set { m_hvoTarget = value; }
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
				else if (rel.RelationType != this.RelationType)
					return false;
				else if (rel.CmObject.Hvo == this.CmObject.Hvo && rel.TargetHvo == this.TargetHvo)
					return true;
				else if (rel.CmObject.Hvo == this.TargetHvo && rel.TargetHvo == this.CmObject.Hvo)
					return true;
				else
					return false;
			}

			internal void MarkAsProcessed()
			{
				m_obj = null;
			}

			internal bool HasBeenProcessed()
			{
				return m_obj == null;
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
					m_rel.Type, m_rel.Order, m_hvoTarget, (m_obj == null ? 0 : m_obj.Hvo));
			}

			internal string AsResidueString()
			{
				if (m_sResidue == null)
					m_sResidue = String.Empty;
				if (IsSequence)
				{
					return String.Format("<relation type=\"{0}\" ref=\"{1}\" order=\"{2}\"/>{3}",
						m_rel.Type, m_rel.Ref, m_rel.Order, Environment.NewLine);
				}
				else
				{
					return String.Format("<relation type=\"{0}\" ref=\"{1}\"/>{2}",
						m_rel.Type, m_rel.Ref, Environment.NewLine);
				}
			}
		}
		private List<PendingRelation> m_rgPendingRelation = new List<PendingRelation>();
		private List<PendingRelation> m_rgPendingTreeTargets = new List<PendingRelation>();
		private LinkedList<PendingRelation> m_rgPendingCollectionRelations = new LinkedList<PendingRelation>();

		/// <summary>
		///
		/// </summary>
		internal class PendingLexEntryRef
		{
			ICmObject m_obj;
			int m_hvoTarget;
			LiftRelation m_rel;
			List<string> m_rgsComplexFormTypes = new List<string>();
			List<string> m_rgsVariantTypes = new List<string>();
			bool m_fIsPrimary = false;
			int m_nHideMinorEntry = 0;

			string m_sResidue;
			// preserve trait values from older LIFT files based on old FieldWorks model
			string m_sEntryType;
			string m_sMinorEntryCondition;
			bool m_fExcludeAsHeadword;
			LiftField m_summary;

			public PendingLexEntryRef(ICmObject obj, LiftRelation rel, LiftEntry entry)
			{
				m_obj = obj;
				m_hvoTarget = 0;
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
				get { return m_obj; }
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
				get { return m_hvoTarget; }
				set { m_hvoTarget = value; }
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
		private List<PendingLexEntryRef> m_rgPendingLexEntryRefs = new List<PendingLexEntryRef>();

		internal class PendingModifyTime
		{
			private ILexEntry m_le;
			private DateTime m_dt;

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
		private List<PendingModifyTime> m_rgPendingModifyTimes = new List<PendingModifyTime>();

		private int m_cEntriesAdded = 0;
		private int m_cSensesAdded = 0;
		private int m_cEntriesDeleted = 0;
		private DateTime m_dtStart;		// when import started
		/// <summary>
		/// This stores the information for one object's LIFT import residue.
		/// </summary>
		class LiftResidue
		{
			private int m_flid;
			public XmlDocument m_xdoc;

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
		private Dictionary<int, LiftResidue> m_dictResidue = new Dictionary<int, LiftResidue>();

		internal abstract class ConflictingData
		{
			protected string m_sType;
			protected string m_sField;
			protected ConflictingData(string sType, string sField)
			{
				m_sType = sType;
				m_sField = sField;
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
			FdoUi.FwLink link = FdoUi.FwLink.Create("lexiconEdit", le.Guid,
				le.Cache.ServerName, le.Cache.DatabaseName);
			return link.ToString();
		}
		internal class ConflictingEntry : ConflictingData
		{
			private ILexEntry m_leOrig;
			private ILexEntry m_leNew;
			public ConflictingEntry(string sField, ILexEntry leOrig)
				: base(LexTextControls.ksEntry, sField)
			{
				m_leOrig = leOrig;
			}
			public override string OrigHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leOrig), HtmlString(m_leOrig.Headword)
				return String.Format("<a href=\"{0}\">{1}</a>", LinkRef(m_leOrig),
					FlexLiftMerger.TsStringAsHtml(m_leOrig.HeadWord, m_leOrig.Cache));
			}

			public override string DupHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leNew), HtmlString(m_leNew.Headword)
				return String.Format("<a href=\"{0}\">{1}</a>", LinkRef(m_leNew),
					FlexLiftMerger.TsStringAsHtml(m_leNew.HeadWord, m_leNew.Cache));
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
			public ConflictingSense(string sField, ILexSense lsOrig)
				: base(LexTextControls.ksSense, sField)
			{
				m_lsOrig = lsOrig;
			}
			public override string OrigHtmlReference()
			{
				return String.Format("<a href=\"{0}\">{1}</a>",
					LinkRef(m_lsOrig.Entry),
					FlexLiftMerger.TsStringAsHtml((m_lsOrig as LexSense).OwnerOutlineName, m_lsOrig.Cache));
			}
			public override string DupHtmlReference()
			{
				return String.Format("<a href=\"{0}\">{1}</a>",
					LinkRef(m_lsNew.Entry),
					FlexLiftMerger.TsStringAsHtml((m_lsNew as LexSense).OwnerOutlineName, m_lsNew.Cache));
			}
			public ILexSense DupSense
			{
				set { m_lsNew = value; }
			}
		}
		private ConflictingData m_cdConflict = null;
		private List<ConflictingData> m_rgcdConflicts = new List<ConflictingData>();

		private List<EticCategory> m_rgcats = new List<EticCategory>();

		private string m_sLiftFile;
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

			internal PendingErrorReport(Guid guid, int flid, int ws, FdoCache cache)
			{
				m_guid = guid;
				m_flid = flid;
				m_ws = ws;
				m_cache = cache;
			}

			internal virtual string FieldName
			{
				get
				{
					// TODO: make this more informative and user-friendly.
					return m_cache.MetaDataCacheAccessor.GetFieldName((uint)m_flid);
				}
			}

			private int EntryHvo()
			{
				int hvo = m_cache.GetIdFromGuid(m_guid);
				int clid = m_cache.GetClassOfObject(hvo);
				if (clid == LexEntry.kclsidLexEntry)
				{
					return hvo;
				}
				else
				{
					return m_cache.GetOwnerOfObjectOfClass(hvo, LexEntry.kclsidLexEntry);
				}
			}

			internal string EntryHtmlReference()
			{
				ILexEntry le = LexEntry.CreateFromDBObject(m_cache, EntryHvo());
				return String.Format("<a href=\"{0}\">{1}</a>",
					LinkRef(le),
					FlexLiftMerger.TsStringAsHtml(le.HeadWord, m_cache));

			}

			internal string WritingSystem
			{
				get
				{
					if (m_ws > 0)
					{
						ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, m_ws);
						return lgws.Name.UserDefaultWritingSystem;
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
				if (that != null && this.m_flid == that.m_flid && this.m_guid == that.m_guid &&
					this.m_ws == that.m_ws)
				{
					if (this.m_cache != null && that.m_cache != null)
					{
						return this.m_cache.DatabaseName == that.m_cache.DatabaseName &&
							this.m_cache.ServerName == that.m_cache.ServerName;
					}
					else
					{
						return this.m_cache == null && that.m_cache == null;
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

			public InvalidData(string sMsg, Guid guid, int flid, string val, int ws, FdoCache cache)
				: base(guid, flid, ws, cache)
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
				return that != null && this.m_sMsg == that.m_sMsg && this.m_sValue == that.m_sValue &&
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

			public InvalidRelation(PendingLexEntryRef pend, FdoCache cache)
				: base(pend.CmObject.Guid, 0, 0, cache)
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
					{
						return LexTextControls.ksEntryInvalidRef;
					}
					else
					{
						Debug.Assert(m_pendRef is ILexSense);
						return String.Format(LexTextControls.ksSenseInvalidRef,
							(m_pendRef.CmObject as LexSense).OwnerOutlineName.Text);
					}
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

			public TruncatedData(string sText, int cchMax, Guid guid, int flid, int ws, FdoCache cache)
				: base(guid, flid, ws, cache)
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
			m_cache = cache;
			m_msImport = msImport;
			m_fTrustModTimes = fTrustModTimes;
			m_rgmmt = new MoMorphTypeCollection(m_cache);

			// remember initial conditions.
			m_dtStart = DateTime.Now;

			m_cache.EnableBulkLoadingIfPossible(true);
			InitializePossibilityMap(m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS,
				m_dictPOS);
			InitializeMorphTypes();
			InitializePossibilityMap(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS,
				m_dictComplexFormType);
			InitializePossibilityMap(m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS,
				m_dictVariantType);
			InitializePossibilityMap(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS,
				m_dictSemDom);
			EnhancePossibilityMapForWeSay(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS,
				m_dictSemDom);
			InitializePossibilityMap(m_cache.LangProject.TranslationTagsOA.PossibilitiesOS,
				m_dictTransType);
			InitializePossibilityMap(m_cache.LangProject.AnthroListOA.PossibilitiesOS,
				m_dictAnthroCode);
			InitializePossibilityMap(m_cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS,
				m_dictDomType);
			InitializePossibilityMap(m_cache.LangProject.LexDbOA.SenseTypesOA.PossibilitiesOS,
				m_dictSenseType);
			InitializePossibilityMap(m_cache.LangProject.LexDbOA.StatusOA.PossibilitiesOS,
				m_dictStatus);
			InitializePossibilityMap(m_cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS,
				m_dictUsageType);
			InitializePossibilityMap(m_cache.LangProject.LocationsOA.PossibilitiesOS,
				m_dictLocation);
			foreach (PhEnvironment env in m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS)
			{
				// More than one environment may have the same string representation.  This
				// is unfortunate, but it does happen.
				string s = env.StringRepresentation.Text;
				if (!String.IsNullOrEmpty(s))
				{
					List<int> rghvo;
					if (m_dictEnvirons.TryGetValue(s, out rghvo))
					{
						rghvo.Add(env.Hvo);
					}
					else
					{
						rghvo = new List<int>();
						rghvo.Add(env.Hvo);
						m_dictEnvirons.Add(s, rghvo);
					}
				}
			}
			InitializePossibilityMap(m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS,
				m_dictLexRefTypes);
			InitializeReverseLexRefTypesMap();
			InitializeSlotMaps();
			InitializeReversalMaps();
			InitializeReversalPOSMaps();
			m_cache.EnableBulkLoadingIfPossible(false);
			LoadCategoryCatalog();
			// Store a case-insensitive map for looking up existing writing systems.
			m_rfcWs = new RfcWritingSystem(cache);
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

		private void InitializePossibilityMap(FdoOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, int> dict)
		{
			string s;
			foreach (ICmPossibility poss in possibilities)
			{
				Set<string> setKeys = new Set<string>();
				foreach (int ws in m_cache.LanguageEncodings.HvoArray)
				{
					s = poss.Abbreviation.GetAlternative(ws);
					if (!String.IsNullOrEmpty(s))
					{
						setKeys.Add(s);
						setKeys.Add(s.ToLowerInvariant());
					}
					s = poss.Name.GetAlternative(ws);
					if (!String.IsNullOrEmpty(s))
					{
						setKeys.Add(s);
						setKeys.Add(s.ToLowerInvariant());
					}
				}
				foreach (string key in setKeys)
				{
					// If it's ambiguous, assume the first one encountered is correct.
					if (!dict.ContainsKey(key))
						dict.Add(key, poss.Hvo);
				}
				InitializePossibilityMap(poss.SubPossibilitiesOS, dict);
			}
		}

		/// <summary>
		/// WeSay stores Semantic Domain values as "abbr name", so fill in keys like that
		/// for lookup during import.
		/// </summary>
		/// <param name="possibilities"></param>
		/// <param name="dict"></param>
		private void EnhancePossibilityMapForWeSay(FdoOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, int> dict)
		{
			string sAbbr;
			string sName;
			foreach (ICmPossibility poss in possibilities)
			{
				Set<string> setKeys = new Set<string>();
				foreach (int ws in m_cache.LanguageEncodings.HvoArray)
				{
					sAbbr = poss.Abbreviation.GetAlternative(ws);
					if (!String.IsNullOrEmpty(sAbbr))
					{
						sName = poss.Name.GetAlternative(ws);
						if (!String.IsNullOrEmpty(sName))
						{
							setKeys.Add(String.Format("{0} {1}", sAbbr, sName));
							setKeys.Add(String.Format("{0} {1}",
								sAbbr.ToLowerInvariant(), sName.ToLowerInvariant()));
						}
					}
				}
				foreach (string key in setKeys)
				{
					// If it's ambiguous, assume the first one encountered is correct.
					if (!dict.ContainsKey(key))
						dict.Add(key, poss.Hvo);
				}
				EnhancePossibilityMapForWeSay(poss.SubPossibilitiesOS, dict);
			}
		}

		private void InitializeMorphTypes()
		{
			Guid guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphBoundRoot);
			m_dictMMT.Add("bound root", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphBoundStem);
			m_dictMMT.Add("bound stem", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphClitic);
			m_dictMMT.Add("clitic", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphDiscontiguousPhrase);
			m_dictMMT.Add("discontiguous phrase", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphEnclitic);
			m_dictMMT.Add("enclitic", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphParticle);
			m_dictMMT.Add("particle", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphPhrase);
			m_dictMMT.Add("phrase", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphProclitic);
			m_dictMMT.Add("proclitic", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphRoot);
			m_dictMMT.Add("root", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphStem);
			m_dictMMT.Add("stem", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphCircumfix);
			m_dictMMT.Add("circumfix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphInfix);
			m_dictMMT.Add("infix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphInfixingInterfix);
			m_dictMMT.Add("infixing interfix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphPrefix);
			m_dictMMT.Add("prefix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphPrefixingInterfix);
			m_dictMMT.Add("prefixing interfix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphSimulfix);
			m_dictMMT.Add("simulfix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphSuffix);
			m_dictMMT.Add("suffix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphSuffixingInterfix);
			m_dictMMT.Add("suffixing interfix", m_cache.GetIdFromGuid(guid));
			guid = (Guid)m_gconv.ConvertFrom(MoMorphType.kguidMorphSuprafix);
			m_dictMMT.Add("suprafix", m_cache.GetIdFromGuid(guid));
		}

		private void InitializeSlotMaps()
		{
			// TODO: implement this.
		}

		private void InitializeReversalMaps()
		{
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs =
					new Dictionary<MuElement, List<IReversalIndexEntry>>();
				m_mapToMapToRIE.Add(ri, mapToRIEs);
				InitializeReversalMap(ri.EntriesOC, mapToRIEs);
			}
		}

		private void InitializeReversalMap(FdoOwningCollection<IReversalIndexEntry> entries,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs)
		{
			foreach (IReversalIndexEntry rie in entries)
			{
				foreach (int ws in m_cache.LanguageEncodings.HvoArray)
				{
					string sForm = rie.ReversalForm.GetAlternative(ws);
					if (!String.IsNullOrEmpty(sForm))
					{
						MuElement mue = new MuElement(ws, sForm);
						AddToReversalMap(mue, rie, mapToRIEs);
					}
				}
				if (rie.SubentriesOC.Count > 0)
				{
					Dictionary<MuElement, List<IReversalIndexEntry>> submapToRIEs =
						new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRIE.Add(rie, submapToRIEs);
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
				Dictionary<string, int> dict = new Dictionary<string, int>();
				InitializePossibilityMap(ri.PartsOfSpeechOA.PossibilitiesOS, dict);
				if (m_dictWsReversalPOS.ContainsKey(ri.WritingSystemRAHvo))
				{
					// REVIEW: SHOULD WE LOG A WARNING HERE?  THIS SHOULD NEVER HAPPEN!
					// (BUT IT HAS AT LEAST ONCE IN A 5.4.1 PROJECT)
				}
				else
				{
					m_dictWsReversalPOS.Add(ri.WritingSystemRAHvo, dict);
				}
			}
		}

		private void InitializeReverseLexRefTypesMap()
		{
			string s;
			foreach (ILexRefType lrt in m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS)
			{
				Set<string> setKeys = new Set<string>();
				foreach (int ws in m_cache.LanguageEncodings.HvoArray)
				{
					s = lrt.ReverseAbbreviation.GetAlternative(ws);
					if (!String.IsNullOrEmpty(s))
						setKeys.Add(s);
					s = lrt.ReverseName.GetAlternative(ws);
					if (!String.IsNullOrEmpty(s))
						setKeys.Add(s);
				}
				foreach (string key in setKeys)
				{
					// If it's ambiguous, assume the first one encountered is correct.
					if (!m_dictRevLexRefTypes.ContainsKey(key))
						m_dictRevLexRefTypes.Add(key, lrt.Hvo);
				}
			}
		}

		private void LoadCategoryCatalog()
		{
			string sPath = System.IO.Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory,
				"Templates\\GOLDEtic.xml");
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
			EticCategory cat = new EticCategory();
			cat.Id = id;
			cat.ParentId = parent;
			foreach (XmlNode xn in node.SelectNodes("abbrev"))
			{
				string sWs = XmlUtils.GetAttributeValue(xn, "ws");
				string sAbbrev = xn.InnerText;
				if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sAbbrev))
					cat.SetAbbrev(sWs, sAbbrev);
			}
			foreach (XmlNode xn in node.SelectNodes("term"))
			{
				string sWs = XmlUtils.GetAttributeValue(xn, "ws");
				string sName = xn.InnerText;
				if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sName))
					cat.SetName(sWs, sName);
			}
			foreach (XmlNode xn in node.SelectNodes("def"))
			{
				string sWs = XmlUtils.GetAttributeValue(xn, "ws");
				string sDesc = xn.InnerText;
				if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sDesc))
					cat.SetDesc(sWs, sDesc);
			}
			m_rgcats.Add(cat);
			foreach (XmlNode xn in node.SelectNodes("item"))
			{
				string sType = XmlUtils.GetAttributeValue(xn, "type");
				string sChildId = XmlUtils.GetAttributeValue(xn, "id");
				if (sType == "category" && !String.IsNullOrEmpty(sChildId))
					LoadCategoryNode(xn, sChildId, id);
			}
		}
		#endregion // Constructors and other initialization methods

		#region String matching, merging, extracting, etc.
		/// <summary>
		/// Merge in a form that may need to have morphtype markers stripped from it.
		/// </summary>
		private void MergeInAllomorphForms(LiftMultiText forms, MultiUnicodeAccessor mua,
			int clsidForm, Guid guidEntry, int flid)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, string> multi;
			if (m_msImport == MergeStyle.msKeepOnlyNew)
				multi = mua.GetAllAlternatives();
			else
				multi = new Dictionary<int, string>();
			foreach (string key in forms.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				string form = forms[key].Text;
				if (wsHvo > 0 && !String.IsNullOrEmpty(form))
				{
					multi.Remove(wsHvo);
					if (!m_fCreatingNewEntry && m_msImport == MergeStyle.msKeepOld)
					{
						if (String.IsNullOrEmpty(mua.GetAlternative(wsHvo)))
							mua.SetAlternative(StripAlloForm(form, clsidForm, guidEntry, flid), wsHvo);
					}
					else
					{
						mua.SetAlternative(StripAlloForm(form, clsidForm, guidEntry, flid), wsHvo);
					}
				}
			}
			foreach (int ws in multi.Keys)
				mua.SetAlternative(null, ws);
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
		private void MergeIn(MultiStringAccessor msa, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, ITsString> multi;
			if (m_msImport == MergeStyle.msKeepOnlyNew)
				multi = msa.GetAllAlternatives();
			else
				multi = new Dictionary<int, ITsString>();
			if (forms != null && forms.Keys != null)
			{
				int cchMax = m_cache.MaxFieldLength(msa.Flid);
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						if (!m_fCreatingNewEntry &&
							!m_fCreatingNewSense &&
							m_msImport == MergeStyle.msKeepOld)
						{
							TsStringAccessor tsa = msa.GetAlternative(wsHvo);
							if (tsa == null || tsa.Length == 0)
							{
								ITsString tss = CreateTsStringFromLiftString(forms[key], wsHvo,
									msa.Flid, guidObj, cchMax);
								msa.SetAlternative(tss, wsHvo);
							}
						}
						else
						{
							ITsString tss = CreateTsStringFromLiftString(forms[key], wsHvo,
								msa.Flid, guidObj, cchMax);
							msa.SetAlternative(tss, wsHvo);
						}
					}
				}
			}
			foreach (int ws in multi.Keys)
				msa.SetAlternative(null, ws);
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
			m_rgTruncated.Add(new TruncatedData(sText, cchMax, guid, flid, ws, m_cache));
		}

		private ITsString CreateTsStringFromLiftString(LiftString liftstr, int wsHvo)
		{
			ITsStrBldr tsb = m_tsf.GetBldr();
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
					string sPath = span.LinkURL;
					if (sPath.StartsWith("file://"))
						sPath = sPath.Substring(7).Replace('/', '\\');	// Assumes Microsoft OS!
					char chOdt = Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName);
					string sRef = chOdt.ToString() + sPath;
					tpb.SetStrPropValue((int)FwTextPropType.ktptObjData, sRef);
				}
				tsb.SetProperties(span.Index, span.Index + span.Length, tpb.GetTextProps());
			}
			return tsb.GetString();
		}

		/// <summary>
		/// Merge in a MultiUnicode type value.
		/// </summary>
		private void MergeIn(MultiUnicodeAccessor mua, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, string> multi;
			if (m_msImport == MergeStyle.msKeepOnlyNew)
				multi = mua.GetAllAlternatives();
			else
				multi = new Dictionary<int, string>();
			if (forms != null && forms.Keys != null)
			{
				int cchMax = m_cache.MaxFieldLength(mua.Flid);
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						string sText = forms[key].Text;
						if (sText.Length > cchMax)
						{
							StoreTruncatedDataInfo(sText, cchMax, guidObj, mua.Flid, wsHvo);
							sText = sText.Substring(0, cchMax);
						}
						if (!m_fCreatingNewEntry && !m_fCreatingNewSense && m_msImport == MergeStyle.msKeepOld)
						{
							if (String.IsNullOrEmpty(mua.GetAlternative(wsHvo)))
								mua.SetAlternative(sText, wsHvo);
						}
						else
						{
							mua.SetAlternative(sText, wsHvo);
						}
					}
				}
			}
			foreach (int ws in multi.Keys)
				mua.SetAlternative(null, ws);
		}

		public int GetWsFromLiftLang(string key)
		{
			return m_rfcWs.GetWsFromRfcLang(key, m_sLiftDir);
		}

		private void MergeLiftMultiTexts(LiftMultiText mtCurrent, LiftMultiText mtNew)
		{
			foreach (string key in mtNew.Keys)
			{
				if (mtCurrent.ContainsKey(key))
				{
					if (m_fCreatingNewEntry || m_fCreatingNewSense || m_msImport != MergeStyle.msKeepOld)
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

		private bool StringsConflict(TsStringAccessor tsaOld, ITsString tssNew)
		{
			if (tsaOld == null || tsaOld.Length == 0)
				return false;
			else
				return StringsConflict(tsaOld.UnderlyingTsString, tssNew);
		}

		private bool StringsConflict(TsStringAccessor tsaOld, string sNew)
		{
			if (tsaOld == null || tsaOld.Length == 0)
				return false;
			else
				return StringsConflict(tsaOld.Text, sNew);
		}

		private bool MultiStringsConflict(MultiUnicodeAccessor mua, LiftMultiText lmt, bool fStripMarkers,
			Guid guidEntry, int flid)
		{
			if (mua == null || lmt == null || lmt.IsEmpty)
				return false;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				string sNew = lmt[key].Text;
				if (fStripMarkers)
					sNew = StripAlloForm(sNew, 0, guidEntry, flid);
				string sOld = mua.GetAlternative(wsHvo);
				if (String.IsNullOrEmpty(sOld))
					continue;
				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
				if (sNewNorm != sOldNorm)
					return true;
			}
			return false;
		}

		private bool MultiStringsConflict(MultiStringAccessor msa, LiftMultiText lmt)
		{
			if (msa == null || lmt == null || lmt.IsEmpty)
				return false;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				ITsString tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				TsStringAccessor tsa = msa.GetAlternative(wsHvo);
				if (tsa == null || tsa.Length == 0)
					continue;
				ITsString tssOld = tsa.UnderlyingTsString;
				ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (!tssOldNorm.Equals(tssNewNorm))
					return true;
			}
			return false;
		}

		private int MultiStringMatches(MultiUnicodeAccessor mua, LiftMultiText lmt, bool fStripMarkers,
			Guid guidEntry, int flid)
		{
			if (mua == null && (lmt == null || lmt.IsEmpty))
				return 1;
			if (mua == null || lmt == null || lmt.IsEmpty)
				return 0;
			int cMatches = 0;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				string sOld = mua.GetAlternative(wsHvo);
				if (String.IsNullOrEmpty(sOld))
					continue;
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

		private int MultiStringMatches(MultiStringAccessor msa, LiftMultiText lmt)
		{
			if (msa == null && (lmt == null || lmt.IsEmpty))
				return 1;
			if (msa == null || lmt == null || lmt.IsEmpty)
				return 0;
			int cMatches = 0;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;
				TsStringAccessor tsa = msa.GetAlternative(wsHvo);
				if (tsa == null || tsa.Length == 0)
					continue;
				ITsString tssOld = tsa.UnderlyingTsString;
				ITsString tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (tssOldNorm.Equals(tssNewNorm))
					++cMatches;
			}
			return cMatches;
		}

		private bool SameMultilingualContent(LiftMultiText contents, MultiUnicodeAccessor mua)
		{
			foreach (string key in contents.Keys)
			{
				int ws = GetWsFromLiftLang(key);
				string sNew = contents[key].Text;
				string sOld = mua.GetAlternative(ws);
				if (String.IsNullOrEmpty(sNew) && String.IsNullOrEmpty(sOld))
					continue;
				if (String.IsNullOrEmpty(sNew) || String.IsNullOrEmpty(sOld))
					return false;
				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
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
		/// <param name="muaAbbr">accessor for abbreviation (or null)</param>
		/// <param name="muaName">accessor for name</param>
		/// <returns></returns>
		private bool HasMatchingAlternative(string sVal, MultiUnicodeAccessor muaAbbr,
			MultiUnicodeAccessor muaName)
		{
			foreach (int ws in m_cache.LanguageEncodings.HvoArray)
			{
				if (muaAbbr != null)
				{
					string sAbbr = muaAbbr.GetAlternative(ws);
					if (sAbbr != null)
					{
						// TODO: try sAbbr.Equals(sVal, StringComparison.InvariantCultureIgnoreCase)
						if (sAbbr.ToLowerInvariant() == sVal)
							return true;
					}
				}
				if (muaName != null)
				{
					string sName = muaName.GetAlternative(ws);
					if (sName != null)
					{
						if (sName.ToLowerInvariant() == sVal)
							return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Write the string as HTML, interpreting the string properties as best we can.
		/// </summary>
		/// <param name="tss"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		public static string TsStringAsHtml(ITsString tss, FdoCache cache)
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
					ILgWritingSystem lws = LgWritingSystem.CreateFromDBObject(cache, ws);
					if (lws != null)
					{
						sLang = lws.RFC4646bis;
						sDir = lws.RightToLeft ? "RTL" : "LTR";
						sFont = lws.DefaultSerif;
						if (String.IsNullOrEmpty(sFont))
							sFont = lws.DefaultBodyFont;
						if (String.IsNullOrEmpty(sFont))
							sFont = lws.DefaultSansSerif;
						if (String.IsNullOrEmpty(sFont))
							sFont = lws.DefaultMonospace;
					}
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
			int hvo = m_cache.GetIdFromGuid(guid);	// zero if a LexEntry with the given guid doesn't exist.
			if (hvo == 0)
				return;
			int clid = m_cache.GetClassOfObject(hvo);
			if (clid == LexEntry.kclsidLexEntry)	// make sure it's a LexEntry!
			{
				// TODO: Compare mod times? or our mod time against import's delete time?
				m_cache.DeleteObject(hvo);
				++m_cEntriesDeleted;
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
			bool fCreateNew = entry.Hvo == 0;
			if (!fCreateNew && m_msImport == MergeStyle.msKeepBoth)
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
				if (m_msImport == MergeStyle.msKeepOnlyNew)
					m_setUnchangedEntry.Add(guid);
				return null;	// assume nothing has changed.
			}
			LiftEntry entry = new LiftEntry(info, guid, order, m_cache);
			if (m_msImport == MergeStyle.msKeepOnlyNew)
				m_setChangedEntry.Add(entry.Guid);
			return entry;
		}

		LiftExample ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.GetOrMakeExample(
			LiftSense sense, Extensible info)
		{
			LiftExample example = new LiftExample();
			example.Id = info.Id;
			example.Guid = GetGuidInExtensible(info);
			example.Hvo = m_cache.GetIdFromGuid(example.Guid);
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
			LiftURLRef pict = new LiftURLRef();
			pict.URL = href;
			pict.Label = caption;
			sense.Illustrations.Add(pict);
		}

		void ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample>.MergeInMedia(
			LiftObject obj, string href, LiftMultiText caption)
		{
			LiftPhonetic phon = obj as LiftPhonetic;
			if (phon != null)
			{
				LiftURLRef url = new LiftURLRef();
				url.URL = href;
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
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
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
					break;
				case "note-type":	// I think we can ignore these.
					break;
				case "paradigm":	// I think we can ignore these.
					break;
				case "semantic_domain":	// initialize map, adding to existing list if needed.
					ProcessSemanticDomain(id, guidAttr, parent, description, label, abbrev);
					break;
				case "anthro_codes":	// original FLEX export
				case "anthro-code":		// initialize map, adding to existing list if needed.
					ProcessAnthroItem(id, guidAttr, parent, description, label, abbrev);
					break;
				case "status":
					ProcessPossibility(id, guidAttr, parent, description, label, abbrev,
						m_dictStatus, m_rgnewStatus, m_cache.LangProject.LexDbOA.StatusOA);
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
				default:
					if (range.EndsWith("-slot") || range.EndsWith("-Slots"))
						ProcessSlotDefinition(range, id, guidAttr, parent, description, label, abbrev);
					else if (range.EndsWith("-infl-class") || range.EndsWith("-InflClasses"))
						ProcessInflectionClassDefinition(range, id, guidAttr, parent, description, label, abbrev);
					break;
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
			if (flid == (int)LexEntry.LexEntryTags.kflidLiftResidue)
				LexEntry.ExtractLIFTResidue(m_cache, hvo, (int)LexEntry.LexEntryTags.kflidImportResidue, flid);
			else if (flid == (int)LexSense.LexSenseTags.kflidLiftResidue)
				LexEntry.ExtractLIFTResidue(m_cache, hvo, (int)LexSense.LexSenseTags.kflidImportResidue, flid);
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
				return FindOrCreateResidue(extensible, null, (int)LexEntry.LexEntryTags.kflidLiftResidue);
			else if (extensible is ILexSense)
				return FindOrCreateResidue(extensible, null, (int)LexSense.LexSenseTags.kflidLiftResidue);
			else if (extensible is ILexEtymology)
				return FindOrCreateResidue(extensible, null, (int)LexEtymology.LexEtymologyTags.kflidLiftResidue);
			else if (extensible is ILexExampleSentence)
				return FindOrCreateResidue(extensible, null, (int)LexExampleSentence.LexExampleSentenceTags.kflidLiftResidue);
			else if (extensible is ILexPronunciation)
				return FindOrCreateResidue(extensible, null, (int)LexPronunciation.LexPronunciationTags.kflidLiftResidue);
			else if (extensible is ILexReference)
				return FindOrCreateResidue(extensible, null, (int)LexReference.LexReferenceTags.kflidLiftResidue);
			else if (extensible is IMoForm)
				return FindOrCreateResidue(extensible, null, (int)MoForm.MoFormTags.kflidLiftResidue);
			else if (extensible is IMoMorphSynAnalysis)
				return FindOrCreateResidue(extensible, null, (int)MoMorphSynAnalysis.MoMorphSynAnalysisTags.kflidLiftResidue);
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
			foreach (LiftURLRef url in phon.Media)
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

		private string CreateXmlForUrlRef(LiftURLRef url, string tag)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<{0} href=\"{1}\">", tag, url.URL);
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
				foreach (LiftField field in var.Fields)
				{
					string sXml = CreateXmlForField(field);
					InsertResidueContent(xdResidue, sXml);
				}
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
			XmlReader reader = new XmlTextReader(sXml, XmlNodeType.Element, context);
			XmlNode xn = xdResidue.ReadNode(reader);
			if (xn != null)
			{
				xdResidue.FirstChild.AppendChild(xn);
				xn = xdResidue.ReadNode(reader);	// add trailing newline
				if (xn != null)
					xdResidue.FirstChild.AppendChild(xn);
			}
		}

		public bool IsDateSet(DateTime dt)
		{
			return dt != null && dt != default(DateTime) && dt != m_defaultDateTime;
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
				// Note: when/if LexExampleSentence.Reference is written as a <field>
				// instead of a <note>, the next loop will presumably be changed.
				foreach (LiftField field in expl.Fields)
				{
					string sXml = CreateXmlForField(field);
					InsertResidueContent(xdResidue, sXml);
				}
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
			string sClass = m_cache.GetClassName((uint)clid);
			string sTag = String.Format("{0}-{1}", sClass, sLabel);
			int flid = 0;
			if (m_dictCustomFlid.TryGetValue(sTag, out flid))
				return flid;
			string sDesc = String.Empty;
			string sSpec = null;
			if (lmtDesc != null)
			{
				LiftString lstr = null;
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
			int type = (int)CellarModuleDefns.kcptMultiBigString;
			int wsSelector = LangProject.kwsAnalVerns;
			int clidDst = 0;
			string sDstCls = "CmObject";
			if (!String.IsNullOrEmpty(sSpec))
			{
				string[] rgsDef = sSpec.Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
				type = GetCustomFieldType(rgsDef);
				if (type < (int)CellarModuleDefns.kcptMin || type >= (int)CellarModuleDefns.kcptLim)
					type = (int)CellarModuleDefns.kcptMultiBigString;
				wsSelector = GetCustomFieldWsSelector(rgsDef);
				clidDst = GetCustomFieldDstCls(rgsDef, out sDstCls);
			}
			foreach (FieldDescription fd in FieldDescription.FieldDescriptors(m_cache))
			{
				if (fd.Custom != 0 && fd.Userlabel == sLabel && fd.Class == clid)
				{
					bool fOk = CheckForCompatibleTypes(type, fd);
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
				case (int)CellarModuleDefns.kcptBoolean:
				case (int)CellarModuleDefns.kcptInteger:
				case (int)CellarModuleDefns.kcptNumeric:
				case (int)CellarModuleDefns.kcptFloat:
				case (int)CellarModuleDefns.kcptTime:
				case (int)CellarModuleDefns.kcptGuid:
				case (int)CellarModuleDefns.kcptImage:
				case (int)CellarModuleDefns.kcptGenDate:
				case (int)CellarModuleDefns.kcptBinary:
					clidDst = -1;
					break;
				case (int)CellarModuleDefns.kcptString:
				case (int)CellarModuleDefns.kcptUnicode:
				case (int)CellarModuleDefns.kcptBigString:
				case (int)CellarModuleDefns.kcptBigUnicode:
				case (int)CellarModuleDefns.kcptMultiString:
				case (int)CellarModuleDefns.kcptMultiUnicode:
				case (int)CellarModuleDefns.kcptMultiBigString:
				case (int)CellarModuleDefns.kcptMultiBigUnicode:
					if (wsSelector == 0)
						wsSelector = LangProject.kwsAnalVerns;		// we need a WsSelector value!
					clidDst = -1;
					break;
				case (int)CellarModuleDefns.kcptOwningAtom:
				case (int)CellarModuleDefns.kcptReferenceAtom:
				case (int)CellarModuleDefns.kcptOwningCollection:
				case (int)CellarModuleDefns.kcptReferenceCollection:
				case (int)CellarModuleDefns.kcptOwningSequence:
				case (int)CellarModuleDefns.kcptReferenceSequence:
					if (clidDst == 0 && sDstCls != "CmObject")
						sDstCls = "CmObject";
					break;
				default:
					type = (int)CellarModuleDefns.kcptMultiBigString;
					if (wsSelector == 0)
						wsSelector = LangProject.kwsAnalVerns;
					clidDst = -1;
					break;
			}
			FieldDescription fdNew = new FieldDescription(m_cache);
			fdNew.Type = type;
			fdNew.Class = clid;
			fdNew.Userlabel = sLabel;
			fdNew.HelpString = sDesc;
			fdNew.WsSelector = wsSelector;
			fdNew.DstCls = clidDst;
			fdNew.UpdateDatabase();
			m_dictCustomFlid.Add(sTag, fdNew.Id);
			m_cache.MetaDataCacheAccessor.Reload(m_cache.DatabaseAccessor, true);
			FieldDescription.ClearDataAbout(m_cache);
			return fdNew.Id;
		}

		private static bool CheckForCompatibleTypes(int type, FieldDescription fd)
		{
			if (fd.Type == type)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptMultiString && type == (int)CellarModuleDefns.kcptMultiBigString)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptMultiBigString &&	type == (int)CellarModuleDefns.kcptMultiString)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptMultiUnicode && type == (int)CellarModuleDefns.kcptMultiBigUnicode)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptMultiBigUnicode && type == (int)CellarModuleDefns.kcptMultiUnicode)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptString && type == (int)CellarModuleDefns.kcptBigString)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptBigString && type == (int)CellarModuleDefns.kcptString)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptUnicode && type == (int)CellarModuleDefns.kcptBigUnicode)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptBigUnicode && type == (int)CellarModuleDefns.kcptUnicode)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptBinary && type == (int)CellarModuleDefns.kcptImage)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptImage && type == (int)CellarModuleDefns.kcptBinary)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptOwningCollection && type == (int)CellarModuleDefns.kcptOwningSequence)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptOwningSequence && type == (int)CellarModuleDefns.kcptOwningCollection)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptReferenceCollection && type == (int)CellarModuleDefns.kcptReferenceSequence)
				return true;
			if (fd.Type == (int)CellarModuleDefns.kcptReferenceSequence && type == (int)CellarModuleDefns.kcptReferenceCollection)
				return true;
			return false;
		}

		private int GetCustomFieldType(string[] rgsDef)
		{
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("Type="))
				{
					string sValue = sDef.Substring(5);
					return (int)Enum.Parse(typeof(CellarModuleDefns), sValue, true);
				}
			}
			return 0;
		}

		private int GetCustomFieldWsSelector(string[] rgsDef)
		{
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("WsSelector="))
				{
					string sValue = sDef.Substring(11);
					int ws = (int)Enum.Parse(typeof(CellarModuleDefns), sValue, true);
					if (ws == 0)
						ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sValue);
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
				ICmAnthroItem csdParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictAnthroCode.ContainsKey(parent))
				{
					int hvoParent = m_dictAnthroCode[parent];
					csdParent = CmAnthroItem.CreateFromDBObject(m_cache, hvoParent);
				}
				ICmAnthroItem cai = new CmAnthroItem();
				if (csdParent != null)
					csdParent.SubPossibilitiesOS.Append(cai);
				else
					m_cache.LangProject.AnthroListOA.PossibilitiesOS.Append(cai);
				if (!String.IsNullOrEmpty(guidAttr))
					cai.Guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				MergeIn(cai.Name, label, cai.Guid);
				MergeIn(cai.Abbreviation, abbrev, cai.Guid);
				MergeIn(cai.Description, description, cai.Guid);
				m_dictAnthroCode[id] = cai.Hvo;
				m_rgnewAnthroCode.Add(cai);
			}
		}

		private static int FindAbbevOrLabelInDict(LiftMultiText abbrev, LiftMultiText label,
			Dictionary<string, int> dict)
		{
			if (abbrev != null && abbrev.Keys != null)
			{
				foreach (string key in abbrev.Keys)
				{
					if (dict.ContainsKey(abbrev[key].Text))
						return dict[abbrev[key].Text];
				}
			}
			if (label != null && label.Keys != null)
			{
				foreach (string key in label.Keys)
				{
					if (dict.ContainsKey(label[key].Text))
						return dict[label[key].Text];
				}
			}
			return 0;
		}

		private void ProcessSemanticDomain(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			int hvo = GetHvoForGuidIfExisting(id, guidAttr, m_dictSemDom);
			if (hvo <= 0)
			{
				ICmSemanticDomain csdParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictSemDom.ContainsKey(parent))
				{
					int hvoParent = m_dictSemDom[parent];
					csdParent = CmSemanticDomain.CreateFromDBObject(m_cache, hvoParent);
				}
				ICmSemanticDomain csd = new CmSemanticDomain();
				if (csdParent != null)
					csdParent.SubPossibilitiesOS.Append(csd);
				else
					m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Append(csd);
				if (!String.IsNullOrEmpty(guidAttr))
					csd.Guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				MergeIn(csd.Name, label, csd.Guid);
				MergeIn(csd.Abbreviation, abbrev, csd.Guid);
				MergeIn(csd.Description, description, csd.Guid);
				m_dictSemDom[id] = csd.Hvo;
				m_rgnewSemDom.Add(csd);
			}
		}

		private void ProcessPossibility(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, int> dict, List<ICmPossibility> rgNew, ICmPossibilityList list)
		{
			int hvo = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (hvo <= 0)
			{
				ICmPossibility possParent = null;
				if (!String.IsNullOrEmpty(parent) && dict.ContainsKey(parent))
				{
					int hvoParent = dict[parent];
					possParent = CmPossibility.CreateFromDBObject(m_cache, hvoParent);
				}
				ICmPossibility poss = new CmPossibility();
				if (possParent != null)
					possParent.SubPossibilitiesOS.Append(poss);
				else
					list.PossibilitiesOS.Append(poss);
				if (!String.IsNullOrEmpty(guidAttr))
					poss.Guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				MergeIn(poss.Name, label, poss.Guid);
				MergeIn(poss.Abbreviation, abbrev, poss.Guid);
				MergeIn(poss.Description, description, poss.Guid);
				dict[id] = poss.Hvo;
			}
		}

		private void ProcessPartOfSpeech(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			int hvo = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictPOS,
				m_cache.LangProject.PartsOfSpeechOA);
			if (hvo <= 0)
			{
				IPartOfSpeech posParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictPOS.ContainsKey(parent))
				{
					int hvoParent = m_dictPOS[parent];
					posParent = PartOfSpeech.CreateFromDBObject(m_cache, hvoParent);
				}
				IPartOfSpeech pos = new PartOfSpeech();
				if (posParent != null)
					posParent.SubPossibilitiesOS.Append(pos);
				else
					m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(pos);
				if (!String.IsNullOrEmpty(guidAttr))
					pos.Guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				MergeIn(pos.Name, label, pos.Guid);
				MergeIn(pos.Abbreviation, abbrev, pos.Guid);
				MergeIn(pos.Description, description, pos.Guid);
				m_dictPOS[id] = pos.Hvo;
				// Try to find this in the category catalog list, so we can add in more information.
				EticCategory cat = FindMatchingEticCategory(label);
				if (cat != null)
					AddEticCategoryInfo(cat, pos);
				m_rgnewPOS.Add(pos);
			}
		}

		private void AddEticCategoryInfo(EticCategory cat, IPartOfSpeech pos)
		{
			if (cat != null)
			{
				pos.CatalogSourceId = cat.Id;
				foreach (string lang in cat.MultilingualName.Keys)
				{
					int ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(lang);
					if (ws > 0)
					{
						string sName = pos.Name.GetAlternative(ws);
						if (String.IsNullOrEmpty(sName))
							pos.Name.SetAlternative(cat.MultilingualName[lang], ws);
					}
				}
				foreach (string lang in cat.MultilingualAbbrev.Keys)
				{
					int ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(lang);
					if (ws > 0)
					{
						string sAbbrev = pos.Abbreviation.GetAlternative(ws);
						if (String.IsNullOrEmpty(sAbbrev))
							pos.Abbreviation.SetAlternative(cat.MultilingualAbbrev[lang], ws);
					}
				}
				foreach (string lang in cat.MultilingualDesc.Keys)
				{
					int ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(lang);
					if (ws > 0)
					{
						TsStringAccessor tsa = pos.Description.GetAlternative(ws);
						if (tsa == null || tsa.Length == 0)
							pos.Description.SetAlternative(cat.MultilingualDesc[lang], ws);
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
					string sWs = m_rfcWs.ConvertFromRFCtoICU(lang);
					string sCatName;
					if (cat.MultilingualName.TryGetValue(sWs, out sCatName))
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
			int hvo = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictMMT,
				m_cache.LangProject.LexDbOA.MorphTypesOA);
			if (hvo <= 0)
			{
				IMoMorphType mmtParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictPOS.ContainsKey(parent))
				{
					int hvoParent = m_dictMMT[parent];
					mmtParent = MoMorphType.CreateFromDBObject(m_cache, hvoParent);
				}
				IMoMorphType mmt = new MoMorphType();
				if (mmtParent != null)
					mmtParent.SubPossibilitiesOS.Append(mmt);
				else
					m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.Append(mmt);
				if (!String.IsNullOrEmpty(guidAttr))
					mmt.Guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				MergeIn(mmt.Name, label, mmt.Guid);
				MergeIn(mmt.Abbreviation, abbrev, mmt.Guid);
				MergeIn(mmt.Description, description, mmt.Guid);
				m_dictMMT[id] = mmt.Hvo;
				m_rgnewMMT.Add(mmt);
			}
		}

		private int FindExistingPossibility(string id, string guidAttr, LiftMultiText label,
			LiftMultiText abbrev, Dictionary<string, int> dict, ICmPossibilityList list)
		{
			int hvo = GetHvoForGuidIfExisting(id, guidAttr, dict);
			if (hvo <= 0)
			{
				ICmPossibility poss = FindMatchingPossibility(list.PossibilitiesOS, label, abbrev);
				if (poss != null)
				{
					hvo = poss.Hvo;
					dict[id] = hvo;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return hvo;
		}

		private int GetHvoForGuidIfExisting(string id, string guidAttr, Dictionary<string, int> dict)
		{
			int hvo = 0;
			if (!String.IsNullOrEmpty(guidAttr))
			{
				hvo = m_cache.GetIdFromGuid(guidAttr);
				if (hvo > 0)
				{
					dict[id] = hvo;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return hvo;
		}

		ICmPossibility FindMatchingPossibility(FdoOwningSequence<ICmPossibility> possibilities,
			LiftMultiText label, LiftMultiText abbrev)
		{
			foreach (ICmPossibility item in possibilities)
			{
				if (HasMatchingAlternative(item.Name, label) &&
					HasMatchingAlternative(item.Abbreviation, abbrev))
				{
					return item;
				}
				ICmPossibility poss = FindMatchingPossibility(item.SubPossibilitiesOS, label, abbrev);
				if (poss != null)
					return poss;
			}
			return null;
		}

		private bool HasMatchingAlternative(MultiUnicodeAccessor mua, LiftMultiText text)
		{
			if (text != null && text.Keys != null)
			{
				foreach (string key in text.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					string sValue = text[key].Text;
					string sAlt = mua.GetAlternative(wsHvo);
					if (String.IsNullOrEmpty(sValue) || String.IsNullOrEmpty(sAlt))
						continue;
					if (sValue.ToLowerInvariant() == sAlt.ToLowerInvariant())
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
			int ws = m_rfcWs.GetWsFromRfcLang(id, m_sLiftDir);
			Debug.Assert(ws >= 1);
			ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
			MergeIn(lgws.Abbr, abbrev, lgws.Guid);
			MergeIn(lgws.Name, label, lgws.Guid);
			MergeIn(lgws.Description, description, lgws.Guid);
		}

		private void ProcessSlotDefinition(string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			int idx = range.IndexOf("-slot");
			if (idx < 0)
				idx = range.IndexOf("-Slots");
			string sOwner = range.Substring(0, idx);
			int hvoOwner = 0;
			if (m_dictPOS.ContainsKey(sOwner))
				hvoOwner = m_dictPOS[sOwner];
			if (hvoOwner < 1)
				hvoOwner = FindMatchingPossibility(sOwner.ToLowerInvariant(),
					m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPOS);
			if (hvoOwner < 1)
				return;
			IPartOfSpeech posOwner = PartOfSpeech.CreateFromDBObject(m_cache, hvoOwner);
			IMoInflAffixSlot slot = null;
			foreach (IMoInflAffixSlot slotT in posOwner.AffixSlotsOC)
			{

				if (HasMatchingAlternative(slotT.Name, label))
				{
					slot = slotT;
					break;
				}
			}
			if (slot == null)
			{
				slot = new MoInflAffixSlot();
				posOwner.AffixSlotsOC.Add(slot);
				MergeIn(slot.Name, label, slot.Guid);
				MergeIn(slot.Description, description, slot.Guid);
				// TODO: How to handle "Optional" field.
			}
		}

		private void ProcessInflectionClassDefinition(string range, string id, string guidAttr,
			string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			int idx = range.IndexOf("-infl-class");
			if (idx < 0)
				idx = range.IndexOf("-InflClasses");
			string sOwner = range.Substring(0, idx);
			int hvoOwner = 0;
			if (m_dictPOS.ContainsKey(sOwner))
				hvoOwner = m_dictPOS[sOwner];
			if (hvoOwner < 1)
				hvoOwner = FindMatchingPossibility(sOwner.ToLowerInvariant(),
					m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPOS);
			if (hvoOwner < 1)
				return;
			Dictionary<string, int> dict = null;
			if (m_dictDictSlots.ContainsKey(sOwner))
			{
				dict = m_dictDictSlots[sOwner];
			}
			else
			{
				dict = new Dictionary<string, int>();
				m_dictDictSlots[sOwner] = dict;
			}
			IPartOfSpeech posOwner = PartOfSpeech.CreateFromDBObject(m_cache, hvoOwner);
			IMoInflClass infl = null;
			IMoInflClass inflParent = null;
			if (!String.IsNullOrEmpty(parent))
			{
				int hvoParent = 0;
				if (dict.ContainsKey(parent))
				{
					hvoParent = dict[parent];
					inflParent = MoInflClass.CreateFromDBObject(m_cache, hvoParent);
				}
				else
				{
					inflParent = FindMatchingInflectionClass(parent, posOwner.InflectionClassesOC, dict);
				}
			}
			else
			{
				foreach (IMoInflClass inflT in posOwner.InflectionClassesOC)
				{
					if (HasMatchingAlternative(inflT.Name, label) &&
						HasMatchingAlternative(inflT.Abbreviation, abbrev))
					{
						infl = inflT;
						break;
					}
				}
			}
			if (infl == null)
			{
				infl = new MoInflClass();
				if (inflParent == null)
					posOwner.InflectionClassesOC.Add(infl);
				else
					inflParent.SubclassesOC.Add(infl);
				MergeIn(infl.Abbreviation, abbrev, infl.Guid);
				MergeIn(infl.Name, label, infl.Guid);
				MergeIn(infl.Description, description, infl.Guid);
				dict[id] = infl.Hvo;
			}
		}

		private IMoInflClass FindMatchingInflectionClass(string parent,
			FdoOwningCollection<IMoInflClass> collection, Dictionary<string, int> dict)
		{
			foreach (IMoInflClass infl in collection)
			{
				if (HasMatchingAlternative(parent.ToLowerInvariant(), infl.Abbreviation, infl.Name))
				{
					dict[parent] = infl.Hvo;
					return infl;
				}
				IMoInflClass inflT = FindMatchingInflectionClass(parent, infl.SubclassesOC, dict);
				if (inflT != null)
					return inflT;
			}
			return null;
		}

		private int FindMatchingPossibility(string sVal,
			FdoOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, int> dict)
		{
			foreach (ICmPossibility poss in possibilities)
			{
				if (HasMatchingAlternative(sVal, poss.Abbreviation, poss.Name))
				{
					if (dict != null)
						dict.Add(sVal, poss.Hvo);
					return poss.Hvo;
				}
				int hvoT = FindMatchingPossibility(sVal, poss.SubPossibilitiesOS, dict);
				if (hvoT != 0)
					return hvoT;
			}
			return 0;
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
			return new LiftSense(info, guid, m_cache, owner);
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

		private int GetHvoFromTargetIdString(string targetId)
		{
			if (m_mapIdHvo.ContainsKey(targetId))
				return m_mapIdHvo[targetId];
			string sGuid = FindGuidInString(targetId);
			if (!String.IsNullOrEmpty(sGuid))
			{
				Guid guidTarget = (Guid)GuidConv.ConvertFrom(sGuid);
				return m_cache.GetIdFromGuid(guidTarget);
			}
			return 0;
		}
		#endregion // Process Guids in import data

		#region Methods to find or create list items
		private int FindOrCreatePartOfSpeech(string val)
		{
			int hvo;
			if (m_dictPOS.TryGetValue(val, out hvo) ||
				m_dictPOS.TryGetValue(val.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			IPartOfSpeech pos = new PartOfSpeech();
			m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(pos);
			// Try to find this in the category catalog list, so we can add in more information.
			EticCategory cat = FindMatchingEticCategory(val);
			if (cat != null)
				AddEticCategoryInfo(cat, pos);
			if (String.IsNullOrEmpty(pos.Name.AnalysisDefaultWritingSystem))
				pos.Name.AnalysisDefaultWritingSystem = val;
			if (String.IsNullOrEmpty(pos.Abbreviation.AnalysisDefaultWritingSystem))
				pos.Abbreviation.AnalysisDefaultWritingSystem = val;
			m_dictPOS.Add(val, pos.Hvo);
			m_rgnewPOS.Add(pos);
			return pos.Hvo;
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

		private List<int> FindOrCreateEnvironment(string sEnv)
		{
			List<int> rghvo;
			if (!m_dictEnvirons.TryGetValue(sEnv, out rghvo))
			{
				IPhEnvironment envNew = new PhEnvironment();
				m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Append(envNew);
				envNew.StringRepresentation.UnderlyingTsString = m_cache.MakeAnalysisTss(sEnv);
				rghvo = new List<int>();
				rghvo.Add(envNew.Hvo);
				m_dictEnvirons.Add(sEnv, rghvo);
				m_rgnewEnvirons.Add(envNew);
			}
			return rghvo;
		}

		private int FindMorphType(string sTypeName)
		{
			int hvoMmt;
			if (m_dictMMT.TryGetValue(sTypeName, out hvoMmt) ||
				m_dictMMT.TryGetValue(sTypeName.ToLowerInvariant(), out hvoMmt))
			{
				return hvoMmt;
			}
			return 0;
		}

		private int FindOrCreateLexRefType(string relationTypeName, bool fIsSequence)
		{
			int hvo;
			if (m_dictLexRefTypes.TryGetValue(relationTypeName, out hvo) ||
				m_dictLexRefTypes.TryGetValue(relationTypeName.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			if (m_dictRevLexRefTypes.TryGetValue(relationTypeName, out hvo) ||
				m_dictRevLexRefTypes.TryGetValue(relationTypeName.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ILexRefType lrt = new LexRefType();
			m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Append(lrt);
			lrt.Name.AnalysisDefaultWritingSystem = relationTypeName;
			if ((String.IsNullOrEmpty(m_sLiftProducer) || m_sLiftProducer.StartsWith("WeSay")) &&
				(relationTypeName == "BaseForm"))
			{
				lrt.Abbreviation.AnalysisDefaultWritingSystem = "base";
				lrt.ReverseName.AnalysisDefaultWritingSystem = "Derived Forms";
				lrt.ReverseAbbreviation.AnalysisDefaultWritingSystem = "deriv";
				lrt.MappingType = (int)LexRefType.MappingTypes.kmtEntryTree;
			}
			else
			{
				lrt.Abbreviation.AnalysisDefaultWritingSystem = relationTypeName;
				if (fIsSequence)
					lrt.MappingType = (int)LexRefType.MappingTypes.kmtEntryOrSenseSequence;
				else
					lrt.MappingType = (int)LexRefType.MappingTypes.kmtEntryOrSenseCollection;
			}
			m_dictLexRefTypes.Add(relationTypeName, lrt.Hvo);
			m_rgnewLexRefTypes.Add(lrt);
			return lrt.Hvo;
		}

		private int FindComplexFormType(string sOldEntryType)
		{
			int hvo;
			if (m_dictComplexFormType.TryGetValue(sOldEntryType, out hvo) ||
				m_dictComplexFormType.TryGetValue(sOldEntryType.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			return 0;
		}

		private int FindVariantType(string sOldEntryType)
		{
			int hvo;
			if (m_dictVariantType.TryGetValue(sOldEntryType, out hvo) ||
				m_dictVariantType.TryGetValue(sOldEntryType.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			return 0;
		}

		private int FindOrCreateComplexFormType(string sType)
		{
			int hvo;
			if (m_dictComplexFormType.TryGetValue(sType, out hvo) ||
				m_dictComplexFormType.TryGetValue(sType.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ILexEntryType let = new LexEntryType();
			m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Append(let);
			let.Abbreviation.AnalysisDefaultWritingSystem = sType;
			let.Name.AnalysisDefaultWritingSystem = sType;
			let.ReverseAbbr.AnalysisDefaultWritingSystem = sType;
			m_dictComplexFormType.Add(sType, let.Hvo);
			m_rgnewComplexFormType.Add(let);
			return let.Hvo;
		}

		private int FindOrCreateVariantType(string sType)
		{
			int hvo;
			if (m_dictVariantType.TryGetValue(sType, out hvo) ||
				m_dictVariantType.TryGetValue(sType.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ILexEntryType let = new LexEntryType();
			m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Append(let);
			let.Abbreviation.AnalysisDefaultWritingSystem = sType;
			let.Name.AnalysisDefaultWritingSystem = sType;
			let.ReverseAbbr.AnalysisDefaultWritingSystem = sType;
			m_dictVariantType.Add(sType, let.Hvo);
			m_rgnewVariantType.Add(let);
			return let.Hvo;
		}

		private int FindOrCreateAnthroCode(string traitValue)
		{
			int hvo;
			if (m_dictAnthroCode.TryGetValue(traitValue, out hvo) ||
				m_dictAnthroCode.TryGetValue(traitValue.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmAnthroItem ant = new CmAnthroItem();
			m_cache.LangProject.AnthroListOA.PossibilitiesOS.Append(ant);
			ant.Abbreviation.AnalysisDefaultWritingSystem = traitValue;
			ant.Name.AnalysisDefaultWritingSystem = traitValue;
			m_dictAnthroCode.Add(traitValue, ant.Hvo);
			m_rgnewAnthroCode.Add(ant);
			return ant.Hvo;
		}

		private int FindOrCreateSemanticDomain(string traitValue)
		{
			int hvo;
			if (m_dictSemDom.TryGetValue(traitValue, out hvo) ||
				m_dictSemDom.TryGetValue(traitValue.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmSemanticDomain sem = new CmSemanticDomain();
			m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Append(sem);
			sem.Abbreviation.AnalysisDefaultWritingSystem = traitValue;
			sem.Name.AnalysisDefaultWritingSystem = traitValue;
			m_dictSemDom.Add(traitValue, sem.Hvo);
			m_rgnewSemDom.Add(sem);
			return sem.Hvo;
		}

		private int FindOrCreateDomainType(string traitValue)
		{
			int hvo;
			if (m_dictDomType.TryGetValue(traitValue, out hvo) ||
				m_dictDomType.TryGetValue(traitValue.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmPossibility poss = new CmPossibility();
			m_cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS.Append(poss);
			poss.Abbreviation.AnalysisDefaultWritingSystem = traitValue;
			poss.Name.AnalysisDefaultWritingSystem = traitValue;
			m_dictDomType.Add(traitValue, poss.Hvo);
			m_rgnewDomType.Add(poss);
			return poss.Hvo;
		}

		private int FindOrCreateSenseType(string traitValue)
		{
			int hvo;
			if (m_dictSenseType.TryGetValue(traitValue, out hvo) ||
				m_dictSenseType.TryGetValue(traitValue.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmPossibility poss = new CmPossibility();
			m_cache.LangProject.LexDbOA.SenseTypesOA.PossibilitiesOS.Append(poss);
			poss.Abbreviation.AnalysisDefaultWritingSystem = traitValue;
			poss.Name.AnalysisDefaultWritingSystem = traitValue;
			m_dictSenseType.Add(traitValue, poss.Hvo);
			m_rgnewSenseType.Add(poss);
			return poss.Hvo;
		}

		private int FindOrCreateStatus(string traitValue)
		{
			int hvo;
			if (m_dictStatus.TryGetValue(traitValue, out hvo) ||
				m_dictStatus.TryGetValue(traitValue.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmPossibility poss = new CmPossibility();
			m_cache.LangProject.LexDbOA.StatusOA.PossibilitiesOS.Append(poss);
			poss.Abbreviation.AnalysisDefaultWritingSystem = traitValue;
			poss.Name.AnalysisDefaultWritingSystem = traitValue;
			m_dictStatus.Add(traitValue, poss.Hvo);
			m_rgnewStatus.Add(poss);
			return poss.Hvo;
		}

		private int FindOrCreateTranslationType(string sType)
		{
			int hvo;
			if (m_dictTransType.TryGetValue(sType, out hvo) ||
				m_dictTransType.TryGetValue(sType.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmPossibility poss = new CmPossibility();
			m_cache.LangProject.TranslationTagsOA.PossibilitiesOS.Append(poss);
			poss.Name.AnalysisDefaultWritingSystem = sType;
			m_dictTransType.Add(sType, poss.Hvo);
			m_rgnewTransType.Add(poss);
			return poss.Hvo;
		}

		private int FindOrCreateUsageType(string traitValue)
		{
			int hvo;
			if (m_dictUsageType.TryGetValue(traitValue, out hvo) ||
				m_dictUsageType.TryGetValue(traitValue.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmPossibility poss = new CmPossibility();
			m_cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Append(poss);
			poss.Abbreviation.AnalysisDefaultWritingSystem = traitValue;
			poss.Name.AnalysisDefaultWritingSystem = traitValue;
			m_dictUsageType.Add(traitValue, poss.Hvo);
			m_rgnewUsageType.Add(poss);
			return poss.Hvo;
		}

		private int FindOrCreateLocation(string traitValue)
		{
			int hvo;
			if (m_dictLocation.TryGetValue(traitValue, out hvo) ||
				m_dictLocation.TryGetValue(traitValue.ToLowerInvariant(), out hvo))
			{
				return hvo;
			}
			ICmLocation poss = new CmLocation();
			m_cache.LangProject.LocationsOA.PossibilitiesOS.Append(poss);
			poss.Abbreviation.AnalysisDefaultWritingSystem = traitValue;
			poss.Name.AnalysisDefaultWritingSystem = traitValue;
			m_dictLocation.Add(traitValue, poss.Hvo);
			m_rgnewLocation.Add(poss);
			return poss.Hvo;
		}
		#endregion // Methods to find or create list items

		#region Methods for handling relation links
		/// <summary>
		/// After all the entries (and senses) have been imported, then the relations among
		/// them can be set since all the target ids can be resolved.
		/// This is also an opportunity to delete unwanted objects if we're keeping only
		/// the imported data.
		/// </summary>
		public void ProcessPendingRelations()
		{
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
			}
			StorePendingCollectionRelations();
			for (int i = 0; i < m_rgPendingTreeTargets.Count; ++i)
			{
				ProcessRemainingTreeRelation(m_rgPendingTreeTargets[i]);
			}
			// Now create the LexEntryRef type links.
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
			}
			// We can now store residue everywhere since any bogus relations have been added
			// to residue.
			WriteAccumulatedResidue();

			// If we're keeping only the imported data, erase any unused entries or senses.
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				GatherUnwantedEntries();
				DeleteUnwantedObjects();
			}
			// Now that the relations have all been set, it's safe to set the entry
			// modification times.
			foreach (PendingModifyTime pmt in m_rgPendingModifyTimes)
				pmt.SetModifyTime();
		}

		private void ProcessRemainingTreeRelation(PendingRelation rel)
		{
			Debug.Assert(rel.TargetHvo != 0);
			if (rel.TargetHvo == 0)
				return;
			string sType = rel.RelationType;
			Debug.Assert(!rel.IsSequence);
			int hvoType = FindOrCreateLexRefType(sType, false);
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, hvoType);
			if (!TreeRelationAlreadyExists(lrt, rel))
			{
				LexReference lr = new LexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Append(rel.TargetHvo);
				lr.TargetsRS.Append(rel.CmObject.Hvo);
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
			int hvo = m_rgPendingLexEntryRefs[i].CmObject.Hvo;
			string sEntryType = m_rgPendingLexEntryRefs[i].EntryType;
			string sMinorEntryCondition = m_rgPendingLexEntryRefs[i].MinorEntryCondition;
			DateTime dateCreated = m_rgPendingLexEntryRefs[i].DateCreated;
			DateTime dateModified = m_rgPendingLexEntryRefs[i].DateModified;
			string sResidue = m_rgPendingLexEntryRefs[i].Residue;
			while (i < m_rgPendingLexEntryRefs.Count)
			{
				PendingLexEntryRef pend = m_rgPendingLexEntryRefs[i];
				// If the object, entry type (in an old LIFT file), or minor entry condition
				// (in an old LIFT file) has changed, we're into another LexEntryRef.
				if (pend.CmObject.Hvo != hvo || pend.EntryType != sEntryType ||
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
				pend.TargetHvo = GetHvoFromTargetIdString(m_rgPendingLexEntryRefs[i].TargetId);
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
			int hvoTarget = 0;
			if (rgRefs.Count == 1 && rgRefs[0].RelationType == "main")
			{
				hvoTarget = rgRefs[0].CmObject.Hvo;
				string sRef = rgRefs[0].TargetId;
				int hvo;
				if (!String.IsNullOrEmpty(sRef) && m_mapIdHvo.TryGetValue(sRef, out hvo))
				{
					ICmObject cmo = CmObject.CreateFromDBObject(m_cache, hvo);
					Debug.Assert(cmo is ILexEntry);
					le = cmo as ILexEntry;
				}
				else
				{
					// log error message about invalid link in <relation type="main" ref="...">.
					InvalidRelation bad = new InvalidRelation(rgRefs[0], m_cache);
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
			ILexEntryRef ler = new LexEntryRef();
			le.EntryRefsOS.Append(ler);
			SetLexEntryTypes(rgRefs, ler);
			ler.HideMinorEntry = rgRefs[0].HideMinorEntry;
			if (rgRefs[0].Summary != null && rgRefs[0].Summary.Content != null)
				MergeIn(ler.Summary, rgRefs[0].Summary.Content, ler.Guid);
			for (int i = 0; i < rgRefs.Count; ++i)
			{
				PendingLexEntryRef pend = rgRefs[i];
				if (pend.RelationType == "main" && i == 0 && hvoTarget != 0)
				{
					ler.ComponentLexemesRS.Append(hvoTarget);
					ler.PrimaryLexemesRS.Append(hvoTarget);
				}
				else if (pend.TargetHvo != 0)
				{
					ler.ComponentLexemesRS.Append(pend.TargetHvo);
					if (pend.IsPrimary || pend.RelationType == "main")
						ler.PrimaryLexemesRS.Append(pend.TargetHvo);
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
				le.SensesOS.Append(new LexSense());
				(le as LexEntry).EnsureValidMSAsForSenses();
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
				ler.RefType = LexEntryRef.krtComplexForm;
			if (rgsVariantTypes.Count > 0)
				ler.RefType = LexEntryRef.krtVariant;
			if (rgsComplexFormTypes.Count > 0 && rgsVariantTypes.Count > 0)
			{
				// TODO: Complain to the user that he's getting ahead of the programmers!
			}
			foreach (string sType in rgsComplexFormTypes)
			{
				if (!String.IsNullOrEmpty(sType))
				{
					int hvo = FindOrCreateComplexFormType(sType);
					ler.ComplexEntryTypesRS.Append(hvo);
				}
			}
			foreach (string sType in rgsVariantTypes)
			{
				if (!String.IsNullOrEmpty(sType))
				{
					int hvo = FindOrCreateVariantType(sType);
					ler.VariantEntryTypesRS.Append(hvo);
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
				int hvo = FindComplexFormType(sOldEntryType);
				if (hvo == 0)
				{
					hvo = FindVariantType(sOldEntryType);
					if (hvo == 0 && sOldEntryType.ToLowerInvariant() != "main entry")
					{
						if (String.IsNullOrEmpty(sOldCondition))
						{
							ler.ComplexEntryTypesRS.Append(FindOrCreateComplexFormType(sOldEntryType));
							ler.RefType = LexEntryRef.krtComplexForm;
						}
						else
						{
							hvo = FindOrCreateVariantType(sOldEntryType);
						}
					}
					if (hvo != 0)
					{
						if (String.IsNullOrEmpty(sOldCondition))
						{
							ler.VariantEntryTypesRS.Append(hvo);
						}
						else
						{
							LexEntryType subtype = null;
							ILexEntryType type = LexEntryType.CreateFromDBObject(m_cache, hvo);
							foreach (ICmPossibility poss in type.SubPossibilitiesOS)
							{
								LexEntryType sub = poss as LexEntryType;
								if (sub != null &&
									(sub.Name.AnalysisDefaultWritingSystem == sOldCondition ||
									 sub.Abbreviation.AnalysisDefaultWritingSystem == sOldCondition ||
									 sub.ReverseAbbr.AnalysisDefaultWritingSystem == sOldCondition))
								{
									subtype = sub;
									break;
								}
							}
							if (subtype == null)
							{
								subtype = new LexEntryType();
								type.SubPossibilitiesOS.Append(subtype as ICmPossibility);
								subtype.Name.AnalysisDefaultWritingSystem = sOldCondition;
								subtype.Abbreviation.AnalysisDefaultWritingSystem = sOldCondition;
								subtype.ReverseAbbr.AnalysisDefaultWritingSystem = sOldCondition;
								m_rgnewVariantType.Add(subtype);
							}
							ler.VariantEntryTypesRS.Append(subtype);
						}
						ler.RefType = LexEntryRef.krtVariant;
					}
				}
				else
				{
					ler.ComplexEntryTypesRS.Append(hvo);
					ler.RefType = LexEntryRef.krtComplexForm;
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
				if (rgRelation[i].TargetHvo == 0)
					StoreResidue(rgRelation[i].CmObject, rgRelation[i].AsResidueString());
			}
			for (int i = rgRelation.Count - 1; i >= 0; --i)
			{
				if (rgRelation[i].TargetHvo == 0)
					rgRelation.RemoveAt(i);
			}
			if (rgRelation.Count == 0)
				return;
			// Store the list of relations appropriately as a LexReference with a proper type.
			string sType = rgRelation[0].RelationType;
			int hvoType = FindOrCreateLexRefType(sType, rgRelation[0].IsSequence);
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, hvoType);
			switch (lrt.MappingType)
			{
				case (int)LexRefType.MappingTypes.kmtEntryAsymmetricPair:
				case (int)LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case (int)LexRefType.MappingTypes.kmtSenseAsymmetricPair:
					StoreAsymmetricPairRelations(lrt, rgRelation,
						ObjectIsFirstInRelation(rgRelation[0].RelationType, lrt));
					break;
				case (int)LexRefType.MappingTypes.kmtEntryPair:
				case (int)LexRefType.MappingTypes.kmtEntryOrSensePair:
				case (int)LexRefType.MappingTypes.kmtSensePair:
					StorePairRelations(lrt, rgRelation);
					break;
				case (int)LexRefType.MappingTypes.kmtEntryCollection:
				case (int)LexRefType.MappingTypes.kmtEntryOrSenseCollection:
				case (int)LexRefType.MappingTypes.kmtSenseCollection:
					CollapseCollectionRelationPairs(rgRelation);
					break;
				case (int)LexRefType.MappingTypes.kmtEntryOrSenseSequence:
				case (int)LexRefType.MappingTypes.kmtEntrySequence:
				case (int)LexRefType.MappingTypes.kmtSenseSequence:
					StoreSequenceRelation(lrt, rgRelation);
					break;
				case (int)LexRefType.MappingTypes.kmtEntryOrSenseTree:
				case (int)LexRefType.MappingTypes.kmtEntryTree:
				case (int)LexRefType.MappingTypes.kmtSenseTree:
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
				Debug.Assert(rgRelation[i].TargetHvo != 0);
				if (rgRelation[i].TargetHvo == 0)
					continue;
				if (AsymmetricPairRelationAlreadyExists(lrt, rgRelation[i], fFirst))
					continue;
				LexReference lr = new LexReference();
				lrt.MembersOC.Add(lr);
				if (fFirst)
				{
					lr.TargetsRS.Append(rgRelation[i].CmObject.Hvo);
					lr.TargetsRS.Append(rgRelation[i].TargetHvo);
				}
				else
				{
					lr.TargetsRS.Append(rgRelation[i].TargetHvo);
					lr.TargetsRS.Append(rgRelation[i].CmObject.Hvo);
				}
				StoreRelationResidue(lr, rgRelation[i]);
			}
		}

		private bool AsymmetricPairRelationAlreadyExists(ILexRefType lrt, PendingRelation rel,
			bool fFirst)
		{
			int hvo1 = rel.CmObject.Hvo;
			int hvo2 = rel.TargetHvo;
			foreach (LexReference lr in lrt.MembersOC)
			{
				int[] targets = lr.TargetsRS.HvoArray;
				if (targets.Length != 2)
					continue;		// SHOULD NEVER HAPPEN!!
				int hvoA = targets[0];
				int hvoB = targets[1];
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
				Debug.Assert(rgRelation[i].TargetHvo != 0);
				if (rgRelation[i].TargetHvo == 0)
					continue;
				if (PairRelationAlreadyExists(lrt, rgRelation[i]))
					continue;
				LexReference lr = new LexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Append(rgRelation[i].CmObject.Hvo);
				lr.TargetsRS.Append(rgRelation[i].TargetHvo);
				StoreRelationResidue(lr, rgRelation[i]);
			}
		}

		private bool PairRelationAlreadyExists(ILexRefType lrt, PendingRelation rel)
		{
			int hvo1 = rel.CmObject.Hvo;
			int hvo2 = rel.TargetHvo;
			foreach (LexReference lr in lrt.MembersOC)
			{
				int[] targets = lr.TargetsRS.HvoArray;
				if (targets.Length != 2)
					continue;		// SHOULD NEVER HAPPEN!!
				int hvoA = targets[0];
				int hvoB = targets[1];
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
				Debug.Assert(rel.TargetHvo != 0);
				if (rel.TargetHvo == 0)
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

		private void StorePendingCollectionRelations()
		{
			while (m_rgPendingCollectionRelations.Count > 0)
			{
				PendingRelation rel = m_rgPendingCollectionRelations.First.Value;
				m_rgPendingCollectionRelations.RemoveFirst();
				StoreCollectionRelation(rel);
			}
		}

		private void StoreCollectionRelation(PendingRelation relMain)
		{
			string sType = relMain.RelationType;
			int hvoType = FindOrCreateLexRefType(sType, relMain.IsSequence);
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, hvoType);
			Set<int> currentRel = new Set<int>();
			currentRel.Add(relMain.CmObject.Hvo);
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
					if (currentRel.Contains(rel.CmObject.Hvo))
						hvoNew = rel.TargetHvo;
					else if (currentRel.Contains(rel.TargetHvo))
						hvoNew = rel.CmObject.Hvo;
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
						if ((rel2.CmObject.Hvo == hvoNew && currentRel.Contains(rel2.TargetHvo)) ||
							(rel2.TargetHvo == hvoNew && currentRel.Contains(rel2.CmObject.Hvo)))
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
			LexReference lr = new LexReference();
			lrt.MembersOC.Add(lr);
			foreach (int hvo in currentRel)
				lr.TargetsRS.Append(hvo);
			StoreRelationResidue(lr, relMain);
		}

		private bool CollectionRelationAlreadyExists(ILexRefType lrt, Set<int> setRelation)
		{
			foreach (LexReference lr in lrt.MembersOC)
			{
				int[] targets = lr.TargetsRS.HvoArray;
				if (targets.Length != setRelation.Count)
					continue;
				bool fSame = true;
				foreach (int hvo in setRelation)
				{
					if (!IsMember(targets, hvo))
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
			LexReference lr = new LexReference();
			lrt.MembersOC.Add(lr);
			for (int i = 0; i < rgRelation.Count; ++i)
				lr.TargetsRS.Append(rgRelation[i].TargetHvo);
			StoreRelationResidue(lr, rgRelation[0]);
		}

		private bool SequenceRelationAlreadyExists(ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			foreach (LexReference lr in lrt.MembersOC)
			{
				int[] targets = lr.TargetsRS.HvoArray;
				if (targets.Length != rgRelation.Count)
					continue;
				bool fSame = true;
				for (int i = 0; i < targets.Length; ++i)
				{
					if (targets[i] != rgRelation[i].TargetHvo)
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
				LexReference lr = new LexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Append(rgRelation[0].CmObject.Hvo);
				for (int i = 0; i < rgRelation.Count; ++i)
					lr.TargetsRS.Append(rgRelation[i].TargetHvo);
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

		private void StoreRelationResidue(LexReference lr, PendingRelation pend)
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
			foreach (LexReference lr in lrt.MembersOC)
			{
				int[] targets = lr.TargetsRS.HvoArray;
				if (targets.Length != rgRelation.Count + 1)
					continue;
				if (targets[0] != rgRelation[0].CmObject.Hvo)
					continue;
				int[] rghvoRef = new int[rgRelation.Count];
				for (int i = 0; i < rghvoRef.Length; ++i)
					rghvoRef[i] = targets[i + 1];
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
			foreach (LexReference lr in lrt.MembersOC)
			{
				int[] targets = lr.TargetsRS.HvoArray;
				if (targets.Length == 0 || targets[0] != rel.TargetHvo)
					continue;
				if (IsMember(targets, rel.CmObject.Hvo))
					return true;
			}
			return false;
		}

		private bool ObjectIsFirstInRelation(string sType, ILexRefType lrt)
		{
			if (HasMatchingAlternative(sType.ToLowerInvariant(), lrt.Abbreviation, lrt.Name))
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
			int hvo = m_rgPendingRelation[i].CmObject.Hvo;
			string sType = m_rgPendingRelation[i].RelationType;
			DateTime dateCreated = m_rgPendingRelation[i].DateCreated;
			DateTime dateModified = m_rgPendingRelation[i].DateModified;
			string sResidue = m_rgPendingRelation[i].Residue;
			while (i < m_rgPendingRelation.Count)
			{
				PendingRelation pend = m_rgPendingRelation[i];
				// If the object or relation type (or residue) has changed, we're into another
				// lexical relation.
				if (pend.CmObject.Hvo != hvo || pend.RelationType != sType ||
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
				pend.TargetHvo = GetHvoFromTargetIdString(m_rgPendingRelation[i].TargetId);
				rgRelation.Add(pend);	// We handle missing/unrecognized targets later.
				prev = pend;
				++i;
			}
			return rgRelation;
		}

		private void GatherUnwantedEntries()
		{
			foreach (ILexEntry le in m_cache.LangProject.LexDbOA.EntriesOC)
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
				CmObject.DeleteObjects(m_deletedObjects, m_cache);
				CmObject.DeleteOrphanedObjects(m_cache, false, null);
			}
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

			System.IO.StreamWriter writer = new System.IO.StreamWriter(sHtmlFile,
				false, System.Text.Encoding.UTF8);
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
			ListNewPossibilities(writer, LexTextControls.ksPartsOfSpeechAdded, m_rgnewPOS);
			ListNewPossibilities(writer, LexTextControls.ksMorphTypesAdded, m_rgnewMMT);
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
			ListNewWritingSystems(writer, LexTextControls.ksWritingSystemsAdded, m_rfcWs.AddedWrtSys);
			ListNewInflectionClasses(writer, LexTextControls.ksInflectionClassesAdded, m_rgnewInflClasses);
			ListNewSlots(writer, LexTextControls.ksInflectionalAffixSlotsAdded, m_rgnewSlots);
			ListConflictsFound(writer, LexTextControls.ksConflictsResultedInDup, m_rgcdConflicts);
			ListInvalidData(writer);
			ListTruncatedData(writer);
			ListInvalidRelations(writer);
			writer.WriteLine("</body>");
			writer.WriteLine("</html>");
			writer.Close();
			return sHtmlFile;
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
					writer.WriteLine("<li>{0}</li>", env.StringRepresentation.UnderlyingTsString.Text);
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewWritingSystems(System.IO.StreamWriter writer, string sMsg,
			List<ILgWritingSystem> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (ILgWritingSystem lgws in list)
					writer.WriteLine("<li>{0} ({1})</li>", lgws.ChooserNameTS.Text, lgws.ICULocale);
				writer.WriteLine("</ul>");
				// Ensure that the rest of the program knows about these new writing systems!
				m_cache.ResetLanguageEncodings();
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
					int hvo = infl.OwnerHVO;
					while (hvo != 0 && m_cache.GetClassOfObject(hvo) != PartOfSpeech.kclsidPartOfSpeech)
					{
						Debug.Assert(m_cache.GetClassOfObject(hvo) == MoInflClass.kclsidMoInflClass);
						if (m_cache.GetClassOfObject(hvo) == MoInflClass.kclsidMoInflClass)
						{
							IMoInflClass owner = MoInflClass.CreateFromDBObject(m_cache, hvo);
							sPos.Insert(0, String.Format(": {0}", owner.Name.BestAnalysisVernacularAlternative.Text));
						}
						hvo = m_cache.GetOwnerOfObject(hvo);
					}
					if (hvo != 0)
					{
						IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, hvo);
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
					int hvo = slot.OwnerHVO;
					if (hvo != 0 && m_cache.GetClassOfObject(hvo) == PartOfSpeech.kclsidPartOfSpeech)
					{
						IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, hvo);
						sPos = pos.Name.BestAnalysisVernacularAlternative.Text;
					}
					writer.WriteLine("<li>{0}: {1}</li>", sPos, slot.Name.BestAnalysisVernacularAlternative.Text);
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
			int hvo = m_cache.GetIdFromGuid(guid);
			if (hvo != 0)
			{
				DateTime dtMod = m_cache.GetTimeProperty(hvo, (int)LexEntry.LexEntryTags.kflidDateModified);
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
				ILexEntry le = new LexEntry();
				m_cache.LangProject.LexDbOA.EntriesOC.Add(le);
				if (m_cdConflict != null && m_cdConflict is ConflictingEntry)
				{
					(m_cdConflict as ConflictingEntry).DupEntry = le;
					m_rgcdConflicts.Add(m_cdConflict);
					m_cdConflict = null;
				}
				bool fNeedNewId = false;
				if (entry.Guid != Guid.Empty)
				{
					if (m_cache.GetIdFromGuid(entry.Guid) == 0)
						le.Guid = entry.Guid;		// should match with current entry.Id
					else
						fNeedNewId = true;
				}
				StoreEntryId(le, entry);
				le.HomographNumber = entry.Order;
				CreateLexemeForm(le, entry);		// also sets CitationForm if it exists.
				if (fNeedNewId)
				{
					XmlDocument xdEntryResidue = FindOrCreateResidue(le, entry.Id, (int)LexEntry.LexEntryTags.kflidLiftResidue);
					XmlAttribute xa = xdEntryResidue.FirstChild.Attributes["id"];
					if (xa == null)
					{
						xa = xdEntryResidue.CreateAttribute("id");
						xdEntryResidue.FirstChild.Attributes.Append(xa);
					}
					xa.Value = (le as LexEntry).LIFTid;
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
				FindOrCreateResidue(le, entry.Id, (int)LexEntry.LexEntryTags.kflidLiftResidue);
				m_mapIdHvo.Add(entry.Id, le.Hvo);
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
			(le as LexEntry).EnsureValidMSAsForSenses();
			// This can end up deleting old MSAs (when none were created by the import),
			// and is extremely slow for large databases (>1 sec per deletion).  One test
			// showed >80% of the import time was deleting old MSAs in this function.  :-(
			// So, since we try hard to reuse MSAs, we'll pretend for now that we aren't
			// introducing any redundancies!  :-)
			// We perhaps should have a separate tool for merging and deleting redundant
			// MSAs, but that doesn't exist (yet).
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
					m_cache.SetUnicodeProperty(hvo, flid, sLiftResidue);
			}
			m_dictResidue.Clear();
		}

		private static int StartOfLiftResidue(ITsStrBldr tsb)
		{
			int idx = tsb.Length;
			if (tsb.Text != null)
			{
				idx = tsb.Text.IndexOf("<lift-residue id=");
				if (idx < 0)
					idx = tsb.Length;
			}
			return idx;
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
			ILexEntry le = LexEntry.CreateFromDBObject(m_cache, entry.Hvo);
			if (LexemeFormsConflict(le, entry))
				return true;
			if (EntryEtymologiesConflict(le.EtymologyOA, entry.Etymologies))
			{
				m_cdConflict = new ConflictingEntry("Etymology", le);
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
			ILexEntry le = LexEntry.CreateFromDBObject(m_cache, entry.Hvo);
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
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				foreach (int hvo in le.SensesOS.HvoArray)
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftSense sense in entry.Senses)
			{
				ILexSense ls;
				map.TryGetValue(sense, out ls);
				if (ls == null || (m_msImport == MergeStyle.msKeepBoth && SenseHasConflictingData(ls, sense)))
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

		private ILexSense FindExistingSense(FdoOwningSequence<ILexSense> rgsenses, LiftSense sense)
		{
			if (sense.Hvo == 0)
				return null;
			foreach (ILexSense ls in rgsenses)
			{
				if (ls.Hvo == sense.Hvo)
					return ls;
			}
			return null;
		}

		private bool SenseHasConflictingData(ILexSense ls, LiftSense sense)
		{
			m_cdConflict = null;
			//sense.Order;
			if (MultiStringsConflict(ls.Gloss, sense.Gloss, false, Guid.Empty, 0))
			{
				m_cdConflict = new ConflictingSense("Gloss", ls);
				return true;
			}
			if (MultiStringsConflict(ls.Definition, sense.Definition))
			{
				m_cdConflict = new ConflictingSense("Definition", ls);
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
					le.Guid, (int)LexEntry.LexEntryTags.kflidLexemeForm);
				le.LexemeFormOA = mf;
				FinishMoForm(mf, entry.LexicalForm, tssForm, mmt, realForm,
					le.Guid, (int)LexEntry.LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm,
					le.LexemeFormOA == null ? MoStemAllomorph.kClassId : le.LexemeFormOA.ClassID,
					le.Guid, (int)LexEntry.LexEntryTags.kflidCitationForm);
			}
		}

		private IMoMorphType FindMorphType(ref string form, out int clsid, Guid guidEntry, int flid)
		{
			string fullForm = form;
			try
			{
				return MoMorphType.FindMorphType(m_cache, m_rgmmt, ref form, out clsid);
			}
			catch (Exception error)
			{
				InvalidData bad = new InvalidData(error.Message, guidEntry, flid, fullForm, 0, m_cache);
				if (!m_rgInvalidData.Contains(bad))
					m_rgInvalidData.Add(bad);
				form = fullForm;
				clsid = MoStemAllomorph.kclsidMoStemAllomorph;
				Guid guidMmt = new Guid(MoMorphType.kguidMorphStem);
				return MoMorphType.CreateFromDBObject(m_cache, m_cache.GetIdFromGuid(guidMmt));
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
						le.Guid, (int)LexEntry.LexEntryTags.kflidLexemeForm);
					if (MoMorphType.IsAffixType(m_cache, mmt.Hvo))
						mf = new MoAffixAllomorph();
					else
						mf = new MoStemAllomorph();
					le.LexemeFormOA = mf;
					mf.MorphTypeRA = mmt;
				}
				else
				{
					clsid = mf.ClassID;
				}
				MergeInAllomorphForms(entry.LexicalForm, mf.Form, clsid, le.Guid,
					(int)LexEntry.LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm,
					le.LexemeFormOA == null ? MoStemAllomorph.kClassId : le.LexemeFormOA.ClassID,
					le.Guid, (int)LexEntry.LexEntryTags.kflidCitationForm);
			}
		}

		private bool LexemeFormsConflict(ILexEntry le, LiftEntry entry)
		{
			if (MultiStringsConflict(le.CitationForm, entry.CitationForm, true,
				le.Guid, (int)LexEntry.LexEntryTags.kflidCitationForm))
			{
				m_cdConflict = new ConflictingEntry("Citation Form", le);
				return true;
			}
			if (le.LexemeFormOAHvo != 0)
			{
				if (MultiStringsConflict(le.LexemeFormOA.Form, entry.LexicalForm, true,
					le.Guid, (int)LexEntry.LexEntryTags.kflidLexemeForm))
				{
					m_cdConflict = new ConflictingEntry("Lexeme Form", le);
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
						if (le.ExcludeAsHeadword != fExclude && (m_fCreatingNewEntry || m_msImport != MergeStyle.msKeepOld))
							le.ExcludeAsHeadword = fExclude;
						// if EntryType is set, this may be used to initialize HideMinorEntry in a LexEntryRef.
						entry.ExcludeAsHeadword = fExclude;
						break;
					case "donotuseforparsing":	// original FLEX export = DoNotUseForParsing
					case "do-not-use-for-parsing":
						bool fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.DoNotUseForParsing != fDontUse && (m_fCreatingNewEntry || m_msImport != MergeStyle.msKeepOld))
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
			int hvoMmt = FindMorphType(traitValue);
			if (le.LexemeFormOA == null)
			{
				if (MoMorphType.IsAffixType(m_cache, hvoMmt))
					le.LexemeFormOA = new MoAffixAllomorph();
				else
					le.LexemeFormOA = new MoStemAllomorph();
				le.LexemeFormOA.MorphTypeRAHvo = hvoMmt;
			}
			else if (le.LexemeFormOA.MorphTypeRAHvo != hvoMmt &&
				(m_fCreatingNewEntry || m_msImport != MergeStyle.msKeepOld || le.LexemeFormOA.MorphTypeRAHvo == 0))
			{
				Debug.Assert(le is LexEntry);
				if (MoMorphType.IsAffixType(m_cache, hvoMmt))
				{
					if (le.LexemeFormOA is IMoStemAllomorph)
						(le as LexEntry).ReplaceMoForm(le.LexemeFormOA, new MoAffixAllomorph());
				}
				else
				{
					if (!(le.LexemeFormOA is IMoStemAllomorph))
						(le as LexEntry).ReplaceMoForm(le.LexemeFormOA, new MoStemAllomorph());
				}
				le.LexemeFormOA.MorphTypeRAHvo = hvoMmt;
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
						if (le.LexemeFormOA != null && le.LexemeFormOA.MorphTypeRAHvo != 0)
						{
							int hvoMmt = FindMorphType(lt.Value);
							if (le.LexemeFormOA.MorphTypeRAHvo != hvoMmt)
							{
								m_cdConflict = new ConflictingEntry("Morph Type", le);
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
							m_cdConflict = new ConflictingEntry("Exclude As Headword", le);
							return true;
						}
						break;
					case "donotuseforparsing":	// original FLEX export = DoNotUseForParsing
					case "do-not-use-for-parsing":
						bool fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.DoNotUseForParsing != fDontUse)
						{
							m_cdConflict = new ConflictingEntry("Do Not Use For Parsing", le);
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
						MergeIn(le.Bibliography, note.Content, le.Guid);
						break;
					case "":		// WeSay uses untyped notes in entries; LIFT now exports like this.
					case "comment":	// older Flex exported LIFT files have this type value.
						MergeIn(le.Comment, note.Content, le.Guid);
						break;
					case "restrictions":
						MergeIn(le.Restrictions, note.Content, le.Guid);
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
						if (MultiStringsConflict(le.Bibliography, note.Content))
						{
							m_cdConflict = new ConflictingEntry("Bibliography", le);
							return true;
						}
						break;
					case "comment":
						if (MultiStringsConflict(le.Comment, note.Content))
						{
							m_cdConflict = new ConflictingEntry("Note", le);
							return true;
						}
						break;
					case "restrictions":
						if (MultiStringsConflict(le.Restrictions, note.Content, false, Guid.Empty, 0))
						{
							m_cdConflict = new ConflictingEntry("Restrictions", le);
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
						StoreTsStringValue(m_fCreatingNewEntry, le.ImportResidue, lf.Content);
						break;
					case "literal_meaning":	// original FLEX export
					case "literal-meaning":
						MergeIn(le.LiteralMeaning, lf.Content, le.Guid);
						break;
					case "summary_definition":	// original FLEX export
					case "summary-definition":
						MergeIn(le.SummaryDefinition, lf.Content, le.Guid);
						break;
					default:
						ProcessUnknownField(le, entry, lf,
							"LexEntry", "custom-entry-", LexEntry.kclsidLexEntry);
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
							ITsStrBldr tsb = le.ImportResidue.UnderlyingTsString.GetBldr();
							int idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
								tsb.Replace(idx, tsb.Length, null, null);
							if (StringsConflict(tsb.GetString(), GetFirstLiftTsString(lf.Content)))
							{
								m_cdConflict = new ConflictingEntry("Import Residue", le);
								return true;
							}
						}
						break;
					case "literal_meaning":	// original FLEX export
					case "literal-meaning":
						if (MultiStringsConflict(le.LiteralMeaning, lf.Content))
						{
							m_cdConflict = new ConflictingEntry("Literal Meaning", le);
							return true;
						}
						break;
					case "summary_definition":	// original FLEX export
					case "summary-definition":
						if (MultiStringsConflict(le.SummaryDefinition, lf.Content))
						{
							m_cdConflict = new ConflictingEntry("Summary Definition", le);
							return true;
						}
						break;
					default:
						int flid;
						if (m_dictCustomFlid.TryGetValue("LexEntry-" + lf.Type, out flid))
						{
							if (CustomFieldDataConflicts("LexEntry", le.Hvo, flid, lf.Content))
							{
								m_cdConflict = new ConflictingEntry(String.Format("{0} (custom field)", lf.Type), le);
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessCustomFieldData(string sClass, int hvo, int flid, LiftMultiText contents)
		{
			int type = m_cache.MetaDataCacheAccessor.GetFieldType((uint)flid);
			string sView = sClass + "_" + m_cache.MetaDataCacheAccessor.GetFieldName((uint)flid);
			switch (type)
			{
				case (int)CellarModuleDefns.kcptString:
				case (int)CellarModuleDefns.kcptBigString:
					StoreTsStringValue(m_fCreatingNewEntry | m_fCreatingNewSense,
						new TsStringAccessor(m_cache, hvo, flid),
						contents);
					break;
				case (int)CellarModuleDefns.kcptMultiString:
				case (int)CellarModuleDefns.kcptMultiBigString:
					MultiStringAccessor msa = new MultiStringAccessor(m_cache, hvo, flid, sView);
					MergeIn(msa, contents, m_cache.GetGuidFromId(hvo));
					break;
				case (int)CellarModuleDefns.kcptMultiUnicode:
				case (int)CellarModuleDefns.kcptMultiBigUnicode:
					MultiUnicodeAccessor mua = new MultiUnicodeAccessor(m_cache, hvo, flid, sView);
					MergeIn(mua, contents, m_cache.GetGuidFromId(hvo));
					break;
				default:
					break;
			}
		}

		private bool CustomFieldDataConflicts(string sClass, int hvo, int flid, LiftMultiText contents)
		{
			int type = m_cache.MetaDataCacheAccessor.GetFieldType((uint)flid);
			string sView = sClass + "_" + m_cache.MetaDataCacheAccessor.GetFieldName((uint)flid);
			switch (type)
			{
				case (int)CellarModuleDefns.kcptString:
				case (int)CellarModuleDefns.kcptBigString:
					TsStringAccessor tsa = new TsStringAccessor(m_cache, hvo, flid);
					if (StringsConflict(tsa, GetFirstLiftTsString(contents)))
						return true;
					break;
				case (int)CellarModuleDefns.kcptMultiString:
				case (int)CellarModuleDefns.kcptMultiBigString:
					MultiStringAccessor msa = new MultiStringAccessor(m_cache, hvo, flid, sView);
					if (MultiStringsConflict(msa, contents))
						return true;
					break;
				case (int)CellarModuleDefns.kcptMultiUnicode:
				case (int)CellarModuleDefns.kcptMultiBigUnicode:
					MultiUnicodeAccessor mua = new MultiUnicodeAccessor(m_cache, hvo, flid, sView);
					if (MultiStringsConflict(mua, contents, false, Guid.Empty, 0))
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
					le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms);
				le.AlternateFormsOS.Append(mf);
				FinishMoForm(mf, lv.Form, tssForm, mmt, realForm,
					le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms);
				ProcessMoFormTraits(mf, lv);
				StoreResidueFromVariant(mf, lv);
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
				clsidForm = MoStemAllomorph.kclsidMoStemAllomorph;
				Guid guidType = new Guid(MoMorphType.kguidMorphStem);
				int hvoType = m_cache.GetIdFromGuid(guidType);
				mmt = MoMorphType.CreateFromDBObject(m_cache, hvoType);
				realForm = null;
			}
			else
			{
				realForm = tssForm.Text;
				mmt = FindMorphType(ref realForm, out clsidForm, guidEntry, flid);
			}
			int hvoType2;
			int clsidForm2 = GetMoFormClassFromTraits(traits, out hvoType2);
			if (clsidForm2 != 0 && hvoType2 != 0)
			{
				if (hvoType2 != mmt.Hvo)
					mmt = MoMorphType.CreateFromDBObject(m_cache, hvoType2);
				clsidForm = clsidForm2;
			}
			switch (clsidForm)
			{
				case MoStemAllomorph.kclsidMoStemAllomorph:
					return new MoStemAllomorph();
				case MoAffixAllomorph.kclsidMoAffixAllomorph:
					return new MoAffixAllomorph();
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
					le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms);
				if (mf == null)
				{
					ITsString tssForm = GetFirstLiftTsString(lv.Form);
					if (tssForm == null || tssForm.Text == null)
						continue;
					IMoMorphType mmt;
					string realForm = tssForm.Text;
					mf = CreateMoForm(lv.Traits, tssForm, out mmt, out realForm,
						le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms);
					le.AlternateFormsOS.Append(mf);
					FinishMoForm(mf, lv.Form, tssForm, mmt, realForm,
						le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms);
					dictHvoVariant.Add(mf.Hvo, lv);
				}
				else
				{
					MergeInAllomorphForms(lv.Form, mf.Form, mf.ClassID,
						le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms);
				}
				ProcessMoFormTraits(mf, lv);
				StoreResidueFromVariant(mf, lv);
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
					le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms);
				if (mf != null)
				{
					if (MultiStringsConflict(mf.Form, lv.Form, true,
						le.Guid, (int)LexEntry.LexEntryTags.kflidAlternateForms))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Alternate Form ({0})",
							TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)), le);
						return true;
					}
					if (MoFormTraitsConflict(mf, lv.Traits))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Alternate Form ({0}) details",
							TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)), le);
						return true;
					}
					++cCommon;
				}
			}
			if (cCommon < Math.Min(le.AlternateFormsOS.Count, list.Count))
			{
				m_cdConflict = new ConflictingEntry("Alternate Forms", le);
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
				int cCurrent = MultiStringMatches(mf.Form, lv.Form, true, guidEntry, flid);
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

		private int GetMoFormClassFromTraits(List<LiftTrait> traits, out int hvoType)
		{
			hvoType = 0;
			foreach (LiftTrait lt in traits)
			{
				if (lt.Name.ToLowerInvariant() == "morphtype" ||
					lt.Name.ToLowerInvariant() == "morph-type")
				{
					hvoType = FindMorphType(lt.Value);
					bool fAffix = MoMorphType.IsAffixType(m_cache, hvoType);
					if (fAffix)
						return MoAffixAllomorph.kclsidMoAffixAllomorph;
					else
						return MoStemAllomorph.kclsidMoStemAllomorph;
				}
			}
			return 0;	// no subclass info in the traits
		}

		private void ProcessMoFormTraits(IMoForm form, LiftVariant variant)
		{
			foreach (LiftTrait lt in variant.Traits)
			{
				int hvo = 0;
				switch (lt.Name.ToLowerInvariant())
				{
					case "morphtype":	// original FLEX export = MorphType
					case "morph-type":
						if (!m_fCreatingNewEntry && m_msImport == MergeStyle.msKeepOld && form.MorphTypeRAHvo != 0)
							continue;
						hvo = FindMorphType(lt.Value);
						bool fAffix = MoMorphType.IsAffixType(m_cache, hvo);
						if (fAffix && form is IMoStemAllomorph)
						{
							IMoStemAllomorph stem = form as IMoStemAllomorph;
							IMoAffixAllomorph affix = new MoAffixAllomorph();
							ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, form.OwnerHVO);
							(entry as LexEntry).ReplaceMoForm(stem, affix);
							form = affix;
						}
						else if (!fAffix && form is IMoAffixAllomorph)
						{
							IMoAffixAllomorph affix = form as IMoAffixAllomorph;
							IMoStemAllomorph stem = new MoStemAllomorph();
							ILexEntry entry = LexEntry.CreateFromDBObject(m_cache, form.OwnerHVO);
							(entry as LexEntry).ReplaceMoForm(affix, stem);
							form = stem;
						}
						if (hvo != form.MorphTypeRAHvo)
							form.MorphTypeRAHvo = hvo;
						break;
					case "environment":
						List<int> rghvo = FindOrCreateEnvironment(lt.Value);
						if (form is IMoStemAllomorph)
						{
							AddEnvironmentIfNeeded(rghvo, (form as IMoStemAllomorph).PhoneEnvRC);
						}
						else if (form is IMoAffixAllomorph)
						{
							AddEnvironmentIfNeeded(rghvo, (form as IMoAffixAllomorph).PhoneEnvRC);
						}
						break;
					default:
						StoreTraitAsResidue(form, lt);
						break;
				}
			}
		}

		private static void AddEnvironmentIfNeeded(List<int> rghvo, FdoReferenceCollection<IPhEnvironment> rgenv)
		{
			if (rgenv != null && rghvo != null)
			{
				bool fAlready = false;
				foreach (int hvo in rghvo)
				{
					if (rgenv.Contains(hvo))
					{
						fAlready = true;
						break;
					}
				}
				if (!fAlready && rghvo.Count > 0)
					rgenv.Add(rghvo[0]);
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
						if (mf.MorphTypeRAHvo != 0)
						{
							int hvo = FindMorphType(lt.Value);
							if (mf.MorphTypeRAHvo != hvo)
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

		private void CreateEntryPronunciations(ILexEntry le, LiftEntry entry)
		{
			foreach (LiftPhonetic phon in entry.Pronunciations)
			{
				ILexPronunciation pron = new LexPronunciation();
				le.PronunciationsOS.Append(pron);
				MergeIn(pron.Form, phon.Form, pron.Guid);
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
					pron = new LexPronunciation();
					le.PronunciationsOS.Append(pron);
					dictHvoPhon.Add(pron.Hvo, phon);
				}
				MergeIn(pron.Form, phon.Form, pron.Guid);
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
					if (!m_cache.LangProject.CurPronunWssRS.Contains(ws))
						m_cache.LangProject.CurPronunWssRS.Append(ws);
				}
			}
		}

		private void MergePronunciationMedia(ILexPronunciation pron, LiftPhonetic phon)
		{
			foreach (LiftURLRef uref in phon.Media)
			{
				string sFile = uref.URL;
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
					string sFolder = StringUtils.LocalMedia;
					media = new CmMedia();
					pron.MediaFilesOS.Append(media);
					string sLiftDir = Path.GetDirectoryName(m_sLiftFile);
					// Paths to try for resolving given filename:
					// {directory of LIFT file}/audio/filename
					// {FW ExtLinkRootDir}/filename
					// {FW ExtLinkRootDir}/Media/filename
					// {FW DataDir}/filename
					// {FW DataDir}/Media/filename
					// give up and store relative path Pictures/filename (even though it doesn't exist)
					string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
						String.Format("audio{0}{1}", Path.DirectorySeparatorChar, sFile));
					if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.ExtLinkRootDir))
					{
						sPath = Path.Combine(m_cache.LangProject.ExtLinkRootDir, sFile);
						if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.ExtLinkRootDir))
						{
							sPath = Path.Combine(m_cache.LangProject.ExtLinkRootDir,
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
						media.InitializeNewMedia(sPath, sLabel, sFolder, ws);
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
				MergeIn(media.Label, uref.Label, media.Guid);
			}
		}

		private ICmMedia FindMatchingMedia(FdoOwningSequence<ICmMedia> rgmedia, string sFile,
			LiftMultiText lmtLabel)
		{
			ICmMedia mediaMatching = null;
			int cMatches = 0;
			foreach (ICmMedia media in rgmedia)
			{
				if (media.MediaFileRAHvo == 0)
					continue;	// should NEVER happen!
				if (media.MediaFileRA.InternalPath == sFile ||
					Path.GetFileName(media.MediaFileRA.InternalPath) == sFile)
				{
					int cCurrent = MultiStringMatches(media.Label, lmtLabel);
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
					if (MultiStringsConflict(pron.Form, phon.Form, false, Guid.Empty, 0))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Pronunciation ({0})",
							TsStringAsHtml(pron.Form.BestVernacularAnalysisAlternative, m_cache)), le);
						return true;
					}
					if (PronunciationFieldsOrTraitsConflict(pron, phon))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Pronunciation ({0}) details",
							TsStringAsHtml(pron.Form.BestVernacularAnalysisAlternative, m_cache)), le);
						return true;
					}
					// TODO: Compare phon.Media and pron.MediaFilesOS
					++cCommon;
				}
			}
			if (cCommon < Math.Min(le.PronunciationsOS.Count, list.Count))
			{
				m_cdConflict = new ConflictingEntry("Pronunciations", le);
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
					Dictionary<int, string> forms = pron.Form.GetAllAlternatives();
					fFormMatches = (forms.Count == 0);
				}
				else
				{
					cCurrent = MultiStringMatches(pron.Form, phon.Form, false, Guid.Empty, 0);
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
							string sURL = phon.Media[i].URL;
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

		private void ProcessPronunciationFieldsAndTraits(ILexPronunciation pron, LiftPhonetic phon)
		{
			foreach (LiftField field in phon.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "cvpattern":
					case "cv-pattern":
						StoreTsStringValue(m_fCreatingNewEntry, pron.CVPattern, field.Content);
						break;
					case "tone":
						StoreTsStringValue(m_fCreatingNewEntry, pron.Tone, field.Content);
						break;
					default:
						ProcessUnknownField(pron, phon, field,
							"LexPronunciation", "custom-pronunciation-", LexPronunciation.kclsidLexPronunciation);
						break;
				}
			}
			int hvo;
			foreach (LiftTrait trait in phon.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case "location":
						hvo = FindOrCreateLocation(trait.Value);
						if (pron.LocationRAHvo != hvo && (m_fCreatingNewEntry || m_msImport != MergeStyle.msKeepOld || pron.LocationRAHvo == 0))
							pron.LocationRAHvo = hvo;
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
			int hvo;
			foreach (LiftTrait trait in phon.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case "location":
						hvo = FindOrCreateLocation(trait.Value);
						if (pron.LocationRAHvo != 0 && pron.LocationRAHvo != hvo)
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
						ILexEtymology ety = new LexEtymology();
						le.EtymologyOA = ety;
					}
					MergeIn(le.EtymologyOA.Form, let.Form, le.EtymologyOA.Guid);
					MergeIn(le.EtymologyOA.Gloss, let.Gloss, le.EtymologyOA.Guid);
					if (let.Source != null)
					{
						if (!m_fCreatingNewEntry && m_msImport == MergeStyle.msKeepOld)
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
				if (MultiStringsConflict(lexety.Form, ety.Form, false, Guid.Empty, 0))
					return true;
				if (MultiStringsConflict(lexety.Gloss, ety.Gloss, false, Guid.Empty, 0))
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
						MergeIn(ety.Comment, field.Content, ety.Guid);
						break;
					//case "multiform":		causes problems on round-tripping
					//    MergeIn(ety.Form, field.Content, ety.Guid);
					//    break;
					default:
						ProcessUnknownField(ety, let, field,
							"LexEtymology", "custom-etymology-", LexEtymology.kclsidLexEtymology);
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
						if (MultiStringsConflict(lexety.Comment, field.Content))
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
				ILexSense ls = new LexSense();
				le.SensesOS.Append(ls);
				FillInNewSense(ls, sense);
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
				ILexSense lsSub = new LexSense();
				ls.SensesOS.Append(lsSub);
				FillInNewSense(lsSub, sub);
			}
			finally
			{
				m_fCreatingNewSense = fSavedCreatingNew;
			}
		}

		private void FillInNewSense(ILexSense ls, LiftSense sense)
		{
			if (m_cdConflict != null && m_cdConflict is ConflictingSense)
			{
				(m_cdConflict as ConflictingSense).DupSense = ls;
				m_rgcdConflicts.Add(m_cdConflict);
				m_cdConflict = null;
			}
			//sense.Order;
			bool fNeedNewId = false;
			if (sense.Guid != Guid.Empty)
			{
				if (m_cache.GetIdFromGuid(sense.Guid) == 0)
					ls.Guid = sense.Guid;
				else
					fNeedNewId = true;
			}
			StoreSenseId(ls, sense.Id);
			MergeIn(ls.Gloss, sense.Gloss, ls.Guid);
			MergeIn(ls.Definition, sense.Definition, ls.Guid);
			if (fNeedNewId)
			{
				XmlDocument xd = FindOrCreateResidue(ls, sense.Id, (int)LexSense.LexSenseTags.kflidLiftResidue);
				XmlAttribute xa = xd.FirstChild.Attributes["id"];
				if (xa == null)
				{
					xa = xd.CreateAttribute("id");
					xd.FirstChild.Attributes.Append(xa);
				}
				xa.Value = (ls as LexSense).LIFTid;
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
			MergeIn(ls.Gloss, sense.Gloss, ls.Guid);
			MergeIn(ls.Definition, sense.Definition, ls.Guid);
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
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				foreach (int hvo in ls.SensesOS.HvoArray)
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftSense sub in sense.Subsenses)
			{
				ILexSense lsSub;
				map.TryGetValue(sub, out lsSub);
				if (lsSub == null || (m_msImport == MergeStyle.msKeepBoth && SenseHasConflictingData(lsSub, sub)))
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
				FindOrCreateResidue(ls, sId, (int)LexSense.LexSenseTags.kflidLiftResidue);
				m_mapIdHvo.Add(sId, ls.Hvo);
			}
		}

		private void CreateSenseExamples(ILexSense ls, LiftSense sense)
		{
			foreach (LiftExample expl in sense.Examples)
			{
				ILexExampleSentence les = new LexExampleSentence();
				ls.ExamplesOS.Append(les);
				if (expl.Guid != Guid.Empty && m_cache.GetIdFromGuid(expl.Guid) == 0)
					les.Guid = expl.Guid;
				if (!String.IsNullOrEmpty(expl.Id))
					m_mapIdHvo.Add(expl.Id, les.Hvo);
				MergeIn(les.Example, expl.Content, les.Guid);
				CreateExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference.UnderlyingTsString) && !String.IsNullOrEmpty(expl.Source))
					les.Reference.UnderlyingTsString = m_cache.MakeAnalysisTss(expl.Source);
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
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				foreach (int hvo in ls.ExamplesOS.HvoArray)
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
				{
					les = new LexExampleSentence();
					ls.ExamplesOS.Append(les);
					if (expl.Guid != Guid.Empty && m_cache.GetIdFromGuid(expl.Guid) == 0)
						les.Guid = expl.Guid;
				}
				if (!String.IsNullOrEmpty(expl.Id))
					m_mapIdHvo.Add(expl.Id, les.Hvo);
				MergeIn(les.Example, expl.Content, les.Guid);
				MergeExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference.UnderlyingTsString) && !String.IsNullOrEmpty(expl.Source))
					les.Reference.UnderlyingTsString = m_cache.MakeAnalysisTss(expl.Source);
			}
		}

		private ILexExampleSentence FindingMatchingExampleSentence(ILexSense ls, LiftExample expl)
		{
			ILexExampleSentence les = null;
			if (expl.Guid != Guid.Empty)
			{
				int hvo = m_cache.GetIdFromGuid(expl.Guid);
				if (hvo != 0 && m_cache.GetClassOfObject(hvo) == LexExampleSentence.kclsidLexExampleSentence)
				{
					les = LexExampleSentence.CreateFromDBObject(m_cache, hvo);
					if (les.OwnerHVO != ls.Hvo)
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
					int hvo = m_cache.GetIdFromGuid(expl.Guid);
					if (hvo != 0 && m_cache.GetClassOfObject(hvo) == LexExampleSentence.kclsidLexExampleSentence)
					{
						les = LexExampleSentence.CreateFromDBObject(m_cache, hvo);
						if (les.OwnerHVO != ls.Hvo)
							les = null;
					}
				}
				if (les == null)
					les = FindExampleSentence(ls.ExamplesOS, expl);
				if (les == null)
					continue;
				MergeIn(les.Example, expl.Content, les.Guid);
				if (MultiStringsConflict(les.Example, expl.Content))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0})",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls);
					return true;
				}
				if (StringsConflict(les.Reference, expl.Source))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Reference",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls);
					return true;
				}
				if (ExampleTranslationsConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Translations",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls);
					return true;
				}
				if (ExampleNotesConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Reference",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls);
					return true;
				}
			}
			return false;
		}

		private ILexExampleSentence FindExampleSentence(FdoOwningSequence<ILexExampleSentence> rgexamples, LiftExample expl)
		{
			List<ILexExampleSentence> matches = new List<ILexExampleSentence>();
			int cMatches = 0;
			foreach (ILexExampleSentence les in rgexamples)
			{
				int cCurrent = MultiStringMatches(les.Example, expl.Content);
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
				bool fSameReference = MatchingItemInNotes(les.Reference.UnderlyingTsString, "reference", expl.Notes);
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

		private int TranslationsMatch(FdoOwningCollection<ICmTranslation> oldList, List<LiftTranslation> newList)
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

		private static bool StringsMatch(string sOld, string sNew)
		{
			if (String.IsNullOrEmpty(sOld) && String.IsNullOrEmpty(sNew))
			{
				return true;
			}
			else if (String.IsNullOrEmpty(sOld) || String.IsNullOrEmpty(sNew))
			{
				return false;
			}
			else
			{
				string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				return sOldNorm == sNewNorm;
			}
		}

		private void CreateExampleTranslations(ILexExampleSentence les, LiftExample expl)
		{
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct = new CmTranslation();
				les.TranslationsOC.Add(ct);
				MergeIn(ct.Translation, tran.Content, ct.Guid);
				if (!String.IsNullOrEmpty(tran.Type))
					ct.TypeRAHvo = FindOrCreateTranslationType(tran.Type);
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
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				foreach (int hvo in les.TranslationsOC.HvoArray)
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct;
				map.TryGetValue(tran, out ct);
				if (ct == null)
				{
					ct = new CmTranslation();
					les.TranslationsOC.Add(ct);
				}
				MergeIn(ct.Translation, tran.Content, ct.Guid);
				if (!String.IsNullOrEmpty(tran.Type))
				{
					int hvo = FindOrCreateTranslationType(tran.Type);
					if (ct.TypeRAHvo != hvo && (m_fCreatingNewSense || m_msImport != MergeStyle.msKeepOld || ct.TypeRAHvo == 0))
						ct.TypeRAHvo = hvo;
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
				if (MultiStringsConflict(ct.Translation, tran.Content))
					return true;
				if (!String.IsNullOrEmpty(tran.Type))
				{
					int hvo = FindOrCreateTranslationType(tran.Type);
					if (ct.TypeRAHvo != hvo && ct.TypeRAHvo != 0)
						return true;
				}
			}
			return false;
		}

		private ICmTranslation FindExampleTranslation(FdoOwningCollection<ICmTranslation> rgtranslations,
			LiftTranslation tran)
		{
			ICmTranslation ctMatch = null;
			int cMatches = 0;
			foreach (ICmTranslation ct in rgtranslations)
			{
				int cCurrent = MultiStringMatches(ct.Translation, tran.Content);
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
						StoreTsStringValue(m_fCreatingNewSense, les.Reference, note.Content);
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

		private void ProcessSenseGramInfo(ILexSense ls, LiftSense sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				// except we always need a grammatical info element...
			}
			if (sense.GramInfo == null)
				return;
			if (!m_fCreatingNewSense && m_msImport == MergeStyle.msKeepOld && ls.MorphoSyntaxAnalysisRAHvo != 0)
				return;
			LiftGrammaticalInfo gram = sense.GramInfo;
			string sTraitPos = gram.Value;
			int hvoPos = 0;
			string sType = null;
			string sFromPOS = null;
			Dictionary<string, List<string>> dictPosSlots = new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> dictPosInflClasses = new Dictionary<string, List<string>>();
			List<string> rgsResidue = new List<string>();
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
					List<string> rgsInflClasses;
					if (!dictPosInflClasses.TryGetValue(sPos, out rgsInflClasses))
					{
						rgsInflClasses = new List<string>();
						dictPosInflClasses.Add(sPos, rgsInflClasses);
					}
					rgsInflClasses.Add(trait.Value);
				}
				else
				{
					rgsResidue.Add(CreateXmlForTrait(trait));
				}
			}
			if (!String.IsNullOrEmpty(sTraitPos))
				hvoPos = FindOrCreatePartOfSpeech(sTraitPos);
			FindOrCreateMSA(ls, hvoPos, sType, sFromPOS, dictPosSlots, dictPosInflClasses, rgsResidue);
		}

		/// <summary>
		/// Creating individual MSAs for every sense, and then merging identical MSAs at the
		/// end is expensive: deleting each redundant MSA takes ~360 msec, which can add up
		/// quickly even for only a few hundred duplications created here.  (See LT-9006.)
		/// </summary>
		private void FindOrCreateMSA(ILexSense ls, int hvoPos, string sType, string sFromPOS,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses, List<string> rgsResidue)
		{
			IMoMorphSynAnalysis msaSense = null;
			bool fNew;
			switch (sType)
			{
				case "inflAffix":
					fNew = FindOrCreateInflAffixMSA(ls.Entry, hvoPos, dictPosSlots, dictPosInflClasses, rgsResidue,
						ref msaSense);
					break;
				case "derivAffix":
					fNew = FindOrCreateDerivAffixMSA(ls.Entry, hvoPos, sFromPOS, dictPosSlots, dictPosInflClasses, rgsResidue,
						ref msaSense);
					break;
				case "derivStepAffix":
					fNew = FindOrCreateDerivStepAffixMSA(ls.Entry, hvoPos, dictPosSlots, dictPosInflClasses, rgsResidue,
						ref msaSense);
					break;
				case "affix":
					fNew = FindOrCreateUnclassifiedAffixMSA(ls.Entry, hvoPos, dictPosSlots, dictPosInflClasses, rgsResidue,
						ref msaSense);
					break;
				default:
					fNew = FindOrCreateStemMSA(ls.Entry, hvoPos, dictPosSlots, dictPosInflClasses, rgsResidue,
						ref msaSense);
					break;
			}
			if (fNew)
			{
				ProcessMsaSlotInformation(dictPosSlots, msaSense);
				ProcessMsaInflectionClassInfo(dictPosInflClasses, msaSense);
				StoreResidue(msaSense, rgsResidue);
			}
			ls.MorphoSyntaxAnalysisRAHvo = msaSense.Hvo;
		}

		/// <summary>
		/// Find or create an IMoStemMsa which matches the given values.
		/// </summary>
		/// <param name="le"></param>
		/// <param name="hvoPos"></param>
		/// <param name="dictPosSlots"></param>
		/// <param name="dictPosInflClasses"></param>
		/// <param name="rgsResidue"></param>
		/// <param name="msaSense"></param>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateStemMSA(ILexEntry le, int hvoPos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoStemMsa msaStem = msa as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRAHvo == hvoPos &&
					MsaSlotInfoMatches(dictPosSlots, msaStem) &&
					MsaInflClassInfoMatches(dictPosInflClasses, msaStem))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = new MoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (hvoPos != 0)
				(msaSense as MoStemMsa).PartOfSpeechRAHvo = hvoPos;
			return true;
		}

		/// <summary>
		/// Find or create an IMoUnclassifiedAffixMsa which matches the given values.
		/// </summary>
		/// <param name="le"></param>
		/// <param name="hvoPos"></param>
		/// <param name="dictPosSlots"></param>
		/// <param name="dictPosInflClasses"></param>
		/// <param name="rgsResidue"></param>
		/// <param name="msaSense"></param>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateUnclassifiedAffixMSA(ILexEntry le, int hvoPos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoUnclassifiedAffixMsa msaAffix = msa as IMoUnclassifiedAffixMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRAHvo == hvoPos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = new MoUnclassifiedAffixMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (hvoPos != 0)
				(msaSense as MoUnclassifiedAffixMsa).PartOfSpeechRAHvo = hvoPos;
			return true;
		}

		/// <summary>
		/// Find or create an IMoDerivStepMsa which matches the given values.
		/// </summary>
		/// <param name="le"></param>
		/// <param name="hvoPos"></param>
		/// <param name="dictPosSlots"></param>
		/// <param name="dictPosInflClasses"></param>
		/// <param name="rgsResidue"></param>
		/// <param name="msaSense"></param>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateDerivStepAffixMSA(ILexEntry le, int hvoPos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoDerivStepMsa msaAffix = msa as IMoDerivStepMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRAHvo == hvoPos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = new MoDerivStepMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (hvoPos != 0)
				(msaSense as MoDerivStepMsa).PartOfSpeechRAHvo = hvoPos;
			return true;
		}

		/// <summary>
		/// Find or create an IMoDerivAffMsa which matches the given values.
		/// </summary>
		/// <param name="le"></param>
		/// <param name="hvoPos"></param>
		/// <param name="sFromPOS"></param>
		/// <param name="dictPosSlots"></param>
		/// <param name="dictPosInflClasses"></param>
		/// <param name="rgsResidue"></param>
		/// <param name="msaSense"></param>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateDerivAffixMSA(ILexEntry le, int hvoPos, string sFromPOS,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			int hvoFrom = 0;
			if (!String.IsNullOrEmpty(sFromPOS))
				hvoFrom = FindOrCreatePartOfSpeech(sFromPOS);
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoDerivAffMsa msaAffix = msa as IMoDerivAffMsa;
				if (msaAffix != null &&
					msaAffix.ToPartOfSpeechRAHvo == hvoPos &&
					msaAffix.FromPartOfSpeechRAHvo == hvoFrom &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = new MoDerivAffMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (hvoPos != 0)
				(msaSense as MoDerivAffMsa).ToPartOfSpeechRAHvo = hvoPos;
			if (hvoFrom != 0)
				(msaSense as MoDerivAffMsa).FromPartOfSpeechRAHvo = hvoFrom;
			return true;
		}

		/// <summary>
		/// Find or create an IMoInflAffMsa which matches the given values.
		/// </summary>
		/// <param name="le"></param>
		/// <param name="hvoPos"></param>
		/// <param name="dictPosSlots"></param>
		/// <param name="dictPosInflClasses"></param>
		/// <param name="rgsResidue"></param>
		/// <param name="msaSense"></param>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateInflAffixMSA(ILexEntry le, int hvoPos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoInflAffMsa msaAffix = msa as IMoInflAffMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRAHvo == hvoPos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, msaAffix))
				// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = new MoInflAffMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (hvoPos != 0)
				(msaSense as MoInflAffMsa).PartOfSpeechRAHvo = hvoPos;
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
			IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
				return;
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
			IMoStemMsa msaStem = msa as IMoStemMsa;
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				if (msaDeriv != null)
					msaDeriv.ToInflectionClassRAHvo = 0;
				else if (msaStep != null)
					msaStep.InflectionClassRAHvo = 0;
				else if (msaStem != null)
					msaStem.InflectionClassRAHvo = 0;
			}
			foreach (string sPos in dictPosInflClasses.Keys)
			{
				int hvoPos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && hvoPos != 0)
				{
					IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, hvoPos);
					if (!m_fCreatingNewSense && m_msImport == MergeStyle.msKeepOld)
					{
						if (msaDeriv != null && msaDeriv.ToInflectionClassRAHvo != 0)
							return;
						if (msaStep != null && msaStep.InflectionClassRAHvo != 0)
							return;
						if (msaStem != null && msaStem.InflectionClassRAHvo != 0)
							return;
					}
					foreach (string sInflClass in rgsInflClasses)
					{
						IMoInflClass incl = null;
						foreach (IMoInflClass inclT in pos.InflectionClassesOC)
						{
							if (HasMatchingAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name))
							{
								incl = inclT;
								break;
							}
						}
						if (incl == null)
						{
							incl = new MoInflClass();
							pos.InflectionClassesOC.Add(incl);
							incl.Name.AnalysisDefaultWritingSystem = sInflClass;
							m_rgnewInflClasses.Add(incl);
						}
						if (msaDeriv != null)
							msaDeriv.ToInflectionClassRAHvo = incl.Hvo;
						else if (msaStep != null)
							msaStep.InflectionClassRAHvo = incl.Hvo;
						else if (msaStem != null)
							msaStem.InflectionClassRAHvo = incl.Hvo;
					}
				}
			}
		}

		private bool MsaInflClassInfoMatches(Dictionary<string, List<string>> dictPosInflClasses,
			IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
				return true;
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
			IMoStemMsa msaStem = msa as IMoStemMsa;
			bool fMatch = true;
			foreach (string sPos in dictPosInflClasses.Keys)
			{
				int hvoPos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && hvoPos != 0)
				{
					IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, hvoPos);
					foreach (string sInflClass in rgsInflClasses)
					{
						IMoInflClass incl = null;
						foreach (IMoInflClass inclT in pos.InflectionClassesOC)
						{
							if (HasMatchingAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name))
							{
								incl = inclT;
								break;
							}
						}
						if (incl == null)
						{
							// Go ahead and create the new inflection class now.
							incl = new MoInflClass();
							pos.InflectionClassesOC.Add(incl);
							incl.Name.AnalysisDefaultWritingSystem = sInflClass;
							m_rgnewInflClasses.Add(incl);
						}
						if (msaDeriv != null)
							fMatch = msaDeriv.ToInflectionClassRAHvo == incl.Hvo;
						else if (msaStep != null)
							fMatch = msaStep.InflectionClassRAHvo == incl.Hvo;
						else if (msaStem != null)
							fMatch = msaStem.InflectionClassRAHvo == incl.Hvo;
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
				int hvoPos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Count > 0 && hvoPos != 0)
				{
					IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, hvoPos);
					foreach (string sSlot in rgsSlot)
					{
						IMoInflAffixSlot slot = null;
						foreach (IMoInflAffixSlot slotT in pos.AffixSlotsOC)
						{
							if (HasMatchingAlternative(sSlot.ToLowerInvariant(), null, slotT.Name))
							{
								slot = slotT;
								break;
							}
						}
						if (slot == null)
						{
							slot = new MoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.AnalysisDefaultWritingSystem = sSlot;
							m_rgnewSlots.Add(slot);
						}
						if (!msaInfl.SlotsRC.Contains(slot.Hvo))
							msaInfl.SlotsRC.Add(slot.Hvo);
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
				int hvoPos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Count > 0 && hvoPos != 0)
				{
					IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, hvoPos);
					foreach (string sSlot in rgsSlot)
					{
						IMoInflAffixSlot slot = null;
						foreach (IMoInflAffixSlot slotT in pos.AffixSlotsOC)
						{
							if (HasMatchingAlternative(sSlot.ToLowerInvariant(), null, slotT.Name))
							{
								slot = slotT;
								break;
							}
						}
						if (slot == null)
						{
							// Go ahead and create the new slot -- we'll need it shortly.
							slot = new MoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.AnalysisDefaultWritingSystem = sSlot;
							m_rgnewSlots.Add(slot);
						}
						if (!msaInfl.SlotsRC.Contains(slot.Hvo))
							return false;
					}
				}
			}
			return true;
		}

		private bool SenseGramInfoConflicts(ILexSense ls, LiftGrammaticalInfo gram)
		{
			if (ls.MorphoSyntaxAnalysisRAHvo == 0 || gram == null)
				return false;
			string sPOS = gram.Value;
			int hvoPos = 0;
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
				hvoPos = FindOrCreatePartOfSpeech(sPOS);
			IMoMorphSynAnalysis msa = ls.MorphoSyntaxAnalysisRA;
			int hvoPosOld = 0;
			switch (sType)
			{
				case "inflAffix":
					IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
					if (msaInfl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls);
						return true;
					}
					hvoPosOld = msaInfl.PartOfSpeechRAHvo;
					break;
				case "derivAffix":
					IMoDerivAffMsa msaDerv = msa as IMoDerivAffMsa;
					if (msaDerv == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls);
						return true;
					}
					hvoPosOld = msaDerv.ToPartOfSpeechRAHvo;
					if (!String.IsNullOrEmpty(sFromPOS))
					{
						int hvoNewFrom = FindOrCreatePartOfSpeech(sFromPOS);
						int hvoOldFrom = msaDerv.FromPartOfSpeechRAHvo;
						if (hvoNewFrom != 0 && hvoOldFrom != 0 && hvoNewFrom != hvoOldFrom)
							return true;
					}
					break;
				case "derivStepAffix":
					IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
					if (msaStep == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls);
						return true;
					}
					hvoPosOld = msaStep.PartOfSpeechRAHvo;
					break;
				case "affix":
					IMoUnclassifiedAffixMsa msaUncl = msa as IMoUnclassifiedAffixMsa;
					if (msaUncl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls);
						return true;
					}
					hvoPosOld = msaUncl.PartOfSpeechRAHvo;
					break;
				default:
					IMoStemMsa msaStem = msa as IMoStemMsa;
					if (msaStem == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls);
						return true;
					}
					hvoPosOld = msaStem.PartOfSpeechRAHvo;
					break;
			}
			if (hvoPosOld != hvoPos && hvoPosOld != 0 && hvoPos != 0)
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Part of Speech", ls);
				return true;
			}
			if (MsaSlotInformationConflicts(dictPosSlots, msa))
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Slot", ls);
				return true;
			}
			if (MsaInflectionClassInfoConflicts(dictPosInflClasses, msa))
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Inflection Class", ls);
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
			foreach (LiftURLRef uref in sense.Illustrations)
			{
				string sFile = uref.URL;
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
		private void CreatePicture(ILexSense ls, LiftURLRef uref, string sFile, int ws)
		{
			string sFolder = StringUtils.LocalPictures;
			ICmPicture pict = new CmPicture();
			ls.PicturesOS.Append(pict);
			// Paths to try for resolving given filename:
			// {directory of LIFT file}/pictures/filename
			// {FW ExtLinkRootDir}/filename
			// {FW ExtLinkRootDir}/Pictures/filename
			// {FW DataDir}/filename
			// {FW DataDir}/Pictures/filename
			// give up and store relative path Pictures/filename (even though it doesn't exist)
			string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
				String.Format("pictures{0}{1}", Path.DirectorySeparatorChar, sFile));
			if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.ExtLinkRootDir))
			{
				sPath = Path.Combine(m_cache.LangProject.ExtLinkRootDir, sFile);
				if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.ExtLinkRootDir))
				{
					sPath = Path.Combine(m_cache.LangProject.ExtLinkRootDir,
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
			try
			{
				pict.InitializeNewPicture(sPath, GetFirstLiftTsString(uref.Label), sFolder, ws);
			}
			catch (ArgumentException ex)

			{
				// If sFile is empty, trying to create the CmFile for the picture will throw.
				// We don't care about this error as the caption will still be set properly.
				Debug.WriteLine("Error initializing picture: " + ex.Message);
			}
			if (!File.Exists(sPath))
			{
				pict.PictureFileRA.InternalPath =
					String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, sFile);
			}
			MergeIn(pict.Caption, uref.Label, pict.Guid);
		}

		private void MergeSenseIllustrations(ILexSense ls, LiftSense sense)
		{
			Dictionary<LiftURLRef, ICmPicture> map = new Dictionary<LiftURLRef, ICmPicture>();
			Set<int> setUsed = new Set<int>();
			foreach (LiftURLRef uref in sense.Illustrations)
			{
				string sFile = uref.URL.Replace('/', '\\');
				ICmPicture pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				map.Add(uref, pict);
				if (pict != null)
					setUsed.Add(pict.Hvo);
			}
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				foreach (int hvo in ls.PicturesOS.HvoArray)
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftURLRef uref in sense.Illustrations)
			{
				ICmPicture pict;
				map.TryGetValue(uref, out pict);
				if (pict == null)
				{
					string sFile = uref.URL.Replace('/', '\\');
					int ws = 0;
					if (uref.Label != null && !uref.Label.IsEmpty)
						ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
					else
						ws = m_cache.DefaultVernWs;
					CreatePicture(ls, uref, sFile, ws);
				}
				else
				{
					MergeIn(pict.Caption, uref.Label, pict.Guid);
				}
			}
		}

		private ICmPicture FindPicture(FdoOwningSequence<ICmPicture> rgpictures, string sFile,
			LiftMultiText lmtLabel)
		{
			ICmPicture pictMatching = null;
			int cMatches = 0;
			foreach (ICmPicture pict in rgpictures)
			{
				if (pict.PictureFileRAHvo == 0)
					continue;	// should NEVER happen!
				if (pict.PictureFileRA.InternalPath == sFile ||
					Path.GetFileName(pict.PictureFileRA.InternalPath) == sFile)
				{
					int cCurrent = MultiStringMatches(pict.Caption, lmtLabel);
					if (cCurrent >= cMatches)
					{
						pictMatching = pict;
						cMatches = cCurrent;
					}
				}
			}
			return pictMatching;
		}

		private bool SenseIllustrationsConflict(ILexSense ls, List<LiftURLRef> list)
		{
			if (ls.PicturesOS.Count == 0 || list.Count == 0)
				return false;
			foreach (LiftURLRef uref in list)
			{
				string sFile = uref.URL;
				if (sFile.StartsWith("file://"))
					sFile = sFile.Substring(7);
				ICmPicture pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				if (pict == null)
					continue;
				if (MultiStringsConflict(pict.Caption, uref.Label))
				{
					m_cdConflict = new ConflictingSense("Picture Caption", ls);
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
				if (rev.Form == null || rev.Form.IsEmpty)
					continue;
				IReversalIndexEntry rie = ProcessReversal(rev);
				if (!ls.ReversalEntriesRC.Contains(rie.Hvo))
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
				if (!m_mapToMapToRIE.TryGetValue(riOwning, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRIE.Add(riOwning, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, riOwning.EntriesOC);
			}
			else
			{
				IReversalIndexEntry rieOwner = ProcessReversal(rev.Main);	// recurse!
				if (!m_mapToMapToRIE.TryGetValue(rieOwner, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRIE.Add(rieOwner, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, rieOwner.SubentriesOC);
			}
			MergeIn(rie.ReversalForm, rev.Form, rie.Guid);
			ProcessReversalGramInfo(rie, rev.GramInfo);
			return rie;
		}

		private IReversalIndexEntry FindOrCreateMatchingReversal(LiftMultiText form,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs,
			FdoOwningCollection<IReversalIndexEntry> entriesOC)
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
						if (SameMultilingualContent(form, rieT.ReversalForm))
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
				rie = new ReversalIndexEntry();
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
				if (contents.Keys.Count == 1)
					sWs = contents.FirstValue.Key;
				else
					sWs = contents.FirstValue.Key.Split(new char[] { '_' })[0];
			}
			int ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sWs);
			if (ws == 0)
			{
				IWritingSystem wrSys = m_cache.LanguageWritingSystemFactoryAccessor.get_Engine(sWs);
				ws = wrSys.WritingSystem;
			}
			// A linear search should be safe here, because we don't expect more than 2 or 3
			// reversal indexes in any given project.
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				if (ri.WritingSystemRAHvo == ws)
				{
					riOwning = ri;
					break;
				}
			}
			if (riOwning == null)
			{
				riOwning = new ReversalIndex();
				m_cache.LangProject.LexDbOA.ReversalIndexesOC.Add(riOwning);
				riOwning.WritingSystemRAHvo = ws;
			}
			return riOwning;
		}

		private void ProcessReversalGramInfo(IReversalIndexEntry rie, LiftGrammaticalInfo gram)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.msKeepOnlyNew)
				rie.PartOfSpeechRAHvo = 0;
			if (gram == null || String.IsNullOrEmpty(gram.Value))
				return;
			string sPOS = gram.Value;
			IReversalIndex ri = rie.ReversalIndex;
			Dictionary<string, int> dict = null;
			if (m_dictWsReversalPOS.ContainsKey(ri.WritingSystemRAHvo))
			{
				dict = m_dictWsReversalPOS[ri.WritingSystemRAHvo];
			}
			else
			{
				dict = new Dictionary<string, int>();
				m_dictWsReversalPOS.Add(ri.WritingSystemRAHvo, dict);
			}
			if (dict.ContainsKey(sPOS))
			{
				if (!m_fCreatingNewSense && m_msImport == MergeStyle.msKeepOld)
				{
					if (rie.PartOfSpeechRAHvo == 0)
						rie.PartOfSpeechRAHvo = dict[sPOS];
				}
				else
				{
					rie.PartOfSpeechRAHvo = dict[sPOS];
				}
			}
			else
			{
				IPartOfSpeech pos = new PartOfSpeech();
				ri.PartsOfSpeechOA.PossibilitiesOS.Append(pos);
				// Use the name and abbreviation from a regular PartOfSpeech if available, otherwise
				// just use the key and hope the user can sort it out later.
				if (m_dictPOS.ContainsKey(sPOS))
				{
					IPartOfSpeech posMain = PartOfSpeech.CreateFromDBObject(m_cache, m_dictPOS[sPOS]);
					pos.Abbreviation.MergeAlternatives(posMain.Abbreviation);
					pos.Name.MergeAlternatives(posMain.Name);
				}
				else
				{
					pos.Abbreviation.AnalysisDefaultWritingSystem = sPOS;
					pos.Name.AnalysisDefaultWritingSystem = sPOS;
				}
				if (!m_fCreatingNewSense && m_msImport == MergeStyle.msKeepOld)
				{
					if (rie.PartOfSpeechRAHvo == 0)
						rie.PartOfSpeechRAHvo = pos.Hvo;
				}
				else
				{
					rie.PartOfSpeechRAHvo = pos.Hvo;
				}
				dict.Add(sPOS, pos.Hvo);
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
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				MergeIn(ls.AnthroNote, null, ls.Guid);
				MergeIn(ls.Bibliography, null, ls.Guid);
				MergeIn(ls.DiscourseNote, null, ls.Guid);
				MergeIn(ls.EncyclopedicInfo, null, ls.Guid);
				MergeIn(ls.GeneralNote, null, ls.Guid);
				MergeIn(ls.GrammarNote, null, ls.Guid);
				MergeIn(ls.PhonologyNote, null, ls.Guid);
				MergeIn(ls.Restrictions, null, ls.Guid);
				MergeIn(ls.SemanticsNote, null, ls.Guid);
				MergeIn(ls.SocioLinguisticsNote, null, ls.Guid);
				ls.Source.Text = null;
			}
			foreach (LiftNote note in sense.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "anthropology":
						MergeIn(ls.AnthroNote, note.Content, ls.Guid);
						break;
					case "bibliography":
						MergeIn(ls.Bibliography, note.Content, ls.Guid);
						break;
					case "discourse":
						MergeIn(ls.DiscourseNote, note.Content, ls.Guid);
						break;
					case "encyclopedic":
						MergeIn(ls.EncyclopedicInfo, note.Content, ls.Guid);
						break;
					case "":		// WeSay uses untyped notes in senses; LIFT now exports like this.
					case "general":	// older Flex exported LIFT files have this type value.
						MergeIn(ls.GeneralNote, note.Content, ls.Guid);
						break;
					case "grammar":
						MergeIn(ls.GrammarNote, note.Content, ls.Guid);
						break;
					case "phonology":
						MergeIn(ls.PhonologyNote, note.Content, ls.Guid);
						break;
					case "restrictions":
						MergeIn(ls.Restrictions, note.Content, ls.Guid);
						break;
					case "semantics":
						MergeIn(ls.SemanticsNote, note.Content, ls.Guid);
						break;
					case "sociolinguistics":
						MergeIn(ls.SocioLinguisticsNote, note.Content, ls.Guid);
						break;
					case "source":
						StoreTsStringValue(m_fCreatingNewSense, ls.Source, note.Content);
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
						if (MultiStringsConflict(ls.AnthroNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Anthropology Note", ls);
							return true;
						}
						break;
					case "bibliography":
						if (MultiStringsConflict(ls.Bibliography, note.Content))
						{
							m_cdConflict = new ConflictingSense("Bibliography", ls);
							return true;
						}
						break;
					case "discourse":
						if (MultiStringsConflict(ls.DiscourseNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Discourse Note", ls);
							return true;
						}
						break;
					case "encyclopedic":
						if (MultiStringsConflict(ls.EncyclopedicInfo, note.Content))
						{
							m_cdConflict = new ConflictingSense("Encyclopedic Info", ls);
							return true;
						}
						break;
					case "general":
						if (MultiStringsConflict(ls.GeneralNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("General Note", ls);
							return true;
						}
						break;
					case "grammar":
						if (MultiStringsConflict(ls.GrammarNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Grammar Note", ls);
							return true;
						}
						break;
					case "phonology":
						if (MultiStringsConflict(ls.PhonologyNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Phonology Note", ls);
							return true;
						}
						break;
					case "restrictions":
						if (MultiStringsConflict(ls.Restrictions, note.Content, false, Guid.Empty, 0))
						{
							m_cdConflict = new ConflictingSense("Restrictions", ls);
							return true;
						}
						break;
					case "semantics":
						if (MultiStringsConflict(ls.SemanticsNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Semantics Note", ls);
							return true;
						}
						break;
					case "sociolinguistics":
						if (MultiStringsConflict(ls.SocioLinguisticsNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Sociolinguistics Note", ls);
							return true;
						}
						break;
					case "source":
						if (StringsConflict(ls.Source, GetFirstLiftTsString(note.Content)))
						{
							m_cdConflict = new ConflictingSense("Source", ls);
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
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				ls.ImportResidue.Text = null;
				ls.ScientificName.Text = null;
				ClearCustomFields(LexSense.kclsidLexSense);
			}
			foreach (LiftField field in sense.Fields)
			{
				string sType = field.Type;
				switch (sType.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						StoreTsStringValue(m_fCreatingNewSense, ls.ImportResidue, field.Content);
						break;
					case "scientific_name":	// original FLEX export
					case "scientific-name":
						StoreTsStringValue(m_fCreatingNewSense, ls.ScientificName, field.Content);
						break;
					default:
						ProcessUnknownField(ls, sense, field,
							"LexSense", "custom-sense-", LexSense.kclsidLexSense);
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
				ProcessCustomFieldData(sClass, co.Hvo, flid, field.Content);
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
					FindOrCreateResidue(co, obj.Id, (int)LexEntry.LexEntryTags.kflidLiftResidue);
					StoreFieldAsResidue(co, field);
				}
				else
				{
					ProcessCustomFieldData(sClass, co.Hvo, flid, field.Content);
				}
			}
		}

		private void StoreTsStringValue(bool fCreatingNew, TsStringAccessor tsa, LiftMultiText lmt)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.msKeepOnlyNew)
				tsa.Text = null;
			ITsString tss = GetFirstLiftTsString(lmt);
			if (TsStringIsNullOrEmpty(tss))
				return;
			if (!fCreatingNew && m_msImport == MergeStyle.msKeepOld)
			{
				if (TsStringIsNullOrEmpty(tsa.UnderlyingTsString))
					tsa.UnderlyingTsString = tss;
			}
			else
			{
				tsa.UnderlyingTsString = tss;
			}
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
							ITsStrBldr tsb = ls.ImportResidue.UnderlyingTsString.GetBldr();
							int idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
								tsb.Replace(idx, tsb.Length, null, null);
							if (StringsConflict(tsb.GetString(), GetFirstLiftTsString(field.Content)))
							{
								m_cdConflict = new ConflictingSense("Import Residue", ls);
								return true;
							}
						}
						break;
					case "scientific_name":	// original FLEX export
					case "scientific-name":
						if (StringsConflict(ls.ScientificName, GetFirstLiftTsString(field.Content)))
						{
							m_cdConflict = new ConflictingSense("Scientific Name", ls);
							return true;
						}
						break;
					default:
						int flid;
						if (m_dictCustomFlid.TryGetValue("LexSense-" + sType, out flid))
						{
							if (CustomFieldDataConflicts("LexSense", ls.Hvo, flid, field.Content))
							{
								m_cdConflict = new ConflictingSense(String.Format("{0} (custom field)", sType), ls);
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
			if (m_msImport == MergeStyle.msKeepOnlyNew)
			{
				ls.AnthroCodesRC.RemoveAll();
				ls.SemanticDomainsRC.RemoveAll();
				ls.DomainTypesRC.RemoveAll();
				ls.SenseTypeRAHvo = 0;
				ls.StatusRAHvo = 0;
				ls.UsageTypesRC.RemoveAll();
			}
			int hvo;
			foreach (LiftTrait lt in sense.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "anthro-code":
						hvo = FindOrCreateAnthroCode(lt.Value);
						if (!ls.AnthroCodesRC.Contains(hvo))
							ls.AnthroCodesRC.Add(hvo);
						break;
					case "semanticdomainddp4":	// for WeSay 0.4 compatibility
					case "semantic_domain":
					case "semantic-domain":
					case "semantic-domain-ddp4":
						hvo = FindOrCreateSemanticDomain(lt.Value);
						if (!ls.SemanticDomainsRC.Contains(hvo))
							ls.SemanticDomainsRC.Add(hvo);
						break;
					case "domaintype":	// original FLEX export = DomainType
					case "domain-type":
						hvo = FindOrCreateDomainType(lt.Value);
						if (!ls.DomainTypesRC.Contains(hvo))
							ls.DomainTypesRC.Add(hvo);
						break;
					case "sensetype":	// original FLEX export = SenseType
					case "sense-type":
						hvo = FindOrCreateSenseType(lt.Value);
						if (ls.SenseTypeRAHvo != hvo && (m_fCreatingNewSense || m_msImport != MergeStyle.msKeepOld || ls.SenseTypeRAHvo == 0))
							ls.SenseTypeRAHvo = hvo;
						break;
					case "status":
						hvo = FindOrCreateStatus(lt.Value);
						if (ls.StatusRAHvo != hvo && (m_fCreatingNewSense || m_msImport != MergeStyle.msKeepOld || ls.StatusRAHvo == 0))
							ls.StatusRAHvo = hvo;
						break;
					case "usagetype":	// original FLEX export = UsageType
					case "usage-type":
						hvo = FindOrCreateUsageType(lt.Value);
						if (!ls.UsageTypesRC.Contains(hvo))
							ls.UsageTypesRC.Add(hvo);
						break;
					default:
						StoreTraitAsResidue(ls, lt);
						break;
				}
			}
		}

		private bool SenseTraitsConflict(ILexSense ls, List<LiftTrait> list)
		{
			int hvo;
			foreach (LiftTrait lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "anthro-code":
						hvo = FindOrCreateAnthroCode(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case "semantic_domain":
					case "semantic-domain":
						hvo = FindOrCreateSemanticDomain(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case "domaintype":	// original FLEX export = DomainType
					case "domain-type":
						hvo = FindOrCreateDomainType(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case "sensetype":	// original FLEX export = SenseType
					case "sense-type":
						hvo = FindOrCreateSenseType(lt.Value);
						if (ls.SenseTypeRAHvo != 0 && ls.SenseTypeRAHvo != hvo)
						{
							m_cdConflict = new ConflictingSense("Sense Type", ls);
							return true;
						}
						break;
					case "status":
						hvo = FindOrCreateStatus(lt.Value);
						if (ls.StatusRAHvo != 0 && ls.StatusRAHvo != hvo)
						{
							m_cdConflict = new ConflictingSense("Status", ls);
							return true;
						}
						break;
					case "usagetype":	// original FLEX export = UsageType
					case "usage-type":
						hvo = FindOrCreateUsageType(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
				}
			}
			return false;
		}
		#endregion // Methods for storing entry data
	}
}
