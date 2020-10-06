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
	/// This class implements "sense" from the LIFT standard.
	/// It corresponds to LexSense from the FieldWorks model.
	/// </summary>
	internal sealed class CmLiftSense : LiftObject
	{
		private CmLiftSense()
		{
			Subsenses = new List<CmLiftSense>();
			Illustrations = new List<LiftUrlRef>();
			Reversals = new List<CmLiftReversal>();
			Examples = new List<CmLiftExample>();
			Notes = new List<CmLiftNote>();
			Relations = new List<CmLiftRelation>();
		}

		internal CmLiftSense(Extensible info, Guid guid, LiftObject owner, FlexLiftMerger merger)
			: this()
		{
			Id = info.Id;
			Guid = guid;
			CmObject = guid == Guid.Empty ? null : merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			Owner = owner;
		}

		public override string XmlTag => "sense/subsense";

		internal ICmObject CmObject { get; set; }

		internal int Order { get; set; }

		internal LiftGrammaticalInfo GramInfo { get; set; }

		internal LiftMultiText Gloss { get; set; }  // safe-XML

		internal LiftMultiText Definition { get; set; } // safe-XML

		internal List<CmLiftRelation> Relations { get; }

		internal List<CmLiftNote> Notes { get; }

		internal List<CmLiftExample> Examples { get; }

		internal List<CmLiftReversal> Reversals { get; }

		internal List<LiftUrlRef> Illustrations { get; }

		internal List<CmLiftSense> Subsenses { get; }

		internal LiftObject Owner { get; }

		internal CmLiftEntry OwningEntry
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
	}
}