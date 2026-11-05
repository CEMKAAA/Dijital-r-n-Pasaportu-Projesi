# Mini Dijital Ürün Pasaportu

ASP.NET Core (.NET 10), Entity Framework Core ve SQLite ile geliştirilen teknik ödev.

## Geliştirme durumu

1. **Tamamlandı - veritabanı ve test altyapısı:** Model, üretim partisi ve ürün tabloları; ilişkiler, benzersizlik kuralları ve ilk migration.
2. **Tamamlandı - alan doğrulaması:** GS1 kontrol basamağı, zorunlu alanlar, yüzdeler ve karbon değeri.
3. **Tamamlandı - CSV içe aktarma API'si:** Satır numaralı hata raporu, satır başına transaction ve tekrar yükleme kontrolü.
4. Sıradaki aşama: HTML, JSON-LD, görünürlük ayrımı ve PNG QR kodu.
5. Yükleme ekranı ve uçtan uca kontroller.

Tamamlanan kodu okumak ve görüşmeye hazırlanmak için [adım adım kod rehberine](docs/KOD_REHBERI.md) bakın.

## Gereksinimler

- .NET 10 SDK
- Git
- İlk paket indirmesinde NuGet erişimi

Komutlar depo kökünde çalıştırılır:

```powershell
dotnet restore .\MiniDpp.sln
dotnet tool restore --tool-manifest .\dotnet-tools.json
dotnet run --project .\src\MiniDpp.Api --no-launch-profile --urls http://localhost:5100
```

Uygulama başlangıçta uygulanmamış migration'ları çalıştırır. Veritabanı dosyası
`src/MiniDpp.Api/mini-dpp.db` konumundadır ve Git'e eklenmez. Bu otomatik geçiş
yaklaşımı tek süreçli yerel demo içindir.

İlk kontrol: <http://localhost:5100/health>

## Testler

```powershell
dotnet test .\MiniDpp.sln --no-restore --disable-build-servers -m:1
```

Veritabanı testleri gerçek SQLite kullanır. Testlere ait veriler bellekte veya
benzersiz geçici dosyalarda oluşturulur; uygulamanın veritabanı kullanılmaz.

## Şema değişiklikleri

```powershell
dotnet ef migrations add DegisiklikAdi --project .\src\MiniDpp.Api --startup-project .\src\MiniDpp.Api
```

Üretilen migration ve model snapshot dosyaları kaynak kodla birlikte commit edilir.
Uygulama yeniden başlatıldığında migration uygulanır. Elle uygulamak için:

```powershell
dotnet ef database update --project .\src\MiniDpp.Api --startup-project .\src\MiniDpp.Api
```

## Veri modeli kararları

- Bir modelin birden fazla üretim partisi, bir partinin birden fazla ürün kaydı olabilir.
- Model adı benzersizdir. Parti numarası, ait olduğu model içerisinde benzersizdir.
- Model adı ve parti numarası değerleri büyük/küçük harfe duyarlı karşılaştırılır; uçlardaki boşluklar temizlenir.
- GTIN metin olarak saklanır ve benzersizdir; baştaki sıfırlar korunur.
- Elyaf yüzdeleri, üretim yeri ve karbon değeri ürün kaydında tutulur.
- Bağlı kayıtlar varken model veya parti silinmesi engellenir.
- Kaynak test verisi: `Assignment/urunler_duzeltilmis_test.csv`.

## Alan doğrulama kuralları

- Alanların başındaki ve sonundaki boşluklar temizlenir.
- GTIN boş olamaz; tam 14 ASCII rakamından oluşmalı ve GS1 kontrol basamağı doğru olmalıdır.
- Model adı, parti numarası ve üretim yeri boş olamaz.
- Pamuk ve elastan oranları sayısal olmalı, ayrı ayrı 0-100 aralığında bulunmalı ve toplamları tam 100 olmalıdır.
- Karbon değeri sayısal ve sıfır veya pozitif olmalıdır.
- Sayılar, bilgisayarın bölge ayarından bağımsız okunur. Ondalık ayırıcı noktadır; binlik ayırıcı ve bilimsel gösterim kabul edilmez.
- Bir satırdaki bütün alan hataları birlikte döner. Hatalı satır için `ValidatedProduct` oluşturulmaz.

Örnek dosya bu kurallarla değerlendirildiğinde beklenen sonuç **3 geçerli, 2 hatalı** satırdır.
Başlık 1. satır kabul edildiğinde 4. satırda GTIN eksiktir; 5. satırdaki elyaf toplamı ise 98'dir.
CSV içe aktarma API'si bu doğrulayıcıyı kullanır.

## CSV içe aktarma

Uygulama çalışırken, depo kökünde ikinci bir terminal açın:

```powershell
curl.exe --silent --show-error --fail-with-body -F "file=@Assignment/urunler_duzeltilmis_test.csv;type=text/csv" http://localhost:5100/api/products/import
```

Linux/macOS'ta aynı komutta `curl.exe` yerine `curl` kullanılır.

- Endpoint: `POST /api/products/import`
- İstek: `multipart/form-data`; dosya alanının adı `file`.
- Dosya: En fazla 1 MiB, UTF-8; BOM desteklenir.
- Ayırıcı: Virgül. Tırnak içindeki virgül, kaçırılmış çift tırnak ve satır sonları desteklenir.
- Başlıkta yedi örnek sütunun tümü bulunmalıdır; fazladan veya yinelenen sütun kabul edilmez.
- Sütun sırası değişebilir; başlıkların büyük/küçük harfi ve uç boşlukları dikkate alınmaz.
- Boş veri satırları atlanmaz; hata olarak raporlanır.
- CSV'yi Excel'de açıp yeniden kaydetmeyin; GTIN'in başındaki sıfırlar kaybolabilir.

Örnek dosyanın ilk yüklenmesinde yanıtın sayaçları:

```json
{
  "totalRows": 5,
  "importedCount": 3,
  "unchangedCount": 0,
  "rejectedCount": 2
}
```

Yanıt ayrıca iki liste içerir:

- `errors`: Her hatalı kayıt için `rowNumber` ve açıklamaları içeren `errors` listesi.
- `products`: Kabul edilen kayıtların `gtin`, `modelName`, `batchNumber` ve `status` değerleri.

| CSV satırı | Örnek dosyadaki hata |
|---|---|
| 4 | GTIN boş |
| 5 | Pamuk ve elastan toplamı %98 |

`totalRows` başlık hariç işlenen kayıt sayısıdır. `rowNumber`, başlığı 1 sayan
fiziksel satır numarasıdır. Tırnak içindeki alan birkaç satır sürerse kaydın
başlangıç satırı raporlanır. Sayaçlarda `importedCount + unchangedCount + rejectedCount = totalRows`.

### Tekrar yükleme ve kayıt davranışı

- Aynı GTIN ve aynı bilgiler: `unchangedCount` artar, ürünün durumu `unchanged` olur.
- Aynı GTIN ve farklı bilgiler: Satır hatası döner; mevcut ürün güncellenmez.
- Yeni GTIN: Ürün kaydedilir ve durumu `created` olur.
- Aynı model ve parti tekrar kullanılır; gereksiz model/parti kaydı oluşturulmaz.
- Her satır ayrı transaction ile işlenir. Bir satırın kayıt hatası, o satırın
  model, parti ve ürün değişikliklerini birlikte geri alır; diğer satırlar devam eder.

Örnek dosya ikinci kez yüklenirse sonuç: **0 yeni, 3 değişmeyen, 2 hatalı**.
Ürün güncelleme ve sürüm geçmişi bonusu bu kapsamda uygulanmamıştır.

### HTTP yanıtları

| Durum | HTTP |
|---|---|
| CSV işlendi; satır hataları `errors` içinde | 200 |
| Dosya eksik/boş, başlık hatalı, veri satırı yok veya UTF-8 geçersiz | 400 |
| Dosya 1 MiB sınırını aşıyor | 413 |

Dosya düzeyindeki hatalar `application/problem+json` biçiminde döner.
