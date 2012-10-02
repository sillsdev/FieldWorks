<!--
function movepic(img_name,img_src) {
document[img_name].src=img_src;
}

function setClassName(objId, className) {
		document.getElementById(objId).className = className;
}

function showAllDocs() {
	setClassByClass("tr","docsClosed","docsOpen");
	for (var loop = 2; loop <
document.images.length; loop++)
{
	document.images[loop].src = 'expanded.gif';
}
	}
// Loops start at 2 below to avoid first two instances of images which are used in
// how-to-collapse-and-expand documentation.
function hideAllDocs() {
	setClassByClass("tr","docsOpen","docsClosed");
	for (var loop = 2; loop <
document.images.length; loop++)
{
	document.images[loop].src = 'collapsed.gif';
}
	}
function expand(thistag) {
   var thisTagImage = thistag+"Image";
   styleObj=document.getElementById(thistag).style;
   classObj=document.getElementById(thistag).className
   if (classObj=='docsClosed')
	{

		setClassName(thistag,"docsOpen");
		movepic(thisTagImage,'expanded.gif');
	}
   else
	{
		setClassName(thistag,"docsClosed");
		movepic(thisTagImage,'collapsed.gif');
	}
}

function expandold(thistag) {
   var thisTagImage = thistag+"Image";
   styleObj=document.getElementById(thistag).style;
   if (styleObj.display=='none')
	{
		styleObj.display='';
		movepic(thisTagImage,'expanded.gif');
	}
   else
	{
		styleObj.display='none';
		movepic(thisTagImage,'collapsed.gif');
	}
}

// setClassByClass: given an element type and a class selector,
// new class to set to, set the old class to the new class.
// args:
//  t - type of tag to check for (e.g., SPAN)
//  c - class name
//  y - newclass
var ie = (document.all) ? true : false;
function setClassByClass(t,c,y){
	var elements;
	if(t == '*') {
		// '*' not supported by IE/Win 5.5 and below
		elements = (ie) ? document.all : document.getElementsByTagName('*');
	} else {
		elements = document.getElementsByTagName(t);
	}
	for(var i = 0; i < elements.length; i++){
		var node = elements.item(i);
		for(var j = 0; j < node.attributes.length; j++) {
			if(node.attributes.item(j).nodeName == 'class') {
				if(node.attributes.item(j).nodeValue == c) {
					eval('node.className' + " = '" +y + "'");
				}
			}
		}
	}
}
