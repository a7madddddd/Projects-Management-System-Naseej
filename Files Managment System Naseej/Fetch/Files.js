
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
    if (categoryId === null) {
        return null;
    }
    return cacheManager.get(`category_${categoryId}`, async () => {
        try {
            return await apiCall(`https://localhost:44320/api/FileCategories/${categoryId}`);
        } catch (error) {
            console.error(`Error fetching category ${categoryId}:`, error);
            return null;
        }
    });
}

async function fetchFiles() {
    try {
        const files = await apiCall('https://localhost:44320/api/Files');

        // Enrich files with user and category details
        const enrichedFiles = await Promise.all(files.map(async (file) => {
            const [userData, categoryData] = await Promise.all([
                fetchUserDetails(file.uploadedBy),
                file.categoryId !== null ? fetchCategoryDetails(file.categoryId) : Promise.resolve(null)
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
            return categories;
        } catch (error) {
            console.error('Error fetching categories:', error);
            Swal.fire({
                icon: 'error',
                title: 'Categories Load Failed',
                text: 'Unable to load file categories.'
            });
            return [];
        }
    },
    async convertFile(fileId, targetExtension) {
        try {
            // Check permission
            if (!window.roleManager.canPerformAction('edit_file')) {
                Swal.fire({
                    icon: 'error',
                    title: 'Permission Denied',
                    text: 'You do not have permission to convert files.'
                });
                return;
            }

            // Show loading
            Swal.fire({
                title: 'Converting File...',
                html: 'Please wait while the file is being converted',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Perform conversion
            const response = await fetch(`https://localhost:44320/api/Files/${fileId}/convert`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
                },
                body: JSON.stringify(targetExtension)
            });

            // Handle response
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'File conversion failed');
            }

            const convertedFile = await response.json();

            // Success notification
            Swal.fire({
                icon: 'success',
                title: 'File Converted',
                text: `File converted to ${convertedFile.targetExtension}`,
                footer: `New filename: ${convertedFile.convertedFileName}`
            });

            // Refresh files table
            await this.renderFilesTable();

        } catch (error) {
            console.error('File conversion error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Conversion Failed',
                text: error.message || 'Unable to convert file.'
            });
        }
    },
    // Existing file action methods
    async viewFileDetails(fileId) {
        try {
            // Fetch file details
            const fileDetails = await apiCall(`https://localhost:44320/api/Files/${fileId}`);

            // Fetch categories to get category name
            const categories = await this.fetchCategories();

            // Find category name
            const category = categories.find(cat => cat.categoryId === fileDetails.categoryId);
            const categoryName = category ? category.categoryName : 'Uncategorized';

            // Create a modal with comprehensive file information and preview options
            Swal.fire({
                title: 'File Details',
                html: `
                    <div class="text-start">
                        <p><strong>File Name:</strong> ${fileDetails.fileName}${fileDetails.fileExtension}</p>
                        <p><strong>Uploaded By:</strong> ${fileDetails.uploadedBy}</p>
                        <p><strong>Upload Date:</strong> ${new Date(fileDetails.uploadDate).toLocaleString()}</p>
                        <p><strong>File Size:</strong> ${this.formatFileSize(fileDetails.fileSize)}</p>
                        <p><strong>Category:</strong> ${categoryName}</p>
                        <p><strong>Public:</strong> ${fileDetails.isPublic ? 'Yes' : 'No'}</p>
                    </div>
                `,
                showCloseButton: true,
                showCancelButton: true,
                confirmButtonText: 'Open File',
                cancelButtonText: 'Download',
                preConfirm: () => {
                    // Open file in browser
                    this.openFileInBrowser(fileId, fileDetails.fileName, fileDetails.fileExtension);
                    return false; // Prevent modal closing
                }
            }).then((result) => {
                // Handle download if cancel button is clicked
                if (result.dismiss === Swal.DismissReason.cancel) {
                    this.downloadFile(fileId, fileDetails.fileName, fileDetails.fileExtension);
                }
            });

        } catch (error) {
            console.error('View file details error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Unable to retrieve file details.'
            });
        }
    },

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    },

    // Method to open file in browser
    async openFileInBrowser(fileId, fileName, fileExtension) {
        try {
            const fileDetails = await apiCall(`https://localhost:44320/api/Files/${fileId}`);
            const fullFileName = fileDetails.fileName + fileDetails.fileExtension;

            // Check if it's an Excel file
            if (fileExtension === '.xlsx' || fileExtension === '.xls') {
                // Fetch the file as a blob
                const response = await fetch(`https://localhost:44320/api/Files/serve/${encodeURIComponent(fullFileName)}?view=true`);
                const blob = await response.blob();

                // Create a modal for Excel viewer
                this.createExcelViewerModal(blob, fileName, fileId);
            } else {
                // For other files, open normally
                const fileUrl = `https://localhost:44320/api/Files/serve/${encodeURIComponent(fullFileName)}?view=true`;
                window.open(fileUrl, '_blank');
            }
        } catch (error) {
            console.error('Open file error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Open File Failed',
                text: 'Unable to open file in browser.',
                footer: `Error: ${error.message}`
            });
        }
    },



    allowedExtensions: ['.pdf', '.xlsx', '.xls', '.docx', '.txt'],
    maxFileSize: 10 * 1024 * 1024, // 10 MB

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

        // Get the selected file
        const file = fileInput.files[0];

        // Client-side file validation
        try {
            this.validateFile(file);
        } catch (error) {
            Swal.fire({
                icon: 'error',
                title: 'Validation Error',
                text: error.message
            });
            return;
        }

        // Create FormData
        const formData = new FormData();
        formData.append('file', file);
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

            // Perform file upload
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
                    const errorData = await response.text();
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

    // Client-side file validation method
    validateFile(file) {
        // Check file size
        if (file.size > this.maxFileSize) {
            throw new Error('File size exceeds the maximum allowed size of 10 MB.');
        }

        // Check file extension
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
        if (!this.allowedExtensions.includes(fileExtension)) {
            throw new Error('Invalid file type. Allowed types are: PDF, Excel, Word, and Text.');
        }

        return true;
    },

    createExcelViewerModal(blob, fileName, fileId) {
        // Extensive logging at the start
        console.group('Excel Viewer Modal Initialization');
        console.log('File ID:', fileId);
        console.log('Original Filename:', fileName);
        console.log('Blob Size:', blob.size);
        console.log('Blob Type:', blob.type);
        console.groupEnd();

        // Create modal HTML with a container for Handsontable
        const modalHtml = `
    <div class="modal fade" id="excelViewerModal" tabindex="-1">
        <div class="modal-dialog modal-fullscreen">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Excel Viewer - ${fileName}</h5>
                    <select id="sheet-selector" class="form-select w-auto ms-3"></select>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <div id="excel-container" style="width:100%; height:70vh;"></div>
                </div>
                <div class="modal-footer">
                    <button id="save-changes" class="btn btn-primary">Save Changes</button>
                    <button class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>
    `;

        // Append modal to body
        if (!document.getElementById('excelViewerModal')) {
            const modalDiv = document.createElement('div');
            modalDiv.innerHTML = modalHtml;
            document.body.appendChild(modalDiv);
        }

        // Show loading
        const loadingModal = Swal.fire({
            title: 'Loading Excel File...',
            html: 'Please wait while the file is being processed',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        // Create a timeout to handle potential loading issues
        const loadingTimeout = setTimeout(() => {
            Swal.fire({
                icon: 'error',
                title: 'Loading Timeout',
                text: 'File loading took too long. Please try again.',
                footer: 'Possible causes: large file, network issue, file corruption'
            });
        }, 30000); // 30 seconds timeout

        // Use FileReader to read the blob
        const reader = new FileReader();

        // Error handling for FileReader
        reader.onerror = (error) => {
            clearTimeout(loadingTimeout);
            console.error('FileReader error:', error);
            Swal.fire({
                icon: 'error',
                title: 'File Reading Error',
                text: 'Unable to read the file. Please try again.',
                footer: `Error: ${error}`
            });
        };

        reader.onload = async (e) => {
            try {
                // Clear the loading timeout
                clearTimeout(loadingTimeout);

                // Log raw event data
                console.group('FileReader onload');
                console.log('Result Type:', typeof e.target.result);
                console.log('Result Length:', e.target.result.length);
                console.groupEnd();

                // Attempt to read the workbook with multiple methods
                let workbook;
                try {
                    // Try binary first
                    workbook = XLSX.read(e.target.result, { type: 'binary' });
                } catch (binaryError) {
                    console.warn('Binary read failed, trying array method', binaryError);
                    try {
                        // Fallback to array
                        workbook = XLSX.read(e.target.result, { type: 'array' });
                    } catch (arrayError) {
                        console.error('Both binary and array reads failed', arrayError);
                        throw arrayError;
                    }
                }

                // Log workbook details
                console.group('Workbook Details');
                console.log('Sheet Names:', workbook.SheetNames);
                console.log('Sheets:', Object.keys(workbook.Sheets));
                console.groupEnd();

                // Populate sheet selector
                const sheetSelector = document.getElementById('sheet-selector');
                sheetSelector.innerHTML = workbook.SheetNames.map((name, index) =>
                    `<option value="${index}">${name}</option>`
                ).join('');

                // Convert first sheet to array for Handsontable
                const sheetName = workbook.SheetNames[0];
                const worksheet = workbook.Sheets[sheetName];

                // Log worksheet details
                console.group('Worksheet Details');
                console.log('Sheet Name:', sheetName);
                console.log('Worksheet Keys:', Object.keys(worksheet));
                console.groupEnd();

                const sheetData = XLSX.utils.sheet_to_json(worksheet, { header: 1 });

                // Log sheet data
                console.group('Sheet Data');
                console.log('Data Length:', sheetData.length);
                console.log('First Row:', sheetData[0]);
                console.groupEnd();

                // Initialize Handsontable
                const container = document.getElementById('excel-container');
                const hot = new Handsontable(container, {
                    data: sheetData,
                    rowHeaders: true,
                    colHeaders: true,
                    height: '100%',
                    width: '100%',
                    licenseKey: 'non-commercial-and-evaluation',
                    contextMenu: true,
                    manualColumnResize: true,
                    manualRowResize: true,
                    filters: true,
                    dropdownMenu: true,
                    // Track changes
                    afterChange: function (changes, source) {
                        if (source !== 'loadData') {
                            this.hasChanges = true;
                        }
                    }
                });

                // Close loading modal
                Swal.close();

                // Show modal
                const excelModal = new bootstrap.Modal(document.getElementById('excelViewerModal'));
                excelModal.show();

                // Save changes button
                document.getElementById('save-changes').addEventListener('click', async () => {
                    try {
                        // Check if there are changes
                        if (!hot.hasChanges) {
                            Swal.fire({
                                icon: 'info',
                                title: 'No Changes',
                                text: 'No modifications have been made to the file.'
                            });
                            return;
                        }

                        // Get modified data
                        const modifiedData = hot.getData();

                        // Remove completely empty rows
                        const cleanedData = modifiedData.filter(row =>
                            row.some(cell => cell !== null && cell !== undefined && cell !== '')
                        );

                        // Update the existing worksheet
                        const newWorksheet = XLSX.utils.aoa_to_sheet(cleanedData);
                        workbook.Sheets[sheetName] = newWorksheet;

                        // Generate workbook as array buffer
                        const wbout = XLSX.write(workbook, {
                            bookType: 'xlsx',
                            type: 'array'
                        });

                        // Ensure fileName has .xlsx extension
                        const fileNameWithExtension = fileName.toLowerCase().endsWith('.xlsx')
                            ? fileName
                            : `${fileName}.xlsx`;

                        // Create a File object
                        const modifiedFile = new File([wbout], fileNameWithExtension, {
                            type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                            lastModified: Date.now()
                        });

                        // Prepare FormData
                        const formData = new FormData();
                        formData.append('file', modifiedFile, fileNameWithExtension);

                        // Show loading
                        Swal.fire({
                            title: 'Updating File...',
                            html: 'Please wait while the file is being updated',
                            allowOutsideClick: false,
                            didOpen: () => {
                                Swal.showLoading();
                            }
                        });

                        // Perform file update
                        const response = await fetch(`https://localhost:44320/api/Files/update/${fileId}`, {
                            method: 'PUT',
                            headers: {
                                'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
                            },
                            body: formData
                        });

                        // Check response
                        if (!response.ok) {
                            const errorText = await response.text();
                            console.error('Server Error Response:', errorText);
                            throw new Error(errorText || 'File update failed');
                        }

                        // Parse response
                        const responseData = await response.json();

                        // Reset changes flag
                        hot.hasChanges = false;

                        // Success notification
                        Swal.fire({
                            icon: 'success',
                            title: 'File Updated',
                            text: responseData.message || 'Excel file has been successfully modified.'
                        });

                        // Refresh files table
                        await this.renderFilesTable();

                    } catch (error) {
                        console.error('Error in save changes:', error);
                        Swal.fire({
                            icon: 'error',
                            title: 'Save Error',
                            text: 'Unable to save changes.',
                            footer: `Error: ${error.message}`
                        });
                    }
                });

            } catch (error) {
                console.error('Excel parsing error:', error);
                Swal.fire({
                    icon: 'error',
                    title: 'Excel Viewer Error',
                    text: 'Unable to parse Excel file.',
                    footer: `Error: ${error.message}`
                });
            }
        };

        // Read the blob
        try {
            reader.readAsBinaryString(blob);
        } catch (error) {
            console.error('Error reading blob:', error);
            Swal.fire({
                icon: 'error',
                title: 'Blob Reading Error',
                text: 'Unable to read file blob.',
                footer: `Error: ${error.message}`
            });
        }
    },
    async uploadModifiedExcel(modifiedFile, originalFileName) {
        try {
            // Create FormData
            const formData = new FormData();
            formData.append('file', modifiedFile, originalFileName);

            // Show loading
            Swal.fire({
                title: 'Uploading Modified File...',
                html: 'Please wait while the file is being updated',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Perform file upload
            const response = await fetch('https://localhost:44320/api/Files/update', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
                },
                body: formData
            });

            // Check response
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Server Error Response:', errorText);

                throw new Error(errorText || 'File upload failed');
            }

            // Parse response
            const responseData = await response.json();

            // Success notification
            Swal.fire({
                icon: 'success',
                title: 'File Updated',
                text: responseData.message || 'Excel file has been successfully modified and uploaded.'
            });

            // Refresh files table
            await this.renderFilesTable();

            return responseData;

        } catch (error) {
            console.error('Modified file upload error:', error);

            Swal.fire({
                icon: 'error',
                title: 'Upload Failed',
                text: error.message || 'Unable to upload modified file.',
                footer: `Error: ${error.message}`
            });

            throw error;
        }
    },



    async updateFile() {
        try {
            // Get the file ID from the button's data attribute
            const fileId = document.getElementById('updateFileBtn').dataset.fileId;

            // Get form and validate
            const form = document.getElementById('updateFileForm');
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            // Create FormData
            const formData = new FormData();

            // Add file if selected
            const fileInput = document.getElementById('fileInput2');
            if (fileInput.files.length > 0) {
                formData.append('File', fileInput.files[0]);
            }

            // Add other form fields
            formData.append('FileName', document.getElementById('fileName2').value);
            formData.append('CategoryId', document.getElementById('updateCategorySelect2').value);
            formData.append('IsPublic', document.getElementById('isPublic2').checked);

            // Show loading
            Swal.fire({
                title: 'Updating File...',
                html: 'Please wait while the file is being updated',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Get the auth token
            const authToken = sessionStorage.getItem('authToken');
            if (!authToken) {
                throw new Error('Authentication token not found in session storage');
            }

            // Log the token for debugging
            console.log('Using auth token:', authToken);

            // Perform file update
            const response = await fetch(`https://localhost:44320/api/Files/${fileId}`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${authToken}`
                },
                body: formData
            });

            // Check response
            if (!response.ok) {
                const errorText = await response.text();
                let errorMessage = 'File update failed';
                try {
                    const errorData = JSON.parse(errorText);
                    errorMessage = errorData.errors ?
                        Object.values(errorData.errors).flat().join(', ') :
                        errorData.title || errorMessage;
                } catch {
                    // If parsing fails, use the original error message
                }
                throw new Error(errorMessage);
            }

            // Parse response
            const responseData = await response.json();

            // Success notification
            Swal.fire({
                icon: 'success',
                title: 'File Updated',
                text: 'File has been successfully updated.'
            });

            // Close the modal
            const updateModal = bootstrap.Modal.getInstance(document.getElementById('updateFileModal'));
            if (updateModal) {
                updateModal.hide();
            }

            // Refresh files table
            await this.renderFilesTable();

            return responseData;

        } catch (error) {
            console.error('Update file error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Update Failed',
                text: error.message || 'Unable to update file.'
            });
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

        try {
            // Fetch file details
            const fileDetails = await apiCall(`https://localhost:44320/api/Files/${fileId}`);

            // Make sure categories are loaded
            await this.loadCategories();

            // Populate modal with file details
            this.populateUpdateModal(fileDetails);

            // Show the modal
            const updateModal = new bootstrap.Modal(document.getElementById('updateFileModal'));
            updateModal.show();

        } catch (error) {
            console.error('Edit file error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Unable to retrieve file details for editing.'
            });
        }
    },
    async loadCategories() {
        try {
            // Use the correct endpoint for categories
            const categories = await apiCall('https://localhost:44320/api/FileCategories');
            sessionStorage.setItem('categories', JSON.stringify(categories));
            return categories;
        } catch (error) {
            console.error('Failed to load categories:', error);
            Swal.fire({
                icon: 'warning',
                title: 'Warning',
                text: 'Failed to load categories. Some options may not be available.'
            });
            return [];
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

        try {
            // Fetch file details
            const fileDetails = await apiCall(`https://localhost:44320/api/Files/${fileId}`);

            // Populate modal with file details
            this.populateUpdateModal(fileDetails);

            // Show the modal
            const updateModal = new bootstrap.Modal(document.getElementById('updateFileModal'));
            updateModal.show();

        } catch (error) {
            console.error('Edit file error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Unable to retrieve file details for editing.'
            });
        }
    },

    populateUpdateModal(fileDetails) {
        // Get stored categories from session storage
        const storedCategories = JSON.parse(sessionStorage.getItem('categories') || '[]');

        // Populate file name
        document.getElementById('fileName2').value = fileDetails.fileName;

        // Populate category select
        const categorySelect = document.getElementById('updateCategorySelect2');
        categorySelect.innerHTML = ''; // Clear existing options

        // Add default option
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = 'Select Category';
        categorySelect.appendChild(defaultOption);

        // Populate categories
        storedCategories.forEach(category => {
            const option = document.createElement('option');
            option.value = category.categoryId;
            option.textContent = category.categoryName;

            // Set selected if matches file's category
            if (category.categoryId === fileDetails.categoryId) {
                option.selected = true;
            }

            categorySelect.appendChild(option);
        });

        // Set public checkbox
        document.getElementById('isPublic2').checked = fileDetails.isPublic;

        // Store current file ID for update
        const updateFileBtn = document.getElementById('updateFileBtn');
        updateFileBtn.dataset.fileId = fileDetails.fileId;
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
        try {
            // Show loading indicator
            Swal.fire({
                title: 'Preparing Download...',
                html: 'Please wait while the file is being prepared',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Fetch file download
            const response = await fetch(`https://localhost:44320/api/Files/download/${fileId}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`,
                    'Accept': 'application/octet-stream'
                }
            });

            // Check response
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'File download failed');
            }

            // Get blob
            const blob = await response.blob();

            // Fetch file details to get correct filename
            const fileDetails = await apiCall(`https://localhost:44320/api/Files/${fileId}`);
            const fullFileName = fileDetails.fileName + fileDetails.fileExtension;

            // Create download link
            const downloadLink = document.createElement('a');
            downloadLink.href = window.URL.createObjectURL(blob);
            downloadLink.download = fullFileName;

            // Append to body, click, and remove
            document.body.appendChild(downloadLink);
            downloadLink.click();
            document.body.removeChild(downloadLink);

            // Success notification
            Swal.fire({
                icon: 'success',
                title: 'Download Started',
                text: `Downloading ${fullFileName}`
            });

        } catch (error) {
            console.error('Download error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Download Failed',
                text: 'Unable to download file.',
                footer: `Error: ${error.message}`
            });
        }
    }
};


async function initializePage() {
    try {
        // Create global role manager
        window.roleManager = new RoleAccessManager();

        // Initialize user roles from token
        const userRoles = window.roleManager.initializeUserRoles();

        // Log roles for debugging
        console.log('Initialized User Roles:', userRoles);

        // Fetch categories and store in session storage
        const categories = await FileActions.fetchCategories();

        // Ensure categories are stored
        if (categories && categories.length > 0) {
            sessionStorage.setItem('categories', JSON.stringify(categories));
        } else {
            console.warn('No categories fetched');
        }

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
}


// Initialization with improved error handling
// Initialization and setup
// Initialization and setup
document.addEventListener('DOMContentLoaded', async () => {
    // Initialize categories on page load
    await FileActions.loadCategories();

    const updateModal = document.getElementById('updateFileModal');
    if (updateModal) {
        // Prevent aria-hidden issues
        updateModal.addEventListener('show.bs.modal', async (event) => {
            // Remove aria-hidden when modal is shown
            updateModal.removeAttribute('aria-hidden');
            // Ensure proper focus management
            const modalCloseBtn = updateModal.querySelector('.btn-close');
            if (modalCloseBtn) {
                modalCloseBtn.focus();
            }
            setupUpdateModal();

            // Fetch categories and populate the dropdown
            const categories = await FileActions.fetchCategories();
            const categorySelect = document.getElementById('categorySelect');
            categorySelect.innerHTML = '<option value="">Select Category</option>';
            categories.forEach(category => {
                const option = document.createElement('option');
                option.value = category.categoryId;
                option.textContent = category.categoryName;
                categorySelect.appendChild(option);
            });
        });

        const updateFileBtn = document.getElementById('updateFileBtn');
        if (updateFileBtn) {
            updateFileBtn.addEventListener('click', () => {
                FileActions.updateFile();
            });
        }

        updateModal.addEventListener('hidden.bs.modal', (event) => {
            // Restore aria-hidden when modal is closed
            updateModal.setAttribute('aria-hidden', 'true');
            // Reset form
            const form = updateModal.querySelector('form');
            if (form) {
                form.reset();
                form.classList.remove('was-validated');
            }
        });
    }
});

// Function to set up the update modal
function setupUpdateModal() {
    const updateModal = document.getElementById('updateFileModal');
    if (updateModal) {
        updateModal.addEventListener('show.bs.modal', async (event) => {
            try {
                // Remove aria-hidden when modal is shown
                updateModal.removeAttribute('aria-hidden');

                // Ensure proper focus management
                const modalCloseBtn = updateModal.querySelector('.btn-close');
                if (modalCloseBtn) {
                    modalCloseBtn.focus();
                }

                // Fetch categories and populate the dropdown
                const categories = await FileActions.fetchCategories();
                if (categories && categories.length > 0) {
                    const categorySelect = document.getElementById('updateCategorySelect2');
                    categorySelect.innerHTML = '<option value="">Select Category</option>';
                    categories.forEach(category => {
                        const option = document.createElement('option');
                        option.value = category.categoryId;
                        option.textContent = category.categoryName;
                        categorySelect.appendChild(option);
                    });
                } else {
                    console.warn('No categories fetched for update modal');
                    const categorySelect = document.getElementById('updateCategorySelect2');
                    categorySelect.innerHTML = '<option value="">No categories available</option>';
                }
            } catch (error) {
                console.error('Error fetching categories for update modal:', error);
                const categorySelect = document.getElementById('updateCategorySelect2');
                categorySelect.innerHTML = '<option value="">Error fetching categories</option>';
            }
        });

        const updateFileBtn = document.getElementById('updateFileBtn');
        if (updateFileBtn) {
            updateFileBtn.addEventListener('click', () => {
                FileActions.updateFile();
            });
        }

        updateModal.addEventListener('hidden.bs.modal', (event) => {
            // Restore aria-hidden when modal is closed
            updateModal.setAttribute('aria-hidden', 'true');

            // Reset form
            const form = updateModal.querySelector('form');
            if (form) {
                form.reset();
                form.classList.remove('was-validated');
            }
        });
    }
}

// Function to show error modal
function showErrorModal(title, text, footer) {
    Swal.fire({
        icon: 'error',
        title: title,
        text: text,
        footer: `Error: ${footer}`
    });
}
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





