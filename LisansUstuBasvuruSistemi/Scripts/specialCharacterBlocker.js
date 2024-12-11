class SpecialCharacterBlocker {
    constructor(className, invalidChars = [], maxLength = null) {
        this.className = className;
        this.invalidChars = invalidChars;
        this.maxLength = maxLength;
        this.specialCharacters = SpecialCharacter.getAllSpecialCharacters();
        this.init();
    }

    init() {
        const inputs = document.querySelectorAll(`.${this.className}`);

        inputs.forEach(input => {
            input.addEventListener('keypress', (e) => this.handleKeyPress(e, input));
            input.addEventListener('paste', (e) => this.handlePaste(e, input));
        });
    }

    handleKeyPress(event, input) {
        const char = String.fromCharCode(event.which || event.keyCode);

        // Geçersiz karakter kontrolü
        if (this.invalidChars.includes(char)) {
            const invalidCharName = this.getCharacterNameByChar(char); // Karakter adı
            event.preventDefault();
            alert(`Bu alanda ${char} ${invalidCharName}  karakterini kullanamazsınız.\r\nYasaklı karakterler:\r\n${this.invalidChars.join("  ")}`);
            return;
        }

        // Maxlength kontrolü
        if (this.maxLength !== null && input.value.length >= this.maxLength) {
            event.preventDefault();
            alert(`Bu alana en fazla ${this.maxLength} karakter girebilirsiniz.`);
        }
    }

    handlePaste(event, input) {
        const pasteText = (event.clipboardData || window.clipboardData).getData('text');
        const usedInvalidChars = this.getUsedInvalidChars(pasteText);

        if (usedInvalidChars.length > 0) {
            const invalidCharNames = usedInvalidChars.map(char => this.getCharacterNameByChar(char)); // Karakter adlarını al
            event.preventDefault();
            alert(`Bu alanda ${invalidCharNames.join(", ")} ${usedInvalidChars.join(", ")} karakterlerini kullanamazsınız.\r\nYasaklı karakterler:\r\n${this.invalidChars.join("  ")}`);
            return;
        }
        if (this.maxLength !== null && (input.value.length + pasteText.length) > this.maxLength) {
            event.preventDefault();
            alert(`Bu alana en fazla ${this.maxLength} karakter girebilirsiniz.`);
        }
    }

    getUsedInvalidChars(text) {
        return this.invalidChars.filter(char => text.includes(char));
    }

    getCharacterNameByChar(char) {
        const character = this.specialCharacters.find(character => character.char === char);
        return character ? character.name : 'Bilinmeyen karakter';
    }

    getInvalidSpecialCharacterNames() {
        return this.specialCharacters.filter(character => this.invalidChars.includes(character.char))
            .map(character => character.name).join(", ");
    }
}

class SpecialCharacter {
    constructor(char, name) {
        this.char = char;
        this.name = name;
    }

    static getAllSpecialCharacters() {
        return [
            // En sık kullanılan noktalama işaretleri
            new SpecialCharacter('.', 'Nokta'),
            new SpecialCharacter(',', 'Virgül'),
            new SpecialCharacter('!', 'Ünlem işareti'),
            new SpecialCharacter('?', 'Soru işareti'),
            new SpecialCharacter('"', 'Çift tırnak işareti'),
            new SpecialCharacter("'", 'Tek tırnak işareti'),
            new SpecialCharacter('‘', 'Açılış tırnak işareti'),
            new SpecialCharacter('’', 'Kapanış tırnak işareti'),
            new SpecialCharacter(':', 'İki nokta'),
            new SpecialCharacter(';', 'Noktalı virgül'),
            new SpecialCharacter('-', 'Eksi işareti'),

            // Sık kullanılan matematiksel işaretler
            new SpecialCharacter('+', 'Artı işareti'),
            new SpecialCharacter('=', 'Eşittir işareti'),
            new SpecialCharacter('×', 'Çarpı işareti'),
            new SpecialCharacter('÷', 'Bölü işareti'),

            // Programlama ve web'de sık kullanılan karakterler
            new SpecialCharacter('@', 'Kuyruklu a işareti'),
            new SpecialCharacter('#', 'Diyez işareti'),
            new SpecialCharacter('/', 'Eğik çizgi'),
            new SpecialCharacter('\\', 'Ters eğik çizgi'),
            new SpecialCharacter('_', 'Alt çizgi'),

            // Parantezler ve gruplandırma işaretleri
            new SpecialCharacter('(', 'Sol parantez'),
            new SpecialCharacter(')', 'Sağ parantez'),
            new SpecialCharacter('[', 'Sol köşeli parantez'),
            new SpecialCharacter(']', 'Sağ köşeli parantez'),
            new SpecialCharacter('{', 'Sol süslü parantez'),
            new SpecialCharacter('}', 'Sağ süslü parantez'),

            // Para birimleri
            new SpecialCharacter('$', 'Dolar işareti'),
            new SpecialCharacter('€', 'Euro işareti'),
            new SpecialCharacter('£', 'Sterlin işareti'),

            // Yaygın semboller
            new SpecialCharacter('%', 'Yüzde işareti'),
            new SpecialCharacter('&', 'Ve işareti (ampersand)'),
            new SpecialCharacter('*', 'Yıldız işareti'),
            new SpecialCharacter('•', 'Madde imi'),

            // Karşılaştırma işaretleri
            new SpecialCharacter('<', 'Küçüktür işareti'),
            new SpecialCharacter('>', 'Büyüktür işareti'),
            new SpecialCharacter('≠', 'Eşit değildir işareti'),

            // Ok işaretleri
            new SpecialCharacter('→', 'Sağ ok işareti'),
            new SpecialCharacter('←', 'Sol ok işareti'),
            new SpecialCharacter('↑', 'Yukarı ok işareti'),
            new SpecialCharacter('↓', 'Aşağı ok işareti'),

            // Tescil ve telif işaretleri
            new SpecialCharacter('©', 'Telif hakkı işareti'),
            new SpecialCharacter('®', 'Tescilli marka işareti'),
            new SpecialCharacter('™', 'Ticari marka işareti'),

            // Diğer matematiksel ve bilimsel işaretler
            new SpecialCharacter('±', 'Artı eksi işareti'),
            new SpecialCharacter('∞', 'Sonsuzluk işareti'),
            new SpecialCharacter('°', 'Derece işareti'),
            new SpecialCharacter('µ', 'Mikro işareti'),

            // Daha az kullanılan para birimleri
            new SpecialCharacter('¥', 'Yen işareti'),
            new SpecialCharacter('¢', 'Sent işareti'),

            // Özel semboller ve işaretler
            new SpecialCharacter('♥', 'Kalp işareti'),
            new SpecialCharacter('♦', 'Karo işareti'),
            new SpecialCharacter('♪', 'Nota işareti'),
            new SpecialCharacter('☆', 'Boş yıldız işareti'),
            new SpecialCharacter('☀', 'Güneş işareti'),

            // En az kullanılan özel karakterler
            new SpecialCharacter('¶', 'Paragraf işareti'),
            new SpecialCharacter('§', 'Paragraf işareti'),
            new SpecialCharacter('¬', 'Değil işareti'),
            new SpecialCharacter('`', 'Kesme işareti'),
            new SpecialCharacter('|', 'Dikey çizgi'),
            new SpecialCharacter('^', 'Şapka işareti'),
            new SpecialCharacter('~', 'Tilde işareti')
        ]
    }
}
