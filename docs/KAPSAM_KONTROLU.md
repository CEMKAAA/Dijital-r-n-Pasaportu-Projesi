# Ödev gereksinimleri ve teslim kontrolü

Yerel ödev yönergesindeki zorunlu kapsam esas alınmıştır. Kaynak PDF yerelde
tutulur; özgün örnek CSV kaynak kod deposuna dahildir.

## Zorunlu işlevler

| Gereksinim | Uygulamadaki karşılığı | Doğrulama |
|---|---|---|
| CSV yükleme | Ana sayfadaki form ve `POST /api/products/import` | `HomeApiTests`, `CsvImportApiTests` |
| Hatalı satırı kaydetmeme, diğerlerine devam | Alan doğrulaması ve satır başına transaction | Örnek CSV 3 kayıt / 2 hata; zorunlu veritabanı hatası testi |
| Satır numarası ve anlaşılır neden | Başlık 1 sayılır; fiziksel başlangıç satırı ve hata listesi | `CsvProductReaderTests`; örnek hatalar 4 ve 5 |
| Kalıcı veritabanı | SQLite, ilk migration, başlangıçta migration uygulama | Dosyayı yeniden açma testi ve yayımlanmış uygulamayı yeniden başlatma |
| Model ve batch/lot ayrı ilişkili tablolar | `ProductModels`, `ProductBatches`, `Products`; yabancı anahtarlar | `DatabaseTests`, model/parti yeniden kullanım testi |
| GTIN metin ve baştaki sıfırlar | `string` ve SQLite `TEXT`; benzersiz indeks | Veritabanı gidiş-dönüş, HTTP ve JSON-LD testleri |
| HTML pasaport | `GET /dpp/{gtin}` | Üç geçerli ürün ve iki görünüm için alan karşılaştırması |
| schema.org/Product JSON-LD | `GET /dpp/{gtin}.jsonld` | Tip, GTIN, sayısal alanlar, birimler ve içerik türü testleri |
| PNG QR | `GET /dpp/{gtin}/qr.png` | PNG imzası; üç QR'ın bağımsız okuyucuyla çözümlenmesi |
| Tüketicide karbon gizli | Ortak pasaport nesnesinde karbon null; özellik listesine eklenmez | HTML ve JSON-LD'de alan/değer yokluğu testleri |
| `rol=yetkili` ile karbon görünür | Aynı sorgu ve ortak özellik listesi | İki çıktıda görünürlük ve sıfır karbon testi |
| Kurulum/çalıştırma/CSV/URL/varsayım README'si | Kısa README ve ayrıntılı API belgesi | Komutlar, örnek dosya ve yayımlanabilir çıktı kontrolü |

## Alan kuralları

| Kural | Durum |
|---|---|
| GTIN boş olamaz, 14 rakam ve GS1 kontrol basamağı | Doğrulandı |
| Pamuk ve elastan sayısal, 0-100 arası, toplam 100 | Doğrulandı |
| Model, parti ve üretim yeri boş olamaz | Doğrulandı |
| Karbon sayısal ve negatif olmayan değer | Doğrulandı |
| CSV'deki baştaki sıfırların korunması | Doğrulandı; örnek CSV baytları değiştirilmeden indiriliyor |

## Çalıştırılan kontroller

- Debug ve Release: **121 .NET testi başarılı**.
- JavaScript HTTP istemcisi: **9 test başarılı**.
- JavaScript dosyalarında sözdizimi kontrolü başarılı.
- Release publish çıktısı ayrı bir yerel klasörde çalıştırıldı.
- Publish çıktısında ana sayfa, örnek CSV, dosya yükleme, ürün listesi,
  bütün pasaport bağlantıları, iki görünüm, CSS ve JS dosyaları HTTP ile doğrulandı.
- Örnek CSV: **3 yeni ürün; 4. ve 5. satırlarda hata**.
- Tekrar yükleme: **0 yeni, 3 değişmeyen, 2 hatalı**.
- Publish uygulaması kapatılıp açıldı; üç kayıt ve pasaportlar korundu.
- QR kodunun çözümlenen adresi tüketici HTML adresidir; `rol` içermez.
- Yerel test veritabanları ve publish çıktısı `.artifacts/` altındadır ve Git'e eklenmez.

## Açıkça belirtilen kapsam

- ASP.NET Core / C# kullanıldı. Basit HTML/CSS/JavaScript arayüz, yönergede kabul edilen seçenektir.
- React ve TypeScript zorunlu olmadığı için ayrı bir ön yüz derleme altyapısı eklenmedi.
- Buluta yayınlama ve kullanıcı hesabı/JWT, yönergede beklenmiyor.
- Önceki ürün sürümlerini saklama **isteğe bağlı bonusu uygulanmadı**.
  Farklı bilgilerle gelen mevcut GTIN, açıklayıcı hatayla reddedilir.

## Son kullanıcı tarafından tamamlanacaklar

- [ ] Ana sayfayı ve bir pasaportu tarayıcıda masaüstü/mobil genişlikte gözden geçir.
- [ ] Son değişiklikleri verilen Türkçe mesajla commit et.
- [ ] GitHub deposuna gönder; inceleme için erişimi kontrol et.
- [ ] Depo bağlantısını teslim e-postasına kendin ilet.

**Görsel kontrol sınırı:** Bu oturumda tarayıcı süreci başlatma, izin politikası
tarafından engellendi. HTML/JSON-LD, CSS/JS erişimi ve HTTP akışı doğrulandı;
gerçek tarayıcıda tıklama ve masaüstü/mobil ekran görüntüsü kontrolü yapılmış
olarak raporlanmıyor.
