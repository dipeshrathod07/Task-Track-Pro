// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
let baseApiUrl = "http://localhost:5267/api";

function checkIdentityAndAccess() {
    let user = JSON.parse(sessionStorage.getItem("user"));
    let controller = window.location.href.split("/")[3].toLowerCase();
    switch (controller) {
        case "admin":
            if (user == null || user.role != "A") {
                Swal.fire({
                    title: "Unauthorized access",
                    text: "Redirecting...",
                    icon: "error",
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    window.location.assign("/Auth/Login");
                });
            }
            break;
        case "employee":
            if (user == null || user.role != "E") {
                Swal.fire({
                    title: "Unauthorized access",
                    text: "Redirecting...",
                    icon: "error",
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    window.location.assign("/Auth/Login");
                });
            }
            break;
        default:
            console.log("Authentication not required");
    }
}

//

function setProfileDiv() {
    let user = JSON.parse(sessionStorage.getItem("user"));
    if (user != null) {
        // user is logged in
        htmlContent = `<div class="btn-group">
        <button type="button" class="btn btn-light dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false">
            <span>${user.firstName} ${user.lastName}</span>
            <img class="rounded-circle" style="height:40px; width:40px;" src="/profile_images/${user.image}" onerror="this.src='/profile_images/placeholder.jpg'">
        </button>
        <ul class="dropdown-menu dropdown-menu-end">
            <li><a class="dropdown-item" href="/${user.role == "A" ? "Admin" : "Employee"}/Profile">Profile</a></li>
            <li><a class="dropdown-item" href="/Auth/Logout">Logout</a></li>
        </ul>
    </div>`;
    } else {
        // visitor
        htmlContent = `<a class="btn btn-light" href="/Auth/Login">Sign In</a>`;
    }
    $("#profileMenu").html(htmlContent);
}

// NOTIFCATIONS ============================================================

function loadNotifications() {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5267/notificationHub") // Adjust according to your backend
        .withAutomaticReconnect()
        .build();

    connection.start()
        .then(() => {
            console.log("Connected to SignalR hub");
        })
        .catch(err => console.error("SignalR connection failed: ", err));

    connection.on("ReceiveNotifications", function (notification) {
        // console.log("New Notification Received:", notification);
        updateNotificationList(notification);
    });
}


function markAsRead(notificationId) {
    return new Promise(function (resolve, reject) {
        $.ajax({
            url: `${baseApiUrl}/notification/mark-read/${notificationId}`,
            method: 'PUT',
            success: function () {
                loadNotifications(); // Refresh the list
                resolve();
            },
            error: function (xhr) {
                console.error('Error marking notification as read:', xhr);
                reject(xhr);
            }
        });
    });
}

function markAllAsRead() {
    const user = JSON.parse(sessionStorage.getItem("user"));
    if (!user) return;
    return new Promise(function (resolve, reject) {
        $.ajax({
            url: `${baseApiUrl}/notification/mark-all-read/${user.userId}`,
            method: 'PUT',
            success: function () {
                loadNotifications(); // Refresh the list
                resolve();
            },
            error: function (xhr) {
                console.error('Error marking all notifications as read:', xhr);
                reject(xhr);
            }
        });
    });
}

let firstLoad = true;

function updateNotificationList(notifications) {
    const notificationList = $("#notificationList");

    notifications = notifications.filter(a => a.userId == user.userId);

    if (!notifications || notifications.length === 0) {
        notificationList.html(`<div class="p-3 text-center text-muted">No notifications</div>`);
        $("#notificationCount").hide();
        return;
    }

    // ✅ Sort by timestamp (assuming createdAt is a valid Unix timestamp or ISO string)
    // notifications.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt)).reverse();
    // notifications.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));

    if (firstLoad) {
        notificationList.html(""); // Clear existing content only on first load
    }

    notifications.forEach(notification => {
        // Prevent duplicate notifications from being added
        if ($(`.notification-item[data-id="${notification.notificationId}"]`).length === 0) {
            const html = `
                <div class="notification-item ${notification.isRead ? '' : 'unread'}" 
                     onclick="markAsRead(${notification.notificationId})"
                     data-id="${notification.notificationId}">
                    <div class="d-flex align-items-center">
                        <div class="notification-icon ${notification.type.toLowerCase()}">
                            <i class="fas ${getNotificationIcon(notification.type.toLowerCase())}"></i>
                        </div>
                        <div class="flex-grow-1">
                            <div class="notification-title">${notification.title}</div>
                            <div class="notification-desc">${notification.description}</div>
                            <div class="notification-time">
                                <i class="far fa-clock"></i> ${formatTimeAgo(notification.createdAt)}
                            </div>
                        </div>
                    </div>
                </div>
            `;

            // ✅ Always prepend newest notifications
            notificationList.prepend(html);
        }
    });

    $("#notificationCount").text(notifications.length).show();
    firstLoad = false; // Mark first load as complete
}



function getNotificationIcon(type) {
    switch (type) {
        case 'task': return 'fa-tasks';
        case 'message': return 'fa-envelope';
        case 'alert': return 'fa-exclamation-triangle';
        default: return 'fa-bell';
    }
}

function formatTimeAgo(date) {
    const now = new Date();
    const diff = now - new Date(date);
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) return `${days}d ago`;
    if (hours > 0) return `${hours}h ago`;
    if (minutes > 0) return `${minutes}m ago`;
    return 'Just now';
}

// function to send admin notification
function sendAdminNotification(title, description, type = "alert") {
    return new Promise((resolve, reject) => {
        const notification = {
            userId: "9d5c1ce8-e30e-4861-b144-61212b455b40", // Admin's userId
            title: title,
            description: description,
            type: type
        };

        $.ajax({
            url: 'http://localhost:5267/api/notification',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(notification),
            success: resolve,
            error: reject
        });
    });
}

// function to send user notification
function sendUserNotification(userId, title, description, type = "alert") {
    return new Promise((resolve, reject) => {
        const notification = {
            userId: userId,
            title: title,
            description: description,
            type: type
        };

        $.ajax({
            url: 'http://localhost:5267/api/notification',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(notification),
            success: resolve,
            error: reject
        });
    });
}


// CHAT MESSAGES ==========================================================

function loadUnreadMessages() {
    const user = JSON.parse(sessionStorage.getItem("user"));
    if (!user) return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`http://localhost:5267/chatHub`)// Adjust according to your backend
        .withAutomaticReconnect()
        .build();

    connection.start()
        .then(() => {
            console.log("Connected to SignalR hub");
        })
        .catch(err => console.error("SignalR connection failed: ", err));

    connection.on("ReceiveMessages", function (notification) {
        // console.log("New Message Received:", notification);
        updateMessageList(notification);
    });
}

function updateMessageList(messages) {
    const messageList = $("#messageList");
    const user = JSON.parse(sessionStorage.getItem("user"));
    if (!user) return;

    messages = messages.filter(a => a.senderId == user.userId || a.receiverId == user.userId);
    // console.log(messages)

    // messageList.empty();

    if (!messages || messages.length === 0) {
        messageList.empty();
        messageList.append(`
            <div class="p-3 text-center text-muted">
                <i class="fas fa-inbox fa-2x mb-2"></i>
                <div>No new messages</div>
            </div>
        `);
        $("#messageCount").hide();
        return;
    }

    if (firstLoad) {
        notificationList.html(""); // Clear existing content only on first load
    }

    messages.forEach(msg => {
        const html = `
            <div class="message-item unread">
                <div class="d-flex justify-content-between align-items-start">
                    <div class="flex-grow-1 pe-2" onclick="goToChat('${msg.senderId}')">
                        <div class="message-sender">
                            <i class="fas fa-user-circle me-1"></i>
                            ${msg.senderName || 'User'}
                        </div>
                        <div class="message-preview">${msg.message}</div>
                        <div class="message-time">
                            <i class="far fa-clock me-1"></i>${formatTimeAgo(msg.timestamp)}
                        </div>
                    </div>
                    <button class="btn btn-sm text-primary mark-read-btn" 
                            onclick="event.stopPropagation(); markChatAsRead(${msg.chatId}, this)" 
                            title="Mark as read">
                        <i class="fas fa-check"></i>
                    </button>
                </div>
            </div>
        `;
        messageList.prepend(html);
    });

    $("#messageCount").text(messages.length).show();
    firstLoad = false;
}

function markChatAsRead(chatId, button) {
    $.ajax({
        url: `${baseApiUrl}/chat/mark-read/${chatId}`,
        method: 'PUT',
        success: function () {
            $(button).closest('.message-item').fadeOut(300, function () {
                $(this).remove();
                const remainingMessages = $('.message-item').length;
                $("#messageCount").text(remainingMessages);
                if (remainingMessages === 0) {
                    $("#messageCount").hide();
                    $("#messageList").html(`
                        <div class="p-3 text-center text-muted">
                            No new messages
                        </div>
                    `);
                }
            });
        },
        error: function (xhr) {
            console.error('Error marking message as read:', xhr);
        }
    });
}

function goToChat(senderId) {
    window.location.href = `ChatUser/${senderId}`;
}

// DOCUMENT READY FUCNTION ==============================================

$(document).ready(function () {
    loadNotifications()
    loadUnreadMessages();


    // Handle notification click events
    $(document).on('click', '.notification-item', function () {
        const notificationId = $(this).data('id');
        markAsRead(notificationId);
    });
});

// ========================================================================

// Important
checkIdentityAndAccess();
//
setProfileDiv();
