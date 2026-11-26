(function () {
    "use strict";
    const link = document.querySelector("[data-add-menu]");
    const dialog = document.querySelector("[data-menu-dialog]");
    if (!link || !dialog) return;
    const content = dialog.querySelector("[data-menu-dialog-content]");
    const error = dialog.querySelector("[data-menu-dialog-error]");
    let busy = false;

    async function loadForm(url, options) {
        if (busy) return;
        busy = true;
        error.hidden = true;
        link.setAttribute("aria-disabled", "true");
        const submit = content.querySelector('button:not([type]), button[type="submit"]');
        if (submit) submit.disabled = true;
        try {
            const response = await fetch(url, {
                ...options,
                headers: { "X-Menu-Dialog": "1" }
            });
            if (response.redirected) {
                window.location.assign(response.url);
                return;
            }
            const page = new DOMParser().parseFromString(await response.text(), "text/html");
            const form = page.querySelector("form.admin-form");
            if (!response.ok || !form) throw new Error("Menu form unavailable");
            content.replaceChildren(form);
            form.querySelector("input:not([type=hidden])")?.focus();
        } catch {
            error.textContent = "Chưa thể hoàn tất. Vui lòng thử lại.";
            error.hidden = false;
        } finally {
            busy = false;
            link.removeAttribute("aria-disabled");
            if (submit) submit.disabled = false;
        }
    }

    link.addEventListener("click", event => {
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        if (!dialog.open) dialog.showModal();
        loadForm(link.href);
    });
    dialog.addEventListener("click", event => {
        if (event.target.closest("[data-close-menu-dialog]") && !busy) dialog.close();
    });
    dialog.addEventListener("cancel", event => {
        if (busy) event.preventDefault();
    });
    dialog.addEventListener("submit", event => {
        const form = event.target;
        event.preventDefault();
        loadForm(form.action, { method: "POST", body: new FormData(form) });
    });
}());

(function () {
    "use strict";
    const stockFilter = document.getElementById("stock-filter");
    const stockRows = Array.from(document.querySelectorAll("[data-stock-status]"));
    const emptyStock = document.querySelector("[data-stock-empty]");

    function filterStock() {
        for (const row of stockRows) {
            row.hidden = stockFilter.value !== "all" && row.dataset.stockStatus !== stockFilter.value;
        }
        emptyStock.hidden = stockRows.some(row => !row.hidden);
    }

    stockFilter.addEventListener("change", filterStock);
    filterStock();

    const adminMobile = window.matchMedia("(max-width: 820px)");
    const actionMenus = document.querySelectorAll(".admin-menu-actions");

    function updateActionMenus() {
        for (const menu of actionMenus) menu.open = !adminMobile.matches;
    }

    adminMobile.addEventListener("change", updateActionMenus);
    updateActionMenus();
}());
