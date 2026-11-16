(function () {
    "use strict";

    const widget = document.querySelector("[data-cafe-chat]");
    if (!widget) return;

    const panel = widget.querySelector(".chat-panel");
    const launcher = widget.querySelector("[data-chat-open]");
    const minimize = widget.querySelector("[data-chat-minimize]");
    const unread = widget.querySelector("[data-chat-unread]");
    const chatCount = widget.querySelector("[data-chat-count]");
    const picker = widget.querySelector("[data-chat-customer]");
    const status = widget.querySelector("[data-chat-status]");
    const list = widget.querySelector("[data-chat-messages]");
    const form = widget.querySelector("[data-chat-form]");
    const input = form.elements.message;
    const send = form.querySelector("button");
    const isAdmin = widget.dataset.role === "admin";
    const timeFormat = new Intl.DateTimeFormat("vi-VN", {
        day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit"
    });

    launcher.addEventListener("click", () => {
        launcher.hidden = true;
        launcher.setAttribute("aria-expanded", "true");
        panel.hidden = false;
        markRead();
        list.scrollTop = list.scrollHeight;
        if (!input.disabled) input.focus();
        else minimize.focus();
    });

    minimize.addEventListener("click", () => {
        selectForDeletion(null);
        panel.hidden = true;
        launcher.hidden = false;
        launcher.setAttribute("aria-expanded", "false");
        launcher.focus();
    });

    if (!window.signalR) {
        status.textContent = "Chưa thể tải trò chuyện. Vui lòng tải lại trang.";
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(widget.dataset.hub)
        .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 3000 })
        .build();
    const messages = new Map();
    const expandedDates = new Set();
    const deletedMessages = new Set();
    const conversations = new Map();
    const drafts = new Map();
    const unreadConversations = new Map();
    let messageToDelete = null;
    let selected = null;
    let connected = false;
    let sending = false;
    let selectVersion = 0;
    let refreshVersion = 0;

    function updateUnread() {
        const count = [...unreadConversations.values()].reduce((total, ids) => total + ids.size, 0);
        unread.hidden = count === 0;
        unread.textContent = count > 99 ? "99+" : String(count);
        chatCount.hidden = count === 0;
        chatCount.textContent = unread.textContent;
        launcher.setAttribute("aria-label", count ? `Mở trò chuyện, ${count} tin chưa đọc` : "Mở trò chuyện");
    }

    function markRead() {
        unreadConversations.delete(isAdmin ? selected : null);
        updateUnread();
        if (isAdmin) renderConversations();
    }

    function selectForDeletion(id) {
        messageToDelete = id;
        list.classList.toggle("chat-selecting", id !== null);
        list.querySelectorAll(".chat-message").forEach(item => {
            const active = item.dataset.messageId === String(id);
            item.classList.toggle("chat-selected", active);
            const remove = item.querySelector(".chat-delete");
            if (remove) remove.hidden = !active;
        });
    }

    panel.addEventListener("click", event => {
        if (!event.target.closest(".chat-selected")) selectForDeletion(null);
    });

    panel.addEventListener("keydown", event => {
        if (event.key === "Escape") selectForDeletion(null);
    });

    function watchLongPress(button, id) {
        let timer;
        let origin;
        let held = false;
        const cancel = () => {
            clearTimeout(timer);
            origin = null;
        };

        button.addEventListener("pointerdown", event => {
            if (event.button !== 0) return;
            held = false;
            origin = { x: event.clientX, y: event.clientY };
            timer = setTimeout(() => {
                held = true;
                selectForDeletion(id);
            }, 500);
        });
        button.addEventListener("pointermove", event => {
            if (origin && Math.hypot(event.clientX - origin.x, event.clientY - origin.y) > 10)
                cancel();
        });
        ["pointerup", "pointercancel", "pointerleave"].forEach(name => button.addEventListener(name, cancel));
        button.addEventListener("contextmenu", event => {
            event.preventDefault();
            cancel();
            selectForDeletion(id);
        });
        button.addEventListener("click", event => {
            if (!held) return;
            event.stopImmediatePropagation();
            held = false;
        }, { capture: true });
    }

    function updateControls() {
        if (picker) picker.disabled = !connected || conversations.size === 0 || sending;
        input.disabled = !connected || sending || (isAdmin && !selected);
        send.disabled = input.disabled;
    }

    function renderConversations() {
        const options = [new Option(conversations.size ? "Chọn khách hàng" : "Chưa có cuộc trò chuyện", "")];
        for (const [id, name] of conversations) {
            const count = unreadConversations.get(id)?.size ?? 0;
            options.push(new Option(count ? `${name} (${count})` : name, id));
        }
        picker.replaceChildren(...options);
        if (selected && !conversations.has(selected)) {
            selected = null;
            ++selectVersion;
            input.value = "";
            messages.clear();
            displayMessages([]);
        }
        picker.value = selected || "";
        updateControls();
    }

    async function loadConversations() {
        const version = ++refreshVersion;
        const entries = await connection.invoke("GetConversations");
        if (version !== refreshVersion) return;

        conversations.clear();
        entries.forEach(entry => conversations.set(entry.customerId, entry.userName));
        renderConversations();
    }

    function displayMessages(incoming, scrollToEnd = false) {
        const wasAtEnd = list.scrollHeight - list.scrollTop - list.clientHeight < 40;
        const oldTop = list.scrollTop;
        incoming.forEach(message => {
            if (!deletedMessages.has(message.id)) messages.set(message.id, message);
        });
        const ordered = Array.from(messages.values()).sort((a, b) => a.id - b.id);
        const removed = ordered.splice(0, Math.max(0, ordered.length - 100));
        removed.forEach(message => {
            messages.delete(message.id);
            expandedDates.delete(message.id);
        });

        list.replaceChildren(...ordered.map((message) => {
            const row = document.createElement("li");
            row.className = "chat-message-row";
            const item = document.createElement("div");
            item.dataset.messageId = message.id;
            item.className = message.fromAdmin === isAdmin ? "chat-message chat-own" : "chat-message";

            const text = document.createElement("button");
            text.type = "button";
            text.className = "chat-message-text";
            text.textContent = message.text;
            const time = document.createElement("time");
            time.dateTime = message.sentAt;
            time.textContent = timeFormat.format(new Date(message.sentAt));
            time.hidden = !expandedDates.has(message.id);
            text.setAttribute("aria-expanded", String(!time.hidden));
            text.addEventListener("click", () => {
                time.hidden = !time.hidden;
                text.setAttribute("aria-expanded", String(!time.hidden));
                if (time.hidden) expandedDates.delete(message.id);
                else expandedDates.add(message.id);
            });
            item.append(text);
            if (isAdmin && !message.fromAdmin) {
                watchLongPress(text, message.id);
                const remove = document.createElement("button");
                remove.type = "button";
                remove.className = "chat-delete";
                remove.textContent = "Xóa";
                remove.setAttribute("aria-label", "Xóa tin nhắn của khách");
                remove.disabled = !connected;
                remove.addEventListener("click", async () => {
                    if (!connected || !window.confirm("Xóa vĩnh viễn tin nhắn này?")) return;
                    remove.disabled = true;
                    try {
                        if (!await connection.invoke("DeleteCustomerMessage", message.customerAccountId, message.id))
                            removeMessage(message.customerAccountId, message.id);
                    } catch {
                        status.textContent = "Chưa thể xóa tin nhắn. Vui lòng thử lại.";
                    } finally {
                        remove.disabled = !connected;
                    }
                });
                item.append(remove);
            }
            row.append(time, item);
            return row;
        }));

        selectForDeletion(messages.has(messageToDelete) ? messageToDelete : null);
        list.scrollTop = scrollToEnd || wasAtEnd ? list.scrollHeight : oldTop;
    }

    async function loadMessages(scrollToEnd = false) {
        const version = ++selectVersion;
        if (isAdmin && !selected) return;

        const history = await connection.invoke("GetMessages", selected);
        if (version === selectVersion) displayMessages(history, scrollToEnd);
    }

    if (picker) {
        picker.addEventListener("change", async () => {
            if (selected) drafts.set(selected, input.value);
            selected = picker.value || null;
            input.value = drafts.get(selected) || "";
            markRead();
            messages.clear();
            displayMessages([]);
            renderConversations();
            status.textContent = "";
            try {
                await loadMessages(true);
            } catch {
                if (connected) status.textContent = "Chưa thể tải tin nhắn.";
            }
        });
    }

    connection.on("MessageReceived", (message) => {
        const inCurrentChat = !isAdmin || selected === message.customerAccountId;
        const isIncoming = message.fromAdmin !== isAdmin;
        if (isIncoming && (panel.hidden || !inCurrentChat)) {
            const key = isAdmin ? message.customerAccountId : null;
            if (!unreadConversations.has(key)) unreadConversations.set(key, new Set());
            unreadConversations.get(key).add(message.id);
            updateUnread();
        }

        if (inCurrentChat) displayMessages([message]);
        if (isAdmin && !inCurrentChat && isIncoming) {

            if (conversations.has(message.customerAccountId)) renderConversations();
            else loadConversations().catch(() => {
                if (connected) status.textContent = "Chưa thể tải danh sách khách.";
            });
        }
    });

    function removeMessage(customerId, messageId) {
        deletedMessages.add(messageId);
        expandedDates.delete(messageId);
        unreadConversations.get(isAdmin ? customerId : null)?.delete(messageId);
        updateUnread();
        if (messages.delete(messageId)) displayMessages([]);
        if (isAdmin) loadConversations().catch(() => {
            if (connected) status.textContent = "Chưa thể tải danh sách khách.";
        });
    }

    connection.on("MessageDeleted", removeMessage);

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        const text = input.value.trim();
        if (!text || input.disabled) return;

        sending = true;
        updateControls();
        status.textContent = "";
        try {
            const currentMessage = await connection.invoke("SendMessage", selected, text);
            displayMessages([currentMessage], true);
            input.value = "";
        } catch {
            status.textContent = "Chưa thể gửi tin nhắn. Vui lòng thử lại.";
        } finally {
            sending = false;
            updateControls();
            if (!panel.hidden && !input.disabled) input.focus();
        }
    });

    async function restoreChat() {
        connected = true;
        updateControls();
        status.textContent = "";
        messages.clear();
        deletedMessages.clear();
        displayMessages([]);
        try {
            if (isAdmin) await loadConversations();
            await loadMessages(true);
        } catch {
            if (connected) status.textContent = "Chưa thể tải trò chuyện.";
        }
    }

    connection.onreconnecting(() => {
        connected = false;
        status.textContent = "Đang kết nối lại…";
        updateControls();
    });

    connection.onreconnected(restoreChat);

    async function connect() {
        try {
            await connection.start();
            await restoreChat();
        } catch {
            status.textContent = "Đang chờ kết nối…";
            setTimeout(connect, 3000);
        }
    }

    connection.onclose(() => {
        connected = false;
        updateControls();
        status.textContent = "Đã mất kết nối. Đang kết nối lại…";
        setTimeout(connect, 3000);
    });

    connect();
}());
