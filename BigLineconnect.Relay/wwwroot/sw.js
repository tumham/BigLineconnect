const CACHE_NAME = 'bigline-pwa-v3707';

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.map((key) => caches.delete(key))
      );
    }).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  const url = event.request.url;
  if (event.request.mode === 'navigate' || url.includes('app.js') || url.includes('index.html') || url.includes('mobil') || url.includes('terminal') || url.includes('manifest.json') || url.includes('sw.js')) {
    event.respondWith(fetch(event.request, { cache: 'no-store' }));
    return;
  }
  event.respondWith(
    fetch(event.request).catch(() => caches.match(event.request))
  );
});
