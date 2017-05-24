## --------------------------------------------------------------------------------------------
## Copyright (c) 2009-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the LcmGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
using SIL.LCModel.DomainImpl;
using SIL.LCModel.Infrastructure.Impl;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;

namespace SIL.LCModel.IOC
{
	/// <summary>
	/// Bootstrapper for LCM Common Service Locator.
	/// </summary>
	internal partial class LcmServiceLocatorFactory
	{
		private static void AddFactories(Registry registry)
		{
#foreach($module in $lcmgenerate.Modules)
#foreach($class in $module.Classes)
#if ($class.Name == "LgWritingSystem")
#set( $classSfx = "FactoryLcm" )
#else
#set( $classSfx = "Factory" )
#end
#if(!$class.IsAbstract)
			registry
				.For<I${class.Name}$classSfx>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<${class.Name}$classSfx>();
#end
#end
#end
		}

		private static void AddRepositories(Registry registry)
		{
#foreach($module in $lcmgenerate.Modules)
#foreach($class in $module.Classes)
			registry
				.For<I${class.Name}Repository>()
				.LifecycleIs(new SingletonLifecycle())
				.Use<${class.Name}Repository>();
#end
#end
		}
	}
}
