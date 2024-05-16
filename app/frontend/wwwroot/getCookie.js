// Define the getCookie function
function getCookie(name) {
    let decodedCookie = decodeURIComponent(document.cookie);
    let cookieArr = decodedCookie.split(";");
    for (let i = 0; i < cookieArr.length; i++) {
        let cookiePair = cookieArr[i].split("=");
        if (name === cookiePair[0].trim()) {
            return cookiePair[1];  // Return the decoded value
        }
    }
    return null;
}

// Retrieve the XSRF token using the getCookie function
let xsrfToken = getCookie("XSRF-TOKEN");

if (xsrfToken) {
    // Make an AJAX request using the XSRF token
    fetch('/your-endpoint', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-XSRF-TOKEN': xsrfToken
        },
        body: JSON.stringify({ key: 'value' })
    })
    .then(response => response.json())
    .then(data => console.log(data))
    .catch(error => console.error('Error:', error));
} else {
    console.error('XSRF token not found');
}
