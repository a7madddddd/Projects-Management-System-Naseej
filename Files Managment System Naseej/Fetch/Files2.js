class FileService {
    constructor() {
        this.baseUrl = 'https://localhost:44320/api';
        this.token = sessionStorage.getItem('authToken');
    }

    // Get authentication headers
    getHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`,
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        };
    }

    // Fetch files
    async fetchFiles(pageNumber = 1, pageSize = 5, searchQuery = '') {
        try {
            const response = await fetch(
                `${this.baseUrl}/Files/list-files?PageNumber=${pageNumber}&PageSize=${pageSize}&SearchQuery=${searchQuery}`,
                {
                    method: 'GET',
                    headers: this.getHeaders()
                }
            );

            if (!response.ok) {
                throw new Error('Failed to fetch files');
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching files:', error);
            throw error;
        }
    }   

    // Get file view link
    async getFileViewLink(googleDriveFileId) {
        // Only attempt to get view link if googleDriveFileId is not null
        if (!googleDriveFileId) {
            throw new Error('No Google Drive File ID available');
        }

        try {
            const response = await fetch(`${this.baseUrl}/Files/view-link/${googleDriveFileId}`, {
                method: 'GET',
                headers: this.getHeaders()
            });

            if (!response.ok) {
                throw new Error('Failed to get file view link');
            }

            return await response.json();
        } catch (error) {
            console.error('Error getting file view link:', error);
            throw error;
        }
    }
}

// Render functions
class FileRenderer {
    constructor(tableBodyId) {
        this.tableBody = document.getElementById(tableBodyId);
        this.fileService = new FileService();
    }

    // Render local files
    renderLocalFiles(files) {
        // Clear existing rows
        this.tableBody.innerHTML = '';

        // Check if there are any files
        if (files.length === 0) {
            this.showNoFilesMessage();
            return;
        }

        files.forEach(file => {
            // Use arrow function to preserve 'this' context
            const row = this.createFileRow(file);
            this.tableBody.appendChild(row);
        });
    }

    // Create file row method
    createFileRow(file) {
        const row = document.createElement('tr');

        // Determine file icon based on extension
        const fileIcon = this.getFileIcon(file.fileExtension);

        // Name column
        const nameCell = document.createElement('td');
        nameCell.innerHTML = `
            <div class="d-inline-flex justify-content-center align-items-center thumb-md bg-blue-subtle rounded mx-auto me-1">
                <i class="${fileIcon} fs-18 align-self-center mb-0 text-blue"></i>
            </div>
            <a href="#" class="text-body">${file.fileName}</a>
        `;
        row.appendChild(nameCell);

        // Last Modified column
        const modifiedCell = document.createElement('td');
        modifiedCell.className = 'text-end';
        modifiedCell.textContent = this.formatDate(file.uploadDate);
        row.appendChild(modifiedCell);

        // Size column
        const sizeCell = document.createElement('td');
        sizeCell.className = 'text-end';
        sizeCell.textContent = this.formatFileSize(file.fileSize);
        row.appendChild(sizeCell);

        // Members column (placeholder)
        const membersCell = document.createElement('td');
        membersCell.className = 'text-end';
        membersCell.textContent = file.uploadedByName || 'Unknown';
        row.appendChild(membersCell);


        // Action column
        const actionCell = document.createElement('td');
        actionCell.className = 'text-end';
        actionCell.innerHTML = this.createActionButtons(file);
        row.appendChild(actionCell);

        return row;
    }

    // Show message when no files are available
    showNoFilesMessage() {
        const row = document.createElement('tr');
        const cell = document.createElement('td');
        cell.setAttribute('colspan', '5');
        cell.classList.add('text-center', 'text-muted');
        cell.textContent = 'No files synced with Google Drive';
        row.appendChild(cell);
        this.tableBody.appendChild(row);
    }

    // Get file icon based on extension
    getFileIcon(extension) {
        const extensionMap = {
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

        return extensionMap[extension] || extensionMap['default'];
    }

    // Create action buttons
    createActionButtons(file) {
        return `
            <a href="#" onclick="viewFileDetails('${file.googleDriveFileId}')">
                <i class="las la-info-circle text-secondary fs-18"></i>
            </a>
            <a href="#" onclick="downloadFile('${file.googleDriveFileId}')">
                <i class="las la-download text-secondary fs-18"></i>
            </a>
            <a href="#" onclick="updateFile('${file.googleDriveFileId}')">
                <i class="las la-pen text-secondary fs-18"></i>
            </a>
            <a href="#" onclick="deleteFile('${file.googleDriveFileId}')">
                <i class="las la-trash-alt text-secondary fs-18"></i>
            </a>
        `;
    }

    // Format date
    formatDate(dateString) {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('en-US', {
            day: 'numeric',
            month: 'short',
            year: 'numeric'
        });
    }

    // Format file size
    formatFileSize(bytes) {
        if (!bytes) return 'N/A';
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
        if (bytes === 0) return '0 Byte';
        const i = parseInt(Math.floor(Math.log(bytes) / Math.log(1024)));
        return Math.round(bytes / Math.pow(1024, i), 2) + ' ' + sizes[i];
    }
}

// Global action handlers
async function viewFileDetails(googleDriveFileId) {
    // Only proceed if googleDriveFileId is not null
    if (!googleDriveFileId) {
        alert('No Google Drive link available for this file.');
        return;
    }

    try {
        const fileService = new FileService();
        const viewLink = await fileService.getFileViewLink(googleDriveFileId);

        // Open the view link in a new tab
        if (viewLink.webViewLink) {
            window.open(viewLink.webViewLink, '_blank');
        } else {
            alert('No view link available for this file.');
        }
    } catch (error) {
        console.error('Error viewing file details:', error);
        alert('Failed to get file view link.');
    }
}

// Wrap everything in a module pattern to avoid global scope pollution
class FileActionService {
    constructor() {
        this.baseUrl = 'https://localhost:44320/api';
        this.token = sessionStorage.getItem('authToken');
    }

    // Get authentication headers
    getHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`
        };
    }

    // Download file from Google Drive
    async downloadFile(googleDriveFileId) {
        try {
            const response = await fetch(`${this.baseUrl}/Files/download-from-drive/${googleDriveFileId}`, {
                method: 'GET',
                headers: this.getHeaders()
            });

            if (!response.ok) {
                throw new Error('Failed to download file');
            }

            // Get filename from content-disposition header
            const filename = this.getFilenameFromHeader(response.headers);

            // Convert response to blob
            const blob = await response.blob();

            // Create a link element to trigger download
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();

            // Clean up
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            Swal.fire({
                icon: 'success',
                title: 'Download Started',
                text: `File ${filename} is being downloaded`
            });
        } catch (error) {
            console.error('Download error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Download Failed',
                text: error.message || 'Unable to download file'
            });
        }
    }

    // Extract filename from content-disposition header
    getFilenameFromHeader(headers) {
        const contentDisposition = headers.get('content-disposition');
        if (contentDisposition) {
            const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
            if (filenameMatch && filenameMatch[1]) {
                return filenameMatch[1].replace(/['"]/g, '');
            }
        }
        return 'downloaded-file';
    }

    // Update file in Google Drive
    async updateFile(googleDriveFileId, file) {
        try {
            const formData = new FormData();
            formData.append('file', file);

            const response = await fetch(`${this.baseUrl}/Files/update-in-drive/${googleDriveFileId}`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${this.token}`
                },
                body: formData
            });

            if (!response.ok) {
                throw new Error('Failed to update file');
            }

            const result = await response.json();

            Swal.fire({
                icon: 'success',
                title: 'File Updated',
                text: result.message || 'File successfully updated in Google Drive'
            });

            // Refresh file list if function exists
            if (typeof initializeFileTable === 'function') {
                initializeFileTable();
            }

            return result;
        } catch (error) {
            console.error('Update file error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Update Failed',
                text: error.message || 'Unable to update file'
            });
            throw error;
        }
    }

    // Delete file
    async deleteFile(fileId) {
        try {
            // Confirm deletion
            const result = await Swal.fire({
                title: 'Are you sure?',
                text: 'Do you want to delete this file from Google Drive?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            });

            if (result.isConfirmed) {
                // Send delete request to the new endpoint
                const response = await fetch(`${this.baseUrl}/Files/delete-file/${fileId}`, {
                    method: 'DELETE',
                    headers: this.getHeaders()
                });

                // Check response
                if (!response.ok) {
                    // Try to parse error response
                    const errorData = await response.json().catch(() => ({}));

                    throw new Error(
                        errorData.message ||
                        `Failed to delete file. Status: ${response.status}`
                    );
                }

                // Parse successful response
                const deleteResult = await response.json();

                // Show success message
                Swal.fire({
                    icon: 'success',
                    title: 'Deleted!',
                    text: deleteResult.message || 'The file has been deleted successfully from Google Drive.'
                });

                // Refresh file list if function exists
                if (typeof initializeFileTable === 'function') {
                    initializeFileTable();
                }

                return deleteResult;
            }
        } catch (error) {
            console.error('Delete file error:', error);

            Swal.fire({
                icon: 'error',
                title: 'Deletion Failed',
                text: error.message || 'Unable to delete file',
                footer: '<a href="#">Contact support if the problem persists</a>'
            });

            throw error;
        }
    }
}

// Create a global object to hold the service instance
window.FileActions = {
    service: null,

    // Initialize method
    init: function () {
        this.service = new FileActionService();
    },

    // Wrapper methods
    downloadFile: function (googleDriveFileId) {
        if (this.service) {
            this.service.downloadFile(googleDriveFileId);
        } else {
            console.error('File action service not initialized');
        }
    },

    updateFile: function (googleDriveFileId) {
        if (this.service) {
            // Trigger file input for update
            const fileInput = document.createElement('input');
            fileInput.type = 'file';
            fileInput.onchange = (event) => {
                const file = event.target.files[0];
                if (file) {
                    this.service.updateFile(googleDriveFileId, file);
                }
            };
            fileInput.click();
        } else {
            console.error('File action service not initialized');
        }
    },

    deleteFile: function (fileId) {
        if (this.service) {
            this.service.deleteFile(fileId);
        } else {
            console.error('File action service not initialized');
        }
    }
};

// Global functions that can be used in HTML onclick events
window.downloadFile = function (googleDriveFileId) {
    FileActions.downloadFile(googleDriveFileId);
};

window.updateFile = function (googleDriveFileId) {
    FileActions.updateFile(googleDriveFileId);
};

window.deleteFile = function (fileId) {
    FileActions.deleteFile(fileId);
};

// Initialize the service when the document loads
document.addEventListener('DOMContentLoaded', () => {
    FileActions.init();
});




// Main initialization
async function initializeFileTable() {
    const fileService = new FileService();
    const fileRenderer = new FileRenderer('documents-table-body');

    try {
        // Fetch and render local files
        const localFiles = await fileService.fetchFiles();
        fileRenderer.renderLocalFiles(localFiles.files);
    } catch (error) {
        console.error('Error initializing file table:', error);
        const tableBody = document.getElementById('documents-table-body');
        tableBody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-danger">
                    Failed to load files. ${error.message}
                </td>
            </tr>
        `;
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeFileTable);

class FileUploadService {
    constructor() {
        this.baseUrl = 'https://localhost:44320/api';
        this.token = sessionStorage.getItem('authToken');
    }

    // Get authentication headers
    getHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`
        };
    }

    // Upload file to drive
    async uploadFileToDrive(file) {
        const formData = new FormData();
        formData.append('file', file);

        try {
            const response = await fetch(`${this.baseUrl}/Files/upload-to-drive`, {
                method: 'POST',
                headers: this.getHeaders(),
                body: formData
            });

            if (!response.ok) {
                // Try to parse error response
                let errorText = 'File upload failed';
                try {
                    const errorResponse = await response.json();
                    errorText = errorResponse.message || errorResponse.details || errorText;
                } catch {
                    errorText = await response.text() || errorText;
                }

                throw new Error(errorText);
            }

            return await response.json();
        } catch (error) {
            console.error('Upload error:', error);
            throw error;
        }
    }
}

class FileUploadHandler {
    constructor() {
        this.uploadService = new FileUploadService();
        this.initEventListeners();
    }

    initEventListeners() {
        // Use specific IDs for reliable selection
        const fileInput = document.getElementById('fileUploadInput');
        const uploadButton = document.getElementById('uploadFileButton');

        if (fileInput && uploadButton) {
            // Add change event listener to the file input
            fileInput.addEventListener('change', (e) => this.handleFileSelection(e));

            // Add click event to button to trigger file input
            uploadButton.addEventListener('click', () => {
                fileInput.click();
            });
        } else {
            console.error('File upload elements not found', {
                fileInputFound: !!fileInput,
                uploadButtonFound: !!uploadButton
            });
        }
    }

    // Validate files before upload
    validateFiles(files) {
        const allowedTypes = [
            'application/pdf',
            'application/vnd.ms-excel',
            'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
            'application/msword',
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
            'text/plain',
            'image/jpeg',
            'image/png'
        ];

        const maxFileSize = 50 * 1024 * 1024; // 50 MB

        return Array.from(files).filter(file => {
            const isValidType = allowedTypes.includes(file.type);
            const isValidSize = file.size <= maxFileSize;

            if (!isValidType || !isValidSize) {
                console.warn(`Invalid file: ${file.name}`, {
                    type: file.type,
                    size: file.size,
                    validType: isValidType,
                    validSize: isValidSize
                });
            }

            return isValidType && isValidSize;
        });
    }

    async handleFileSelection(event) {
        const files = event.target.files;
        if (files.length === 0) return;

        try {
            // Validate files
            const validFiles = this.validateFiles(files);

            if (validFiles.length === 0) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Invalid Files',
                    text: 'No valid files selected. Please check file types and sizes.'
                });
                return;
            }

            // Confirm upload with SweetAlert
            const result = await Swal.fire({
                title: 'Upload Files',
                text: `Are you sure you want to upload ${validFiles.length} file(s) to Google Drive?`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Yes, upload!',
                cancelButtonText: 'Cancel'
            });

            if (result.isConfirmed) {
                await this.uploadFiles(validFiles);
            }
        } catch (error) {
            this.handleUploadError(error);
        }
    }

    async uploadFiles(files) {
        // Show loading alert
        const uploadAlert = Swal.fire({
            title: 'Uploading...',
            html: 'Please wait while files are being uploaded',
            didOpen: () => {
                Swal.showLoading();
            },
            allowOutsideClick: false
        });

        try {
            const uploadPromises = files.map(file =>
                this.uploadService.uploadFileToDrive(file)
            );

            const results = await Promise.allSettled(uploadPromises);

            // Close loading alert
            Swal.close();

            // Process upload results
            const successfulUploads = results.filter(r => r.status === 'fulfilled');
            const failedUploads = results.filter(r => r.status === 'rejected');

            // Detailed results tracking
            const successDetails = successfulUploads.map(result =>
                `${result.value.fileName || 'Unknown file'}`
            );
            const failedDetails = failedUploads.map(result =>
                `${result.reason.message || 'Unknown error'}`
            );

            if (successfulUploads.length > 0) {
                Swal.fire({
                    icon: 'success',
                    title: 'Upload Successful',
                    html: `
                        ${successfulUploads.length} file(s) uploaded successfully
                        ${failedUploads.length > 0 ? `<br>${failedUploads.length} file(s) failed to upload` : ''}
                        ${successDetails.length > 0 ? `<br>Successful files: ${successDetails.join(', ')}` : ''}
                        ${failedDetails.length > 0 ? `<br>Failed files: ${failedDetails.join(', ')}` : ''}
                    `
                });

                // Refresh file list
                if (typeof initializeFileTable === 'function') {
                    initializeFileTable();
                }
            } else {
                throw new Error('No files were uploaded successfully');
            }
        } catch (error) {
            this.handleUploadError(error);
        }
    }

    handleUploadError(error) {
        Swal.fire({
            icon: 'error',
            title: 'Upload Failed',
            text: error.message || 'An error occurred during file upload',
            footer: '<a href="#">Contact support if the problem persists</a>'
        });
    }
}

// Initialize file upload handler
document.addEventListener('DOMContentLoaded', () => {
    // Ensure the file input exists before initializing
    const fileInput = document.getElementById('fileUploadInput');
    const uploadButton = document.getElementById('uploadFileButton');

    if (fileInput && uploadButton) {
        new FileUploadHandler();
    } else {
        console.error('File upload elements not found', {
            fileInput: !!fileInput,
            uploadButton: !!uploadButton
        });
    }
});