document.addEventListener('DOMContentLoaded', function () {
    const rolesContainer = document.querySelector('.row.justify-content-center');
    const token = sessionStorage.getItem('authToken');

    // Fetch roles and users
    Promise.all([
        fetch('https://localhost:44320/api/Roles', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'accept': 'text/plain'
            }
        }),
        fetch('https://localhost:44320/api/User', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'accept': 'text/plain'
            }
        })
    ])
        .then(responses => Promise.all(responses.map(response => response.json())))
        .then(([roles, users]) => {
            // Find SuperAdmin user
            const superAdmin = users.find(user =>
                user.roles.includes('SuperAdmin')
            );

            // Clear existing content
            rolesContainer.innerHTML = '';

            // Create card for each role
            roles.forEach(role => {
                const card = createRoleCard(role, superAdmin);
                rolesContainer.appendChild(card);
            });
        })
        .catch(error => {
            console.error('Error fetching roles or users:', error);
        });

    // Function to create role card
    function createRoleCard(role, superAdmin) {
        const col = document.createElement('div');
        col.className = 'col-md-6 col-lg-4';

        // Determine badge color based on role
        const badgeColors = {
            'SuperAdmin': 'bg-danger-subtle text-danger',
            'Admin': 'bg-primary-subtle text-primary',
            'Editor': 'bg-warning-subtle text-warning',
            'Viewer': 'bg-info-subtle text-info',
            'Uploader': 'bg-success-subtle text-success'
        };

        col.innerHTML = `
            <div class="card">
                <div class="card-body">
                    <div class="">
                        <div class="mt-2">
                            <span class="badge ${badgeColors[role.roleName] || 'bg-purple-subtle text-purple'} px-2 py-1 fw-semibold">
                                ${role.roleName}
                            </span>
                            |
                            <p class="mb-0 text-muted fs-12 d-inline-block">${new Date().toLocaleDateString()}</p>
                        </div>
                        <a href="#" class="d-block fs-22 fw-semibold text-body my-2 text-truncate">
                            ${role.roleName} Role
                        </a>
                        <p class="text-muted">${role.description}</p>
                        <hr class="hr-dashed">
                        <div class="d-flex justify-content-between">
                            <div class="d-flex align-items-center">
                                <div class="flex-grow-1 ms-2 text-truncate text-start">
                                    <h6 class="m-0 text-dark">
                                        ${superAdmin
                ? `${superAdmin.firstName} ${superAdmin.lastName}`
                : 'No SuperAdmin Found'}
                                    </h6>
                                    <p class="mb-0 text-muted">by <a href="#">${role.roleName}</a></p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        return col;
    }
});