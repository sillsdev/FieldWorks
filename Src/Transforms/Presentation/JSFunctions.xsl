<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		TraceScript
		Output the JavaScript script to handle dynamic "tree" tracing and find dialog
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="TraceScript">
		<script language="JavaScript" id="clientEventHandlersJS">
			<xsl:text disable-output-escaping="yes">
	function ButtonShowDetails()
	{
	if (TraceSection.style.display == 'none')
	{
	  TraceSection.style.display = 'block';
	  ShowDetailsButton.value = "Hide Details";
	}
	else
	{
	  TraceSection.style.display = 'none';
	  ShowDetailsButton.value = "Show Details";
	}
	}
	// Center the mouse position in the browser
	function CenterNodeInBrowser(node)
	{
	var posx = 0;
	var posy = 0;
	if (!e) var e = window.event;
	if (e.pageX || e.pageY)
	{
		posx = e.pageX;
		posy = e.pageY;
	}
	else if (e.clientX || e.clientY)
	{
		posx = e.clientX + document.body.scrollLeft;
		posy = e.clientY + document.body.scrollTop;
	}
	// posx and posy contain the mouse position relative to the document
	curY = findPosY(node);
	offset = document.body.clientHeight/2;
	window.scrollTo(0, curY-offset); // scroll to about the middle if possible
	}
	// findPosX() and findPosY() are from http://www.quirksmode.org/js/findpos.html
	function findPosX(obj)
{
	var curleft = 0;
	if (obj.offsetParent)
	{
		while (obj.offsetParent)
		{
			curleft += obj.offsetLeft
			obj = obj.offsetParent;
		}
	}
	else if (obj.x)
		curleft += obj.x;
	return curleft;
}

function findPosY(obj)
{
	var curtop = 0;
	if (obj.offsetParent)
	{
		while (obj.offsetParent)
		{
			curtop += obj.offsetTop
			obj = obj.offsetParent;
		}
	}
	else if (obj.y)
		curtop += obj.y;
	return curtop;
}

// nextSibling function that skips over textNodes.
function NextNonTextSibling(node)
{
	while(node.nextSibling.nodeName == "#text")
		node = node.nextSibling;

	return node.nextSibling;
}

// This script based on the one given in http://www.codeproject.com/jscript/dhtml_treeview.asp.
function Toggle(node, path, imgOffset)
{
	var displayNode= node;
	Images = new Array('beginminus.gif', 'beginplus.gif', 'lastminus.gif', 'lastplus.gif', 'minus.gif', 'plus.gif', 'singleminus.gif', 'singleplus.gif',
										 'beginminusRTL.gif', 'beginplusRTL.gif', 'lastminusRTL.gif', 'lastplusRTL.gif', 'minusRTL.gif', 'plusRTL.gif', 'singleminusRTL.gif', 'singleplusRTL.gif');
	// Unfold the branch if it isn't visible

	if (NextNonTextSibling(node).style.display == 'none')
	{
		displayNode = node.nextSibling;
		searchNode = displayNode;
		// Change the image (if there is an image)
		if (node.childNodes.length > 0)
		{
			if (node.childNodes.item(0).nodeName == "IMG")
			{
				var str = node.childNodes.item(0).src;
				var pos = str.indexOf(Images[1 + imgOffset]); // beginplus.gif
				if (pos >= 0)
				{
					node.childNodes.item(0).src = path + Images[0 + imgOffset]; // "beginminus.gif";
				}
				else
				{
					pos = str.indexOf(Images[7 + imgOffset]); // "singleplus.gif");
					if (pos >= 0)
					{
						node.childNodes.item(0).src = path + Images[6 + imgOffset]; // "singleminus.gif";
					}
					else
					{
						pos = str.indexOf(Images[3 + imgOffset]); // "lastplus.gif");
						if (pos >= 0)
						{
							node.childNodes.item(0).src = path + Images[2 + imgOffset]; // "lastminus.gif";
						}
						else
						{
							node.childNodes.item(0).src = path + Images[4 + imgOffset]; // "minus.gif";
						}
					}
				}
			}
		}
		NextNonTextSibling(node).style.display = 'block';
		CenterNodeInBrowser(node);
	}
	// Collapse the branch if it IS visible
	else
	{
		// Change the image (if there is an image)
		if (node.childNodes.length > 0)
		{
			if (node.childNodes.item(0).nodeName == "IMG")
				var str = node.childNodes.item(0).src;
				var pos = str.indexOf(Images[0 + imgOffset]); // "beginminus.gif");
				if (pos >= 0)
				{
					node.childNodes.item(0).src = path + Images[1 + imgOffset]; // "beginplus.gif";
				}
				else
				{
					pos = str.indexOf(Images[6 + imgOffset]); // "singleminus.gif");
					if (pos >= 0)
					{
						node.childNodes.item(0).src = path + Images[7 + imgOffset]; // "singleplus.gif";
					}
					else
					{
						pos = str.indexOf(Images[2 + imgOffset]); // "lastminus.gif");
						if (pos >= 0)
						{
							node.childNodes.item(0).src = path + Images[3 + imgOffset]; // "lastplus.gif";
						}
						else
						{
							node.childNodes.item(0).src = path + Images[5 + imgOffset]; // "plus.gif";
						}
					}
				}
	}
	NextNonTextSibling(node).style.display = 'none';
	displayNode = node.closest("div");
	searchNode = displayNode;
}
}
			</xsl:text>
		</script>
	<xsl:call-template name="FindScript"/>
	</xsl:template>

	<!--
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		FindScript
		Output the JavaScript script to handle find dialog
		Parameters: none
		- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	-->
	<xsl:template name="FindScript">
		<style type="text/css">
			.interblock {
			display: -moz-inline-box;
			display:
			inline-block;
			vertical-align: top;

			}
			.searchHighlight { background-color: #ffff00;}
			.searchFocusHighlight { background-color: orange;}
		</style>
		<script language="JavaScript" id="clientEventHandlersJS">
			<xsl:text disable-output-escaping="yes">
var searchText = "";
var matchCase = false;
/************************************
* GeckoWebBrowser does not come with its own Find capability.
* This is an attempt to provide such a capabilty with a view to
* what is needed for FlEx's Morph Sketch and Try a Word.
*/
// Object to store positions for multiple matches, keyed by text
var storedPositions = [];
var searchHighlightClass = "searchHighlight";
var searchFocusHighlightClass = "searchFocusHighlight";
var searchNode = document.body;
// Function to find elements by text, highlight matched text, and store positions
function findAndHighlightText(text, caseSensitive = false) {
	if (storedPositions.length > 0) {
		// Clear previous highlights for this text
		removeHighlightSpans(document.body);
	}
	if (text.length == 0)
		return 0;
	matchCase = caseSensitive;
	const positions = [];
	searchText = caseSensitive ? text : text.toLowerCase();
	let matchIndex = 0;
	if (searchNode == null) {
		// Start searching from body
		searchNode = document.body;
	}
	searchTextNodes(searchNode, searchText, caseSensitive, positions);
	if (positions.length == 0) {
		storedPositions = [];
		return 0;
	}
	else {
		// Store positions
		storedPositions = positions;
		scrollToStoredPosition(0);
	}
	return positions.length;
}
// Recursive function to search text nodes
function searchTextNodes(node, searchText, caseSensitive, positions) {
	if (node.nodeType == Node.TEXT_NODE) {
		const nodeText = caseSensitive ? node.textContent : node.textContent.toLowerCase();
		if (nodeText.includes(searchText) &amp;&amp; node.parentElement.closest("div[style~='display:none']") == null) {
			// Only use nodes that have matching text and are visible
			const parent = node.parentElement;
			if (parent &amp;&amp; parent.nodeType == Node.ELEMENT_NODE) {
				// Split the text node at each match
				const fragment = document.createDocumentFragment();
				const parts = nodeText.split(searchText);
				var pos = 0;
				parts.forEach(part => {
					if (part.length == 0) {
						pos = insertHighlightSpan(pos, node, searchText, fragment, positions, parent);
					}
					fragment.appendChild(document.createTextNode(node.textContent.substring(pos, pos + part.length)));
					pos += part.length;
					if (part.length > 0) {
						pos = insertHighlightSpan(pos, node, searchText, fragment, positions, parent);
					}
				});
				node.replaceWith(fragment);
			}
		}
	} else {
		// Create a copy of child nodes to avoid issues with live DOM changes
		Array.from(node.childNodes).forEach(child => searchTextNodes(child, searchText, caseSensitive, positions));
	}
}
// Function to insert a higlighted span for a matching string
function insertHighlightSpan(pos, node, searchText, fragment, positions, parent) {
	const span = document.createElement('span');
	span.className = searchHighlightClass;
	const newPos = pos + searchText.length
	span.textContent = node.textContent.substring(pos, newPos);
	if (span.textContent.length == 0) {
		return newPos;
	}
	span.dataset.highlightText = searchText; // For tracking
	fragment.appendChild(span);
	// Store position of the parent element
	const rect = parent.getBoundingClientRect();
	positions.push({
		x: rect.left + window.scrollX,
		y: rect.top + window.scrollY,
		highlightSpan: span // Store span for scrolling
	});
	return newPos;
}
// Recursive function to replace highlighted spans with their text.
// Trying to use the stored positions leaves some spans still highlighted.
// So we just walk the DOM tree and remove any we find.
function removeHighlightSpans(node) {
	const parent = node.parentElement;
	if (node.nodeType == Node.ELEMENT_NODE &amp;&amp; node.nodeName == 'SPAN' &amp;&amp; (node.className == searchHighlightClass || node.className == searchFocusHighlightClass)) {
		node.replaceWith(node.textContent);
	}
	Array.from(node.childNodes).forEach(child => removeHighlightSpans(child));
	if (parent != null) {
		parent.normalize();
	}
}
function cleanUpHighlights()
{
	removeHighlightSpans(document.body);
}
// Function to scroll to a specific match by index
function scrollToStoredPosition(matchIndex = 0, matchNext = true) {
	const positions = storedPositions;
	if (!positions || positions.length == 0) {
		return;
	}
	if (matchIndex &lt; 0 || matchIndex >= positions.length) {
		return;
	}
	const position = positions[matchIndex];
	var node = position.highlightSpan;
	var lastIndex = (matchIndex - 1) >= 0 ? matchIndex - 1 : positions.length - 1;
	if (!matchNext) {
		lastIndex = (matchIndex + 1) >= positions.length - 1 ? 0 : matchIndex + 1;
	}
	positions[lastIndex].highlightSpan.className = searchHighlightClass;
	position.highlightSpan.className = searchFocusHighlightClass;
	// Scroll to the stored coordinates
	// Try to center vertically
	var vOffset = document.body.clientHeight / 2;
	window.scrollTo({
		top: position.y - vOffset,
		left: position.x,
		behavior: 'instant'
	});
	return true;
}
			</xsl:text>
		</script>
	</xsl:template>
</xsl:stylesheet>
