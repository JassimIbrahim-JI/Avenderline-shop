// modern-admin.js (jQuery 3.6.0 compatible)
(function ($) {
    "use strict";

    // Global configuration
    const config = {
        refreshInterval: 30000,
        maxExportRecords: 500
    };

    // Core initialization
    $(function () {

        $('[data-bs-toggle="tooltip"]').tooltip();

        $('#newsletterCheck').on('change', function () {
            const checkbox = $(this);
            const isChecked = checkbox.is(':checked');
            const url = checkbox.data('url');

            $.post({
                url: url,
                data: { isSubscribed: isChecked },
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                }
            })
                .done(function (response) {
                    if (response.success) {
                        Toast.show('success', response.message);
                        // Optional: Update UI state
                    } else {
                        checkbox.prop('checked', !isChecked); // Revert checkbox
                        Toast.show('danger', response.message);
                    }
                })
                .fail(function () {
                    checkbox.prop('checked', !isChecked); // Revert checkbox
                    Toast.show('danger', 'Request failed. Please try again.');
                });
        })

        initGlobalHandlers();
        initComponents();
        loadDynamicContent();
        setupRefreshInterval();
    });

    function initGlobalHandlers() {
        // AJAX loading overlay
        $(document)
            .ajaxStart(() => $('.loading-overlay').show())
            .ajaxStop(() => $('.loading-overlay').hide());

        // CSV export confirmation
        $('#exportCsvBtn').on('click', handleExportClick);
    }

    function updateNotificationBadge() {
        $.get('/Order/GetNotificationCount')
            .done(function (data) {
                var $badges = $('.badge-notification');
                if (data.count > 0) {
                    $badges.text(data.count).show();
                } else {
                    $badges.hide();
                }
            })
            .fail(function (jqXHR, textStatus, errorThrown) {
                console.error("Error fetching notification count:", textStatus, errorThrown);
            });
    }
    function initComponents() {
        initSidebar();
        initNotificationSystem();
        initMessageSystem();
        initDeleteModal();
        updateNotificationBadge();
        HandleSubscirbeLogic();
    }

    function loadDynamicContent() {
        loadNotifications();
        loadMessages();
    }

    function setupRefreshInterval() {
        setInterval(() => {
            loadNotifications();
            loadMessages();
        }, config.refreshInterval);
    }

    // Sidebar management
    function initSidebar() {
        const $sidebar = $('#accordionSidebar');
        const $body = $('body');

        // Toggle sidebar
        $('#sidebarToggle, #sidebarToggleTop').on('click', function (e) {
            e.preventDefault();
            $body.toggleClass('sidebar-toggled');
            $sidebar.toggleClass('toggled');

            // Store state in localStorage
            localStorage.setItem('sidebarToggled', $body.hasClass('sidebar-toggled'));
        });

        // Initialize from localStorage
        if (localStorage.getItem('sidebarToggled') === 'true') {
            $body.addClass('sidebar-toggled');
            $sidebar.addClass('toggled');
        }

        // Collapse handling
        $('.collapse').on('show.bs.collapse', function () {
            $(this).parent().addClass('active');
        }).on('hide.bs.collapse', function () {
            $(this).parent().removeClass('active');
        });

        // Responsive handling
        $(window).on('resize', function () {
            if ($(window).width() < 768) {
                $sidebar.addClass('toggled');
                $body.addClass('sidebar-toggled');
            }
        }).trigger('resize');
    }

    // Notification system
    function initNotificationSystem() {
        $(document).on('click', '.notification-item', function () {
            const notificationId = $(this).data('id');
            markNotificationRead(notificationId);
        });

        // Orders link handler
        $('#ordersTab').on('shown.bs.tab', function () {
            // Clear notifications via AJAX
            $.post('/Order/ClearNotification')
                .done(function () {
                    // Update badge immediately
                    $('.badge-notification').hide();
                })
                .fail(function (jqXHR, textStatus, errorThrown) {
                    console.error("Clear failed:", textStatus, errorThrown);
                });
        });

        $('#ordersLink').on('click', function (e) {
            e.preventDefault();
            var $link = $(this);

            $.post('/Order/ClearNotification')
                .done(function () {
                    $('.badge-notification').hide();
                    window.location.href = $link.attr('href');
                });
        });
    }

    function markNotificationRead(id) {
        $.post('/Notification/MarkNotificationAsRead', { id })
            .then(loadNotifications)
            .catch(handleAjaxError);
    }

    // Message system
    function initMessageSystem() {
        $(document).on('click', '.message-item', function () {
            const messageId = $(this).data('id');
            markMessageRead(messageId);
        });
    }

    function markMessageRead(id) {
        $.post('/Notification/MarkMessageAsRead', { id })
            .then(loadMessages)
            .catch(handleAjaxError);
    }

    // Data loading functions
    function loadNotifications() {
        $.ajax({
            url: '/Notification/GetOrderNotifications',
            method: 'GET'
        }).done(updateNotificationUI).fail(handleAjaxError);
    }

    function loadMessages() {
        $.ajax({
            url: '/Notification/GetLoginMessages',
            method: 'GET'
        }).done(updateMessageUI).fail(handleAjaxError);
    }

    // UI updaters
    function updateNotificationUI(notifications) {
        const $container = $('#notificationsList').empty();
        let unreadCount = 0;

        notifications.forEach(notification => {
            const isUnread = !notification.isRead;
            unreadCount += isUnread ? 1 : 0;

            $container.append(createNotificationElement(notification, isUnread));
        });

        $('#notificationCount').text(unreadCount);
    }

    function updateMessageUI(messages) {
        const $container = $('#messagesList').empty();
        let unreadCount = 0;

        messages.forEach(message => {
            const isUnread = !message.isRead;
            unreadCount += isUnread ? 1 : 0;

            $container.append(createMessageElement(message, isUnread));
        });

        $('#messageCount').text(unreadCount);
    }

    // Element templates
    function createNotificationElement(notification, isUnread) {
        return `
            <a class="dropdown-item d-flex align-items-center notification-item" 
               href="/Order/Details/${notification.relatedId}" 
               data-id="${notification.id}">
                <div class="mr-3">
                    <div class="icon-circle bg-primary">
                        <i class="fas fa-shopping-cart text-white"></i>
                    </div>
                </div>
                <div>
                    <div class="small text-gray-500">${notification.date}</div>
                    <span class="${isUnread ? 'font-weight-bold' : ''}">${notification.message}</span>
                </div>
            </a>`;
    }

    function createMessageElement(message, isUnread) {
        return `
            <a class="dropdown-item d-flex align-items-center message-item" 
               href="/AdminAccount/Details/${message.userId}" 
               data-id="${message.id}">
                <div class="dropdown-list-image mr-3">
                    <div class="status-indicator bg-success"></div>
                </div>
                <div>
                    <div class="text-truncate ${isUnread ? 'font-weight-bold' : ''}">${message.message}</div>
                    <div class="small text-gray-500">${message.date}</div>
                </div>
            </a>`;
    }

    // Delete modal handler
    function initDeleteModal() {
        $('#confirmDeleteModal').on('show.bs.modal', function (event) {
            const $button = $(event.relatedTarget);
            const modal = $(this);

            modal.find('#userName').text($button.data('userName'));
            modal.find('#userId').val($button.data('userId'));
            modal.find('#deleteForm').attr('action', `/AdminAccount/Delete/${$button.data('userId')}`);
        });
    }

    // Export handler
    function handleExportClick(e) {
        const params = new URLSearchParams(window.location.search);
        const recordCount = parseInt(params.get('count') || 0 );
        if (recordCount > config.maxExportRecords &&
            !confirm(`This export will include ${recordCount} records. Proceed?`)) {
            e.preventDefault();
        }
    }


    function HandleSubscirbeLogic()
    {
        $('#profileForm').on('submit', function (e) {
            e.preventDefault();
            const form = $(this);

            // Clear previous validation states
            $('.is-invalid, .is-valid').removeClass('is-invalid is-valid');

            // Validate required fields
            let isValid = true;
            const requiredFields = ['FullName', 'StreetAddress', 'PhoneNumber'];

            requiredFields.forEach(field => {
                const input = $(`#${field}`);
                const value = input.val() || '';
                if (!value) {
                    input.addClass('is-invalid');
                    isValid = false;
                } else {
                    input.addClass('is-valid');
                }
            });

            const areaSelect = $('#Area');
            if (!areaSelect.val()) {
                areaSelect.addClass('is-invalid');
                isValid = false;
            } else {
                areaSelect.addClass('is-valid');
            }

            if (!isValid) {
                $('#completionAlert').removeClass('d-none');
                Toast.show('danger', 'Please fill in all required fields.');
                return;
            }

            // Submit via AJAX
            $.ajax({
                type: form.attr('method'),
                url: form.attr('action'),
                data: form.serialize(),
                success: function (data) {
                    if (data.success) {
                        Toast.show('success', data.message);

                        // Redirect logic
                        if (data.redirectUrl) {
                            setTimeout(() => {
                                window.location.href = data.redirectUrl;
                            }, 2000);
                        } else if (data.incomplete) {

                            $('#completionAlert').removeClass('d-none');
                        } else {
                            setTimeout(() => {
                                window.location.href = data.redirectUrl || '/Home/Index';
                            }, 2000);
                        }
                    } else {
                        Toast.show('danger', data.message);
                    }
                },
                error: function (xhr) {
                    let message = 'An error occurred. Please try again.';
                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        message = xhr.responseJSON.message;
                    }
                    Toast.show('danger', message);
                }
            });
        });

        $('#newsletterCheck').on('change', function () {
            const checkbox = $(this);
            const isChecked = checkbox.is(':checked');
            const url = checkbox.data('url');

            $.post({
                url: url,
                data: { isSubscribed: isChecked },
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                }
            })
                .done(function (response) {
                    if (response.success) {
                        Toast.show('success', response.message);
                        // Optional: Update UI state
                    } else {
                        checkbox.prop('checked', !isChecked); // Revert checkbox
                        Toast.show('danger', response.message);
                    }
                })
                .fail(function () {
                    checkbox.prop('checked', !isChecked); // Revert checkbox
                    Toast.show('danger', 'Request failed. Please try again.');
                });
        })
        $('.needs-validation').each(function () {
            $(this).on('submit', function (e) {
                if (!this.checkValidity()) {
                    e.preventDefault();
                    e.stopPropagation();
                }
                $(this).addClass('was-validated');
            });
        });

    }

    // Error handling
    function handleAjaxError(xhr) {
        console.error('AJAX Error:', xhr.responseText);
        showToast('Error', 'An error occurred while processing your request', 'danger');
    }

    // Toast notification (ensure you have toast.js included)
    function showToast(title, message, type) {
        const toast = new bootstrap.Toast(document.getElementById('toastNotification'));
        // Update toast content based on parameters
        $('#toastTitle').text(title);
        $('#toastMessage').text(message);
        $('#toastNotification').removeClass('bg-success bg-danger bg-warning').addClass(`bg-${type}`);
        toast.show();
    }

})(jQuery);