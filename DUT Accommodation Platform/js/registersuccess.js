
$(document).ready(function () {
    // Start countdown
    var seconds = @Model.CountdownSeconds;
    var countdownElement = $('#countdown');

    var countdown = setInterval(function () {
        seconds--;
        countdownElement.text(seconds);

        if (seconds <= 0) {
            clearInterval(countdown);
            window.location.href = '@Model.RedirectUrl';
        }
    }, 1000);

    // Fallback in case the redirect fails
    setTimeout(function () {
        window.location.href = '@Model.RedirectUrl';
    }, @(Model.CountdownSeconds * 1000));
        });

// Handle success page redirection
function handleSuccessPageRedirection() {
    if ($('#countdown').length) {
        const redirectUrl = $('body').data('redirect-url');
        let seconds = parseInt($('#countdown').text());

        const countdown = setInterval(function () {
            seconds--;
            $('#countdown').text(seconds);

            if (seconds <= 0) {
                clearInterval(countdown);
                window.location.href = redirectUrl;
            }
        }, 1000);

        // Fallback redirect
        setTimeout(function () {
            window.location.href = redirectUrl;
        }, seconds * 1000);
    }
}

$(document).ready(function () {
    handleSuccessPageRedirection();
});