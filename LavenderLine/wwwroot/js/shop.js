(function ($) {
    'use strict';



    // Configuration
    var config = {
        defaultPageSize: 9,
        maxPageSize: 36,
        debounceTime: 500,
        priceDecimalPlaces: 2
    };

    // State
    var isLoading = false;
    let initialLoad = true;
    var currentPage = 1;
    var totalProducts = 0;
    var pageSize = config.defaultPageSize;
    var sliderDebounce;

    // Cached DOM Elements
    var $productContainer = $('#productContainer');
    var $loadingIndicator = $('#loading-overlay');
    var $loadMore = $('#loadMore');
    var $priceSlider = $('#priceSlider');
    var $sortBy = $('#sortBy');
    var $showPerPage = $('#showPerPage');

    // Price Slider Initialization
    function initializePriceSlider(category = 'all') {
        return $.ajax({
            url: '/Home/GetPriceRange',
            data: { category: category },
            dataType: 'json',
            timeout:10000
        }).then(function (data) {
            if (!data || typeof data.min === 'undefined' || typeof data.max === 'undefined') {
                throw new Error("Invalid price range data");
            }

            // Handle zero-range scenario
            if (data.min === data.max) data.max += 100;

            // Destroy existing slider
            if ($priceSlider[0].noUiSlider) {
                $priceSlider[0].noUiSlider.destroy();
            }

            // Create new slider
            noUiSlider.create($priceSlider[0], {
                start: [data.min, data.max],
                connect: true,
                range: {
                    min: data.min,
                    max: data.max === data.min ? data.min + 100 : data.max
                },
                format: {
                    to: value => value.toFixed(config.priceDecimalPlaces),
                    from: Number
                }
            });

            // Initial price box update
            updatePriceBoxes(data.min, data.max);

            // Debounced update handler
            $priceSlider[0].noUiSlider.on('update', (values) => {
                updatePriceBoxes(values[0], values[1]);
                clearTimeout(sliderDebounce);
                sliderDebounce = setTimeout(() => {
                    currentPage = 1;
                    loadProducts();
                }, config.debounceTime);
            });

            return data;
        }).catch(error => {
            console.error("Price slider error:", error);
            throw error;
        });
    }

    function updatePriceBoxes(min, max) {
        $('#minPrice').val(`QR ${parseFloat(min).toFixed(config.priceDecimalPlaces)}`);
        $('#maxPrice').val(`QR ${parseFloat(max).toFixed(config.priceDecimalPlaces)}`);
    }

    // Product Loading
    async function loadProducts() {
        if (isLoading) return;

        if (!initialLoad) {
            $loadingIndicator.show();
        }
        // Determine if the current page is the arrivals page
        const isArrivalPage = window.location.pathname.includes('/Home/Arrivals');

        try {

            await new Promise(resolve => setTimeout(resolve, 2000));
            const token = $('meta[name="__RequestVerificationToken"]').attr('content');
            const category = $('input[name="category"]:checked').val();
            const priceRange = $priceSlider[0].noUiSlider.get(true);

            const response = await $.ajax({
                url: '/Home/GetFilteredProducts',
                data: {
                    __RequestVerificationToken: token,
                    category: category,
                    minPrice: parseFloat(priceRange[0]),
                    maxPrice: parseFloat(priceRange[1]),
                    sortBy: $sortBy.val(),
                    page: currentPage,
                    pageSize: Math.min(pageSize, config.maxPageSize),
                    showExcludedOnly : isArrivalPage ? true : "Shop page"
                },
                dataType: 'json'
            });

            renderProducts(response.products);
            totalProducts = response.totalCount;
            updateProductCount();

            var cartItems = await CartManager.updateCartState();
            const inCartIds = cartItems?.map?.(item => item.productId) || [];
            updateCartButtonState(inCartIds);

            CartManager.refreshWishlist();

        } catch (error) {
            console.error("Product load error:", error);
            Toast.show("danger", "Failed to load products. Please try again.");
        } finally {
            if (!initialLoad) {
                $loadingIndicator.hide();
            }
            // flip the flag after the very first call:
            initialLoad = false;
            isLoading = false;
        }
    }
    function updateCartButtonState(inCartIds)
    {
        $('.shop-btn').each(function () {
            const productId = $(this).data('product-id');
            const isInCart = inCartIds.includes(productId);
            CartManager.updateCartButtonIcon(productId, isInCart);
        });
    }


    function initLazyLoading() {
        const $lazyImages = $('img[data-src]');
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (!img.src && img.dataset.src) {
                        img.src = img.dataset.src;
                    }
                    $(img).removeAttr('data-src');
                    observer.unobserve(img);
                }
            });
        }, {
            rootMargin: '200px 0px'
        });

        $lazyImages.each((index, img) => observer.observe(img));
    }

    function renderProducts(products) {
        if (currentPage === 1) {
            $productContainer.empty();
        }

        if (!products?.length) {
            $productContainer.html(`
            <div class="col-12 text-center py-5">
                <i class="fas fa-box-open fa-3x text-muted mb-3"></i>
                <h4>No products found</h4>
                <p>Try adjusting your filters</p>
            </div>
        `);
            return;
        }

        const productHtml = products.map(product => `
        <div class="col-md-4 mb-4">
            <div class="product mx-2 p-0 mb-5">
                <div class="product-image-container ${product.quantity < 1 ? 'sold-out' : ''}">
              
                <div class="shimmer-placeholder" aria-hidden="true"></div>

                    <img src="${product.imageUrl}" 
                        alt="${product.name}"
                        class="img-fluid"
                        loading="lazy"
                        decoding="async"
                        data-src="${product.imageUrl}"
                        width="400"
                        height="400">
                         
                   ${product.quantity >= 1 ? `
            <div class="product-overlay">
                <button class="shop-btn"
                        data-product-id="${product.productId}"
                        data-bs-toggle="modal"
                        data-bs-target="#colorSizeModal-${product.productId}">
                    <i class="fas fa-shopping-cart"></i>
                </button>
                <a href="/Home/Details/${product.productId}" class="quick-view-button">
                    <i class="fas fa-eye"></i> Quick View
                </a>
            </div>` : ''}

                   ${product.quantity < 1 ? '<div class="sold-out-label">Sold Out</div>' : ''}

                </div>
                <label class="text-muted">
                    ${product.categoryName}
                </label>
                <h3 class="product-name">${product.name}</h3>
                <div class="product-price">
                    ${product.isOnSale ?
                `<span class="text-muted text-decoration-line-through me-2">
                            ${product.originalPrice.toFixed(2)}
                        </span>` : ''}
                    <span class="current-price fw-bold">
                        QR ${product.price.toLocaleString()}
                    </span>
                </div>
                <i class="far fa-heart favi-icon toggle-wishlist-btn" 
                   data-product-id="${product.productId}" 
                   title="Add to Favorites"></i>
            </div>
        </div>

        <!-- Modal for this product -->
        <div class="modal fade" id="colorSizeModal-${product.productId}" tabindex="-1" 
             aria-labelledby="colorSizeModalLabel" aria-hidden="true" 
             data-initial-length="${product.lengths[0]}"
             data-initial-size="${product.sizes[0]}">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="colorSizeModalLabel">Customize ${product.name}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <form action="/Cart/AddItem" method="post" data-cart-form>
                        <input type="hidden" name="__RequestVerificationToken" value="${$('input[name="__RequestVerificationToken"]').val()}" />
                        <input type="hidden" name="ProductId" value="${product.productId}" />
                        <input type="hidden" id="selectedLength" name="Length" value="${product.lengths[0]}" required />
                        <input type="hidden" id="selectedSize" name="Size" value="${product.sizes[0]}" required />

                        <div class="modal-body">
                            <div class="d-flex align-items-center">
                                <img src="${product.imageUrl}" class="product-image-modal me-3"
                                     loading="lazy"
                                     width="400"
                                     height="400">
                                <div>
                                    <h5 class="mb-1">${product.name}</h5>
                                    <p class="text-muted mb-2">QAR ${product.price.toFixed(2)}</p>
                                    <div class="d-flex gap-2">
                                        <!-- Length Selector -->
                                        <div class="w-100">
                                            <div class="dropdown sizelength-selector border">
                                                <button class="dropdown-toggle d-flex align-items-center justify-content-between text-dark w-100" 
                                                        type="button" 
                                                        id="lengthDropdown" 
                                                        data-bs-toggle="dropdown" 
                                                        aria-expanded="false">
                                                    <span class="selected-length-text">${product.lengths[0]}</span>
                                                    <i class="fa fa-chevron-down language-arrow"></i>
                                                </button>
                                                <ul class="dropdown-menu w-100" aria-labelledby="lengthDropdown">
                                                    ${product.lengths.map(length => `
                                                        <li>
                                                            <a class="dropdown-item length-option" 
                                                               data-length="${length}">
                                                                ${length}
                                                            </a>
                                                        </li>
                                                    `).join('')}
                                                </ul>
                                            </div>
                                            <small class="text-muted mt-1 d-block">Selected Length: <span class="selected-length">${product.lengths[0]}</span></small>
                                        </div>

                                        <!-- Size Selector -->
                                        <div class="w-100">
                                            <div class="dropdown sizelength-selector border">
                                                <button type="button" 
                                                        class="dropdown-toggle d-flex align-items-center justify-content-between text-dark w-100" 
                                                        id="sizeDropdown" 
                                                        aria-expanded="false" 
                                                        data-bs-toggle="dropdown">
                                                    <span class="selected-size-text">${product.sizes[0]}</span>
                                                    <i class="fa fa-chevron-down language-arrow"></i>
                                                </button>
                                                <ul class="dropdown-menu w-100" aria-labelledby="sizeDropdown">
                                                    ${product.sizes.map(size => `
                                                        <li>
                                                            <a class="dropdown-item size-option" 
                                                               data-size="${size}">
                                                                ${size}
                                                            </a>
                                                        </li>
                                                    `).join('')}
                                                </ul>
                                            </div>
                                            <small class="text-muted mt-1 d-block">Selected Size: <span class="selected-size">${product.sizes[0]}</span></small>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- Quantity Selector -->
                            <div class="d-flex align-items-center gap-3 mt-2">
                                <div class="input-group border" style="max-width: 150px;">
                                    <div class="d-flex align-items-center">
                                        <button type="button" 
                                                class="btn btn-sm btn-custom quantity-btn" 
                                                data-action="decrement">-</button>
                                        <input type="number" 
                                               name="Quantity" 
                                               class="form-control mx-2 text-center bg-white no-border" 
                                               value="1" 
                                               min="1" 
                                               max="${product.quantity}"
                                               data-quantity-input 
                                               readonly 
                                               style="max-width: 50px;">
                                        <button type="button" 
                                                class="btn btn-sm btn-custom quantity-btn" 
                                                data-action="increment">+</button>
                                    </div>
                                </div>
                                <small class="text-muted">Available: ${product.quantity}</small>
                            </div>
                        </div>

                        <div class="modal-footer">
                            <div class="row w-100 justify-content-center">
                                <div class="col-auto">
                                    <button type="submit" 
                                            class="shop-modal-btn w-100 btn-lg text-nowrap" 
                                            ${product.quantity < 1 ? "disabled" : ""}>
                                        <i class="fas fa-cart-plus me-2"></i>
                                        ${product.quantity > 0 ? "Add to Cart" : "Sold Out"}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `).join('');

        $productContainer[currentPage === 1 ? 'html' : 'append'](productHtml);
        $loadMore.toggle((currentPage * pageSize) < totalProducts);

        initializeModalOptions();
        handleImageLoad();
        initLazyLoading();
        CartManager.refreshWishlist();
    }


    function handleImageLoad() {
        document.querySelectorAll('.product-image-container img').forEach(img => {
            const shimmer = img.previousElementSibling;
            if (img.complete) {
                img.classList.add('loaded');
                shimmer.style.display = 'none';
            }

            img.addEventListener('load', () => {
                img.classList.add('loaded');
                shimmer.style.display = 'none';
            });

            img.addEventListener('error', () => {
                shimmer.style.display = 'none';
                console.error('Image failed to load:', img.dataset.src);
            });
        });
    }

    function initializeModalOptions() {
        // Set up length option handlers
        $('.length-option').off('click').on('click', function () {
            const selectedLength = $(this).data('length');
            const $modal = $(this).closest('.modal');

            // Update hidden input
            $modal.find('#selectedLength').val(selectedLength);

            // Update dropdown text
            $modal.find('.selected-length-text').text(selectedLength);
            $modal.find('.selected-length').text(selectedLength);
        });

        // Set up size option handlers
        $('.size-option').off('click').on('click', function () {
            const selectedSize = $(this).data('size');
            const $modal = $(this).closest('.modal');

            // Update hidden input
            $modal.find('#selectedSize').val(selectedSize);

            // Update dropdown text
            $modal.find('.selected-size-text').text(selectedSize);
            $modal.find('.selected-size').text(selectedSize);
        });
        
        $('.modal').each(function () {
            const initialLength = $(this).data('initial-length');
            const initialSize = $(this).data('initial-size');

            if (initialLength && initialSize) {
                $(this).find('#selectedLength').val(initialLength);
                $(this).find('#selectedSize').val(initialSize);
                $(this).find('.selected-length-text, .selected-length').text(initialLength);
                $(this).find('.selected-size-text, .selected-size').text(initialSize);
            }
        });
    }


    // Product Count Update
    function updateProductCount() {
        const start = ((currentPage - 1) * pageSize) + 1;
        const end = Math.min(currentPage * pageSize, totalProducts);
        const displayText = `Showing ${String(start).padStart(2, '0')}-${end} of ${totalProducts} products`;
        $('.filter-sorting p').text(displayText);
    }

    // Initialization
    $(function () {
        initializePriceSlider()
            .then(loadProducts)
            .catch(console.error);


        initializeModalOptions();

        // Event Handlers
        $('input[name="category"]').on('change', function () {
            currentPage = 1;
            initializePriceSlider($(this).val())
                .then(loadProducts)
                .catch(console.error);
        });

        $showPerPage.on('change', function () {
            currentPage = 1;
            pageSize = Math.min(parseInt($(this).val(), 10) || config.defaultPageSize, config.maxPageSize);
            loadProducts();
        });

        $sortBy.on('change', () => {
            currentPage = 1;
            loadProducts();
        });


        $('#filterButton').on('click', function () {
            currentPage = 1;
            loadProducts();
        });

        $loadMore.on('click', function () {
            if ((currentPage * pageSize) < totalProducts) {
                currentPage++;
                loadProducts();
            }
        });
    });

})(jQuery);