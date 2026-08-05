// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Constructs the lexical-edit control for a host, proving the key dual-run property:
	/// when the flag is off, the Avalonia control (and therefore the Avalonia runtime) is
	/// never constructed. The Avalonia builder is supplied as a delegate so this factory
	/// itself carries no Avalonia dependency and can be tested in isolation.
	/// </summary>
	public sealed class EditControlFactory
	{
		private readonly Func<object> _winFormsControlBuilder;
		private readonly Func<object> _avaloniaControlBuilder;

		/// <summary>
		/// Number of times the Avalonia builder has been invoked. Tests assert this stays 0
		/// when the resolved framework is Legacy.
		/// </summary>
		public int AvaloniaConstructionCount { get; private set; }

		public EditControlFactory(
			Func<object> winFormsControlBuilder,
			Func<object> avaloniaControlBuilder)
		{
			_winFormsControlBuilder = winFormsControlBuilder
				?? throw new ArgumentNullException(nameof(winFormsControlBuilder));
			_avaloniaControlBuilder = avaloniaControlBuilder
				?? throw new ArgumentNullException(nameof(avaloniaControlBuilder));
		}

		/// <summary>
		/// Builds the control for the resolved framework. The Avalonia builder is invoked only
		/// when <paramref name="framework"/> is <see cref="UIFramework.Avalonia"/>.
		/// </summary>
		public object Create(UIFramework framework)
		{
			if (framework == UIFramework.Avalonia)
			{
				AvaloniaConstructionCount++;
				return _avaloniaControlBuilder();
			}

			return _winFormsControlBuilder();
		}
	}
}
