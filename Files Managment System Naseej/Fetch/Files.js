// Utility function for API calls
async function apiCall(url, method = 'GET', body = null, headers = {}) {
    const token = sessionStorage.getItem('authToken');

    const defaultHeaders = {
        'accept': 'application/json',
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    };

    const config = {
        method,
        headers: { ...defaultHeaders, ...headers },
        ...(body && { body: JSON.stringify(body) })
    };

    try {
        const response = await fetch(url, config);

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'An error occurred');
        }

        return await response.json();
    } catch (error) {
        Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: error.message || 'Something went wrong!',
            footer: '<a href="#">Contact support if the problem persists</a>'
        });
        throw error;
    }
}

// Cached data manager
class CacheManager {
    constructor() {
        this.cache = new Map();
    }

    async get(key, fetchFunction) {
        if (this.cache.has(key)) {
            return this.cache.get(key);
        }

        const data = await fetchFunction();
        this.cache.set(key, data);
        return data;
    }

    clear() {
        this.cache.clear();
    }
}

// Global cache manager
const cacheManager = new CacheManager();


// Enhanced file fetching with caching
async function fetchUserDetails(userId) {
    return cacheManager.get(`user_${userId}`, async () => {
        return await apiCall(`https://localhost:44320/api/User/${userId}`);
    });
}

async function fetchCategoryDetails(categoryId) {
    return cacheManager.get(`category_${categoryId}`, async () => {
        return await apiCall(`https://localhost:44320/api/FileCategories/${categoryId}`);
    });
}

async function fetchFiles() {
    try {
        const files = await apiCall('https://localhost:44320/api/Files');

        // Enrich files with user and category details
        const enrichedFiles = await Promise.all(files.map(async (file) => {
            const [userData, categoryData] = await Promise.all([
                fetchUserDetails(file.uploadedBy),
                fetchCategoryDetails(file.categoryId)
            ]);

            return {
                ...file,
                uploaderUsername: userData?.username || 'Unknown',
                categoryName: categoryData?.categoryName || 'Uncategorized'
            };
        }));

        return enrichedFiles;
    } catch (error) {
        console.error('Files fetch error:', error);
        return [];
    }
}





class RoleAccessManager {
    constructor() {
        this.roles = {
            SuperAdmin: {
                permissions: [
                    'view_all_files', 'edit_all_files', 'delete_all_files',
                    'upload_files', 'download_files', 'manage_users', 'manage_roles'
                ]
            },
            Admin: {
                permissions: [
                    'view_all_files', 'edit_files', 'delete_files',
                    'upload_files', 'download_files', 'manage_users'
                ]
            },
            Editor: {
                permissions: ['view_files', 'edit_files', 'download_files']
            },
            Viewer: {
                permissions: ['view_files', 'download_files']
            },
            Uploader: {
                permissions: ['view_files', 'upload_files', 'download_files']
            }
        };

        this.currentUserRoles = [];
        this.renderFilesTable = this.renderFilesTable.bind(this);
        this.setupSearch = this.setupSearch.bind(this);
        this.setupFilters = this.setupFilters.bind(this);
        this.applyFiltersAndSearch = this.applyFiltersAndSearch.bind(this);
    }

    // Method to decode JWT token
    decodeJWT(token) {
        try {
            // Split the token and decode the payload
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace('-', '+').replace('_', '/');
            const payload = JSON.parse(window.atob(base64));

            // Extract roles from the token
            const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            // Ensure roles is always an array
            return Array.isArray(roles) ? roles : [roles];
        } catch (error) {
            console.error('Error decoding JWT:', error);
            return [];
        }
    }

    // Initialize user roles from token
    initializeUserRoles() {
        const token = sessionStorage.getItem('authToken');

        if (!token) {
            console.error('No authentication token found');
            return [];
        }

        // Decode token and set roles
        this.currentUserRoles = this.decodeJWT(token);

        // Store roles in session storage for later use
        sessionStorage.setItem('userRoles', JSON.stringify(this.currentUserRoles));

        return this.currentUserRoles;
    }

    // Check if user has a specific permission
    hasPermission(permission) {
        return this.currentUserRoles.some(role =>
            this.roles[role]?.permissions.includes(permission)
        );
    }

    // Enhanced permission check with additional context
    canPerformAction(action, resourceOwner) {
        const currentUser = sessionStorage.getItem('username');

        // If no roles are set, try to initialize
        if (this.currentUserRoles.length === 0) {
            this.initializeUserRoles();
        }

        // SuperAdmin has full access
        if (this.currentUserRoles.includes('SuperAdmin')) return true;

        switch (action) {
            case 'view_file':
                return this.hasPermission('view_files');
            case 'edit_file':
                return this.hasPermission('edit_files') &&
                    (resourceOwner === currentUser || this.currentUserRoles.includes('Admin'));
            case 'delete_file':
                return this.hasPermission('delete_files') &&
                    (resourceOwner === currentUser || this.currentUserRoles.includes('Admin'));
            case 'download_file':
                return this.hasPermission('download_files');
            case 'upload_file':
                return this.hasPermission('upload_files');
            default:
                return false;
        }
    }
    setupMultipleSearches() {
        // Get all search inputs
        const globalSearch = document.getElementById('global-search');
        const fileNameSearch = document.getElementById('filename-search');
        const userSearch = document.getElementById('user-search');

        // Combine search inputs
        const searchInputs = [
            { input: globalSearch, column: null },
            { input: fileNameSearch, column: 1 }, // File Name column index
            { input: userSearch, column: 2 }      // User column index
        ];

        // Add event listeners to search inputs
        searchInputs.forEach(({ input, column }) => {
            if (input) {
                input.addEventListener('input', () => this.performMultiSearch(searchInputs));
            }
        });
    }

    performMultiSearch(searchInputs) {
        // Get all table rows
        const rows = document.querySelectorAll('#filesTableBody tr');

        rows.forEach(row => {
            let shouldShow = true;

            // Check each search input
            searchInputs.forEach(({ input, column }) => {
                if (!input) return;

                const searchTerm = input.value.toLowerCase().trim();

                // If no search term, skip this check
                if (searchTerm === '') return;

                // Global search (check entire row)
                if (column === null) {
                    const rowText = row.textContent.toLowerCase();
                    if (!rowText.includes(searchTerm)) {
                        shouldShow = false;
                    }
                } else {
                    // Specific column search
                    const cellText = row.cells[column].textContent.toLowerCase();
                    if (!cellText.includes(searchTerm)) {
                        shouldShow = false;
                    }
                }
            });

            // Apply visibility
            row.style.display = shouldShow ? '' : 'none';
        });
    }



    setupSearch() {
        // Create search input dynamically if not already present
        let searchContainer = document.querySelector('.search-container');
        if (!searchContainer) {
            searchContainer = document.createElement('div');
            searchContainer.classList.add('search-container', 'col-auto');

            const searchInput = document.createElement('input');
            searchInput.type = 'text';
            searchInput.id = 'global-search';
            searchInput.classList.add('form-control');
            searchInput.placeholder = 'Search files...';

            searchContainer.appendChild(searchInput);

            // Insert search input before the filter dropdown
            const filterDropdown = document.querySelector('.bg-primary-subtle');
            if (filterDropdown && filterDropdown.parentNode) {
                filterDropdown.parentNode.parentNode.insertBefore(searchContainer, filterDropdown.parentNode);
            }
        }

        // Add event listener for search
        const searchInput = document.getElementById('global-search');
        if (searchInput) {
            searchInput.addEventListener('input', this.applyFiltersAndSearch);
        }
    }

    setupFilters() {
        // Get all filter checkboxes
        const filterAll = document.getElementById('filter-all');
        const filterNew = document.getElementById('filter-one');
        const filterActive = document.getElementById('filter-two');
        const filterInactive = document.getElementById('filter-three');

        // Add event listeners to checkboxes
        [filterAll, filterNew, filterActive, filterInactive].forEach(checkbox => {
            if (checkbox) {
                checkbox.addEventListener('change', this.applyFiltersAndSearch);
            }
        });

        // Initial setup to handle "All" checkbox behavior
        if (filterAll) {
            filterAll.addEventListener('change', (e) => {
                const isChecked = e.target.checked;
                const otherCheckboxes = [filterNew, filterActive, filterInactive];

                otherCheckboxes.forEach(checkbox => {
                    if (checkbox) {
                        checkbox.checked = isChecked;
                    }
                });

                this.applyFiltersAndSearch();
            });
        }
    }

    applyFiltersAndSearch() {
        // Get all filter checkboxes
        const filterAll = document.getElementById('filter-all');
        const filterNew = document.getElementById('filter-one');
        const filterActive = document.getElementById('filter-two');
        const filterInactive = document.getElementById('filter-three');
        const searchInput = document.getElementById('global-search');

        // Get all table rows
        const rows = document.querySelectorAll('#filesTableBody tr');

        rows.forEach(row => {
            // Check search condition
            const searchTerm = searchInput ? searchInput.value.toLowerCase().trim() : '';
            const matchesSearch = !searchInput || row.textContent.toLowerCase().includes(searchTerm);

            // Check filter condition
            const statusBadge = row.querySelector('.badge');
            const isActive = statusBadge.classList.contains('bg-success-subtle');
            const isInactive = statusBadge.classList.contains('bg-secondary-subtle');

            let matchesFilter = false;

            // If all is checked, show all rows
            if (filterAll.checked) {
                matchesFilter = true;
            } else {
                // Check individual filter conditions
                if (filterNew.checked && isActive) {
                    matchesFilter = true;
                }
                if (filterActive.checked && isActive) {
                    matchesFilter = true;
                }
                if (filterInactive.checked && isInactive) {
                    matchesFilter = true;
                }
            }

            // Apply visibility based on both search and filter
            row.style.display = (matchesSearch && matchesFilter) ? '' : 'none';
        });
    }




    renderFilesTable(files) {
        // Ensure files is an array
        if (!Array.isArray(files)) {
            console.error('Invalid files data:', files);
            return;
        }

        // Check if the table body exists
        const tableBody = document.getElementById('filesTableBody');
        if (!tableBody) {
            console.error('Files table body not found');
            return;
        }

        // Clear existing rows
        tableBody.innerHTML = '';

        files.forEach((file, index) => {
            // Check user permissions for this file
            const canView = this.canPerformAction('view_file', file.uploaderUsername);
            const canEdit = this.canPerformAction('edit_file', file.uploaderUsername);
            const canDelete = this.canPerformAction('delete_file', file.uploaderUsername);
            const canDownload = this.canPerformAction('download_file');

            // Skip files user cannot view
            if (!canView) return;

            const row = document.createElement('tr');
            row.innerHTML = `
            <td style="width: 16px;">
                <div class="form-check">
                    <input type="checkbox" class="form-check-input" name="check" id="customCheck${index}">
                </div>
            </td>
            <td class="ps-0">
                <p class="d-inline-block align-middle mb-0">
                    <span class="font-13 fw-medium">${file.fileName}${file.fileExtension}</span>
                </p>
            </td>
            <td>${file.uploaderUsername}</td>
            <td>${new Date(file.uploadDate).toLocaleDateString()}</td>
            <td>
                <span class="badge ${file.isActive ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}">
                    ${file.isActive ? 'Active' : 'Inactive'}
                </span>
            </td>
            <td>${file.categoryName}</td>
            <td>${file.isPublic ? 'Yes' : 'No'}</td>
            <td class="text-end">
                <a href="#" onclick="FileActions.viewFileDetails(${file.fileId})">
                    <i class="las la-info-circle text-secondary fs-18"></i>
                </a>
                ${canEdit ? `
                    <a href="#" onclick="FileActions.editFile(${file.fileId})">
                        <i class="las la-pen text-secondary fs-18"></i>
                    </a>
                ` : ''}
                ${canDelete ? `
                    <a href="#" onclick="FileActions.deleteFile(${file.fileId})">
                        <i class="las la-trash-alt text-secondary fs-18"></i>
                    </a>
                ` : ''}
                ${canDownload ? `
                    <a href="#" onclick="FileActions.downloadFile(${file.fileId}, '${file.fileName}', '${file.fileExtension}')">
                        <i class="las la-download text-secondary fs-18"></i>
                    </a>
                ` : ''}
            </td>
        `;

            tableBody.appendChild(row);
        });

        // Optional: Apply additional table features
        this.applyTableFeatures();
        this.applyFilters();
        this.setupMultipleSearches();
        this.setupSearch();
        this.setupFilters();
    }

    applyFilters() {
        // Get all filter checkboxes
        const filterAll = document.getElementById('filter-all');
        const filterNew = document.getElementById('filter-one');
        const filterActive = document.getElementById('filter-two');
        const filterInactive = document.getElementById('filter-three');

        // Get all table rows
        const rows = document.querySelectorAll('#filesTableBody tr');

        // Add event listeners to checkboxes
        [filterAll, filterNew, filterActive, filterInactive].forEach(checkbox => {
            checkbox.addEventListener('change', this.filterTable.bind(this));
        });

        // Initial filter application
        this.filterTable();
    }
    filterTable() {
        // Get all filter checkboxes
        const filterAll = document.getElementById('filter-all');
        const filterNew = document.getElementById('filter-one');
        const filterActive = document.getElementById('filter-two');
        const filterInactive = document.getElementById('filter-three');

        // Get all table rows
        const rows = document.querySelectorAll('#filesTableBody tr');

        // If all is checked, show all rows
        if (filterAll.checked) {
            rows.forEach(row => row.style.display = '');
            return;
        }

        rows.forEach(row => {
            // Find the status badge in the row
            const statusBadge = row.querySelector('.badge');
            const isActive = statusBadge.classList.contains('bg-success-subtle');
            const isInactive = statusBadge.classList.contains('bg-secondary-subtle');

            // Determine visibility based on checked filters
            let shouldShow = false;

            if (filterNew.checked && isActive) {
                shouldShow = true;
            }

            if (filterActive.checked && isActive) {
                shouldShow = true;
            }

            if (filterInactive.checked && isInactive) {
                shouldShow = true;
            }

            // Apply visibility
            row.style.display = shouldShow ? '' : 'none';
        });
    }

    // Optional method to apply additional table features
    applyTableFeatures() {
        // Existing search functionality
        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.placeholder = 'Search files...';
        searchInput.classList.add('form-control', 'mb-3');

        // Insert search input before table
        const tableContainer = document.getElementById('filesTableContainer');
        if (tableContainer) {
            tableContainer.insertBefore(searchInput, document.getElementById('filesTable'));

            // Search logic
            searchInput.addEventListener('input', (e) => {
                const searchTerm = e.target.value.toLowerCase();
                const rows = document.querySelectorAll('#filesTableBody tr');

                rows.forEach(row => {
                    const rowText = row.textContent.toLowerCase();
                    row.style.display = rowText.includes(searchTerm) ? '' : 'none';
                });
            });
        }

        // Apply filters
        this.applyFilters();
        this.setupMultipleSearches();
        this.setupSearch();
        this.setupFilters();
    }
}



// Modify the initialization to ensure roles are set
document.addEventListener('DOMContentLoaded', async () => {
    try {
        // Create global role manager
        window.roleManager = new RoleAccessManager();

        // Initialize user roles from token
        const userRoles = window.roleManager.initializeUserRoles();

        // Log roles for debugging
        console.log('Initialized User Roles:', userRoles);

        // Fetch and render files
        await FileActions.renderFilesTable();
    } catch (error) {
        console.error('Initialization error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Initialization Failed',
            text: 'Unable to load application resources.',
            footer: `Error: ${error.message}`
        });
    }
});

// Optional: Add a reset search function
function resetSearches() {
    // Clear all search inputs
    const searchInputs = [
        document.getElementById('global-search'),
        document.getElementById('filename-search'),
        document.getElementById('user-search')
    ];

    searchInputs.forEach(input => {
        if (input) input.value = '';
    });

    // Show all rows
    const rows = document.querySelectorAll('#filesTableBody tr');
    rows.forEach(row => row.style.display = '');
} constructor

// Modify file actions to include role check
// Modify FileActions to be a more robust object
const FileActions = {
    async renderFilesTable() {
        try {
            // Ensure we have a role manager
            if (!window.roleManager) {
                throw new Error('Role manager not initialized');
            }

            // Fetch files
            const files = await fetchFiles();

            // Use role manager's render method
            window.roleManager.renderFilesTable(files);
        } catch (error) {
            console.error('Error rendering files table:', error);
            Swal.fire({
                icon: 'error',
                title: 'Rendering Failed',
                text: 'Unable to render files table.',
                footer: `Error: ${error.message}`
            });
        }
    },
    async fetchCategories() {
        try {
            const categories = await apiCall('https://localhost:44320/api/FileCategories');
            const categorySelect = document.getElementById('categorySelect');

            // Clear existing options
            categorySelect.innerHTML = '<option value="">Select Category</option>';

            // Populate categories
            categories.forEach(category => {
                const option = document.createElement('option');
                option.value = category.categoryId;
                option.textContent = category.categoryName;
                categorySelect.appendChild(option);
            });
        } catch (error) {
            console.error('Error fetching categories:', error);
            Swal.fire({
                icon: 'error',
                title: 'Categories Load Failed',
                text: 'Unable to load file categories.'
            });
        }
    },
    // Existing file action methods
    async viewFileDetails(fileId) {
        // Check permission before proceeding
        if (!window.roleManager.canPerformAction('view_file')) {
            Swal.fire({
                icon: 'error',
                title: 'Permission Denied',
                text: 'You do not have permission to view file details.',
                footer: `Your current roles: ${window.roleManager.currentUserRoles.join(', ')}`
            });
            return;
        }

        try {
            const fileDetails = await apiCall(`https://localhost:44320/api/Files/${fileId}`);

            Swal.fire({
                title: 'File Details',
                html: `
                    <div class="text-start">
                        <p><strong>File Name:</strong> ${fileDetails.fileName}</p>
                        <p><strong>Extension:</strong> ${fileDetails.fileExtension}</p>
                        <p><strong>Uploaded By:</strong> ${fileDetails.uploadedBy}</p>
                        <p><strong>Upload Date:</strong> ${new Date(fileDetails.uploadDate).toLocaleString()}</p>
                    </div>
                `,
                icon: 'info'
            });
        } catch (error) {
            console.error('View file details error:', error);
        }
    },
    async uploadFile() {
        // Check upload permission
        if (!window.roleManager.canPerformAction('upload_file')) {
            Swal.fire({
                icon: 'error',
                title: 'Permission Denied',
                text: 'You do not have permission to upload files.'
            });
            return;
        }

        // Get form and file input
        const form = document.getElementById('fileUploadForm');
        const fileInput = document.getElementById('fileInput');
        const fileName = document.getElementById('fileName').value;
        const categorySelect = document.getElementById('categorySelect');
        const isPublic = document.getElementById('isPublic').checked;

        // Validate form
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        // Check if file is selected
        if (fileInput.files.length === 0) {
            Swal.fire({
                icon: 'error',
                title: 'File Missing',
                text: 'Please select a file to upload.'
            });
            return;
        }

        // Create FormData
        const formData = new FormData();
        formData.append('file', fileInput.files[0]);
        formData.append('FileName', fileName);
        formData.append('CategoryId', categorySelect.value);
        formData.append('IsPublic', isPublic);

        // Get user ID from session or token
        const userId = sessionStorage.getItem('userId') || '2'; // Default to 2 if not found
        formData.append('UserId', userId);

        try {
            // Show loading indicator
            Swal.fire({
                title: 'Uploading...',
                html: 'Please wait while the file is being uploaded',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Perform file upload with detailed error handling
            const token = sessionStorage.getItem('authToken');
            const response = await fetch('https://localhost:44320/api/Files', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                body: formData
            });

            // Detailed error handling
            if (!response.ok) {
                let errorDetails = 'Unknown error occurred';
                try {
                    const errorData = await response.text(); // Use text() instead of json()
                    console.error('Error response:', errorData);

                    // Try to parse as JSON if possible
                    try {
                        const parsedError = JSON.parse(errorData);
                        errorDetails = parsedError.title ||
                            (parsedError.errors ?
                                Object.values(parsedError.errors).flat().join(', ') :
                                errorDetails);
                    } catch {
                        errorDetails = errorData;
                    }
                } catch (parseError) {
                    console.error('Error parsing error response:', parseError);
                }

                throw new Error(errorDetails);
            }

            // Parse successful response
            const responseData = await response.json();

            // Success handling
            Swal.fire({
                icon: 'success',
                title: 'File Uploaded',
                text: 'Your file has been successfully uploaded.'
            });

            // Refresh files table
            await this.renderFilesTable();

            // Close modal
            const modalElement = document.getElementById('addBoard');
            if (modalElement) {
                const modalInstance = bootstrap.Modal.getInstance(modalElement);
                if (modalInstance) {
                    modalInstance.hide();
                } else {
                    // Fallback to data-bs-dismiss method
                    $(modalElement).modal('hide');
                }
            }

            return responseData;

        } catch (error) {
            console.error('File upload error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Upload Failed',
                text: error.message || 'Unable to upload file.',
                footer: `Error Details: ${error.message}`
            });

            throw error;
        }
    },


    async editFile(fileId) {
        // Check edit permission
        if (!window.roleManager.canPerformAction('edit_file')) {
            Swal.fire({
                icon: 'error',
                title: 'Permission Denied',
                text: 'You do not have permission to edit files.'
            });
            return;
        }

        Swal.fire({
            title: 'Edit File',
            input: 'text',
            inputLabel: 'New File Name',
            inputPlaceholder: 'Enter new file name',
            showCancelButton: true,
            confirmButtonText: 'Update',
            preConfirm: async (newFileName) => {
                try {
                    await apiCall(`https://localhost:44320/api/Files/${fileId}`, 'PUT', {
                        fileName: newFileName
                    });

                    Swal.fire({
                        icon: 'success',
                        title: 'File Updated',
                        text: 'File name has been updated successfully!'
                    });

                    // Refresh files list
                    await this.renderFilesTable();
                } catch (error) {
                    Swal.showValidationMessage(`Request failed: ${error.message}`);
                }
            }
        });
    },

    async deleteFile(fileId) {
        // Check delete permission
        if (!window.roleManager.canPerformAction('delete_file')) {
            Swal.fire({
                icon: 'error',
                title: 'Permission Denied',
                text: 'You do not have permission to delete files.'
            });
            return;
        }

        Swal.fire({
            title: 'Are you sure?',
            text: 'You won\'t be able to revert this!',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, delete it!'
        }).then(async (result) => {
            if (result.isConfirmed) {
                try {
                    await apiCall(`https://localhost:44320/api/Files/${fileId}`, 'DELETE');

                    Swal.fire({
                        icon: 'success',
                        title: 'Deleted!',
                        text: 'The file has been deleted.'
                    });

                    // Refresh files list
                    await this.renderFilesTable();
                } catch (error) {
                    console.error('Delete file error:', error);
                }
            }
        });
    },

    async downloadFile(fileId, fileName, fileExtension) {
        // Check download permission
        if (!window.roleManager.canPerformAction('download_file')) {
            Swal.fire({
                icon: 'error',
                title: 'Permission Denied',
                text: 'You do not have permission to download files.'
            });
            return;
        }

        try {
            const blob = await apiCall(`https://localhost:44320/api/Files/download/${fileId}`, 'GET', null, {
                'Accept': 'application/octet-stream'
            });

            const downloadLink = document.createElement('a');
            downloadLink.href = window.URL.createObjectURL(new Blob([blob]));
            downloadLink.download = `${fileName}${fileExtension}`;

            document.body.appendChild(downloadLink);
            downloadLink.click();
            document.body.removeChild(downloadLink);

            Swal.fire({
                icon: 'success',
                title: 'Download Started',
                text: `Downloading ${fileName}${fileExtension}`
            });
        } catch (error) {
            console.error('Download error:', error);
        }
    }
};

// Initialization with improved error handling
// Initialization and setup
document.addEventListener('DOMContentLoaded', async () => {
    try {
        // Create global role manager
        window.roleManager = new RoleAccessManager();

        // Initialize user roles from token
        const userRoles = window.roleManager.initializeUserRoles();

        // Log roles for debugging
        console.log('Initialized User Roles:', userRoles);

        // Fetch and render files
        await FileActions.renderFilesTable();

        // Fetch categories
        await FileActions.fetchCategories();

        // Setup file input listener
        const fileInput = document.getElementById('fileInput');
        const fileNameInput = document.getElementById('fileName');

        if (fileInput && fileNameInput) {
            fileInput.addEventListener('change', (e) => {
                const file = e.target.files[0];
                if (file) {
                    fileNameInput.value = file.name;
                }
            });
        }



    } catch (error) {
        console.error('Initialization error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Initialization Failed',
            text: 'Unable to load application resources.',
            footer: `Error: ${error.message}`
        });
    }
});

function resetFiltersAndSearch() {
    // Reset checkboxes
    const filterAll = document.getElementById('filter-all');
    const filterNew = document.getElementById('filter-one');
    const filterActive = document.getElementById('filter-two');
    const filterInactive = document.getElementById('filter-three');
    const searchInput = document.getElementById('global-search');

    if (filterAll) filterAll.checked = true;
    if (filterNew) filterNew.checked = true;
    if (filterActive) filterActive.checked = true;
    if (filterInactive) filterInactive.checked = true;
    if (searchInput) searchInput.value = '';

    // Trigger filter and search
    if (window.roleManager) {
        window.roleManager.applyFiltersAndSearch();
    }
}

// Initialization
