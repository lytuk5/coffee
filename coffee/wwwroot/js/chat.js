(function () {
    "use strict";

    const widget = document.querySelector("[data-cafe-chat]");
    if (!widget) return;

    const panel = widget.querySelector(".chat-panel");
    const launcher = widget.querySelector("[data-chat-open]");
    const minimize = widget.querySelector("[data-chat-minimize]");
    const unread = widget.querySelector("[data-chat-unread]");
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
        unread.hidden = true;
        list.scrollTop = list.scrollHeight;
        if (!input.disabled) input.focus();
        else minimize.focus();
    });

    minimize.addEventListener("click", () => {
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
    const deletedMessages = new Set();
    const conversations = new Map();
    const drafts = new Map();
    const unreadConversations = new Set();
    let selected = null;
    let connected = false;
    let sending = false;
    let selectVersion = 0;
    let refreshVersion = 0;

    function updateControls() {
        if (picker) picker.disabled = !connected || conversations.size === 0 || sending;
        input.disabled = !connected || sending || (isAdmin && !selected);
        send.disabled = input.disabled;
    }

    function renderConversations() {
        const options = [new Option(conversations.size ? "Chọn khách hàng" : "Chưa có cuộc trò chuyện", "")];
        for (const [id, name] of conversations) {
            options.push(new Option(unreadConversations.has(id) ? name + " (tin mới)" : name, id));
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
        removed.forEach(message => messages.delete(message.id));

        list.replaceChildren(...ordered.map((message) => {
            const item = document.createElement("li");
            item.className = message.fromAdmin === isAdmin ? "chat-message chat-own" : "chat-message";

            const text = document.createElement("p");
            text.textContent = message.text;
            const time = document.createElement("time");
            time.dateTime = message.sentAt;
            time.textContent = timeFormat.format(new Date(message.sentAt));
            item.append(text, time);
            if (isAdmin && !message.fromAdmin) {
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
            return item;
        }));

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
            unreadConversations.delete(selected);
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
        if (isIncoming && panel.hidden) unread.hidden = false;

        if (inCurrentChat) displayMessages([message]);
        if (isAdmin && !inCurrentChat && isIncoming) {
            unreadConversations.add(message.customerAccountId);
            if (conversations.has(message.customerAccountId)) renderConversations();
            else loadConversations().catch(() => {
                if (connected) status.textContent = "Chưa thể tải danh sách khách.";
            });
        }
    });

    function removeMessage(customerId, messageId) {
        deletedMessages.add(messageId);
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
