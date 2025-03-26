// Constants for pagination
const ITEMS_PER_PAGE = 5;

// Function to fetch categories from API
async function fetchCategories() {
    try {
        const response = await fetch('https://localhost:44320/api/FileCategories', {
            method: 'GET',
            headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
            }
        });

        if (!response.ok) {
            // Handle unauthorized or forbidden errors
            if (response.status === 401 || response.status === 403) {
                window.location.href = 'auth-login.html';
                return [];
            }
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();

        // Remove duplicates with more detailed logging
        const seenIds = new Set();
        const uniqueCategories = data.filter(category => {
            if (seenIds.has(category.categoryId)) {
                console.warn(`Duplicate category found and removed: ${category.categoryName}`);
                return false;
            }
            seenIds.add(category.categoryId);
            return true;
        });

        return uniqueCategories;
    } catch (error) {
        console.error('Error fetching categories:', error);
        return [];
    }
}

// Function to fetch files for a given category
async function fetchFiles(categoryId) {
    // Ensure categoryId is a valid number
    const validCategoryId = parseInt(categoryId);

    if (isNaN(validCategoryId)) {
        console.error('Invalid category ID:', categoryId);
        return [];
    }

    try {
        const response = await fetch(`https://localhost:44320/api/Files/category/${validCategoryId}`, {
            method: 'GET',
            headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
            }
        });

        if (!response.ok) {
            console.error(`Failed to fetch files for category ${categoryId}`);
            return [];
        }

        return await response.json();
    } catch (error) {
        console.error('Error fetching files:', error);
        return [];
    }
}

// Function to get user role from token
function getUserRoleFromToken() {
    const token = sessionStorage.getItem('authToken');
    if (token) {
        try {
            // Decode the token
            const payload = JSON.parse(atob(token.split('.')[1]));

            // Check for roles in different possible locations
            const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
                || payload.roles
                || payload.role
                || ['Unknown'];

            // Ensure roles is always an array
            return Array.isArray(roles) ? roles[0] : roles;
        } catch (error) {
            console.error('Error parsing token:', error);
            return 'Unknown';
        }
    }
    return 'Unknown';
}

// Function to display categories with pagination
// Function to display categories with pagination
// Function to display categories with pagination
async function displayCategories(categories, currentPage) {
    const parentTableBody = document.getElementById('parentTableBody');
    const parentPagination = document.getElementById('parentPagination');

    if (!parentTableBody || !parentPagination) {
        console.error('Required DOM elements not found');
        return;
    }

    parentTableBody.innerHTML = '';
    parentPagination.innerHTML = '';

    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
    const endIndex = startIndex + ITEMS_PER_PAGE;
    const paginatedCategories = categories.slice(startIndex, endIndex);

    const userRole = getUserRoleFromToken();
    const canEditDelete = ['SuperAdmin', 'Admin'].includes(userRole);

    // Use Promise.all to fetch files for all categories concurrently
    const categoriesWithFiles = await Promise.all(paginatedCategories.map(async (category) => {
        const files = await fetchFiles(category.categoryId);
        return {
            ...category,
            files: files
        };
    }));

    categoriesWithFiles.forEach((category) => {
        const row = document.createElement('tr');
        row.setAttribute('data-category-id', category.categoryId);

        const actionCell = `
            <td class="text-end">
                ${canEditDelete ? `
                    <a href="#" class="edit-category-btn me-2" data-category-id="${category.categoryId}">
                        <i class="las la-pen text-secondary fs-18"></i>
                    </a>
                    <a href="#" class="delete-category-btn me-2" data-category-id="${category.categoryId}">
                        <i class="las la-trash-alt text-secondary fs-18"></i>
                    </a>
                ` : ''}
                <a href="#" class="show-files-btn" data-category-id="${category.categoryId}">
                    <i class="las la-info-circle text-secondary fs-18"></i>
                </a>
            </td>
        `;

        // Determine last updated date
        const lastUpdatedDate = category.files.length > 0
            ? new Date(Math.max(...category.files.map(f => new Date(f.uploadDate).getTime()))).toLocaleDateString()
            : 'N/A';

        row.innerHTML = `
            <td style="width: 16px;">
                <div class="form-check">
                    <input type="checkbox" class="form-check-input" name="check" id="customCheck${category.categoryId}">
                </div>
            </td>
            <td class="ps-0 category-name">${category.categoryName}</td>
            <td class="category-description">${category.description}</td>
            <td>
                <span class="badge bg-${category.isActive ? 'success' : 'danger'}-subtle text-${category.isActive ? 'success' : 'danger'}">
                    ${category.isActive ? 'Active' : 'Inactive'}
                </span>
            </td>
            <td>${category.files.length}</td>
            <td>${lastUpdatedDate}</td>
            ${actionCell}
        `;

        parentTableBody.appendChild(row);
    });

    // Pagination logic remains the same
    const totalPages = Math.ceil(categories.length / ITEMS_PER_PAGE);
    for (let i = 1; i <= totalPages; i++) {
        const li = document.createElement('li');
        li.className = 'page-item';
        if (i === currentPage) {
            li.classList.add('active');
        }
        li.innerHTML = `<a class="page-link" href="#" data-page="${i}">${i}</a>`;
        parentPagination.appendChild(li);
    }
}

// Function to display files in the modal with pagination
async function displayFiles(files, currentPage) {
    const filesTableBody = document.getElementById('filesTableBody');
    const childPagination = document.getElementById('childPagination');

    if (!filesTableBody || !childPagination) {
        console.error('Required DOM elements not found');
        return;
    }

    filesTableBody.innerHTML = '';
    childPagination.innerHTML = '';

    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
    const endIndex = startIndex + ITEMS_PER_PAGE;
    const paginatedFiles = files.slice(startIndex, endIndex);

    paginatedFiles.forEach(file => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${file.fileName}${file.fileExtension || ''}</td>
            <td>${file.fileSize} bytes</td>
            <td>a7mad (SuperAdmin)</td>
            <td>${new Date(file.uploadDate).toLocaleString()}</td>
            <td>${file.lastModifiedDate ? new Date(file.lastModifiedDate).toLocaleString() : 'Never'}</td>
            <td>
                <span class="badge bg-${file.isActive ? 'success' : 'danger'}-subtle text-${file.isActive ? 'success' : 'danger'}">
                    ${file.isActive ? 'Active' : 'Inactive'}
                </span>
            </td>
        `;
        filesTableBody.appendChild(row);
    });

    // Generate pagination
    const totalPages = Math.ceil(files.length / ITEMS_PER_PAGE);
    for (let i = 1; i <= totalPages; i++) {
        const li = document.createElement('li');
        li.className = 'page-item';
        if (i === currentPage) {
            li.classList.add('active');
        }
        li.innerHTML = `<a class="page-link" href="#" data-page="${i}">${i}</a>`;
        childPagination.appendChild(li);
    }
}

// Function to add a new category
async function addCategory(categoryName, description, isActive) {
    try {
        const response = await fetch('https://localhost:44320/api/FileCategories', {
            method: 'POST',
            headers: {
                'accept': 'text/plain',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
            },
            body: JSON.stringify({
                categoryName: categoryName,
                description: description,
                isActive: isActive
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error adding category:', error);
        throw error;
    }
}

// Function to edit a category
async function editCategory(categoryId, categoryName, description, isActive) {
    try {
        const response = await fetch(`https://localhost:44320/api/FileCategories/${categoryId}`, {
            method: 'PUT',
            headers: {
                'accept': 'text/plain',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
            },
            body: JSON.stringify({
                categoryId: categoryId,
                categoryName: categoryName,
                description: description,
                isActive: isActive
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error editing category:', error);
        throw error;
    }
}

// Function to delete a category
async function deleteCategory(categoryId) {
    try {
        const response = await fetch(`https://localhost:44320/api/FileCategories/${categoryId}`, {
            method: 'DELETE',
            headers: {
                'accept': '*/*',
                'Authorization': `Bearer ${sessionStorage.getItem('authToken')}`
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return true;
    } catch (error) {
        console.error('Error deleting category:', error);
        throw error;
    }
}
// Add this function to handle filtering

// Main event listener
document.addEventListener('DOMContentLoaded', async () => {
    // Wait for a short delay to ensure all DOM elements are loaded
    await new Promise(resolve => setTimeout(resolve, 100));

    // Make categories globally accessible
    window.categories = await fetchCategories();
    window.currentPage = 1;

    // Filter functionality
    const filterAll = document.getElementById('filter-all');
    const otherFilters = [
        document.getElementById('filter-one'),
        document.getElementById('filter-five')
    ];

    // Function to filter categories
    async function filterCategories() {
        const filters = {
            all: filterAll.checked,
            active: document.getElementById('filter-one').checked,
            inactive: document.getElementById('filter-five').checked
        };

        // Filter the categories based on selected filters
        let filteredCategories = window.categories.filter(category => {
            // If 'All' is checked, show everything
            if (filters.all) return true;

            // Check active/inactive status
            if (filters.active && filters.inactive) return true;
            if (filters.active && category.isActive) return true;
            if (filters.inactive && !category.isActive) return true;

            return false;
        });

        // Refresh the table with filtered categories
        await displayCategories(filteredCategories, 1);
    }

    // Initial display of categories
    await displayCategories(window.categories, window.currentPage);

    // Event listener for parent table pagination
    const parentPagination = document.getElementById('parentPagination');
    if (parentPagination) {
        parentPagination.addEventListener('click', async (e) => {
            if (e.target.tagName === 'A') {
                e.preventDefault();
                window.currentPage = parseInt(e.target.getAttribute('data-page'));
                await displayCategories(window.categories, window.currentPage);
            }
        });
    }

    // Filter all checkbox - toggle other checkboxes
    filterAll.addEventListener('change', async (e) => {
        otherFilters.forEach(filter => {
            filter.checked = e.target.checked;
        });
        await filterCategories();
    });

    // Other filter checkboxes
    otherFilters.forEach(filter => {
        filter.addEventListener('change', async () => {
            // Uncheck 'All' if any specific filter is unchecked
            if (!filter.checked) {
                filterAll.checked = false;
            }
            await filterCategories();
        });
    });

    // Initial filter setup
    await filterCategories();

    // Add Category Button Event Listener
    document.getElementById('addCategoryBtn').addEventListener('click', () => {
        const userRole = getUserRoleFromToken();
        if (!['SuperAdmin', 'Admin'].includes(userRole)) {
            Swal.fire({
                icon: 'error',
                title: 'Access Denied',
                text: 'You do not have permission to add categories.'
            });
            return;
        }

        // Reset the form
        document.getElementById('categoryForm').reset();
        document.getElementById('categoryId').value = ''; // Clear any existing ID
        document.getElementById('categoryModalLabel').textContent = 'Add Category';

        // Show the modal
        const categoryModal = new bootstrap.Modal(document.getElementById('categoryModal'));
        categoryModal.show();
    });

    // Form submission event listener
    document.getElementById('categoryForm').addEventListener('submit', async (e) => {
        e.preventDefault();

        const categoryId = document.getElementById('categoryId').value;
        const categoryName = document.getElementById('categoryName').value;
        const description = document.getElementById('categoryDescription').value;
        const isActive = document.getElementById('categoryIsActive').checked;

        try {
            let result;
            if (categoryId) {
                // Edit existing category
                result = await editCategory(
                    parseInt(categoryId),
                    categoryName,
                    description,
                    isActive
                );

                // Update local categories array
                const index = window.categories.findIndex(cat => cat.categoryId === parseInt(categoryId));
                if (index !== -1) {
                    window.categories[index] = result;
                }
            } else {
                // Add new category
                result = await addCategory(
                    categoryName,
                    description,
                    isActive
                );

                // Add to local categories array
                window.categories.push(result);
            }

            // Refresh the table and apply filters
            filterCategories();

            // Close the modal
            const categoryModal = bootstrap.Modal.getInstance(document.getElementById('categoryModal'));
            categoryModal.hide();

            // Show success message
            Swal.fire({
                icon: 'success',
                title: categoryId ? 'Category Updated' : 'Category Added',
                text: categoryId
                    ? 'Category has been successfully updated.'
                    : 'New category has been successfully created.'
            });
        } catch (error) {
            // Show error message
            Swal.fire({
                icon: 'error',
                title: 'Operation Failed',
                text: categoryId
                    ? 'Failed to update category. Please try again.'
                    : 'Failed to add category. Please try again.'
            });
        }
    });

    const parentTableBody = document.getElementById('parentTableBody');
    if (parentTableBody) {
        parentTableBody.addEventListener('click', async (e) => {
            const row = e.target.closest('tr');
            if (!row) return;

            const categoryId = row.getAttribute('data-category-id');
            if (!categoryId) return;

            // Edit button handler
            if (e.target.closest('.edit-category-btn')) {
                e.preventDefault();
                const userRole = getUserRoleFromToken();
                if (!['SuperAdmin', 'Admin'].includes(userRole)) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Access Denied',
                        text: 'You do not have permission to edit categories.'
                    });
                    return;
                }

                // Find the category in the existing categories array
                const category = window.categories.find(cat => cat.categoryId === parseInt(categoryId));

                if (!category) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Category Not Found',
                        text: 'Please refresh the page and try again.'
                    });
                    return;
                }

                // Populate the form
                document.getElementById('categoryId').value = category.categoryId;
                document.getElementById('categoryName').value = category.categoryName;
                document.getElementById('categoryDescription').value = category.description;
                document.getElementById('categoryIsActive').checked = category.isActive;
                document.getElementById('categoryModalLabel').textContent = 'Edit Category';

                // Show the modal
                const categoryModal = new bootstrap.Modal(document.getElementById('categoryModal'));
                categoryModal.show();
            }

            // Delete button handler
            else if (e.target.closest('.delete-category-btn')) {
                e.preventDefault();
                const userRole = getUserRoleFromToken();
                if (!['SuperAdmin', 'Admin'].includes(userRole)) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Access Denied',
                        text: 'You do not have permission to delete categories.'
                    });
                    return;
                }

                Swal.fire({
                    title: 'Are you sure?',
                    text: 'Do you want to delete this category?',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#d33',
                    confirmButtonText: 'Yes, delete it!'
                }).then(async (result) => {
                    if (result.isConfirmed) {
                        try {
                            await deleteCategory(categoryId);

                            // Remove category from local array
                            window.categories = window.categories.filter(cat => cat.categoryId !== parseInt(categoryId));

                            // Refresh the table and apply filters
                            filterCategories();

                            Swal.fire({
                                icon: 'success',
                                title: 'Deleted!',
                                text: 'Category has been deleted.'
                            });
                        } catch (error) {
                            Swal.fire({
                                icon: 'error',
                                title: 'Delete Failed',
                                text: 'Failed to delete category. Please try again.'
                            });
                        }
                    }
                });
            }

            // Files button handler
            else if (e.target.closest('.show-files-btn')) {
                e.preventDefault();

                try {
                    const files = await fetchFiles(categoryId);
                    let childCurrentPage = 1;

                    await displayFiles(files, childCurrentPage);

                    // Show the existing modal
                    const modal = new bootstrap.Modal(document.getElementById('filesModal'));
                    modal.show();

                    // Event listener for child table pagination
                    const childPagination = document.getElementById('childPagination');
                    if (childPagination) {
                        // Remove existing listeners
                        const oldChildPagination = childPagination.cloneNode(true);
                        childPagination.parentNode.replaceChild(oldChildPagination, childPagination);

                        oldChildPagination.addEventListener('click', (e) => {
                            if (e.target.tagName === 'A') {
                                e.preventDefault();
                                childCurrentPage = parseInt(e.target.getAttribute('data-page'));
                                displayFiles(files, childCurrentPage);
                            }
                        });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: 'Failed to fetch files. Please try again.'
                    });
                }
            }
        });
    }
});