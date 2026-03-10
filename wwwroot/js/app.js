/**
 * QuickBooks Custom Fields Manager
 * Main JavaScript Application
 */

class QuickBooksCustomFieldsApp {
    constructor() {
        this.apiBaseUrl = '/api';
        this.isAuthenticated = false;
        this.currentToken = null;
        this.customFields = [];
        this.editingFieldId = null;
        this.invoices = [];
        this.customers = [];
        this.items = [];
        this.lineItemCounter = 0;
        this.currentInvoicePage = 1;
        this.invoicePageSize = 10;
        this.invoiceHasMore = false;
        this.editingInvoice = null;
        
        // Initialize the application
        this.init();
    }

    /**
     * Initialize the application
     */
    async init() {
        console.log('Initializing QuickBooks Custom Fields App...');
        this.bindEvents();
        console.log('Events bound, checking auth status...');
        await this.checkAuthStatus();
        
        // Listen for OAuth callback messages
        window.addEventListener('message', (event) => {
            if (event.data.type === 'oauth_success') {
                this.handleOAuthSuccess(event.data.realmId);
            } else if (event.data.type === 'oauth_error') {
                this.showToast('Authentication failed: ' + event.data.error, 'error');
            }
        });
        console.log('App initialization complete');
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        console.log('Binding events...');
        
        // Authentication events
        document.getElementById('connectBtn').addEventListener('click', () => this.initiateOAuth());
        document.getElementById('refreshTokenBtn').addEventListener('click', () => this.refreshToken());
        document.getElementById('disconnectBtn').addEventListener('click', () => this.disconnect());

        // Custom fields management events
        document.getElementById('refreshFieldsBtn').addEventListener('click', () => this.loadCustomFields());
        document.getElementById('createFieldBtn').addEventListener('click', () => this.openCreateFieldModal());
        document.getElementById('saveFieldBtn').addEventListener('click', () => this.saveField());

        // Invoice management events
        const refreshInvoicesBtn = document.getElementById('refreshInvoicesBtn');
        const createInvoiceBtn = document.getElementById('createInvoiceBtn');
        const saveInvoiceBtn = document.getElementById('saveInvoiceBtn');
        const addLineItemBtn = document.getElementById('addLineItemBtn');
        
        if (refreshInvoicesBtn) {
            console.log('Binding refreshInvoicesBtn');
            refreshInvoicesBtn.addEventListener('click', () => this.loadInvoices());
        } else {
            console.error('refreshInvoicesBtn not found!');
        }
        
        if (createInvoiceBtn) {
            console.log('Binding createInvoiceBtn');
            createInvoiceBtn.addEventListener('click', () => this.openCreateInvoiceModal());
        } else {
            console.error('createInvoiceBtn not found!');
        }
        
        if (saveInvoiceBtn) {
            saveInvoiceBtn.addEventListener('click', () => this.saveInvoice());
        } else {
            console.error('saveInvoiceBtn not found!');
        }
        
        if (addLineItemBtn) {
            addLineItemBtn.addEventListener('click', () => this.addLineItem());
        } else {
            console.error('addLineItemBtn not found!');
        }

        // Search functionality
        document.getElementById('searchFields').addEventListener('input', (e) => this.filterFields(e.target.value));

        // Entity type change handler
        document.getElementById('entityType').addEventListener('change', (e) => this.handleEntityTypeChange(e.target.value));

        // Modal events
        document.getElementById('fieldModal').addEventListener('hidden.bs.modal', () => this.resetFieldForm());
        
        const invoiceModal = document.getElementById('invoiceModal');
        if (invoiceModal) {
            invoiceModal.addEventListener('hidden.bs.modal', () => this.resetInvoiceForm());
        } else {
            console.error('invoiceModal not found!');
        }
        
        console.log('Event binding complete');
    }

    /**
     * Check current authentication status
     */
    async checkAuthStatus() {
        try {
            console.log('Checking auth status...');
            const response = await fetch(`${this.apiBaseUrl}/oauth/status`);
            const result = await response.json();
            console.log('Auth status result:', result);

            if (result.success && result.data.isAuthenticated) {
                console.log('User is authenticated, loading data...');
                this.isAuthenticated = true;
                this.currentToken = result.data;
                this.showAuthenticatedState();
                await this.verifyScopes();
                console.log('About to load custom fields...');
                await this.loadCustomFields();
                console.log('About to load invoices...');
                await this.loadInvoices();
                console.log('Data loading complete');
            } else {
                console.log('User not authenticated');
                this.showUnauthenticatedState();
            }
        } catch (error) {
            console.error('Error checking auth status:', error);
            this.showUnauthenticatedState();
        }
    }

    /**
     * Initiate OAuth flow
     */
    async initiateOAuth() {
        // Check if already authenticated with a valid token
        if (this.isAuthenticated && this.currentToken) {
            this.showToast('Already connected to QuickBooks', 'info');
            return;
        }

        try {
            const response = await fetch(`${this.apiBaseUrl}/oauth/authorize`);
            const result = await response.json();

            if (result.success) {
                // Open OAuth URL in popup window
                const popup = window.open(
                    result.data.authorizationUrl,
                    'quickbooks_oauth',
                    'width=800,height=600,scrollbars=yes,resizable=yes'
                );

                // Check if popup was blocked
                if (!popup) {
                    this.showToast('Popup blocked. Please allow popups and try again.', 'error');
                    return;
                }

                // Monitor popup
                const checkClosed = setInterval(() => {
                    if (popup.closed) {
                        clearInterval(checkClosed);
                        // Check auth status after popup closes
                        setTimeout(() => this.checkAuthStatus(), 1000);
                    }
                }, 1000);
            } else {
                this.showToast('Failed to initiate OAuth: ' + result.error, 'error');
            }
        } catch (error) {
            console.error('OAuth initiation error:', error);
            this.showToast('Failed to start authentication process', 'error');
        }
    }

    /**
     * Handle successful OAuth callback
     */
    async handleOAuthSuccess(realmId) {
        this.showToast('Successfully connected to QuickBooks!', 'success');
        await this.checkAuthStatus();
    }

    /**
     * Refresh OAuth token
     */
    async refreshToken() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/oauth/refresh`, {
                method: 'POST'
            });
            const result = await response.json();

            if (result.success) {
                this.showToast('Token refreshed successfully', 'success');
                await this.checkAuthStatus();
            } else {
                this.showToast('Failed to refresh token: ' + result.error, 'error');
            }
        } catch (error) {
            console.error('Token refresh error:', error);
            this.showToast('Failed to refresh token', 'error');
        }
    }

    /**
     * Disconnect from QuickBooks
     */
    async disconnect() {
        if (!confirm('Are you sure you want to disconnect from QuickBooks?')) {
            return;
        }

        try {
            const response = await fetch(`${this.apiBaseUrl}/oauth/disconnect`, {
                method: 'POST'
            });
            const result = await response.json();

            if (result.success) {
                this.showToast('Successfully disconnected from QuickBooks', 'success');
                this.isAuthenticated = false;
                this.currentToken = null;
                this.showUnauthenticatedState();
            } else {
                this.showToast('Failed to disconnect: ' + result.error, 'error');
            }
        } catch (error) {
            console.error('Disconnect error:', error);
            this.showToast('Failed to disconnect', 'error');
        }
    }

    /**
     * Verify required scopes
     */
    async verifyScopes() {
        const scopeSection = document.getElementById('scopeSection');
        const scopeStatus = document.getElementById('scopeStatus');
        const scopeDetails = document.getElementById('scopeDetails');

        scopeSection.classList.remove('d-none');
        
        // Simulate scope verification (in real implementation, this would check actual scopes)
        setTimeout(() => {
            scopeStatus.classList.add('d-none');
            scopeDetails.classList.remove('d-none');
            
            // Show management section
            document.getElementById('managementSection').classList.remove('d-none');
        }, 1500);
    }

    /**
     * Load custom fields from API
     */
    async loadCustomFields() {
        const container = document.getElementById('fieldsTableContainer');
        
        // Show loading state
        container.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-2 text-muted">Loading custom fields...</p>
            </div>
        `;

        try {
            const response = await fetch(`${this.apiBaseUrl}/customfields/summaries`);
            const result = await response.json();

            if (result.success) {
                this.customFields = result.data || [];
                this.renderCustomFieldsTable();
            } else {
                throw new Error(result.error || 'Failed to load custom fields');
            }
        } catch (error) {
            console.error('Error loading custom fields:', error);
            container.innerHTML = `
                <div class="text-center py-4">
                    <i class="bi bi-exclamation-triangle text-warning display-4"></i>
                    <h5 class="mt-3">Failed to Load Custom Fields</h5>
                    <p class="text-muted">${error.message}</p>
                    <button class="btn btn-primary" onclick="app.loadCustomFields()">
                        <i class="bi bi-arrow-clockwise me-1"></i>
                        Try Again
                    </button>
                </div>
            `;
        }
    }

    /**
     * Render custom fields table
     */
    renderCustomFieldsTable() {
        const container = document.getElementById('fieldsTableContainer');
        
        if (this.customFields.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <i class="bi bi-inbox"></i>
                    <h5>No Custom Fields Found</h5>
                    <p class="text-muted">Create your first custom field to get started.</p>
                    <button class="btn btn-primary" onclick="app.openCreateFieldModal()">
                        <i class="bi bi-plus-circle me-1"></i>
                        Create Field
                    </button>
                </div>
            `;
            return;
        }

        const tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Type</th>
                            <th>Status</th>
                            <th>Associations</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${this.customFields.map(field => this.renderFieldRow(field)).join('')}
                    </tbody>
                </table>
            </div>
        `;

        container.innerHTML = tableHtml;
    }

    /**
     * Render individual field row
     */
    renderFieldRow(field) {
        const statusClass = field.active ? 'active' : 'inactive';
        const statusIcon = field.active ? 'check-circle-fill' : 'x-circle-fill';
        const typeIcon = this.getFieldTypeIcon(field.dataType);
        
        // Check if PRINT is in allowedOperations
        const hasPrint = (field.associations || []).some(assoc => 
            assoc.allowedOperations?.includes('PRINT') ||
            (assoc.subAssociations || []).some(sub => sub.allowedOperations?.includes('PRINT'))
        );
        const printBadge = hasPrint ? '<span class="badge bg-info ms-1" title="Print on Page"><i class="bi bi-printer"></i></span>' : '';
        
        const associations = field.associations || [];
        const associationTags = associations.map(assoc => {
            const entityClass = assoc.entity.includes('Transaction') ? 'transaction' : 'contact';
            const entityName = assoc.entity.split('/').pop();
            
            // Create friendly labels for subentities
            const subEntitiesLabels = (assoc.subEntities || []).map(sub => this.getSubAssociationLabel(sub)).join(', ');
            const tooltipText = subEntitiesLabels || 'No sub-entities';
            
            return `<span class="association-tag ${entityClass}" title="${this.escapeHtml(tooltipText)}">${entityName}</span>`;
        }).join('');

        return `
            <tr data-field-id="${field.id}">
                <td>
                    <div class="d-flex align-items-center">
                        <span class="field-type-icon ${field.dataType.toLowerCase()}">${typeIcon}</span>
                        <strong>${this.escapeHtml(field.name)}</strong>${printBadge}
                    </div>
                </td>
                <td>
                    <span class="badge bg-secondary">${field.dataType}</span>
                </td>
                <td>
                    <span class="field-status ${statusClass}">
                        <i class="bi bi-${statusIcon}"></i>
                        ${field.active ? 'Active' : 'Inactive'}
                    </span>
                </td>
                <td>
                    <div class="associations">
                        ${associationTags || '<span class="text-muted">None</span>'}
                    </div>
                </td>
                <td>
                    <div class="action-buttons">
                        <button class="btn btn-sm btn-outline-primary" onclick="app.editField('${field.id}')" title="Edit">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-secondary" onclick="app.toggleFieldStatus('${field.id}', ${!field.active})" title="${field.active ? 'Deactivate' : 'Activate'}">
                            <i class="bi bi-${field.active ? 'pause' : 'play'}"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="app.deleteField('${field.id}')" title="Deactivate">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }

    /**
     * Get field type icon
     */
    getFieldTypeIcon(dataType) {
        const icons = {
            'STRING': 'Aa',
            'NUMBER': '123',
            'DATE': 'Cal',
            'BOOLEAN': 'T/F'
        };
        return icons[dataType] || '?';
    }

    /**
     * Get friendly label for subassociation value
     */
    getSubAssociationLabel(value) {
        const labels = {
            // Sales
            'SALE_CREDIT': 'Credit memo',
            'SALE_REFUND': 'Refund receipt',
            'SALE': 'Sales Receipt',
            'SALE_INVOICE': 'Invoice',
            'SALE_ESTIMATE': 'Estimate',
            'SALE_ORDER': 'Sales order',
            // Purchases
            'PURCHASE_ORDER': 'Purchase order',
            'PURCHASE': 'Expense',
            'PURCHASE_BILL': 'Bill',
            'PURCHASE::CHECK': 'Check',
            'PURCHASE_CREDIT': 'Vendor Credit',
            'PURCHASE::CREDIT_CARD_CREDIT': 'Credit card credit',
            // Contacts
            'CUSTOMER': 'Customers',
            'VENDOR': 'Vendors'
        };
        return labels[value] || value;
    }

    /**
     * Filter fields based on search term
     */
    filterFields(searchTerm) {
        const rows = document.querySelectorAll('#fieldsTableContainer tbody tr');
        const term = searchTerm.toLowerCase();

        rows.forEach(row => {
            const fieldName = row.querySelector('td strong').textContent.toLowerCase();
            const fieldType = row.querySelector('.badge').textContent.toLowerCase();
            
            if (fieldName.includes(term) || fieldType.includes(term)) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    /**
     * Open create field modal
     */
    openCreateFieldModal() {
        this.editingFieldId = null;
        document.getElementById('modalTitle').textContent = 'Create Custom Field';
        document.getElementById('saveFieldBtn').innerHTML = '<i class="bi bi-check-circle me-1"></i>Create Field';
        
        // Reset form to default values
        document.getElementById('fieldForm').reset();
        document.getElementById('fieldActive').checked = true; // Ensure Active is checked by default
        document.getElementById('fieldRequired').checked = false;
        document.getElementById('fieldPrintOnPage').checked = false;
        
        const modal = new bootstrap.Modal(document.getElementById('fieldModal'));
        modal.show();
    }

    /**
     * Edit existing field
     */
    async editField(fieldId) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/customfields/definitions/${fieldId}`);
            const result = await response.json();

            if (result.success) {
                this.editingFieldId = fieldId;
                this.populateFieldForm(result.data);
                
                document.getElementById('modalTitle').textContent = 'Edit Custom Field';
                document.getElementById('saveFieldBtn').innerHTML = '<i class="bi bi-check-circle me-1"></i>Update Field';
                
                const modal = new bootstrap.Modal(document.getElementById('fieldModal'));
                modal.show();
            } else {
                this.showToast('Failed to load field details: ' + result.error, 'error');
            }
        } catch (error) {
            console.error('Error loading field for edit:', error);
            this.showToast('Failed to load field details', 'error');
        }
    }

    /**
     * Populate form with field data
     */
    populateFieldForm(field) {
        document.getElementById('fieldName').value = field.label || '';
        document.getElementById('fieldType').value = field.dataType || '';
        document.getElementById('fieldActive').checked = field.active !== false;

        // Handle associations
        if (field.associations && field.associations.length > 0) {
            const association = field.associations[0];
            document.getElementById('entityType').value = association.associatedEntity || '';
            this.handleEntityTypeChange(association.associatedEntity);

            // Set sub-associations and check for PRINT in allowedOperations
            let hasPrintOperation = false;
            if (association.subAssociations) {
                association.subAssociations.forEach(subAssoc => {
                    const checkbox = document.querySelector(`input[value="${subAssoc.associatedEntity}"]`);
                    if (checkbox) {
                        checkbox.checked = true;
                    }
                    // Check if PRINT is in allowedOperations
                    if (subAssoc.allowedOperations?.includes('PRINT')) {
                        hasPrintOperation = true;
                    }
                });
            }
            // Also check parent association allowedOperations
            if (association.allowedOperations?.includes('PRINT')) {
                hasPrintOperation = true;
            }

            document.getElementById('fieldRequired').checked = association.validationOptions?.required || false;
            document.getElementById('fieldPrintOnPage').checked = hasPrintOperation;
        }
    }

    /**
     * Handle entity type change
     */
    handleEntityTypeChange(entityType) {
        const subContainer = document.getElementById('subAssociationsContainer');
        const transactionTypes = document.getElementById('transactionSubTypes');
        const contactTypes = document.getElementById('contactSubTypes');
        const helpText = document.getElementById('subAssociationsHelpText');

        // Reset all checkboxes and remove disabled state
        document.querySelectorAll('#subAssociationsContainer input[type="checkbox"]').forEach(cb => {
            cb.checked = false;
            cb.disabled = false;
        });

        if (entityType === '/transactions/Transaction') {
            subContainer.classList.remove('d-none');
            transactionTypes.classList.remove('d-none');
            contactTypes.classList.add('d-none');
            if (helpText) {
                helpText.innerHTML = '<i class="bi bi-check-circle me-1"></i>Multiple sub-types within the same entity are supported';
            }
        } else if (entityType === '/network/Contact') {
            subContainer.classList.remove('d-none');
            transactionTypes.classList.add('d-none');
            contactTypes.classList.remove('d-none');
            if (helpText) {
                helpText.innerHTML = '<i class="bi bi-info-circle me-1"></i>Only one contact type can be selected (entities are mutually exclusive)';
            }
            this.setupContactTypeMutualExclusivity();
        } else {
            subContainer.classList.add('d-none');
        }
    }

    /**
     * Setup mutual exclusivity for contact type checkboxes
     */
    setupContactTypeMutualExclusivity() {
        const contactCheckboxes = [
            document.getElementById('customer'),
            document.getElementById('vendor')
        ];
        const contactTransactionTypes = document.getElementById('contactTransactionTypes');

        contactCheckboxes.forEach(checkbox => {
            // Remove existing listeners by cloning and replacing
            const newCheckbox = checkbox.cloneNode(true);
            checkbox.parentNode.replaceChild(newCheckbox, checkbox);
            
            newCheckbox.addEventListener('change', function() {
                if (this.checked) {
                    // Uncheck and disable other contact type checkboxes
                    contactCheckboxes.forEach(cb => {
                        const currentCb = document.getElementById(cb.id);
                        if (currentCb && currentCb.id !== this.id) {
                            currentCb.checked = false;
                            currentCb.disabled = true;
                        }
                    });
                    // Show contact transaction types
                    if (contactTransactionTypes) {
                        contactTransactionTypes.classList.remove('d-none');
                    }
                } else {
                    // Re-enable all checkboxes when unchecked
                    contactCheckboxes.forEach(cb => {
                        const currentCb = document.getElementById(cb.id);
                        if (currentCb) {
                            currentCb.disabled = false;
                        }
                    });
                    // Hide contact transaction types and uncheck them
                    if (contactTransactionTypes) {
                        contactTransactionTypes.classList.add('d-none');
                        document.querySelectorAll('#contactTransactionTypes input[type="checkbox"]').forEach(cb => {
                            cb.checked = false;
                        });
                    }
                }
            });
        });
    }

    /**
     * Save field (create or update)
     */
    async saveField() {
        const form = document.getElementById('fieldForm');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const fieldData = this.collectFieldData();
        const saveBtn = document.getElementById('saveFieldBtn');
        const originalText = saveBtn.innerHTML;

        // Show loading state
        saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';
        saveBtn.disabled = true;

        try {
            let response;
            if (this.editingFieldId) {
                // Update existing field
                response = await fetch(`${this.apiBaseUrl}/customfields/${this.editingFieldId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(fieldData)
                });
            } else {
                // Create new field
                response = await fetch(`${this.apiBaseUrl}/customfields`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(fieldData)
                });
            }

            const result = await response.json();

            if (result.success) {
                this.showToast(
                    this.editingFieldId ? 'Field updated successfully' : 'Field created successfully',
                    'success'
                );
                
                // Close modal and refresh fields
                bootstrap.Modal.getInstance(document.getElementById('fieldModal')).hide();
                await this.loadCustomFields();
            } else {
                throw new Error(result.error || 'Failed to save field');
            }
        } catch (error) {
            console.error('Error saving field:', error);
            this.showToast('Failed to save field: ' + error.message, 'error');
        } finally {
            // Restore button state
            saveBtn.innerHTML = originalText;
            saveBtn.disabled = false;
        }
    }

    /**
     * Collect form data
     */
    collectFieldData() {
        const name = document.getElementById('fieldName').value.trim();
        const type = document.getElementById('fieldType').value;
        const active = document.getElementById('fieldActive').checked;
        const required = document.getElementById('fieldRequired').checked;
        const printOnPage = document.getElementById('fieldPrintOnPage').checked;
        const entityType = document.getElementById('entityType').value;

        const fieldData = {
            name: name,
            type: type,
            active: active,
            printOnPage: printOnPage
        };

        // Handle associations based on entity type
        if (entityType === '/transactions/Transaction') {
            // For transactions, collect transaction sub-types
            const subAssociations = [];
            document.querySelectorAll('#transactionSubTypes input[type="checkbox"]:checked').forEach(cb => {
                subAssociations.push(cb.value);
            });

            if (subAssociations.length > 0) {
                fieldData.associations = [{
                    associatedEntity: entityType,
                    active: true,
                    required: required,
                    associationCondition: 'INCLUDED',
                    subAssociations: subAssociations
                }];
            }
        } else if (entityType === '/network/Contact') {
            // For contacts, collect contact type and transaction types
            const contactTypes = [];
            document.querySelectorAll('#contactSubTypes input[type="checkbox"]:checked').forEach(cb => {
                contactTypes.push(cb.value);
            });

            const transactionTypes = [];
            document.querySelectorAll('#contactTransactionTypes input[type="checkbox"]:checked').forEach(cb => {
                transactionTypes.push(cb.value);
            });

            // Combine contact type with transaction types
            // Format: CUSTOMER or VENDOR as the sub-association
            if (contactTypes.length > 0) {
                const subAssociations = contactTypes;
                
                // If transaction types are selected, append them to indicate the scope
                // Note: This may need backend support for proper nesting
                if (transactionTypes.length > 0) {
                    // For now, we'll add transaction types as additional sub-associations
                    subAssociations.push(...transactionTypes);
                }

                fieldData.associations = [{
                    associatedEntity: entityType,
                    active: true,
                    required: required,
                    associationCondition: 'INCLUDED',
                    subAssociations: subAssociations
                }];
            }
        }

        return fieldData;
    }

    /**
     * Toggle field status (activate/deactivate)
     */
    async toggleFieldStatus(fieldId, newStatus) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/customfields/${fieldId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ active: newStatus })
            });

            const result = await response.json();

            if (result.success) {
                this.showToast(
                    `Field ${newStatus ? 'activated' : 'deactivated'} successfully`,
                    'success'
                );
                await this.loadCustomFields();
            } else {
                throw new Error(result.error || 'Failed to update field status');
            }
        } catch (error) {
            console.error('Error toggling field status:', error);
            this.showToast('Failed to update field status', 'error');
        }
    }

    /**
     * Delete field (Note: QuickBooks API only supports deactivation, not actual deletion)
     */
    async deleteField(fieldId) {
        const field = this.customFields.find(f => f.id === fieldId);
        const fieldName = field ? field.name : 'this field';

        if (!confirm(`Are you sure you want to deactivate "${fieldName}"?\n\nNote: QuickBooks does not support permanent deletion of custom fields. This will deactivate the field instead.`)) {
            return;
        }

        try {
            const response = await fetch(`${this.apiBaseUrl}/customfields/${fieldId}`, {
                method: 'DELETE'
            });

            const result = await response.json();

            if (result.success) {
                this.showToast('Field deactivated successfully', 'success');
                await this.loadCustomFields();
            } else {
                throw new Error(result.error || 'Failed to deactivate field');
            }
        } catch (error) {
            console.error('Error deactivating field:', error);
            this.showToast('Failed to deactivate field', 'error');
        }
    }

    /**
     * Reset field form
     */
    resetFieldForm() {
        document.getElementById('fieldForm').reset();
        document.getElementById('subAssociationsContainer').classList.add('d-none');
        this.editingFieldId = null;
    }

    /**
     * Show authenticated state
     */
    showAuthenticatedState() {
        document.getElementById('connectionStatus').textContent = 'Connected';
        document.getElementById('connectionStatus').className = 'badge bg-success';
        
        document.getElementById('authContent').classList.add('d-none');
        document.getElementById('authStatus').classList.remove('d-none');
        
        // Update auth status details
        if (this.currentToken) {
            document.getElementById('realmId').textContent = this.currentToken.realmId || 'N/A';
            
            if (this.currentToken.expiresAt) {
                const expiryDate = new Date(this.currentToken.expiresAt);
                document.getElementById('tokenExpiry').textContent = expiryDate.toLocaleString();
            }
        }
    }

    /**
     * Show unauthenticated state
     */
    showUnauthenticatedState() {
        document.getElementById('connectionStatus').textContent = 'Not Connected';
        document.getElementById('connectionStatus').className = 'badge bg-secondary';
        
        document.getElementById('authContent').classList.remove('d-none');
        document.getElementById('authStatus').classList.add('d-none');
        document.getElementById('scopeSection').classList.add('d-none');
        document.getElementById('managementSection').classList.add('d-none');
    }

    /**
     * Show toast notification
     */
    showToast(message, type = 'info') {
        const toast = document.getElementById('toast');
        const toastIcon = document.getElementById('toastIcon');
        const toastTitle = document.getElementById('toastTitle');
        const toastMessage = document.getElementById('toastMessage');

        // Set icon and title based on type
        const config = {
            success: { icon: 'bi-check-circle-fill text-success', title: 'Success' },
            error: { icon: 'bi-exclamation-triangle-fill text-danger', title: 'Error' },
            warning: { icon: 'bi-exclamation-triangle-fill text-warning', title: 'Warning' },
            info: { icon: 'bi-info-circle-fill text-info', title: 'Information' }
        };

        const { icon, title } = config[type] || config.info;
        
        toastIcon.className = `bi ${icon} me-2`;
        toastTitle.textContent = title;
        toastMessage.textContent = message;

        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();
    }

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Load invoices from API
     */
    async loadInvoices(page = 1) {
        const container = document.getElementById('invoicesTableContainer');
        
        container.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-2 text-muted">Loading invoices...</p>
            </div>
        `;

        try {
            console.log('Fetching invoices from:', `${this.apiBaseUrl}/invoice/list?page=${page}&pageSize=${this.invoicePageSize}`);
            const response = await fetch(`${this.apiBaseUrl}/invoice/list?page=${page}&pageSize=${this.invoicePageSize}`);
            console.log('Invoice response status:', response.status);
            
            const result = await response.json();
            console.log('Invoice result:', result);

            if (result.success) {
                this.invoices = result.data.invoices || [];
                this.currentInvoicePage = result.data.page || page;
                this.invoiceHasMore = result.data.hasMore || false;
                console.log('Loaded invoices:', this.invoices.length, 'Page:', this.currentInvoicePage);
                this.renderInvoicesTable();
            } else {
                throw new Error(result.error || 'Failed to load invoices');
            }
        } catch (error) {
            console.error('Error loading invoices:', error);
            container.innerHTML = `
                <div class="text-center py-4">
                    <i class="bi bi-exclamation-triangle text-warning display-4"></i>
                    <h5 class="mt-3">Failed to Load Invoices</h5>
                    <p class="text-muted">${error.message}</p>
                    <button class="btn btn-primary" onclick="app.loadInvoices()">
                        <i class="bi bi-arrow-clockwise me-1"></i>
                        Try Again
                    </button>
                </div>
            `;
        }
    }

    /**
     * Render invoices table
     */
    renderInvoicesTable() {
        const container = document.getElementById('invoicesTableContainer');
        
        if (this.invoices.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <i class="bi bi-inbox"></i>
                    <h5>No Invoices Found</h5>
                    <p class="text-muted">Create your first invoice to get started.</p>
                    <button class="btn btn-primary" onclick="app.openCreateInvoiceModal()">
                        <i class="bi bi-plus-circle me-1"></i>
                        Create Invoice
                    </button>
                </div>
            `;
            return;
        }

        const tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>Invoice #</th>
                            <th>Customer</th>
                            <th>Date</th>
                            <th>Due Date</th>
                            <th>Total</th>
                            <th>Balance</th>
                            <th>Custom Fields</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${this.invoices.map(invoice => this.renderInvoiceRow(invoice)).join('')}
                    </tbody>
                </table>
            </div>
            ${this.renderInvoicePagination()}
        `;

        container.innerHTML = tableHtml;
    }

    /**
     * Render pagination controls for invoices
     */
    renderInvoicePagination() {
        const hasPrevious = this.currentInvoicePage > 1;
        const hasNext = this.invoiceHasMore;
        
        if (!hasPrevious && !hasNext) {
            return '';
        }

        return `
            <div class="d-flex justify-content-between align-items-center mt-3">
                <div class="text-muted">
                    Page ${this.currentInvoicePage}
                </div>
                <div>
                    <button class="btn btn-outline-secondary btn-sm me-2" 
                            onclick="app.loadInvoices(${this.currentInvoicePage - 1})" 
                            ${!hasPrevious ? 'disabled' : ''}>
                        <i class="bi bi-chevron-left"></i> Previous
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" 
                            onclick="app.loadInvoices(${this.currentInvoicePage + 1})" 
                            ${!hasNext ? 'disabled' : ''}>
                        Next <i class="bi bi-chevron-right"></i>
                    </button>
                </div>
            </div>
        `;
    }

    /**
     * Render individual invoice row
     */
    renderInvoiceRow(invoice) {
        // Handle both PascalCase and lowercase property names from API
        const invoiceId = invoice.Id || invoice.id;
        const docNumber = invoice.DocNumber || invoice.docNumber || invoiceId || 'N/A';
        const customerName = invoice.CustomerRef?.Name || invoice.customerRef?.name || 
                           invoice.CustomerRef?.DisplayName || invoice.customerRef?.displayName || 'N/A';
        const txnDate = invoice.TxnDate || invoice.txnDate || 'N/A';
        const dueDate = invoice.DueDate || invoice.dueDate || 'N/A';
        const totalAmt = invoice.TotalAmt ?? invoice.totalAmt ?? 0;
        const balance = invoice.Balance ?? invoice.balance ?? 0;
        
        // Handle custom fields with proper name display
        const customFields = invoice.CustomField || invoice.customField || [];
        const customFieldsDisplay = customFields.length > 0
            ? customFields
                .filter(cf => {
                    // Check for any value type (String, Number, or Date) - also check Type property
                    const hasValue = (cf.StringValue || cf.stringValue || 
                                     cf.NumberValue || cf.numberValue || 
                                     cf.DateValue || cf.dateValue ||
                                     cf.Value || cf.value);
                    return hasValue && hasValue !== '';
                })
                .map(cf => {
                    const name = cf.Name || cf.name || 'Custom Field';
                    // Get value from any data type (including generic Value property)
                    let value = cf.StringValue || cf.stringValue || 
                                cf.NumberValue || cf.numberValue ||
                                cf.DateValue || cf.dateValue ||
                                cf.Value || cf.value || 'N/A';
                    
                    // Format date values as mm/dd/yyyy (QBO style)
                    const dateValue = cf.DateValue || cf.dateValue;
                    if (dateValue) {
                        const date = new Date(dateValue + 'T00:00:00');
                        if (!isNaN(date.getTime())) {
                            const month = String(date.getMonth() + 1).padStart(2, '0');
                            const day = String(date.getDate()).padStart(2, '0');
                            const year = date.getFullYear();
                            value = `${month}/${day}/${year}`;
                        }
                    }
                    
                    return `<span class="badge bg-info" title="${this.escapeHtml(name)}">${this.escapeHtml(name)}: ${this.escapeHtml(String(value))}</span>`;
                }).join(' ')
            : '<span class="text-muted">None</span>';

        // Store invoice as JSON string for the view/edit action
        const invoiceJson = this.escapeHtml(JSON.stringify(invoice));

        return `
            <tr>
                <td><strong>#${this.escapeHtml(docNumber)}</strong></td>
                <td>${this.escapeHtml(customerName)}</td>
                <td>${this.escapeHtml(txnDate)}</td>
                <td>${this.escapeHtml(dueDate)}</td>
                <td>$${totalAmt.toFixed(2)}</td>
                <td>$${balance.toFixed(2)}</td>
                <td>${customFieldsDisplay}</td>
                <td>
                    <button class="btn btn-sm btn-outline-primary" onclick='app.viewInvoice(${invoiceJson})' title="View/Edit Invoice">
                        <i class="bi bi-pencil"></i> Edit
                    </button>
                </td>
            </tr>
        `;
    }

    /**
     * View/Edit invoice
     */
    async viewInvoice(invoice) {
        try {
            this.editingInvoice = invoice;
            
            // Load customers, items, and custom fields
            await Promise.all([
                this.loadCustomersForInvoice(),
                this.loadItems(),
                this.loadCustomFieldsForInvoice()
            ]);

            // Update modal title
            const docNumber = invoice.DocNumber || invoice.docNumber || invoice.Id || invoice.id || 'N/A';
            document.getElementById('invoiceModalTitle').textContent = `Edit Invoice #${docNumber}`;

            // Update button text
            document.getElementById('saveInvoiceBtn').innerHTML = '<i class="bi bi-check-circle me-1"></i>Update Invoice';
            
            // Populate customer - dropdown is now loaded, so we can set the value
            const customerId = invoice.CustomerRef?.value || invoice.CustomerRef?.Value || 
                              invoice.customerRef?.value || invoice.customerRef?.Value;
            if (customerId) {
                const customerSelect = document.getElementById('invoiceCustomer');
                customerSelect.value = customerId;
                console.log('Selected customer:', customerId, 'Found in dropdown:', customerSelect.value === customerId);
            }
            
            // Populate dates
            document.getElementById('invoiceDate').value = invoice.TxnDate || invoice.txnDate || '';
            document.getElementById('invoiceDueDate').value = invoice.DueDate || invoice.dueDate || '';
            
            // Populate line items
            const lineItemsContainer = document.getElementById('lineItemsContainer');
            lineItemsContainer.innerHTML = '';
            this.lineItemCounter = 0;
            
            const lines = invoice.Line || invoice.line || [];
            const salesLines = lines.filter(line => 
                (line.DetailType || line.detailType) === 'SalesItemLineDetail' ||
                (line.DetailType || line.detailType) === 'SalesItemLine'
            );
            
            if (salesLines.length > 0) {
                salesLines.forEach(line => {
                    this.addLineItem();
                    const lineElement = lineItemsContainer.querySelector(`[data-line-id="${this.lineItemCounter}"]`);
                    if (lineElement) {
                        // Set item dropdown
                        const itemRef = line.SalesItemLineDetail?.ItemRef?.value || line.salesItemLineDetail?.itemRef?.value || '';
                        const itemSelect = lineElement.querySelector('[data-field="item"]');
                        if (itemSelect && itemRef) {
                            itemSelect.value = itemRef;
                        }
                        
                        lineElement.querySelector('[data-field="description"]').value = line.Description || line.description || '';
                        lineElement.querySelector('[data-field="quantity"]').value = line.SalesItemLineDetail?.Qty || line.salesItemLineDetail?.qty || 1;
                        lineElement.querySelector('[data-field="unitPrice"]').value = line.SalesItemLineDetail?.UnitPrice || line.salesItemLineDetail?.unitPrice || 0;
                        lineElement.querySelector('[data-field="amount"]').value = line.Amount || line.amount || 0;
                        
                        // Store ItemRef and Line.Id for this line item
                        const lineId = line.Id || line.id || '';
                        lineElement.setAttribute('data-item-ref', itemRef);
                        lineElement.setAttribute('data-line-item-id', lineId);
                    }
                });
            } else {
                this.addLineItem();
            }
            
            // Update invoice total after loading line items
            this.updateInvoiceTotal();
            
            // Render all custom fields with existing values from the invoice
            const customFields = invoice.CustomField || invoice.customField || [];
            this.renderInvoiceCustomFields(customFields);
            
            // Populate notes
            const memo = invoice.CustomerMemo?.value || invoice.customerMemo?.value || '';
            document.getElementById('invoiceNotes').value = memo;

            const modal = new bootstrap.Modal(document.getElementById('invoiceModal'));
            modal.show();
        } catch (error) {
            console.error('Error viewing invoice:', error);
            this.showToast('Failed to load invoice: ' + error.message, 'error');
        }
    }

    /**
     * Open create invoice modal
     */
    async openCreateInvoiceModal() {
        try {
            this.editingInvoice = null;
            
            // Load customers, items, and custom fields
            await Promise.all([
                this.loadCustomersForInvoice(),
                this.loadItems(),
                this.loadCustomFieldsForInvoice()
            ]);

            // Update modal title
            document.getElementById('invoiceModalTitle').textContent = 'Create Invoice';
            
            // Update button text
            document.getElementById('saveInvoiceBtn').innerHTML = '<i class="bi bi-plus-circle me-1"></i>Create Invoice';
            
            // Set default date to today
            const today = new Date().toISOString().split('T')[0];
            document.getElementById('invoiceDate').value = today;
            
            // Reset form
            this.resetInvoiceForm();
            
            // Render custom fields (empty for new invoice)
            this.renderInvoiceCustomFields([]);
            
            // Add one default line item
            this.lineItemCounter = 0;
            this.addLineItem();

            const modal = new bootstrap.Modal(document.getElementById('invoiceModal'));
            modal.show();
        } catch (error) {
            console.error('Error opening invoice modal:', error);
            this.showToast('Failed to open invoice form: ' + error.message, 'error');
        }
    }

    /**
     * Load customers for invoice dropdown
     */
    async loadCustomersForInvoice() {
        try {
            console.log('Loading customers for invoice...');
            const response = await fetch(`${this.apiBaseUrl}/invoice/customers`);
            console.log('Customers response status:', response.status);
            const result = await response.json();
            console.log('Customers result:', result);

            if (result.success) {
                this.customers = result.data || [];
                console.log('Number of customers loaded:', this.customers.length);
                
                const customerSelect = document.getElementById('invoiceCustomer');
                customerSelect.innerHTML = '<option value="">Select customer...</option>';
                
                this.customers.forEach(customer => {
                    console.log('Adding customer:', customer);
                    const option = document.createElement('option');
                    // Handle both PascalCase and camelCase
                    option.value = customer.Id || customer.id;
                    option.textContent = customer.DisplayName || customer.displayName || 
                                       customer.CompanyName || customer.companyName || 
                                       `${customer.GivenName || customer.givenName || ''} ${customer.FamilyName || customer.familyName || ''}`.trim();
                    customerSelect.appendChild(option);
                });
                console.log('Customer dropdown populated with', this.customers.length, 'customers');
            } else {
                console.error('Failed to load customers:', result.error);
                this.showToast('Failed to load customers: ' + (result.error || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error loading customers:', error);
            this.showToast('Failed to load customers', 'error');
        }
    }

    /**
     * Load items (products/services) from QuickBooks
     */
    async loadItems() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/item/list`);
            const result = await response.json();

            if (result.success) {
                this.items = result.data || [];
                console.log('Items loaded:', this.items.length);
            } else {
                console.error('Failed to load items:', result.error);
                this.showToast('Failed to load items: ' + (result.error || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error loading items:', error);
            this.showToast('Failed to load items', 'error');
        }
    }

    /**
     * Load custom fields for invoice (only active SALE_INVOICE fields)
     * Stores them for later use when rendering the invoice form
     */
    async loadCustomFieldsForInvoice() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/customfields/definitions`);
            const result = await response.json();

            if (result.success) {
                // Filter to only invoice-related custom fields
                this.invoiceCustomFields = (result.data || []).filter(field => {
                    if (!field.active) return false;
                    const hasInvoiceAssoc = field.associations?.some(assoc => 
                        assoc.subAssociations?.some(sub => 
                            sub.associatedEntity === 'SALE_INVOICE'
                        )
                    );
                    return hasInvoiceAssoc;
                });
                console.log('Invoice custom fields loaded:', this.invoiceCustomFields.length);
            }
        } catch (error) {
            console.error('Error loading custom fields for invoice:', error);
            this.invoiceCustomFields = [];
        }
    }

    /**
     * Render all custom fields for the invoice form
     * @param {Array} existingValues - Custom field values from an existing invoice (for edit mode)
     */
    renderInvoiceCustomFields(existingValues = []) {
        const container = document.getElementById('invoiceCustomFieldsContainer');
        const countBadge = document.getElementById('customFieldCount');
        const noFieldsMsg = document.getElementById('noCustomFieldsMsg');
        
        if (!this.invoiceCustomFields || this.invoiceCustomFields.length === 0) {
            container.innerHTML = '<div class="text-muted text-center py-2"><i class="bi bi-info-circle me-1"></i>No custom fields available for invoices</div>';
            countBadge.textContent = '0';
            return;
        }

        // Update count badge
        countBadge.textContent = this.invoiceCustomFields.length;
        
        // Build HTML for each custom field
        let html = '';
        this.invoiceCustomFields.forEach(field => {
            const fieldId = field.legacyIDV2 || field.id;
            const dataType = field.dataType || 'STRING';
            const fieldName = field.label || 'Unknown';
            
            // Find existing value for this field (match by name since DefinitionId may differ)
            const existingField = existingValues.find(ev => {
                const evName = ev.Name || ev.name || '';
                const evDefId = ev.DefinitionId || ev.definitionId || '';
                return evName === fieldName || evDefId === fieldId;
            });
            
            // Get the value based on data type
            let currentValue = '';
            if (existingField) {
                currentValue = existingField.StringValue || existingField.stringValue ||
                              existingField.NumberValue || existingField.numberValue ||
                              existingField.DateValue || existingField.dateValue ||
                              existingField.Value || existingField.value || '';
            }
            
            // Determine input type based on data type (use date for DATE to show calendar picker)
            const inputType = dataType === 'NUMBER' ? 'number' : (dataType === 'DATE' ? 'date' : 'text');
            const stepAttr = dataType === 'NUMBER' ? 'step="any"' : '';
            const placeholder = dataType === 'NUMBER' ? 'Enter number' : (dataType === 'DATE' ? '' : 'Enter text');
            
            // Type icon and color
            const typeInfo = {
                'STRING': { icon: 'Aa', color: 'primary' },
                'NUMBER': { icon: '123', color: 'success' },
                'DATE': { icon: '📅', color: 'warning' }
            };
            const info = typeInfo[dataType] || typeInfo['STRING'];
            
            html += `
                <div class="custom-field-input mb-2" data-field-id="${fieldId}" data-field-name="${this.escapeHtml(fieldName)}" data-data-type="${dataType}">
                    <div class="input-group input-group-sm">
                        <span class="input-group-text" title="${dataType}">
                            <span class="badge bg-${info.color}">${info.icon}</span>
                        </span>
                        <span class="input-group-text flex-grow-0" style="min-width: 120px; max-width: 200px; font-size: 0.85em;">
                            ${this.escapeHtml(fieldName)}
                        </span>
                        <input type="${inputType}" 
                               class="form-control custom-field-value" 
                               placeholder="${placeholder}"
                               value="${this.escapeHtml(String(currentValue))}"
                               ${stepAttr}>
                        ${currentValue ? '<span class="input-group-text text-success" title="Has value"><i class="bi bi-check-circle-fill"></i></span>' : '<span class="input-group-text text-muted" title="No value"><i class="bi bi-dash-circle"></i></span>'}
                    </div>
                </div>
            `;
        });
        
        container.innerHTML = html;
    }

    /**
     * Add line item to invoice form
     */
    addLineItem() {
        this.lineItemCounter++;
        const container = document.getElementById('lineItemsContainer');
        
        // Build item options
        let itemOptions = '<option value="">Select item/service *</option>';
        this.items.forEach(item => {
            const itemName = item.name || item.Name || 'Unnamed';
            const itemId = item.id || item.Id;
            itemOptions += `<option value="${itemId}">${itemName}</option>`;
        });
        
        const lineItemHtml = `
            <div class="card mb-2" data-line-id="${this.lineItemCounter}">
                <div class="card-body">
                    <div class="row">
                        <div class="col-md-3">
                            <select class="form-select form-select-sm" data-field="item" required>
                                ${itemOptions}
                            </select>
                        </div>
                        <div class="col-md-3">
                            <input type="text" class="form-control form-control-sm" placeholder="Description" data-field="description">
                        </div>
                        <div class="col-md-1">
                            <input type="number" class="form-control form-control-sm" placeholder="Qty *" data-field="quantity" min="1" value="1" required>
                        </div>
                        <div class="col-md-2">
                            <input type="number" class="form-control form-control-sm" placeholder="Price *" data-field="unitPrice" step="0.01" min="0" required>
                        </div>
                        <div class="col-md-2">
                            <input type="text" class="form-control form-control-sm" placeholder="Amount" data-field="amount" readonly>
                        </div>
                        <div class="col-md-1">
                            <button type="button" class="btn btn-sm btn-outline-danger w-100" onclick="app.removeLineItem(${this.lineItemCounter})">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        container.insertAdjacentHTML('beforeend', lineItemHtml);
        
        // Add event listeners for amount calculation and item selection
        const lineItem = container.querySelector(`[data-line-id="${this.lineItemCounter}"]`);
        const itemSelect = lineItem.querySelector('[data-field="item"]');
        const qtyInput = lineItem.querySelector('[data-field="quantity"]');
        const priceInput = lineItem.querySelector('[data-field="unitPrice"]');
        const amountInput = lineItem.querySelector('[data-field="amount"]');
        
        const updateAmount = () => {
            const qty = parseFloat(qtyInput.value) || 0;
            const price = parseFloat(priceInput.value) || 0;
            amountInput.value = (qty * price).toFixed(2);
            this.updateInvoiceTotal();
        };
        
        // Auto-populate unit price when item is selected
        itemSelect.addEventListener('change', () => {
            const selectedItemId = itemSelect.value;
            if (selectedItemId) {
                const selectedItem = this.items.find(item => 
                    (item.id || item.Id) === selectedItemId
                );
                if (selectedItem) {
                    const unitPrice = selectedItem.unitPrice || selectedItem.UnitPrice || 0;
                    priceInput.value = unitPrice.toFixed(2);
                    updateAmount();
                }
            }
        });
        
        qtyInput.addEventListener('input', updateAmount);
        priceInput.addEventListener('input', updateAmount);
    }

    /**
     * Update invoice total
     */
    updateInvoiceTotal() {
        const lineItemElements = document.querySelectorAll('#lineItemsContainer .card');
        let total = 0;
        
        lineItemElements.forEach(element => {
            const amountInput = element.querySelector('[data-field="amount"]');
            if (amountInput) {
                const amount = parseFloat(amountInput.value) || 0;
                total += amount;
            }
        });
        
        const totalElement = document.getElementById('invoiceTotal');
        if (totalElement) {
            totalElement.textContent = `$${total.toFixed(2)}`;
        }
    }

    /**
     * Remove line item from invoice form
     */
    removeLineItem(lineId) {
        const lineItem = document.querySelector(`[data-line-id="${lineId}"]`);
        if (lineItem) {
            lineItem.remove();
            this.updateInvoiceTotal();
        }
    }

    /**
     * Save invoice
     */
    async saveInvoice() {
        const form = document.getElementById('invoiceForm');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const invoiceData = this.collectInvoiceData();
        if (!invoiceData) {
            return;
        }

        const saveBtn = document.getElementById('saveInvoiceBtn');
        const originalText = saveBtn.innerHTML;
        const isEditing = this.editingInvoice !== null;

        saveBtn.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span>${isEditing ? 'Updating...' : 'Creating...'}`;
        saveBtn.disabled = true;

        try {
            const url = isEditing ? `${this.apiBaseUrl}/invoice/update` : `${this.apiBaseUrl}/invoice/create`;
            const method = isEditing ? 'PUT' : 'POST';
            
            // Add invoice ID and sync token for updates
            if (isEditing) {
                invoiceData.invoiceId = this.editingInvoice.Id || this.editingInvoice.id;
                invoiceData.syncToken = this.editingInvoice.SyncToken || this.editingInvoice.syncToken;
            }
            
            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(invoiceData)
            });

            const result = await response.json();

            if (result.success) {
                this.showToast(isEditing ? 'Invoice updated successfully' : 'Invoice created successfully', 'success');
                bootstrap.Modal.getInstance(document.getElementById('invoiceModal')).hide();
                await this.loadInvoices();
            } else {
                throw new Error(result.error || 'Failed to create invoice');
            }
        } catch (error) {
            console.error('Error creating invoice:', error);
            this.showToast('Failed to create invoice: ' + error.message, 'error');
        } finally {
            saveBtn.innerHTML = originalText;
            saveBtn.disabled = false;
        }
    }

    /**
     * Collect invoice data from form
     */
    collectInvoiceData() {
        const customerId = document.getElementById('invoiceCustomer').value;
        const invoiceDate = document.getElementById('invoiceDate').value;
        const dueDate = document.getElementById('invoiceDueDate').value;
        const notes = document.getElementById('invoiceNotes').value;

        if (!customerId || !invoiceDate) {
            this.showToast('Please fill in all required fields', 'error');
            return null;
        }

        // Collect all custom field values
        const customFields = [];
        const customFieldInputs = document.querySelectorAll('#invoiceCustomFieldsContainer .custom-field-input');
        customFieldInputs.forEach(fieldElement => {
            const fieldId = fieldElement.dataset.fieldId;
            const fieldName = fieldElement.dataset.fieldName;
            const dataType = fieldElement.dataset.dataType;
            const valueInput = fieldElement.querySelector('.custom-field-value');
            const value = valueInput ? valueInput.value.trim() : '';
            
            // Only include fields that have a value
            if (value) {
                customFields.push({
                    definitionId: fieldId,
                    name: fieldName,
                    value: value,
                    dataType: dataType
                });
            }
        });

        // Collect line items
        const lineItems = [];
        const lineItemElements = document.querySelectorAll('#lineItemsContainer .card');
        
        for (const element of lineItemElements) {
            const itemSelect = element.querySelector('[data-field="item"]');
            const selectedItemId = itemSelect ? itemSelect.value : element.getAttribute('data-item-ref');
            const description = element.querySelector('[data-field="description"]').value.trim();
            const quantityValue = element.querySelector('[data-field="quantity"]').value;
            const unitPriceValue = element.querySelector('[data-field="unitPrice"]').value;
            const quantity = parseFloat(quantityValue);
            const unitPrice = parseFloat(unitPriceValue);
            
            if (!selectedItemId || quantityValue === '' || unitPriceValue === '' || isNaN(quantity) || isNaN(unitPrice)) {
                this.showToast('Please fill in all line item fields (Item, Quantity, and Unit Price)', 'error');
                return null;
            }
            
            // Get Line.Id from line item data attribute (for updates)
            const lineItemId = element.getAttribute('data-line-item-id') || '';
            
            const lineItem = {
                itemId: selectedItemId,
                description: description,
                quantity: quantity,
                unitPrice: unitPrice,
                amount: quantity * unitPrice
            };
            
            // Include Line.Id if this is an existing line item (for updates)
            if (lineItemId) {
                lineItem.lineId = lineItemId;
            }
            
            lineItems.push(lineItem);
        }

        if (lineItems.length === 0) {
            this.showToast('Please add at least one line item', 'error');
            return null;
        }

        return {
            customerId: customerId,
            invoiceDate: invoiceDate,
            dueDate: dueDate || null,
            lineItems: lineItems,
            customFields: customFields, // Now an array of all custom fields with values
            notes: notes
        };
    }

    /**
     * Reset invoice form
     */
    resetInvoiceForm() {
        document.getElementById('invoiceForm').reset();
        document.getElementById('lineItemsContainer').innerHTML = '';
        document.getElementById('invoiceCustomFieldsContainer').innerHTML = '<div class="text-muted text-center py-2"><i class="bi bi-info-circle me-1"></i>Loading custom fields...</div>';
        this.lineItemCounter = 0;
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new QuickBooksCustomFieldsApp();
});
