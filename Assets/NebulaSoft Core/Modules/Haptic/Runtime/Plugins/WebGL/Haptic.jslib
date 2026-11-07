var HapticPlugin = {
	patterns: {},

    _Initialize: function() {
		this.patterns = {};
    },
	
    _Play: function(duration) {
        if (navigator.vibrate) {
            navigator.vibrate(duration);
        }
    },

    _RegisterPattern: function(idPtr, idLen, ptr, length) {
        var id = UTF8ToString(idPtr, idLen);
		
		var pattern = [];
		for (var i = 0; i < length; i++) {
			pattern.push(HEAP32[(ptr >> 2) + i]);
		}
		
		this.patterns[id] = {};
		this.patterns[id] = pattern;
    },

    _PlayPattern: function(idPtr, idLen) {
        var id = UTF8ToString(idPtr, idLen);
		
		var pattern = this.patterns[id];
		if (pattern) {
			if (navigator.vibrate) {
				navigator.vibrate(pattern);
			}
		} else {
			console.error("Haptic pattern with ID '" + id + "' can't be found!");
		}
    }
};

mergeInto(LibraryManager.library, HapticPlugin);