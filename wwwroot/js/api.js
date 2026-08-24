/**
 * Shared fetch helper for all dashboards.
 * Same-origin by default, since the API serves this static site itself.
 */
const Api = (() => {
  const BASE = ''; // same origin

  function staffToken() {
    return localStorage.getItem('mnce_staff_token');
  }

  async function request(path, { method = 'GET', body, staff = false } = {}) {
    const headers = { 'Content-Type': 'application/json' };
    if (staff) {
      const token = staffToken();
      if (token) headers['X-Staff-Token'] = token;
    }

    const res = await fetch(BASE + path, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    if (res.status === 204) return null;

    let data = null;
    try { data = await res.json(); } catch { /* no body */ }

    if (!res.ok) {
      const message = (data && data.message) || `Request failed (${res.status})`;
      const err = new Error(message);
      err.status = res.status;
      throw err;
    }
    return data;
  }

  return {
    get: (path, opts) => request(path, { ...opts, method: 'GET' }),
    post: (path, body, opts) => request(path, { ...opts, method: 'POST', body }),
    put: (path, body, opts) => request(path, { ...opts, method: 'PUT', body }),
    patch: (path, body, opts) => request(path, { ...opts, method: 'PATCH', body }),
    del: (path, opts) => request(path, { ...opts, method: 'DELETE' }),
    staffToken,
  };
})();

function money(amount) {
  return 'R' + Number(amount).toFixed(2);
}

function timeAgo(isoString) {
  // The API stores/serializes timestamps as UTC but without a trailing "Z"
  // (SQLite doesn't preserve DateTimeKind), so normalize before parsing.
  const utcString = isoString.endsWith('Z') ? isoString : isoString + 'Z';
  const seconds = Math.floor((Date.now() - new Date(utcString).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ${minutes % 60}m ago`;
}

function showToast(message) {
  let toast = document.querySelector('.toast');
  if (!toast) {
    toast = document.createElement('div');
    toast.className = 'toast';
    document.body.appendChild(toast);
  }
  toast.textContent = message;
  toast.classList.add('is-visible');
  clearTimeout(toast._timer);
  toast._timer = setTimeout(() => toast.classList.remove('is-visible'), 2600);
}
