
export function showToast(id) {
    const toastEle = document.getElementById(id);
    if (!toastEle) {
        return;
    }
    const toastBootstrap = bootstrap.Toast.getOrCreateInstance(toastEle);
    toastBootstrap.show();
}

export function hideToast(id) {
    const toastEle = document.getElementById(id);
    if (!toastEle) {
        return;
    }
    const toastBootstrap = bootstrap.Toast.getOrCreateInstance(toastEle);
    toastBootstrap.hide();
}