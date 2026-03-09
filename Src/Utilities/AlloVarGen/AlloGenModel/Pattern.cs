// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SIL.AlloGenModel
{
	public class Pattern
	{
		public Matcher Matcher { get; set; } = new Matcher();
		public List<MorphType> MorphTypes { get; set; }
		public Category Category { get; set; } = new Category();

		public Pattern()
		{
			MorphTypes = new List<MorphType>();
		}

		public void SetDefaultMorphTypes()
		{
			MorphType mtBoundRoot = CreateMorphType(
				"bound root",
				"d7f713e4-e8cf-11d3-9764-00c04f186933"
			);
			MorphTypes.Add(mtBoundRoot);
			MorphType mtBoundStem = CreateMorphType(
				"bound stem",
				"d7f713e7-e8cf-11d3-9764-00c04f186933"
			);
			MorphTypes.Add(mtBoundStem);
			MorphType mtRoot = CreateMorphType("root", "d7f713e5-e8cf-11d3-9764-00c04f186933");
			MorphTypes.Add(mtRoot);
			MorphType mtStem = CreateMorphType("stem", "d7f713e8-e8cf-11d3-9764-00c04f186933");
			MorphTypes.Add(mtStem);
		}

		private MorphType CreateMorphType(string name, string guid)
		{
			MorphType morphType = new MorphType();
			morphType.Name = name;
			morphType.Guid = guid;
			morphType.Active = true;
			return morphType;
		}

		public Pattern Duplicate()
		{
			var newPattern = new Pattern();
			newPattern.Matcher = Matcher.Duplicate();
			var newCat = new Category();
			newCat.Active = Category.Active;
			newCat.Guid = Category.Guid;
			newCat.Name = Category.Name;
			newPattern.Category = newCat;
			var newMTs = new List<MorphType>();
			foreach (MorphType mt in MorphTypes)
			{
				var newMT = new MorphType();
				newMT.Active = mt.Active;
				newMT.Guid = mt.Guid;
				newMT.Name = mt.Name;
				newMTs.Add(newMT);
			}
			newPattern.MorphTypes = newMTs;
			return newPattern;
		}

		public override bool Equals(Object obj)
		{
			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
			{
				return false;
			}
			else
			{
				Pattern pat = (Pattern)obj;
				return (Matcher.Equals(pat.Matcher))
					&& (MorphTypes.SequenceEqual(pat.MorphTypes))
					&& (Category.Equals(pat.Category));
			}
		}

		public override int GetHashCode()
		{
			return Tuple.Create(Matcher, MorphTypes, Category).GetHashCode();
		}
	}
}
