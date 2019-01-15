(function($, pako) {

    var encode6BitChar = function(b) {
        if (b < 10)
            return String.fromCharCode(48 + b);
        b -= 10;
        if (b < 26)
            return String.fromCharCode(65 + b);
        b -= 26;
        if (b < 26)
            return String.fromCharCode(97 + b);
        b -= 26;
        if (b === 0) {
            return "-";
        }
        if (b === 1) {
            return "_";
        }
        return "?";
    }

    var add3Chars = function(b1, b2, b3) {
        var c1 = b1 >> 2;
        var c2 = ((b1 & 0x3) << 4) | (b2 >> 4);
        var c3 = ((b2 & 0xF) << 2) | (b3 >> 6);
        var c4 = b3 & 0x3F;
        var r = "";
        r += encode6BitChar(c1 & 0x3F);
        r += encode6BitChar(c2 & 0x3F);
        r += encode6BitChar(c3 & 0x3F);
        r += encode6BitChar(c4 & 0x3F);
        return r;
    }

    var encodeUml = function(data) {
        var r = "";
        for (var i = 0; i < data.length; i += 3) {
            if (i + 2 === data.length) {
                r += add3Chars(data.charCodeAt(i), data.charCodeAt(i + 1), 0);
            } else if (i + 1 === data.length) {
                r += add3Chars(data.charCodeAt(i), 0, 0);
            } else {
                r += add3Chars(data.charCodeAt(i), data.charCodeAt(i + 1), data.charCodeAt(i + 2));
            }
        }
        return r;
    }

    var bindUml = function(node) {
        var $img = $(node);
        var umlCode = $img.attr("data-uml");
        if (umlCode != null) {
            var data = String.fromCharCode.apply(null, pako.deflateRaw(unescape(encodeURIComponent(umlCode))));
            $img.attr("src", "http://www.plantuml.com/plantuml/img/" + encodeUml(data));
        }
    };

    var observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === "childList" && mutation.addedNodes && mutation.addedNodes.length > 0) {
                for (var i = 0; i < mutation.addedNodes.length; i++) {
                    var node = mutation.addedNodes[i];
                    if (node.nodeType === Node.ELEMENT_NODE && node.tagName && node.tagName.toLowerCase() === "img") {
                        bindUml(node);
                    }
                }
            } else if (mutation.type === "attributes" && mutation.attributeName === "data-uml") {
                bindUml(mutation.target);
            }
        });
    });

    var config = {
        attributes: true,
        attributeFilter: ["data-uml"],
        childList: true,
        subtree: true
    };

    observer.observe(document.body, config);
    $("img").each(function() {
        bindUml(this);
    });

})(jQuery, pako);