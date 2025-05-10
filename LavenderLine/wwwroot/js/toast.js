// toast.jquery.js
(function ($) {
    'use strict';

    const iconPaths = {
        success: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z',
        danger: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z',
        warning: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z',
        info: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z',

        'cart-add': 'M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49c.08-.14.12-.31.12-.48 0-.55-.45-1-1-1H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z',

        'cart-remove': 'M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49c.08-.14.12-.31.12-.48 0-.55-.45-1-1-1H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2zm-4-9h4v2h-4V7z',

        'wishlist-add': 'M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z',

        'wishlist-remove': 'M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35zm4-14.35h-4v2h4V7z',

        login: 'M12 15V17M12 7V13M12 21C7.029 21 3 16.971 3 12 3 7.029 7.029 3 12 3c4.97 0 9 4.029 9 9 0 4.97-4.029 9-9 9z',
        register: 'M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z',
        profile: 'M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm0 3a3 3 0 1 1 0 6 3 3 0 0 1 0-6zm0 13.2a8.2 8.2 0 0 1-6-3.98C6.03 13.99 10 12.9 12 12.9c1.99 0 5.97 1.09 6 3.98a8.2 8.2 0 0 1-6 3.32z',
        email: 'M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z',
        password: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm-2-3h4v2h-4zm0-3h4v2h-4zm0-3h4v2h-4z',
        clock: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm1-8h4v2h-4v-4h-2v4z'

    };


    var Toast = {
        queue: [],
        isShowing: false,

        show: function (type, message) {
           
            var isDuplicate = $.grep(this.queue, function (item) {
                return item.message === message;
            }).length > 0;

            if (isDuplicate) return;

            this.queue.push({ type: type, message: message });
            if (!this.isShowing) {
                this.showNext();
            }
        },

        showNext: function () {
            if (this.queue.length === 0) {
                this.isShowing = false;
                return;
            }

            this.isShowing = true;
            const nextItem = this.queue.shift();
            const type = nextItem.type;
            const message = nextItem.message;

            const $container = $('#toast-container');
            const $template = $container.find('.toast').first();

            const $toast = $template.clone()
                .removeClass('toast-success toast-danger toast-warning toast-info')
                .addClass(`toast-${type}`)
                .find('.toast-body').text(message).end()
                .find('.toast-icon').html(`<path fill="currentColor" d="${iconPaths[type]}"/>`).end();

            $toast.on('hidden.bs.toast', function () {
                $(this).remove();
                Toast.showNext();
            });

            $container.append($toast);
            $toast.toast({
                autohide: true,
                delay: 3000
            }).toast('show');
        }
    };

    window.Toast = Toast;
})(jQuery);