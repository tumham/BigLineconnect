document.addEventListener('DOMContentLoaded', () => {
    const landingPage = document.getElementById('landing-page');
    const viewport = document.getElementById('viewport');
    const idInput = document.getElementById('lc-id-input');
    const connectBtn = document.getElementById('lc-connect-btn');
    const statusMsg = document.getElementById('lc-status');
    const canvas = document.getElementById('lc-canvas');
    const ctx = canvas.getContext('2d', { alpha: false, desynchronized: true });
    const kbBadge = document.getElementById('v-kb-badge');
    const disconnectBtn = document.getElementById('v-disconnect-btn');

    let socket = null;
    let connected = false;
    let lastMoveTime = 0;

    // Formatting ID input
    idInput.addEventListener('input', (e) => {
        let val = e.target.value.replace(/\D/g, '');
        if (val.length > 3) {
            val = val.substring(0, 3) + ' ' + val.substring(3, 6);
        }
        e.target.value = val;
    });

    connectBtn.addEventListener('click', () => {
        const rawId = idInput.value.replace(/\D/g, '');
        if (rawId.length !== 6) {
            statusMsg.textContent = '❌ Lütfen 6 haneli geçerli bir LightConnect ID giriniz.';
            statusMsg.style.color = '#e74c3c';
            return;
        }
        startLightConnection(rawId);
    });

    disconnectBtn.addEventListener('click', () => {
        if (socket) socket.close();
    });

    function startLightConnection(id) {
        connectBtn.disabled = true;
        connectBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> <span>Bağlanıyor...</span>';
        statusMsg.textContent = '🟡 Sunucuya bağlanılıyor...';
        statusMsg.style.color = '#f1c40f';

        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const wsUrl = `${protocol}//${window.location.host}/lc-client?id=${id}`;

        try {
            socket = new WebSocket(wsUrl);
            socket.binaryType = 'arraybuffer';

            socket.onopen = () => {
                connected = true;
                statusMsg.textContent = '🟢 Bağlandı! Ekran bekleniyor...';
                statusMsg.style.color = '#2ecc71';
            };

            socket.onmessage = (event) => {
                if (event.data instanceof ArrayBuffer) {
                    // Binary JPEG screen frame
                    const sizeKb = event.data.byteLength / 1024;
                    kbBadge.textContent = `${sizeKb.toFixed(0)} KB`;

                    if (viewport.classList.contains('hidden')) {
                        landingPage.classList.add('hidden');
                        viewport.classList.remove('hidden');
                    }

                    const blob = new Blob([event.data], { type: 'image/jpeg' });
                    const url = URL.createObjectURL(blob);
                    const img = new Image();
                    img.onload = () => {
                        if (canvas.width !== img.width || canvas.height !== img.height) {
                            canvas.width = img.width;
                            canvas.height = img.height;
                        }
                        ctx.drawImage(img, 0, 0);
                        URL.revokeObjectURL(url);
                    };
                    img.onerror = () => URL.revokeObjectURL(url);
                    img.src = url;
                } else if (typeof event.data === 'string') {
                    if (event.data === 'ERROR:NOT_FOUND') {
                        alert('❌ Uzak Masaüstü ID bulunamadı veya kapalı!');
                        socket.close();
                    }
                }
            };

            socket.onclose = () => {
                connected = false;
                viewport.classList.add('hidden');
                landingPage.classList.remove('hidden');
                connectBtn.disabled = false;
                connectBtn.innerHTML = '<span>Bağlan</span> <i class="fa-solid fa-bolt"></i>';
                statusMsg.textContent = '🔴 Bağlantı sonlandırıldı.';
                statusMsg.style.color = '#e74c3c';
            };

            socket.onerror = () => {
                statusMsg.textContent = '❌ Bağlantı Hatası!';
                statusMsg.style.color = '#e74c3c';
            };

        } catch (err) {
            connectBtn.disabled = false;
            connectBtn.innerHTML = '<span>Bağlan</span> <i class="fa-solid fa-bolt"></i>';
            statusMsg.textContent = '❌ Bağlantı hatası: ' + err.message;
        }
    }

    // Canvas Input Engine
    function getCanvasMousePos(e) {
        const rect = canvas.getBoundingClientRect();
        if (rect.width <= 0 || rect.height <= 0) return { x: 0, y: 0 };

        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const x = Math.max(0, Math.min(1, mouseX / rect.width));
        const y = Math.max(0, Math.min(1, mouseY / rect.height));

        return { x, y };
    }

    function sendClick(button, action, x, y) {
        if (connected && socket && socket.readyState === WebSocket.OPEN) {
            socket.send(JSON.stringify({ type: 'click', button, action, x, y }));
        }
    }

    function sendMove(x, y) {
        if (connected && socket && socket.readyState === WebSocket.OPEN) {
            socket.send(JSON.stringify({ type: 'move', x, y }));
        }
    }

    canvas.addEventListener('mousedown', (e) => {
        if (!connected) return;
        const pos = getCanvasMousePos(e);
        let button = e.button === 2 ? 'right' : (e.button === 1 ? 'middle' : 'left');
        sendClick(button, 'down', pos.x, pos.y);
        e.preventDefault();
    });

    canvas.addEventListener('mouseup', (e) => {
        if (!connected) return;
        const pos = getCanvasMousePos(e);
        let button = e.button === 2 ? 'right' : (e.button === 1 ? 'middle' : 'left');
        sendClick(button, 'up', pos.x, pos.y);
        e.preventDefault();
    });

    canvas.addEventListener('mousemove', (e) => {
        if (!connected) return;
        const now = performance.now();
        if (now - lastMoveTime < 16) return;
        lastMoveTime = now;

        const pos = getCanvasMousePos(e);
        sendMove(pos.x, pos.y);
        e.preventDefault();
    });

    canvas.addEventListener('contextmenu', (e) => e.preventDefault());

    // Toolbar Buttons
    document.getElementById('tb-esc').addEventListener('click', () => {
        if (connected && socket) socket.send(JSON.stringify({ type: 'key', key: 'escape', action: 'down' }));
    });
    document.getElementById('tb-tab').addEventListener('click', () => {
        if (connected && socket) socket.send(JSON.stringify({ type: 'key', key: 'tab', action: 'down' }));
    });
    document.getElementById('tb-cad').addEventListener('click', () => {
        if (connected && socket) socket.send(JSON.stringify({ type: 'key', key: 'delete', action: 'down' }));
    });

    // Keyboard Input
    window.addEventListener('keydown', (e) => {
        if (!connected || !socket) return;
        let keyName = e.key;
        if (keyName === ' ') keyName = 'space';
        socket.send(JSON.stringify({ type: 'key', key: keyName, action: 'down' }));
    });

    window.addEventListener('keyup', (e) => {
        if (!connected || !socket) return;
        let keyName = e.key;
        if (keyName === ' ') keyName = 'space';
        socket.send(JSON.stringify({ type: 'key', key: keyName, action: 'up' }));
    });
});
