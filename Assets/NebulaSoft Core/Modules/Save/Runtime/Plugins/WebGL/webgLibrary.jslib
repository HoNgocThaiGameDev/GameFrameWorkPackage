mergeInto(LibraryManager.library, {
    prefix: "",
    
    init: function(gamePrefix) {
        this.prefix = UTF8ToString(gamePrefix);
    },

    load: function(keyName) {
        var keyString = UTF8ToString(keyName);
        var prefixedKey = this.prefix + "_" + keyString;
        var storedString = "";

        if (localStorage.getItem(prefixedKey) !== null) {
            storedString = localStorage.getItem(prefixedKey);

            var bufferSize = lengthBytesUTF8(storedString) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(storedString, buffer, bufferSize);

            return buffer;
        }

        return null;
    },

    save: function(keyName, data) {
        var prefixedKey = this.prefix + "_" + UTF8ToString(keyName);
        localStorage.setItem(prefixedKey, UTF8ToString(data));
    },

    deleteItem: function(keyName) {
        var prefixedKey = this.prefix + "_" + UTF8ToString(keyName);
        localStorage.removeItem(prefixedKey);
    }
});