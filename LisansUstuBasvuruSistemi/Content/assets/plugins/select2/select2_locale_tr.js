/**
 * Select2 Turkish translation.
 * 
 * Author: Salim KAYABAŞI <salim.kayabasi@gmail.com>
 */
(function ($) {
    "use strict";

    // Select2'nin yüklenip yüklenmediğini kontrol edelim
    if ($.fn.select2) {
        $.fn.select2.defaults.language = {
            noResults: function () {
                return "Sonuç bulunamadı";
            },
            inputTooShort: function (args) {
                var n = args.minimum - args.input.length;
                return "En az " + n + " karakter daha girmelisiniz";
            },
            inputTooLong: function (args) {
                var n = args.input.length - args.maximum;
                return n + " karakter azaltmalısınız";
            },
            maximumSelected: function (args) {
                return "Sadece " + args.maximum + " seçim yapabilirsiniz";
            },
            loadingMore: function () {
                return "Daha fazla...";
            },
            searching: function () {
                return "Aranıyor...";
            }
        };
    } else {
        console.log('Select2 kütüphanesi yüklenmemiş!');
    }
})(jQuery);
