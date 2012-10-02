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

		#region ISilDataAccess related methods
#if( $class.AtomicProperties.Count > 0 )

#parse( "SDAObjectPropertyMethods.vm.cs" )
#end
#if( $class.VectorProperties.Count > 0 )

#parse( "SDAVectorPropertyMethods.vm.cs" )
#end
#if( $class.IntegerProperties.Count > 0 )

#parse( "SDAIntegerPropertyMethods.vm.cs" )
#end
#if( $class.BooleanProperties.Count > 0 )

#parse( "SDABooleanPropertyMethods.vm.cs" )
#end
#if( $class.GuidProperties.Count > 0 )

#parse( "SDAGuidPropertyMethods.vm.cs" )
#end
#if( $class.DateTimeProperties.Count > 0 )

#parse( "SDADateTimePropertyMethods.vm.cs" )
#end
#if( $class.GenDateProperties.Count > 0 )

#parse( "SDAGenDatePropertyMethods.vm.cs" )
#end
#if( $class.BinaryProperties.Count > 0 )

#parse( "SDABinaryPropertyMethods.vm.cs" )
#end

#parse( "SDAStringPropertyMethods.vm.cs" )

		#endregion ISilDataAccess related methods