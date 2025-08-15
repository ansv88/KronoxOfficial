// Funktion för att synkronisera alla TinyMCE-editorer
window.syncAllEditors = function () {
    if (typeof tinymce !== 'undefined') {
        tinymce.triggerSave();
        console.log("Synkroniserade alla TinyMCE-editorer");
        return true;
    }
    console.warn("TinyMCE är inte tillgänglig");
    return false;
};

window.elementExists = function (elementId) {
    const element = document.getElementById(elementId);
    return element !== null;
};

// Hantera varning för osparade ändringar
window.setUnsavedChangesWarning = function (hasUnsavedChanges) {
    if (hasUnsavedChanges) {
        window.onbeforeunload = function () {
            return "Du har osparade ändringar. Är du säker på att du vill lämna sidan?";
        };
    } else {
        window.onbeforeunload = null;
    }
};

// Initialisera karusell för förhandsvisning
window.initPreviewCarousel = function () {
    var myCarousel = document.getElementById('previewCarousel');
    if (myCarousel) {
        var carousel = new bootstrap.Carousel(myCarousel, {
            interval: 5000
        });
        console.log("Karusell initierad");
    } else {
        console.warn("Karusellelement hittades inte");
    }
};

// Kopiering till urklipp
window.copyToClipboard = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        console.log("Kopierat till urklipp:", text.substring(0, 30) + (text.length > 30 ? "..." : ""));
        return true;
    } catch (err) {
        console.error("Kunde inte kopiera till urklipp:", err);
        return false;
    }
};

// Explicit confirm-metod för att säkerställa att den är tillgänglig
window.showConfirmDialog = function (message) {
    console.log("Visar bekräftelsedialog:", message);
    return confirm(message);
};

// Alert-metod som är säkrare att anropa från Blazor
window.showAlert = function (message) {
    console.log("Visar alert:", message);
    alert(message);
    return true;
};

// Initialisera "Tillbaka till toppen"-knapp
window.initScrollTopButton = function () {
    const scrollBtn = document.getElementById('scrollTopBtn');
    if (!scrollBtn) {
        console.error("scrollTopBtn element not found!");
        return;
    }

    window.addEventListener('scroll', function () {
        const scrollPosition = window.scrollY || document.documentElement.scrollTop;

        if (scrollPosition > 300) {
            scrollBtn.classList.remove('d-none');
        } else {
            scrollBtn.classList.add('d-none');
        }
    });

    // Trigga manuellt för att kontrollera initial status
    const initialScroll = window.scrollY || document.documentElement.scrollTop;
    if (initialScroll > 300) {
        scrollBtn.classList.remove('d-none');
    }
};

// Initialisera karusell för medlemslogotyper på startsidan
window.initCarousel = function () {
    const desktopCarousel = document.getElementById('desktopCarousel');
    if (desktopCarousel) {
        const carousel1 = new bootstrap.Carousel(desktopCarousel, {
            interval: 3000,
            wrap: true,
            pause: 'hover'
        });
    }

    // Initialisera mobilkarusell
    const mobileCarousel = document.getElementById('mobileCarousel');
    if (mobileCarousel) {
        const carousel2 = new bootstrap.Carousel(mobileCarousel, {
            interval: 3000,
            wrap: true,
            pause: 'hover'
        });
    }
};

// Toast-meddelanden
window.toast = {
    success: function (message) {
        showToast(message, 'success', 'Klart!');
    },
    error: function (message) {
        showToast(message, 'danger', 'Fel');
    },
    info: function (message) {
        showToast(message, 'info', 'Information');
    },
    warning: function (message) {
        showToast(message, 'warning', 'Varning');
    }
};

// Hjälpfunktion för att skapa och visa toast
function showToast(message, type, title) {
    console.log(`Toast [${type}]:`, message);

    // Skapa ett unikt ID för toasten
    const id = 'toast-' + Date.now();

    // Definiera CSS-klasser baserat på typ
    const bgClass = type ? `bg-${type}` : 'bg-light';
    const textClass = (type === 'warning' || type === 'light') ? 'text-dark' : 'text-white';

    // Skapa toast HTML
    const toast = `
        <div id="${id}" class="toast ${bgClass} ${textClass}" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header">
                <strong class="me-auto">${title}</strong>
                <small>Just nu</small>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Stäng"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;

    // Lägg till i containern
    const container = document.getElementById('toast-container');
    if (container) {
        container.insertAdjacentHTML('beforeend', toast);

        // Hämta det nya toast-elementet och visa det
        const toastElement = document.getElementById(id);
        const bsToast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: 5000
        });

        bsToast.show();

        // Ta bort toast-elementet från DOM när det är dolt
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    } else {
        // Fallback om containern inte finns
        alert(`${title}: ${message}`);
    }
}


// Lyssna på DOMContentLoaded för att initialisera UI-komponenter
document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM helt laddat, initierar UI-komponenter');

    // Initialisera tillbaka-till-toppen knapp om den finns
    window.initScrollTopButton();

    // Initialisera eventuell medlemskarusell på startsidan
    window.initCarousel();

    console.log('Main.js initiering klar, alla JavaScript-metoder är registrerade');
});


window.initPreviewForHomeAdmin = function () {
    console.log("Initierar HomeAdmin-förhandsgranskning");

    // Ge lite tid för DOM att uppdateras
    setTimeout(function () {
        const previewCarousel = document.getElementById('previewCarousel');
        if (previewCarousel) {
            console.log("Hittade previewCarousel, initierar Bootstrap-karusell");
            try {
                const carousel = new bootstrap.Carousel(previewCarousel, {
                    interval: 5000
                });
                console.log("Karusell initierad");
            } catch (e) {
                console.error("Fel vid initiering av karusell:", e);
            }
        } else {
            console.warn("Hittade inte previewCarousel-element");
        }
    }, 200);
};

// Återanslutningskod för Blazor Server
window.handleBlazorDisconnect = function () {
    const maxRetryCount = 3;
    let currentRetryCount = 0;

    const reconnectInterval = setInterval(() => {
        if (currentRetryCount >= maxRetryCount) {
            clearInterval(reconnectInterval);
            console.log("Kunde inte återansluta efter flera försök. Laddar om sidan...");
            location.reload();
            return;
        }

        currentRetryCount++;
        console.log(`Försök ${currentRetryCount}/${maxRetryCount} att återansluta...`);

        // Försök att pinga servern
        fetch('api/health', {
            method: 'GET',
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache',
                'Expires': '0'
            }
        })
            .then(response => {
                if (response.ok) {
                    console.log("API svarar, laddar om sidan...");
                    location.reload();
                    clearInterval(reconnectInterval);
                } else {
                    console.log(`API svarade med statuskod ${response.status}, fortsätter försöka...`);
                }
            })
            .catch(error => {
                console.error("Kunde inte nå API:", error);
            });
    }, 3000);
};

// Observera Blazor-anslutningsstatus
window.addEventListener('DOMContentLoaded', () => {
    const observer = new MutationObserver(mutations => {
        mutations.forEach(mutation => {
            if (mutation.target.classList.contains('components-reconnect-show') ||
                mutation.target.classList.contains('components-reconnect-failed')) {
                window.handleBlazorDisconnect();
            }
        });
    });

    const reconnectModal = document.getElementById('components-reconnect-modal');
    if (reconnectModal) {
        observer.observe(reconnectModal, { attributes: true, attributeFilter: ['class'] });
    }
});

window.showFeatureImage = function (imageUrl) {
    DotNet.invokeMethodAsync('KronoxFront', 'ShowFeatureImageFromJs', imageUrl);
};

// Initialisera klick på funktionsbilder
window.initFeatureImageClicks = function () {
    document.querySelectorAll('.feature-thumb').forEach(img => {
        img.addEventListener('click', function () {
            const imageUrl = this.getAttribute('src');
            if (imageUrl) {
                window.showFeatureImage(imageUrl);
            }
        });
    });
};

// Funktion för att ladda ner fil
window.downloadFileFromStream = async function (streamRef, fileName) {
    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();

    // Rensa upp
    setTimeout(() => {
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }, 100);
};

// Filhantering för uppladdning av feature-bilder
window.getFileAsBase64 = function (inputId) {
    return new Promise((resolve) => {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) {
            resolve(null);
            return;
        }

        const file = input.files[0];
        const reader = new FileReader();

        reader.onload = function (e) {
            resolve(e.target.result);
        };

        reader.readAsDataURL(file);
    });
};

window.clickElement = function (id) {
    const element = document.getElementById(id);
    if (element) element.click();
};

window.getSelectedFileInfo = function (id) {
    const input = document.getElementById(id);
    if (input && input.files && input.files.length > 0) {
        const file = input.files[0];
        return {
            name: file.name,
            size: file.size,
            type: file.type
        };
    }
    return null;
};

// Hantera kretsåteranslutning
window.handleCircuitReconnect = function () {
    try {
        // Lagra senast aktiva sida före uppdatering
        sessionStorage.setItem('lastPage', window.location.href);

        // Kontrollera säkert om Blazor reconnectionHandler finns innan den används
        if (typeof Blazor !== 'undefined' && Blazor && Blazor.reconnectionHandler) {
            Blazor.reconnectionHandler.onConnectionUp = function () {
                window.location.reload();
            };
        }
    } catch (e) {
        console.error("Error setting up circuit reconnect handler:", e);
    }
};

// Initialisera återanslutningshanterare först efter att Blazor är fullständigt laddad
document.addEventListener('DOMContentLoaded', function () {
    if (typeof Blazor !== 'undefined') {
        window.handleCircuitReconnect();
    } else {
        // Om Blazor inte är tillgänglig än, vänta på den
        window.addEventListener('blazorStarted', window.handleCircuitReconnect);
    }
});

window.logFormData = function (data) {
    console.log("Form data:", data);
    return true;
};

// Event-lyssnare för editorer som läggs till
window.addEventListener('DOMContentLoaded', function () {
    if (typeof tinymce !== 'undefined') {
        tinymce.on('AddEditor', function (e) {
            console.log('TinyMCE editor added:', e.editor.id);
        });

        tinymce.on('RemoveEditor', function (e) {
            console.log('TinyMCE editor removed:', e.editor.id);
        });
    }
});