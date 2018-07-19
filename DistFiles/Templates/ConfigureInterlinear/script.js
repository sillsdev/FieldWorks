// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
$("tbody").sortable({
	handle: ".grab",
	cursor: "grabbing",
	containment: "document",
	opacity: 0.5,
	stop: hasStopped
});

$("#specialTable tbody").sortable({
	handle: ".grab",
	cursor: "grabbing",
	containment: "document",
	opacity: 0.5,
	stop: hasStopped
});

var rows;

$(document).ready(function() {
	initRows();

	if ($("input[type='checkbox']:checked").length > 0) {
		$("#warning").hide();
	}
});

function initRows() {
	rows = [];
	$(document).find(".row").each(function(i, el) {
		var $trs = $(this),
			rowId = $trs.eq(0).attr("id");
		rows.push(rowId);
	})
}

function getRows() {
	return(rows);
}

function getNumOfCheckedBoxes() {
	return($("input[type='checkbox']:checked").length);
}

function hasStopped(event, ui) {
	initRows();
}

$("input[type='checkbox']").on("change", function() {
	if ($("input[type='checkbox']:checked").length == 0) {
		$("#warning").show();
	} else {
		$("#warning").hide();
	}
});