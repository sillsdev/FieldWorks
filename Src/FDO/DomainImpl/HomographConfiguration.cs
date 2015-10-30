// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary>
	/// This class stores various settings related to how homograph numbers (and sense numbers) are displayed.
	/// </summary>
	public class HomographConfiguration
	{
		/// <summary>
		/// Various places we can choose to show or not show homograph numbers and sense numbers.
		/// </summary>
		public enum HeadwordVariant
		{
			/// <summary>
			/// The default, in the UI and at the start of each entry in the dictionary
			/// </summary>
			Main,
			/// <summary>
			/// Cross-references in teh Dictionary
			/// </summary>
			DictionaryCrossRef,
			/// <summary>
			/// Cross-references in the Reversal index.
			/// </summary>
			ReversalCrossRef // Keep as last item.
		}

		private bool[] m_showHomographVariants = new bool[(int)HeadwordVariant.ReversalCrossRef + 1];
		/// <summary>
		/// Make one.
		/// </summary>
		public HomographConfiguration()
		{
			RestoreDefaults();
		}

		private void RestoreDefaults()
		{
			for (int i = 0; i < m_showHomographVariants.Length; i++)
				m_showHomographVariants[i] = true;
			ShowSenseNumberRef = true;
			ShowSenseNumberReversal = true;
			HomographNumberBefore = false;
		}

		/// <summary>
		/// The style used for homograph numbers
		/// </summary>
		public const string ksHomographNumberStyle = "Homograph-Number";
		/// <summary>
		/// The style used for sense numbers in cross references.
		/// </summary>
		public const string ksSenseReferenceNumberStyle = "Sense-Reference-Number";

		/// <summary>
		/// True to display homograph numbers before the headword; false (default) to display after.
		/// This is a global setting that affects all varieties of headword.
		/// </summary>
		public bool HomographNumberBefore { get; set; }

		/// <summary>
		/// True to display homograph numbers in the default headword method used as the header of an entry in the dictionary
		/// and in the program UI. Note that we can only show them in cross-refs if we show them in the main headword.
		/// (One reason for this is the design of the dialog, which does not allow to specify before/after if we choose 'hide'.)
		/// </summary>
		public bool ShowHomographNumber(HeadwordVariant hv)
		{
			return m_showHomographVariants[(int) hv] && m_showHomographVariants[(int)HeadwordVariant.Main];
		}

		/// <summary>
		/// Set whether homograph numbers are displayed for the specified variant.
		/// </summary>
		public void SetShowHomographNumber(HeadwordVariant hv, bool val)
		{
			m_showHomographVariants[(int)hv] = val;
		}

		/// <summary>
		/// Controls whether OwnerOutlineName includes sense number (but only if homograph number is shown).
		/// This appears in dictionary cross-refs. It corresponds to HeadwordVariant.DictionaryCrossRef,
		/// but there is no "Main" case for Sense cross-refs.
		/// </summary>
		public bool ShowSenseNumberRef { get; set; }
		/// <summary>
		/// Controls whether OwnerOutlineName includes sense number (but only if homograph number is shown).
		/// This appears in reversal cross-refs. It corresponds to HeadwordVariant.ReversalCrossRef,
		/// but there is no "Main" case for Sense cross-refs.
		/// </summary>
		public bool ShowSenseNumberReversal { get; set; }

		/// <summary>
		/// Provides a convenient way to get the appropriate ShowSenseNumber flag. Passing "main" doesn't
		/// make sense.
		/// This also implements the constraint that we don't show sense number when homograph number is suppressed.
		/// </summary>
		public bool ShowSenseNumber(HeadwordVariant hv)
		{
			if (!ShowHomographNumber(hv))
				return false;
			if (hv == HeadwordVariant.ReversalCrossRef)
				return ShowSenseNumberReversal;
			return ShowSenseNumberRef;
		}

		/// <summary>
		/// Get/Set a representation of state suitable for persistence
		/// </summary>
		public string PersistData
		{
			get
			{
				var builder = new StringBuilder();
				if (ShowHomographNumber(HeadwordVariant.Main))
				{
					if (HomographNumberBefore)
						builder.Append("before ");
					if (!ShowHomographNumber(HeadwordVariant.DictionaryCrossRef))
					{
						builder.Append("hn:dcr ");
					}
					if (!ShowHomographNumber(HeadwordVariant.ReversalCrossRef))
					{
						builder.Append("hn:rcr ");
					}
					if (!ShowSenseNumberRef)
					{
						builder.Append("snRef ");
					}
					if (!ShowSenseNumberReversal)
					{
						builder.Append("snRev ");
					}
				}
				else
				{
					builder.Append("hide");
				}

				return builder.ToString();
			}

			set
			{
				RestoreDefaults();
				foreach (var item in value.Split(' '))
				{
					switch(item)
					{
						case "before":
							HomographNumberBefore = true;
							break;
						case "hide":
							SetShowHomographNumber(HeadwordVariant.Main, false);
							break;
						case "snRef":
							ShowSenseNumberRef = false;
							break;
						case "snRev":
							ShowSenseNumberReversal= false;
							break;
						case "hn:dcr":
							SetShowHomographNumber(HeadwordVariant.DictionaryCrossRef, false);
							break;
						case "hn:rcr":
							SetShowHomographNumber(HeadwordVariant.ReversalCrossRef, false);
							break;
					}
				}
			}
		}
	}
}
