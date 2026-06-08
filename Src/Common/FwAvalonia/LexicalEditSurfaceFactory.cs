// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Constructs the Lexical Edit surface for a host, proving the key dual-run property:
	/// when the flag is off, the Avalonia surface (and therefore the Avalonia runtime) is
	/// never constructed. The Avalonia builder is supplied as a delegate so this factory
	/// itself carries no Avalonia dependency and can be tested in isolation.
	/// </summary>
	public sealed class LexicalEditSurfaceFactory
	{
		private readonly Func<object> _winFormsSurfaceBuilder;
		private readonly Func<object> _avaloniaSurfaceBuilder;

		/// <summary>
		/// Number of times the Avalonia builder has been invoked. Tests assert this stays 0
		/// when the resolved surface is WinForms.
		/// </summary>
		public int AvaloniaConstructionCount { get; private set; }

		public LexicalEditSurfaceFactory(
			Func<object> winFormsSurfaceBuilder,
			Func<object> avaloniaSurfaceBuilder)
		{
			_winFormsSurfaceBuilder = winFormsSurfaceBuilder
				?? throw new ArgumentNullException(nameof(winFormsSurfaceBuilder));
			_avaloniaSurfaceBuilder = avaloniaSurfaceBuilder
				?? throw new ArgumentNullException(nameof(avaloniaSurfaceBuilder));
		}

		/// <summary>
		/// Builds the surface for the given resolution. The Avalonia builder is invoked only
		/// when <paramref name="surface"/> is <see cref="LexicalEditSurface.Avalonia"/>.
		/// </summary>
		public object Create(LexicalEditSurface surface)
		{
			if (surface == LexicalEditSurface.Avalonia)
			{
				AvaloniaConstructionCount++;
				return _avaloniaSurfaceBuilder();
			}

			return _winFormsSurfaceBuilder();
		}
	}
}
