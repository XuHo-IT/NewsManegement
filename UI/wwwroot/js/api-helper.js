// Common API Helper Functions

const ApiHelper = {
    baseUrl: 'https://localhost:5001',
    
    getToken: function() {
        // Note: Token is stored in session, this would be called from the server
        // In a real scenario, this would be handled via HTTP-only cookies or session
        return localStorage.getItem('authToken');
    },

    setToken: function(token) {
        localStorage.setItem('authToken', token);
    },

    makeRequest: function(endpoint, method = 'GET', body = null, includeAuth = true) {
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json'
            }
        };

        if (includeAuth) {
            const token = this.getToken();
            if (token) {
                options.headers['Authorization'] = `Bearer ${token}`;
            }
        }

        if (body && (method === 'POST' || method === 'PUT')) {
            options.body = JSON.stringify(body);
        }

        return fetch(`${this.baseUrl}${endpoint}`, options);
    },

    getJsonAsync: function(endpoint) {
        return this.makeRequest(endpoint)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            });
    },

    postJsonAsync: function(endpoint, data) {
        return this.makeRequest(endpoint, 'POST', data)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            });
    },

    putJsonAsync: function(endpoint, data) {
        return this.makeRequest(endpoint, 'PUT', data)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            });
    },

    deleteAsync: function(endpoint) {
        return this.makeRequest(endpoint, 'DELETE')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.status === 204 ? null : response.json();
            });
    }
};

// Utility Functions
const Utils = {
    getStatusName: function(status) {
        const statuses = {
            1: 'Draft',
            2: 'Pending',
            3: 'Approved',
            4: 'Published',
            5: 'Archived'
        };
        return statuses[status] || 'Unknown';
    },

    getStatusBadgeClass: function(status) {
        const classes = {
            1: 'bg-secondary',      // Draft
            2: 'bg-warning',        // Pending
            3: 'bg-info',          // Approved
            4: 'bg-success',       // Published
            5: 'bg-dark'           // Archived
        };
        return classes[status] || 'bg-secondary';
    },

    formatDate: function(dateString) {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    },

    formatDateTime: function(dateString) {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    showAlert: function(message, type = 'info') {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const container = document.querySelector('main') || document.body;
        container.insertBefore(alertDiv, container.firstChild);

        setTimeout(() => {
            alertDiv.remove();
        }, 5000);
    },

    showErrorAlert: function(message) {
        this.showAlert(message, 'danger');
    },

    showSuccessAlert: function(message) {
        this.showAlert(message, 'success');
    },

    showLoadingSpinner: function(containerId) {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = `
                <div class="text-center py-5">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="mt-3">Loading...</p>
                </div>
            `;
        }
    },

    clearContainer: function(containerId) {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = '';
        }
    }
};

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ApiHelper, Utils };
}
