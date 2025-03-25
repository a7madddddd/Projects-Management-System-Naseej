function checkAuthentication() {
    const token = sessionStorage.getItem('authToken');

    // If no token, redirect to login page
    if (!token) {
        window.location.href = 'auth-login.html';
    }
}
// 
// Perform initial check on page load
document.addEventListener('DOMContentLoaded', checkAuthentication);

// Check authentication every second
setInterval(checkAuthentication, 100);
