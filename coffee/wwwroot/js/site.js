// wwwroot/js/site.js
// Chỉ xử lý giao diện (hộp thoại chọn món, menu điện thoại, toast).
// Toàn bộ nghiệp vụ (giỏ hàng, đặt hàng) chạy ở phía máy chủ qua các Controller.

(function () {
    "use strict";

    const PLACEHOLDER = "/images/menu/placeholder.svg";
    const formatCurrency = (n) => Number(n).toLocaleString("vi-VN") + "đ";

    // ---------- Toast: tự ẩn sau 3 giây ----------
    document.querySelectorAll("#toast-container .toast").forEach((toast) => {
        setTimeout(() => {
            toast.classList.remove("toast-show");
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    });

    // ---------- Menu trên điện thoại ----------
    const toggle = document.getElementById("mobile-menu-toggle");
    const nav = document.querySelector(".main-nav");
    if (toggle && nav) {
        toggle.addEventListener("click", () => nav.classList.toggle("nav-open"));
    }

    // ---------- Xác nhận trước khi hủy đơn ----------
    document.querySelectorAll("[data-confirm]").forEach((btn) => {
        btn.addEventListener("click", (e) => {
            if (!window.confirm(btn.dataset.confirm)) e.preventDefault();
        });
    });

    document.addEventListener("click", (event) => {
        const button = event.target.closest("[data-password-toggle]");
        if (!button) return;

        const input = document.getElementById(button.getAttribute("aria-controls"));
        if (!input) return;

        const visible = input.type === "password";
        input.type = visible ? "text" : "password";
        button.textContent = visible ? "Ẩn" : "Hiện";
        button.setAttribute("aria-label", visible ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        button.setAttribute("aria-pressed", String(visible));
    });

    const loginDialog = document.getElementById("login-dialog");
    if (loginDialog && typeof loginDialog.showModal === "function") {
        const standalone = loginDialog.hasAttribute("open");
        const title = loginDialog.querySelector("#login-dialog-title");
        const error = loginDialog.querySelector(".login-dialog-error");
        let changing = false;

        if (standalone) {
            loginDialog.removeAttribute("open");
            loginDialog.showModal();
            loginDialog.addEventListener("close", () => {
                window.location.assign(loginDialog.dataset.loginBack);
            });
        }

        document.querySelectorAll("[data-open-login]").forEach((link) => {
            link.addEventListener("click", (event) => {
                if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
                event.preventDefault();
                if (!loginDialog.open) loginDialog.showModal();
            });
        });

        loginDialog.querySelector("[data-close-login]")?.addEventListener("click", () => {
            loginDialog.close();
        });

        async function loadAccountContent(url, options, focusPassword = false) {
            if (changing) return;
            changing = true;
            error.hidden = true;
            try {
                const response = await fetch(url, {
                    ...options,
                    headers: { "X-Account-Dialog": "1" }
                });
                if (response.redirected) {
                    window.location.assign(response.url);
                    return;
                }

                const result = new DOMParser().parseFromString(await response.text(), "text/html");
                const content = result.querySelector("[data-account-content]");
                if (!response.ok || !content) throw new Error("Account request failed");
                loginDialog.querySelector("[data-account-content]").replaceWith(content);
                title.textContent = content.dataset.title;
                const focus = focusPassword ? content.querySelector('input[type="password"]') : null;
                (focus ?? content.querySelector("[autofocus]"))?.focus();
            } catch {
                error.textContent = "Chưa thể hoàn tất. Vui lòng thử lại.";
                error.hidden = false;
            } finally {
                changing = false;
            }
        }

        loginDialog.addEventListener("click", (event) => {
            const link = event.target.closest("[data-account-view]");
            if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault();
            loadAccountContent(link.href);
        });

        loginDialog.addEventListener("submit", async (event) => {
            const form = event.target;
            if (!form.matches("[data-account-form]")) return;
            event.preventDefault();
            if (changing) return;

            const button = form.querySelector('button[type="submit"], button:not([type])');
            button.disabled = true;
            await loadAccountContent(form.action, {
                method: "POST",
                body: new URLSearchParams(new FormData(form))
            }, form.querySelector("#login-password") !== null);
            button.disabled = false;
        });
    }

    const promotion = document.getElementById("promotion-dialog");
    if (promotion) {
        const form = promotion.querySelector("form");
        const discount = form.elements.discountPercent;
        const hasDiscount = form.elements.hasDiscount;
        function updateDiscountInput() {
            promotion.querySelector("[data-promotion-discount]").hidden = !hasDiscount.checked;
            discount.disabled = !hasDiscount.checked;
            discount.required = hasDiscount.checked;
        }

        document.querySelectorAll("[data-promotion]").forEach(button => {
            button.addEventListener("click", () => {
                form.elements.id.value = button.dataset.id;
                form.elements.isHot.checked = button.dataset.hot === "true";
                discount.value = button.dataset.discount || "";
                hasDiscount.checked = discount.value !== "";
                promotion.querySelector("h2").textContent = button.dataset.name;
                updateDiscountInput();
                promotion.showModal();
            });
        });
        hasDiscount.addEventListener("change", updateDiscountInput);
        promotion.querySelector("[data-promotion-close]").addEventListener("click", () => promotion.close());
    }

    const checkoutDialog = document.getElementById("checkout-dialog");
    if (checkoutDialog && typeof checkoutDialog.showModal === "function") {
        checkoutDialog.removeAttribute("open");
        checkoutDialog.showModal();
        checkoutDialog.addEventListener("close", () => window.location.assign(checkoutDialog.dataset.back));
    }

    document.querySelectorAll("[data-menu-gallery]").forEach((gallery) => {
        const mainPhoto = gallery.querySelector("[data-gallery-main]");
        const thumbnails = Array.from(gallery.querySelectorAll("[data-gallery-photo]"));
        if (thumbnails.length < 2) return;

        const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
        let selected = 0;
        let timer;

        function show(index) {
            selected = index;
            mainPhoto.src = thumbnails[index].dataset.galleryPhoto;
            if (!reducedMotion.matches) {
                mainPhoto.animate([{ opacity: .55 }, { opacity: 1 }], { duration: 260 });
            }
            thumbnails.forEach((photo, position) => {
                photo.setAttribute("aria-pressed", String(position === index));
            });
        }

        function schedule() {
            clearTimeout(timer);
            if (reducedMotion.matches || document.hidden) return;
            timer = setTimeout(() => {
                show((selected + 1) % thumbnails.length);
                schedule();
            }, 3500);
        }

        thumbnails.forEach((thumbnail, index) => {
            thumbnail.addEventListener("click", () => {
                show(index);
                schedule();
            });
        });
        document.addEventListener("visibilitychange", schedule);
        schedule();
    });

    // ---------- Hộp thoại chọn size / số lượng / ghi chú ----------
    const modal = document.getElementById("item-modal");
    if (!modal) return;

    const form = modal.querySelector("form");
    const field = (name) => form.querySelector(`[name="${name}"]`);
    const byId = (id) => document.getElementById(id);

    let quantity = 1;
    let basePrice = 0;

    function setQuantity(value) {
        quantity = Math.max(1, Math.min(99, value));
        byId("modal-qty-value").textContent = quantity;
        field("quantity").value = quantity;
    }

    function updatePrice() {
        const checked = form.querySelector('input[name="size"]:checked');
        const price = checked ? Number(checked.dataset.price) : null;
        byId("modal-price").textContent = price === null ? "Vui lòng chọn size" : formatCurrency(price);
    }

    function openModal(button) {
        const isBuyNow = button.dataset.mode === "buyNow";
        const sizes = JSON.parse(button.dataset.sizes || "[]");
        if (sizes.length === 0) sizes.push({ label: "M", price: Number(button.dataset.price) });

        field("itemId").value = button.dataset.id;
        field("buyNow").value = isBuyNow ? "true" : "false";
        field("note").value = "";
        basePrice = Number(button.dataset.price);

        byId("modal-title").textContent = isBuyNow ? "Mua ngay" : "Thêm vào giỏ hàng";
        byId("modal-confirm").textContent = isBuyNow ? "Tiếp tục" : "Thêm vào giỏ";
        byId("modal-name").textContent = button.dataset.name;

        const img = byId("modal-image");
        img.onerror = () => { img.onerror = null; img.src = PLACEHOLDER; };
        img.src = button.dataset.image || PLACEHOLDER;
        img.alt = button.dataset.name;

        const sizeGroup = byId("modal-size-group");
        const sizeList = byId("modal-size-list");
        sizeList.innerHTML = "";

        if (sizes.length > 0) {
            sizeGroup.classList.remove("hidden");
            sizes.forEach((size, index) => {
                const label = document.createElement("label");
                label.className = "size-option";

                const input = document.createElement("input");
                input.type = "radio";
                input.name = "size";
                input.value = size.label;
                input.dataset.price = size.price;
                input.required = true;
                input.addEventListener("change", updatePrice);

                const text = document.createElement("span");
                text.textContent = `${size.label} — ${formatCurrency(size.price)}`;

                label.append(input, text);
                sizeList.appendChild(label);
            });
        } else {
            sizeGroup.classList.add("hidden");
        }

        setQuantity(1);
        updatePrice();
        modal.classList.remove("hidden");
    }

    function closeModal() {
        modal.classList.add("hidden");
    }

    document.querySelectorAll("[data-open-item]").forEach((btn) => {
        btn.addEventListener("click", () => openModal(btn));
    });

    modal.querySelectorAll("[data-qty]").forEach((btn) => {
        btn.addEventListener("click", () => setQuantity(quantity + Number(btn.dataset.qty)));
    });

    modal.querySelectorAll("[data-close-modal]").forEach((btn) => {
        btn.addEventListener("click", closeModal);
    });

    modal.addEventListener("click", (e) => {
        if (e.target === modal) closeModal();
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") closeModal();
    });
})();

document.addEventListener("change", function (event) {
    if (!event.target.matches("[data-size-price-toggle]")) return;
    const field = event.target.closest(".menu-size-price").querySelector("[data-size-price-field]");
    field.hidden = !event.target.checked;
    field.querySelector("input").disabled = !event.target.checked;
    if (event.target.checked) field.querySelector("input").focus();
});

document.addEventListener("change", function (event) {
    if (!event.target.matches("[data-order-size]")) return;
    const option = event.target.selectedOptions[0];
    event.target.form.querySelector("[data-order-size-price]").textContent =
        option.dataset.price || "Vui lòng chọn size";
});

document.addEventListener("change", event => {
    if (!event.target.matches("[data-category-select]")) return;
    const field = event.target.form.querySelector("[data-new-category]");
    const input = field.querySelector("input");
    field.hidden = event.target.value !== "+new";
    input.disabled = field.hidden;
    if (!field.hidden) input.focus();
});
