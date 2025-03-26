class AuthService {
    constructor() {
        this.baseUrl = 'https://localhost:44320/api';
        this.loginUrl = `${this.baseUrl}/Account/login`;
    }

    async loginUser(username, password, rememberMe = false) {
        try {
            // Get the current page URL to return after login
            const currentPath = window.location.pathname;
            const returnUrl = encodeURIComponent(currentPath);

            const response = await fetch(this.loginUrl, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'accept': '*/*',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    usernameOrEmail: username,
                    password: password,
                    rememberMe: rememberMe
                })
            });

            if (!response.ok) {
                // Try to parse error message from response
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || 'Login failed');
            }

            const data = await response.json();

            // Store token and additional info
            sessionStorage.setItem('authToken', data.token);
            sessionStorage.setItem('loginTime', new Date().toISOString());

            // Optional: Store remember me preference
            if (rememberMe) {
                localStorage.setItem('rememberMe', 'true');
                localStorage.setItem('username', username);
            } else {
                localStorage.removeItem('rememberMe');
                localStorage.removeItem('username');
            }

            // Redirect to original page or default dashboard
            this.redirectAfterLogin(returnUrl);

            return data;
        } catch (error) {
            console.error('Login error:', error);

            // Use SweetAlert for error notification
            Swal.fire({
                icon: 'error',
                title: 'Login Failed',
                text: error.message || 'Please check your credentials',
                confirmButtonText: 'Try Again'
            });

            throw error;
        }
    }

    redirectAfterLogin(returnUrl) {
        // Decode the return URL
        const decodedReturnUrl = decodeURIComponent(returnUrl);

        // List of allowed pages to prevent redirect to login page
        const allowedPages = ['/index.html', '/', '/dashboard.html'];

        // Determine where to redirect
        if (decodedReturnUrl &&
            decodedReturnUrl !== '/auth-login.html' &&
            decodedReturnUrl !== '/') {
            window.location.href = decodedReturnUrl;
        } else {
            // Default to dashboard or home
            window.location.href = 'index.html';
        }
    }

    // Check authentication status
    isAuthenticated() {
        const token = sessionStorage.getItem('authToken');
        if (!token) return false;

        try {
            // Optional: Decode token to check expiration
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace('-', '+').replace('_', '/');
            const payload = JSON.parse(window.atob(base64));

            // Check token expiration
            const currentTime = Math.floor(Date.now() / 1000);
            return payload.exp > currentTime;
        } catch (error) {
            console.error('Token validation error:', error);
            return false;
        }
    }

    // Logout method
    logout() {
        // Remove authentication token and related data
        sessionStorage.removeItem('authToken');
        sessionStorage.removeItem('loginTime');
        localStorage.removeItem('rememberMe');
        localStorage.removeItem('username');

        // Redirect to login page
        window.location.href = 'auth-login.html';
    }

    // Prefill username if remember me was previously selected
    prefillUsername() {
        const rememberMe = localStorage.getItem('rememberMe');
        const savedUsername = localStorage.getItem('username');

        if (rememberMe === 'true' && savedUsername) {
            const usernameInput = document.getElementById('username');
            const rememberMeCheckbox = document.getElementById('customSwitchSuccess');

            if (usernameInput) usernameInput.value = savedUsername;
            if (rememberMeCheckbox) rememberMeCheckbox.checked = true;
        }
    }
}

// Authentication Middleware
function checkAuthentication() {
    const authService = new AuthService();
    const currentPath = window.location.pathname;

    // Pages that don't require authentication
    const publicPages = ['/auth-login.html', '/auth-register.html', '/auth-recover-pw.html'];

    // Check if the current page is a public page
    const isPublicPage = publicPages.some(page =>
        currentPath.endsWith(page) || currentPath === page
    );

    // If not a public page and not authenticated, redirect to login
    if (!isPublicPage && !authService.isAuthenticated()) {
        // Store the current path to return after login
        const returnUrl = encodeURIComponent(currentPath);
        window.location.href = `auth-login.html?returnUrl=${returnUrl}`;
        return false;
    }

    // If authenticated and trying to access login page, redirect to dashboard
    if (authService.isAuthenticated() && currentPath.endsWith('/auth-login.html')) {
        window.location.href = 'index.html';
        return false;
    }

    return true;
}

// Global login function to work with existing HTML
function loginUser() {
    const authService = new AuthService();

    // Get username and password from input fields
    const username = document.getElementById('username').value;
    const password = document.getElementById('userpassword').value;

    // Check remember me status
    const rememberMeCheckbox = document.getElementById('customSwitchSuccess');
    const rememberMe = rememberMeCheckbox ? rememberMeCheckbox.checked : false;

    // Attempt login
    authService.loginUser(username, password, rememberMe)
        .catch(error => {
            // Error handling is done in loginUser method
            console.error('Login submission error:', error);
        });
}

// Initialize authentication on page load
document.addEventListener('DOMContentLoaded', () => {
    const authService = new AuthService();

    // Check authentication first
    checkAuthentication();

    // If on login page, prefill username if needed
    if (window.location.pathname.endsWith('/auth-login.html')) {
        authService.prefillUsername();

        // Add event listener to login form
        const loginForm = document.getElementById('loginForm');
        if (loginForm) {
            loginForm.addEventListener('submit', (e) => {
                e.preventDefault();
                loginUser();
            });
        }
    }
});