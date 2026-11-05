# Mini DPP: Adım adım kod rehberi

Bu rehber, tamamlanan geliştirme aşamalarıyla birlikte güncellenir.
Koddaki **ÖNEMLİ** yorumları, özellikle açıklayabilmen gereken kararları işaretler.
Dosyaları aşağıdaki sırayla okuyabilirsin.

## 1. Önce uygulamanın amacını bil

Uygulama CSV'deki ürün bilgilerini doğrulayacak, geçerli kayıtları SQLite'a
yazacak ve her GTIN için okunabilir HTML, JSON-LD ve PNG QR kodu sunacak.

Şu anda tamamlanan kısım veritabanı ve alan doğrulamasıdır. Dosya yükleme ve
pasaport endpoint'leri sonraki aşamalarda eklenecek.

## 2. Üç sınıf arasındaki ilişki

Dosyalar: [ProductModel.cs](../src/MiniDpp.Api/Models/ProductModel.cs),
[ProductBatch.cs](../src/MiniDpp.Api/Models/ProductBatch.cs),
[Product.cs](../src/MiniDpp.Api/Models/Product.cs).

```text
ProductModel (1) ---- (çok) ProductBatch (1) ---- (çok) Product
```

- `ProductModel`, `Slim Denim 501` gibi model adını tutar.
- `ProductBatch`, modele bağlı `B2027-01` gibi üretim partisini tutar.
- `Product`, GTIN'i ve o kaydın elyaf, üretim yeri ve karbon bilgilerini tutar.

`Id`, veritabanındaki iç kimliktir. `ProductModelId` ve `ProductBatchId`,
tablolar arasındaki bağlantıları tutan yabancı anahtarlardır.

Nesne özellikleri ise C# içerisinde ilişkili veriye erişmemizi sağlar:

```csharp
product.ProductBatch.ProductModel.Name
```

Bu ifade üründen partiye, partiden modele gider ve model adını okur.
Veritabanından sorgularken bu ilişkilerin yüklenmesini de sağlamamız gerekir;
testlerdeki `Include` ve `ThenInclude` çağrıları bunu yapar.

**ÖNEMLİ:** `required`, C# kodunda nesne oluştururken özelliğin atanmasını
zorunlu kılar. Veritabanındaki yabancı anahtar ve benzersizlik kuralları
ayrıca EF Core modeli içerisinde tanımlanır.

## 3. AppDbContext neden var?

Dosya: [AppDbContext.cs](../src/MiniDpp.Api/Data/AppDbContext.cs).

`AppDbContext`, EF Core üzerinden sorgulama, ekleme ve kaydetme işlemlerini
yapacağımız sınıftır. `DbContext` sınıfından miras alır.

`DbSet<Product> Products`, ürün kayıtlarına erişim noktasıdır.
`Products => Set<Product>()`, bu özelliğin okunmasında ilgili `DbSet`'i getirir.

`OnModelCreating` içerisinde şu kurallar bulunur:

| Kural | Gerekçesi |
|---|---|
| GTIN benzersiz | Bir pasaport adresinin tek bir ürün kaydını göstermesi |
| Model adı benzersiz | Aynı model için tekrar tekrar model kaydı açılmaması |
| Model + parti numarası birlikte benzersiz | Aynı modelin aynı partisinin tekrarlanmaması |
| Yabancı anahtarlar | Parti ve ürünün mevcut bir üst kayda bağlanması |
| `DeleteBehavior.Restrict` | Alt kayıtlar varken üst kaydın silinememesi |

`WithMany()` çağrısının boş olması ilişkinin çok tarafını kaldırmaz.
Üst sınıfta bir koleksiyon özelliği tanımlamadığımızı belirtir.

## 4. Program.cs uygulamayı nasıl başlatıyor?

Dosya: [Program.cs](../src/MiniDpp.Api/Program.cs).

1. `CreateBuilder`, yapılandırmayı ve uygulamanın hizmet altyapısını hazırlar.
2. `GetConnectionString`, `appsettings.json` içindeki bağlantı metnini okur.
3. `?? throw`, ayar eksikse açıklayıcı bir hata verir.
4. `SqliteConnectionStringBuilder`, SQLite bağlantı ayarlarını düzenler.
5. `Path.Combine`, veritabanı dosyasının tam yolunu içerik klasöründen oluşturur.
6. `AddDbContext`, context sınıfını dependency injection altyapısına kaydeder.
7. `Build`, uygulamayı oluşturur.
8. Başlangıçtaki scope içerisinde `MigrateAsync`, bekleyen migration'ları uygular.
9. `MapGet`, `/health` adresinin cevabını tanımlar.
10. `RunAsync`, sunucuyu çalıştırır.

**ÖNEMLİ:** Hizmet kayıtları `Build()` çağrısından önce yapılır.

Dependency injection, ihtiyaç duyulan nesnelerin uygulama tarafından
oluşturulup ilgili kodlara verilmesidir. Context'in bağlantı ayarları da
`DbContextOptions<AppDbContext>` üzerinden yapıcı metoda ulaşır.

`CreateAsyncScope`, burada başlangıç işleminin hizmet ömrünü sınırlar.
`await using`, işlem sonunda context gibi kaynakların temizlenmesini sağlar.
`await`, asenkron işlem bitene kadar bağımlı sonraki adıma geçmez.

## 5. Migration ve veritabanı dosyası

Klasör: [Migrations](../src/MiniDpp.Api/Migrations).

Migration, şema değişikliğini tarif eden kaynak koddur:

- `Up`, değişikliği uygular; ilk migration tabloları ve indeksleri oluşturur.
- `Down`, ilgili şema değişikliğini geri alacak işlemleri tarif eder.
- Model snapshot, EF Core'un sonraki değişiklikleri karşılaştıracağı son modeldir.

Migration dosyaları Git'e eklenir. `mini-dpp.db`, yerel çalışma verisidir ve
`.gitignore` sayesinde Git'e eklenmez.

EF Core, uygulanan migration'ları `__EFMigrationsHistory` tablosunda izler.
Bu yüzden uygulama her başladığında mevcut tabloları yeniden oluşturmaz.
Örnekte kullanılan başlangıçta migration uygulama yaklaşımı tek süreçli
yerel demo için seçilmiştir.

## 6. Ham CSV verisi ile doğrulanmış veri

Dosyalar: [CsvProductRow.cs](../src/MiniDpp.Api/Importing/CsvProductRow.cs),
[ValidatedProduct.cs](../src/MiniDpp.Api/Importing/ValidatedProduct.cs),
[ProductRowValidationResult.cs](../src/MiniDpp.Api/Importing/ProductRowValidationResult.cs).

`CsvProductRow` içerisindeki alanlar henüz metindir. Örneğin karbon alanına
`abc` yazılmış olabilir. Bu değer, anlamlı bir hata üretmek için doğrulayıcıya ulaşır.

`ValidatedProduct` içerisindeki yüzdeler ve karbon ise `decimal` türündedir.
Bu nesne yalnızca bütün alanlar doğruysa oluşturulur.

`record`, bu değerleri birlikte taşıyan bir C# veri yapısıdır. Buradaki
positional record tanımında parantezdeki alanlar, oluşturma parametreleri
ve okunabilir özellikler hâline gelir.

`ProductRowValidationResult`, başarılıysa ürünü, hatalıysa hata listesini taşır.
`IsValid`, hem ürünün var olduğunu hem hata listesinin boş olduğunu kontrol eder.

**ÖNEMLİ:** Bu veri yapıları veritabanı entity'lerinden ayrıdır. Doğrulama
veritabanına yazma işlemi yapmaz; bir satırın kuralları sağlayıp sağlamadığını belirler.

## 7. GTIN kontrolü

Dosya: [GtinValidator.cs](../src/MiniDpp.Api/Importing/GtinValidator.cs).

İlk kontrol, GTIN'in tam 14 ASCII rakamından oluşmasıdır. `char.IsAsciiDigit`,
yalnızca `0` ile `9` arasındaki karakterleri kabul eder.

**ÖNEMLİ:** GTIN bir tanımlayıcıdır ve `string` olarak kalır. Tamamını `long`
veya `int` türüne çevirmek baştaki sıfırları kaybettirir.

GTIN-14 için son rakam kontrol basamağıdır. İlk 13 rakam, soldan başlayarak
sırayla 3 ve 1 ile çarpılıp toplanır. Toplamı bir sonraki 10 katına tamamlayan
rakam, beklenen kontrol basamağıdır.

Örneğimizde `08690000000012` için ilk 13 rakamın ağırlıklı toplamı 38'dir.
40'a tamamlayan rakam 2'dir ve son karakterle eşleşir.

Kodda bunu şu ifade hesaplar:

```csharp
var expectedCheckDigit = (10 - weightedSum % 10) % 10;
```

Son `% 10`, toplam zaten 10'un katıysa kontrol basamağını 0 yapar.
`gtin[index] - '0'`, tek bir rakam karakterini sayısal değerine dönüştürür.
`!`, önceki kontrol nedeniyle burada null olmayacağını bildiğimiz değer için
derleyicinin null uyarısını bastırır; çalışma zamanında ayrı bir kontrol eklemez.

Kaynak: [GS1 kontrol basamağı açıklaması](https://www.gs1uk.org/knowledge-hub/standards/what-is-a-check-digit).
Bu kontrol GS1 kaydına çevrim içi sorgu yapmaz; biçim ve kontrol basamağını denetler.

## 8. Diğer alanlar nasıl doğrulanıyor?

Dosya: [ProductRowValidator.cs](../src/MiniDpp.Api/Importing/ProductRowValidator.cs).

- `Normalize`, eksik değeri boş metne çevirir ve uçlardaki boşlukları temizler.
- `ReadRequiredText`, zorunlu metinlerin boşluğunu kontrol eder.
- `decimal.TryParse`, geçersiz sayıda exception fırlatmadan başarısızlık bildirir.
- `InvariantCulture`, sonucun Windows'un Türkçe veya İngilizce ayarına bağlı olmasını önler.
- `NumberStyles`, ondalık noktası ve baştaki işarete izin verir; binlik ayırıcıyı kabul etmez.
- `TryReadPercentage`, sayısal olma ve 0-100 aralığı kontrollerini birlikte yapar.
- İki oran da geçerliyse toplamın tam 100 olması kontrol edilir.
- Karbon için sayısal olma ve negatif olmama kuralları uygulanır.

**ÖNEMLİ:** Önce yüzde aralıkları kontrol edilir, sonra toplama yapılır.
Böylece çok büyük değerler geldiğinde `decimal` taşması oluşmaz.

**ÖNEMLİ:** Aynı satırdaki bütün alan hataları listelenir. Hata varsa
`Product` sonucu null'dır; geçersiz değerler doğrulanmış nesneye aktarılmaz.

Sayısal gösterim örnekleri:

| Ham değer | Sonuç |
|---|---|
| `12.4` | Geçerli ondalık sayı |
| `12,4` | Geçersiz; nokta kullanılmalı |
| `1,000` | Geçersiz; binlik ayırıcı kabul edilmiyor |
| `NaN` veya `Infinity` | Geçersiz |
| `1e2` | Geçersiz; bilimsel gösterim kabul edilmiyor |

## 9. Testleri okuma sırası

1. [DatabaseTests.cs](../tests/MiniDpp.Tests/DatabaseTests.cs): Gerçek SQLite ile
   ilişki, benzersizlik, silme kısıtı, baştaki sıfırlar ve yeniden açmada kalıcılık.
2. [GtinValidatorTests.cs](../tests/MiniDpp.Tests/GtinValidatorTests.cs): Geçerli
   GS1 örnekleri, biçim hataları ve tek bir rakam değiştirildiğinde reddedilmesi.
3. [ProductRowValidatorTests.cs](../tests/MiniDpp.Tests/ProductRowValidatorTests.cs):
   Alan kuralları, sınır değerleri, sayı biçimi, taşma ve Türkçe sistem ayarı.

`[Fact]`, tek bir test senaryosudur. `[Theory]` ve `[InlineData]`, aynı davranışı
farklı girdilerle kontrol eder. `with`, record'un bir kopyasını belirtilen
alanları değiştirerek oluşturur; testlerde tek bir hatayı izole etmeyi kolaylaştırır.

## 10. Görüşmede açıklayabilmen gerekenler

- GTIN neden sayısal tür yerine metin olarak saklanıyor?
- Model ve parti neden ayrı tablolar? Bağlantı hangi alanlarda tutuluyor?
- `DbSet` tanımlamak ile migration uygulamak arasında ne fark var?
- `required` ile veritabanındaki yabancı anahtar kuralı neyi denetliyor?
- GS1 kontrol basamağı nasıl hesaplanıyor?
- Sayılar neden `decimal` ve `InvariantCulture` ile okunuyor?
- Yüzde toplamından önce neden aralık kontrolü yapılıyor?
- Hatalı bir satırdan neden `ValidatedProduct` oluşturulmuyor?
