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
const landingPage = document.getElementById('landing-page');
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
if (targetIdInput) {
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
}

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
if (connectBtn) {
    connectBtn.addEventListener('click', () => {
        const rawId = targetIdInput.value.replace(/\s/g, '');
        if (!rawId || rawId.length !== 9) {
            showToast('Lütfen 9 haneli geçerli bir bağlantı ID\'si girin.', 'error');
            return;
        }
        
        connectToHost(rawId);
    });
}

// Disconnect Button Event
if (disconnectBtn) {
    disconnectBtn.addEventListener('click', () => {
        if (socket) {
            socket.close();
        }
    });
}

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
            if (landingPage) landingPage.classList.add('hidden');
            if (viewerScreen) viewerScreen.classList.remove('hidden');
            startFreeSessionTimer();
        };
        
        socket.onclose = () => {
            connected = false;
            if (customCloseReason) {
                showToast(customCloseReason, 'error');
            } else {
                showToast('Bağlantı sonlandırıldı.', 'info');
            }
            if (viewerScreen) viewerScreen.classList.add('hidden');
            if (landingPage) landingPage.classList.remove('hidden');
            if (passwordModal) passwordModal.classList.add('hidden');
            if (hiddenKeyboardInput) hiddenKeyboardInput.disabled = false;
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
                    if (passwordModal) passwordModal.classList.remove('hidden');
                    if (hiddenKeyboardInput) hiddenKeyboardInput.disabled = true;
                    if (accessPasswordInput) {
                        accessPasswordInput.disabled = false;
                        accessPasswordInput.value = '';
                        setTimeout(() => {
                            accessPasswordInput.focus();
                        }, 150);
                    }
                    showToast('Karşı bilgisayara bağlanmak için Erişim Şifresi gereklidir.', 'info');
                } else if (event.data === 'AUTH_WAITING') {
                    if (connectionStatus) {
                        connectionStatus.innerHTML = `<span class="status-dot online"></span>Onay Bekleniyor...`;
                    }
                    showToast('Bağlantı için karşı bilgisayarın onayı bekleniyor...', 'info');
                } else if (event.data === 'AUTH_SUCCESS') {
                    if (passwordModal) passwordModal.classList.add('hidden');
                    if (hiddenKeyboardInput) hiddenKeyboardInput.disabled = false;
                    showToast('Doğrulama başarılı!', 'success');
                } else if (event.data === 'AUTH_FAILED') {
                    customCloseReason = 'Hatalı erişim şifresi girildi!';
                    if (passwordModal) passwordModal.classList.add('hidden');
                    if (hiddenKeyboardInput) hiddenKeyboardInput.disabled = false;
                    socket.close();
                } else if (event.data === 'AUTH_REJECTED') {
                    customCloseReason = 'Bağlantı isteği kullanıcı tarafından reddedildi!';
                    if (passwordModal) passwordModal.classList.add('hidden');
                    if (hiddenKeyboardInput) hiddenKeyboardInput.disabled = false;
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
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: 'move', x, y }));
    }
}

function sendClick(button, action) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: 'click', button, action }));
    }
}

function sendDoubleClick(button = 'left', x = null, y = null) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        const payload = { type: 'double_click', button };
        if (x !== null && y !== null) {
            payload.x = x;
            payload.y = y;
        }
        socket.send(JSON.stringify(payload));
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
    const naturalWidth = canvas.naturalWidth || canvas.width || 1920;
    const naturalHeight = canvas.naturalHeight || canvas.height || 1080;
    
    if (rect.width <= 0 || rect.height <= 0) return { x: 0, y: 0 };

    const imageAspect = naturalWidth / naturalHeight;
    const containerAspect = rect.width / rect.height;
    
    let renderedWidth, renderedHeight, offsetX, offsetY;
    
    if (containerAspect > imageAspect) {
        // Black bars on left & right
        renderedHeight = rect.height;
        renderedWidth = rect.height * imageAspect;
        offsetX = (rect.width - renderedWidth) / 2;
        offsetY = 0;
    } else {
        // Black bars on top & bottom
        renderedWidth = rect.width;
        renderedHeight = rect.width / imageAspect;
        offsetX = 0;
        offsetY = (rect.height - renderedHeight) / 2;
    }
    
    // getBoundingClientRect() already incorporates panX, panY, and CSS scale transforms
    const mouseX = clientX - rect.left - offsetX;
    const mouseY = clientY - rect.top - offsetY;
    
    const x = mouseX / renderedWidth;
    const y = mouseY / renderedHeight;
    
    return {
        x: Math.max(0, Math.min(1, x)),
        y: Math.max(0, Math.min(1, y))
    };
}

if (screenImg) {
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
        const delta = e.deltaY < 0 ? 120 : -120;
        sendScroll(delta);
        e.preventDefault();
    }, { passive: false });

    // Mobile Touch Events & Bulletproof Tap Engine
    let startTouch1X = null, startTouch1Y = null, startPan1X = 0, startPan1Y = 0;
    let touchStartX = 0, touchStartY = 0, touchStartTime = 0, isMultiTouch = false;
    let lastTapTime = 0, lastTapX = 0, lastTapY = 0;

    screenImg.addEventListener('touchstart', (e) => {
        if (!connected) return;
        
        if (e.touches.length === 1) {
            isMultiTouch = false;
            const touch = e.touches[0];
            touchStartX = touch.clientX;
            touchStartY = touch.clientY;
            touchStartTime = Date.now();

            startTouch1X = touch.clientX;
            startTouch1Y = touch.clientY;
            startPan1X = panX;
            startPan1Y = panY;
        } else if (e.touches.length === 2) {
            isMultiTouch = true;
            startTouch1X = null;
            
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
            if (scale > 1.0 && startTouch1X !== null) {
                panX = startPan1X + (touch.clientX - startTouch1X);
                panY = startPan1Y + (touch.clientY - startTouch1Y);
                updateTransform();
            } else {
                const pos = getMousePos(screenImg, touch.clientX, touch.clientY);
                sendMove(pos.x, pos.y);
            }
        } else if (e.touches.length === 2) {
            const dx = e.touches[0].clientX - e.touches[1].clientX;
            const dy = e.touches[0].clientY - e.touches[1].clientY;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            const midX = (e.touches[0].clientX + e.touches[1].clientX) / 2;
            const midY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
            
            scale = Math.max(1.0, Math.min(6.0, startScale * (distance / startTouchDistance)));
            panX = startPanX + (midX - startMidX);
            panY = startPanY + (midY - startMidY);
            
            updateTransform();
        }
        e.preventDefault();
    }, { passive: false });

    screenImg.addEventListener('touchend', (e) => {
        if (!connected || isMultiTouch) return;
        
        const now = Date.now();
        const duration = now - touchStartTime;
        
        if (duration < 600 && e.changedTouches.length === 1) {
            const touch = e.changedTouches[0];
            const moveDist = Math.sqrt((touch.clientX - touchStartX) ** 2 + (touch.clientY - touchStartY) ** 2);
            
            if (moveDist < 60) {
                const pos = getMousePos(screenImg, touch.clientX, touch.clientY);
                sendMove(pos.x, pos.y);

                const timeDiff = now - lastTapTime;
                const doubleTapDist = Math.sqrt((touch.clientX - lastTapX) ** 2 + (touch.clientY - lastTapY) ** 2);

                if (timeDiff < 500 && doubleTapDist < 60) {
                    sendDoubleClick('left', pos.x, pos.y);
                    showToast('Çift Tıklama Yollandı ⚡', 'info');
                    lastTapTime = 0;
                } else if (currentMouseMode === 'double') {
                    sendDoubleClick('left', pos.x, pos.y);
                    showToast('Çift Tıklama Yollandı ⚡', 'info');
                    lastTapTime = 0;
                } else if (currentMouseMode === 'right') {
                    sendClick('right', 'down');
                    setTimeout(() => sendClick('right', 'up'), 40);
                    showToast('Sağ Tıklama Yollandı 🔴', 'info');
                    lastTapTime = 0;
                } else {
                    sendClick('left', 'down');
                    setTimeout(() => sendClick('left', 'up'), 40);
                    lastTapTime = now;
                    lastTapX = touch.clientX;
                    lastTapY = touch.clientY;
                }
            }
        }
        
        startTouch1X = null;
        e.preventDefault();
    }, { passive: false });

    screenImg.addEventListener('touchcancel', (e) => {
        if (!connected) return;
        startTouch1X = null;
    });

    screenImg.style.transformOrigin = 'center center';
    screenImg.style.transition = 'none';
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

// On-Screen Touch Numpad Actions
document.querySelectorAll('.numpad-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
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
    });
});

// Password submit actions
function sendPassword() {
    if (!accessPasswordInput) return;
    const pass = accessPasswordInput.value.replace(/\D/g, '');
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send("AUTH_PASS:" + pass);
        showToast('Şifre gönderildi, doğrulanıyor...', 'info');
    }
}
if (submitPasswordBtn) submitPasswordBtn.addEventListener('click', sendPassword);
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
    const urlParams = new URLSearchParams(window.location.search);
    const magicId = urlParams.get('id') || urlParams.get('remoteid');
    if (magicId && targetIdInput) {
        const clean = magicId.replace(/\D/g, '');
        if (clean.length >= 6) {
            targetIdInput.value = clean;
            targetIdInput.dispatchEvent(new Event('input'));
            
            // Scroll to connect widget
            setTimeout(() => {
                document.getElementById('baglan')?.scrollIntoView({ behavior: 'smooth' });
                showToast('Uzak Masaüstü ID algılandı! Bağlan butonuna basın.', 'info');
            }, 300);
        }
    }
}

// 2. WhatsApp Share Generator
function shareViaWhatsApp() {
    const idVal = targetIdInput ? targetIdInput.value.replace(/\D/g, '') : '';
    if (!idVal || idVal.length < 6) {
        alert('Lütfen öncelikle 9 haneli Uzak Masaüstü ID girin!');
        return;
    }
    const magicUrl = `https://biglineconnect.bigus.com.tr/?id=${idVal}`;
    const text = encodeURIComponent(`Merhaba, BigLineconnect Uzak Masaüstü Bağlantı Bilgilerim:\nID: ${idVal}\nTek tıkla bağlanmak için linke tıklayın:\n${magicUrl}`);
    window.open(`https://wa.me/?text=${text}`, '_blank');
}

// 3. One-Click Copy Magic Link
function copyMagicLink() {
    const idVal = targetIdInput ? targetIdInput.value.replace(/\D/g, '') : '';
    if (!idVal || idVal.length < 6) {
        alert('Lütfen öncelikle 9 haneli Uzak Masaüstü ID girin!');
        return;
    }
    const magicUrl = `https://biglineconnect.bigus.com.tr/?id=${idVal}`;
    navigator.clipboard.writeText(magicUrl).then(() => {
        showToast('Sihirli Bağlantı Linki Kopyalandı! (WhatsApp veya Mail ile gönderebilirsiniz)', 'success');
    }).catch(() => {
        showToast('Kopyalandı: ' + magicUrl, 'success');
    });
}

document.addEventListener('DOMContentLoaded', () => {
    checkMagicLink();
});

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
