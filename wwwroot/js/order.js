/**
 * Customer ordering page: browse menu -> build cart -> checkout -> live tracking.
 */
(() => {
  let categories = [];       // [{id, name, items:[...]}]
  const cart = new Map();    // menuItemId -> { item, qty }
  let channel = 'Collection';
  let trackedOrderId = null;
  let hubConnection = null;
  const categoryImages = {
    plates: 'https://images.unsplash.com/photo-1559847844-5315695dadae?auto=format&fit=crop&w=1200&q=82',
    platters: 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=1200&q=82'
  };
  const fallbackImage = categoryImages.plates;
  const itemImages = {
    wors: 'https://images.unsplash.com/photo-1559847844-5315695dadae?auto=format&fit=crop&w=1200&q=82',
    beef: 'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=1200&q=82',
    chicken: 'https://images.unsplash.com/photo-1604908176997-125f25cc6f3d?auto=format&fit=crop&w=1200&q=82',
    platter: 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=1200&q=82'
  };

  function itemImage(item) {
    if (item.imageUrl) return item.imageUrl;
    const name = item.name.toLowerCase();
    if (name.includes('wing') || name.includes('chicken')) return itemImages.chicken;
    if (name.includes('wors')) return itemImages.wors;
    if (name.includes('beef')) return itemImages.beef;
    return itemImages.platter;
  }

  const el = {
    root: document.getElementById('menu-content'),
    cartBar: document.getElementById('cart-bar'),
    cartCount: document.getElementById('cart-count'),
    cartTotal: document.getElementById('cart-total'),
    openCheckout: document.getElementById('open-checkout'),
    checkoutBackdrop: document.getElementById('checkout-backdrop'),
    checkoutSheet: document.getElementById('checkout-sheet'),
    checkoutClose: document.getElementById('checkout-close'),
    checkoutItems: document.getElementById('checkout-items'),
    addressField: document.getElementById('address-field'),
    submitOrder: document.getElementById('submit-order'),
    submitTotal: document.getElementById('submit-total'),
    checkoutError: document.getElementById('checkout-error'),
    paymentBackdrop: document.getElementById('payment-backdrop'),
    paymentSheet: document.getElementById('payment-sheet'),
    paymentClose: document.getElementById('payment-close'),
    paymentOrderNumber: document.getElementById('payment-order-number'),
    paymentPriceLines: document.getElementById('payment-price-lines'),
    cardFields: document.getElementById('card-fields'),
    cashNote: document.getElementById('cash-note'),
    cardNumber: document.getElementById('card-number'),
    cardName: document.getElementById('card-name'),
    cardMonth: document.getElementById('card-month'),
    cardYear: document.getElementById('card-year'),
    cardCvv: document.getElementById('card-cvv'),
    paymentError: document.getElementById('payment-error'),
    paymentTotal: document.getElementById('payment-total'),
    submitPayment: document.getElementById('submit-payment'),
    confirmBackdrop: document.getElementById('confirm-backdrop'),
    confirmSheet: document.getElementById('confirm-sheet'),
    confirmOrderNumber: document.getElementById('confirm-order-number'),
    confirmItems: document.getElementById('confirm-items'),
    confirmError: document.getElementById('confirm-error'),
    submitToKitchen: document.getElementById('submit-to-kitchen'),
    trackBackdrop: document.getElementById('track-backdrop'),
    trackSheet: document.getElementById('track-sheet'),
    trackOrderNumber: document.getElementById('track-order-number'),
    trackerFill: document.getElementById('tracker-fill'),
    trackMessage: document.getElementById('track-message'),
    trackLoyalty: document.getElementById('track-loyalty'),
    trackNewOrder: document.getElementById('track-new-order'),
    openSupport: document.getElementById('open-support'),
    supportBackdrop: document.getElementById('support-backdrop'),
    supportSheet: document.getElementById('support-sheet'),
    supportClose: document.getElementById('support-close'),
    supportError: document.getElementById('support-error'),
    supportSubmit: document.getElementById('support-submit'),
  };

  // ---- Menu rendering -------------------------------------------------

  async function loadMenu() {
    try {
      categories = await Api.get('/api/menu');
      renderMenu();
    } catch (err) {
      el.root.innerHTML = `<div class="empty-state">Couldn't load the menu. Pull to refresh or try again shortly.</div>`;
    }
  }

  function renderMenu() {
    el.root.innerHTML = '';
    categories.forEach(cat => {
      const badge = document.createElement('div');
      badge.className = 'section-badge';
      badge.style.setProperty('--section-image', `url("${categoryImages[cat.name.toLowerCase()] || fallbackImage}")`);
      badge.textContent = cat.name.toUpperCase();
      el.root.appendChild(badge);

      const grid = document.createElement('div');
      grid.className = 'menu-grid';

      cat.items.forEach(item => {
        grid.appendChild(renderMenuCard(item));
      });

      el.root.appendChild(grid);
    });

    if (categories.length === 0) {
      el.root.innerHTML = `<div class="empty-state">No menu items yet. Check back soon.</div>`;
    }
  }

  function renderMenuCard(item) {
    const card = document.createElement('div');
    card.className = 'menu-card' + (item.isAvailable ? '' : ' is-unavailable');

    const qty = cart.has(item.id) ? cart.get(item.id).qty : 0;

    card.innerHTML = `
      <div class="menu-card__image" style="background-image:url('${itemImage(item)}')"></div>
      <div class="menu-card__info">
        ${item.servingInfo ? `<span class="menu-card__serving">${item.servingInfo}</span><br/>` : ''}
        <p class="menu-card__name">${item.name}</p>
        ${item.description ? `<p class="menu-card__description">${item.description}</p>` : ''}
        <span class="menu-card__price">${money(item.price)}</span>
        ${!item.isAvailable ? '<div class="sold-out-tag">Sold out today</div>' : ''}
      </div>
      <div class="qty-stepper">
        <button type="button" data-action="dec">−</button>
        <span>${qty}</span>
        <button type="button" data-action="inc">+</button>
      </div>
    `;

    const qtyLabel = card.querySelector('.qty-stepper span');
    card.querySelector('[data-action="inc"]').addEventListener('click', () => {
      const current = cart.get(item.id) || { item, qty: 0 };
      current.qty += 1;
      cart.set(item.id, current);
      qtyLabel.textContent = current.qty;
      updateCartBar();
    });
    card.querySelector('[data-action="dec"]').addEventListener('click', () => {
      const current = cart.get(item.id);
      if (!current) return;
      current.qty -= 1;
      if (current.qty <= 0) cart.delete(item.id);
      else cart.set(item.id, current);
      qtyLabel.textContent = current.qty > 0 ? current.qty : 0;
      updateCartBar();
    });

    return card;
  }

  // ---- Cart -------------------------------------------------------------

  function cartTotal() {
    let total = 0, count = 0;
    cart.forEach(({ item, qty }) => { total += item.price * qty; count += qty; });
    return { total, count };
  }

  function updateCartBar() {
    const { total, count } = cartTotal();
    el.cartBar.classList.toggle('is-visible', count > 0);
    el.cartCount.textContent = `${count} item${count === 1 ? '' : 's'}`;
    el.cartTotal.textContent = money(total);
  }

  function renderCheckoutItems() {
    el.checkoutItems.innerHTML = '';
    cart.forEach(({ item, qty }) => {
      const row = document.createElement('div');
      row.className = 'menu-card';
      row.style.marginBottom = '8px';
      row.innerHTML = `
        <div class="menu-card__image" style="background-image:url('${itemImage(item)}')"></div>
        <div class="menu-card__info">
          <p class="menu-card__name">${item.name}</p>
          <span class="menu-card__price">${money(item.price * qty)}</span>
        </div>
        <div class="qty-stepper">
          <button type="button" data-action="dec">−</button>
          <span>${qty}</span>
          <button type="button" data-action="inc">+</button>
        </div>
      `;
      row.querySelector('[data-action="inc"]').addEventListener('click', () => {
        cart.get(item.id).qty += 1;
        renderCheckoutItems();
        renderMenu();
        updateCartBar();
        updateSubmitTotal();
      });
      row.querySelector('[data-action="dec"]').addEventListener('click', () => {
        const current = cart.get(item.id);
        current.qty -= 1;
        if (current.qty <= 0) cart.delete(item.id);
        renderCheckoutItems();
        renderMenu();
        updateCartBar();
        updateSubmitTotal();
      });
      el.checkoutItems.appendChild(row);
    });

    if (cart.size === 0) {
      el.checkoutItems.innerHTML = '<div class="empty-state">Your order is empty.</div>';
    }
  }

  function updateSubmitTotal() {
    const { total } = cartTotal();
    el.submitTotal.textContent = money(total);
  }

  // ---- Checkout sheet ------------------------------------------------

  function openCheckout() {
    renderCheckoutItems();
    updateSubmitTotal();
    el.checkoutBackdrop.classList.add('is-open');
    el.checkoutSheet.style.display = 'block';
  }
  function closeCheckout() {
    el.checkoutBackdrop.classList.remove('is-open');
    el.checkoutSheet.style.display = 'none';
  }

  el.openCheckout.addEventListener('click', openCheckout);
  el.checkoutClose.addEventListener('click', closeCheckout);
  el.checkoutBackdrop.addEventListener('click', closeCheckout);

  document.querySelectorAll('.channel-toggle button').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.channel-toggle button').forEach(b => b.classList.remove('is-active'));
      btn.classList.add('is-active');
      channel = btn.dataset.channel;
      el.addressField.style.display = channel === 'Delivery' ? 'block' : 'none';
    });
  });

  el.submitOrder.addEventListener('click', submitOrder);

  async function submitOrder() {
    el.checkoutError.textContent = '';

    const name = document.getElementById('cust-name').value.trim();
    const phone = document.getElementById('cust-phone').value.trim();
    const email = document.getElementById('cust-email').value.trim();
    const address = document.getElementById('cust-address').value.trim();
    const notes = document.getElementById('cust-notes').value.trim();
    if (cart.size === 0) { el.checkoutError.textContent = 'Add at least one item to your order.'; return; }
    if (!name) { el.checkoutError.textContent = 'Please enter your name.'; return; }
    if (!phone) { el.checkoutError.textContent = 'Please enter your phone number.'; return; }
    if (!email || !email.includes('@')) { el.checkoutError.textContent = 'Please enter a valid email address.'; return; }
    if (channel === 'Delivery' && !address) { el.checkoutError.textContent = 'Please enter a delivery address.'; return; }

    const payload = {
      customerName: name,
      phone,
      email,
      channel,
      deliveryAddress: channel === 'Delivery' ? address : null,
      notes: notes || null,
      items: Array.from(cart.values()).map(({ item, qty }) => ({
        menuItemId: item.id,
        quantity: qty,
      })),
    };

    el.submitOrder.disabled = true;
    el.submitOrder.textContent = 'Placing order…';

    try {
      const order = await Api.post('/api/orders', payload);
      cart.clear();
      updateCartBar();
      closeCheckout();
      openPaymentSheet(order);
    } catch (err) {
      el.checkoutError.textContent = err.message || 'Something went wrong. Please try again.';
    } finally {
      el.submitOrder.disabled = false;
      el.submitOrder.innerHTML = `Continue to payment — <span id="submit-total">${money(cartTotal().total)}</span>`;
    }
  }

  // ---- Payment and explicit kitchen submission -------------------------

  let currentOrder = null;
  let payMethod = 'Card';

  function renderPriceLines(order) {
    let html = `<div class="price-line"><span>Subtotal</span><span>${money(order.subtotal)}</span></div>`;
    if (order.discountAmount > 0) html += `<div class="price-line discount"><span>🎁 Loyalty reward</span><span>−${money(order.discountAmount)}</span></div>`;
    html += `<div class="price-line total"><span>Total due</span><span>${money(order.totalAmount)}</span></div>`;
    el.paymentPriceLines.innerHTML = html;
    el.paymentTotal.textContent = money(order.totalAmount);
  }

  function openPaymentSheet(order) {
    currentOrder = order;
    payMethod = 'Card';
    el.cardFields.style.display = 'block';
    el.cashNote.style.display = 'none';
    el.paymentError.textContent = '';
    el.paymentOrderNumber.textContent = order.orderNumber;
    renderPriceLines(order);
    el.paymentBackdrop.classList.add('is-open');
    el.paymentSheet.style.display = 'block';
  }

  function closePaymentSheet() {
    el.paymentBackdrop.classList.remove('is-open');
    el.paymentSheet.style.display = 'none';
  }

  el.paymentClose.addEventListener('click', closePaymentSheet);
  el.paymentBackdrop.addEventListener('click', closePaymentSheet);
  document.querySelectorAll('.pay-method-row button').forEach(btn => btn.addEventListener('click', () => {
    document.querySelectorAll('.pay-method-row button').forEach(b => b.classList.remove('is-active'));
    btn.classList.add('is-active');
    payMethod = btn.dataset.method;
    el.cardFields.style.display = payMethod === 'Card' ? 'block' : 'none';
    el.cashNote.style.display = payMethod === 'Card' ? 'none' : 'block';
  }));

  el.submitPayment.addEventListener('click', async () => {
    el.paymentError.textContent = '';
    if (!currentOrder) return;
    const payload = { orderId: currentOrder.id, method: payMethod };
    if (payMethod === 'Card') {
      const number = el.cardNumber.value.replace(/\s/g, '');
      const name = el.cardName.value.trim();
      const month = parseInt(el.cardMonth.value, 10);
      const year = parseInt(el.cardYear.value, 10);
      const cvv = el.cardCvv.value.trim();
      if (!number || number.length < 12) { el.paymentError.textContent = 'Enter a valid card number.'; return; }
      if (!name) { el.paymentError.textContent = 'Enter the name on the card.'; return; }
      if (!month || month < 1 || month > 12) { el.paymentError.textContent = 'Enter a valid expiry month.'; return; }
      if (!year || String(year).length !== 4) { el.paymentError.textContent = 'Enter a valid expiry year.'; return; }
      if (!cvv || cvv.length < 3) { el.paymentError.textContent = 'Enter a valid CVV.'; return; }
      Object.assign(payload, { cardNumber: number, cardHolderName: name, expiryMonth: month, expiryYear: year, cvv });
    }
    el.submitPayment.disabled = true;
    try {
      const result = await Api.post('/api/payments/charge', payload);
      if (!result.success) { el.paymentError.textContent = result.message || 'Payment failed. Please try again.'; return; }
      currentOrder = result.order;
      closePaymentSheet();
      openConfirmSheet(currentOrder);
    } catch (err) {
      el.paymentError.textContent = err.message || 'Payment failed. Please try again.';
    } finally {
      el.submitPayment.disabled = false;
    }
  });

  el.submitToKitchen.addEventListener('click', async () => {
    if (!currentOrder) return;
    el.confirmError.textContent = '';
    el.submitToKitchen.disabled = true;
    try {
      const order = await Api.post(`/api/orders/${currentOrder.id}/submit`, null);
      closeConfirmSheet();
      openTracker(order);
    } catch (err) {
      el.confirmError.textContent = err.message || 'Could not submit the order to the kitchen.';
    } finally {
      el.submitToKitchen.disabled = false;
    }
  });

  function openConfirmSheet(order) {
    el.confirmError.textContent = '';
    el.confirmOrderNumber.textContent = order.orderNumber;
    el.confirmItems.innerHTML = `<div class="price-line total"><span>Total paid</span><span>${money(order.totalAmount)}</span></div>`;
    el.confirmBackdrop.classList.add('is-open');
    el.confirmSheet.style.display = 'block';
  }

  function closeConfirmSheet() {
    el.confirmBackdrop.classList.remove('is-open');
    el.confirmSheet.style.display = 'none';
  }

  // ---- Order tracking ---------------------------------------------------

  const STEP_ORDER = ['Pending', 'Preparing', 'Ready', 'Completed'];

  function openTracker(order) {
    trackedOrderId = order.id;
    el.trackOrderNumber.textContent = order.orderNumber;
    updateTrackerUI(order.status);
    el.trackLoyalty.textContent = order.discountAmount > 0
      ? 'Your 50% loyalty reward was applied. Points balance: ' + (order.loyaltyPoints || 0)
      : `Loyalty progress: ${order.loyaltyOrderCount || 0}/7 orders. Earn 50% off your next reward order.`;

    el.trackBackdrop.classList.add('is-open');
    el.trackSheet.style.display = 'block';

    connectToOrderUpdates(order.id);
  }

  function updateTrackerUI(status) {
    const idx = STEP_ORDER.indexOf(status);
    document.querySelectorAll('.tracker__step').forEach((stepEl, i) => {
      stepEl.classList.toggle('is-done', i < idx);
      stepEl.classList.toggle('is-current', i === idx);
    });
    const fillPct = idx <= 0 ? 4 : (idx / (STEP_ORDER.length - 1)) * 100;
    el.trackerFill.style.width = fillPct + '%';

    const messages = {
      Pending: "We've got your order — the fire's already going.",
      Preparing: 'On the grill now 🔥 — smells good already.',
      Ready: 'Ready! Come grab it (or your driver is on the way).',
      Completed: 'Enjoy your meal! Thanks for ordering from Mnce Tpain.',
      Cancelled: 'This order was cancelled. Please contact the shop if that seems wrong.',
    };
    el.trackMessage.textContent = messages[status] || '';
  }

  async function connectToOrderUpdates(orderId) {
    if (typeof signalR === 'undefined') return; // library failed to load - tracker still works via the initial state above

    try {
      if (!hubConnection) {
        hubConnection = new signalR.HubConnectionBuilder()
          .withUrl('/hubs/orders')
          .withAutomaticReconnect()
          .build();

        hubConnection.on('OrderStatusChanged', (order) => {
          if (order.id === trackedOrderId) updateTrackerUI(order.status);
        });

        await hubConnection.start();
      }
      await hubConnection.invoke('JoinOrderGroup', orderId);
    } catch {
      // Live updates are a nice-to-have; the sheet already shows the status at open time.
    }
  }

  el.trackNewOrder.addEventListener('click', () => {
    el.trackBackdrop.classList.remove('is-open');
    el.trackSheet.style.display = 'none';
  });

  function closeSupport() {
    el.supportBackdrop.classList.remove('is-open');
    el.supportSheet.style.display = 'none';
  }
  el.openSupport.addEventListener('click', () => {
    el.supportError.textContent = '';
    el.supportBackdrop.classList.add('is-open');
    el.supportSheet.style.display = 'block';
  });
  el.supportClose.addEventListener('click', closeSupport);
  el.supportBackdrop.addEventListener('click', closeSupport);
  el.supportSubmit.addEventListener('click', async () => {
    const payload = {
      customerName: document.getElementById('support-name').value.trim(),
      phone: document.getElementById('support-phone').value.trim(),
      type: document.getElementById('support-type').value,
      description: document.getElementById('support-description').value.trim(),
      orderId: trackedOrderId
    };
    if (!payload.customerName || !payload.phone || !payload.description) {
      el.supportError.textContent = 'Please provide your name, phone, and message.';
      return;
    }
    el.supportSubmit.disabled = true;
    try {
      await Api.post('/api/support-calls', payload);
      closeSupport();
      showToast('Message sent to the shop.');
    } catch (err) {
      el.supportError.textContent = err.message || 'Unable to send your message.';
    } finally {
      el.supportSubmit.disabled = false;
    }
  });

  loadMenu();
})();
