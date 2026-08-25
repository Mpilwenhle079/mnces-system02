(() => {
  const state = {
    token: localStorage.getItem('mnce_staff_token') || '',
    staffName: localStorage.getItem('mnce_staff_name') || '',
    staffRole: localStorage.getItem('mnce_staff_role') || '',
    connection: null,
    orders: [],
    categories: [],
    summary: null,
  };

  const els = {
    loginShell: document.getElementById('login-shell'),
    dashboard: document.getElementById('dashboard'),
    pinInput: document.getElementById('pin-input'),
    loginError: document.getElementById('login-error'),
    loginBtn: document.getElementById('login-btn'),
    livePill: document.getElementById('live-pill'),
    staffName: document.getElementById('staff-name'),
    logoutBtn: document.getElementById('logout-btn'),
    orderFilter: document.getElementById('order-filter'),
    topItemsBody: document.getElementById('top-items-body'),
    ordersBody: document.getElementById('orders-body'),
    menuBody: document.getElementById('menu-body'),
    statOrders: document.getElementById('stat-orders'),
    statRevenue: document.getElementById('stat-revenue'),
    statActive: document.getElementById('stat-active'),
    statCompleted: document.getElementById('stat-completed'),
    addItemBtn: document.getElementById('add-item-btn'),
    itemBackdrop: document.getElementById('item-backdrop'),
    itemSheet: document.getElementById('item-sheet'),
    itemClose: document.getElementById('item-close'),
    itemSheetTitle: document.getElementById('item-sheet-title'),
    itemId: document.getElementById('item-id'),
    itemCategory: document.getElementById('item-category'),
    itemName: document.getElementById('item-name'),
    itemServing: document.getElementById('item-serving'),
    itemPrice: document.getElementById('item-price'),
    itemDescription: document.getElementById('item-description'),
    itemImageUrl: document.getElementById('item-image-url'),
    itemAvailable: document.getElementById('item-available'),
    itemError: document.getElementById('item-error'),
    itemSave: document.getElementById('item-save'),
    callsBody: document.getElementById('calls-body'),
    refreshCallsBtn: document.getElementById('refresh-calls-btn'),
  };

  function statusClass(status) {
    return (status || 'pending').toLowerCase();
  }

  function setLoggedInUI() {
    els.loginShell.style.display = 'none';
    els.dashboard.style.display = 'block';
    els.staffName.textContent = state.staffName || 'Staff';
  }

  function setLive(isLive) {
    els.livePill.classList.toggle('is-live', isLive);
    els.livePill.innerHTML = `<span class="status-pill__dot"></span> ${isLive ? 'live' : 'reconnecting…'}`;
  }

  function setLoggedOutUI() {
    els.loginShell.style.display = 'block';
    els.dashboard.style.display = 'none';
    els.pinInput.value = '';
    els.loginError.textContent = '';
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
    els.pinInput.focus();
  }

  function renderSummary() {
    const summary = state.summary || {
      todayOrderCount: 0,
      todayRevenue: 0,
      pendingCount: 0,
      preparingCount: 0,
      readyCount: 0,
      completedTodayCount: 0,
      topItemsToday: []
    };

    els.statOrders.textContent = String(summary.todayOrderCount ?? 0);
    els.statRevenue.textContent = money(summary.todayRevenue ?? 0);
    els.statActive.textContent = String((summary.pendingCount ?? 0) + (summary.preparingCount ?? 0));
    els.statCompleted.textContent = String(summary.completedTodayCount ?? 0);

    if (!summary.topItemsToday || !summary.topItemsToday.length) {
      els.topItemsBody.innerHTML = '<tr><td colspan="3" class="muted">No item sales yet.</td></tr>';
      return;
    }

    els.topItemsBody.innerHTML = summary.topItemsToday.map(item => `
      <tr>
        <td>${item.name}</td>
        <td>${item.quantitySold}</td>
        <td>${money(item.revenue)}</td>
      </tr>
    `).join('');
  }

  function renderOrders() {
    const filter = els.orderFilter.value;
    const filtered = filter ? state.orders.filter(o => o.status === filter) : state.orders;

    if (!filtered.length) {
      els.ordersBody.innerHTML = '<tr><td colspan="8" class="muted">No orders found.</td></tr>';
      return;
    }

    els.ordersBody.innerHTML = filtered.map(order => `
      <tr>
        <td>${order.orderNumber}</td>
        <td>${order.customerName || 'Unknown'}</td>
        <td>${order.channel}</td>
        <td>
          <ul class="order-item-list">
            ${order.items.map(item => `<li>${item.quantity}× ${item.itemNameSnapshot || item.itemName}</li>`).join('')}
          </ul>
        </td>
        <td>${money(order.totalAmount)}</td>
        <td><span class="badge ${order.paymentStatus === 'Succeeded' ? 'ready' : 'cancelled'}">${order.paymentStatus || 'Unpaid'}</span></td>
        <td><span class="badge ${statusClass(order.status)}">${order.status}</span></td>
        <td>${new Date(order.createdAt).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}</td>
      </tr>
    `).join('');
  }

  function renderMenuCategories() {
    if (!state.categories.length) {
      els.menuBody.innerHTML = '<tr><td colspan="6" class="muted">No menu items yet.</td></tr>';
      return;
    }

    const rows = [];
    state.categories.forEach(category => {
      if (!category.items || !category.items.length) {
        rows.push(`
          <tr>
            <td>${category.name}</td>
            <td colspan="5" class="muted">No items in this category.</td>
          </tr>
        `);
        return;
      }

      category.items.forEach(item => {
        rows.push(`
          <tr>
            <td>${category.name}</td>
            <td>${item.name}</td>
            <td>${item.servingInfo || '—'}</td>
            <td>${money(item.price)}</td>
            <td><span class="badge ${item.isAvailable ? 'ready' : 'cancelled'}">${item.isAvailable ? 'Available' : 'Hidden'}</span></td>
            <td>
              <div class="row-actions">
                <button class="icon-btn" data-action="edit" data-id="${item.id}">Edit</button>
                <button class="icon-btn" data-action="toggle" data-id="${item.id}" data-available="${item.isAvailable}">${item.isAvailable ? 'Hide' : 'Show'}</button>
              </div>
            </td>
          </tr>
        `);
      });
    });

    els.menuBody.innerHTML = rows.join('');

    els.menuBody.querySelectorAll('[data-action="edit"]').forEach(btn => {
      btn.addEventListener('click', () => openItemSheet(Number(btn.dataset.id)));
    });

    els.menuBody.querySelectorAll('[data-action="toggle"]').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = Number(btn.dataset.id);
        const current = btn.dataset.available === 'true';
        try {
          await Api.patch(`/api/admin/menu/items/${id}/availability?isAvailable=${!current}`, null, { staff: true });
          await refreshDashboard();
        } catch (err) {
          showToast(err.message || 'Unable to update availability.');
        }
      });
    });
  }

  function populateCategorySelect() {
    els.itemCategory.innerHTML = state.categories.map(category => `
      <option value="${category.id}">${category.name}</option>
    `).join('');
  }

  function openItemSheet(itemId = null) {
    populateCategorySelect();
    els.itemError.textContent = '';
    els.itemBackdrop.classList.add('is-open');
    els.itemSheet.style.display = 'block';

    if (!itemId) {
      els.itemSheetTitle.textContent = 'Add menu item';
      els.itemId.value = '';
      els.itemCategory.value = String(state.categories[0]?.id || '');
      els.itemName.value = '';
      els.itemServing.value = '';
      els.itemPrice.value = '';
      els.itemDescription.value = '';
      els.itemImageUrl.value = '';
      els.itemAvailable.checked = true;
      return;
    }

    const item = state.categories.flatMap(category => category.items).find(entry => entry.id === itemId);
    if (!item) return;

    els.itemSheetTitle.textContent = 'Edit menu item';
    els.itemId.value = String(item.id);
    els.itemCategory.value = String(item.categoryId);
    els.itemName.value = item.name;
    els.itemServing.value = item.servingInfo || '';
    els.itemPrice.value = item.price;
    els.itemDescription.value = item.description || '';
    els.itemImageUrl.value = item.imageUrl || '';
    els.itemAvailable.checked = !!item.isAvailable;
  }

  function closeItemSheet() {
    els.itemBackdrop.classList.remove('is-open');
    els.itemSheet.style.display = 'none';
    els.itemError.textContent = '';
  }

  async function saveItem() {
    const payload = {
      categoryId: Number(els.itemCategory.value),
      name: els.itemName.value.trim(),
      servingInfo: els.itemServing.value.trim() || null,
      price: Number(els.itemPrice.value),
      description: els.itemDescription.value.trim() || null,
      imageUrl: els.itemImageUrl.value.trim() || null,
      isAvailable: els.itemAvailable.checked,
    };

    if (!payload.name) {
      els.itemError.textContent = 'Please enter an item name.';
      return;
    }
    if (!Number.isFinite(payload.price) || payload.price < 0) {
      els.itemError.textContent = 'Please enter a valid price.';
      return;
    }

    try {
      const id = els.itemId.value;
      if (id) {
        await Api.put(`/api/admin/menu/items/${id}`, payload, { staff: true });
      } else {
        await Api.post('/api/admin/menu/items', payload, { staff: true });
      }
      closeItemSheet();
      await refreshDashboard();
    } catch (err) {
      els.itemError.textContent = err.message || 'Something went wrong while saving the item.';
    }
  }

  async function refreshDashboard() {
    const [summary, orders, categories, calls] = await Promise.all([
      Api.get('/api/dashboard/summary', { staff: true }),
      Api.get('/api/orders?take=100', { staff: true }),
      Api.get('/api/admin/menu/categories', { staff: true }),
      Api.get('/api/support-calls', { staff: true }),
    ]);

    state.summary = summary;
    state.orders = orders || [];
    state.categories = categories || [];

    renderSummary();
    renderOrders();
    renderMenuCategories();
    els.callsBody.innerHTML = (calls || []).length ? calls.map(call => `
      <tr><td>${call.customerName}<br><span class="muted">${call.phone}</span></td><td>${call.type}</td><td>${call.description}</td>
      <td><select data-call-id="${call.id}" class="call-status"><option ${call.status === 'Open' ? 'selected' : ''}>Open</option><option ${call.status === 'Resolved' ? 'selected' : ''}>Resolved</option></select></td>
      <td>${new Date(call.createdAt).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}</td><td>${call.orderId || '—'}</td></tr>`).join('') : '<tr><td colspan="6" class="muted">No calls recorded.</td></tr>';
    els.callsBody.querySelectorAll('.call-status').forEach(select => select.addEventListener('change', async () => {
      await Api.patch(`/api/support-calls/${select.dataset.callId}/status?status=${encodeURIComponent(select.value)}`, null, { staff: true });
    }));
  }

  async function login() {
    const pin = els.pinInput.value.trim();
    if (!pin) {
      els.loginError.textContent = 'Please enter your manager PIN.';
      return;
    }

    try {
      const result = await Api.post('/api/auth/staff-login', { pinCode: pin });
      if (result.role !== 'Admin') {
        els.loginError.textContent = 'This dashboard is restricted to managers.';
        return;
      }
      state.token = result.token;
      state.staffName = result.name;
      state.staffRole = result.role;
      localStorage.setItem('mnce_staff_token', state.token);
      localStorage.setItem('mnce_staff_name', state.staffName);
      localStorage.setItem('mnce_staff_role', state.staffRole);
      els.loginError.textContent = '';
      setLoggedInUI();
      await refreshDashboard();
      await connectHub();
    } catch (err) {
      els.loginError.textContent = err.message || 'Incorrect PIN.';
    }
  }

  async function connectHub() {
    if (typeof signalR === 'undefined') {
      setLive(false);
      els.livePill.innerHTML = '<span class="status-pill__dot"></span> live updates unavailable';
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/orders')
      .withAutomaticReconnect()
      .build();

    state.connection = connection;
    connection.on('NewOrder', async () => {
      setLive(true);
      await refreshDashboard();
    });
    connection.on('OrderStatusChanged', async () => {
      setLive(true);
      await refreshDashboard();
    });

    try {
      await connection.start();
      await connection.invoke('JoinStaffGroup');
      setLive(true);
    } catch {
      setLive(false);
      els.livePill.innerHTML = '<span class="status-pill__dot"></span> offline';
    }
  }

  els.loginBtn.addEventListener('click', login);
  els.pinInput.addEventListener('keydown', event => {
    if (event.key === 'Enter') login();
  });
  els.logoutBtn.addEventListener('click', setLoggedOutUI);
  els.orderFilter.addEventListener('change', renderOrders);
  els.addItemBtn.addEventListener('click', () => openItemSheet());
  els.itemClose.addEventListener('click', closeItemSheet);
  els.itemBackdrop.addEventListener('click', closeItemSheet);
  els.itemSave.addEventListener('click', saveItem);
  els.refreshCallsBtn.addEventListener('click', refreshDashboard);

  if (state.token) {
    state.staffName = state.staffName || 'Staff';
    state.staffRole = state.staffRole || 'Admin';
    setLoggedInUI();
    refreshDashboard();
    connectHub();
  }
})();
