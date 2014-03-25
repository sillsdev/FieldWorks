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
			string input = "xyz";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), "should not have changed an input string that did not end in numbers");
			input = "111jabcj2222xyz";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), "should not have changed an input string that did not end in numbers");
			input = null;
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), "should not have changed an input that did not end in numbers");
			// Ends in space
			input = "1234 ";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input));
		}

		[Test]
		public void EndingInNumberGivesConversion()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("1234"), Is.EqualTo("\u1234"), "should have converted to unicode character");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("222j333"), Is.EqualTo("222j\u0333"), "should have converted ending numbers to unicode character");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("j333"), Is.EqualTo("j\u0333"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("j1234"), Is.EqualTo("j\u1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("222j1234"), Is.EqualTo("222j\u1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(""), Is.EqualTo(""));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jabc"), Is.EqualTo("111j\u0abc"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("BbzcCx111jAbC"), Is.EqualTo("BbzcCx111j\u0abc"));
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
			// Only supporting 4 digits presently
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111j12345"), Is.EqualTo("111j1\u2345"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jaaaaa"), Is.EqualTo("111ja\uaaaa"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jaaaaaa"), Is.EqualTo("111jaa\uaaaa"));
		}

		[Test]
		public void AllowForUPlusNotation()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jU+1234"), Is.EqualTo("111j\u1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("u+1234"), Is.EqualTo("\u1234"));
		}

		[Test]
		public void CharacterToNumbers()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("A"), Is.EqualTo("0041"), "should have converted final character to four hex-digit unicode representation");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(""), Is.EqualTo(""));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA"), Is.EqualTo("A0041"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("1234A"), Is.EqualTo("12340041"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("\u1234"), Is.EqualTo("1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA\u1234"), Is.EqualTo("AA1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA5555"), Is.EqualTo("AA5550035"), "Should have converted final character to four hex-digit representation");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AB\nBA"), Is.EqualTo("AB\nB0041"), "Should have worked even with a newline character in input");

			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("\uD834\uDD1E"), Is.EqualTo("\uD834\uDD1E"), "Don't operate on surrogates");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("\U0001D11E"), Is.EqualTo("\U0001D11E"),"Don't operate on the resulting surrogates");
		}

		[Test]
		public void ConvertEitherWay()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinal("Some text here1234"), Is.EqualTo("Some text here\u1234"), "Should have detected final numbers and given a unicode character");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinal("Some text here!"), Is.EqualTo("Some text here0021"), "Should have detected final non-number character and given four hex digits representing it");
		}
	}
}
