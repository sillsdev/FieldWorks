// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

#if __MonoCS__
namespace SIL.FieldWorks.Common.RootSites
{
	// UIAutomation is part of WPF and thus not available on Mono.
	// We provide the interfaces here so that we can compile (without requiring the Windows-only
	// assemblies which causes problems e.g. when restoring nuget packages).
	// ENHANCE: refactor our code so that we compile without the WPF interfaces on Linux

	/// <summary/>
	public interface IRawElementProviderFragment
	{
	}

	/// <summary/>
	public interface IRawElementProviderFragmentRoot
	{
	}

	/// <summary/>
	public interface ITextProvider
	{
	}

	/// <summary/>
	public interface IValueProvider
	{
	}

	/// <summary/>
	public interface NavigateDirection
	{
	}

	/// <summary/>
	public interface IRawElementProviderSimple
	{
	}

	/// <summary/>
	public enum ProviderOptions
	{
		/// <summary/>
		ServerSideProvider
	}
}
#endif
