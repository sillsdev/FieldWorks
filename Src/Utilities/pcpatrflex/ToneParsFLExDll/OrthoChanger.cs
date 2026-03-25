// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.ToneParsFLEx
{
	public class OrthoChanger
	{
		List<OrthoChangeMapping> mappings = new List<OrthoChangeMapping>();
		const char kComment = '|';
		const char doubleQuote = '"';
		const char singleQuote = '\'';

		public string OrthoFileContents { get; set; } = "";
		public bool ChangesExist { get; set; }

		public OrthoChanger() { }

		public void LoadOrthoChangesFile(string inputOrthoChangeFile)
		{
			if (!File.Exists(inputOrthoChangeFile))
			{
				ChangesExist = false;
			}
			else
			{
				OrthoFileContents = File.ReadAllText(inputOrthoChangeFile, Encoding.UTF8);
				ChangesExist = true;
			}
		}

		public List<OrthoChangeMapping> CreateOrthoChanges()
		{
			mappings.Clear();
			int chIndex = FindFirstChIndex(OrthoFileContents);
			int finalIndex = chIndex;
			while (finalIndex > -1)
			{
				var mapping = CreateOrthoChangeMapping(OrthoFileContents, chIndex, out finalIndex);
				if (finalIndex > -1)
				{
					mappings.Add(mapping);
					chIndex =
						FindFirstChIndex(OrthoFileContents.Substring(finalIndex)) + finalIndex;
				}
			}
			if (mappings.Count == 0)
			{
				ChangesExist = false;
			}
			return mappings;
		}

		public OrthoChangeMapping CreateOrthoChangeMapping(
			string chString,
			int index,
			out int finalIndex
		)
		{
			OrthoChangeMapping mapping = new OrthoChangeMapping();
			char quoteToMatch = doubleQuote;
			var state = State.BEGIN;
			int fromStart = -1;
			int toStart = -1;
			State lastState = State.BEGIN;
			finalIndex = -1;
			while (index < chString.Length)
			{
				if (chString[index] == kComment && state != State.COMMENT)
				{
					lastState = state;
					state = State.COMMENT;
				}
				switch (state)
				{
					case State.BEGIN:
						if (chString[index] == doubleQuote || chString[index] == singleQuote)
						{
							quoteToMatch = chString[index];
							state = State.QUOTE1;
						}
						break;
					case State.QUOTE1:
						fromStart = index;
						state = State.QUOTE2;
						break;
					case State.QUOTE2:
						if (chString[index] == quoteToMatch)
						{
							mapping.From = chString.Substring(fromStart, index - fromStart);
							state = State.BETWEEN;
						}
						break;
					case State.BETWEEN:
						if (chString[index] == doubleQuote || chString[index] == singleQuote)
						{
							quoteToMatch = chString[index];
							state = State.QUOTE3;
						}
						break;
					case State.QUOTE3:
						toStart = index;
						if (chString[index] == quoteToMatch)
						{
							// case where the to mapping is empty
							index--;
						}
						state = State.QUOTE4;
						break;
					case State.QUOTE4:
						if (chString[index] == quoteToMatch)
						{
							mapping.To = chString.Substring(toStart, index - toStart);
							state = State.PROCESS;
						}
						break;
					case State.COMMENT:
						if (index < chString.Length && chString[index] == '\n')
						{
							state = lastState;
						}
						break;
					case State.PROCESS:
						finalIndex = index;
						index = chString.Length;
						state = State.END;
						break;
				}
				index++;
			}
			if (state == State.PROCESS)
			{
				// can occur when the fourth quote is at the end of the input string
				finalIndex = index;
			}
			return mapping;
		}

		public int FindFirstChIndex(string contents)
		{
			int chIndex = contents.IndexOf("\\ch ");
			if (chIndex != 0)
			{
				chIndex = contents.IndexOf("\n\\ch ");
				if (chIndex > -1)
				{
					chIndex++;
				}
			}
			return chIndex;
		}

		public string ApplyChangesToWord(string word)
		{
			if (String.IsNullOrEmpty(word))
				return "";
			foreach (OrthoChangeMapping mapping in mappings)
			{
				word = word.Replace(mapping.From, mapping.To);
			}
			return word;
		}

		enum State
		{
			BEGIN,
			QUOTE1,
			QUOTE2,
			BETWEEN,
			QUOTE3,
			QUOTE4,
			COMMENT,
			PROCESS,
			END
		}
	}
}
