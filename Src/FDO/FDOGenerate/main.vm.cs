## --------------------------------------------------------------------------------------------
## Copyright (C) 2006 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
#set( $generated = "_Generated")
#foreach($module in $fdogenerate.Modules)
	$fdogenerate.SetOutput("${module.RelativePath}\\${module.Name}.cs")
	$fdogenerate.Process("module.vm.cs")
#end
## Generate interfaces
$fdogenerate.SetOutput("FdoInterfaces.cs")
$fdogenerate.Process("Interfaces.vm.cs")
