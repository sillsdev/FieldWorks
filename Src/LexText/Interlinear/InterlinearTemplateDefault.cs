// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// The template a project starts with (and can restore via "Replace with Default LaTeX"),
	/// reproducing the format validated in LT-22645's gb4e export -- trailing "\\" on both gll
	/// lines, one word/morpheme per token -- re-expressed with LT-22712's closed placeholders.
	/// </summary>
	public static class InterlinearTemplateDefault
	{
		public const string Text =
@"\ea
{{words_source}} \\
\gll {{morphemes_source}} \\
{{gloss}} \\
\glt {{free_translation}}
\z
" + InterlinearTemplateHelpText.Sentinel + @"
" + InterlinearTemplateHelpTextBody.Body;
	}

	/// <summary>Kept separate from InterlinearTemplateDefault so the help text can be reused
	/// (e.g. appended when a user starts a template from scratch) without the surrounding example
	/// block.</summary>
	public static class InterlinearTemplateHelpTextBody
	{
		public const string Body =
@"%
% This block is never exported -- it only exists to explain the fields
% above to whoever edits this template next.
%
% Everything above the ""Template help"" line is real LaTeX, resolved once
% per interlinear example and copied into the exported .tex file. Anywhere
% you see a {{name}} placeholder, the export fills in that example's data;
% everything else (including \ea, \gll, \glt, \z, and the trailing \\ line
% breaks) is static text you can rearrange or restyle freely.
%
% Placeholders, grouped by what they align with:
%
%   {{words_source}}            -- always required. The sentence's words,
%                                   one token per word, in the project's
%                                   main vernacular writing system.
%   {{words_ipa}}                -- optional. Same words, in the IPA
%                                   writing system, aligned 1:1 with
%                                   {{words_source}} if both are present.
%   {{words_transliteration}}    -- optional. Same words, in a secondary
%                                   (e.g. romanized) vernacular writing
%                                   system, aligned the same way.
%
%   {{morphemes_source}}         -- one of these three is always required.
%   {{morphemes_ipa}}               Each is the sentence broken into
%   {{morphemes_transliteration}}   morphemes, in one of the three
%                                   writing systems above.
%   {{gloss}}                    -- always required. The lexical gloss for
%                                   each morpheme, aligned 1:1 with
%                                   whichever morphemes_* line(s) you use.
%
%   {{free_translation}}         -- optional. The example's free
%   {{literal_translation}}         translation / literal translation /
%   {{note}}                        note, each its own line if used.
%
% {{gloss}}, {{free_translation}}, {{literal_translation}}, and {{note}}
% all resolve against whichever analysis language you pick when you run
% the export -- the same template produces an English or a French
% publication without editing the project.
%
% A line containing a placeholder that has no data for a given example
% (e.g. {{note}} when that sentence has no note) is dropped entirely for
% that example, rather than leaving a stray empty line behind.";
	}
}
