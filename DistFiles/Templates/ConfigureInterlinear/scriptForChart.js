var wordGlossCheckboxes = $("input[name='5060001[]']");
var wordCheckbox = $("input[name='5062001[]']:first");
var wordCheckboxes = $("input[name='5062001[]']");
var lexGlossCheckboxes = $("input[name='5112004[]']");
var morphemesCheckbox = $("input[name='5112002[]']:first");
var morphemesCheckboxes = $("input[name='5112002[]']");

wordGlossCheckboxes.on("change", function() {
	if ($(this).is(":checked") && $("input[name='5062001[]']:checked").length == 0) {
		wordCheckbox.prop("checked", true);
	} else if ($(this).is(":checked") && $("input[name='5062001[]']:checked").length > 0) {
		return;
	}
});

lexGlossCheckboxes.on("change", function() {
	if ($(this).is(":checked") && $("input[name='5112002[]']:checked").length == 0) {
		morphemesCheckbox.prop("checked", true);
	} else if ($(this).is(":checked") && $("input[name='5112002[]']:checked").length > 0) {
		return;
	}
});

wordCheckboxes.on("change", function() {
	if ($(this).is(":not(:checked)") && $("input[name='5062001[]']:checked").length == 0 && $("input[name='5060001[]']:checked").length > 0) {
		wordGlossCheckboxes.prop("checked", false);
	}
});

morphemesCheckboxes.on("change", function() {
	if ($(this).is(":not(:checked)") && $("input[name='5112002[]']:checked").length == 0 && $("input[name='5112004[]']:checked").length > 0) {
		lexGlossCheckboxes.prop("checked", false);
	}
});