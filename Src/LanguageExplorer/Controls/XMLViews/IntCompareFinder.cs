// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using System.Xml.Linq;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Filters;
using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// IntCompareFinder is an implementation of StringFinder that modifies the sort
	/// string by adding leading zeros to pad it to ten digits.
	/// </summary>
	internal class IntCompareFinder : LayoutFinder
	{
		/// <summary>
		/// normal constructor.
		/// </summary>
		public IntCompareFinder(LcmCache cache, string layoutName, XElement colSpec, IApp app)
			: base(cache, layoutName, colSpec, app)
		{
		}

		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public IntCompareFinder()
		{
		}

		#region StringFinder Members

		const int maxDigits = 10; // Int32.MaxValue.ToString().Length;, but that is not 'const'!

		/// <summary>
		/// Get a key from the item for sorting. Add enough leading zeros so string comparison
		/// works.
		///
		/// Collator sorting generally ignores the minus sign as being a hyphen.  So we have
		/// to be tricky handling negative numbers.  Nine's complement with an inverted sign
		/// digit should do the trick...
		/// </summary>
		public override string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			var baseResult = base.SortStrings(item, sortedFromEnd);
			if (sortedFromEnd)
			{
				return baseResult; // what on earth would it mean??
			}

			if (baseResult.Length != 1)
			{
				return baseResult;
			}
			var sVal = baseResult[0];
			if (sVal.Length == 0)
			{
				return new[] { "9" + new string('0', maxDigits) };
			}
			string prefix;
			char chFiller;
			if (sVal[0] == '-')
			{
				sVal = NinesComplement(sVal.Substring(1));
				prefix = "0";	// negative numbers come first.
				chFiller = '9';
			}
			else
			{
				prefix = "9";	// positive numbers come later.
				chFiller = '0';
			}

			return sVal.Length == maxDigits ? new[] { prefix + sVal } : new[] { prefix + new string(chFiller, maxDigits - sVal.Length) + sVal };
		}

		private string NinesComplement(string sNumber)
		{
			var bldr = new StringBuilder();
			while (sNumber.Length > 0)
			{
				switch (sNumber[0])
				{
					case '0': bldr.Append('9'); break;
					case '1': bldr.Append('8'); break;
					case '2': bldr.Append('7'); break;
					case '3': bldr.Append('6'); break;
					case '4': bldr.Append('5'); break;
					case '5': bldr.Append('4'); break;
					case '6': bldr.Append('3'); break;
					case '7': bldr.Append('2'); break;
					case '8': bldr.Append('1'); break;
					case '9': bldr.Append('0'); break;
					default:
						throw new Exception("Invalid character found in supposed integer string!");
				}
				sNumber = sNumber.Substring(1);
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			return other is IntCompareFinder && base.SameFinder(other);
		}

		#endregion
	}
}