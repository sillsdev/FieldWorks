// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements "entry" from the LIFT standard.
	/// It corresponds to LexEntry in the FieldWorks model.
	/// </summary>
	public class CmLiftEntry : LiftObject, ICmLiftObject
	{
		public CmLiftEntry()
		{
			Etymologies = new List<CmLiftEtymology>();
			Relations = new List<CmLiftRelation>();
			Notes = new List<CmLiftNote>();
			Senses = new List<CmLiftSense>();
			Variants = new List<CmLiftVariant>();
			Pronunciations = new List<CmLiftPhonetic>();
			Order = 0;
			DateDeleted = DateTime.MinValue;
		}

		public CmLiftEntry(Extensible info, Guid guid, int order, FlexLiftMerger merger)
		{
			Etymologies = new List<CmLiftEtymology>();
			Relations = new List<CmLiftRelation>();
			Notes = new List<CmLiftNote>();
			Senses = new List<CmLiftSense>();
			Variants = new List<CmLiftVariant>();
			Pronunciations = new List<CmLiftPhonetic>();
			Id = info.Id;
			Guid = guid;
			CmObject = guid == Guid.Empty ? null : merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			DateDeleted = DateTime.MinValue;
			Order = order;
		}

		public ICmObject CmObject { get; set; }

		public int Order { get; set; }

		public DateTime DateDeleted { get; set; }

		public LiftMultiText LexicalForm { get; set; } // safe-XML

		public LiftMultiText CitationForm { get; set; } // safe-XML

		public List<CmLiftPhonetic> Pronunciations { get; }

		public List<CmLiftVariant> Variants { get; }

		public List<CmLiftSense> Senses { get; }

		public List<CmLiftNote> Notes { get; }

		public List<CmLiftRelation> Relations { get; }

		public List<CmLiftEtymology> Etymologies { get; }

		public override string XmlTag => "entry";

		public string EntryType { get; set; }

		public string MinorEntryCondition { get; set; }

		public bool ExcludeAsHeadword { get; set; }
	}
}