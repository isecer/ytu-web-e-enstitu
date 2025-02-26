(function () {
    function convertToTrOrEnUpperCase(event) {
        const inputElement = event.target;
        var charMap = {
            'i': 'İ',
            'ı': 'I',
            'ğ': 'Ğ',
            'ü': 'Ü',
            'ş': 'Ş',
            'ö': 'Ö',
            'ç': 'Ç',
            'İ': 'İ',
            'Ğ': 'Ğ',
            'Ü': 'Ü',
            'Ş': 'Ş',
            'Ö': 'Ö',
            'Ç': 'Ç'
        };
        if ($(inputElement).hasClass('UpperTextEn')) {
            charMap = {
                'ı': 'I',
                //'ğ': 'G',
                //'ü': 'U',
                //'ş': 'S',
                //'ö': 'O',
                //'ç': 'C',
                //'İ': 'I',
                //'Ğ': 'G',
                //'Ü': 'U',
                //'Ş': 'S',
                //'Ö': 'O',
                //'Ç': 'C'
            };
        }
        const start = inputElement.selectionStart;
        const end = inputElement.selectionEnd;

        const resultString = inputElement.value.split('').map((char, index) => {
            if (charMap[char]) {
                return charMap[char];
            }
            return char.toUpperCase();
        }).join('');
        inputElement.value = resultString;
        // Değiştirilen harfin başlangıç ve bitiş indekslerini kullanarak imleci ayarla
        inputElement.setSelectionRange(start, end);
    }
    const inputElementsTr = document.querySelectorAll('.UpperTextTr,.UpperTextEn'); 
    inputElementsTr.forEach(inputElement => {
        inputElement.addEventListener('input', convertToTrOrEnUpperCase);
        convertToTrOrEnUpperCase({ target: inputElement });
    });

})();