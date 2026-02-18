using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.FieldWorks.Common.Avalonia.Preview;

namespace SIL.FieldWorks.Common.Avalonia.PreviewHost;

internal sealed class ModuleCatalog
{
	public IReadOnlyList<ModuleInfo> Modules { get; }

	public ModuleCatalog()
	{
		var assemblies = LoadAssemblies(AppContext.BaseDirectory);
		Modules = assemblies
			.SelectMany(GetModules)
			.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public ModuleInfo? Find(string? id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return Modules.FirstOrDefault(m => string.Equals(m.Id, "advanced-entry", StringComparison.OrdinalIgnoreCase))
				?? Modules.FirstOrDefault();

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
			attrs = assembly.GetCustomAttributes<FwPreviewModuleAttribute>().ToArray();
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

internal sealed record ModuleInfo(string Id, string DisplayName, Type WindowType, Type? DataProviderType);
