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















document.addEventListener('DOMContentLoaded', function () {
    const notificationContainer = document.querySelector('#All');
    const notificationBadge = document.querySelector('.badge.bg-primary-subtle');
    const viewAllNotificationsBtn = document.querySelector('.dropdown-item.text-center');

    // Fetch recent files
    function fetchRecentFiles() {
        const token = sessionStorage.getItem('authToken');

        fetch('https://localhost:44320/api/Files/list-files?PageNumber=1&PageSize=10', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'accept': '*/*'
            }
        })
            .then(response => response.json())
            .then(data => {
                // Filter files from last 2 days
                const twoDaysAgo = new Date();
                twoDaysAgo.setDate(twoDaysAgo.getDate() - 2);

                const recentFiles = data.files.filter(file => {
                    const uploadDate = new Date(file.uploadDate);
                    return uploadDate >= twoDaysAgo;
                });

                // Update notification badge
                notificationBadge.textContent = recentFiles.length;

                // Clear existing notifications
                notificationContainer.innerHTML = '';

                // Render recent files
                recentFiles.forEach(file => {
                    const notificationItem = createNotificationElement(file);
                    notificationContainer.appendChild(notificationItem);
                });

                // Store notifications for view all functionality
                localStorage.setItem('recentNotifications', JSON.stringify(recentFiles));
            })
            .catch(error => {
                console.error('Error fetching notifications:', error);
            });
    }

    // Create notification element
    function createNotificationElement(file) {
        const notificationItem = document.createElement('a');
        notificationItem.href = '#';
        notificationItem.className = 'dropdown-item py-3';

        // Calculate time ago
        const timeAgo = formatTimeAgo(new Date(file.uploadDate));

        // Set notification content
        notificationItem.innerHTML = `
            <small class="float-end text-muted ps-2">${timeAgo}</small>
            <div class="d-flex align-items-center">
                <div class="flex-shrink-0 bg-primary-subtle text-primary thumb-md rounded-circle">
                    <i class="${getFileIcon(file.fileExtension)} fs-4"></i>
                </div>
                <div class="flex-grow-1 ms-2 text-truncate">
                    <h6 class="my-0 fw-normal text-dark fs-13">New File Uploaded</h6>
                    <small class="text-muted mb-0">${file.fileName}</small>
                </div>
            </div>
        `;

        return notificationItem;
    }

    // Format time ago
    function formatTimeAgo(date) {
        const now = new Date();
        const diffInMinutes = Math.round((now - date) / (1000 * 60));

        if (diffInMinutes < 60) return `${diffInMinutes} min ago`;
        if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)} hours ago`;
        return `${Math.floor(diffInMinutes / 1440)} days ago`;
    }

    // Get file icon based on extension
    function getFileIcon(extension) {
        const iconMap = {
            '.pdf': 'fa-solid fa-file-pdf',
            '.doc': 'fa-solid fa-file-word',
            '.docx': 'fa-solid fa-file-word',
            '.xls': 'fa-solid fa-file-excel',
            '.xlsx': 'fa-solid fa-file-excel',
            '.ppt': 'fa-solid fa-file-powerpoint',
            '.pptx': 'fa-solid fa-file-powerpoint',
            '.txt': 'fa-solid fa-file-alt',
            '.jpg': 'fa-solid fa-file-image',
            '.jpeg': 'fa-solid fa-file-image',
            '.png': 'fa-solid fa-file-image',
            'default': 'fa-solid fa-file'
        };

        return iconMap[extension] || iconMap['default'];
    }

    // View All Notifications Modal
    function createNotificationsModal() {
        // Create modal HTML
        const modalHtml = `
            <div class="modal fade" id="notificationsModal" tabindex="-1" aria-labelledby="notificationsModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="notificationsModalLabel">All Notifications</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <table class="table table-striped">
                                <thead>
                                    <tr>
                                        <th>File Type</th>
                                        <th>File Name</th>
                                        <th>Uploaded By</th>
                                        <th>Upload Date</th>
                                    </tr>
                                </thead>
                                <tbody id="notificationsTableBody">
                                    <!-- Notifications will be inserted here -->
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Append modal to body if not exists
        if (!document.getElementById('notificationsModal')) {
            const modalContainer = document.createElement('div');
            modalContainer.innerHTML = modalHtml;
            document.body.appendChild(modalContainer);
        }

        // Add event listener to view all button
        viewAllNotificationsBtn.addEventListener('click', () => {
            const notifications = JSON.parse(localStorage.getItem('recentNotifications') || '[]');
            const tableBody = document.getElementById('notificationsTableBody');

            // Clear previous entries
            tableBody.innerHTML = '';

            // Populate table
            notifications.forEach(file => {
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td><i class="${getFileIcon(file.fileExtension)} me-2"></i>${file.fileExtension}</td>
                    <td>${file.fileName}</td>
                    <td>${file.uploadedByName}</td>
                    <td>${formatTimeAgo(new Date(file.uploadDate))}</td>
                `;
                tableBody.appendChild(row);
            });

            // Show modal
            const modal = new bootstrap.Modal(document.getElementById('notificationsModal'));
            modal.show();
        });
    }

    // Initial fetch
    fetchRecentFiles();

    // Create notifications modal
    createNotificationsModal();

    // Optional: Refresh every 5 minutes
    setInterval(fetchRecentFiles, 5 * 60 * 1000);
});
















document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.querySelector('input[placeholder="Search here..."]');
    const token = sessionStorage.getItem('authToken');

    // Debounce function
    function debounce(func, delay) {
        let timeoutId;
        return function () {
            const context = this;
            const args = arguments;
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                func.apply(context, args);
            }, delay);
        };
    }

    // Create search results container
    function createSearchResultsContainer() {
        let container = document.getElementById('searchResultsContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'searchResultsContainer';
            container.className = 'search-results-container';
            container.style.cssText = `
                position: absolute;
                top: 100%;
                left: 0;
                width: 100%;
                background: #1e1e2d;
                border: 1px solid #2c2c3a;
                border-top: none;
                z-index: 1000;
                display: none;
                border-radius: 0 0 8px 8px;
                box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            `;
            searchInput.parentElement.style.position = 'relative';
            searchInput.parentElement.appendChild(container);
        }
        return container;
    }

    // Perform search
    const performSearch = debounce(function () {
        const searchTerm = searchInput.value.trim().toLowerCase();

        if (searchTerm.length < 2) {
            const container = document.getElementById('searchResultsContainer');
            if (container) container.style.display = 'none';
            return;
        }

        // Fetch roles for search
        fetch('https://localhost:44320/api/Roles', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'accept': 'text/plain'
            }
        })
            .then(response => response.json())
            .then(roles => {
                // Filter roles based on search term
                const filteredRoles = roles.filter(role =>
                    role.roleName.toLowerCase().includes(searchTerm) ||
                    role.description.toLowerCase().includes(searchTerm)
                );

                // Render search results
                renderSearchResults(filteredRoles);
            })
            .catch(error => {
                console.error('Search error:', error);
            });
    }, 300);

    // Render search results
    function renderSearchResults(results) {
        const container = createSearchResultsContainer();
        container.innerHTML = ''; // Clear previous results
        container.style.display = results.length ? 'block' : 'none';

        // Render results
        results.forEach(role => {
            const resultItem = document.createElement('div');
            resultItem.className = 'search-result-item';
            resultItem.style.cssText = `
                display: flex;
                align-items: center;
                padding: 10px;
                border-bottom: 1px solid #2c2c3a;
                cursor: pointer;
            `;

            resultItem.innerHTML = `
                <div class="result-icon me-3">
                    <i class="fas fa-file text-primary"></i>
                </div>
                <div class="result-content">
                    <h6 class="m-0 text-white">${role.roleName}</h6>
                    <small class="text-muted">${role.description}</small>
                </div>
            `;

            // Click handler
            resultItem.addEventListener('click', () => {
                console.log('Selected role:', role);
            });

            container.appendChild(resultItem);
        });

        // Close dropdown when clicking outside
        function closeDropdownHandler(e) {
            if (!container.contains(e.target) && e.target !== searchInput) {
                container.style.display = 'none';
                document.removeEventListener('click', closeDropdownHandler);
            }
        }

        // Add delay to prevent immediate closure
        setTimeout(() => {
            document.addEventListener('click', closeDropdownHandler);
        }, 0);
    }

    // Add event listener to search input
    searchInput.addEventListener('input', performSearch);
});