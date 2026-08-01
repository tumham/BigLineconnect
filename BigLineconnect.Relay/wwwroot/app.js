let socket = null;
let connected = false;
let currentMouseMode = 'left'; // 'left' or 'right'

// Zoom & Pan State
let scale = 1.0;
let panX = 0;
let panY = 0;
let startTouchDistance = 0;
let startScale = 1.0;
let startMidX = 0;
let startMidY = 0;
let startPanX = 0;
let startPanY = 0;

// DOM Elements
const connectionScreen = document.getElementById('connection-screen');
const viewerScreen = document.getElementById('viewer-screen');
const targetIdInput = document.getElementById('target-id');
const connectBtn = document.getElementById('connect-btn');
const disconnectBtn = document.getElementById('disconnect-btn');
const fullscreenBtn = document.getElementById('fullscreen-btn');
const screenImg = document.getElementById('screen-img');
const canvasContainer = document.getElementById('canvas-container');
const connectionStatus = document.getElementById('connection-status');
const toggleKeyboardBtn = document.getElementById('toggle-keyboard-btn');
const hiddenKeyboardInput = document.getElementById('hidden-keyboard-input');
const mouseModeBtn = document.getElementById('mouse-mode-btn');
const mouseModeText = document.getElementById('mouse-mode-text');
const toastElement = document.getElementById('toast');
const passwordModal = document.getElementById('password-modal');
const accessPasswordInput = document.getElementById('access-password-input');
const submitPasswordBtn = document.getElementById('submit-password-btn');

// Auto-format ID input (e.g. 123 456 789)
targetIdInput.addEventListener('input', (e) => {
    let value = e.target.value.replace(/\D/g, ''); // Numbers only
    if (value.length > 9) value = value.substring(0, 9);
    
    // Format as XXX XXX XXX
    let formatted = '';
    if (value.length > 6) {
        formatted = `${value.substring(0, 3)} ${value.substring(3, 6)} ${value.substring(6)}`;
    } else if (value.length > 3) {
        formatted = `${value.substring(0, 3)} ${value.substring(3)}`;
    } else {
        formatted = value;
    }
    
    e.target.value = formatted;
});

// Toast system
function showToast(message, type = 'info') {
    toastElement.textContent = message;
    toastElement.className = `toast ${type}`;
    toastElement.classList.remove('hidden');
    
    setTimeout(() => {
        toastElement.classList.add('hidden');
    }, 3000);
}

// Connect Button Event
connectBtn.addEventListener('click', () => {
    const rawId = targetIdInput.value.replace(/\s/g, '');
    if (!rawId || rawId.length !== 9) {
        showToast('Lütfen 9 haneli geçerli bir bağlantı ID\'si girin.', 'error');
        return;
    }
    
    connectToHost(rawId);
});

// Disconnect Button Event
disconnectBtn.addEventListener('click', () => {
    if (socket) {
        socket.close();
    }
});

// Connect to C# Relay WebSockets
function connectToHost(id) {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/connect-client?id=${id}`;
    showToast('Bağlantı kuruluyor...', 'info');
    
    try {
        socket = new WebSocket(wsUrl);
        socket.binaryType = 'arraybuffer';
        
        let customCloseReason = null;

        socket.onopen = () => {
            connected = true;
            customCloseReason = null;
            showToast('Bağlantı kuruldu!', 'success');
            connectionScreen.classList.add('hidden');
            viewerScreen.classList.remove('hidden');
        };
        
        socket.onclose = () => {
            connected = false;
            if (customCloseReason) {
                showToast(customCloseReason, 'error');
            } else {
                showToast('Bağlantı sonlandırıldı.', 'info');
            }
            viewerScreen.classList.add('hidden');
            connectionScreen.classList.remove('hidden');
            passwordModal.classList.add('hidden');
            socket = null;
            
            // Reset zoom state
            scale = 1.0;
            panX = 0;
            panY = 0;
            updateTransform();
        };
        
        socket.onerror = (error) => {
            console.error('WebSocket Error:', error);
            showToast('Bağlantı hatası!', 'error');
        };
        
        socket.onmessage = async (event) => {
            let debugInfo = "";
            let sizeKb = 0;
            let blob = null;
            if (event.data instanceof ArrayBuffer) {
                const header = new Uint8Array(event.data.slice(0, 4));
                const hex = Array.from(header).map(b => b.toString(16).toUpperCase().padStart(2, '0')).join(' ');
                debugInfo = `AB [${hex}]`;
                sizeKb = event.data.byteLength / 1024;
                blob = new Blob([event.data], { type: 'image/jpeg' });
            } else if (event.data instanceof Blob) {
                debugInfo = `Blob ${event.data.size} B`;
                sizeKb = event.data.size / 1024;
                blob = event.data.slice(0, event.data.size, 'image/jpeg'); // Change content type safely
            }

            if (blob) {
                if (connectionStatus) {
                    connectionStatus.innerHTML = `<span class="status-dot online"></span>${debugInfo} ${sizeKb.toFixed(0)} KB`;
                }
                const url = URL.createObjectURL(blob);
                const oldUrl = screenImg.src;
                screenImg.src = url;
                if (oldUrl.startsWith('blob:')) {
                    URL.revokeObjectURL(oldUrl); // Free memory!
                }
            } else if (typeof event.data === 'string') {
                if (event.data === 'ERROR:ID_NOT_FOUND') {
                    customCloseReason = 'Bağlantı ID\'si bulunamadı veya bilgisayar kapalı.';
                    socket.close();
                } else if (event.data === 'ERROR:BUSY') {
                    customCloseReason = 'Bu bilgisayar şu an meşgul.';
                    socket.close();
                } else if (event.data === 'AUTH_REQUIRED') {
                    passwordModal.classList.remove('hidden');
                    accessPasswordInput.value = '';
                    accessPasswordInput.focus();
                    showToast('Karşı bilgisayara bağlanmak için Erişim Şifresi gereklidir.', 'info');
                } else if (event.data === 'AUTH_WAITING') {
                    if (connectionStatus) {
                        connectionStatus.innerHTML = `<span class="status-dot online"></span>Onay Bekleniyor...`;
                    }
                    showToast('Bağlantı için karşı bilgisayarın onayı bekleniyor...', 'info');
                } else if (event.data === 'AUTH_SUCCESS') {
                    passwordModal.classList.add('hidden');
                    showToast('Doğrulama başarılı!', 'success');
                } else if (event.data === 'AUTH_FAILED') {
                    customCloseReason = 'Hatalı erişim şifresi girildi!';
                    passwordModal.classList.add('hidden');
                    socket.close();
                } else if (event.data === 'AUTH_REJECTED') {
                    customCloseReason = 'Bağlantı isteği kullanıcı tarafından reddedildi!';
                    socket.close();
                } else if (event.data.startsWith('{')) {
                    try {
                        const json = JSON.parse(event.data);
                        if (json.type === 'clipboard') {
                            navigator.clipboard.writeText(json.text).then(() => {
                                showToast('Pano Eşitlendi', 'success');
                            }).catch(err => {});
                        }
                    } catch (e) {}
                }
            }
        };
    } catch (e) {
        showToast('Bağlantı hatası!', 'error');
        console.error(e);
    }
}

// Fullscreen Button
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

// Mouse Mode Toggler (Left Click / Right Click for Mobile)
mouseModeBtn.addEventListener('click', () => {
    if (currentMouseMode === 'left') {
        currentMouseMode = 'right';
        mouseModeBtn.classList.remove('toggle-active');
        mouseModeBtn.style.background = 'rgba(231, 76, 60, 0.2)';
        mouseModeBtn.style.color = '#e74c3c';
        mouseModeBtn.style.borderColor = 'rgba(231, 76, 60, 0.4)';
        mouseModeText.textContent = 'Sağ Tık';
        showToast('Dokunma Modu: Sağ Tıklama', 'info');
    } else {
        currentMouseMode = 'left';
        mouseModeBtn.style.background = '';
        mouseModeBtn.style.color = '';
        mouseModeBtn.style.borderColor = '';
        mouseModeBtn.classList.add('toggle-active');
        mouseModeText.textContent = 'Sol Tık';
        showToast('Dokunma Modu: Sol Tıklama', 'info');
    }
});

// Keyboard Button for Mobile
toggleKeyboardBtn.addEventListener('click', () => {
    hiddenKeyboardInput.focus();
    showToast('Klavye aktif hale getirildi.', 'success');
});

// hidden keyboard input handling
hiddenKeyboardInput.addEventListener('keydown', (e) => {
    if (!connected || !socket) return;
    
    // Send key down and quick release key up
    sendKey(e.key, 'down');
    setTimeout(() => sendKey(e.key, 'up'), 40);
});

hiddenKeyboardInput.addEventListener('input', () => {
    hiddenKeyboardInput.value = ''; // Always keep clear
});

// Helper functions to send inputs via WS
function sendMove(x, y) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: 'move', x, y }));
    }
}

function sendClick(button, action) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: 'click', button, action }));
    }
}

function sendScroll(deltaY) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: 'scroll', deltaY }));
    }
}

function sendKey(key, action) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: 'key', key, action }));
    }
}

// Mouse Event listeners on canvas
function getMousePos(canvas, clientX, clientY) {
    const rect = canvas.getBoundingClientRect();
    const x = (clientX - rect.left - panX) / (rect.width * scale);
    const y = (clientY - rect.top - panY) / (rect.height * scale);
    return {
        x: Math.max(0, Math.min(1, x)),
        y: Math.max(0, Math.min(1, y))
    };
}

screenImg.addEventListener('mousedown', (e) => {
    if (!connected) return;
    const pos = getMousePos(screenImg, e.clientX, e.clientY);
    sendMove(pos.x, pos.y);
    
    let button = 'left';
    if (e.button === 2) button = 'right';
    else if (e.button === 1) button = 'middle';
    
    sendClick(button, 'down');
    e.preventDefault();
});

screenImg.addEventListener('mouseup', (e) => {
    if (!connected) return;
    let button = 'left';
    if (e.button === 2) button = 'right';
    else if (e.button === 1) button = 'middle';
    
    sendClick(button, 'up');
    e.preventDefault();
});

screenImg.addEventListener('mousemove', (e) => {
    if (!connected) return;
    const pos = getMousePos(screenImg, e.clientX, e.clientY);
    sendMove(pos.x, pos.y);
    e.preventDefault();
});

screenImg.addEventListener('contextmenu', (e) => {
    e.preventDefault();
});

screenImg.addEventListener('wheel', (e) => {
    if (!connected) return;
    // Map web scroll to OS scroll unit (usually 120 or -120)
    const delta = e.deltaY < 0 ? 120 : -120;
    sendScroll(delta);
    e.preventDefault();
}, { passive: false });

// Mobile Touch Events with Pinch-to-Zoom and Panning
screenImg.addEventListener('touchstart', (e) => {
    if (!connected) return;
    
    if (e.touches.length === 1) {
        const touch = e.touches[0];
        const pos = getMousePos(screenImg, touch.clientX, touch.clientY);
        sendMove(pos.x, pos.y);
        sendClick(currentMouseMode, 'down');
    } else if (e.touches.length === 2) {
        // Cancel single touch click down
        sendClick(currentMouseMode, 'up');
        
        const dx = e.touches[0].clientX - e.touches[1].clientX;
        const dy = e.touches[0].clientY - e.touches[1].clientY;
        startTouchDistance = Math.sqrt(dx * dx + dy * dy);
        startScale = scale;
        
        startMidX = (e.touches[0].clientX + e.touches[1].clientX) / 2;
        startMidY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
        startPanX = panX;
        startPanY = panY;
    }
    e.preventDefault();
}, { passive: false });

screenImg.addEventListener('touchmove', (e) => {
    if (!connected) return;
    
    if (e.touches.length === 1) {
        const touch = e.touches[0];
        const pos = getMousePos(screenImg, touch.clientX, touch.clientY);
        sendMove(pos.x, pos.y);
    } else if (e.touches.length === 2) {
        const dx = e.touches[0].clientX - e.touches[1].clientX;
        const dy = e.touches[0].clientY - e.touches[1].clientY;
        const distance = Math.sqrt(dx * dx + dy * dy);
        
        const midX = (e.touches[0].clientX + e.touches[1].clientX) / 2;
        const midY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
        
        const pX = midX - startMidX;
        const pY = midY - startMidY;
        
        scale = startScale * (distance / startTouchDistance);
        panX = startPanX + pX;
        panY = startPanY + pY;
        
        updateTransform();
    }
    e.preventDefault();
}, { passive: false });

screenImg.addEventListener('touchend', (e) => {
    if (!connected) return;
    
    // Always send click release on touchend for safety
    sendClick(currentMouseMode, 'up');
    
    if (e.touches.length === 1) {
        // Reset single touch state
        const touch = e.touches[0];
        // Move mouse to where the remaining finger is (so they can continue dragging if they want)
        const pos = getMousePos(screenImg, touch.clientX, touch.clientY);
        sendMove(pos.x, pos.y);
    }
    e.preventDefault();
}, { passive: false });

screenImg.addEventListener('touchcancel', (e) => {
    if (!connected) return;
    sendClick(currentMouseMode, 'up');
});

// Global Key Listeners for Desktop client keyboard input
window.addEventListener('keydown', (e) => {
    if (!connected) return;
    // Do not capture keys if password modal is open or user is typing in input fields
    if (document.activeElement === targetIdInput || document.activeElement === accessPasswordInput) return;
    if (passwordModal && !passwordModal.classList.contains('hidden')) return;
    
    sendKey(e.key, 'down');
    e.preventDefault();
});

window.addEventListener('keyup', (e) => {
    if (!connected) return;
    if (document.activeElement === targetIdInput || document.activeElement === accessPasswordInput) return;
    if (passwordModal && !passwordModal.classList.contains('hidden')) return;
    
    sendKey(e.key, 'up');
    e.preventDefault();
});

// Floating buttons key injection
document.querySelectorAll('.special-key').forEach(button => {
    button.addEventListener('mousedown', () => {
        if (!connected) return;
        const key = button.getAttribute('data-key');
        sendKey(key, 'down');
    });
    
    button.addEventListener('mouseup', () => {
        if (!connected) return;
        const key = button.getAttribute('data-key');
        sendKey(key, 'up');
    });
});

// CAD (Ctrl+Alt+Del) Special Action
const cadBtn = document.getElementById('btn-ctrl-alt-del');
cadBtn.addEventListener('click', () => {
    if (!connected) return;
    
    showToast('Ctrl + Alt + Del gönderildi.', 'info');
    
    // Simulate complex key combinations sequentially
    sendKey('control', 'down');
    sendKey('alt', 'down');
    sendKey('delete', 'down');
    
    setTimeout(() => {
        sendKey('delete', 'up');
        sendKey('alt', 'up');
        sendKey('control', 'up');
    }, 100);
});

// Password submit actions
function sendPassword() {
    const pass = accessPasswordInput.value;
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send("AUTH_PASS:" + pass);
        showToast('Şifre gönderildi, doğrulanıyor...', 'info');
    }
}
submitPasswordBtn.addEventListener('click', sendPassword);
accessPasswordInput.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') sendPassword();
});

// Clipboard sync (Send client clipboard to Host)
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

// Initialize image styles for zoom & pan
screenImg.style.transformOrigin = '0 0';
screenImg.style.transition = 'none';

function updateTransform() {
    if (scale <= 1.0) {
        scale = 1.0;
        panX = 0;
        panY = 0;
    } else {
        const rect = canvasContainer.getBoundingClientRect();
        const maxPanX = 0;
        const minPanX = rect.width - (rect.width * scale);
        const maxPanY = 0;
        const minPanY = rect.height - (rect.height * scale);
        
        panX = Math.max(minPanX, Math.min(maxPanX, panX));
        panY = Math.max(minPanY, Math.min(maxPanY, panY));
    }
    screenImg.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
}
