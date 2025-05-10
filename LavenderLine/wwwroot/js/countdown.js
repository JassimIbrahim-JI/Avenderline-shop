'use strict';

// === Global Functions ===

function initializeCountdown(endDate) {
    const timerElement = $('.countdown-timer');
    const endTime = new Date(endDate).getTime();

    // Clear existing interval
    const existingInterval = timerElement.data('countdownInterval');
    if (existingInterval) {
        clearInterval(existingInterval);
    }

    const interval = setInterval(() => {
        const now = new Date().getTime();
        const distance = endTime - now;

        if (distance < 0) {
            clearInterval(interval);
            timerElement.html('<div class="text-danger fw-bold">Promotion Ended!</div>');
            return;
        }

        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        $('#hoursValue').text(String(hours).padStart(2, '0'));
        $('#minutesValue').text(String(minutes).padStart(2, '0'));
        $('#secondsValue').text(String(seconds).padStart(2, '0'));
    }, 1000);

    timerElement.data('countdownInterval', interval);
}

$(function ()
{

    const initialEndDate = $('.countdown-timer').data('enddate');
    if (initialEndDate) {
        initializeCountdown(initialEndDate);
    }

    $(".promotion-checkbox").on("change", function () {
        var productId = parseInt($(this).data("id"));
        var category = $(this).data("category");

        if (this.checked) {
            // Open the modal and set data
            $("#promotionModal")
                .data("productId", productId)
                .data("category", category);

            $("#promotionCategory").val(category);
            $("#promotionProductId").val(productId)
            var promotionModal = new bootstrap.Modal(document.getElementById('promotionModal'));
            promotionModal.show();
        } else {
            // Handle removal of promotion if unchecked
            $.ajax({
                url: '/AdminProduct/RemovePromotion',
                type: 'POST',
                data: { productId: productId },
                success: function (response) {
                    if (response.success) {
                        alert("Promotion removed successfully!");
                        location.reload();
                    } else {
                        alert("Failed to remove promotion: " + response.message);
                    }
                },
                error: function (error) {
                    console.error("Error removing promotion:", error);
                }
            });
        }
    });

    $("#savePromotionBtn").on("click", function () {
        // Check if form is valid
        var isValid = $("#promotionForm")[0].checkValidity();
        if (!isValid) {
            alert("Please fill out all required fields.");
            return;
        }

        var promotion = {
            Title: $("#promotionTitle").val(),
            PromotionText: $("#promotionText").val(),
            DiscountPercentage: $("#discountPercentage").val(),
            EndDate: $("#endDate").val(),
            ProductId: parseInt($("#promotionProductId").val()),
            Category: $("#promotionCategory").val()
        };

        console.log(promotion);


        // Disable button to prevent multiple submissions
        $("#savePromotionBtn").prop("disabled", true);

        // Save the promotion via AJAX
        $.ajax({
            url: '/AdminProduct/AddPromotion',
            type: 'POST',
            data: promotion,
            success: function (response) {
                if (response.success) {
                    alert("Promotion added successfully!");
                    // Close the modal
                    var promotionModal = bootstrap.Modal.getInstance(document.getElementById('promotionModal'));
                    // Check the promotion checkbox after success
                    $(".promotion-checkbox[data-id='" + promotion.ProductId + "']").prop("checked", true);
                    $.get('/AdminProduct/GetCurrentPromotion', { productId: promotion.ProductId }, function (data) {
                        // Update the timer's end date
                        $('.countdown-timer').attr('data-enddate', data.endDate);
                        // Restart the countdown
                        initializeCountdown(data.endDate);
                    });
                    promotionModal.hide();
                } else {
                    alert("Failed to add promotion: " + response.message);
                }
            },
            error: function (error) {
                console.error("Error adding promotion:", error);
            },
            complete: function () {
                // Re-enable the save button
                $("#savePromotionBtn").prop("disabled", false);
            }
        });
    });

    $("#promotionModal").on("hidden.bs.modal", function () {
        $(this).find("form")[0].reset();
      
    });

});