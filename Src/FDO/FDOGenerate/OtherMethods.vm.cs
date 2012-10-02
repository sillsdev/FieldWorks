## --------------------------------------------------------------------------------------------
## Copyright (C) 2007-2008 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
## This will generate the various methods used by the FDO aware ISilDataAccess implementation.
## Using these methods avoids using Reflection in that implementation.
##
		#region Other methods

#parse( "InitializeFromBEPMethods.vm.cs" )
## ISilDataAccess related methods
#parse( "IDomainDataByFlidAccessorMethods.vm.cs" )
#if ($class.OwningProperties.Count > 0)
#parse( "RemoveOwneeMethod.vm.cs" )
#end
#if ($class.ObjectProperties.Count > 0)
#parse( "DeleteMethod.vm.cs" )
#parse( "ClearIncomingRefsOnOutgoingRefs.vm.cs" )
#parse( "RestoreIncomingRefsOnOutgoingRefs.vm.cs" )
#end
#if ($class.AtomicRefProperties.Count > 0)
#parse( "RemoveAReferenceCore.vm.cs" )
#end
#if ($class.AtomicRefProperties.Count > 0 || $class.CollectionRefProperties.Count > 0 || $class.SequenceRefProperties.Count > 0)
#parse( "AllReferencedObjects.vm.cs" )
#end

		#endregion Other methods