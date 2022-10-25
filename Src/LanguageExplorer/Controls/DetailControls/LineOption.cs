// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class LineOption : IEquatable<LineOption>
	{
		internal LineOption(int flid, string label)
		{
			Flid = flid;
			Label = label;
		}

		public override string ToString()
		{
			return Label;
		}

		internal int Flid { get; }

		internal string Label { get; }
		public bool Equals(LineOption other)
		{
			if (other is null)
				return false;

			return this.Flid == other.Flid;
		}

		public override bool Equals(object obj) => Equals(obj as LineOption);
		public override int GetHashCode() => Flid.GetHashCode();
	}
}