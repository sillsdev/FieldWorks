using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	public class UnicodeCharacterEditingHelperTests
	{
		[Test]
		public void TextSuffixReturnsSame()
		{
			var errorMessage = "should not have changed an input that did not end in numbers";
			string input = "xyz";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			input = "111jabcj2222xyz";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			input = null;
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			// Ends in space
			input = "1234 ";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			input = "";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
		}

		[Test]
		public void EndingInNumberGivesConversion()
		{
			var errorMessage = "should have converted ending numbers to unicode character";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("1234"), Is.EqualTo("\u1234"), "should have converted to unicode character");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("222j333"), Is.EqualTo("222j\u0333"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("j333"), Is.EqualTo("j\u0333"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("j1234"), Is.EqualTo("j\u1234"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("222j1234"), Is.EqualTo("222j\u1234"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jabc"), Is.EqualTo("111j\u0abc"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("BbzcCx111jAbC"), Is.EqualTo("BbzcCx111j\u0abc"), "Should have handled mixed-case hex digits");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("11\n1jabc"), Is.EqualTo("11\n1j\u0abc"),"Should have worked even with a newline character in input");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("FFFF"), Is.EqualTo("\uFFFF"), "should have supported high numbers");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jedcb"), Is.EqualTo("111j\uedcb"), "should have supported high numbers");
		}

		[Test]
		public void EndingInHexDigitsForSurrogateDoesNotOperate()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jD834"), Is.EqualTo("111jD834"), "Don't operate on surrogates");
		}

		[Test]
		public void ManyDigitsAtEndOnlyAccountsForLastFour()
		{
			var errorMessage = "Only handle last 4 digits";
			// Only supporting 4 digits presently
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111j12345"), Is.EqualTo("111j1\u2345"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jaaaaa"), Is.EqualTo("111ja\uaaaa"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jaaaaaa"), Is.EqualTo("111jaa\uaaaa"), errorMessage);
		}

		[Test]
		public void AllowForUPlusNotation()
		{
			var errorMessage = "Handle U+ or u+ notation";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jU+1234"), Is.EqualTo("111j\u1234"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("u+1234"), Is.EqualTo("\u1234"), errorMessage);
		}

		[Test]
		public void CharacterToNumbersConvertsFinal()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("A"), Is.EqualTo("0041"), "should have converted final character to four hex-digit unicode representation");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA"), Is.EqualTo("A0041"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("1234A"), Is.EqualTo("12340041"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("\u1234"), Is.EqualTo("1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA\u1234"), Is.EqualTo("AA1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA5555"), Is.EqualTo("AA5550035"), "Should have converted final character to four hex-digit representation");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AB\nBA"), Is.EqualTo("AB\nB0041"), "Should have worked even with a newline character in input");
		}

		[Test]
		public void CharacterToNumbersDoesNotOperateOnSurrogates()
		{
			string input;
			input = "\uD834\uDD1E";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input), "Don't operate on surrogates");
			input = "\U0001D11E";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input),"Don't operate on the resulting surrogates");
		}

		[Test]
		public void CharacterToNumbersIsNoopForEmptyOrNullInput()
		{
			string input = "";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input), "Don't change if empty string");
			input = null;
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input), "Don't change if null");
		}

		[Test]
		public void ConvertEitherWay()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinal("Some text here1234"), Is.EqualTo("Some text here\u1234"), "Should have detected final numbers and given a unicode character");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinal("Some text here!"), Is.EqualTo("Some text here0021"), "Should have detected final non-number character and given four hex digits representing it");
		}
	}
}
