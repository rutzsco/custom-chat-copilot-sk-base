function refreshTokens() {
    let refreshUrl = "/.auth/refresh";
    $.ajax(refreshUrl).done(function () {
        console.log("Token refresh completed successfully.");
    }).fail(function () {
        console.log("Token refresh failed. See application logs for details.");
    });
}

refreshTokens();

setInterval(refreshTokens, 1800000); // refresh the EasyAuth access token every 30 minutes since this is used by the OBO flow to get the access token for the backend API