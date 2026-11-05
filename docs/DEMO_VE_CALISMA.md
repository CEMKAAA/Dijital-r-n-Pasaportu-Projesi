# Demo ve görüşmeye hazırlık

Önce [kod rehberini](KOD_REHBERI.md) sırayla oku. Her kararı kendi sözlerinle
açıklamaya çalış; ekran paylaşımında dosyalar üzerinden veri akışını gösterebilmelisin.

## Beş dakikalık demo sırası

1. Depo kökünde README'deki komutla uygulamayı çalıştır.
2. `http://localhost:5100` adresini aç; örnek CSV'yi indir ve dosya alanından yükle.
3. İlk yüklemede 3 yeni kayıt, 2 hatalı satır olduğunu göster.
4. 4. satırdaki boş GTIN'i ve 5. satırdaki %98 toplamını hata raporunda açıkla.
5. Bir ürünün HTML pasaportunu aç; model, parti, yer ve elyaf bilgilerini göster.
6. Yetkili görünümüne geç; karbon değerinin eklendiğini göster.
7. JSON-LD bağlantısını aç; `Product`, `gtin14`, `additionalProperty` ve rol farkını göster.
8. QR'ı PNG olarak indir; tüketici adresini kodladığını açıkla.
9. Ana sayfaya dönüp aynı CSV'yi tekrar yükle. Yeni kayıt yerine değişmeyen sayısının arttığını göster.
10. Uygulamayı durdurup yeniden başlat; ürünlerin korunduğunu göster.

Örnek daha önce yüklenmişse ilk denemede de değişmeyen kayıtlar görünmesi normaldir.
Demo için veritabanını silmek gerekmez; raporu mevcut duruma göre açıkla.

## Dosyalar üzerinden anlatma sırası

1. `Program.cs`: Yapılandırma, dependency injection, migration ve endpoint kayıtları.
2. `Models`: Model, parti ve ürün ilişkileri.
3. `Data/AppDbContext.cs`: Benzersizlik ve yabancı anahtar kuralları.
4. `Importing/CsvProductReader.cs`: CSV yapısı ve doğru satır numarası.
5. `Importing/ProductRowValidator.cs` ve `GtinValidator.cs`: Alan kuralları.
6. `Importing/CsvImportService.cs`: Satır başına transaction ve tekrar yükleme.
7. `Passports/PassportService.cs` ve `ProductPassport.cs`: Ortak veri ve karbon seçimi.
8. HTML/JSON-LD oluşturucuları ve `QrCodeService.cs`: Çıktılar.
9. `Web` ve `wwwroot`: Ana sayfa, dosya yükleme ve hata gösterimi.
10. `tests`: Örnek dosya, veri tutarlılığı ve hata senaryoları.

## Cevaplayabilmen gereken temel sorular

- GTIN neden `string`? Baştaki sıfırlar ve ürün kimliği olması.
- Model ve parti neden ayrı tablolar? Tekrarı azaltmak ve ilişkiyi korumak.
- `required` ile yabancı anahtar aynı şey mi? İlki C# nesne oluşturmayı, ikincisi veritabanı ilişkisini denetler.
- Niçin tek dosya transaction'ı kullanılmadı? Bir satırın hatası diğer geçerli satırları geri almamalı.
- Rollback sonrası `ChangeTracker.Clear` niçin var? Başarısız nesneler sonraki kayda taşınmamalı.
- GS1 hesabı nasıl çalışıyor? İlk 13 rakama 3/1 ağırlıkları, toplamı 10'un katına tamamlayan son rakam.
- Sayılar neden `decimal` ve `InvariantCulture` ile okunuyor? Ondalık hassasiyet ve bilgisayar ayarından bağımsız sonuç.
- Karbon niçin iki çıktıdan da kayboluyor? Ortak pasaport nesnesi ve özellik listesi görünümü belirliyor.
- Demo rolü nasıl seçiliyor? `rol=yetkili` sorgu parametresiyle.
- QR neden farklı bir sunucu adresi ayarına ihtiyaç duyuyor? QR mutlak ve erişilebilir bir adres taşımalı.

## Küçük değişiklik alıştırmaları

Bu alıştırmalar mevcut teslim davranışını değiştirmeden, ayrı bir çalışma
dalında denenebilir. Her değişiklikten sonra ilgili testleri çalıştır.

### 1. Yüzde hatasına gerçek toplamı ekle

`ProductRowValidator` içerisindeki toplam hatasına gelen toplamı da yazdır.
Aralık kontrolünden sonra hesaplama yapıldığını koru. Hata raporunun API ve
ekranda ek ön yüz değişikliği olmadan güncellendiğini göster.

### 2. Bir pasaport etiketini değiştir

`ProductPassport.GetProperties` içerisindeki bir etiketi değiştir. HTML ve
JSON-LD'nin aynı listeden geldiği için birlikte değiştiğini açıkla.

### 3. Dosya boyutu sınırını değiştir

Sunucudaki `ImportEndpoints.MaxUploadBytes`, istemcideki `MAX_UPLOAD_BYTES`,
sınır testleri ve belgelerin birlikte güncellenmesi gerektiğini bul.
Tarayıcı kontrolünün kullanıcıya erken geri bildirim verdiğini, sunucunun
kendi kontrolünü de uyguladığını açıkla.

## Tarayıcıda son elle kontrol

- Dosya seç, yükle; butonun yükleme sırasında pasifleştiğini ve tekrar açıldığını kontrol et.
- Hata tablosu ve kayıtlı ürünlerin aynı sayfada güncellendiğini kontrol et.
- HTML, JSON-LD ve QR bağlantılarını aç.
- Yetkili/tüketici geçişini ve JSON-LD bağlantısının rolü koruduğunu kontrol et.
- Pencereyi daralt; tabloların kendi alanlarında kaydırılabildiğini kontrol et.
- Klavyeyle form, butonlar ve bağlantılar arasında ilerle.

## Teslim

Son commit'ten sonra GitHub'a gönder. README, kaynak kod, testler, migration,
örnek CSV ve Türkçe commit geçmişinin depoda bulunduğunu kontrol et.
Depo herkese açık olmalı veya değerlendiriciye erişim verilmelidir.
Yönergede verilen son teslim: **10 Kasım 2026, 23.59 Türkiye saati**.
