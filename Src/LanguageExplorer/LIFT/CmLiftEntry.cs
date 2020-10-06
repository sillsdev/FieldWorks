// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.LIFT
{
	/// <summary>
	/// This class implements "entry" from the LIFT standard.
	/// It corresponds to LexEntry in the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftEntry : LiftObject
	{
		private CmLiftEntry()
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

		internal CmLiftEntry(Extensible info, Guid guid, int order, FlexLiftMerger merger)
			: this()
		{
			Id = info.Id;
			Guid = guid;
			CmObject = guid == Guid.Empty ? null : merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			Order = order; // Reset from default constructor tp provided value.
		}

		public override string XmlTag => "entry";

		internal ICmObject CmObject { get; set; }

		internal int Order { get; set; }

		internal DateTime DateDeleted { get; set; }

		internal LiftMultiText LexicalForm { get; set; } // safe-XML

		internal LiftMultiText CitationForm { get; set; } // safe-XML

		internal List<CmLiftPhonetic> Pronunciations { get; }

		internal List<CmLiftVariant> Variants { get; }

		internal List<CmLiftSense> Senses { get; }

		internal List<CmLiftNote> Notes { get; }

		internal List<CmLiftRelation> Relations { get; }

		internal List<CmLiftEtymology> Etymologies { get; }

		internal string EntryType { get; set; }

		internal string MinorEntryCondition { get; set; }

		internal bool ExcludeAsHeadword { get; set; }
	}
}