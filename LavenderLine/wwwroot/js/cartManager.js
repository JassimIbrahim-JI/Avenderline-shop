var CartManager = {
    init: function () {
        this.bindEvents();
        this.setupAjax();
        this.updateCartState();
        this.updateWishlistState();
        this.initModalAnimations();
    },

    setupAjax: function () {
        $.ajaxSetup({
            beforeSend: function (xhr) {
              
                var token = $('meta[name="__RequestVerificationToken"]').attr('content');
                if (token) {
                    xhr.setRequestHeader("__RequestVerificationToken", token);
                }
                  xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            },
            xhrFields: { withCredentials: true }
        });
    },


    bindEvents: function () {
        var self = this;

        $(document).on('show.bs.modal', '[id^="colorSizeModal-"]', function (e) {
            var $modal = $(this);
            const button = $(e.relatedTarget);
            const productId = button.data('product-id');

            const defaultLength = $modal.data('initial-length');
            const defaultSize = $modal.data('initial-size');

            $modal.find('#lengthDropdown .selected-length-text').text(defaultLength);
            $modal.find('#sizeDropdown .selected-size-text').text(defaultSize);
            $modal.find('#selectedLength').val(defaultLength);
            $modal.find('#selectedSize').val(defaultSize);

            // Update dropdown button text
            $modal.find('#lengthDropdown .dropdown-toggle').html(`
        ${defaultLength} <i class="fa fa-chevron-down language-arrow"></i>
    `);
            $modal.find('#sizeDropdown .dropdown-toggle').html(`
        ${defaultSize} <i class="fa fa-chevron-down language-arrow"></i>
    `);

            // Reset modal state
            $modal.find('.modal-content')
                .removeClass('sold-out-modal loading')
                .find('button[type="submit"]')
                .prop('disabled', false)
                .html('<i class="fas fa-cart-plus"></i> Add to Cart');


            if ($modal.data('ajaxRequest')) {
                $modal.data('ajaxRequest').abort();
            }

            $modal.find('.modal-content').addClass('loading-overlay');

            const ajaxRequest = $.ajax({
                url: `/Cart/GetProductStock?productId=${productId}`,
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "__RequestVerificationToken": $('meta[name="__RequestVerificationToken"]').attr('content')
                }
            }).done(response => {
                if (response.success) {
                    const selectedLength = response.selectedLength || response.lengths[0];
                    const selectedSize = response.selectedSize || response.sizes[0];

                    $modal.find('#selectedLength').val(selectedLength);
                    $modal.find('#selectedSize').val(selectedSize);

                    // Update dropdown button text
                    $modal.find('#lengthDropdown .dropdown-toggle').html(`
                ${selectedLength} <i class="fa fa-chevron-down language-arrow"></i>
            `);
                    $modal.find('#sizeDropdown .dropdown-toggle').html(`
                ${selectedSize} <i class="fa fa-chevron-down language-arrow"></i>
            `);


                    const isSoldOut = response.quantity < 1;
                    $modal.find('.modal-content')
                        .toggleClass('sold-out-modal', isSoldOut)
                        .removeClass('loading-overlay');

                    $modal.find('button[type="submit"]')
                        .prop('disabled', isSoldOut)
                        .html(isSoldOut ?
                            '<i class="fas fa-times-circle"></i> Sold Out' :
                            '<i class="fas fa-cart-plus"></i> Add to Cart');

                    $modal.find('[data-quantity-input]')
                        .attr('max', response.quantity)
                        .val(Math.min(1, response.quantity));
                }
            }).fail((textStatus) => {
                if (textStatus !== 'abort') {
                    $modal.find('.modal-content').removeClass('loading-overlay');
                    Toast.show('danger', 'Failed to check product availability');
                }
            });

            $modal.data('ajaxRequest', ajaxRequest);
        });

        $(document).on('click', '.quantity-btn', function (e) {
            self.handleQuantityChange(e);
        });
        $(document).on('click', '.shop-btn', function (e) {
            const productId = $(this).data('product-id');
            const $productCard = $(this).closest('.product');

            if ($productCard.hasClass('sold-out')) {
                e.preventDefault();
                Toast.show('warning', 'This item is sold out');
                return;
            }
        });

        $(document).on('submit', '[data-cart-form]', function (e) {
            self.handleCartForm(e);
        });

        $(document).on('click', '.remove-item-btn', function (e) {
            self.handleRemoveItem(e);
        });

        $(document).on('click', '#clearCart', function (e) {
            self.handleClearCart(e);
        });

        $(document).on('click', '.length-option', (e) => this.selectLength(e));
        $(document).on('click', '.size-option', (e) => this.selectSize(e));

        $(document).on('click', '.toggle-wishlist-btn', function (e) {
            self.toggleItem(e);
        }.bind(this));

        $(document).on('click', '.remove-from-wishlist-btn', function (e) {
            self.removeItem(e);
        });

        $(document).on('wishlist:updated', function () {
            self.refreshWishlist();
        });
    },


    initModalAnimations: function () {
        $(document).on('show.bs.modal', '[id^="colorSizeModal-"]', function () {
            $(this).addClass('animate__fadeInDown');
        });

        $(document).on('hidden.bs.modal', '[id^="colorSizeModal-"]', function () {
            $(this).removeClass('animate__fadeInDown');
        });


        // Add to initModalAnimations
        $(document).on('mouseenter', '.dropdown-item', function () {
            $(this).css({
                'transform': 'translateX(5px)',
                'background': 'rgba(60, 47, 42, 0.05)'
            });
        }).on('mouseleave', '.dropdown-item', function () {
            $(this).css({
                'transform': 'translateX(0)',
                'background': 'transparent'
            });
        });

        // Add to handleCartForm
        $(document).on('mouseenter', '[data-cart-form] button[type="submit"]', function () {
            $(this).css('transform', 'scale(1.02)');
        }).on('mouseleave', '[data-cart-form] button[type="submit"]', function () {
            $(this).css('transform', 'scale(1)');
        });


        $(document).on('mouseenter', '.option-label', function () {
            $(this).css('transform', 'scale(1.02)');
        }).on('mouseleave', '.option-label', function () {
            if (!$(this).prev('input').is(':checked')) {
                $(this).css('transform', 'scale(1)');
            }
        });
    },

    handleQuantityChange: function (e) {
        e.preventDefault();
        var $btn = $(e.currentTarget);
        var $input = $btn.siblings('[data-quantity-input]');
        var maxQty = parseInt($input.attr('max'), 10) || Infinity;
        var quantity = parseInt($input.val(), 10) || 1;

        if ($btn.data('action') === 'increment') {
            quantity = Math.min(quantity + 1, maxQty);
        } else {
            quantity = Math.max(1, quantity - 1);
        }

        $input.val(quantity);
        this.updateGrandTotal();
        
    },

    selectLength: function (e) {
        e.preventDefault();
        const $btn = $(e.currentTarget);
        const length = $btn.data('length');
        const $dropdown = $btn.closest('.dropdown');
        const $modal = $btn.closest('.modal');
        $modal.find('#lengthDropdown .selected-length-text').text(length);

        $dropdown.find('.dropdown-toggle').html(`
        ${length} <i class="fa fa-chevron-down language-arrow"></i>
    `);
        $btn.closest('.modal').find('#selectedLength').val(length);
    },

    selectSize: function (e) {
        e.preventDefault();
        const $btn = $(e.currentTarget);
        const size = $btn.data('size');
        const $dropdown = $btn.closest('.dropdown');
        const $modal = $btn.closest('.modal');
        $modal.find('#sizeDropdown .selected-size-text').text(size);

        $dropdown.find('.dropdown-toggle').html(`
        ${size} <i class="fa fa-chevron-down language-arrow"></i>
    `);
        $btn.closest('.modal').find('#selectedSize').val(size);
    },

    handleCartForm: function (e) {
        e.preventDefault();
        var self = this;
        var $form = $(e.currentTarget);

        const length = $form.find('#selectedLength').val();
        const size = $form.find('#selectedSize').val();
        var productId = $form.find('input[name="ProductId"]').val();

        if (!length || !size) {
            Toast.show('danger', 'Please select a size and length.');
            return;
        }

        $.ajax({
            url: '/Cart/AddItem',
            method: 'POST',
            data: $form.serialize(),
            headers: {
                 'X-Requested-With': 'XMLHttpRequest'
            },
        }).done(function (response) {
            if (response.success) {
                self.updateCartUI(response);
                self.updateCartButtonIcon(productId, true);
                self.updateCartState();
                Toast.show('cart-add', 'Item added to cart');
                $form.closest('.modal').modal('hide');

            }
        }).fail(function (error) {
            self.handleError(error);
            if (error.responseJSON && error.responseJSON.showSoldOut) {
                self.showSoldoutLabel($form, true);
            }
          
        });
    },
    handleRemoveItem: function (e) {
        e.preventDefault();
        var self = this;
        var $form = $(e.currentTarget).closest('form');
        var productId = $form.find('input[name="ProductId"]').val();

        var isIndexPage = window.location.pathname.toLowerCase().endsWith('/cart') ||
            window.location.pathname.toLowerCase().endsWith('/cart/index');
        $.ajax({
            url: '/Cart/RemoveFromCart',
            method: 'POST',
            data: $form.serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (response) {
            if (response.success) {
                if (isIndexPage) {
                    window.location.href = '/Cart';
                }
                else {
                self.updateCartUI(response);
                self.updateCartButtonIcon(productId, false);
                self.updateCartState();
                Toast.show('cart-remove', 'Item removed from cart');
                }
            }
        }).fail(self.handleError);
    },
    handleClearCart: function (e) {
        var self = this;
        e.preventDefault();

        if (confirm("Clear entire cart?")) {
            $.ajax({
                url: '/Cart/ClearCart',
                method: 'POST'
            }).done(function (response) {
                if (response.success) {
                    self.updateCartUI(response);
                    self.updateCartButtonIcon(response.productId, false);
                    Toast.show('cart-remove', response.message);
                }
            }).fail(self.handleError);
        }
    },
    updateGrandTotal: function () {
        var total = 0;
        $('.cart-item, .modal-body').each(function () {
            var price = parseFloat($(this).data('price')) || 0;
            var qty = parseInt($(this).data('quantity'), 10) || 1;
            total += price * qty;
        });
        $('#grand-total').text('QR ' + total.toLocaleString());
    },
    updateCartUI: function (response) {
        $('#cart-items').html(response.itemsHtml);

        if ($('#cart-items-index').length) {
            $('#cart-items-index').html(response.itemsHtml);
        }
        $('#grand-total').text('QR ' + response.total);
        $('#cart-total').text('QR ' + response.total);
        $('#cartItemCount').text(response.count);


        if (response.count > 0) {
            $('#cart-total-container').removeClass('d-none');
            $('#cartItemCount').removeClass('d-none');
        } else {
            $('#cart-total-container').addClass('d-none');
            $('#cartItemCount').addClass('d-none');
        }

        this.updateGrandTotal();
      

    },
    showSoldoutLabel: function (context, show) {
        var $modal = context.closest ? context : $(context);
        var availableQty = parseInt($modal.find('[data-quantity-input]').attr('max'), 10) || 0;
        var $submitButton = $modal.find('button[type="submit"]');

        $submitButton
            .prop('disabled', availableQty < 1)
            .text(availableQty > 0 ? 'Add to Cart' : 'Sold Out');
    },
    updateCartButtonIcon: function (productId, isInCart) {
        $('.shop-btn[data-product-id="' + productId + '"]').each(function () {
            var $btn = $(this);
            var $icon = $btn.find('i');
            $btn.toggleClass('success', isInCart)
                .prop('disabled', isInCart || $btn.hasClass('sold-out'))
                .attr('title', isInCart ? 'Already in cart' : 'Add to cart');
            $icon.toggleClass('fa-check', isInCart)
                .toggleClass('fa-shopping-cart', !isInCart);
        });
    },
    updateCartState: function () {
        var self = this;
       return $.get('/Cart/GetCartSummary').then(function (response) {
            response.items.forEach(function (item) {
                self.updateCartButtonIcon(item.productId, true);
            });
            $('#cartItemCount').text(response.count);
             $('#cart-total').text('QR '+ response.total);
           // Update cart total visibility
           if (response.count > 0) {
               $('#cart-total-container').removeClass('d-none'); // Show cart total
               $('#cartItemCount').removeClass('d-none');
           } else {
               $('#cart-total-container').addClass('d-none'); // Hide cart total
               $('#cartItemCount').addClass('d-none');
           }


           return response.items;
       }).catch(function (error) {
            console.error('Error updating cart state:', error);
        });
    },

    refreshWishlist: function () {
        var self = this;
        return $.get('/Wishlist/GetWishlistData').done(function (data) {
            self.updateWishlistUI(data);
            self.updateAllWishlistButtons(data.items);
            self.updateWishlistBadge(data.count);
            return data.items;
        }).fail(self.handleError);
    },
    toggleItem: function (e) {
        e.preventDefault();
        var self = this;
        var button = $(e.currentTarget);
        var productId = button.data('product-id');
        button.prop('disabled', true);

        $.ajax({
            url: '/Wishlist/ToggleItem',
            method: 'POST',
            data: { productId: productId },
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).done(function (response) {
            if (response.success) {
                self.updateWishlistUI(response);
                self.updateAllWishlistButtons(response.items);
                self.updateButtonWishlistState(productId, false);
                Toast.show(response.isFavorite ? 'wishlist-add' : 'wishlist-remove',
                    response.isFavorite ? 'Added to wishlist' : 'Removed from wishlist');
                self.updateWishlistState();
            }
        }).fail(function (error) {
            console.error('Error toggling wishlist item:', error);
        }).always(function () {
            button.prop('disabled', false);
        });
    },
    removeItem: function (e) {
        e.preventDefault();
        var self = this;
        var button = $(e.currentTarget);
        var productId = button.data('product-id');
        var token = $('meta[name="__RequestVerificationToken"]').attr('content'); 

        var isIndexPage = window.location.pathname.toLowerCase().endsWith('/wishlist') ||
            window.location.pathname.toLowerCase().endsWith('/wishlist/index');

        $.ajax({
            url: '/Wishlist/RemoveFromWishlist',
            method: 'POST',
            data: { productId: productId, __RequestVerificationToken: token },
            headers: { "X-Requested-With": "XMLHttpRequest", __RequestVerificationToken: token }
        }).done(function (response) {
            if (response.success) {
                if (isIndexPage) {
                   
                    window.location.href = '/Wishlist';
                }
                else {
                self.updateWishlistUI(response);
                self.updateAllWishlistButtons(response.items);
                self.updateButtonWishlistState(productId, false);
                Toast.show('wishlist-remove', 'Item removed from wishlist');
                self.updateWishlistState();
                }
            }
        }).fail(self.handleError);
    },
    updateButtonWishlistState: function (productId, isFavorite) {
        // Update icon-only elements (Shop page)
        $(`.toggle-wishlist-btn[data-product-id="${productId}"]`)
            .filter('i')
            .toggleClass('fas', isFavorite)
            .toggleClass('far', !isFavorite);

        // Update button elements (Details page)
        $(`button.toggle-wishlist-btn[data-product-id="${productId}"] i`)
            .toggleClass('fas', isFavorite)
            .toggleClass('far', !isFavorite);
        $(`button.toggle-wishlist-btn[data-product-id="${productId}"]`)
            .toggleClass('btn-danger', isFavorite)
            .toggleClass('btn-outline-danger', !isFavorite);
    },
    updateAllWishlistButtons: function (items) {
        var self = this;
        $('.toggle-wishlist-btn').each(function (i, element) {
            var $element = $(element);
            var productId = parseInt($element.data('product-id'), 10);
            var isFavorite = items.some(item => item.productId === productId);

            if ($element.is('i')) { // Handle icon-only elements (Shop page)
                $element.toggleClass('fas', isFavorite)
                    .toggleClass('far', !isFavorite);
            } else { // Handle button elements (Details page)
                $element.find('i')
                    .toggleClass('fas', isFavorite)
                    .toggleClass('far', !isFavorite);
                $element.toggleClass('btn-danger', isFavorite)
                    .toggleClass('btn-outline-danger', !isFavorite);
            }
        });
    },
    updateWishlistUI: function (data) {
        $('#wishlist-items').html(data.itemsHtml);
        if ($('#wishlist-details').length) {
            $('#wishlist-details').html(data.itemsHtml);
        }
        this.updateWishlistBadge(data.count);
    },
    updateWishlistBadge: function (count) {
        $('#wishlistItemCount').text(count).toggleClass('d-none', count === 0);
    },
    updateWishlistState: function () {
        this.refreshWishlist();
    },

    handleError: function (error) {
        const message = (error.responseJSON && error.responseJSON.error) || 'Operation failed';
        const type = error.status === 403 ? 'warning' : 'danger';
        Toast.show(type, message);
    }
};

$(function () {
    CartManager.init();
});