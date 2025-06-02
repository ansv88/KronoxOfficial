// Globala TinyMCE-inställningar
window.tinymceConfig = {
    baseUrl: "/lib/tinymce",
    suffix: "",
    licenseKey: "gpl"
};

// Global konfigurationsobjekt
window.tinymceConfig = {
    height: 300,
    menubar: false,
    plugins: [
        'advlist', 'autolink', 'lists', 'link', 'charmap', 'preview',
        'anchor', 'searchreplace', 'visualblocks', 'code',
        'insertdatetime', 'table', 'help', 'wordcount'
    ],
    toolbar:
        'undo redo | blocks | ' +
        'bold italic forecolor | alignleft aligncenter ' +
        'alignright alignjustify | bullist numlist outdent indent | ' +
        'removeformat | link | code help',
    content_style:
        "body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI'," +
        " Roboto, Helvetica, Arial, sans-serif; font-size:16px }",
    convert_urls: false,
    relative_urls: false,
    license_key: 'gpl',
    setup: function (editor) {
        editor.on('change', function () {
            editor.save();
            var textarea = document.getElementById(editor.id);
            var event = new Event('input', { bubbles: true });
            textarea.dispatchEvent(event);
        });
    }
};

//Hjälpfunktion för att initiera en editor på ett textarea-element
window.initTinyMCE = function (elementId) {

    if (typeof tinymce === 'undefined') {
        console.error('tinymce är inte laddat!');
        return;
    }

    const existing = tinymce.get(elementId);
    if (existing) {
        existing.destroy();
    }

    tinymce.init(Object.assign({}, window.tinymceConfig, {
        selector: `#${elementId}`,
        setup: function (editor) {
            editor.on('change', function () {
                editor.save();
                var textarea = document.getElementById(editor.id);
                var event = new Event('input', { bubbles: true });
                textarea.dispatchEvent(event);
            });
        }
    }));
};

window.destroyAllEditors = function () {
    if (typeof tinymce === 'undefined') return;
    tinymce.remove();
};

window.syncAllEditors = function () {
    if (typeof tinymce === 'undefined') return false;

    tinymce.editors.forEach(function (editor) {
        editor.save();
    });

    return true;
};

window.debugTinyMCEContent = function () {
    if (typeof tinymce === 'undefined') return "TinyMCE not loaded";
    if (!tinymce.editors) return "No editors available";

    var result = {};
    try {
        tinymce.editors.forEach(function (editor) {
            if (editor) {
                result[editor.id] = {
                    content: editor.getContent(),
                    isDirty: editor.isDirty(),
                    hasFocus: editor.hasFocus()
                };
            }
        });
    } catch (e) {
        return { "error": e.message, "editors": tinymce.editors ? tinymce.editors.length : 0 };
    }
    return result;
};

window.tinymceExists = function (editorId) {
    if (typeof tinymce === 'undefined') return false;
    return tinymce.get(editorId) !== null;
};

window.isTinyMCELoaded = function () {
    return (typeof tinymce !== 'undefined') && tinymce.editors && tinymce.editors.length > 0;
};