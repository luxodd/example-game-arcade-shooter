mergeInto(LibraryManager.library, {
    GetParentHost: function () {
        try {
            return allocateUTF8(window.parent.location.host);
        } catch (e) {
            // fallback to referrer or hostname
            if (document.referrer) {
                const ref = new URL(document.referrer);
                return allocateUTF8(ref.host);
            } else {
                return allocateUTF8(window.location.hostname + ":8080");
            }
        }
    },

    GetWebSocketProtocol: function () {
        return allocateUTF8(window.location.protocol === "https:" ? "wss:" : "ws:");
    }
});