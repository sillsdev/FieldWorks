// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.LCModel;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaComplexFormInfo : IPaComplexFormInfo
	{
		/// <summary />
		public PaComplexFormInfo(IPaMultiString complexFormComment, IEnumerable<IPaCmPossibility> complexFormType)
		{
			Components = new List<string>();
			ComplexFormComment = complexFormComment;
			ComplexFormType = complexFormType;
		}

		/// <summary />
		internal static PaComplexFormInfo Create(ILexEntryRef lxEntryRef)
		{
			if (lxEntryRef.RefType != LexEntryRefTags.krtComplexForm)
			{
				return null;
			}

			var pcfi = new PaComplexFormInfo(PaMultiString.Create(lxEntryRef.Summary, lxEntryRef.Cache.ServiceLocator), lxEntryRef.ComplexEntryTypesRS.Select(x => PaCmPossibility.Create(x)));
			foreach (var component in lxEntryRef.ComponentLexemesRS)
			{
				switch (component)
				{
					case ILexEntry entry:
						pcfi.Components.Add(entry.HeadWord.Text);
						break;
					case ILexSense sense:
					{
						var text = sense.Entry.HeadWord.Text;
						if (sense.Entry.SensesOS.Count > 1)
						{
							text += $" {sense.IndexInOwner + 1}";
						}
						pcfi.Components.Add(text);
						break;
					}
				}
			}

			return pcfi;
		}

		/// <inheritdoc />
		[XmlIgnore]
		public List<string> Components { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IPaMultiString ComplexFormComment { get; }

		/// <inheritdoc />
		[XmlIgnore]
		public IEnumerable<IPaCmPossibility> ComplexFormType { get; }
	}
}
