// =======================================================
// START: RACEDAY SITE SCRIPT
// Small progressive enhancements - the site works without them.
// =======================================================
(function () {
  "use strict";

  // Fall back to the placeholder when an image URL cannot be loaded.
  document.querySelectorAll("img[data-fallback]").forEach(function (img) {
    img.addEventListener("error", function () {
      var fallback = img.getAttribute("data-fallback");
      if (fallback && img.getAttribute("src") !== fallback) {
        img.setAttribute("src", fallback);
      }
    });
  });

  // Confirm before destructive actions: <form data-confirm="Are you sure?">
  document.querySelectorAll("form[data-confirm]").forEach(function (form) {
    form.addEventListener("submit", function (event) {
      if (!window.confirm(form.getAttribute("data-confirm"))) {
        event.preventDefault();
      }
    });
  });

  // Disable the submit button after the first click to stop double submissions.
  document.querySelectorAll("form[data-once]").forEach(function (form) {
    form.addEventListener("submit", function () {
      if (form.querySelector(".input-validation-error")) { return; }
      form.querySelectorAll("button[type=submit]").forEach(function (button) {
        setTimeout(function () { button.disabled = true; }, 0);
      });
    });
  });

  // Live preview of a chosen image before it is uploaded.
  document.querySelectorAll("input[type=file][data-preview]").forEach(function (input) {
    var target = document.getElementById(input.getAttribute("data-preview"));
    if (!target) { return; }
    input.addEventListener("change", function () {
      var file = input.files && input.files[0];
      if (!file) { return; }
      if (file.size > 5 * 1024 * 1024) {
        window.alert("That image is larger than 5 MB. Please choose a smaller one.");
        input.value = "";
        return;
      }
      target.setAttribute("src", URL.createObjectURL(file));
    });
  });

  // Show or hide the organiser code field on the register page.
  var role = document.querySelectorAll("input[name=Role]");
  var codeBox = document.getElementById("organiser-code-box");
  if (role.length && codeBox) {
    var update = function () {
      var selected = document.querySelector("input[name=Role]:checked");
      codeBox.style.display = selected && selected.value === "Organiser" ? "block" : "none";
    };
    role.forEach(function (r) { r.addEventListener("change", update); });
    update();
  }

  // Result form: finish time and position only apply to finishers.
  var status = document.getElementById("ResultStatus");
  var finisherFields = document.getElementById("finisher-fields");
  if (status && finisherFields) {
    var toggle = function () {
      finisherFields.style.display = status.value === "Finished" ? "block" : "none";
    };
    status.addEventListener("change", toggle);
    toggle();
  }
})();
// =======================================================
// END: RACEDAY SITE SCRIPT
// =======================================================
