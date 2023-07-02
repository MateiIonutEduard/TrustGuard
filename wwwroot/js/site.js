$(document).ready(function () {
    $('#description').summernote();
});

function CopyTo(uid) {
    var authText = document.getElementById(uid);
    authText.select();

    authText.setSelectionRange(0, 99999);
    navigator.clipboard.writeText(authText.value);
}