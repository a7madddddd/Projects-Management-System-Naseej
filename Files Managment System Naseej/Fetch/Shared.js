function checkAuthentication() {
    const token = sessionStorage.getItem('authToken');
    const currentPage = window.location.pathname.split('/').pop();

    // List of pages that should be accessible without authentication
    const publicPages = ['auth-login.html', 'auth-recover-pw.html'];

    // If no token and not on a public page, redirect to login
    if (!token && !publicPages.includes(currentPage)) {
        window.location.href = 'auth-login.html';
    }

    // Optional: Redirect authenticated users away from login page
    if (token && currentPage === 'auth-login.html') {
        window.location.href = 'index.html'; // or your default authenticated page
    }
}

// Perform initial check on page load
document.addEventListener('DOMContentLoaded', checkAuthentication);

// Check authentication every second
setInterval(checkAuthentication, 1000);







class HeaderService {
    constructor() {
        this.baseUrl = 'https://localhost:44320/api';
        this.token = sessionStorage.getItem('authToken');
    }

    getHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`,
            'Content-Type': 'application/json'
        };
    }

    // Extract user ID from token
    getUserIdFromToken() {
        try {
            const token = this.token;
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace('-', '+').replace('_', '/');
            const payload = JSON.parse(window.atob(base64));
            return payload.UserId;
        } catch (error) {
            console.error('Error decoding token:', error);
            return null;
        }
    }

    async fetchUserProfile() {
        try {
            const userId = this.getUserIdFromToken();
            if (!userId) {
                throw new Error('Unable to extract user ID');
            }

            const response = await fetch(`${this.baseUrl}/User/${userId}`, {
                method: 'GET',
                headers: this.getHeaders()
            });

            if (!response.ok) {
                throw new Error('Failed to fetch user profile');
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching user profile:', error);
            throw error;
        }
    }

    async fetchGreeting() {
        try {
            const response = await fetch(`${this.baseUrl}/User/greeting`, {
                method: 'GET',
                headers: this.getHeaders()
            });

            if (!response.ok) {
                throw new Error('Failed to fetch greeting');
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching greeting:', error);
            return this.getDefaultGreeting();
        }
    }

    getDefaultGreeting() {
        const hour = new Date().getHours();
        let greeting = 'Good Morning';

        if (hour >= 12 && hour < 17) {
            greeting = 'Good Afternoon';
        } else if (hour >= 17) {
            greeting = 'Good Evening';
        }

        return {
            greeting: greeting,
            currentTime: new Date().toISOString()
        };
    }

    async updateHeader() {
        try {
            // Fetch user profile and greeting concurrently
            const [profile, greeting] = await Promise.all([
                this.fetchUserProfile(),
                this.fetchGreeting()
            ]);

            // Update welcome text
            const welcomeTextElement = document.querySelector('.welcome-text h5');
            if (welcomeTextElement) {
                welcomeTextElement.textContent = `${greeting.greeting}, ${profile.firstName}!`;
            }

            // Update profile dropdown using IDs
            const userNameElement = document.getElementById('userName');
            const userRoleElement = document.getElementById('UserRole');

            if (userNameElement) {
                userNameElement.textContent = `${profile.firstName} ${profile.lastName}`;
            }

            if (userRoleElement) {
                userRoleElement.textContent = profile.roles[0] || 'User';
            }

        } catch (error) {
            console.error('Error updating header:', error);
            // Fallback to default values
            const welcomeTextElement = document.querySelector('.welcome-text h5');
            if (welcomeTextElement) {
                welcomeTextElement.textContent = 'Good Morning, User!';
            }
        }
    }

    // Logout method
    logout() {
        // Remove authentication token
        sessionStorage.removeItem('authToken');

        // Optional: Clear other session-related data
        sessionStorage.clear();

        // Redirect to login page
        window.location.href = 'auth-login.html';
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    const headerService = new HeaderService();
    headerService.updateHeader();

    // Add logout event listener
    const logoutButton = document.querySelector('.logout-btn');
    if (logoutButton) {
        logoutButton.addEventListener('click', (e) => {
            e.preventDefault(); // Prevent default link behavior

            // Show confirmation dialog
            Swal.fire({
                title: 'Logout',
                text: 'Are you sure you want to log out?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, logout'
            }).then((result) => {
                if (result.isConfirmed) {
                    headerService.logout();
                }
            });
        });
    }
});