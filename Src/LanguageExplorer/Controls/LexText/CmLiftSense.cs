// Copyright (c) 2011-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel;
using SIL.Lift.Parsing;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements "sense" from the LIFT standard.
	/// It corresponds to LexSense from the FieldWorks model.
	/// </summary>
	public class CmLiftSense : LiftObject, ICmLiftObject
	{
		public CmLiftSense()
		{
			Subsenses = new List<CmLiftSense>();
			Illustrations = new List<LiftUrlRef>();
			Reversals = new List<CmLiftReversal>();
			Examples = new List<CmLiftExample>();
			Notes = new List<CmLiftNote>();
			Relations = new List<CmLiftRelation>();
		}

		public CmLiftSense(Extensible info, Guid guid, LiftObject owner, FlexLiftMerger merger)
		{
			Subsenses = new List<CmLiftSense>();
			Illustrations = new List<LiftUrlRef>();
			Reversals = new List<CmLiftReversal>();
			Examples = new List<CmLiftExample>();
			Notes = new List<CmLiftNote>();
			Relations = new List<CmLiftRelation>();
			Id = info.Id;
			Guid = guid;
			CmObject = guid == Guid.Empty ? null : merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			Owner = owner;
		}

		public ICmObject CmObject { get; set; }

		public int Order { get; set; }

		public LiftGrammaticalInfo GramInfo { get; set; }

		public LiftMultiText Gloss { get; set; }  // safe-XML

		public LiftMultiText Definition { get; set; } // safe-XML

		public List<CmLiftRelation> Relations { get; }

		public List<CmLiftNote> Notes { get; }

		public List<CmLiftExample> Examples { get; }

		public List<CmLiftReversal> Reversals { get; }

		public List<LiftUrlRef> Illustrations { get; }

		public List<CmLiftSense> Subsenses { get; }

		public LiftObject Owner { get; }

		public CmLiftEntry OwningEntry
		{
			get
			{
				LiftObject owner;
				for (owner = Owner; owner is CmLiftSense; owner = (owner as CmLiftSense).Owner)
				{
				}
				return owner as CmLiftEntry;
			}
		}

		public override string XmlTag => "sense/subsense";
	}
}