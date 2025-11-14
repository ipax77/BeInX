let dsmodal = null;
function openModalById(id) {
    const modalElement = document.getElementById(id);

    if (!modalElement) {
        return;
    }

    dsmodal = new bootstrap.Modal(modalElement);
    dsmodal.show();
}

function closeModalById(id) {
    if (dsmodal !== undefined && dsmodal !== null) {
        dsmodal.hide();
        dsmodal = null;
    } else {
        const modalEl = document.getElementById(id);
        const myModal = bootstrap.Modal.getInstance(modalEl);
        if (myModal !== undefined && myModal != null) {
            myModal.hide();
            modalEl.addEventListener('hidden.bs.modal', () => {
                modal.dispose();
            }, { once: true });
        }
    }
}

function createPdfBlobUrl(base64) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }

    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: 'application/pdf' });

    return URL.createObjectURL(blob);
}

function showToast(id) {
    const toastEle = document.getElementById(id);
    if (!toastEle) {
        return;
    }
    const toastBootstrap = bootstrap.Toast.getOrCreateInstance(toastEle);
    toastBootstrap.show();
}

async function downloadFileFromStream(fileName, contentStreamReference, mimeType = 'application/octet-stream') {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

function enableTooltips(elementId) {
    const container = document.getElementById(elementId);
    if (!container) {
        return;
    }

    const oldTooltips = container.querySelectorAll('[data-bs-toggle="tooltip"]');
    oldTooltips.forEach(el => {
        const tooltipInstance = bootstrap.Tooltip.getInstance(el);
        if (tooltipInstance) {
            tooltipInstance.dispose();
        }
    });

    const tooltipTriggerList = container.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl =>
        new bootstrap.Tooltip(tooltipTriggerEl)
    );
}

function disableTooltips(elementId) {
    const container = document.getElementById(elementId);
    if (!container) {
        return;
    }

    const tooltipTriggerList = container.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl =>
        new bootstrap.Tooltip(tooltipTriggerEl)
    );
    tooltipList.forEach(tooltip => {
        tooltip.dispose();
    });
    cleanupOrphanedTooltips();
}

function cleanupOrphanedTooltips() {
    document.querySelectorAll('.tooltip').forEach(tooltip => {
        if (!document.body.contains(tooltip.relatedTarget) && !tooltip.getAttribute('data-bs-popper')) {
            tooltip.remove();
        }
    });

    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
        const instance = bootstrap.Tooltip.getInstance(el);
        if (instance && !document.body.contains(el)) {
            instance.dispose();
        }
    });
}

function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
}