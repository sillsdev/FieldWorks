using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This is our wrapper for Hunspell, currently. It hides everything we don't use in case we want to change again!
	/// </summary>
	public interface ISpellEngine : ICheckWord
	{
		/// <summary>
		/// Controls whether the word is considered correct or not.
		/// </summary>
		/// <param name="word"></param>
		/// <param name="isCorrect"></param>
		void SetStatus(string word, bool isCorrect);

		/// <summary>
		/// Get a list of suggestions for alternate words to use in place of the mis-spelled one.
		/// </summary>
		/// <param name="badWord"></param>
		/// <returns></returns>
		ICollection<string> Suggest(string badWord);

		/// <summary>
		/// A dictionary is considered vernacular if it is one we created and can rewrite if we choose.
		/// It should not be used for any dictionary ID that is not an exact match.
		/// </summary>
		bool IsVernacular { get; }
	}
}