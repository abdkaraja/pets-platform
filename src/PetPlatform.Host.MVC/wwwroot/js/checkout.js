// checkout.js — Stripe Payment Element integration
// Flow: page loads → AJAX creates session → Stripe.js renders Payment Element → user pays → redirect to Confirmation

$(function () {
    var $payBtn = $('#pay-btn');
    var $errorDisplay = $('#error-display');
    var $paymentElement = $('#payment-element');

    // Create Stripe checkout session via AJAX
    $.ajax({
        url: '/Checkout/CreateSession',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            ShippingFullName: $('[name="shippingFullName"]').val(),
            ShippingAddress: $('[name="shippingAddress"]').val(),
            ShippingCity: $('[name="shippingCity"]').val(),
            ShippingPhone: $('[name="shippingPhone"]').val()
        }),
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.publishableKey && response.clientSecret) {
                initializeStripe(response.publishableKey, response.clientSecret, response.sessionId);
            } else {
                showError('Failed to initialize payment. Please try again.');
            }
        },
        error: function () {
            showError('Unable to connect. Please check your internet connection and try again.');
        }
    });

    function initializeStripe(publishableKey, clientSecret, sessionId) {
        var stripe = Stripe(publishableKey);
        var elements = stripe.elements({
            clientSecret: clientSecret,
            appearance: { theme: 'stripe' }
        });

        var paymentElement = elements.create('payment');
        paymentElement.mount('#payment-element');

        // Clear loading placeholder
        $paymentElement.find('.text-gray-400').remove();
        $payBtn.prop('disabled', false);

        $payBtn.on('click', function () {
            var btn = $(this);
            btn.prop('disabled', true).text('Processing...').addClass('opacity-70');
            $errorDisplay.addClass('hidden');

            stripe.confirmPayment({
                elements: elements,
                confirmParams: {
                    return_url: window.location.origin + '/Checkout/Confirmation?session_id=' + sessionId
                }
            }).then(function (result) {
                if (result.error) {
                    showError(result.error.message);
                    var totalAmount = $payBtn.data('total-amount') || '0.00';
                    btn.prop('disabled', false).text('Pay $' + totalAmount).removeClass('opacity-70');
                }
                // On success, Stripe redirects automatically
            });
        });
    }

    function showError(message) {
        $errorDisplay.text(message).removeClass('hidden');
    }
});
