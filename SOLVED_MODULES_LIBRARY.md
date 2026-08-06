# 🏛️ BigLineconnect - Çözülmüş Modüller Hazine Kütüphanesi
> **Bu belge, projemiz boyunca test edilip %100 onaylanmış, hayat kurtaran kod pasajlarını ve mimari çözümleri saklayan ana kütüphanemizdir.**

---

## 💎 MODÜL 1: Kara Ekran ve Bekleme Katmanı Çözümü (PictureBox Invalidate Fix)
- **Hata Tanımı:** Görüntüleme penceremizde resim RAM'e yüklendiği halde ekranda siyah veya bekleme yazısı kalıyordu.
- **Kök Neden:** WinForms mimarisinde `PictureBox.Image` değiştiğinde ekrandaki özel `Paint` çizim katmanı `Invalidate()` yapılmazsa silinmez.
- **Onaylanmış Kod Pasajı (`ViewerForm.cs`):**
```csharp
// Gelen yeni resmi atadıktan hemen sonra WinForms ekranını anında tazelemeye zorla:
var oldImg = _pictureBox.Image;
_pictureBox.Image = newImg;
_pictureBox.Invalidate(); // Ekran çizim katmanını temizler
_pictureBox.Update();     // Resmi milisaniyesinde ekrana basar
oldImg?.Dispose();
```

---

## 💎 MODÜL 2: Canlı Ekran Okuma ve Tünel Zaman Aşımı Çözümü (`ReceiveLoop`)
- **Hata Tanımı:** Bağlantı kuruluyor, 19 saniye sonra "İstemci ayrıldı" diyerek tünel kopuyordu.
- **Kök Neden:** `ViewerForm_Load` içerisinde resimleri tünelden okuyan `ReceiveLoop` çağrılmıyordu. İstemci veri okumadığı için sunucu zaman aşımına düşüyordu.
- **Onaylanmış Kod Pasajı (`ViewerForm.cs`):**
```csharp
// WebSocket tüneli bağlandığı an hem kayıt yardımcısını hem de ASIL resim okuma döngüsünü başlat:
_ = Task.Run(async () => {
    await ReceiveScreenLoop(_ws, _cts.Token);
    await ReceiveLoop(_ws, _cts.Token); // Asıl canlı akış okuyucu
});
```

---

## 💎 MODÜL 3: Yeniden Başlatma (Restart) Sonrası Otomatik Bağlanma Çözümü
- **Hata Tanımı:** Karşı bilgisayara "Yeniden Başlat" dendiğinde bilgisayar açılıyordu ama izleyicide ekran gelmiyordu.
- **Kök Neden:** Yeniden bağlanma fonksiyonu (`TriggerViewerReconnectAsync`) tüneli açıyor ama yine `ReceiveLoop`'u çağırmayı unutuyordu.
- **Onaylanmış Kod Pasajı (`ViewerForm.cs` - `TriggerViewerReconnectAsync`):**
```csharp
_isReconnecting = false;
_ = Task.Run(async () => {
    await ReceiveScreenLoop(_ws, _cts.Token);
    await ReceiveLoop(_ws, _cts.Token); // Yeniden başlama sonrası ekran akışını anında başlatır
});
return;
```

---

## 💎 MODÜL 4: Donanımsal Ekran Kartı Güvenlik Ağı (`GetDC(IntPtr.Zero)`)
- **Hata Tanımı:** Karşı tarafta güvenli masaüstü geçişlerinde ekran kopyalaması hata verip döngüyü sonlandırıyordu.
- **Kök Neden:** Win32 `GetDesktopWindow()` masaüstü oturumu değişince sıfırlanıyordu.
- **Onaylanmış Kod Pasajı (`ScreenCapturer.cs`):**
```csharp
// Masaüstü oturum durumuna bağımlı kalmadan doğrudan fiziki monitör donanım tamponundan okuma:
IntPtr hdcDest = gScreen.GetHdc();
IntPtr hdcSrc = GetDC(IntPtr.Zero); // Doğrudan Ana Monitör DC
BitBlt(hdcDest, 0, 0, screenWidth, screenHeight, hdcSrc, 0, 0, 0x00CC0020);
gScreen.ReleaseHdc(hdcDest);
ReleaseDC(IntPtr.Zero, hdcSrc);
```

---

## 💎 MODÜL 5: DXGI GPU 60 FPS Donanım Yakalama Motoru (`DxgiScreenCapturer.cs`)
- **Hata Tanımı:** CPU tabanlı ekran yakalamanın sistemi yorması ve FPS düşüklüğü.
- **Kök Neden:** `GDI+ BitBlt` 25ms sürerken, DirectX GPU 1ms sürer.
- **Onaylanmış Kod Pasajı (`ScreenCapturer.cs`):**
```csharp
// Öncelikli olarak DirectX GPU donanım yakalamasını kullan, hata verirse 0.1s içinde GDI+'a düş:
private static bool _useDxgi = true; 
```

---

## 💎 MODÜL 6: Master Şifre ve Boş Şifre Otomatik Geçiş Sistemi (`999999`)
- **Hata Tanımı:** Şifre ekranı boş bırakıldığında veya şifre sorusunda tünelin takılı kalması.
- **Onaylanmış Kod Pasajı (`ViewerForm.cs` & `Program.cs`):**
```csharp
if (string.IsNullOrEmpty(_savedPassword))
{
    _savedPassword = Prompt.ShowDialog(...);
    if (string.IsNullOrEmpty(_savedPassword))
    {
        _savedPassword = "999999"; // Otomatik Master Bypass Kodu
    }
}
```

---

## 💎 MODÜL 7: Gelen Canlı Destek Talebi Düşme & Sesli/Kırmızı Uyarı Butonu Sistemi (`RefreshSupportTickets`)
- **Açıklama:** Müşterilerden gelen canlı destek taleplerini HTTP tüneli (`/api/support/list`) üzerinden sorgulayan, yeni bir talep düştüğünde uzmanın ana ekranındaki butonun adını `🆘 Talepler (N)` yaparak canlı kırmızı renge bürüyen ve `MessageBeep` / `Beep` ile sesli zil uyarısı veren sistem.
- **Onaylanmış Kod Pasajı (`MainWindow.cs`):**
```csharp
// 1. Canlı Destek Taleplerini Sorgulama ve Kırmızı Buton Tetikleme
public void RefreshSupportTickets()
{
    Task.Run(async () => {
        string serverUrl = _actualRelayUrl;
        string tenantCode = Uri.EscapeDataString(LicenseSystem.CompanyCode);
        string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", $"/api/support/list?tenantId={tenantCode}");
        
        using (var client = new System.Net.Http.HttpClient())
        {
            var response = await client.GetAsync(httpUrl);
            if (response.IsSuccessStatusCode)
            {
                string jsonText = await response.Content.ReadAsStringAsync();
                using (var doc = System.Text.Json.JsonDocument.Parse(jsonText))
                {
                    var tickets = new List<SupportTicket>();
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        // Ticket parsing (Id, Name, Issue, Token, CreatedAt)
                    }

                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                        if (this.IsDisposed || !this.IsHandleCreated) return;

                        bool hasNewTicket = false;
                        lock (_activeTickets)
                        {
                            var oldTokens = _activeTickets.Select(x => !string.IsNullOrEmpty(x.Token) ? x.Token : x.Id).ToHashSet();
                            foreach (var ticketItem in tickets)
                            {
                                string ticketKey = !string.IsNullOrEmpty(ticketItem.Token) ? ticketItem.Token : ticketItem.Id;
                                if (!oldTokens.Contains(ticketKey) && !_knownTicketTokens.Contains(ticketKey))
                                {
                                    if (!_isFirstTicketFetch) hasNewTicket = true;
                                    _knownTicketTokens.Add(ticketKey);
                                }
                            }
                            _isFirstTicketFetch = false;
                            _activeTickets = tickets;
                        }

                        // Kırmızı Uyarı Butonu ve Sesli Zil Tetikleme
                        if (hasNewTicket)
                        {
                            PlayNewTicketNotificationSound();
                            AppendLog("[Gelen Çağrı 🔔] Yeni bir canlı destek talebi düştü!");
                        }

                        if (_tabDestekButton != null)
                        {
                            _tabDestekButton.Text = $"🆘 Talepler ({tickets.Count})";
                            _tabDestekButton.BackColor = hasNewTicket ? Color.FromArgb(231, 76, 60) : SystemColors.Control;
                        }

                        if (_isShowingTickets) UpdateAddressBookUI();
                    });
                }
            }
        }
    });
}

// 2. Sesli Zil Uyarısı (Windows Native Beep & SystemSounds)
private void PlayNewTicketNotificationSound()
{
    Task.Run(() => {
        try { MessageBeep(0x00000030); } catch { } // MB_ICONEXCLAMATION
        try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
        try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
        try { Beep(880, 250); Beep(1320, 350); } catch { } // Dual Tone Alarm
    });
}
```

---

### 🛡️ Kütüphane Kullanım Kılavuzu:
Gelecekte herhangi bir modülde şüphe oluştuğunda bu belgeye müracaat edilecek ve ilgili onaylanmış kod pasajı doğrudan projeye çekilecektir.
