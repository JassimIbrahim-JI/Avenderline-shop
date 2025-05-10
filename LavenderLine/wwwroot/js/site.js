(function ($) {
    'use strict';

    var $searchInput = $('.search-input');
    var $resultsDropdown = $('.search-results-dropdown');
    var debounceTimer;

    // Search functionality
    $searchInput.on('input', function (e) {
        clearTimeout(debounceTimer);
        var query = $(this).val().trim();

        var $thisDropdown = $(this).siblings('.search-results-dropdown');

        if (query.length < 2) {
            $thisDropdown.hide();
            return;
        }

        debounceTimer = setTimeout(function () {
            $.getJSON('/Home/Search', { query: query })
                .done(function (data) {
                    showResults(data.products);
                })
                .fail(function (jqXHR, textStatus, errorThrown) {
                    console.error('Search failed:', textStatus, errorThrown);
                    $resultsDropdown.html('<div class="search-item dropdown-item">Search temporarily unavailable</div>').show();
                });
        }, 300);
    });

    function showResults(products) {
        products = products || [];
        $resultsDropdown.empty().hide(); 

        if (products.length === 0) {
            $resultsDropdown.html('<div class="search-item dropdown-item">No results found</div>').show();
            return;
        }

        // Products
        $.each(products, function (index, product) {
            product.imageUrl = product.imageUrl || '/images/placeholder.jpg';
            product.categoryName = product.categoryName || 'Uncategorized';
            var price = product.price ? product.price.toLocaleString() : '0.00';
              
            $resultsDropdown.append(
                $('<a>', {
                    'class': 'search-item dropdown-item product-result',
                    'href': '/Home/Details/' + product.productId,
                    'html': '<img src="' + product.imageUrl + '" class="search-result-img me-2" ' +
                        'alt="' + product.name + '" onerror="this.src=\'/images/placeholder.jpg\'">' +
                        '<div>' +
                        '<div>' + product.name + '</div>' +
                        '<small class="text-muted">' + product.categoryName + '</small>' +
                        '<div class="text-dark">QR ' + price + '</div>' +
                        '</div>'
                })
            );
        });

        console.log("Showing results:", products);

        positionDropdown();
        $resultsDropdown.show();
    }

    function positionDropdown() {
        var isMobile = $(window).width() < 992;

        if (isMobile) {
            $resultsDropdown.css({
                'position': 'absolute',
                'width': '100%',
                'z-index': '1050'
            });
        }
    }

    // Notification system
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

    function validateEmail(email) {
        var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    }


    // Document ready
    $(function () {


        const lazyImages = $('img[data-src]');
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const $img = $(entry.target);
                    if (!$img.attr('src') && $img.data('src')) {
                        $img.attr('src', $img.data('src')).removeAttr('data-src');

                        // Handle image load/error events
                        $img.on('load error', function () {
                            const $shimmer = $img.prev('.shimmer-placeholder');
                            $img.addClass('loaded');
                            $shimmer.hide();
                        });
                    }
                    imageObserver.unobserve(entry.target);
                }
            });
        }, { rootMargin: '200px 0px' });

        // Initialize observer for all lazy images
        lazyImages.each(function () {
            imageObserver.observe(this);
        });

        // Handle initially loaded images (cached)
        $('.product-image-container img, .category-image').each(function () {
            const $img = $(this);
            if ($img[0].complete) {
                $img.addClass('loaded');
                $img.prev('.shimmer-placeholder').hide();
            }
        });

        // Preloader
        $(window).on('load', function () {
            $('.loader, #preloder').hide();
        });

        // Handle window resize
   
        $(window).on('resize', function () {
            $resultsDropdown.each(function () {
                if ($(this).is(':visible')) {
                    positionDropdown();
                }
            });
        });

        // trigger for mobile focus state
        $searchInput.on('focus', function () {
            var $thisDropdown = $(this).siblings('.search-results-dropdown');
            if ($(window).width() < 992) {
                positionDropdown();
                // Only show if there's content
                if ($thisDropdown.children().length > 0) {
                    $thisDropdown.show();
                }
            }
        });

        // Navbar active class
        $('.bottom-navbar .nav-link').on('click', function () {
            $('.bottom-navbar .nav-link').removeClass('active');
            $(this).addClass('active');
        });


        $('.bottom-navbar .dropdown-item').on('click', function () {
            $('.bottom-navbar .dropdown-item').removeClass('active');
            $(this).addClass('active');
        });


        // Toggle dropdowns on mobile  
        const $cartIcon = $('.nav-icon.cart-icon');
        const $wishlistIcon = $('.nav-icon.wishlist-icon');
        const $cartDropdown = $('#cart-dropdown');
        const $wishlistDropdown = $('#wishlist-dropdown');

        // Toggle cart dropdown  
        if ($cartIcon.length && $cartDropdown.length) {
            $cartIcon.on('click', function (e) {
                e.preventDefault();
                $cartDropdown.toggleClass('active');
                $wishlistDropdown.removeClass('active'); // Close wishlist if open  
            });
        }

         
        if ($wishlistIcon.length && $wishlistDropdown.length) {
            $wishlistIcon.on('click', function (e) {
                e.preventDefault();
                $wishlistDropdown.toggleClass('active');
                $cartDropdown.removeClass('active'); // Close cart if open  
            });
        }

        
        $(document).on('click', function (e) {
            if (!$cartIcon.is(e.target) && !$cartIcon.has(e.target).length) {
                $cartDropdown.removeClass('active');
            }
            if (!$wishlistIcon.is(e.target) && !$wishlistIcon.has(e.target).length) {
                $wishlistDropdown.removeClass('active');
            }
        });

        // Close dropdowns with close button  
        $('.btn-close-dropdown').on('click', function () {
            const $dropdown = $(this).closest('.cart-dropdown, .wishlist-dropdown');
            if ($dropdown.length) {
                $dropdown.removeClass('active');
            }
        });  
     
     

        // Initialize tooltips
        $('[data-bs-toggle="tooltip"]').tooltip();

        // Scroll effects
        var lastScrollTop = 0;
        var SCROLL_THRESHOLD = 50;

        $(window).on('scroll', function () {
            var scrolled = $(this).scrollTop();
            var translateY = scrolled * 0.5;

            // Parallax effect
            $('#fashionCarousel .carousel-item img').css('transform', 'translateY(' + translateY + 'px)');

            if (Math.abs(scrolled - lastScrollTop) > SCROLL_THRESHOLD) {
                var $middleNavbar = $('#middleNavbar');
                var $bottomNavbar = $('#bottomNavbar');

                if (scrolled > lastScrollTop) {
                    $middleNavbar.add($bottomNavbar).addClass('navbar-hidden');
                } else {
                    $middleNavbar.add($bottomNavbar).removeClass('navbar-hidden');
                }
                lastScrollTop = scrolled;
            }
        });
    

        // Notification system
        updateNotificationBadge();

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

        // Search dropdown handling
        $(document).on('click', function (e) {
            if (!$(e.target).closest('.input-group').length) {
                $resultsDropdown.hide();
            }
        });

        $resultsDropdown.on('click', function (e) {
            e.stopPropagation();
        });

        $('input[required]').on('input', function () {
            const field = $(this);
            if (field.val().trim()) {
                field.removeClass('is-invalid');
                field.addClass('is-valid');
            } else {
                field.removeClass('is-valid');
                field.addClass('is-invalid');
            }
        });

        $('#profileForm').on('submit', function (e) {
            e.preventDefault();
            const form = $(this);
            // Clear previous validation states
            $('.is-invalid, .is-valid').removeClass('is-invalid is-valid');

            // Validate required fields
            let isValid = true;
            // Updated field names to match the form
            const requiredFields = ['FullName', 'Area', 'StreetAddress', 'PhoneNumber'];

            requiredFields.forEach(field => {
                const input = $(`#${field}`);
                if (!input.val().trim()) {
                    input.addClass('is-invalid');
                    isValid = false;
                } else {
                    input.addClass('is-valid');
                }
            });

            // Phone validation (matching backend regex)
            const phoneVal = $('#PhoneNumber').val().replace(/\D/g, '');
            if (!/^[3567]\d{7}$/.test(phoneVal)) {
                $('#PhoneNumber').addClass('is-invalid');
                return false;
            }

            // Check form validity
            if (!isValid) {
                $('.validation-summary-errors').removeClass('d-none');
                Toast.show('danger', 'Please fill in all required fields correctly.');
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
                        updateProfileUI(response);

                        if (response.fullName) {
                            $('.user-fullname').text(response.fullName);
                            $('.navbar-user-name').text(response.fullName);
                        }

                        // Redirect logic
                        if (data.redirectUrl) {
                            setTimeout(() => {
                                window.location.href = data.redirectUrl;
                            }, 1500);

                        }

                        else if (data.incomplete) {
                            // Show completion alert if it exists
                            if ($('#completionAlert').length) {
                                $('#completionAlert').removeClass('d-none');
                            } else {
                                // If element doesn't exist, use validation summary
                                $('.validation-summary-errors').removeClass('d-none')
                                    .html('<div>Please complete all required fields to continue</div>');
                            }
                        } else {
                            // Fallback redirect
                            setTimeout(() => {
                                window.location.href = '/Home/Index';
                            }, 2000);
                        }
                    } else {
                        Toast.show('danger', data.message);
                        // Show validation errors if available
                        if (data.validationErrors) {
                            $('.validation-summary-errors').removeClass('d-none')
                                .html(data.validationErrors.map(e => `<div>${e}</div>`).join(''));
                        }
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

        function updateProfileUI(data) {
            // Update progress bar
            const completionPercentage = calculateCompletionPercentage(data);
            $('.progress-bar').css('width', completionPercentage + '%')
                .attr('aria-valuenow', completionPercentage);

            // Update completion status text
            if (completionPercentage === 100) {
                $('.completion-status').html(
                    '<span class="text-success"><i class="fas fa-check-circle"></i> Profile Complete!</span>'
                );
            } else {
                $('.completion-status').html(
                    `<span class="text-muted">${completionPercentage}% Complete - Fill all required fields</span>`
                );
            }

            // Update form values in case they were modified server-side
            if (data.phoneNumber) {
                $('#PhoneNumber').val(data.phoneNumber.replace('+974', ''));
            }
        }


        // Phone number auto-formatting
        $('#PhoneNumber').on('input', function () {
            let num = $(this).val().replace(/\D/g, '');
            num = num.replace(/^(\+?974|974)/, '').replace(/^0+/, '');

            if (/^[3567]/.test(num)) {
                $(this).val(num.substring(0, 8));
            }
        });

        // Form field validation
        $('#Area').on('change', function () {
            const isValid = $(this).val() !== '';
            $(this).toggleClass('is-valid', isValid).toggleClass('is-invalid', !isValid);
        });

        $('#StreetAddress').on('input', function () {
            const isValid = $(this).val().length >= 5;
            $(this).toggleClass('is-valid', isValid).toggleClass('is-invalid', !isValid);
        });

        // Add validation for FullName field
        $('#FullName').on('input', function () {
            const isValid = $(this).val().trim() !== '';
            $(this).toggleClass('is-valid', isValid).toggleClass('is-invalid', !isValid);
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
    });

    $("#button-addon2").on('click', function () {
        const $btn = $(this);
        const email = $("#emailInput").val().trim();
        const $form = $("#nonAuthSubscribeForm");

        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!validateEmail(email)) {
            Toast.show('danger', 'Please enter a valid email address.');
            return;
        }

        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i>');

        $.ajax({
            url: '/Home/SubscribeNonLoggedIn',
            method: 'POST',
            data: {
                email: email,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.isResubscribe) {
                    Toast.show('success', 'Welcome back! You have been resubscribed.');
                } else {
                    Toast.show('success', response.message);
                }
                $btn.prop('disabled', true);
            },
            error: function (xhr, status, error) {
                console.error("Subscription request failed", {
                    status: xhr.status,
                    statusText: xhr.statusText,
                    responseText: xhr.responseText,
                    error: error
                });
                try {
                    const errorResponse = xhr.responseJSON || JSON.parse(xhr.responseText);
                    console.error("Error response:", errorResponse);
                    Toast.show('danger', errorResponse?.message || 'Subscription failed');
                } catch (e) {
                    console.error("Could not parse error response:", e);
                    Toast.show('danger', 'Subscription failed: ' + xhr.status);
                }
            },
            complete: () => {
                console.log("AJAX request completed");
                $btn.prop('disabled', false).html('Subscribe');
            }
        });
    });

    $('.needs-validation').each(function () {
        const form = $(this);
        form.on('submit', function (e) {
            if (!this.checkValidity()) {
                e.preventDefault();
                e.stopPropagation();
            }
            form.addClass('was-validated');
        });
    });


})(jQuery);
