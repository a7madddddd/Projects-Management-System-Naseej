// Function to decode JWT token
// Function to decode JWT token
function decodeToken(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace('-', '+').replace('_', '/');
        return JSON.parse(window.atob(base64));
    } catch (error) {
        console.error('Error decoding token:', error);
        return null;
    }
}

// Function to check if user has admin roles
function hasAdminRole() {
    const authToken = sessionStorage.getItem('authToken');
    if (!authToken) return false;

    const decodedToken = decodeToken(authToken);
    if (!decodedToken) return false;

    // Check for role in different possible claim locations
    const roles =
        decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
        decodedToken.roles ||
        decodedToken.role;

    // Convert to array if it's not already an array
    const userRoles = Array.isArray(roles) ? roles : [roles];

    const requiredRoles = ['SuperAdmin', 'Admin', 'Editor'];

    console.log('Decoded Roles:', userRoles); // Debug log

    // Ensure case-insensitive comparison
    return userRoles.some(role =>
        requiredRoles.some(requiredRole =>
            role.toLowerCase() === requiredRole.toLowerCase()
        )
    );
}

// Function to get file icon based on file extension
function getFileIcon(fileName) {
    const extension = fileName.split('.').pop().toLowerCase();
    switch (extension) {
        case 'pdf':
            return 'fa-solid fa-file-pdf text-danger';
        case 'doc':
        case 'docx':
            return 'fa-solid fa-file-word text-primary';
        case 'xls':
        case 'xlsx':
            return 'fa-solid fa-file-excel text-success';
        case 'jpg':
        case 'jpeg':
        case 'png':
        case 'gif':
            return 'fa-solid fa-file-image text-info';
        case 'zip':
        case 'rar':
            return 'fa-solid fa-file-zipper text-warning';
        case 'sql':
            return 'fa-solid fa-database text-secondary';
        default:
            return 'fa-solid fa-file fs-18 text-blue';
    }
}
// Function to filter files based on selected time range
// Function to filter files based on selected time range
function filterFiles(files, filterType) {
    const now = new Date();

    return files.filter(file => {
        const fileDate = new Date(file.uploadDate);

        switch (filterType) {
            case 'Today':
                return isSameDay(fileDate, now);

            case 'Last Week':
                const oneWeekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
                return fileDate >= oneWeekAgo && fileDate <= now;

            case 'Last Month':
                const oneMonthAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
                return fileDate >= oneMonthAgo && fileDate <= now;

            case 'This Year':
            default:
                return fileDate.getFullYear() === now.getFullYear();
        }
    });
}

// Helper function to check if two dates are on the same day
function isSameDay(date1, date2) {
    return date1.getFullYear() === date2.getFullYear() &&
        date1.getMonth() === date2.getMonth() &&
        date1.getDate() === date2.getDate();
}

// Modified fetchFiles function to include filtering
async function fetchFiles(filterType = 'This Year') {
    try {
        const authToken = sessionStorage.getItem('authToken');
        if (!authToken) {
            throw new Error('No authentication token found');
        }

        // Fetch files
        const response = await fetch('https://localhost:44320/api/Files/list-files?PageNumber=1&PageSize=10', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Accept': '*/*'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        // Sort by upload date
        const sortedFiles = data.files.sort((a, b) => new Date(b.uploadDate) - new Date(a.uploadDate));

        // Filter files based on selected time range
        const filteredFiles = filterFiles(sortedFiles, filterType);

        // Update dropdown text ONLY in the files section
        const dropdownToggle = document.querySelector('.popular-files-dropdown .dropdown-toggle');
        if (dropdownToggle) {
            dropdownToggle.innerHTML = `
                <i class="icofont-calendar fs-5 me-1"></i> ${filterType}<i class="las la-angle-down ms-1"></i>
            `;
        }

        displayFiles(filteredFiles);
    } catch (error) {
        console.error('Error fetching files:', error);
    }
}




// Function to display files
function displayFiles(files) {
    const tableBody = document.getElementById('filesTableBody');
    tableBody.innerHTML = ''; // Clear previous entries

    const isAdmin = hasAdminRole();
    console.log('Is Admin:', isAdmin); // Debug log to check admin status

    files.forEach(file => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>
                <div class="d-flex align-items-center">
                    <i class="${getFileIcon(file.fileName)} me-3 fs-4"></i>
                    <div class="flex-grow-1 text-truncate">
                        <h6 class="m-0">${file.fileName}</h6>
                        <span class="fs-12 text-muted">ID: ${file.googleDriveFileId}</span>
                    </div>
                </div>
            </td>
            <td>${(file.fileSize / 1024).toFixed(2)} KB</td>
            <td>${file.uploadedByName}</td>
            <td>${new Date(file.uploadDate).toLocaleDateString()}</td>
            <td>
                <div class="d-flex align-items-center">
                    <a href=" ${file.webViewLink}" target="_blank" class="text-info me-2"><i class="las la-info-circle text-secondary fs-18"></i></a>
                   
                </div>
            </td>
        `;
        tableBody.appendChild(row);
    });
}

// Run on page load
// Add event listeners to dropdown items
document.addEventListener('DOMContentLoaded', () => {
    // Initial load with default 'This Year' filter
    fetchFiles();

    // Add click event listeners to dropdown items in the files section
    const dropdownItems = document.querySelectorAll('.popular-files-dropdown .dropdown-item');
    dropdownItems.forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            const filterType = e.target.textContent.trim();
            fetchFiles(filterType);
        });
    });
});



// Function to fetch dashboard statistics
async function fetchDashboardStatistics() {
    try {
        const authToken = sessionStorage.getItem('authToken');
        if (!authToken) {
            throw new Error('No authentication token found');
        }

        // Get current user ID from token or storage
        const userId = JSON.parse(atob(authToken.split('.')[1])).UserId;

        // Parallel API calls
        const [
            totalDocumentsResponse,
            categoriesResponse,
            userFilesResponse
        ] = await Promise.all([
            fetch('https://localhost:44320/api/Files/documents-count', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${authToken}`,
                    'Accept': '*/*'
                }
            }),
            fetch('https://localhost:44320/api/FileCategories', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${authToken}`,
                    'Accept': 'text/plain'
                }
            }),
            fetch(`https://localhost:44320/api/Files/user/${userId}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${authToken}`,
                    'Accept': 'text/plain'
                }
            })
        ]);

        // Parse responses
        const totalDocumentsData = await totalDocumentsResponse.json();
        const categoriesData = await categoriesResponse.json();
        const userFilesData = await userFilesResponse.json();

        // Update dashboard statistics
        updateDashboardStatistics({
            totalFiles: totalDocumentsData.totalDocuments,
            newFiles: totalDocumentsData.totalDocuments, // Assuming new files is total documents
            categories: categoriesData.length,
            userFiles: userFilesData.length,
            userFilesValue: calculateUserFilesValue(userFilesData)
        });

    } catch (error) {
        console.error('Error fetching dashboard statistics:', error);
    }
}

// Function to calculate user files value (example calculation)
function calculateUserFilesValue(files) {
    // Example: Sum of file sizes or a custom valuation logic
    return files.reduce((total, file) => total + (file.fileSize || 0), 0) / 1024; // Convert to KB
}

// Function to update dashboard statistics in the UI
function updateDashboardStatistics(stats) {
    // Total Files
    const totalFilesEl = document.querySelector('.total-files-number');
    if (totalFilesEl) {
        totalFilesEl.textContent = stats.totalFiles + ' Files ' ;
    }

    // New Files
    const newFilesEl = document.querySelector('.new-files-number');
    if (newFilesEl) {
        newFilesEl.textContent = stats.newFiles + ' Files ';
    }

    // Categories
    const categoriesEl = document.querySelector('.categories-number');
    if (categoriesEl) {
        categoriesEl.textContent = stats.categories + ' Categ ';
    }

    // User Files
    const userFilesEl = document.querySelector('.user-files-number');
    if (userFilesEl) {
        userFilesEl.textContent = stats.userFiles + ' Files ';
    }

    // User Files Value
    const userFilesValueEl = document.querySelector('.user-files-value');
    if (userFilesValueEl) {
        userFilesValueEl.textContent = `$${stats.userFilesValue.toFixed(2)}`;
    }
}

// Function to calculate percentage change (mock function, replace with actual logic)
function calculatePercentageChange(current, previous) {
    if (previous === 0) return 0;
    return ((current - previous) / previous * 100).toFixed(1);
}

// Run on page load
document.addEventListener('DOMContentLoaded', () => {
    fetchDashboardStatistics();

    // Optional: Refresh statistics periodically
    setInterval(fetchDashboardStatistics, 5 * 60 * 1000); // Every 5 minutes
});