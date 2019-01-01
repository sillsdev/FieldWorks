// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matchers are able to tell whether a string matches a pattern.
	/// </summary>
	public interface IMatcher
	{
		/// <summary />
		bool Accept(ITsString tssKey);
		/// <summary />
		bool Matches(ITsString arg);
		/// <summary />
		bool SameMatcher(IMatcher other);
		/// <summary />
		bool IsValid();
		/// <summary />
		string ErrorMessage();
		/// <summary />
		bool CanMakeValid();
		/// <summary />
		ITsString MakeValid();
		/// <summary />
		ITsString Label { get; set; }
		/// <summary />
		ILgWritingSystemFactory WritingSystemFactory { get; set; }
		/// <summary>
		/// If there is one specific writing system that the matcher looks for, return it;
		/// otherwise return 0.
		/// </summary>
		int WritingSystem { get; }
	}
}