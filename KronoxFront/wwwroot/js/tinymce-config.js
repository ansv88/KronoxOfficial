if (!window.setCurrentPageKey) {
    window.setCurrentPageKey = function (key) {
        window.currentPageKey = key || 'home';
    };
}
if (!window.clearCurrentPageKey) {
    window.clearCurrentPageKey = function () {
        window.currentPageKey = null;
    };
}

// Globala TinyMCE-inställningar (grunddata)
window.tinymceConfig = {
    baseUrl: "/lib/tinymce",
    suffix: "",
    licenseKey: "gpl"
};

// Global konfig för alla editorer
window.tinymceConfig = {
    height: 300,
    menubar: false,
    plugins: [
        'advlist', 'autolink', 'lists', 'link', 'charmap', 'preview',
        'anchor', 'searchreplace', 'visualblocks', 'code',
        'insertdatetime', 'table', 'help', 'wordcount',
        'image'
    ],
    toolbar:
        'undo redo | blocks | ' +
        'bold italic forecolor | alignleft aligncenter ' +
        'alignright alignjustify | bullist numlist outdent indent | ' +
        'removeformat | link image | code preview help',
    content_style:
        "body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; font-size:16px }",
    convert_urls: false,
    relative_urls: false,
    license_key: 'gpl',

    base_url: '/lib/tinymce',
    suffix: '.min',

    paste_data_images: false,
    automatic_uploads: true,

    // Drag & drop / paste -> ladda upp till ContentController
    images_upload_handler: async function (blobInfo, progress) {
        const pageKey = (window.currentPageKey || 'home');
        const form = new FormData();
        form.append('file', blobInfo.blob(), blobInfo.filename());
        form.append('altText', blobInfo.filename()); // alt = filnamn (kan ändras senare i editor)

        const res = await fetch(`/api/content/${pageKey}/images`, {
            method: 'POST',
            body: form,
            credentials: 'include'
        });
        if (!res.ok) {
            const t = await res.text();
            throw new Error(`Upload failed: ${res.status} ${t}`);
        }
        const json = await res.json();
        return json.url || json.Url || '';
    },

    // Bildknapp -> fråga om alt-text, ladda upp, infoga URL
    file_picker_types: 'image',
    file_picker_callback: async function (cb, value, meta) {
        if (meta.filetype !== 'image') return;

        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';

        input.onchange = async function () {
            const file = this.files && this.files[0];
            if (!file) return;

            // Be användaren om alt-text (default: filnamn)
            let alt = prompt('Alt-text för bilden?', file.name);
            if (!alt || !alt.trim()) alt = file.name;

            const pageKey = (window.currentPageKey || 'home');
            const form = new FormData();
            form.append('file', file, file.name);
            form.append('altText', alt);

            const res = await fetch(`/api/content/${pageKey}/images`, {
                method: 'POST',
                body: form,
                credentials: 'include'
            });
            if (!res.ok) {
                const t = await res.text();
                alert('Bilduppladdning misslyckades.');
                console.warn('Upload failed', t);
                return;
            }

            const json = await res.json();
            const url = json.url || json.Url || '';
            cb(url, { title: file.name, alt: alt });
        };

        input.click();
    },

    setup: function (editor) {
        editor.on('change', function () {
            editor.save();
            var textarea = document.getElementById(editor.id);
            if (textarea) {
                var event = new Event('input', { bubbles: true });
                textarea.dispatchEvent(event);
            }
        });
    }
};

window.initTinyMCE = function (elementId) {
    if (typeof tinymce === 'undefined') {
        console.error('tinymce är inte laddat!');
        return;
    }
    const existing = tinymce.get(elementId);
    if (existing) existing.destroy();

    tinymce.init(Object.assign({}, window.tinymceConfig, {
        selector: `#${elementId}`,
        setup: function (editor) {
            editor.on('change', function () {
                editor.save();
                var textarea = document.getElementById(editor.id);
                if (textarea) {
                    var event = new Event('input', { bubbles: true });
                    textarea.dispatchEvent(event);
                }
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
    tinymce.editors.forEach(function (editor) { editor.save(); });
    return true;
};

window.tinymceExists = function (editorId) {
    if (typeof tinymce === 'undefined') return false;
    return tinymce.get(editorId) !== null;
};

window.isTinyMCELoaded = function () {
    return (typeof tinymce !== 'undefined') && tinymce.editors && tinymce.editors.length > 0;
};