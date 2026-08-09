# 🖥️ BigLineconnect - Web Remote Desktop Mouse Calibration & Handshake Library Guide

This document preserves the exact mathematical equations, event pipelines, and implementation rules for the Web-to-Desktop remote desktop engine.

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

*Saved permanently in BigLineconnect Codebase Library.*
