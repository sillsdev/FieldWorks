// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChkRefMatcher.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for matching up ChkRef objects created from the old key terms list with those
	/// created from the new list (see TE-6216)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ChkRefMatcher
	{
		static Dictionary<int, string> s_table;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Static constructor for the <see cref="ChkRefMatcher"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static ChkRefMatcher()
		{
			s_table = new Dictionary<int, string>();

			s_table[902] = s_table[913] = s_table[7944] = s_table[7946] = s_table[7948] = s_table[7950] =
				s_table[8120] = s_table[8121] = s_table[8122] = s_table[8123] = "A";

			s_table[7945] = s_table[7947] = s_table[7949] = s_table[7951] = "Ha";

			s_table[940] = s_table[945] = s_table[7936] = s_table[7938] = s_table[7940] = s_table[7942] =
				s_table[8048] = s_table[8049] = s_table[8112] = s_table[8113] = s_table[8118] = "a";

			s_table[7937] = s_table[7939] = s_table[7941] = s_table[7943] = "ha";

			s_table[8072] = s_table[8074] = s_table[8076] = s_table[8078] = s_table[8124] = "Ai";

			s_table[8073] = s_table[8075] = s_table[8077] = s_table[8079] = "Hai";

			s_table[8064] = s_table[8066] = s_table[8068] = s_table[8070] = s_table[8114] =
				s_table[8115] = s_table[8116] = s_table[8119] = "ai";

			s_table[8065] = s_table[8067] = s_table[8069] = s_table[8071] = "hai";

			s_table[914] = "B";

			s_table[946] = "b";

			s_table[915] = "G";

			s_table[947] = "g";

			s_table[916] = "D";

			s_table[948] = "d";

			s_table[904] = s_table[917] = s_table[7960] = s_table[7962] = s_table[7964] = s_table[8136] =
				s_table[8137] = "E";

			s_table[7961] = s_table[7963] = s_table[7965] = "He";

			s_table[941] = s_table[949] = s_table[7952] = s_table[7954] = s_table[7956] = s_table[8050] =
				s_table[8051] = "e";

			s_table[7953] = s_table[7955] = s_table[7957] = "he";

			s_table[918] = "Z";

			s_table[950] = "z";

			s_table[905] = s_table[919] = s_table[7976] = s_table[7978] = s_table[7980] = s_table[7982] =
				s_table[8138] = s_table[8139] = "J";

			s_table[7977] = s_table[7979] = s_table[7981] = s_table[7983] = "Hj";

			s_table[942] = s_table[951] = s_table[7968] = s_table[7970] = s_table[7972] = s_table[7974] =
				s_table[8052] = s_table[8053] = s_table[8134] = "j";

			s_table[7969] = s_table[7971] = s_table[7973] = s_table[7975] = "hj";

			s_table[8088] = s_table[8090] = s_table[8092] = s_table[8094] = s_table[8140] = "Ji";

			s_table[8089] = s_table[8091] = s_table[8093] = s_table[8095] = "Hji";

			s_table[8080] = s_table[8082] = s_table[8084] = s_table[8086] = s_table[8130] =
				s_table[8131] = s_table[8132] = s_table[8135] = "ji";

			s_table[8081] = s_table[8083] = s_table[8085] = s_table[8087] = "hji";

			s_table[920] = "Th";

			s_table[952] = "th";

			s_table[906] = s_table[921] = s_table[7992] = s_table[7994] = s_table[7996] = s_table[7998] =
				s_table[8152] = s_table[8153] = s_table[8154] = s_table[8155] = "I";

			s_table[7993] = s_table[7995] = s_table[7997] = s_table[7999] = "Hi";

			s_table[912] = s_table[943] = s_table[953] = s_table[970] = s_table[7984] = s_table[7986] = s_table[7988] =
				s_table[7990] = s_table[8054] = s_table[8055] = s_table[8144] = s_table[8145] =
				s_table[8146] = s_table[8147] = s_table[8150] = s_table[8151] = "i";

			s_table[7985] = s_table[7987] = s_table[7989] = s_table[7991] = "hi";

			s_table[922] = "K";

			s_table[954] = "k";

			s_table[923] = "L";

			s_table[955] = "l";

			s_table[924] = "M";

			s_table[956] = "m";

			s_table[925] = "N";

			s_table[957] = "n";

			s_table[926] = "X";

			s_table[958] = "x";

			s_table[908] = s_table[927] = s_table[8008] = s_table[8010] = s_table[8012] = s_table[8184] =
				s_table[8185] = "O";

			s_table[8009] = s_table[8011] = s_table[8013] = "Ho";

			s_table[959] = s_table[972] = s_table[8000] = s_table[8002] = s_table[8004] = s_table[8056] =
				s_table[8057] = "o";

			s_table[8001] = s_table[8003] = s_table[8005] = "ho";

			s_table[928] = "P";

			s_table[960] = "p";

			s_table[929] = s_table[8172] = "R";

			s_table[961] = s_table[8164] = s_table[8165] = "r";

			s_table[931] = "S";

			s_table[962] = s_table[963] = "s";

			s_table[932] = "T";

			s_table[964] = "t";

			s_table[910] = s_table[933] = s_table[8168] = s_table[8169] = s_table[8170] = s_table[8171] = "U";

			s_table[8025] = s_table[8027] = s_table[8029] = s_table[8031] = "Hu";

			s_table[944] = s_table[965] = s_table[971] = s_table[973] = s_table[8016] = s_table[8018] = s_table[8020] =
				s_table[8022] = s_table[8058] = s_table[8059] = s_table[8160] = s_table[8162] =
				s_table[8163] = s_table[8166] = s_table[8167] = "u";

			s_table[8017] = s_table[8019] = s_table[8021] = s_table[8023] = "hu";

			s_table[934] = "Ph";

			s_table[966] = "ph";

			s_table[935] = "Ch";

			s_table[967] = "ch";

			s_table[936] = "Ps";

			s_table[968] = "ps";

			s_table[911] = s_table[937] = s_table[8040] = s_table[8042] = s_table[8044] = s_table[8046] =
				s_table[8186] = s_table[8187] = "W";

			s_table[8041] = s_table[8043] = s_table[8045] = s_table[8047] = "Hw";

			s_table[969] = s_table[974] = s_table[8032] = s_table[8034] = s_table[8036] = s_table[8038] =
				s_table[8060] = s_table[8061] = s_table[8182] = "w";

			s_table[8033] = s_table[8035] = s_table[8037] = s_table[8039] = "hw";

			s_table[8104] = s_table[8106] = s_table[8108] = s_table[8110] = s_table[8188] = "Wi";

			s_table[8105] = s_table[8107] = s_table[8109] = s_table[8111] = "Hwi";

			s_table[8096] = s_table[8098] = s_table[8100] = s_table[8102] = s_table[8178] =
				s_table[8179] = s_table[8180] = s_table[8183] = "wi";

			s_table[8097] = s_table[8099] = s_table[8101] = s_table[8103] = "hwi";

			s_table[8125] =
			s_table[32] = s_table[40] = s_table[41] = s_table[43] = s_table[44] = s_table[46] = " ";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Transliterates the specified greek string using the algorithm used in the old
		/// Key Terms list.
		/// </summary>
		/// <param name="greek">The greek string (Unicode).</param>
		/// <returns>A (lossy) romanized transliteration</returns>
		/// ------------------------------------------------------------------------------------
		public static string Transliterate(string greek)
		{

			StringBuilder bldr = new StringBuilder();
			foreach (char chr in greek)
			{
				try
				{
					bldr.Append(s_table[(int)chr]);
				}
				catch
				{
					bldr.Append("?");
				}
			}
			return bldr.ToString();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the ChkRefs in the new list that correspond to the given ChkRef in the old
		/// list.
		/// </summary>
		/// <param name="possibilty">A CmPossibility whose SubPossibilities are key terms
		/// in the new list.</param>
		/// <param name="chkRefOld">The old ChkRef.</param>
		/// <returns>A list of IChkRef containing the items corresponding to the given ChkRef
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static List<IChkRef> FindCorrespondingChkRefs(ICmPossibility possibilty,
			IChkRef chkRefOld)
		{
			// TODO (TE-2901): This current implementation is specific to the translition from
			// the old-style list to the new one (see TE-6216). This needs to be carefully
			// enhanced to allow for conversion between different versions that use the new
			// approach (with IDs that should provide for easier correlation).
			List<IChkRef> list = new List<IChkRef>();
			string sTargetWord = chkRefOld.KeyWord.Text.Normalize(NormalizationForm.FormC);
			foreach (IChkTerm term in possibilty.SubPossibilitiesOS)
			{
				foreach (IChkRef chkRef in term.OccurrencesOS)
				{
					string chkRefKeyWord = chkRef.KeyWord.Text.Normalize(NormalizationForm.FormC);
					if (chkRef.Ref == chkRefOld.Ref &&
						Transliterate(chkRefKeyWord) == sTargetWord)
					{
						list.Add(chkRef);
					}
				}
				if (term.SubPossibilitiesOS.Count > 0)
				{
					list.AddRange(FindCorrespondingChkRefs(term, chkRefOld));
				}
			}
			return list;
		}
	}
}