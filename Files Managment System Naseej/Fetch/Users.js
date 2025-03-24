const BASE_URL = 'https://localhost:44320/api';

// Function to check if user is Super Admin

// Function to check if user is Super Admin
function isSuperAdmin() {
    const roles = JSON.parse(sessionStorage.getItem('roles') || '[]');
    return roles.includes('SuperAdmin');
}
// Function to apply filters
// Function to apply filters
function applyFilters() {
    const filters = {
        new: filterCheckboxes.new.checked,
        active: filterCheckboxes.active.checked,
        inactive: filterCheckboxes.inactive.checked
    };

    // Get all user rows
    const userRows = document.querySelectorAll('#userTableBody tr');

    userRows.forEach(row => {
        // Get the status cell
        const statusCell = row.querySelector('td:nth-child(5) .badge');

        if (!statusCell) return;

        const status = statusCell.textContent.trim().toLowerCase();
        let shouldShow = false;

        // Determine visibility based on filters
        switch (status) {
            case 'new':
                shouldShow = filters.new;
                break;
            case 'active':
                shouldShow = filters.active;
                break;
            case 'inactive':
                shouldShow = filters.inactive;
                break;
            default:
                shouldShow = true;
        }

        // Toggle row visibility
        row.style.display = shouldShow ? '' : 'none';
    });

    // Update 'All' checkbox based on other checkboxes
    updateAllCheckbox();
}

// Function to update 'All' checkbox
function updateAllCheckbox() {
    const otherCheckboxes = [
        filterCheckboxes.new,
        filterCheckboxes.active,
        filterCheckboxes.inactive
    ];

    const allChecked = otherCheckboxes.every(cb => cb.checked);
    const allUnchecked = otherCheckboxes.every(cb => !cb.checked);

    filterCheckboxes.all.checked = allChecked;
    filterCheckboxes.all.indeterminate = !(allChecked || allUnchecked);
}


let filterCheckboxes;

// Fetch Users
async function fetchUsers() {
    const token = sessionStorage.getItem('authToken');
    if (!token) {
        alert('Please login first');
        return;
    }

    try {
        const response = await fetch(`${BASE_URL}/User`, {
            headers: {
                'accept': 'text/plain',
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            // Log the full error response
            const errorText = await response.text();
            console.error('Fetch users error:', errorText);
            throw new Error(errorText || 'Failed to fetch users');
        }

        const users = await response.json();
        const userTableBody = document.getElementById('userTableBody');
        userTableBody.innerHTML = ''; // Clear existing rows

        users.forEach(user => {
            // Determine status
            let status = user.isActive ? 'Active' : 'Inactive';

            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${user.username}</td>
                <td>${user.email}</td>
                <td>${user.firstName}</td>
                <td>${user.lastName}</td>
                <td>
                    <span class="badge ${status === 'Active' ? 'bg-success' : 'bg-secondary'}">
                        ${status}
                    </span>
                </td>
                <td class="text-end">
                    <a href="#" onclick='showUserInfo(${JSON.stringify(user)})'>
                        <i class="las la-info-circle text-secondary fs-18"></i>
                    </a>
                    ${isSuperAdmin() ? `
                        <a href="#" class="ms-2" onclick='showUpdateUserModal(${JSON.stringify(user)})'>
                            <i class="las la-pen text-secondary fs-18"></i>
                        </a>
                        <a href="#" class="ms-2" onclick="showDeleteOptions(${user.userId})">
                            <i class="las la-trash-alt text-secondary fs-18"></i>
                        </a>
                    ` : ''}
                </td>
            `;

            userTableBody.appendChild(row);
        });

        // Apply filters after populating the table
        applyFilters();
    } catch (error) {
        console.error('Error fetching users:', error);

        // More user-friendly error handling
        Swal.fire({
            icon: 'error',
            title: 'Fetch Users Failed',
            text: error.message || 'Unable to retrieve users. Please try again later.',
            confirmButtonText: 'Retry',
            showCancelButton: true,
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                fetchUsers(); // Retry fetching users
            }
        });
    }
}

function applyFilters() {
    // Ensure filterCheckboxes is defined
    if (!filterCheckboxes) {
        console.error('Filter checkboxes not initialized');
        return;
    }

    const filters = {
        new: filterCheckboxes.new.checked,
        active: filterCheckboxes.active.checked,
        inactive: filterCheckboxes.inactive.checked
    };

    // Get all user rows
    const userRows = document.querySelectorAll('#userTableBody tr');

    userRows.forEach(row => {
        // Get the status cell
        const statusCell = row.querySelector('td:nth-child(5) .badge');

        if (!statusCell) return;

        const status = statusCell.textContent.trim().toLowerCase();
        let shouldShow = false;

        // Determine visibility based on filters
        switch (status) {
            case 'new':
                shouldShow = filters.new;
                break;
            case 'active':
                shouldShow = filters.active;
                break;
            case 'inactive':
                shouldShow = filters.inactive;
                break;
            default:
                shouldShow = true;
        }

        // Toggle row visibility
        row.style.display = shouldShow ? '' : 'none';
    });

    // Update 'All' checkbox based on other checkboxes
    updateAllCheckbox();
}

// Function to update 'All' checkbox
function updateAllCheckbox() {
    if (!filterCheckboxes) return;

    const otherCheckboxes = [
        filterCheckboxes.new,
        filterCheckboxes.active,
        filterCheckboxes.inactive
    ];

    const allChecked = otherCheckboxes.every(cb => cb.checked);
    const allUnchecked = otherCheckboxes.every(cb => !cb.checked);

    filterCheckboxes.all.checked = allChecked;
    filterCheckboxes.all.indeterminate = !(allChecked || allUnchecked);
}

// Initialize filter checkboxes and event listeners
function initializeFilterCheckboxes() {
    // Define filterCheckboxes globally
    filterCheckboxes = {
        all: document.getElementById('filter-all'),
        new: document.getElementById('filter-one'),
        active: document.getElementById('filter-two'),
        inactive: document.getElementById('filter-three')
    };

    // Add event listeners to checkboxes
    Object.values(filterCheckboxes).forEach(checkbox => {
        checkbox.addEventListener('change', applyFilters);
    });

    // Toggle all checkboxes when 'All' is checked/unchecked
    filterCheckboxes.all.addEventListener('change', function () {
        const isChecked = this.checked;
        Object.values(filterCheckboxes).forEach(checkbox => {
            if (checkbox !== this) {
                checkbox.checked = isChecked;
            }
        });
        applyFilters();
    });
}
// Ensure roles are loaded from the token
function loadUserRoles() {
    const token = sessionStorage.getItem('authToken');
    if (!token) return;

    try {
        // Decode the JWT token to get roles
        const tokenParts = token.split('.');
        if (tokenParts.length === 3) {
            const payload = JSON.parse(atob(tokenParts[1]));
            const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            // Store roles in session storage
            sessionStorage.setItem('roles', JSON.stringify(Array.isArray(roles) ? roles : [roles]));
        }
    } catch (error) {
        console.error('Error decoding token:', error);
    }
}
// Show User Info in Modal
function showUserInfo(user) {
    const modalBody = document.getElementById('userDetailsModalBody');

    // Create a detailed table for user information
    modalBody.innerHTML = `
            <table class="table table-bordered">
                <tbody>
                    <tr>
                        <th>User ID</th>
                        <td>${user.userId}</td>
                    </tr>
                    <tr>
                        <th>Username</th>
                        <td>${user.username}</td>
                    </tr>
                    <tr>
                        <th>Email</th>
                        <td>${user.email}</td>
                    </tr>
                    <tr>
                        <th>First Name</th>
                        <td>${user.firstName}</td>
                    </tr>
                    <tr>
                        <th>Last Name</th>
                        <td>${user.lastName}</td>
                    </tr>
                    <tr>
                        <th>Status</th>
                        <td>${user.isActive ? 'Active' : 'Inactive'}</td>
                    </tr>
                    <tr>
                        <th>Created Date</th>
                        <td>${new Date(user.createdDate).toLocaleString()}</td>
                    </tr>
                    <tr>
                        <th>Updated Date</th>
                        <td>${user.updatedDate ? new Date(user.updatedDate).toLocaleString() : 'Not updated'}</td>
                    </tr>
                    <tr>
                        <th>Roles</th>
                        <td>${user.roles.join(', ')}</td>
                    </tr>
                </tbody>
            </table>
        `;

    // Show the modal
    new bootstrap.Modal(document.getElementById('userDetailsModal')).show();
}

    // Fetch and populate roles
async function fetchAndPopulateRoles() {
    try {
        const token = sessionStorage.getItem('authToken');
        const response = await fetch(`${BASE_URL}/Roles`, {
            headers: {
                'accept': 'text/plain',
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error('Failed to fetch roles');
        }

        const roles = await response.json();
        const rolesSelect = document.getElementById('addUserRole');
        rolesSelect.innerHTML = '<option value="">Select a Role</option>'; // Reset with default option

        roles.forEach(role => {
            const option = document.createElement('option');
            option.value = role.roleId;
            option.textContent = role.roleName;
            rolesSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Error fetching roles:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Failed to load roles'
        });
    }
}

// Handle Add User form submission
async function handleAddUser(event) {
    event.preventDefault();

    // Validate Super Admin status
    if (!isSuperAdmin()) {
        Swal.fire({
            icon: 'error',
            title: 'Access Denied',
            text: 'Only Super Admin can add new users'
        });
        return;
    }

    const token = sessionStorage.getItem('authToken');
    if (!token) {
        Swal.fire({
            icon: 'error',
            title: 'Not Authenticated',
            text: 'Please log in'
        });
        return;
    }

    // Validate role selection
    const selectedRoleId = document.getElementById('addUserRole').value;
    if (!selectedRoleId) {
        Swal.fire({
            icon: 'error',
            title: 'Role Required',
            text: 'Please select a role for the user'
        });
        return;
    }

    // Prepare user data
    const newUser = {
        username: document.getElementById('addUsername').value,
        email: document.getElementById('addEmail').value,
        password: document.getElementById('addPassword').value,
        firstName: document.getElementById('addFirstName').value,
        lastName: document.getElementById('addLastName').value
    };

    try {
        // First, create the user
        const createResponse = await fetch(`${BASE_URL}/User`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'accept': 'text/plain',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(newUser)
        });

        if (!createResponse.ok) {
            const errorText = await createResponse.text();
            throw new Error(errorText || 'Failed to create user');
        }

        const createdUser = await createResponse.json();

        // Then, assign the role
        const roleAssignResponse = await fetch(`${BASE_URL}/User/${createdUser.userId}/roles/${selectedRoleId}`, {
            method: 'POST',
            headers: {
                'accept': '*/*',
                'Authorization': `Bearer ${token}`
            }
        });

        if (!roleAssignResponse.ok) {
            const errorText = await roleAssignResponse.text();
            throw new Error(errorText || 'Failed to assign role');
        }

        Swal.fire({
            icon: 'success',
            title: 'User Added',
            text: 'The new user has been created and assigned a role successfully.'
        });

        // Reset form and close modal
        document.getElementById('addUserForm').reset();
        bootstrap.Modal.getInstance(document.getElementById('addUserModal')).hide();

        // Refresh users list
        fetchUsers();

    } catch (error) {
        console.error('Error adding user:', error);
        Swal.fire({
            icon: 'error',
            title: 'Failed to Add User',
            text: error.message || 'An unexpected error occurred'
        });
    }
}

// Assign roles to user
async function assignUserRoles(userId, roleIds) {
    const token = sessionStorage.getItem('authToken');

    for (const roleId of roleIds) {
        try {
            const response = await fetch(`${BASE_URL}/User/${userId}/roles/${roleId}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (!response.ok) {
                console.error(`Failed to assign role ${roleId} to user ${userId}`);
            }
        } catch (error) {
            console.error(`Error assigning role ${roleId}:`, error);
        }
    }
}






// Show Update User Modal
// Fetch user roles for update
async function fetchUserRolesForUpdate(userId) {
    const token = sessionStorage.getItem('authToken');

    try {
        // Log the user ID for debugging
        console.log('Fetching roles for user ID:', userId);

        // Fetch current user roles
        const userRolesResponse = await fetch(`${BASE_URL}/User/${userId}/roles`, {
            headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        });

        // Log the response status
        console.log('User Roles Response Status:', userRolesResponse.status);

        // Fetch all available roles
        const rolesResponse = await fetch(`${BASE_URL}/Roles`, {
            headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        });

        // Log the roles response status
        console.log('Roles Response Status:', rolesResponse.status);

        if (!userRolesResponse.ok || !rolesResponse.ok) {
            throw new Error('Failed to fetch roles');
        }

        // Parse responses
        const userRoles = await userRolesResponse.json();
        const allRoles = await rolesResponse.json();

        // Log raw responses for debugging
        console.log('User Roles:', userRoles);
        console.log('All Roles:', allRoles);

        // Set current role with fallback
        let currentRole = { roleName: 'Undefined' };

        // Check if user has any roles
        if (userRoles && userRoles.length > 0) {
            // Log the first role
            console.log('First User Role:', userRoles[0]);

            // Try different ways to get the role name
            currentRole = {
                roleName: userRoles[0].roleName ||
                    userRoles[0].name ||
                    userRoles[0] ||
                    'Undefined'
            };
        } else {
            console.log('No roles found for user');
        }

        // Set current role in the input
        document.getElementById('currentUserRole').value =
            typeof currentRole === 'string' ? currentRole :
                currentRole.roleName || 'Undefined';

        // Populate new role dropdown
        const newRoleSelect = document.getElementById('newUserRole');
        newRoleSelect.innerHTML = ''; // Clear existing options

        // Add roles to dropdown
        allRoles.forEach(role => {
            const option = document.createElement('option');
            option.value = role.roleId;
            option.textContent = role.roleName;
            newRoleSelect.appendChild(option);
        });

    } catch (error) {
        console.error('Detailed Error in fetchUserRolesForUpdate:', error);

        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: `Failed to load roles: ${error.message}`,
            footer: 'Check console for more details'
        });

        // Set a default state
        document.getElementById('currentUserRole').value = 'Undefined';
        document.getElementById('newUserRole').innerHTML = '';
    }
}

// Modify showUpdateUserModal to ensure async handling
async function showUpdateUserModal(user) {
    if (!isSuperAdmin()) {
        Swal.fire({
            icon: 'error',
            title: 'Access Denied',
            text: 'Only Super Admin can update users'
        });
        return;
    }

    // Populate user details tab
    document.getElementById('updateUserId').value = user.userId;
    document.getElementById('updateUsername').value = user.username;
    document.getElementById('updateEmail').value = user.email;
    document.getElementById('updateFirstName').value = user.firstName;
    document.getElementById('updateLastName').value = user.lastName;
    document.getElementById('updateStatus').value = user.isActive;

    // Populate user role tab
    document.getElementById('updateRoleUserId').value = user.userId;

    // Ensure roles are fetched before showing modal
    try {
        await fetchUserRolesForUpdate(user.userId);

        // Show the modal after roles are fetched
        new bootstrap.Modal(document.getElementById('updateUserModal')).show();
    } catch (error) {
        console.error('Error in showUpdateUserModal:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Failed to load user roles'
        });
    }
}



// Fetch user roles for update
// Fetch user roles for update


// Modify updateUserRole to handle undefined roles
async function updateUserRole(event) {
    event.preventDefault();

    // Validate Super Admin status
    if (!isSuperAdmin()) {
        Swal.fire({
            icon: 'error',
            title: 'Access Denied',
            text: 'Only Super Admin can update user roles'
        });
        return;
    }

    const token = sessionStorage.getItem('authToken');
    const userId = document.getElementById('updateRoleUserId').value;
    const currentRole = document.getElementById('currentUserRole').value;
    const newRoleId = document.getElementById('newUserRole').value;

    try {
        // Find the current role ID
        const rolesResponse = await fetch(`${BASE_URL}/Roles`, {
            headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        });

        if (!rolesResponse.ok) {
            throw new Error('Failed to fetch roles');
        }

        const roles = await rolesResponse.json();

        // Handle cases where current role might be 'Undefined' or 'No Role'
        let currentRoleObj = roles.find(role => role.roleName === currentRole);

        // If no current role found, default to Viewer role
        if (!currentRoleObj) {
            currentRoleObj = roles.find(role => role.roleName.toLowerCase() === 'viewer');
        }

        // If still no role found, throw an error
        if (!currentRoleObj) {
            throw new Error('No valid current role found');
        }

        // Prepare role update data
        const roleUpdateData = {
            oldRoleId: currentRoleObj.roleId,
            newRoleId: parseInt(newRoleId)
        };

        // Send role update request
        const response = await fetch(`${BASE_URL}/User/${userId}/roles`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'accept': '*/*',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(roleUpdateData)
        });

        if (response.ok) {
            const result = await response.json();

            Swal.fire({
                icon: 'success',
                title: 'Role Updated',
                text: result.message || 'User role updated successfully',
                timer: 1500,
                showConfirmButton: false
            });

            // Close modal and refresh users
            bootstrap.Modal.getInstance(document.getElementById('updateUserModal')).hide();
            fetchUsers();
        } else {
            const errorText = await response.text();
            Swal.fire({
                icon: 'error',
                title: 'Role Update Failed',
                text: errorText || 'Failed to update user role'
            });
        }
    } catch (error) {
        console.error('Error updating user role:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'An unexpected error occurred'
        });
    }
}

// Update User
async function updateUser(event) {
    event.preventDefault();

    // Validate Super Admin status
    if (!isSuperAdmin()) {
        Swal.fire({
            icon: 'error',
            title: 'Access Denied',
            text: 'Only Super Admin can update users'
        });
        return;
    }

    const token = sessionStorage.getItem('authToken');

    // Prepare user data
    const userData = {
        userId: document.getElementById('updateUserId').value,
        username: document.getElementById('updateUsername').value,
        email: document.getElementById('updateEmail').value,
        firstName: document.getElementById('updateFirstName').value,
        lastName: document.getElementById('updateLastName').value,
        isActive: document.getElementById('updateStatus').value === 'true'
    };

    try {
        const response = await fetch(`${BASE_URL}/User/${userData.userId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(userData)
        });

        if (response.ok) {
            Swal.fire({
                icon: 'success',
                title: 'User Updated',
                text: 'User details updated successfully',
                timer: 1500,
                showConfirmButton: false
            });

            fetchUsers(); // Refresh the user list
            bootstrap.Modal.getInstance(document.getElementById('updateUserModal')).hide();
        } else {
            const errorText = await response.text();
            Swal.fire({
                icon: 'error',
                title: 'Update Failed',
                text: errorText || 'Failed to update user details'
            });
        }
    } catch (error) {
        console.error('Error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'An unexpected error occurred'
        });
    }
}

// Delete User
async function showDeleteOptions(userId) {
    if (!isSuperAdmin()) {
        Swal.fire({
            icon: 'error',
            title: 'Access Denied',
            text: 'Only Super Admin can delete users or roles'
        });
        return;
    }

    // Fetch user roles first
    let userRoles = [];
    try {
        const token = sessionStorage.getItem('authToken');
        const rolesResponse = await fetch(`${BASE_URL}/User/${userId}/roles`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (rolesResponse.ok) {
            userRoles = await rolesResponse.json();
        }
    } catch (error) {
        console.error('Error fetching user roles:', error);
    }

    // Show SweetAlert with deletion options
    Swal.fire({
        title: 'Delete Options',
        text: 'Choose what you want to delete',
        icon: 'warning',
        showCancelButton: true,
        showDenyButton: userRoles.length > 0,
        confirmButtonText: 'Delete User',
        denyButtonText: 'Delete User Role',
        cancelButtonText: 'Cancel',
        showCloseButton: true
    }).then((result) => {
        if (result.isConfirmed) {
            // Delete Entire User
            deleteUser(userId);
        } else if (result.isDenied) {
            // Show role selection for deletion
            showRoleDeletionOptions(userId, userRoles);
        }
    });
}

// Fetch User Roles with improved error handling and logging
async function fetchUserRoles(userId) {
    const token = sessionStorage.getItem('authToken');

    try {
        const response = await fetch(`${BASE_URL}/User/${userId}/roles`, {
            method: 'GET',
            headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const rolesData = await response.json();
            console.log('Raw fetched user roles:', rolesData);

            // If the API returns an array of role names (strings)
            if (Array.isArray(rolesData) && typeof rolesData[0] === 'string') {
                // Convert to objects with roleId and roleName properties
                const rolesWithIds = await mapRoleNamesToIds(rolesData);
                console.log('Mapped roles with IDs:', rolesWithIds);
                return rolesWithIds;
            }

            // If the API already returns the correct format
            if (Array.isArray(rolesData) && typeof rolesData[0] === 'object') {
                return rolesData;
            }

            // Return empty array if format is unexpected
            console.warn('Unexpected role data format:', rolesData);
            return [];
        } else {
            const errorText = await response.text();
            console.error('Failed to fetch roles:', errorText);
            console.log('Response status:', response.status);
            console.log('Response headers:', Object.fromEntries(response.headers.entries()));
            return [];
        }
    } catch (error) {
        console.error('Error fetching user roles:', error);
        return [];
    }
}
// Map role names to IDs by making an additional API call if needed
async function mapRoleNamesToIds(roleNames) {
    const token = sessionStorage.getItem('authToken');

    try {
        // Try to fetch all roles with their IDs
        const response = await fetch(`${BASE_URL}/Roles`, {
            method: 'GET',
            headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const allRoles = await response.json();
            console.log('All roles from API:', allRoles);

            // Map the role names to their IDs
            const rolesWithIds = [];

            for (const roleName of roleNames) {
                const role = allRoles.find(r => r.name === roleName || r.roleName === roleName);
                if (role) {
                    rolesWithIds.push({
                        roleId: role.id || role.roleId,
                        roleName: roleName
                    });
                }
            }

            return rolesWithIds;
        } else {
            console.warn('Could not fetch role IDs, using index-based IDs as fallback');

            // If we can't get real IDs, create objects with role names
            // Your backend API will need to handle this case
            const existingRoleIds = {
                'SuperAdmin': 1,
                'Admin': 2,
                'Editor': 3,
                'User': 4,
                'Viewer': 5
                // Add any other known roles here
            };

            return roleNames.map(roleName => ({
                // Use a known ID mapping if available, otherwise use a fallback
                roleId: existingRoleIds[roleName] || roleName.toLowerCase(),
                roleName: roleName
            }));
        }
    } catch (error) {
        console.error('Error mapping role names to IDs:', error);

        // Fallback in case of error
        return roleNames.map(roleName => ({
            roleId: roleName.toLowerCase(),
            roleName: roleName
        }));
    }
}

// Delete User or User Role
async function showDeleteOptions(userId) {
    if (!isSuperAdmin()) {
        Swal.fire({
            icon: 'error',
            title: 'Access Denied',
            text: 'Only Super Admin can delete users or roles'
        });
        return;
    }

    // Fetch user roles first
    const userRoles = await fetchUserRoles(userId);

    // Show SweetAlert with deletion options
    Swal.fire({
        title: 'Delete Options',
        text: 'Choose what you want to delete',
        icon: 'warning',
        showCancelButton: true,
        showDenyButton: userRoles && userRoles.length > 0,
        confirmButtonText: 'Delete User',
        denyButtonText: 'Delete User Role',
        cancelButtonText: 'Cancel',
        showCloseButton: true
    }).then((result) => {
        if (result.isConfirmed) {
            // Delete Entire User
            deleteUser(userId);
        } else if (result.isDenied) {
            // Show role selection for deletion
            showRoleDeletionOptions(userId, userRoles);
        }
    });
}

// Show Role Deletion Options
// Show Role Deletion Options with more robust handling
async function showRoleDeletionOptions(userId, userRoles = null) {
    // Fetch roles if not provided
    const roles = userRoles || await fetchUserRoles(userId);

    // Log received roles for debugging
    console.log('Roles received for deletion:', roles);

    // Ensure roles is an array and not empty
    if (!Array.isArray(roles) || roles.length === 0) {
        Swal.fire({
            icon: 'info',
            title: 'No Roles',
            text: 'This user has no roles to delete'
        });
        return;
    }

    // If only one role (Viewer), prevent deletion
    if (roles.length === 1 && roles[0].roleName === 'Viewer') {
        Swal.fire({
            icon: 'info',
            title: 'Cannot Remove Role',
            text: 'User already has the default Viewer role'
        });
        return;
    }

    // Create role options for selection - ensure we use proper values for roleId
    const roleOptions = roles.reduce((acc, role) => {
        // Exclude Viewer role from deletion options if multiple roles exist
        if (role.roleName !== 'Viewer' || roles.length === 1) {
            // Ensure roleId is the right type (number if numeric)
            const roleId = isNaN(Number(role.roleId)) ? role.roleId : Number(role.roleId);
            acc[roleId] = role.roleName;
        }
        return acc;
    }, {});

    // Log the processed role options
    console.log('Processed role options:', roleOptions);

    // If no valid roles found
    if (Object.keys(roleOptions).length === 0) {
        Swal.fire({
            icon: 'info',
            title: 'No Roles',
            text: 'Could not find any roles to delete'
        });
        return;
    }

    Swal.fire({
        title: 'Select Role to Remove',
        text: 'This will replace the selected role with Viewer role',
        input: 'select',
        inputOptions: roleOptions,
        inputPlaceholder: 'Select a role to remove',
        showCancelButton: true,
        confirmButtonText: 'Remove Role',
        preConfirm: (selectedRoleId) => {
            if (!selectedRoleId) {
                Swal.showValidationMessage('Please select a role');
            }
            return selectedRoleId;
        }
    }).then((result) => {
        if (result.isConfirmed) {
            // Ensure we use the right type for roleId when passing to deleteUserRole
            const roleId = isNaN(Number(result.value)) ? result.value : Number(result.value);
            deleteUserRole(userId, roleId);
        }
    });
}


// Delete Entire User with improved error handling
async function deleteUser(userId) {
    const token = sessionStorage.getItem('authToken');

    try {
        const result = await Swal.fire({
            title: 'Are you sure?',
            text: 'Do you want to delete this user permanently?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, delete it!'
        });

        if (result.isConfirmed) {
            const response = await fetch(`${BASE_URL}/User/${userId}`, {
                method: 'DELETE',
                headers: {
                    'accept': '*/*',
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'Deleted!',
                    text: 'User has been deleted successfully.'
                });
                fetchUsers(); // Refresh the user list
            } else {
                const errorText = await response.text();
                console.error('Delete user error:', errorText);
                Swal.fire({
                    icon: 'error',
                    title: 'Delete Failed',
                    text: errorText || 'Failed to delete user'
                });
            }
        }
    } catch (error) {
        console.error('Error deleting user:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'An unexpected error occurred'
        });
    }
}

// Delete User Role with improved error handling
// Delete User Role with role replacement logic
async function deleteUserRole(userId, roleId) {
    const token = sessionStorage.getItem('authToken');

    try {
        const result = await Swal.fire({
            title: 'Are you sure?',
            text: 'This will replace the current role with Viewer role',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, change role!'
        });

        if (result.isConfirmed) {
            // Log the request details for debugging
            console.log(`Deleting role ID: ${roleId} for user ID: ${userId}`);

            // Ensure roleId is the correct type
            const processedRoleId = isNaN(Number(roleId)) ? roleId : Number(roleId);
            const endpoint = `${BASE_URL}/User/${userId}/roles/${processedRoleId}`;
            console.log('DELETE request to:', endpoint);

            const response = await fetch(endpoint, {
                method: 'DELETE',
                headers: {
                    'accept': 'application/json',
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                let resultMessage = 'Role has been changed successfully.';

                try {
                    const resultData = await response.json();
                    if (resultData && resultData.message) {
                        resultMessage = resultData.message;
                    }
                    console.log('Response data:', resultData);
                } catch (e) {
                    console.log('Response was not JSON or could not be parsed:', e);
                    // Try to get text instead
                    try {
                        const textResponse = await response.text();
                        if (textResponse) {
                            console.log('Text response:', textResponse);
                        }
                    } catch (textError) {
                        console.log('Could not get text response:', textError);
                    }
                }

                Swal.fire({
                    icon: 'success',
                    title: 'Role Changed',
                    text: resultMessage
                });

                fetchUsers(); // Refresh the user list
            } else {
                // Get error details
                let errorMessage = 'Failed to change user role';

                try {
                    // Try to parse as JSON first
                    const errorData = await response.json();
                    console.error('Error data:', errorData);

                    if (errorData && typeof errorData === 'object') {
                        if (errorData.message) {
                            errorMessage = errorData.message;
                        } else if (errorData.title) {
                            errorMessage = errorData.title;
                        } else if (errorData.errors) {
                            const errorKeys = Object.keys(errorData.errors);
                            if (errorKeys.length > 0) {
                                errorMessage = errorData.errors[errorKeys[0]].join(', ');
                            }
                        }
                    }
                } catch (jsonError) {
                    // If not JSON, try to get as text
                    try {
                        const errorText = await response.text();
                        if (errorText) {
                            errorMessage = errorText;
                        }
                    } catch (textError) {
                        console.error('Could not parse error response as text:', textError);
                    }
                }

                console.error('Delete user role error:', errorMessage);
                console.log('Response status:', response.status);
                console.log('Response headers:', Object.fromEntries(response.headers.entries()));

                Swal.fire({
                    icon: 'error',
                    title: 'Role Change Failed',
                    text: errorMessage
                });
            }
        }
    } catch (error) {
        console.error('Error changing user role:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: error.message || 'An unexpected error occurred'
        });
    }
}

// Event Listeners
document.getElementById('updateUserForm').addEventListener('submit', updateUser);

// Initial load
document.addEventListener('DOMContentLoaded', () => {
    // Check if user is logged in
    const token = sessionStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'auth-login.html'; // Redirect to login page
        return;
    }

    // Disable Add User button for non-Super Admins
    const addUserBtn = document.getElementById('addUserBtn');
    if (!isSuperAdmin()) {
        addUserBtn.disabled = true;
        addUserBtn.setAttribute('title', 'Only Super Admin can add users');
    }

    // Initialize filter checkboxes
    initializeFilterCheckboxes();

    // Load user roles and populate roles
    loadUserRoles();
    fetchAndPopulateRoles();

    // Fetch users and apply filters
    fetchUsers();

    // Add event listener for add user form
    document.getElementById('addUserForm').addEventListener('submit', handleAddUser);
    document.getElementById('updateUserForm').addEventListener('submit', updateUser);
    document.getElementById('updateUserRoleForm').addEventListener('submit', updateUserRole);
});
