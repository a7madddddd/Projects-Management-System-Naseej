
function disableRoleInput() {
    const roleInput = document.querySelector('input[name="Role"]');
    if (roleInput) {
        roleInput.setAttribute('disabled', 'true');
        roleInput.setAttribute('readonly', 'true');
    }
}
// Function to fetch user data
async function fetchUserData() {
    // Get the auth token from session storage
    const authToken = sessionStorage.getItem('authToken');

    // Extract userId from the token (assuming it's stored in the token)
    const token = sessionStorage.getItem('authToken');
    const tokenPayload = JSON.parse(atob(token.split('.')[1]));
    const userId = tokenPayload.UserId;

    try {
        // Fetch user data
        const userResponse = await fetch(`https://localhost:44320/api/User/${userId}`, {
            method: 'GET',
            headers: {
                'Accept': 'text/plain',
                'Authorization': `Bearer ${authToken}`
            }
        });

        // Fetch files list
        const filesResponse = await fetch(`https://localhost:44320/api/Files/list-files?PageNumber=1&PageSize=10`, {
            method: 'GET',
            headers: {
                'Accept': '*/*',
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (!userResponse.ok || !filesResponse.ok) {
            throw new Error('Failed to fetch data');
        }

        const userData = await userResponse.json();
        const filesData = await filesResponse.json();

        // Calculate user files count
        const userFilesCount = filesData.files.filter(file => file.uploadedBy === parseInt(userId)).length;

        // Update HTML elements with fetched data
        updateUserProfile(userData, userFilesCount);
        disableRoleInput();

    } catch (error) {
        console.error('Error fetching data:', error);
    }
}

// Function to update user profile in the HTML
function updateUserProfile(userData, userFilesCount) {
    // Update personal information inputs
    const firstNameInput = document.querySelector('input[value="Rosa"]');
    const lastNameInput = document.querySelector('input[value="Dodson"]');
    const emailInput = document.querySelector('input[placeholder="Email"]');
    const roleInput = document.querySelector('input[placeholder="Role"]');
    
    // Update inputs
    if (firstNameInput) firstNameInput.value = userData.firstName;
    if (lastNameInput) lastNameInput.value = userData.lastName;
    if (emailInput) emailInput.value = userData.email;
    if (roleInput) roleInput.value = userData.roles[0] || 'No Role';

    // Update profile header
    const nameElement = document.querySelector('.flex-grow-1 h4');
    const usernameElement = document.querySelector('.flex-grow-1 p');
    const emailLinkElement = document.querySelector('.text-muted.mb-2 a');

    if (nameElement) nameElement.textContent = `${userData.firstName} ${userData.lastName}`;
    if (usernameElement) usernameElement.textContent = `@${userData.username}`;
    if (emailLinkElement) emailLinkElement.textContent = userData.email;

    // Update statistics cards
    const totalFilesElement = document.querySelector('.col-md-6.col-lg-3:nth-child(1) h3');
    const roleElement = document.querySelector('.col-md-6.col-lg-3:nth-child(2) h6');
    const addedAtElement = document.querySelector('.col-md-6.col-lg-3:nth-child(3) h3');
    const lastUpdateElement = document.querySelector('.col-md-6.col-lg-3:nth-child(4) h3');

    if (totalFilesElement) totalFilesElement.textContent = userFilesCount;
    if (roleElement) roleElement.textContent = userData.roles[0] || 'Role';


    function formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    if (addedAtElement) addedAtElement.textContent = formatDate(userData.createdDate);
    if (lastUpdateElement) lastUpdateElement.textContent = formatDate(userData.updatedDate);
}

// Call the function when the page loads
document.addEventListener('DOMContentLoaded', fetchUserData);
async function updateUserInfo() {
    const authToken = sessionStorage.getItem('authToken');
    if (!authToken) {
        console.error("No auth token found");
        return;
    }

    const tokenPayload = JSON.parse(atob(authToken.split('.')[1]));
    const userId = tokenPayload.UserId;

    const updatedUserData = {
        email: document.querySelector('input[name="email"]').value,
        firstName: document.querySelector('input[name="firstName"]').value,
        lastName: document.querySelector('input[name="lastName"]').value,
        isActive: true,  // Ensure this matches API expectations
    };

    try {
        const response = await fetch(`https://localhost:44320/api/User/${userId}`, {
            method: 'PUT',
            headers: {
                "Content-Type": "application/json",
                "Accept": "text/plain",
                "Authorization": `Bearer ${authToken}`,
            },
            body: JSON.stringify(updatedUserData),
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to update user information: ${errorText}`);
        }

        Swal.fire({
            icon: "success",
            title: "Profile Updated!",
            text: "Your profile information has been successfully updated.",
            timer: 3000,
            showConfirmButton: false
        });

       

    } catch (error) {
        console.error("Error updating user info:", error);
        Swal.fire({
            icon: "error",
            title: "Update Failed",
            text: "Something went wrong while updating your profile.",
            confirmButtonText: "OK"
        });
    }
}

// Attach to the Submit button
document.querySelector(".btn-primary").addEventListener("click", (event) => {
    event.preventDefault(); // Prevent default form submission
    updateUserInfo();
});

document.addEventListener('DOMContentLoaded', function () {
    // Select elements
    const currentPasswordInput = document.querySelector('input[placeholder="Password"]');
    const newPasswordInput = document.querySelector('input[placeholder="New Password"]');
    const confirmPasswordInput = document.querySelector('input[placeholder="Re-Password"]');
    const changePasswordButton = document.getElementById('changePasswordBtn');
    const cancelButton = document.getElementById('cancelPasswordBtn');

    // Validate element existence
    if (!currentPasswordInput || !newPasswordInput || !confirmPasswordInput ||
        !changePasswordButton || !cancelButton) {
        console.error('One or more password reset elements not found');
        return;
    }

    // Reset form function
    function resetForm() {
        currentPasswordInput.value = '';
        newPasswordInput.value = '';
        confirmPasswordInput.value = '';
    }

    // Change password function
    async function handlePasswordChange() {
        const currentPassword = currentPasswordInput.value.trim();
        const newPassword = newPasswordInput.value.trim();
        const confirmPassword = confirmPasswordInput.value.trim();

        // Basic validation
        if (!currentPassword || !newPassword || !confirmPassword) {
            Swal.fire({
                icon: 'warning',
                title: 'Validation Error',
                text: 'All fields are required'
            });
            return;
        }

        // Check if new passwords match
        if (newPassword !== confirmPassword) {
            Swal.fire({
                icon: 'warning',
                title: 'Password Mismatch',
                text: 'New passwords do not match'
            });
            return;
        }

        try {
            // Get user ID from token
            const authToken = sessionStorage.getItem('authToken');
            const tokenPayload = JSON.parse(atob(authToken.split('.')[1]));
            const userId = tokenPayload.UserId;

            // Make API call to reset password
            const response = await fetch('https://localhost:44320/api/User/reset-password', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    userId: parseInt(userId),
                    currentPassword: currentPassword,
                    newPassword: newPassword,
                    confirmPassword: confirmPassword
                })
            });

            // Parse response
            let result;
            try {
                result = await response.json();
            } catch (parseError) {
                // If response is not JSON, use text
                const responseText = await response.text();
                result = { Message: responseText };
            }

            // Check response status
            if (response.ok) {
                // Success scenario
                Swal.fire({
                    icon: 'success',
                    title: 'Password Changed',
                    text: result.Message || 'Password updated successfully'
                });

                // Reset form
                resetForm();
            } else {
                // Error scenario
                Swal.fire({
                    icon: 'error',
                    title: 'Password Reset Failed',
                    text: result.Message || 'Your Last Paswword Is Wrong'
                });

                // Log the error for debugging
                console.error('Password reset error:', result);
            }

        } catch (error) {
            console.error('Fetch Error:', error);

            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: error.message || 'An unexpected error occurred'
            });
        }
    }

    // Add event listeners
    changePasswordButton.addEventListener('click', handlePasswordChange);

    cancelButton.addEventListener('click', function () {
        resetForm();
    });
});