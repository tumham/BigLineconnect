var socket = null;
var connected = false;
var currentMouseMode = 'left'; // 'left' or 'right'

// Zoom & Pan State
var scale = 1.0;
var panX = 0;
var panY = 0;
var startTouchDistance = 0;
var startScale = 1.0;
var startMidX = 0;
var startMidY = 0;
var startPanX = 0;
var startPanY = 0;

// DOM Element References (safely initialized)
var landingPage = null;
var viewerScreen = null;
var targetIdInput = null;
var connectBtn = null;
var disconnectBtn = null;
var fullscreenBtn = null;
var screenImg = null;
var canvasContainer = null;
var connectionStatus = null;
var toggleKeyboardBtn = null;
var hiddenKeyboardInput = null;
var mouseModeBtn = null;
var mouseModeText = null;
var toastElement = null;
var passwordModal = null;
var accessPasswordInput = null;
var submitPasswordBtn = null;

function initDOMElements() {
    landingPage = document.getElementById('landing-page');
    viewerScreen = document.getElementById('viewer-screen');
    targetIdInput = document.getElementById('target-id');
    connectBtn = document.getElementById('connect-btn');
    disconnectBtn = document.getElementById('disconnect-btn');
    fullscreenBtn = document.getElementById('fullscreen-btn');
    screenImg = document.getElementById('screen-img');
    canvasContainer = document.getElementById('canvas-container');
    connectionStatus = document.getElementById('connection-status');
    toggleKeyboardBtn = document.getElementById('toggle-keyboard-btn');
    hiddenKeyboardInput = document.getElementById('hidden-keyboard-input');
    mouseModeBtn = document.getElementById('mouse-mode-btn');
    mouseModeText = document.getElementById('mouse-mode-text');
    toastElement = document.getElementById('toast');
    passwordModal = document.getElementById('password-modal');
    accessPasswordInput = document.getElementById('access-password-input');
    submitPasswordBtn = document.getElementById('submit-password-btn');

    bindCanvasInteraction();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initDOMElements);
} else {
    initDOMElements();
}

// Dynamic Helper to Get Elements Safely
const getElem = (id) => document.getElementById(id);

// Toast system
function showToast(message, type = 'info') {
    if (!message || typeof message !== 'string') return;
    var trimmed = message.trim();
    if (trimmed.startsWith('{') && trimmed.endsWith('}')) return; // Never show raw JSON as toast or alert!

    var tElem = document.getElementById('toast');
    if (!tElem) {
        tElem = document.createElement('div');
        tElem.id = 'toast';
        document.body.appendChild(tElem);
    }
    tElem.textContent = message;
    tElem.className = 'toast ' + type;
    tElem.style.cssText = 'position:fixed;top:24px;left:50%;transform:translateX(-50%);z-index:9999999;padding:10px 20px;border-radius:12px;font-weight:700;font-size:13px;box-shadow:0 8px 24px rgba(0,0,0,0.8);background:' + (type === 'error' ? '#ef4444' : (type === 'success' ? '#10b981' : 'rgba(15,23,42,0.92)')) + ';color:#fff;border:1px solid rgba(0,229,255,0.5);backdrop-filter:blur(10px);pointer-events:none;transition:opacity 0.3s ease;opacity:1;';
    
    if (window._toastTimer) clearTimeout(window._toastTimer);
    window._toastTimer = setTimeout(function() {
        if (tElem) tElem.style.opacity = '0';
    }, 2800);
}

// Actions bound at end of file

let isConnectingProcess = false;

function resetConnectButton() {
    const btnElem = document.getElementById('connect-btn');
    if (btnElem) {
        btnElem.disabled = false;
        btnElem.style.pointerEvents = 'auto';
        btnElem.style.opacity = '1';
        btnElem.style.cursor = 'pointer';
        btnElem.innerHTML = '<span>Bağlan</span> <i class="fa-solid fa-arrow-right-to-bracket"></i>';
    }
}
window.resetConnectButton = resetConnectButton;

// Connect Button Event Handler
function startConnectionProcess() {
    console.log('[BigLineconnect] startConnectionProcess triggered');
    const inputElem = document.getElementById('target-id');
    const btnElem = document.getElementById('connect-btn');
    
    if (!inputElem) {
        alert('Hata: ID giriş kutusu bulunamadı.');
        return;
    }

    const rawId = inputElem.value.replace(/\D/g, '');
    if (!rawId || rawId.length < 5) {
        alert('⚠️ Lütfen bağlanmak istediğiniz uzaktaki bilgisayarın 9 haneli ID numarasını girin (Örn: 219 675 629).');
        resetConnectButton();
        return;
    }

    if (btnElem) {
        btnElem.disabled = true;
        btnElem.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> <span>Bağlanıyor...</span>';
    }

    showToast(`Masaüstü (${rawId}) aranıyor...`, 'info');

    connectToHost(rawId);

    // Re-enable button after 4 seconds safety timeout
    setTimeout(() => {
        if (!connected) {
            resetConnectButton();
        }
    }, 4000);
}
window.startConnectionProcess = startConnectionProcess;

// Disconnect Button Event
if (disconnectBtn) {
    disconnectBtn.addEventListener('click', () => {
        if (socket) {
            socket.close();
        }
    });
}

// Connect to C# Relay WebSockets (100% test.html Reference Engine)
function connectToHost(id) {
    const cleanId = String(id).replace(/\D/g, '');
    window.currentConnectedHostId = cleanId;
    window.closeAppSocket = function() {
        if (socket) {
            try { socket.close(); } catch(e) {}
        }
    };
    const wsProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const urlParams = new URLSearchParams(window.location.search || '');
    const ticketToken = urlParams.get('token') || urlParams.get('ticketToken') || '';
    let wsUrl = `${wsProtocol}//${window.location.host}/connect-client?id=${cleanId}`;
    if (ticketToken) {
        wsUrl += `&ticketToken=${encodeURIComponent(ticketToken)}`;
    }
    
    if (socket) {
        try { socket.close(); } catch(e) {}
    }

    showToast(`Masaüstü (${cleanId}) aranıyor...`, 'info');

    socket = new WebSocket(wsUrl);
    socket.binaryType = 'arraybuffer';

    socket.onopen = function() {
        connected = true;
        showToast('Sunucuya bağlandı! Doğrulama bekleniyor...', 'info');
    };

    socket.onmessage = function(ev) {
        if (typeof ev.data === 'string') {
            if (ev.data === 'ERROR:ID_NOT_FOUND') {
                resetConnectButton();
                alert('⚠️ UYARI: (' + cleanId + ') numaralı Masaüstü ID\'si sunucuda bulunamadı!\n\nLütfen bilgisayarınızdaki BigLineconnect.exe uygulamasının AÇIK olduğundan ve "BU CİHAZIN ID\'Sİ" bölümünde yazan 9 haneli kodu doğru girdiğinizden emin olun.');
                socket.close();
            } else if (ev.data === 'ERROR:BUSY') {
                resetConnectButton();
                alert('⚠️ UYARI: Bu bilgisayara şu an başka bir operatör bağlı.');
                socket.close();
            } else if (ev.data === 'AUTH_REQUIRED') {
                var pass = prompt('🔒 Lütfen karşı bilgisayarın ekranında yazan 6 haneli erişim şifresini girin:', '');
                if (pass) {
                    socket.send(pass);
                } else {
                    resetConnectButton();
                    socket.close();
                }
            } else if (ev.data === 'AUTH_SUCCESS') {
                showToast('Erişim Onaylandı! Ekran ve Kontrol Açılıyor...', 'success');
                try {
                    if (socket && socket.readyState === WebSocket.OPEN) {
                        socket.send(JSON.stringify({ type: 'set_quality', quality: 85, maxDim: 0 }));
                    }
                } catch(e) {}
                const landing = document.getElementById('landing-page');
                const viewer = document.getElementById('viewer-screen');
                if (landing) landing.style.setProperty('display', 'none', 'important');
                if (viewer) {
                    viewer.style.setProperty('display', 'flex', 'important');
                    viewer.style.setProperty('pointer-events', 'auto', 'important');
                    viewer.style.setProperty('z-index', '999999', 'important');
                    setTimeout(() => { if (typeof window.updateCanvasCssLayout === 'function') window.updateCanvasCssLayout(); }, 100);
                }
            } else {
                showToast(ev.data, 'info');
            }
        } else if (ev.data instanceof ArrayBuffer) {
            const landing = document.getElementById('landing-page');
            const viewer = document.getElementById('viewer-screen');
            if (landing && landing.style.display !== 'none') {
                landing.style.setProperty('display', 'none', 'important');
            }
            if (viewer && viewer.style.display !== 'flex') {
                viewer.style.setProperty('display', 'flex', 'important');
                viewer.style.setProperty('pointer-events', 'auto', 'important');
                viewer.style.setProperty('z-index', '999999', 'important');
            }

            const screenCanvas = document.getElementById('screen-canvas');
            const ctx = screenCanvas ? screenCanvas.getContext('2d', { alpha: false, desynchronized: true }) : null;
            const fallbackImg = document.getElementById('screen-img');

            let frameBytes = ev.data;
            if (frameBytes && frameBytes.byteLength > 12) {
                const u8 = new Uint8Array(frameBytes);
                if (u8.length >= 20) {
                    const dv = new DataView(frameBytes);
                    const seq = dv.getUint32(8, true);
                    const ackPkt = new Uint8Array([0x41, seq & 0xFF, (seq >> 8) & 0xFF, (seq >> 16) & 0xFF, (seq >> 24) & 0xFF]);
                    if (socket && socket.readyState === WebSocket.OPEN) {
                        try { socket.send(ackPkt.buffer); } catch (e) {}
                    }
                }

                // Check for BigLine-RT (BRTE) Tile/Delta Packet: 'B' 0x42, 'R' 0x52, 'T' 0x54, 'E' 0x45
                if (u8[0] === 0x42 && u8[1] === 0x52 && u8[2] === 0x54 && u8[3] === 0x45) {
                    const dv = new DataView(frameBytes);
                    const flag = u8[4];
                    const w = dv.getUint16(5, true);
                    const h = dv.getUint16(7, true);

                    if (screenCanvas && (screenCanvas.width !== w || screenCanvas.height !== h)) {
                        screenCanvas.width = w;
                        screenCanvas.height = h;
                    }

                    if (flag === 1) { // KEYFRAME
                        const len = dv.getInt32(9, true);
                        const jpegBytes = frameBytes.slice(13, 13 + len);
                        const blob = new Blob([jpegBytes], { type: 'image/jpeg' });
                        const url = URL.createObjectURL(blob);
                        const img = new Image();
                        img.onload = () => {
                            if (ctx) ctx.drawImage(img, 0, 0);
                            URL.revokeObjectURL(url);
                        };
                        img.onerror = () => URL.revokeObjectURL(url);
                        img.src = url;
                        return;
                    } else if (flag === 2) { // DIRTY TILES
                        const tileCount = dv.getUint16(9, true);
                        let offset = 11;
                        for (let t = 0; t < tileCount; t++) {
                            if (offset + 8 > u8.length) break;
                            const col = dv.getUint16(offset, true);
                            const row = dv.getUint16(offset + 2, true);
                            const tileLen = dv.getInt32(offset + 4, true);
                            offset += 8;
                            if (offset + tileLen > u8.length) break;

                            const tileBytes = frameBytes.slice(offset, offset + tileLen);
                            offset += tileLen;

                            const tileBlob = new Blob([tileBytes], { type: 'image/jpeg' });
                            const tileUrl = URL.createObjectURL(tileBlob);
                            const tileImg = new Image();
                            const destX = col * 64;
                            const destY = row * 64;
                            tileImg.onload = () => {
                                if (ctx) ctx.drawImage(tileImg, destX, destY);
                                URL.revokeObjectURL(tileUrl);
                            };
                            tileImg.onerror = () => URL.revokeObjectURL(tileUrl);
                            tileImg.src = tileUrl;
                        }
                        return;
                    }
                }
            }

            let receivedSeq = 0;
            if (frameBytes && frameBytes.byteLength >= 12) {
                const u8 = new Uint8Array(frameBytes);
                if (u8[12] === 0xFF && u8[13] === 0xD8) {
                    const dv = new DataView(frameBytes);
                    receivedSeq = dv.getUint32(8, true);
                    frameBytes = frameBytes.slice(12);
                } else {
                    for (let i = 0; i < Math.min(16, u8.length - 1); i++) {
                        if (u8[i] === 0xFF && u8[i + 1] === 0xD8) {
                            if (i === 12) {
                                const dv = new DataView(frameBytes);
                                receivedSeq = dv.getUint32(8, true);
                            }
                            if (i > 0) frameBytes = frameBytes.slice(i);
                            break;
                        }
                    }
                }
            }

            const blob = new Blob([frameBytes], { type: 'image/jpeg' });
            const url = URL.createObjectURL(blob);

            const tempImg = new Image();
            tempImg.onload = () => {
                if (screenCanvas && ctx) {
                    if (screenCanvas.width !== tempImg.width || screenCanvas.height !== tempImg.height) {
                        screenCanvas.width = tempImg.width;
                        screenCanvas.height = tempImg.height;
                        if (typeof window.updateCanvasCssLayout === 'function') {
                            window.updateCanvasCssLayout();
                        }
                    }
                    ctx.imageSmoothingEnabled = true;
                    ctx.imageSmoothingQuality = 'high';
                    ctx.drawImage(tempImg, 0, 0);
                    if (fallbackImg && fallbackImg.style.display !== 'none') {
                        fallbackImg.style.display = 'none';
                    }
                } else if (fallbackImg) {
                    fallbackImg.src = url;
                    if (fallbackImg.style.display === 'none') {
                        fallbackImg.style.display = 'block';
                    }
                }
                URL.revokeObjectURL(url);

                // Send 0ms Frame ACK to Host for 3G/5Mbps backpressure control
                if (receivedSeq > 0 && socket && socket.readyState === WebSocket.OPEN) {
                    const ackPkt = new Uint8Array(5);
                    ackPkt[0] = 0x41; // 'A'
                    ackPkt[1] = receivedSeq & 0xFF;
                    ackPkt[2] = (receivedSeq >> 8) & 0xFF;
                    ackPkt[3] = (receivedSeq >> 16) & 0xFF;
                    ackPkt[4] = (receivedSeq >> 24) & 0xFF;
                    try { socket.send(ackPkt.buffer); } catch (e) {}
                }
            };
            tempImg.onerror = () => URL.revokeObjectURL(url);
            tempImg.src = url;
        }
    };

    socket.onerror = function(err) {
        console.error('WebSocket Error:', err);
        resetConnectButton();
        showToast('Sunucu bağlantı hatası! Lütfen tekrar deneyin.', 'error');
    };

    socket.onclose = function() {
        connected = false;
        resetConnectButton();
        const landing = document.getElementById('landing-page');
        const viewer = document.getElementById('viewer-screen');
        if (viewer) viewer.style.setProperty('display', 'none', 'important');
        if (landing) landing.style.setProperty('display', 'block', 'important');
        showToast('Bağlantı sonlandırıldı.', 'info');
        socket = null;
    };
}

// Fullscreen Button
if (fullscreenBtn) {
    fullscreenBtn.addEventListener('click', () => {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen()
                .then(() => {
                    fullscreenBtn.innerHTML = '<i class="fa-solid fa-compress"></i>';
                })
                .catch(err => {
                    showToast('Tam ekrana geçilemedi!', 'error');
                });
        } else {
            document.exitFullscreen()
                .then(() => {
                    fullscreenBtn.innerHTML = '<i class="fa-solid fa-expand"></i>';
                });
        }
    });
}

// Mouse Mode Toggler (Sol Tık / Çift Tık ⚡ / Sağ Tık 🔴)
if (mouseModeBtn) {
    const handleMouseModeSwitch = (e) => {
        if (e) e.preventDefault();
        if (currentMouseMode === 'left') {
            currentMouseMode = 'double';
            mouseModeBtn.style.background = 'rgba(0, 229, 255, 0.2)';
            mouseModeBtn.style.color = '#00e5ff';
            mouseModeBtn.style.borderColor = 'rgba(0, 229, 255, 0.5)';
            if (mouseModeText) mouseModeText.textContent = 'Çift Tık ⚡';
            showToast('Dokunma Modu: Çift Tıklama ⚡', 'info');
        } else if (currentMouseMode === 'double') {
            currentMouseMode = 'right';
            mouseModeBtn.style.background = 'rgba(231, 76, 60, 0.2)';
            mouseModeBtn.style.color = '#e74c3c';
            mouseModeBtn.style.borderColor = 'rgba(231, 76, 60, 0.4)';
            if (mouseModeText) mouseModeText.textContent = 'Sağ Tık 🔴';
            showToast('Dokunma Modu: Sağ Tıklama 🔴', 'info');
        } else {
            currentMouseMode = 'left';
            mouseModeBtn.style.background = '';
            mouseModeBtn.style.color = '';
            mouseModeBtn.style.borderColor = '';
            if (mouseModeText) mouseModeText.textContent = 'Sol Tık';
            showToast('Dokunma Modu: Sol Tıklama', 'info');
        }
    };

    mouseModeBtn.addEventListener('click', handleMouseModeSwitch);
    mouseModeBtn.addEventListener('touchstart', handleMouseModeSwitch);
}

// Keyboard Button for Mobile
if (toggleKeyboardBtn) {
    const triggerKeyboard = (e) => {
        if (e) e.preventDefault();
        if (passwordModal && !passwordModal.classList.contains('hidden')) return;
        if (hiddenKeyboardInput) {
            hiddenKeyboardInput.focus();
            hiddenKeyboardInput.click();
        }
        showToast('Klavye aktif hale getirildi.', 'success');
    };
    toggleKeyboardBtn.addEventListener('click', triggerKeyboard);
    toggleKeyboardBtn.addEventListener('touchstart', triggerKeyboard);
}

// hidden keyboard input handling
if (hiddenKeyboardInput) {
    hiddenKeyboardInput.addEventListener('keydown', (e) => {
        if (passwordModal && !passwordModal.classList.contains('hidden')) return;
        if (!connected || !socket) return;
        
        sendKey(e.key, 'down');
        setTimeout(() => sendKey(e.key, 'up'), 40);
    });

    hiddenKeyboardInput.addEventListener('input', () => {
        if (passwordModal && !passwordModal.classList.contains('hidden')) return;
        hiddenKeyboardInput.value = '';
    });
}

// Helper functions to send inputs via WS
function sendMove(x, y) {
    const s = (socket && socket.readyState === WebSocket.OPEN) ? socket : (window.ws && window.ws.readyState === WebSocket.OPEN ? window.ws : null);
    if (s) {
        s.send(JSON.stringify({ type: 'move', x, y }));
    }
}

function sendClick(button, action, x = null, y = null) {
    const s = (socket && socket.readyState === WebSocket.OPEN) ? socket : (window.ws && window.ws.readyState === WebSocket.OPEN ? window.ws : null);
    if (s) {
        const payload = { type: 'click', button, action };
        if (x !== null && y !== null) {
            payload.x = x;
            payload.y = y;
        }
        s.send(JSON.stringify(payload));
    }
}

function sendDoubleClick(button = 'left', x = null, y = null) {
    const s = (socket && socket.readyState === WebSocket.OPEN) ? socket : (window.ws && window.ws.readyState === WebSocket.OPEN ? window.ws : null);
    if (s) {
        const payload = { type: 'double_click', button };
        if (x !== null && y !== null) {
            payload.x = x;
            payload.y = y;
        }
        s.send(JSON.stringify(payload));
    }
}

function sendScroll(deltaY) {
    const s = (socket && socket.readyState === WebSocket.OPEN) ? socket : (window.ws && window.ws.readyState === WebSocket.OPEN ? window.ws : null);
    if (s) {
        s.send(JSON.stringify({ type: 'scroll', deltaY }));
    }
}

function sendKey(key, action) {
    const s = (socket && socket.readyState === WebSocket.OPEN) ? socket : (window.ws && window.ws.readyState === WebSocket.OPEN ? window.ws : null);
    if (s) {
        s.send(JSON.stringify({ type: 'key', key, action }));
    }
}

// Mouse coordinate normalization (Universal, never returns NaN)
function getMousePos(a, b, c) {
    if (typeof window.getMousePos === 'function' && window.getMousePos !== getMousePos) {
        return window.getMousePos(a, b, c);
    }
    const clientX = (typeof c === 'number') ? b : a;
    const clientY = (typeof c === 'number') ? c : b;
    const elem = document.getElementById('screen-canvas') || document.getElementById('canvas-container');
    if (!elem) return { x: 0.5, y: 0.5 };
    const rect = elem.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return { x: 0.5, y: 0.5 };
    const normX = (clientX - rect.left) / rect.width;
    const normY = (clientY - rect.top) / rect.height;
    return {
        x: Math.max(0.0, Math.min(1.0, isNaN(normX) ? 0.5 : normX)),
        y: Math.max(0.0, Math.min(1.0, isNaN(normY) ? 0.5 : normY))
    };
}
window.getMousePos = getMousePos;

// Global Desktop Physical Keyboard Listeners
window.addEventListener('keydown', (e) => {
    if (!connected || !socket) return;
    if (passwordModal && !passwordModal.classList.contains('hidden')) return;
    if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA')) return;

    let keyName = e.key;
    if (keyName === ' ') keyName = 'space';
    sendKey(keyName, 'down');
    e.preventDefault();
});

window.addEventListener('keyup', (e) => {
    if (!connected || !socket) return;
    if (passwordModal && !passwordModal.classList.contains('hidden')) return;
    if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA')) return;

    let keyName = e.key;
    if (keyName === ' ') keyName = 'space';
    sendKey(keyName, 'up');
    e.preventDefault();
});

// Canvas interaction is handled authoritatively by index.html
function bindCanvasInteraction() {
    // No-op: index.html handles canvas and mobile touch interaction cleanly
}

// Global Key Listeners
window.addEventListener('keydown', (e) => {
    if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable)) return;
    if (passwordModal && !passwordModal.classList.contains('hidden')) return;
    if (!connected) return;
    
    sendKey(e.key, 'down');
    e.preventDefault();
});

window.addEventListener('keyup', (e) => {
    if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable)) return;
    if (passwordModal && !passwordModal.classList.contains('hidden')) return;
    if (!connected) return;
    
    sendKey(e.key, 'up');
    e.preventDefault();
});

// Floating buttons key injection with iOS Touch Support
document.querySelectorAll('.special-key').forEach(button => {
    const handleKey = (action) => {
        if (!connected) return;
        const key = button.getAttribute('data-key');
        sendKey(key, action);
    };

    button.addEventListener('mousedown', (e) => { e.preventDefault(); handleKey('down'); });
    button.addEventListener('mouseup', (e) => { e.preventDefault(); handleKey('up'); });
    button.addEventListener('touchstart', (e) => { e.preventDefault(); handleKey('down'); });
    button.addEventListener('touchend', (e) => { e.preventDefault(); handleKey('up'); });
});

// CAD Action
const cadBtn = document.getElementById('btn-ctrl-alt-del');
if (cadBtn) {
    const triggerCad = (e) => {
        if (e) e.preventDefault();
        if (!connected) return;
        showToast('Ctrl + Alt + Del gönderildi.', 'info');
        const s = (socket && socket.readyState === WebSocket.OPEN) ? socket : (window.ws && window.ws.readyState === WebSocket.OPEN ? window.ws : null);
        if (s) {
            s.send(JSON.stringify({ type: 'send_cad' }));
        }
        sendKey('control', 'down');
        sendKey('alt', 'down');
        sendKey('delete', 'down');
        setTimeout(() => {
            sendKey('delete', 'up');
            sendKey('alt', 'up');
            sendKey('control', 'up');
        }, 100);
    };
    cadBtn.addEventListener('click', triggerCad);
    cadBtn.addEventListener('touchstart', triggerCad);
}

// On-Screen Touch Numpad Actions with Instant Touch Feedback & Auto-Submit
document.querySelectorAll('.numpad-btn').forEach(btn => {
    const handlePress = (e) => {
        if (e) {
            e.preventDefault();
            e.stopPropagation();
        }
        btn.classList.add('pressed');
        setTimeout(() => btn.classList.remove('pressed'), 150);

        const val = btn.getAttribute('data-val');
        if (!accessPasswordInput) return;

        if (val === 'clear') {
            accessPasswordInput.value = '';
        } else if (val === 'backspace') {
            accessPasswordInput.value = accessPasswordInput.value.slice(0, -1);
        } else {
            if (accessPasswordInput.value.length < 6) {
                accessPasswordInput.value += val;
            }
        }

        // Auto-Submit when 6th digit is pressed
        if (accessPasswordInput.value.length === 6) {
            setTimeout(sendPassword, 180);
        }
    };

    btn.addEventListener('touchstart', handlePress, { passive: false });
    btn.addEventListener('click', (e) => {
        if (e.detail !== 0 && !('ontouchstart' in window)) {
            handlePress(e);
        }
    });
});

// Password submit actions
function sendPassword() {
    const inputElem = document.getElementById('access-password-input');
    const btnElem = document.getElementById('submit-password-btn');

    if (!inputElem) return;
    const pass = inputElem.value.replace(/\D/g, '');

    if (!pass || pass.length === 0) {
        showToast('Lütfen uzaktaki bilgisayarın 6 haneli şifresini giriniz.', 'error');
        return;
    }

    if (!socket || socket.readyState !== WebSocket.OPEN) {
        showToast('Sunucuya yeniden bağlanılıyor...', 'info');
        const rawId = targetIdInput ? targetIdInput.value.replace(/\D/g, '') : '';
        if (rawId) {
            connectToHost(rawId);
            setTimeout(() => {
                if (socket && socket.readyState === WebSocket.OPEN) {
                    socket.send("AUTH_PASS:" + pass);
                }
            }, 1000);
        }
        return;
    }

    if (btnElem) {
        btnElem.disabled = true;
        btnElem.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> <span>Doğrulanıyor...</span>';
    }

    socket.send("AUTH_PASS:" + pass);
    showToast('Şifre gönderildi, doğrulanıyor...', 'info');

    setTimeout(() => {
        if (btnElem) {
            btnElem.disabled = false;
            btnElem.innerHTML = 'Doğrula & Bağlan <i class="fa-solid fa-arrow-right-to-bracket"></i>';
        }
    }, 3000);
}

if (submitPasswordBtn) {
    submitPasswordBtn.addEventListener('click', (e) => {
        if (e) e.preventDefault();
        sendPassword();
    });
    submitPasswordBtn.addEventListener('touchend', (e) => {
        if (e) e.preventDefault();
        sendPassword();
    });
}

if (accessPasswordInput) {
    accessPasswordInput.addEventListener('input', () => {
        accessPasswordInput.value = accessPasswordInput.value.replace(/\D/g, '').slice(0, 6);
    });
    accessPasswordInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') sendPassword();
    });
}

// Clipboard sync
document.addEventListener('copy', () => {
    setTimeout(() => {
        navigator.clipboard.readText().then(text => {
            if (socket && socket.readyState === WebSocket.OPEN && text) {
                socket.send(JSON.stringify({ type: 'clipboard', text: text }));
            }
        }).catch(err => {});
    }, 100);
});

window.addEventListener('focus', () => {
    if (!connected) return;
    navigator.clipboard.readText().then(text => {
        if (socket && socket.readyState === WebSocket.OPEN && text) {
            socket.send(JSON.stringify({ type: 'clipboard', text: text }));
        }
    }).catch(err => {});
});

function updateTransform() {
    if (!screenImg || !canvasContainer) return;
    if (scale <= 1.0) {
        scale = 1.0;
        panX = 0;
        panY = 0;
    } else {
        const rect = canvasContainer.getBoundingClientRect();
        const maxPanX = (rect.width * (scale - 1)) / 2;
        const minPanX = -maxPanX;
        const maxPanY = (rect.height * (scale - 1)) / 2;
        const minPanY = -maxPanY;
        
        panX = Math.max(minPanX, Math.min(maxPanX, panX));
        panY = Math.max(minPanY, Math.min(maxPanY, panY));
    }
    screenImg.style.transformOrigin = 'center center';
    screenImg.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
}

// 1. Magic Link Auto-Detect (?id=393215720 or ?remoteid=...)
function checkMagicLink() {
    try {
        const search = window.location.search || '';
        const hash = window.location.hash || '';
        const fullHref = window.location.href || '';
        let magicId = '';

        // 1. Regex match for ?id=, &id=, #id=, ?remoteid=, ?hostid=, ?target=
        const match = fullHref.match(/[?&#](?:id|remoteid|hostid|target)=([0-9\s]+)/i);
        if (match && match[1]) {
            magicId = match[1];
        }

        // 2. Query fallback
        if (!magicId && search) {
            const urlParams = new URLSearchParams(search);
            magicId = urlParams.get('id') || urlParams.get('remoteid') || urlParams.get('hostid') || urlParams.get('target') ||
                      urlParams.get('ID') || urlParams.get('Id') || '';
        }
        if (!magicId && hash) {
            const qIdx = hash.indexOf('?');
            const hashQuery = qIdx !== -1 ? hash.substring(qIdx) : ('?' + hash.substring(1));
            const hParams = new URLSearchParams(hashQuery);
            magicId = hParams.get('id') || hParams.get('remoteid') || hParams.get('hostid') || hParams.get('target') ||
                      hParams.get('ID') || hParams.get('Id') || '';
        }

        const inputElem = document.getElementById('target-id') || targetIdInput;
        if (magicId && inputElem) {
            const clean = String(magicId).replace(/\D/g, '');
            if (clean.length >= 4) {
                if (clean.length === 9) {
                    inputElem.value = clean.substring(0, 3) + ' ' + clean.substring(3, 6) + ' ' + clean.substring(6);
                } else {
                    inputElem.value = clean;
                }
                
                // Switch to client connect tab if needed
                if (typeof switchConnectTab === 'function') {
                    switchConnectTab('client');
                }

                // Hide auto-suggestions so connect button is never blocked
                const suggestBox = document.getElementById('id-suggestions-box');
                if (suggestBox) {
                    suggestBox.style.display = 'none';
                    suggestBox.style.pointerEvents = 'none';
                }

                // Scroll to connect widget
                setTimeout(() => {
                    document.getElementById('baglan')?.scrollIntoView({ behavior: 'smooth' });
                }, 150);

                // Auto-trigger connection seamlessly from Telegram / Magic Link!
                if (!window._magicAutoConnected) {
                    window._magicAutoConnected = true;
                    showToast('🚀 Otomatik Bağlanılıyor: ' + inputElem.value, 'info');
                    setTimeout(() => {
                        if (typeof doConnect === 'function') {
                            doConnect();
                        } else if (typeof startConnectionProcess === 'function') {
                            startConnectionProcess();
                        }
                    }, 350);
                }
            }
        }
    } catch(err) {
        console.warn('[app.js] checkMagicLink error:', err);
    }
}

// 2. WhatsApp Share Generator
function shareViaWhatsApp() {
    const inputElem = document.getElementById('target-id');
    const idVal = inputElem ? inputElem.value.replace(/\D/g, '') : '';
    let text = '';
    if (!idVal || idVal.length < 5) {
        text = encodeURIComponent(`Merhaba, BigLineconnect Uzak Masaüstü Bağlantı Sistemi:\nhttps://biglineconnect.bigus.com.tr/`);
    } else {
        const magicUrl = `https://biglineconnect.bigus.com.tr/?id=${idVal}`;
        text = encodeURIComponent(`Merhaba, BigLineconnect Uzak Masaüstü Bağlantı Bilgilerim:\nID: ${idVal}\nTek tıkla bağlanmak için linke tıklayın:\n${magicUrl}`);
    }
    const waUrl = `https://api.whatsapp.com/send?text=${text}`;
    try {
        window.open(waUrl, '_blank');
    } catch(e) {
        window.location.href = waUrl;
    }
}

// 3. One-Click Copy Magic Link
function copyMagicLink() {
    const inputElem = document.getElementById('target-id');
    const idVal = inputElem ? inputElem.value.replace(/\D/g, '') : '';
    const magicUrl = idVal && idVal.length >= 5 ? `https://biglineconnect.bigus.com.tr/?id=${idVal}` : `https://biglineconnect.bigus.com.tr/`;
    
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(magicUrl).then(() => {
            showToast('Sihirli Bağlantı Linki Panoya Kopyalandı!', 'success');
            alert('✅ Sihirli Bağlantı Linki Kopyalandı:\n\n' + magicUrl);
        }).catch(() => {
            alert('📋 Bağlantı Linki:\n\n' + magicUrl);
        });
    } else {
        alert('📋 Bağlantı Linki:\n\n' + magicUrl);
    }
}

if (document.readyState === 'complete' || document.readyState === 'interactive') {
    setTimeout(checkMagicLink, 50);
} else {
    document.addEventListener('DOMContentLoaded', () => {
        checkMagicLink();
    });
}
window.addEventListener('load', checkMagicLink);

// 4. Tab Switcher Logic
function switchConnectTab(mode) {
    const btnClient = document.getElementById('tab-btn-client');
    const btnHost = document.getElementById('tab-btn-host');
    const contentClient = document.getElementById('tab-content-client');
    const contentHost = document.getElementById('tab-content-host');

    if (mode === 'client') {
        btnClient.style.background = 'var(--accent-gradient)';
        btnClient.style.color = '#000';
        btnClient.style.fontWeight = '800';

        btnHost.style.background = 'transparent';
        btnHost.style.color = '#a0aec0';
        btnHost.style.fontWeight = '700';

        contentClient.style.display = 'block';
        contentHost.style.display = 'none';
    } else {
        btnHost.style.background = 'var(--accent-gradient)';
        btnHost.style.color = '#000';
        btnHost.style.fontWeight = '800';

        btnClient.style.background = 'transparent';
        btnClient.style.color = '#a0aec0';
        btnClient.style.fontWeight = '700';

        contentHost.style.display = 'block';
        contentClient.style.display = 'none';
    }
}

// 5. Web Host Screen Sharing Engine (MediaDevices getDisplayMedia)
let webHostMediaStream = null;
let webHostSocket = null;
let webHostInterval = null;
let myWebHostId = '';
let myWebHostPass = '';

async function startWebScreenShare() {
    try {
        webHostMediaStream = await navigator.mediaDevices.getDisplayMedia({
            video: { frameRate: { max: 30 }, cursor: "always" },
            audio: false
        });

        // Generate temporary 9-digit Web ID & Password
        myWebHostId = Math.floor(100000000 + Math.random() * 900000000).toString();
        myWebHostPass = Math.floor(1000 + Math.random() * 9000).toString();

        // Connect to Relay server as a Host so the ID is registered in ActiveHosts!
        const wsProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const hostWsUrl = `${wsProtocol}//${window.location.host}/register-host?requested_id=${myWebHostId}&hwid=WEB_${myWebHostId}&computer_name=WebClient&username=WebUser&os=Browser&version=1.0`;

        webHostSocket = new WebSocket(hostWsUrl);
        webHostSocket.binaryType = 'arraybuffer';

        webHostSocket.onopen = () => {
            console.log("[WebHost] Connected to Relay. ID:", myWebHostId);
            showToast('Ekran paylaşımı sunucuya bağlandı!', 'success');
        };

        const hiddenCanvas = document.createElement('canvas');
        const hiddenCtx = hiddenCanvas.getContext('2d');
        const videoElement = document.createElement('video');
        videoElement.srcObject = webHostMediaStream;
        videoElement.play();

        webHostSocket.onmessage = (e) => {
            if (typeof e.data === 'string') {
                console.log("[WebHost Msg]:", e.data);
                if (e.data.startsWith("AUTH_PASS:")) {
                    const passInput = e.data.substring(10).trim();
                    if (passInput === myWebHostPass) {
                        webHostSocket.send("AUTH_OK");
                    } else {
                        webHostSocket.send("AUTH_FAIL");
                    }
                }
            }
        };

        // Frame capture loop (~15 FPS)
        webHostInterval = setInterval(() => {
            if (!webHostMediaStream || !webHostSocket || webHostSocket.readyState !== WebSocket.OPEN) return;
            if (!videoElement.videoWidth || !videoElement.videoHeight) return;

            hiddenCanvas.width = videoElement.videoWidth;
            hiddenCanvas.height = videoElement.videoHeight;
            hiddenCtx.drawImage(videoElement, 0, 0, hiddenCanvas.width, hiddenCanvas.height);

            hiddenCanvas.toBlob((blob) => {
                if (blob && webHostSocket && webHostSocket.readyState === WebSocket.OPEN) {
                    blob.arrayBuffer().then(buf => {
                        webHostSocket.send(buf);
                    });
                }
            }, 'image/jpeg', 0.6);
        }, 66);

        document.getElementById('my-web-id-display').innerText = `${myWebHostId.substring(0,3)} ${myWebHostId.substring(3,6)} ${myWebHostId.substring(6)}`;
        document.getElementById('my-web-pass-display').innerText = myWebHostPass;

        document.getElementById('web-host-idle-box').style.display = 'none';
        document.getElementById('web-host-active-box').style.display = 'block';

        // Handle user stopping screen share from browser banner
        webHostMediaStream.getVideoTracks()[0].onended = () => {
            stopWebScreenShare();
        };

    } catch (err) {
        console.warn("Ekran paylaşımı başlatılamadı:", err);
        showToast('Ekran paylaşım izni reddedildi veya desteklenmiyor.', 'error');
    }
}

function stopWebScreenShare() {
    if (webHostMediaStream) {
        webHostMediaStream.getTracks().forEach(track => track.stop());
        webHostMediaStream = null;
    }
    if (webHostInterval) {
        clearInterval(webHostInterval);
        webHostInterval = null;
    }
    if (webHostSocket) {
        webHostSocket.close();
        webHostSocket = null;
    }

    document.getElementById('web-host-idle-box').style.display = 'block';
    document.getElementById('web-host-active-box').style.display = 'none';
    showToast('Ekran paylaşımı durduruldu.', 'info');
}

function shareMyWebIdWhatsApp() {
    if (!myWebHostId) return;
    const magicUrl = `https://biglineconnect.bigus.com.tr/?id=${myWebHostId}`;
    const text = encodeURIComponent(`Merhaba, BigLineconnect Web Destek ID'm: ${myWebHostId} | Şifre: ${myWebHostPass}\nTek tıkla bağlanın:\n${magicUrl}`);
    window.open(`https://wa.me/?text=${text}`, '_blank');
}

// 6. Free Web Session Timer Engine (5 Min + Extra Minutes Extension)
let sessionTimerInterval = null;
let sessionRemainingSeconds = 300; // 5 minutes
let sessionExtensionUsed = false;

function isUserLicensed() {
    if (sessionStorage.getItem('adminToken') === 'authenticated') return true;
    if (document.cookie.includes('bigline_admin_session')) return true;
    if (sessionStorage.getItem('bayiToken') === 'authenticated') return true;
    return false;
}

function startFreeSessionTimer() {
    if (isUserLicensed()) {
        console.log("[SessionTimer] User is LICENSED. Unlimited 7/24 session enabled.");
        return;
    }

    sessionRemainingSeconds = 300; // 5 minutes
    sessionExtensionUsed = false;
    if (sessionTimerInterval) clearInterval(sessionTimerInterval);

    console.log("[SessionTimer] Free trial timer started (5 minutes).");

    sessionTimerInterval = setInterval(() => {
        sessionRemainingSeconds--;

        // Show warning modal when 60 seconds remain
        if (sessionRemainingSeconds <= 60 && sessionRemainingSeconds > 0) {
            const modal = document.getElementById('session-timer-modal');
            const secDisplay = document.getElementById('session-timer-seconds');
            if (modal) modal.classList.remove('hidden');
            if (secDisplay) secDisplay.innerText = sessionRemainingSeconds;
        }

        // Expire session when 0 seconds remain
        if (sessionRemainingSeconds <= 0) {
            clearInterval(sessionTimerInterval);
            sessionTimerInterval = null;
            document.getElementById('session-timer-modal')?.classList.add('hidden');
            
            showToast('Ücretsiz web destek süreniz doldu! Masaüstü sürümünü indirin veya lisans satın alın.', 'error');
            
            // Close viewer screen gracefully
            if (connected && disconnectBtn) {
                disconnectBtn.click();
            }
            stopWebScreenShare();
        }
    }, 1000);
}

function extendFreeSessionTimer() {
    sessionRemainingSeconds += 300; // Add +5 Minutes (+300 sec)
    sessionExtensionUsed = true;
    document.getElementById('session-timer-modal')?.classList.add('hidden');
    showToast('🎁 +5 Dakika Ek Ücretsiz Süre Eklendi!', 'success');
}

// 7. Instant Checkout & Online License Generator
function openCheckoutModal() {
    document.getElementById('session-timer-modal')?.classList.add('hidden');
    const modal = document.getElementById('checkout-modal');
    if (modal) modal.classList.remove('hidden');
}

function closeCheckoutModal() {
    const modal = document.getElementById('checkout-modal');
    if (modal) modal.classList.add('hidden');
}

async function submitOnlineCheckout(e) {
    if (e) e.preventDefault();
    const name = document.getElementById('checkout-name').value.trim();
    const email = document.getElementById('checkout-email').value.trim();
    const phone = document.getElementById('checkout-phone').value.trim();

    try {
        showToast('Ödeme bildirimi iletiliyor...', 'info');
        await fetch('/api/support/ticket/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                name: name,
                issue: `[HAVALE ÖDEME BİLDİRİMİ] Pro Lisans Talebi - Tel: ${phone} - Email: ${email}`,
                notes: `Müşteri Havale Bildirimi Yaptı. Ad: ${name}, Email: ${email}, Tel: ${phone}`,
                tenantId: 'SUPERADMIN'
            })
        });

        document.getElementById('checkout-form').style.display = 'none';
        document.getElementById('checkout-success-box').style.display = 'block';
        showToast('Ödeme bildirimi yöneticiye başarıyla ulaştırıldı!', 'success');
    } catch (err) {
        console.warn("Ödeme bildirim hatası:", err);
        document.getElementById('checkout-form').style.display = 'none';
        document.getElementById('checkout-success-box').style.display = 'block';
    }
}

function checkoutViaWhatsApp() {
    const name = document.getElementById('checkout-name') ? document.getElementById('checkout-name').value.trim() : '';
    const email = document.getElementById('checkout-email') ? document.getElementById('checkout-email').value.trim() : '';
    const phone = document.getElementById('checkout-phone') ? document.getElementById('checkout-phone').value.trim() : '';

    const text = encodeURIComponent(`Merhaba, BigLineconnect Lisans Satın Alma ve Ödeme Bildirimi:\nAd Soyad / Firma: ${name || 'Belirtilmedi'}\nE-Posta: ${email || 'Belirtilmedi'}\nTelefon: ${phone || 'Belirtilmedi'}\nYıllık Pro Lisans Paketinizi (Ayda ₺125 TL) almak istiyorum.`);
    window.open(`https://wa.me/?text=${text}`, '_blank');
}

function copyGeneratedLicenseKey() {
    const key = document.getElementById('generated-license-key').innerText.trim();
    if (key) {
        navigator.clipboard.writeText(key).then(() => {
            showToast('Lisans Anahtarı Panoya Kopyalandı!', 'success');
        });
    }
}

// Global Window Function Bindings for Mobile & HTML Event Handlers
window.startConnectionProcess = window.doConnect || startConnectionProcess;
window.connectToHost = connectToHost;
window.sendPassword = sendPassword;
window.switchConnectTab = switchConnectTab;
window.shareViaWhatsApp = shareViaWhatsApp;
window.copyMagicLink = copyMagicLink;
window.openCheckoutModal = openCheckoutModal;
window.closeCheckoutModal = closeCheckoutModal;
window.submitOnlineCheckout = submitOnlineCheckout;
window.checkoutViaWhatsApp = checkoutViaWhatsApp;
window.extendFreeSessionTimer = extendFreeSessionTimer;
window.startFreeSessionTimer = startFreeSessionTimer;


// --- FILE TRANSFER CHUNKING INTEGRATION ---
let fileUploadInProgress = false;

function triggerFileTransfer() {
    if (!connected || !socket || socket.readyState !== WebSocket.OPEN) {
        showToast('LÃ¼tfen Ã¶nce bir cihaza baÄŸlanÄ±n.', 'error');
        return;
    }
    if (fileUploadInProgress) {
        showToast('Zaten aktif bir dosya gÃ¶nderimi var.', 'warning');
        return;
    }
    let fileInput = document.getElementById('file-transfer-input');
    if (!fileInput) {
        fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.id = 'file-transfer-input';
        fileInput.style.display = 'none';
        fileInput.addEventListener('change', handleFileSelected);
        document.body.appendChild(fileInput);
    }
    fileInput.click();
}

async function handleFileSelected(event) {
    const file = event.target.files[0];
    if (!file) return;

    fileUploadInProgress = true;
    
    const progressContainer = document.getElementById('upload-progress-container');
    const progressText = document.getElementById('upload-progress-text');
    const progressBar = document.getElementById('upload-progress-bar');
    
    if (progressContainer) {
        progressContainer.style.setProperty('display', 'flex', 'important');
    }
    if (progressText) {
        progressText.innerText = 'Dosya GÃ¶nderiliyor: %0';
    }
    if (progressBar) {
        progressBar.style.width = '0%';
    }

    try {
        const totalSize = file.size;
        const fileName = file.name;
        const chunkSize = 64 * 1024; // 64KB chunks
        
        // 1. Send batch_start
        const batchStart = {
            type: "batch_start",
            totalFiles: 1,
            totalSize: totalSize,
            senderId: "WebClient",
            targetFolder: "DOWNLOADS"
        };
        socket.send(JSON.stringify(batchStart));
        
        // 2. Send file_start
        const fileStart = {
            type: "file_start",
            name: fileName,
            size: totalSize,
            isFolder: false
        };
        socket.send(JSON.stringify(fileStart));
        
        // Let host process headers
        await new Promise(resolve => setTimeout(resolve, 100));

        let offset = 0;
        
        while (offset < totalSize) {
            const currentChunkSize = Math.min(chunkSize, totalSize - offset);
            const blobSlice = file.slice(offset, offset + currentChunkSize);
            
            const arrayBuffer = await new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => resolve(reader.result);
                reader.onerror = reject;
                reader.readAsArrayBuffer(blobSlice);
            });
            
            // Format packet: 0x46 ('F') byte followed by chunk bytes
            const packet = new Uint8Array(1 + currentChunkSize);
            packet[0] = 0x46; // FileTransferTag
            packet.set(new Uint8Array(arrayBuffer), 1);
            
            socket.send(packet.buffer);
            
            offset += currentChunkSize;
            
            const percent = Math.round((offset / totalSize) * 100);
            if (progressText) {
                progressText.innerText = 'Dosya GÃ¶nderiliyor: %' + percent;
            }
            if (progressBar) {
                progressBar.style.width = percent + '%';
            }
            
            await new Promise(resolve => setTimeout(resolve, 5));
        }
        
        // 3. Send file_end
        const fileEnd = {
            type: "file_end"
        };
        socket.send(JSON.stringify(fileEnd));
        
        // 4. Send batch_end
        const batchEnd = {
            type: "batch_end"
        };
        socket.send(JSON.stringify(batchEnd));
        
        showToast('"' + fileName + '" baÅŸarÄ±yla gÃ¶nderildi!', 'success');
        
    } catch (e) {
        console.error('File upload error:', e);
        showToast('Dosya gÃ¶nderiminde hata oluÅŸtu!', 'error');
        try {
            socket.send(JSON.stringify({ type: "transfer_cancel" }));
        } catch (_) {}
    } finally {
        fileUploadInProgress = false;
        if (event.target) event.target.value = '';
        
        setTimeout(() => {
            if (!fileUploadInProgress && progressContainer) {
                progressContainer.style.setProperty('display', 'none', 'important');
            }
        }, 2000);
    }
}

window.triggerFileTransfer = triggerFileTransfer;
window.handleFileSelected = handleFileSelected;