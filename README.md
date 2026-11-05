# Mini Dijital Ürün Pasaportu

ASP.NET Core (.NET 10), Entity Framework Core ve SQLite ile geliştirilen teknik ödev.

## Geliştirme durumu

1. **Tamamlandı - veritabanı ve test altyapısı:** Model, üretim partisi ve ürün tabloları; ilişkiler, benzersizlik kuralları ve ilk migration.
2. **Tamamlandı - alan doğrulaması:** GS1 kontrol basamağı, zorunlu alanlar, yüzdeler ve karbon değeri.
3. Sıradaki aşama: CSV okuma ve satır bazlı içe aktarma API'si.
4. HTML, JSON-LD, görünürlük ayrımı ve PNG QR kodu.
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
CSV içe aktarma API'si sonraki geliştirme aşamasında bu doğrulayıcıyı kullanacaktır.
