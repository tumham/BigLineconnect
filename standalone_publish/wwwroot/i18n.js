// Internationalization (i18n) engine for BigLineconnect
const translations = {
    tr: {
        // Navbar
        "nav.features": "Özellikler",
        "nav.pricing": "Paketler",
        "nav.download": "İndir",
        "nav.connect": "Hızlı Bağlan",
        "nav.bayi": "Bayi Portalı",
        "nav.admin": "Yönetici Girişi",
        "nav.subtitle": "UZAKTAN ERİŞİM SİSTEMİ",

        // Promo & Free Trial
        "promo.banner": "🎁 30 GÜN ÜCRETSİZ FULL KULLANIM: Kredi kartı gerekmez, hemen indirin ve sınırsız deneyin!",
        "hero.nocard": "<i class=\"fa-solid fa-circle-check\" style=\"color:#2ecc71\"></i> Kredi Kartı Gerekmez • İlk 1 Ay Ücretsiz Full Kullanım",
        "pricing.promo": "🔥 TÜM PAKETLERDE İLK 1 AY %100 ÜCRETSİZ FULL KULLANIM HEDİYE!",

        // Hero
        "hero.badge": "Güvenli & Yerli Uzaktan Destek Altyapısı",
        "hero.title": "Güçlü, Hızlı ve Kesintisiz <br><span class=\"gradient-text\">Uzaktan Erişim Sistemi</span>",
        "hero.desc": "BigLineconnect ile müşterilerinize ve uzak cihazlarınıza saniyeler içinde bağlanın. Port açma gerektirmeyen tak-çalıştır mimarisi, 60 FPS akıcı görüntü transferi ve kurumsal kota yönetimi ile kesintisiz teknik destek sağlayın.",
        "hero.btn.download": "Windows İçin İndir (.exe)",
        "hero.btn.browser": "Tarayıcıdan Hızlı Bağlan",
        "hero.stat1": "60 FPS Akıcı Görüntü",
        "hero.stat2": "TLS 1.3 Şifreleme",
        "hero.stat3": "Yüksek Bulut Kapasitesi",

        // Widget
        "widget.title": "Tarayıcıdan Anında Bağlanın",
        "widget.label": "UZAKTAKİ MASA ID'Sİ (REMOTE ID)",
        "widget.placeholder": "Örn: 123 456 789",
        "widget.connect": "Bağlan",
        "widget.footer": "Güvenli yerel ağ bağlantısı",

        // Features
        "features.title": "Neden BigLineconnect?",
        "features.desc": "Rakipsiz hız, uçtan uca güvenlik ve esnek lisanslama seçenekleri ile işinizi büyütün.",
        "feat1.title": "60 FPS Akıcı Ekran Transferi",
        "feat1.desc": "Düşük gecikme süreli grafik motoru sayesinde uzak bilgisayarları sanki başındaymış gibi yönetin.",
        "feat2.title": "Uçtan Uca 256-bit Şifreleme",
        "feat2.desc": "TLS 1.3 ve SHA-256 kriptografik koruma ile tüm veri akışınız güvence altındadır.",
        "feat3.title": "Port Açma Gerektirmez",
        "feat3.desc": "Modem veya güvenlik duvarı ayarı gerekmez. Kurulumsuz çalıştırın, doğrudan bağlanın.",
        "feat4.title": "Çoklu Operatör & Kanal Yönetimi",
        "feat4.desc": "Ekibinizdeki teknik destek personelini gruplayın, eşzamanlı kanal ve kota kullanımını izleyin.",
        "feat5.title": "Unattended (Arka Plan) Cihaz Kaydı",
        "feat5.desc": "Müşteri bilgisayarlarına şifreli servis kurarak 7/24 kesintisiz erişim ve yönetim imkanı.",
        "feat6.title": "Mobil & Web Tam Uyumlu",
        "feat6.desc": "Masaüstü uygulamasına ek olarak web tarayıcılarından ve mobil cihazlardan sorunsuz bağlanın.",

        // Pricing
        "pricing.title": "Abonelik ve Paketler",
        "pricing.desc": "İhtiyacınıza en uygun paketi seçin, anında kullanmaya başlayın.",
        "pkg1.name": "Başlangıç",
        "pkg1.price": "₺1.490",
        "pkg1.period": "/ yıl",
        "pkg1.f1": "<strong>1 Operatör</strong> Lisansı",
        "pkg1.f2": "<strong>5 Eşzamanlı Kanal</strong>",
        "pkg1.f3": "<strong>50 Unattended Cihaz</strong> Kaydı",
        "pkg1.f4": "Tam Web & Masaüstü Desteği",
        "pkg1.f5": "7/24 Kesintisiz Sunucu Erişimi",
        "pkg1.btn": "Lisans Tanımla",

        "pkg2.badge": "En Çok Tercih Edilen",
        "pkg2.name": "Pro",
        "pkg2.price": "₺3.990",
        "pkg2.period": "/ yıl",
        "pkg2.f1": "<strong>2 Operatör</strong> Lisansı",
        "pkg2.f2": "<strong>10 Eşzamanlı Kanal</strong>",
        "pkg2.f3": "<strong>100 Unattended Cihaz</strong> Kaydı",
        "pkg2.f4": "Tam Web & Masaüstü Desteği",
        "pkg2.f5": "Öncelikli Teknik Destek",
        "pkg2.btn": "Lisans Tanımla",

        "pkg3.name": "Pro+",
        "pkg3.price": "₺4.990",
        "pkg3.period": "/ yıl",
        "pkg3.f1": "<strong>3 Operatör</strong> Lisansı",
        "pkg3.f2": "<strong>15 Eşzamanlı Kanal</strong>",
        "pkg3.f3": "<strong>150 Unattended Cihaz</strong> Kaydı",
        "pkg3.f4": "Tam Web & Masaüstü Desteği",
        "pkg3.f5": "Özel Müşteri Temsilcisi",
        "pkg3.btn": "Lisans Tanımla",

        "pkg4.name": "Kurumsal",
        "pkg4.price": "Özel Teklif",
        "pkg4.period": "",
        "pkg4.f1": "<strong>Sınırsız Operatör</strong>",
        "pkg4.f2": "<strong>Sınırsız / Esnek Kanal</strong>",
        "pkg4.f3": "<strong>Sınırsız Cihaz Kaydı</strong>",
        "pkg4.f4": "Özel Sunucu Konfigürasyonu",
        "pkg4.f5": "Özel Logo & Markalama",
        "pkg4.btn": "İletişime Geçin",

        // Download
        "download.title": "Masaüstü Uygulamasını İndirin",
        "download.desc": "Windows 10, 11 ve Sunucu işletim sistemleri için optimize edilmiş Native AOT exe sürümünü tek tıkla yükleyin.",
        "download.btn": "Windows İstemcisi İndir",

        // Footer
        "footer.copyright": "© 2026 Bigus Mobile ERP & Yazılım Teknolojileri. Tüm hakları saklıdır.",

        // Bayi Portal
        "bayi.title": "Bayi Portalı & Müşteri Yönetimi",
        "bayi.tab.login": "Bayi Girişi",
        "bayi.tab.register": "Bayi Kaydı",
        "bayi.label.id": "Bayi Kodu veya E-Posta",
        "bayi.label.pass": "Şifre",
        "bayi.btn.login": "Giriş Yap",
        "bayi.label.company": "Firma Adı",
        "bayi.label.name": "Yetkili Ad Soyad",
        "bayi.label.email": "E-Posta Adresi",
        "bayi.label.phone": "Telefon",
        "bayi.btn.register": "Bayi Hesabı Oluştur",
        "bayi.dash.code": "Bayi Kodu (Tenant ID)",
        "bayi.dash.quota": "Kayıtlı Cihaz / Servis Kotası",
        "bayi.dash.crm": "Geçmiş CRM Destek Kaydı",
        "bayi.dash.devices": "Bayinize Kayıtlı Cihazlar & Servisler",
        "bayi.dash.history": "Müşteri Destek Geçmişi",

        // Admin Portal
        "admin.title": "Türkiye & Dünya Geneli Canlı Yönetim & Analiz Paneli",
        "admin.login.title": "Yönetici Girişi",
        "admin.login.desc": "Yönetim paneline erişmek için ana şifrenizi girin:",
        "admin.login.btn": "Giriş Yap & Paneli Aç"
    },
    en: {
        // Navbar
        "nav.features": "Features",
        "nav.pricing": "Pricing",
        "nav.download": "Download",
        "nav.connect": "Quick Connect",
        "nav.bayi": "Partner Portal",
        "nav.admin": "Admin Access",
        "nav.subtitle": "REMOTE DESKTOP CLIENT",

        // Promo & Free Trial
        "promo.banner": "🎁 30-DAY FREE FULL TRIAL: No credit card required, download & connect instantly!",
        "hero.nocard": "<i class=\"fa-solid fa-circle-check\" style=\"color:#2ecc71\"></i> No Credit Card Required • First 1 Month Free Full Access",
        "pricing.promo": "🔥 FIRST 1 MONTH 100% FREE FULL TRIAL INCLUDED ON ALL PLANS!",

        // Hero
        "badge": "Secure & High-Performance Remote Support Infrastructure",
        "hero.title": "Powerful, Fast and Seamless <br><span class=\"gradient-text\">Remote Access System</span>",
        "hero.desc": "Connect to your clients and remote computers in seconds with BigLineconnect. Enjoy plug-and-play architecture with zero port forwarding, 60 FPS smooth screen sharing, and enterprise quota management.",
        "hero.btn.download": "Download for Windows (.exe)",
        "hero.btn.browser": "Instant Browser Connect",
        "hero.stat1": "60 FPS Smooth Display",
        "hero.stat2": "TLS 1.3 Encryption",
        "hero.stat3": "High Cloud Capacity",

        // Widget
        "widget.title": "Instant Browser Access",
        "widget.label": "REMOTE DESKTOP ID",
        "widget.placeholder": "e.g. 123 456 789",
        "widget.connect": "Connect Now",
        "widget.footer": "Encrypted direct network connection",

        // Features
        "features.title": "Why Choose BigLineconnect?",
        "features.desc": "Empower your technical support team with unmatched speed, end-to-end security, and flexible licensing.",
        "feat1.title": "60 FPS Ultra-Fast Streaming",
        "feat1.desc": "Low-latency graphics engine allows you to control remote desktops as if sitting right in front of them.",
        "feat2.title": "End-to-End 256-bit Encryption",
        "feat2.desc": "TLS 1.3 and SHA-256 cryptographic security keep all your data streams completely safe.",
        "feat3.title": "Zero Port Forwarding Needed",
        "feat3.desc": "No firewall or router configuration required. Run instantly and establish direct connections.",
        "feat4.title": "Multi-Operator & Channel Control",
        "feat4.desc": "Group your IT support engineers, monitor concurrent channels, and manage usage quotas effortlessly.",
        "feat5.title": "Unattended Background Access",
        "feat5.desc": "Install encrypted background services on client computers for 24/7 uninterrupted remote management.",
        "feat6.title": "Full Web & Mobile Compatibility",
        "feat6.desc": "In addition to the native desktop client, connect seamlessly from any web browser or mobile device.",

        // Pricing
        "pricing.title": "Subscription & Global Plans",
        "pricing.desc": "Select the plan that fits your business needs and start connecting instantly.",
        "pkg1.name": "Starter",
        "pkg1.price": "$49",
        "pkg1.period": "/ year",
        "pkg1.f1": "<strong>1 Operator</strong> License",
        "pkg1.f2": "<strong>5 Concurrent Channels</strong>",
        "pkg1.f3": "<strong>50 Unattended Devices</strong>",
        "pkg1.f4": "Full Web & Desktop Access",
        "pkg1.f5": "24/7 Cloud Relay Access",
        "pkg1.btn": "Get License",

        "pkg2.badge": "Most Popular",
        "pkg2.name": "Pro",
        "pkg2.price": "$129",
        "pkg2.period": "/ year",
        "pkg2.f1": "<strong>2 Operators</strong> License",
        "pkg2.f2": "<strong>10 Concurrent Channels</strong>",
        "pkg2.f3": "<strong>100 Unattended Devices</strong>",
        "pkg2.f4": "Full Web & Desktop Access",
        "pkg2.f5": "Priority Technical Support",
        "pkg2.btn": "Get License",

        "pkg3.name": "Pro+",
        "pkg3.price": "$169",
        "pkg3.period": "/ year",
        "pkg3.f1": "<strong>3 Operators</strong> License",
        "pkg3.f2": "<strong>15 Concurrent Channels</strong>",
        "pkg3.f3": "<strong>150 Unattended Devices</strong>",
        "pkg3.f4": "Full Web & Desktop Access",
        "pkg3.f5": "Dedicated Account Manager",
        "pkg3.btn": "Get License",

        "pkg4.name": "Enterprise",
        "pkg4.price": "Custom Quote",
        "pkg4.period": "",
        "pkg4.f1": "<strong>Unlimited Operators</strong>",
        "pkg4.f2": "<strong>Flexible / Unlimited Channels</strong>",
        "pkg4.f3": "<strong>Unlimited Devices</strong>",
        "pkg4.f4": "Dedicated Relay Server",
        "pkg4.f5": "Custom Branding & Logo",
        "pkg4.btn": "Contact Us",

        // Download
        "download.title": "Download Desktop Application",
        "download.desc": "Get the Native AOT optimized Windows client for Windows 10, 11, and Server OS.",
        "download.btn": "Download Windows Client (.exe)",

        // Footer
        "footer.copyright": "© 2026 Bigus Mobile ERP & Software Technologies. All rights reserved.",

        // Bayi Portal
        "bayi.title": "Partner Portal & Client Management",
        "bayi.tab.login": "Partner Login",
        "bayi.tab.register": "Partner Registration",
        "bayi.label.id": "Partner Code or Email",
        "bayi.label.pass": "Password",
        "bayi.btn.login": "Sign In",
        "bayi.label.company": "Company Name",
        "bayi.label.name": "Contact Person",
        "bayi.label.email": "Email Address",
        "bayi.label.phone": "Phone Number",
        "bayi.btn.register": "Create Partner Account",
        "bayi.dash.code": "Partner Code (Tenant ID)",
        "bayi.dash.quota": "Registered Host Quota",
        "bayi.dash.crm": "CRM Support History",
        "bayi.dash.devices": "Registered Devices & Services",
        "bayi.dash.history": "Client Support Log",

        // Admin Portal
        "admin.title": "Global Live Control & Analytics Dashboard",
        "admin.login.title": "Administrator Login",
        "admin.login.desc": "Enter master password to access control panel:",
        "admin.login.btn": "Login & Open Dashboard"
    }
};

// Current Active Language
let currentLang = 'tr';

function initLanguage() {
    const saved = localStorage.getItem('bigline_lang');
    if (saved && (saved === 'tr' || saved === 'en')) {
        currentLang = saved;
    } else {
        // Auto detect browser language
        const userLang = navigator.language || navigator.userLanguage;
        if (userLang && userLang.toLowerCase().startsWith('tr')) {
            currentLang = 'tr';
        } else {
            currentLang = 'en'; // Default to Global English
        }
    }
    applyTranslations();
}

function setLanguage(lang) {
    if (lang !== 'tr' && lang !== 'en') return;
    currentLang = lang;
    localStorage.setItem('bigline_lang', lang);
    applyTranslations();
}

function toggleLanguage() {
    setLanguage(currentLang === 'tr' ? 'en' : 'tr');
}

function applyTranslations() {
    const langData = translations[currentLang] || translations.tr;

    // Update data-i18n elements
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (langData[key]) {
            el.innerHTML = langData[key];
        }
    });

    // Update placeholders
    document.querySelectorAll('[data-i18n-ph]').forEach(el => {
        const key = el.getAttribute('data-i18n-ph');
        if (langData[key]) {
            el.placeholder = langData[key];
        }
    });

    // Update language toggle buttons UI
    document.querySelectorAll('.lang-btn-text').forEach(btn => {
        btn.textContent = currentLang === 'tr' ? '🇹🇷 TR' : '🇬🇧 EN';
    });
}

// Auto init on DOM ready
document.addEventListener('DOMContentLoaded', initLanguage);
