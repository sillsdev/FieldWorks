// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.FieldWorks.Common.FwAvalonia.Preview;

namespace SIL.FieldWorks.Common.FwAvalonia.PreviewHost
{
	internal sealed class ModuleCatalog
	{
		public ModuleCatalog()
		{
			Modules = LoadAssemblies(AppContext.BaseDirectory)
				.SelectMany(GetModules)
				.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		public IReadOnlyList<ModuleInfo> Modules { get; }

		public ModuleInfo Find(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return Modules.FirstOrDefault();

			return Modules.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
		}

		private static IEnumerable<Assembly> LoadAssemblies(string directory)
		{
			var loaded = new List<Assembly>();
			loaded.AddRange(AppDomain.CurrentDomain.GetAssemblies());

			foreach (var path in Directory.EnumerateFiles(directory, "*.dll"))
			{
				try
				{
					var assemblyName = AssemblyName.GetAssemblyName(path);
					if (loaded.Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName)))
						continue;

					loaded.Add(Assembly.LoadFrom(path));
				}
				catch
				{
					// Ignore native DLLs and any load failures.
				}
			}

			return loaded;
		}

		private static IEnumerable<ModuleInfo> GetModules(Assembly assembly)
		{
			FwPreviewModuleAttribute[] attrs;
			try
			{
				attrs = assembly.GetCustomAttributes(typeof(FwPreviewModuleAttribute), false)
					.OfType<FwPreviewModuleAttribute>()
					.ToArray();
			}
			catch
			{
				yield break;
			}

			foreach (var attr in attrs)
			{
				yield return new ModuleInfo(attr.Id, attr.DisplayName, attr.WindowType, attr.DataProviderType);
			}
		}
	}

	internal sealed class ModuleInfo
	{
		public ModuleInfo(string id, string displayName, Type windowType, Type dataProviderType)
		{
			Id = id;
			DisplayName = displayName;
			WindowType = windowType;
			DataProviderType = dataProviderType;
		}

		public string Id { get; }
		public string DisplayName { get; }
		public Type WindowType { get; }
		public Type DataProviderType { get; }
	}
}
