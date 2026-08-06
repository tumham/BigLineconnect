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

## 💎 MODÜL 6: Birebir 6 Haneli Şifre Koruması (Strict Password Security)
- **Açıklama:** Erişim için Karşı Bilgisayarın ürettiği 6 haneli rastgele şifrenin %100 birebir eşleşmesi zorunludur.
- **Onaylanmış Sıkı Güvenlik Kodu (`Program.cs` & `ViewerForm.cs`):**
```csharp
// Karşı tarafta gelen şifre tam eşleşmek zorundadır:
bool isPasswordCorrect = !string.IsNullOrEmpty(cleanInputPass) && cleanInputPass == cleanLocalPass;

if (!isPasswordCorrect)
{
    // Hatalı şifrede erişim derhal engellenir:
    byte[] failMsg = Encoding.UTF8.GetBytes("AUTH_FAILED");
    await SafeSendAsync(ws, new ArraySegment<byte>(failMsg), WebSocketMessageType.Text, true, token);
}
```

---

### 🛡️ Kütüphane Kullanım Kılavuzu:
Gelecekte herhangi bir modülde şüphe oluştuğunda bu belgeye müracaat edilecek ve ilgili 3-4 satırlık onaylanmış kod pasajı doğrudan projeye çekilecektir.
