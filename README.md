# Mini Dijital Ürün Pasaportu

CSV ürün verilerini doğrulayan, SQLite'a kaydeden ve HTML, JSON-LD, PNG QR çıktıları sunan teknik ödev.
ASP.NET Core / C# (.NET 10), EF Core ve basit HTML/CSS/JavaScript kullanır.

## Kurulum ve çalıştırma

Gereksinim: **.NET 10 SDK**. İlk paket indirmesinde NuGet erişimi gerekir.
Komutları depo kökünde çalıştırın:

```powershell
dotnet restore MiniDpp.sln
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet run --project src/MiniDpp.Api --no-launch-profile --urls http://localhost:5100
```

<http://localhost:5100> adresini açın. **Örnek CSV'yi indir** bağlantısından dosyayı
alıp yükleyin. Ekran kayıt sayılarını, hatalı satırları ve ürün pasaportu bağlantılarını gösterir.

Veritabanı ilk başlangıçta migration ile oluşturulur. Yerel dosya:
`src/MiniDpp.Api/mini-dpp.db`. Uygulama yeniden başladığında veriler korunur;
veritabanı dosyası Git'e eklenmez. Node.js veya Python, uygulamayı çalıştırmak için gerekmez.

## CSV içe aktarma

Uygulama çalışırken ikinci terminalde:

```powershell
curl.exe --silent --show-error --fail-with-body -F "file=@Assignment/urunler_duzeltilmis_test.csv;type=text/csv" http://localhost:5100/api/products/import
```

Linux/macOS'ta `curl.exe` yerine `curl` kullanın. Dosya UTF-8, virgülle ayrılmış
ve en fazla 1 MiB olmalıdır. CSV'yi Excel'de açıp kaydetmeyin; GTIN sıfırları kaybolabilir.

Örnek dosyanın ilk yüklemesi: **3 yeni kayıt, 2 hatalı satır**.
Başlık 1. satırdır; 4. satırda GTIN eksik, 5. satırda elyaf toplamı %98'dir.
Aynı dosya tekrar yüklenirse **0 yeni, 3 değişmeyen, 2 hatalı** sonucu döner.

## Örnek adresler

CSV içe aktarıldıktan sonra:

| Çıktı | Adres |
|---|---|
| HTML | <http://localhost:5100/dpp/08690000000012> |
| Yetkili HTML | <http://localhost:5100/dpp/08690000000012?rol=yetkili> |
| JSON-LD | <http://localhost:5100/dpp/08690000000012.jsonld> |
| Yetkili JSON-LD | <http://localhost:5100/dpp/08690000000012.jsonld?rol=yetkili> |
| PNG QR | <http://localhost:5100/dpp/08690000000012/qr.png> |
| Ürün listesi | <http://localhost:5100/api/products> |
| Sağlık kontrolü | <http://localhost:5100/health> |

Karbon varsayılan HTML ve JSON-LD'de bulunmaz; yalnızca `rol=yetkili` ile eklenir.
Bu, demo görünüm ayrımıdır; kullanıcı hesabı/JWT kullanılmaz.
QR her zaman tüketici HTML adresini açar.

QR hedefinin sunucu adresi `appsettings.json` içindeki **`Dpp:PublicBaseUrl`** ayarıdır.
Farklı portta, hem dinleme hem hedef adresini birlikte verin:

```powershell
dotnet run --project src/MiniDpp.Api --no-launch-profile -- --urls http://localhost:5110 --Dpp:PublicBaseUrl http://localhost:5110
```

Başka cihazdan QR açılacaksa hedef adresi o cihazdan erişilebilir olmalıdır.
`localhost`, QR'ı okuyan cihazın kendisini ifade eder.

## Testler

```powershell
dotnet test MiniDpp.sln -c Release --no-restore --disable-build-servers -m:1
```

İsteğe bağlı JavaScript HTTP istemci testleri için Node.js 20+:

```powershell
node tests/web/import-client.test.mjs
```

Kontroller: **121 .NET testi ve 9 JavaScript testi başarılı**. Testler ayrı SQLite
veritabanları kullanır. Release çıktısından dosya yükleme, pasaportlar, QR hedefi
ve yeniden başlatmada kalıcılık ayrıca HTTP üzerinden doğrulandı.

## Tasarım kararları

- Model, parti ve ürün ayrı ilişkili tablolardır. Model adı ve model+parti numarası benzersizdir.
- GTIN, baştaki sıfırları koruyan benzersiz metindir; 14 ASCII rakamı ve GS1 kontrol basamağı doğrulanır.
- Yüzdeler ve karbon `decimal` ile, bölge ayarından bağımsız okunur; ondalık ayırıcı noktadır.
- Her satır ayrı transaction ile kaydedilir. Hatalı satır diğer geçerli satırları engellemez.
- Aynı GTIN aynı bilgilerle tekrar gelirse kayıt çoğalmaz; farklı bilgilerle gelirse satır reddedilir.
- Model/parti değerleri büyük/küçük harfe duyarlıdır; uç boşlukları temizlenir.
- Migration'ların başlangıçta uygulanması tek süreçli yerel demo içindir.
- Basit HTML arayüz kullanıldı. İsteğe bağlı sürüm geçmişi bonusu uygulanmadı.

## Ayrıntılı belgeler

- [Adım adım kod rehberi ve önemli noktalar](docs/KOD_REHBERI.md)
- [API sözleşmesi, CSV kuralları ve migration komutları](docs/API.md)
- [Ödev gereksinimleri ve doğrulama sonuçları](docs/KAPSAM_KONTROLU.md)
- [Demo sırası ve görüşmeye hazırlık](docs/DEMO_VE_CALISMA.md)
