## --------------------------------------------------------------------------------------------
## Copyright (c) 2007-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
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