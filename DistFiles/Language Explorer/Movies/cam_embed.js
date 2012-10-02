var args = new Object();
var query = location.search.substring(1);

// Get query string
var pairs = query.split( "," );

// Break at comma
for ( var i = 0; i < pairs.length; i++ )
{
   var pos = pairs[i].indexOf('=');
   if( pos == -1 )
   {
	  continue; // Look for "name=value"
   }

   var argname  = pairs[i].substring( 0, pos );  // If not found, skip
   var value    = pairs[i].substring( pos + 1 ); // Extract the name
   args[argname] = unescape( value );            // Extract the value
}
