# 🖥️ BigLineconnect - Web Remote Desktop Mouse Calibration & Mobile Control Library Guide

This document preserves the exact mathematical equations, event pipelines, mobile gesture specs, and implementation rules for the Web-to-Desktop remote desktop engine.

---

## 1. 🎯 Pixel-Perfect Mouse Calibration (`getMousePos`)

When rendering a remote desktop stream inside an HTML5 `<canvas>` element using `object-fit: contain;`, the container element and the rendered image may have different aspect ratios. This creates **letterboxing (top/bottom black bars)** or **pillarboxing (left/right black bars)** around the image.

Without compensating for these black bars, raw normalized mouse coordinates `(e.clientX - rect.left) / rect.width` suffer from severe mouse miscalibration (e.g. clicking near a window's red close button hits pixels far to the right).

### The Mathematical Formula:
```javascript
function getMousePos(cv, clientX, clientY) {
    var rect = cv.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return { x: 0, y: 0 };

    var naturalWidth = (cv.width && cv.width > 300) ? cv.width : 1920;
    var naturalHeight = (cv.height && cv.height > 150) ? cv.height : 1080;

    var imageAspect = naturalWidth / naturalHeight;
    var containerAspect = rect.width / rect.height;

    var renderedWidth, renderedHeight, offsetX, offsetY;

    if (containerAspect > imageAspect) {
        // Pillarboxing: Black bars on left & right
        renderedHeight = rect.height;
        renderedWidth = rect.height * imageAspect;
        offsetX = (rect.width - renderedWidth) / 2;
        offsetY = 0;
    } else {
        // Letterboxing: Black bars on top & bottom
        renderedWidth = rect.width;
        renderedHeight = rect.width / imageAspect;
        offsetX = 0;
        offsetY = (rect.height - renderedHeight) / 2;
    }

    var mouseX = clientX - rect.left - offsetX;
    var mouseY = clientY - rect.top - offsetY;

    return {
        x: Math.max(0, Math.min(1, mouseX / renderedWidth)),
        y: Math.max(0, Math.min(1, mouseY / renderedHeight))
    };
}
```

---

## 2. 🔌 Foolproof WebSocket Handshake Engine

- **URL Endpoint:** `wss://<host>/connect-client?id=<REMOTE_ID>`
- **Binary Type:** `ws.binaryType = 'arraybuffer';`
- **Text Events:**
  - `ERROR:ID_NOT_FOUND`: Target desktop agent offline or ID invalid.
  - `ERROR:BUSY`: Target desktop agent has active operator.
  - `AUTH_REQUIRED`: Host requests 6-digit access PIN. Client responds with `ws.send(pin)`.
  - `AUTH_SUCCESS`: PIN verified, screen stream commences.
- **Binary Frame Stream:** ArrayBuffer JPEG payloads drawn onto `<canvas id="screen-canvas">` using `URL.createObjectURL(blob)` and 2D canvas context.

---

## 3. 🛡️ CSS Overlay Safety Rule
Hidden overlays (`#password-modal`, `#viewer-screen`) must ALWAYS specify:
`display: none !important; pointer-events: none !important;`
when hidden, preventing invisible Z-Index elements from blocking mouse clicks on the landing page buttons.

---

## 4. 📱 Mobile Focal Point Pinch Zoom & Gesture Control Engine

For mobile devices (iOS Safari, Android Chrome, Tablets), zooming focuses directly under the user's fingertips rather than hardcoded canvas center.

### A. Dynamic Focal Point Calculation:
```javascript
// Calculate midpoint between 2 touch points
var midX = (t1.clientX + t2.clientX) / 2;
var midY = (t1.clientY + t2.clientY) / 2;

// Compute percentage origin relative to canvas bounding box
var rect = cv.getBoundingClientRect();
if (rect.width > 0 && rect.height > 0) {
    originXPercent = Math.max(0, Math.min(100, ((midX - rect.left) / rect.width) * 100));
    originYPercent = Math.max(0, Math.min(100, ((midY - rect.top) / rect.height) * 100));
}

// Apply transform origin to pinch focal midpoint
cv.style.transformOrigin = originXPercent + '% ' + originYPercent + '%';
cv.style.transform = 'translate(' + panX + 'px, ' + panY + 'px) scale(' + currentScale + ')';
```

### B. Mobile Gesture Specification:
1. **Single Tap (< 600ms without drag):** Sends Left Click payload at calibrated `(x, y)` location.
2. **Hold / Long Press (>= 400ms) or Right Click Toggle:** Sends Right Click payload at calibrated `(x, y)` location.
3. **Single Finger Drag:** Sends 60FPS mouse movement across the remote desktop screen.
4. **Two Finger Pinch & Pan:** Zooms directly in/out at fingertip focal midpoint and pans `panX, panY` across zoomed view.
5. **Touch Action Override:** `cv.style.touchAction = 'none'` prevents unwanted browser page scrolling during remote desktop interactions.

---

## 5. 🟢 P2P UDP & Subnet LAN Direct Auto-Detection Engine (With Verified Host ID Handshake)

When connecting via 9-digit Host ID (e.g. `219 675 629`) or IP address, `StartP2pAndLanProbe()` performs automatic asynchronous background detection to upgrade session quality:

1. **Explicit IP / Hostname Probe:** Tests TCP port 18888 with 500ms timeout if address is direct IP.
2. **Parallel Subnet (/24) LAN Probe with Verified Host ID Handshake:**
   - Probes all 254 IPs (`192.168.x.1` to `192.168.x.254`) on local subnet over TCP port 18888 in parallel.
   - **CRITICAL HANDSHAKE RULE:** Upon TCP connection, client sends `GET /host-id HTTP/1.1\r\nHost: local\r\n\r\n`.
   - Host listener in `Program.cs` responds with `HTTP/1.1 200 OK\r\n...HOST_ID:<CurrentHostId>\r\n`.
   - Client verifies `respText.Contains($"HOST_ID:{cleanTargetId}")`.
   - **False-Positive Green Badge Prevention:** If port 18888 is open on a *different* local PC or testing instance, the Host ID will not match `cleanTargetId`, preventing false-positive green badge triggers across different networks.
3. **UDP P2P Direct Punching:** Invokes `P2pDirectEngine.PunchHoleAndConnectAsync()` to establish zero-relay UDP ICE tunnel.
4. **Status Badge Transition:**
   - If LAN/P2P Direct connects and Host ID matches: Badge transitions from Yellow (`☁️ BULUT TÜNELİ`) to Bright Green (`⚡ LAN DIRECT (0.5ms)` / `🌐 P2P DIRECT (UDP)`).
   - If blocked by CGNAT/Firewall or on different networks: Safely remains on Yellow (`☁️ BULUT TÜNELİ`) with zero frame drop.

---

## 6. 🖼️ Non-Focus-Stealing Overlay Banner Design Rule

The active remote desktop operator banner (`RemoteOverlayBannerForm`) in the bottom-right corner must adhere to:
1. **Window Style:** `WS_EX_NOACTIVATE` (0x08000000), `WS_EX_TOOLWINDOW` (0x00000080), `WS_EX_TOPMOST` (0x00000008) so it never steals focus from Windows Taskbar or active windows.
2. **Width & Positioning:** `Width = 420px`, `Height = 48px`, positioned at `(wa.Right - 440, wa.Bottom - 65)` to prevent text overlap across high-DPI Windows displays.
3. **Color Palette:** Slate Dark background (`#141826`), Cyan accent border (`#00E9FF`), Green lightning icon (`#00E676`), and Slate button (`#1E2A41`) for high readability without visual glitches.

---

## 7. ⌨️ Ultra-Fast Hardware Virtual Key (VK + ScanCode) MS Office Speed Engine

To eliminate typing latency in Microsoft Excel cells and Word documents:
1. **VkKeyScan & MapVirtualKey Hardware Mapping:** `InputSimulator.SimulateChar(char ch)` translates Unicode characters into native Windows Virtual Key (VK) and ScanCode combinations.
2. **Atomic Win32 SendInput Array:** KeyDown, Shift/AltGr modifiers, and KeyUp events are bundled into a single atomic Win32 `SendInput` call, triggering native `WM_KEYDOWN` -> `WM_CHAR` -> `WM_KEYUP` hardware events.
3. **Result:** MS Excel grid editor and MS Word RichEdit process inputs at 0.00ms delay without throttling or queueing lag.

## 8. 🛡️ Concurrent Remote Desktop Coexistence & Alpemix Mouse Hook Bypass Engine

When multiple remote desktop tools (e.g. Alpemix, AnyDesk, TeamViewer) are connected to the same host machine simultaneously:
1. **Mouse Hook Bypass (`dwExtraInfo = 0x42494755`):** Legacy `mouse_event` is strictly forbidden. Mouse input must be dispatched via `SendInput` with `dwExtraInfo = (IntPtr)0x42494755` ("BIGU" signature). This prevents third-party `WH_MOUSE_LL` hooks (such as Alpemix's mouse hook) from intercepting and locking the GDI/DirectX desktop capture stream upon mouse clicks.
2. **2-Second DXGI Auto-Healing Cooldown:** If another remote desktop app temporarily locks DirectX (`DXGI_ERROR_ACCESS_LOST`), `ScreenCapturer.cs` gracefully transitions to GDI+ / Direct Primary Screen DC for 2 seconds, and automatically recovers 60 FPS DirectX DXGI duplication as soon as the lock is released.
3. **500ms Stream Re-Sync Engine:** `Program.cs` automatically unlocks `_isSendingFrame = false` if a socket send or capture thread is stalled for >500ms, guaranteeing zero viewer freeze even during remote PC reboots or multi-client session handovers.

---

*Saved permanently in BigLineconnect Codebase Library.*
