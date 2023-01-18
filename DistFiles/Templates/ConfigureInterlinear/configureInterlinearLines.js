

// Drag and drop methods
dragula([document.getElementsByClassName("morpheme-container")[0]], {
  moves: function (el, container, handle) {
    return handle.classList.contains('internal-row-handle');
  }
});

dragula([document.getElementsByClassName("segment-container")[0]], {
  moves: function (el, container, handle) {
    return handle.classList.contains('internal-row-handle');
  }
});

dragula([document.getElementById("parent-grid")], {
  moves: function (el, container, handle) {
    return handle.classList.contains('row-handle');
  },
  accepts: function(el, target, source, sibling) {
	  return !sibling.classList.contains('header');
  }
});

// No rows selected warning code
// hide the warning initially (we shouldn't be able to have no rows selected on load)
document.getElementById("warning").style.display = "none";

// Add click handler to all checkboxes
var checkBoxes = document.getElementsByClassName("checkBox");
Array.prototype.forEach.call(checkBoxes, function(cb) {
    cb.onclick=checkBoxClicked;
});

function checkBoxClicked() {
	if(!anyCheckboxSelected()) {
		document.getElementById("warning").style.display = "block";
	}
	else {
		document.getElementById("warning").style.display = "none";
	}
}

function anyCheckboxSelected() {
    var inputElements = document.getElementsByClassName("checkBox");
    for (var i = 0; i < inputElements.length; i++)
        if (inputElements[i].checked)
            return true;
    return false;
}

// This function returns the rows to the C# code for updating the model
function getRows() {
  var rows = Array.from(document.getElementsByClassName("line-choice"));
  return rows.map(function(r){return r.id;});
}