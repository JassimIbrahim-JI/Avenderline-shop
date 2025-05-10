class PaymentManager {
    static init() {
        this.bindEvents();
        this.setupAjax();
        this.initStripe();
    }

    static bindEvents() {
        console.log("Binding events...");  
   }

    static setupAjax() {
        console.log("Setting up AJAX...");
        $.ajaxSetup({
            beforeSend: function (xhr) {
                const token = $('meta[name="csrf-token"]').attr('content');
                if (token) xhr.setRequestHeader("RequestVerificationToken", token);
            },
            xhrFields: {
                withCredentials: true
            }
        });
    }

    static initStripe() {
        const publishableKey = document.getElementById('stripe-publishable-key').value;
        const clientSecret = document.getElementById('stripe-client-secret').value;
        const orderId = document.getElementById('order-id').value;

        const stripe = Stripe(publishableKey);
        const elements = stripe.elements();
        const card = elements.create('card');
        card.mount('#card-element');

        document.getElementById('submit').addEventListener('click', function (event) {
            event.preventDefault();

            stripe.confirmCardPayment(clientSecret, {
                payment_method: {
                    card: card,
                }
            }).then(function (result) {
                if (result.error) {
                    console.error(result.error.message);
                } else {
                    window.location.href = '/Order/Confirmation?orderId=' + orderId;
                }
            });
        });
    }
}

document.addEventListener('DOMContentLoaded', function () {
    PaymentManager.init();
});
