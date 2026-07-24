// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// Pins the .resx localization strategy for the FieldWorks-owned Avalonia strings: every
	/// accessor property must resolve from its project's neutral resx (the accessors fall back to
	/// the string id when an entry is missing, so a resolved value that looks like an id means the
	/// resx and the accessor drifted apart), and resolution must survive a UI culture with no
	/// satellite assembly by falling back to the neutral English resources.
	/// </summary>
	[TestFixture]
	public sealed class AvaloniaLocalizationTests
	{
		private static readonly Type[] AccessorClasses =
		{
			typeof(FwAvaloniaStrings),
			typeof(FwAvaloniaDialogsStrings)
		};

		private static string[] AccessorProperties(Type accessor) =>
			accessor.GetProperties(BindingFlags.Public | BindingFlags.Static)
				.Where(p => p.PropertyType == typeof(string))
				.Select(p => p.Name)
				.ToArray();

		[Test]
		public void EveryAccessorProperty_ResolvesFromTheNeutralResx()
		{
			foreach (var accessor in AccessorClasses)
			{
				var propertyNames = AccessorProperties(accessor);
				Assert.That(propertyNames, Is.Not.Empty, $"{accessor.Name} exposes string properties");
				foreach (var name in propertyNames)
				{
					var value = (string)accessor.GetProperty(name).GetValue(null);
					Assert.That(value, Is.Not.Null.And.Not.Empty,
						$"{accessor.Name}.{name} must have a neutral resx entry");
					Assert.That(value, Does.Not.StartWith("FwAvalonia.").And.Not.StartWith("FwAvaloniaDialogs."),
						$"{accessor.Name}.{name} resolved to its id — the resx entry is missing or renamed");
				}
			}
		}

		[Test]
		public void AccessorProperties_FallBackToNeutralEnglish_ForACultureWithNoSatellite()
		{
			var original = Thread.CurrentThread.CurrentUICulture;
			try
			{
				Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
				Assert.That(FwAvaloniaStrings.Cancel, Is.EqualTo("Cancel"));
				Assert.That(FwAvaloniaDialogsStrings.Help, Is.EqualTo("Help"));
			}
			finally
			{
				Thread.CurrentThread.CurrentUICulture = original;
			}
		}
	}
}
