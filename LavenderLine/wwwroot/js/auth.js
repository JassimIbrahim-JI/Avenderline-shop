(function ($) {
    'use strict';

    $.fn.loadingState = function (isLoading) {
        return this.each(function () {
            const $btn = $(this);
            if (isLoading) {
                $btn.data('original-text', $btn.html())
                    .prop('disabled', true)
                    .html('<span>Loading...</span>');
            } else {
                $btn.prop('disabled', false)
                    .html($btn.data('original-text'));
            }
        });
    };


    class AuthManager {
        constructor() {
            this.initCommonFeatures();
            this.initPageSpecificFeatures();
            this.checkPersistedLoadingState();
            this.initSessionMonitoring();
            this.checkServerMessages();
            this.handleServerErrors();
        }

        // Shared features across all auth pages
        initCommonFeatures() {
            // Password visibility toggle
            $('.password-toggle').on('click', (e) => this.togglePasswordVisibility(e));

            // Form submission handling
            $('.auth-form').on('submit', (e) => this.handleFormSubmit(e));

            // Input validation
            $('input[type="password"]').on('input', (e) => this.handlePasswordInput(e));

            $('.google-login-btn').on('click', (e) => this.handleGoogleLogin(e));

            this.validateContactNumber(); 
        }

        // Page-specific features
        initPageSpecificFeatures() {
            if ($('#strengthMeter').length) {
                // Password strength meter (for reset password and register)
                $('#Password').on('input', (e) => this.checkPasswordStrength($(e.target).val()));
            }

            if ($('#ConfirmPassword').length) {
                // Password match checker (for reset password and register)
                $('#ConfirmPassword').on('input', () => this.checkMatchPassword());
            }
        }

        initSessionMonitoring() {
            $(document).on('heartbeat:expired', () => {
                Toast.show('warning', 'Session will expire soon');
            });

            $(document).on('heartbeat:dead', () => {
                window.location.reload();
            });
        }

        // Toggle password visibility
        togglePasswordVisibility(e) {
            const target = $(e.currentTarget);
            const input = target.closest('.password-field').find('input');
            const isVisible = input.attr('type') === 'text';

            const $message = $('#passwordVisibilityMessage');
            $message.text(isVisible ? 'Password hidden' : 'Password visible');
            setTimeout(() => $message.text(''), 2000);

            input.attr('type', isVisible ? 'password' : 'text');
            target.attr('aria-label', isVisible ? 'Show password' : 'Hide password');
            target.find('svg').html(this.getVisibilityIcon(!isVisible));
        }

        // Get visibility icon SVG
        getVisibilityIcon(visible) {
            return visible ?
                `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M13.9 4.9L17.1 8.1M8.1 17.1L4.9 13.9M19.3 15.3L21 12L19.3 8.7M8.7 4.7L12 3L15.3 4.7M4.7 15.3L3 12L4.7 8.7M15.3 19.3L12 21L8.7 19.3M12 7C9.2 7 7 9.2 7 12C7 14.8 9.2 17 12 17C14.8 17 17 14.8 17 12C17 9.2 14.8 7 12 7Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>` :
                `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M15 12C15 13.6569 13.6569 15 12 15C10.3431 15 9 13.6569 9 12C9 10.3431 10.3431 9 12 9C13.6569 9 15 10.3431 15 12Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    <path d="M12 5C17.5228 5 22 12 22 12C22 12 17.5228 19 12 19C6.47715 19 2 12 2 12C2 12 6.47715 5 12 5Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>`;
        }

        // Handle password input (strength and match)
        handlePasswordInput(e) {
            const password = $(e.target).val();
            if ($('#strengthMeter').length) {
                this.checkPasswordStrength(password);
            }
            if ($('#ConfirmPassword').length) {
                this.checkMatchPassword();
            }
        }

        checkPasswordStrength(password) {
            const zxcvbn = window.zxcvbn; 
            const result = zxcvbn(password);

            this.updateStrengthMeter(result.score);
        }

        updateStrengthMeter(score) {
            const classes = ['strength-weak', 'strength-medium', 'strength-strong'];
            const messages = [
                'Very weak', 'Weak', 'Fair', 'Strong', 'Very strong'
            ];

            $('#strengthMeter')
                .removeClass(classes.join(' '))
                .addClass(classes[Math.floor(score / 2)]);

            $('#passwordStrengthMessage')
                .text(messages[score])
                .removeClass('text-danger text-warning text-success')
                .addClass(score >= 3 ? 'text-success' : score >= 2 ? 'text-warning' : 'text-danger');
        }

        // Check if password has required characters
        hasRequiredChars(password) {
            return /[A-Z]/.test(password) &&
                /[0-9]/.test(password) &&
                /[^A-Za-z0-9]/.test(password);
        }

         validateEmail(email) {
        var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
        }

         validateContactNumber()
            {
             $('#PhoneNumber').on('input', function () {
                 let num = $(this).val().replace(/\D/g, '');
                 num = num.replace(/^(\+?974|974)/, ''); // Remove any prefix
                 num = num.replace(/^0+/, ''); // Remove leading zeros

                 if (/^[3567]/.test(num)) {
                     $(this).val(num.substring(0, 8)); 
                 } else {
                     $(this).val(num); 
                 }
             });

            }

        checkPersistedLoadingState() {
            if (sessionStorage.getItem('googleLoginInProgress')) {
                $('.google-login-btn').loadingState(true);
                sessionStorage.removeItem('googleLoginInProgress');
            }
        }

        checkServerMessages() {
            // Read temp data passed through server
            const toastSuccess = $('#temp-toast-success').text();
            const toastError = $('#temp-toast-error').text();

            if (toastSuccess) Toast.show('success', toastSuccess);
            if (toastError) Toast.show('danger', toastError);
        }

        handleServerErrors() {
            const urlParams = new URLSearchParams(window.location.search);
            const error = urlParams.get('error');

            if (error) {
                Toast.show('danger', decodeURIComponent(error));
                history.replaceState(null, '', window.location.pathname); // Clean URL
            }
        }

        // Check if passwords match
        checkMatchPassword() {
            const password = $('#Password').val();
            const confirm = $('#ConfirmPassword').val();
            const $message = $('#passwordMatchMessage');

            $message.removeClass('text-danger text-success');

            if (!confirm) {
                $message.text('');
                return;
            }

            if (password !== confirm) {
                $message.text('Passwords don\'t match').addClass('text-danger');
            } else {
                $message.text('Passwords match').addClass('text-success');
            }
        }

        // Handle form submission
        async handleFormSubmit(e) {
            e.preventDefault();
            const form = $(e.target);
            const btn = form.find('button[type="submit"]');

            var email = $("#emailInput").val().trim();

            if (!this.validateEmail(email)) {
                Toast.show('danger', 'Please enter a valid email address.');
                return;
            }

            // Client-side validation
            if (!this.validateForm(form)) return;

            try {
                btn.loadingState(true);
                const response = await $.ajax({
                    url: form.attr('action'),
                    method: 'POST',
                    data: form.serialize()
                });

                this.handleSubmitResponse(response);
            } catch (error) {
                this.handleSubmitResponse(error.responseJSON || {
                    success: false,
                    errorCode: 'connection_error'
                });
            } finally {
                btn.loadingState(false);
            }
        }

        // Validate form inputs
        validateForm(form) {
            let isValid = true;
            form.find('input[required]').each(function () {
                const $input = $(this);
                if (!$input.val().trim()) {
                    isValid = false;
                    $input.addClass('is-invalid');
                } else {
                    $input.removeClass('is-invalid');
                }
            });
            return isValid;
        }

        // Handle form submission response
        handleSubmitResponse(response) {

            if (response.success) {
                const toastConfig = {
                    login: { type: 'success', icon: 'login', msg: '🔓 Welcome back! Redirecting...' },
                    logout: { type: 'info', icon: 'login', msg: '👋 Session ended securely' },
                    register: { type: 'success', icon: 'register', msg: '🎉 Account created! Check your email' },
                    email_confirmation: { type: 'success', icon: 'email', msg: '📬 Email verified! Full access granted' },

                    // Password Management
                    password_reset: { type: 'success', icon: 'password', msg: '🔑 Password updated! Login with new credentials' },
                    password_reset_request: { type: 'info', icon: 'email', msg: '📧 Reset link sent if email exists' },

                    // Profile
                    profile_update: { type: 'success', icon: 'profile', msg: '📝 Profile saved successfully' },
                    subscription_update: { type: 'success', icon: 'email', msg: '📩 Subscription preferences updated' },

                    // Google Auth
                    google_login: { type: 'success', icon: 'login', msg: '🌐 Signed in with Google!' },
                    google_register: { type: 'success', icon: 'register', msg: '🌟 Google account linked!' }


                }[response.action] || { type: 'success', icon: 'check', msg: response.message };

                Toast.show(toastConfig.type, toastConfig.msg, toastConfig.icon);
              
                if (response.redirectUrl) {
                    setTimeout(() => {
                        window.location.href = response.redirectUrl;
                    }, response.action === 'logout' ? 500 : 1500);
                }

            } else {
                const errorMap = {
                    invalid_credentials: { type: 'danger', icon: 'warning', msg: '❌ Invalid email/password combination' },
                    email_not_verified: { type: 'warning', icon: 'email', msg: '📭 Verify email to continue' },
                    duplicate_email: { type: 'warning', icon: 'email', msg: '📨 Email already registered' },
                    password_mismatch: { type: 'danger', icon: 'warning', msg: '🔒 Passwords do not match' },
                    password_weak: { type: 'danger', icon: 'warning', msg: '⚠️ Password needs 8+ chars with mix' },
                    session_expired: { type: 'info', icon: 'clock', msg: '⏳ Session expired - please login again' },
                    google_auth_failed: { type: 'danger', msg: '❌ Google authentication failed', icon: 'warning' },
                    email_missing: { type: 'danger', icon: 'email', msg: '❌ No email found with your Google account.' },
                    user_creation_failed: { type: 'danger', icon: 'warning', msg: '❌ Account creation failed. Please contact support.' },
                    invalid_state: { type: 'danger', icon: 'shield', msg: 'Security validation failed. Please refresh and try again.'},
                    invalid_nonce: { type: 'danger', icon: 'refresh-cw', msg: 'Session security mismatch. Please try again.'}
                };

                const error = errorMap[response.errorCode] || {
                    msg: response.message || '⚠️ Something went wrong',
                    type: 'danger'
                };
                Toast.show(error.type, error.msg, error.icon);
                this.highlightInvalidFields(response.errors)
            }
        }

        highlightInvalidFields(errors) {
            $('.is-invalid').removeClass('is-invalid');
            if (errors) {
                Object.keys(errors).forEach(field => {
                    $(`#${field}`).addClass('is-invalid');
                });
            }
        }

        async handleGoogleLogin(e) {
            e.preventDefault();
            const btn = $(e.currentTarget);
            try {
                btn.loadingState(true);

                // Store the current URL or intended destination
                let returnUrl = null;

                // First check URL params
                const urlParams = new URLSearchParams(window.location.search);
                returnUrl = urlParams.get('returnUrl');

                // If no returnUrl in params, use current path or a default path
                if (!returnUrl) {
                    // You might want to store the intended destination in sessionStorage
                    returnUrl = sessionStorage.getItem('intendedDestination') || window.location.pathname;

                    // Clear it after using
                    sessionStorage.removeItem('intendedDestination');
                }

                // Redirect to Google login with the return URL
                window.location.href = `/Account/GoogleLogin?returnUrl=${encodeURIComponent(returnUrl)}`;
            } catch (error) {
                console.error('Google login error:', error);
                Toast.show('danger', 'Google login failed. Please try again.', 'warning');
            } finally {
                btn.loadingState(false);
            }
        }
    }

    $(function () {
        new AuthManager();
    });

})(jQuery);
