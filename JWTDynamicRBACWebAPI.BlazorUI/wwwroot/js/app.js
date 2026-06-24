// =============================================
// JWT RBAC UI — app.js
// Auth utilities, API helpers, navigation guard
// =============================================

const API_BASE = 'http://localhost:5196';
const TOKEN_KEY = 'jwt_token';

// ── JWT Utilities ──────────────────────────

/**
 * Decode a JWT payload without any library
 */
function decodeJwt(token) {
    try {
        const parts = token.split('.');
        if (parts.length !== 3) return null;
        const payload = parts[1]
            .replace(/-/g, '+')
            .replace(/_/g, '/');
        return JSON.parse(atob(payload));
    } catch {
        return null;
    }
}

/**
 * Get token from localStorage
 */
function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

/**
 * Store token in localStorage
 */
function setToken(token) {
    localStorage.setItem(TOKEN_KEY, token);
}

/**
 * Remove token — logout
 */
function clearToken() {
    localStorage.removeItem(TOKEN_KEY);
}

/**
 * Check if user is currently logged in (token exists and not expired)
 */
function isLoggedIn() {
    const token = getToken();
    if (!token) return false;
    const payload = decodeJwt(token);
    if (!payload) return false;
    // exp is in seconds
    return payload.exp && Date.now() / 1000 < payload.exp;
}

/**
 * Get current user's role from JWT
 */
function getUserRole() {
    const token = getToken();
    if (!token) return null;
    const payload = decodeJwt(token);
    // Microsoft ClaimTypes.Role maps to this URI in JWT
    return payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        || payload?.role
        || null;
}

/**
 * Get current user's name
 */
function getUserName() {
    const token = getToken();
    if (!token) return null;
    const payload = decodeJwt(token);
    return payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
        || payload?.unique_name
        || payload?.name
        || 'User';
}

/**
 * Get all permissions from JWT claims
 */
function getPermissions() {
    const token = getToken();
    if (!token) return [];
    const payload = decodeJwt(token);
    const perms = payload?.Permission;
    if (!perms) return [];
    return Array.isArray(perms) ? perms : [perms];
}

/**
 * Check if user has a specific permission
 */
function hasPermission(permission) {
    return getPermissions().includes(permission);
}

// ── Navigation Guards ──────────────────────

/**
 * Require login — if not logged in, redirect to /Login
 */
function requireAuth() {
    if (!isLoggedIn()) {
        window.location.href = '/Login';
        return false;
    }
    return true;
}

/**
 * Require Admin role — if not admin, redirect to /Products
 */
function requireAdmin() {
    if (!requireAuth()) return false;
    if (getUserRole() !== 'Admin') {
        window.location.href = '/Products';
        return false;
    }
    return true;
}

/**
 * Redirect already-logged-in users away from home/login
 */
function redirectIfLoggedIn() {
    if (isLoggedIn()) {
        const role = getUserRole();
        window.location.href = role === 'Admin' ? '/Dashboard' : '/Products';
    }
}

// ── API Helper ─────────────────────────────

/**
 * Make an authenticated API call
 */
async function apiCall(path, options = {}) {
    const token = getToken();
    const headers = {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
        ...(options.headers || {})
    };
    const response = await fetch(`${API_BASE}${path}`, {
        ...options,
        headers
    });
    if (response.status === 401) {
        clearToken();
        window.location.href = '/Login';
        return null;
    }
    return response;
}

// ── Sidebar ────────────────────────────────

/**
 * Populate sidebar user info
 */
function initSidebarUser() {
    const name = getUserName();
    const role = getUserRole();
    const nameEl = document.getElementById('sidebar-username');
    const roleEl = document.getElementById('sidebar-role');
    const avatarEl = document.getElementById('sidebar-avatar');

    if (nameEl) nameEl.textContent = name;
    if (roleEl) {
        roleEl.textContent = role || '';
        roleEl.className = 'badge ' + (role === 'Admin' ? 'badge-blue' : 'badge-green');
    }
    if (avatarEl) avatarEl.textContent = name.charAt(0).toUpperCase();

    // Show admin-only nav items
    if (role === 'Admin') {
        document.querySelectorAll('[data-admin-only]').forEach(el => el.classList.remove('hidden'));
    }
}

/**
 * Logout — clear token and go home
 */
function logout() {
    clearToken();
    window.location.href = '/';
}

// ── Notification Toast ─────────────────────

function showToast(message, type = 'info', duration = 3500) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = `
            position: fixed; bottom: 24px; right: 24px;
            z-index: 9999; display: flex; flex-direction: column; gap: 10px;
        `;
        document.body.appendChild(container);
    }

    const icons = { success: '✅', error: '❌', info: 'ℹ️', warning: '⚠️' };
    const colors = {
        success: '#3fb950', error: '#f85149', info: '#58a6ff', warning: '#d29922'
    };

    const toast = document.createElement('div');
    toast.style.cssText = `
        background: #1c2333; border: 1px solid #30363d;
        border-left: 3px solid ${colors[type]};
        border-radius: 8px; padding: 12px 16px;
        color: #e6edf3; font-size: 13px; font-family: 'Inter', sans-serif;
        display: flex; align-items: center; gap: 8px;
        box-shadow: 0 8px 32px rgba(0,0,0,0.5);
        animation: slideIn 0.25s ease;
        max-width: 340px;
    `;
    toast.innerHTML = `<span>${icons[type]}</span><span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'fadeOut 0.25s ease forwards';
        setTimeout(() => toast.remove(), 250);
    }, duration);
}

// Inject toast keyframes once
const toastStyle = document.createElement('style');
toastStyle.textContent = `
    @keyframes slideIn { from { opacity: 0; transform: translateX(20px); } to { opacity: 1; transform: translateX(0); } }
    @keyframes fadeOut { from { opacity: 1; } to { opacity: 0; transform: translateX(20px); } }
`;
document.head.appendChild(toastStyle);

// ── Modal Helpers ──────────────────────────

function openModal(id) {
    const el = document.getElementById(id);
    if (el) el.classList.add('active');
}

function closeModal(id) {
    const el = document.getElementById(id);
    if (el) el.classList.remove('active');
}

// Close modal on overlay click
document.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal-overlay')) {
        e.target.classList.remove('active');
    }
});
