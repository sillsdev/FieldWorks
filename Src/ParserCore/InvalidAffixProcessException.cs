// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	internal class InvalidAffixProcessException : Exception
	{
		internal InvalidAffixProcessException(IMoAffixProcess affixProcess, bool invalidLhs)
		{
			AffixProcess = affixProcess;
			IsInvalidLhs = invalidLhs;
		}

		internal IMoAffixProcess AffixProcess { get; }

		internal bool IsInvalidLhs { get; }
	}
}