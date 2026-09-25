// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The DetailRules boundary: rules shared by the WinForms and Avalonia detail views take
	/// domain objects and plain values only, so they keep working once WinForms is removed.
	/// One test checks every signature in the namespace, the other the folder's using
	/// directives, which is where a method body would pull a UI stack in.
	/// </summary>
	[TestFixture]
	public class DetailRulesBoundaryTests
	{
		private static readonly string[] ForbiddenAssemblyPrefixes =
		{
			"System.Windows.Forms", "System.Drawing", "Avalonia", "DetailControls", "FwControls",
			"RootSite", "SimpleRootSite", "XMLViews", "Framework", "FwAvalonia"
		};

		private static readonly Regex ForbiddenUsing = new Regex(
			@"^\s*using\s+(static\s+)?(System\.Windows\.Forms|System\.Drawing|Avalonia|SIL\.FieldWorks\.Common\.Framework\.DetailControls|SIL\.FieldWorks\.Common\.Controls|SIL\.FieldWorks\.Common\.FwAvalonia|SIL\.FieldWorks\.Common\.RootSites)",
			RegexOptions.Compiled);

		[Test]
		public void DetailRulesTypes_ReferenceNoUiStackInTheirSignatures()
		{
			var rulesNamespace = typeof(FieldHelpTopics).Namespace;
			var types = typeof(FieldHelpTopics).Assembly.GetTypes()
				.Where(t => t.Namespace == rulesNamespace).ToList();
			Assert.That(types, Is.Not.Empty);

			var offences = new List<string>();
			const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
				| BindingFlags.Static | BindingFlags.DeclaredOnly;
			foreach (var type in types)
			{
				Check(type.BaseType, type.Name + " : base", offences);
				foreach (var i in type.GetInterfaces())
					Check(i, type.Name + " : interface", offences);
				foreach (var f in type.GetFields(all))
					Check(f.FieldType, type.Name + "." + f.Name, offences);
				foreach (var p in type.GetProperties(all))
					Check(p.PropertyType, type.Name + "." + p.Name, offences);
				foreach (var m in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
				{
					if (m is MethodInfo mi)
						Check(mi.ReturnType, type.Name + "." + m.Name + " returns", offences);
					foreach (var prm in m.GetParameters())
						Check(prm.ParameterType, type.Name + "." + m.Name + "(" + prm.Name + ")", offences);
				}
			}
			Assert.That(offences, Is.Empty, "DetailRules must not depend on a UI stack");
		}

		[Test]
		public void DetailRulesSources_ImportNoUiStackNamespace()
		{
			var folder = FindDetailRulesFolder();
			Assume.That(folder, Is.Not.Null, "the repository sources are not beside the test output");
			var offences = new List<string>();
			foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
			{
				var lines = File.ReadAllLines(file);
				for (var i = 0; i < lines.Length; i++)
				{
					if (ForbiddenUsing.IsMatch(lines[i]))
						offences.Add(Path.GetFileName(file) + ":" + (i + 1) + " " + lines[i].Trim());
				}
			}
			Assert.That(offences, Is.Empty, "DetailRules sources must not import a UI stack");
		}

		// The repo's Src/FdoUi/DetailRules, found by walking up from the test output folder.
		private static string FindDetailRulesFolder()
		{
			for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
			{
				var candidate = Path.Combine(dir.FullName, "Src", "FdoUi", "DetailRules");
				if (Directory.Exists(candidate))
					return candidate;
			}
			return null;
		}

		private static void Check(Type type, string site, List<string> offences)
		{
			if (type == null)
				return;
			if (type.IsGenericType)
			{
				foreach (var arg in type.GetGenericArguments())
					Check(arg, site, offences);
			}
			var assembly = type.Assembly.GetName().Name;
			if (ForbiddenAssemblyPrefixes.Any(p => assembly.StartsWith(p, StringComparison.Ordinal)))
				offences.Add(site + " -> " + type.FullName);
		}
	}
}
