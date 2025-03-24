function loginUser() {
    // Get username and password from input fields
    const username = document.getElementById('username').value;
    const password = document.getElementById('userpassword').value;

    // API endpoint
    const loginUrl = 'https://localhost:44320/api/Account/login';

    // Fetch options
    const fetchOptions = {
        method: 'POST',
        credentials: 'include', 

        headers: {
            'accept': '*/*',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            usernameOrEmail: username,
            password: password
        })
    };

    // Perform login
    fetch(loginUrl, fetchOptions)
        .then(response => {
            if (!response.ok) {
                throw new Error('Login failed');
            }
            return response.json();
        })
        .then(data => {
            // Store token in session storage
            sessionStorage.setItem('authToken', data.token);


            // Redirect or perform next action
            window.location.href = 'index.html'; // Replace with your dashboard page
        })
        .catch(error => {
            console.error('Login error:', error);
            alert('Login failed. Please check your credentials.');
        });
}

// Check token on page load
function checkAuthentication() {
    const token = sessionStorage.getItem('authToken');

    // If no token, prevent access to protected pages
    if (!token) {
        // Redirect to login or show login modal
        // alert('Please log in to access this page');
        // // Optionally redirect to login page
        // window.location.href = 'auth-login.html';
    }
}

// Call this function on pages that require authentication
document.addEventListener('DOMContentLoaded', checkAuthentication);