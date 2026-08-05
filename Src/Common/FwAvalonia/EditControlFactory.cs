// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Constructs the record-edit control for the resolved <see cref="UIFramework"/>, invoking only that
	/// framework's builder: under Legacy the Avalonia control, and therefore the Avalonia runtime, is never
	/// constructed. Both builders arrive as delegates, so this type carries no Avalonia dependency.
	/// </summary>
	public sealed class EditControlFactory
	{
		private readonly Func<object> _winFormsControlBuilder;
		private readonly Func<object> _avaloniaControlBuilder;

		/// <summary>Number of times the Avalonia builder has been invoked; zero until Avalonia resolves.</summary>
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
