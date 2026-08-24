/**
 * Kitchen dashboard: staff log in by PIN -> fetch orders -> live board updates.
 */
(() => {
  const state = {
    token: localStorage.getItem('mnce_staff_token') || '',
    staffName: localStorage.getItem('mnce_staff_name') || '',
    staffRole: localStorage.getItem('mnce_staff_role') || '',
    orders: [],
    connection: null,
  };

  const els = {
    loginShell: document.getElementById('login-shell'),
    dashboard: document.getElementById('dashboard'),
    pinInput: document.getElementById('pin-input'),
    loginError: document.getElementById('login-error'),
    loginBtn: document.getElementById('login-btn'),
    adminBtn: document.getElementById('admin-btn'),
    livePill: document.getElementById('live-pill'),
    staffName: document.getElementById('staff-name'),
    logoutBtn: document.getElementById('logout-btn'),
    cols: {
      Pending: document.getElementById('col-pending'),
      Preparing: document.getElementById('col-preparing'),
      Ready: document.getElementById('col-ready'),
    },
    counts: {
      Pending: document.getElementById('count-pending'),
      Preparing: document.getElementById('count-preparing'),
      Ready: document.getElementById('count-ready'),
    },
  };

  function setLoggedInUI() {
    els.loginShell.style.display = 'none';
    els.dashboard.style.display = 'block';
    els.staffName.textContent = state.staffName || 'Staff';
  }

  function setLoggedOutUI() {
    els.loginShell.style.display = 'block';
    els.dashboard.style.display = 'none';
    els.pinInput.value = '';
    els.loginError.textContent = '';
    els.pinInput.focus();
    localStorage.removeItem('mnce_staff_token');
    localStorage.removeItem('mnce_staff_name');
    localStorage.removeItem('mnce_staff_role');
    state.token = '';
    state.staffName = '';
    state.staffRole = '';
    if (state.connection) {
      state.connection.stop();
      state.connection = null;
    }
    window.location.href = 'index.html';
  }

  function getStatusLabel(status) {
    const labels = {
      Pending: 'Pending',
      Preparing: 'Preparing',
      Ready: 'Ready',
      Completed: 'Completed',
      Cancelled: 'Cancelled',
    };
    return labels[status] || status;
  }

  function renderBoard() {
    const statuses = ['Pending', 'Preparing', 'Ready'];
    statuses.forEach(status => {
      const col = els.cols[status];
      col.innerHTML = '';
      const orders = state.orders.filter(o => o.status === status);
      els.counts[status].textContent = String(orders.length);

      if (!orders.length) {
        const empty = document.createElement('div');
        empty.className = 'empty-state';
        empty.textContent = 'No orders';
        col.appendChild(empty);
        return;
      }

      orders.forEach(order => {
        const card = document.createElement('div');
        card.className = 'order-card';
        card.innerHTML = `
          <div class="order-card__head">
            <span class="order-card__num">${order.orderNumber}</span>
            <span class="order-card__time">${timeAgo(order.createdAt)}</span>
          </div>
          <span class="order-card__channel">${order.channel}</span>
          <ul>
            ${order.items.map(item => `<li><span><span class="qty">${item.quantity}×</span>${item.itemName}</span><strong>${money(item.lineTotal)}</strong></li>`).join('')}
          </ul>
          ${order.notes ? `<div class="order-card__note">${order.notes}</div>` : ''}
          <div class="order-card__actions">
            ${status === 'Pending' ? '<button class="btn btn-primary" data-next="Preparing">Start</button>' : ''}
            ${status === 'Preparing' ? '<button class="btn btn-primary" data-next="Ready">Ready</button>' : ''}
            ${status === 'Ready' ? '<button class="btn btn-primary" data-next="Completed">Complete</button>' : ''}
          </div>
        `;

        const btn = card.querySelector('[data-next]');
        if (btn) {
          btn.addEventListener('click', async () => {
            try {
              await Api.put(`/api/orders/${order.id}/status`, { status: btn.dataset.next }, { staff: true });
              await refreshOrders();
            } catch (err) {
              showToast(err.message || 'Unable to update order.');
            }
          });
        }

        col.appendChild(card);
      });
    });
  }

  async function refreshOrders() {
    try {
      const orders = await Api.get('/api/orders?take=100', { staff: true });
      state.orders = orders || [];
      renderBoard();
    } catch (err) {
      showToast(err.message || 'Could not load orders.');
    }
  }

  async function login() {
    const pin = document.getElementById('pin-input').value.trim();
    if (!pin) {
      els.loginError.textContent = 'Please enter your PIN.';
      return;
    }

    try {
      const result = await Api.post('/api/auth/staff-login', { pinCode: pin });
      state.token = result.token;
      state.staffName = result.name;
      state.staffRole = result.role;
      localStorage.setItem('mnce_staff_token', state.token);
      localStorage.setItem('mnce_staff_name', state.staffName);
      localStorage.setItem('mnce_staff_role', state.staffRole);
      els.loginError.textContent = '';
      setLoggedInUI();
      await refreshOrders();
      await connectHub();
    } catch (err) {
      els.loginError.textContent = err.message || 'Incorrect PIN.';
    }
  }

  function openAdminDashboard() {
    localStorage.removeItem('mnce_staff_token');
    localStorage.removeItem('mnce_staff_name');
    localStorage.removeItem('mnce_staff_role');
    window.location.href = 'admin.html';
  }

  async function connectHub() {
    if (typeof signalR === 'undefined') {
      els.livePill.innerHTML = '<span class="status-pill__dot"></span> live updates unavailable';
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/orders')
      .withAutomaticReconnect()
      .build();

    state.connection = connection;

    connection.on('NewOrder', async () => {
      els.livePill.innerHTML = '<span class="status-pill__dot"></span> live';
      await refreshOrders();
    });

    connection.on('OrderStatusChanged', async () => {
      els.livePill.innerHTML = '<span class="status-pill__dot"></span> live';
      await refreshOrders();
    });

    try {
      await connection.start();
      await connection.invoke('JoinStaffGroup');
      els.livePill.classList.add('is-live');
      els.livePill.innerHTML = '<span class="status-pill__dot"></span> live';
    } catch {
      els.livePill.innerHTML = '<span class="status-pill__dot"></span> offline';
    }
  }

  function init() {
    els.loginBtn.addEventListener('click', login);
    els.adminBtn.addEventListener('click', openAdminDashboard);
    els.pinInput.addEventListener('keydown', (event) => {
      if (event.key === 'Enter') login();
    });
    els.logoutBtn.addEventListener('click', setLoggedOutUI);

    if (state.token) {
      state.staffName = state.staffName || 'Staff';
      state.staffRole = state.staffRole || 'Kitchen';
      setLoggedInUI();
      refreshOrders();
      connectHub();
    }
  }

  init();
})();
