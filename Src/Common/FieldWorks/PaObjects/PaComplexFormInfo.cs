using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaComplexFormInfo : IPaComplexFormInfo
	{
		/// ------------------------------------------------------------------------------------
		public PaComplexFormInfo()
		{
			xComponents = new List<string>();
		}

		/// ------------------------------------------------------------------------------------
		internal static PaComplexFormInfo Create(ILexEntryRef lxEntryRef)
		{
			if (lxEntryRef.RefType != LexEntryRefTags.krtComplexForm)
				return null;

			var pcfi = new PaComplexFormInfo();
			pcfi.xComplexFormComment = PaMultiString.Create(lxEntryRef.Summary, lxEntryRef.Cache.ServiceLocator);
			pcfi.xComplexFormType = lxEntryRef.ComplexEntryTypesRS.Select(x => PaCmPossibility.Create(x)).ToList();

			foreach (var component in lxEntryRef.ComponentLexemesRS)
			{
				if (component is ILexEntry)
					pcfi.xComponents.Add(((ILexEntry)component).HeadWord.Text);
				else if (component is ILexSense)
				{
					var lxSense = (ILexSense)component;
					var text = lxSense.Entry.HeadWord.Text;
					if (lxSense.Entry.SensesOS.Count > 1)
						text += string.Format(" {0}", lxSense.IndexInOwner + 1);

					pcfi.xComponents.Add(text);
				}
			}

			return pcfi;
		}

		/// ------------------------------------------------------------------------------------
		public List<string> xComponents { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public List<string> Components
		{
			get { return xComponents; }
		}

		/// ------------------------------------------------------------------------------------
		public PaMultiString xComplexFormComment { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IPaMultiString ComplexFormComment
		{
			get { return xComplexFormComment; }
		}

		/// ------------------------------------------------------------------------------------
		public List<PaCmPossibility> xComplexFormType { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<IPaCmPossibility> ComplexFormType
		{
			get { return xComplexFormType.Cast<IPaCmPossibility>(); }
		}
	}
}
