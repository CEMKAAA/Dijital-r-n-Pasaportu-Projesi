# Mini DPP: Adım adım kod rehberi

Bu rehber, tamamlanan geliştirme aşamalarıyla birlikte güncellenir.
Koddaki **ÖNEMLİ** yorumları, özellikle açıklayabilmen gereken kararları işaretler.
Dosyaları aşağıdaki sırayla okuyabilirsin.

## 1. Önce uygulamanın amacını bil

Uygulama CSV'deki ürün bilgilerini doğrulayacak, geçerli kayıtları SQLite'a
yazacak ve her GTIN için okunabilir HTML, JSON-LD ve PNG QR kodu sunacak.

Şu anda veritabanı, alan doğrulaması, CSV içe aktarma, HTML/JSON-LD pasaportları
ve PNG QR kodu tamamlandı. Yükleme ekranı sonraki aşamada eklenecek.

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
10. `MapImportEndpoints`, CSV yükleme endpoint'ini ekler.
11. `MapDppEndpoints`, HTML, JSON-LD ve QR endpoint'lerini ekler.
12. `RunAsync`, sunucuyu çalıştırır.

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

## 10. HTTP isteği uygulamaya nasıl giriyor?

Dosya: [ImportEndpoints.cs](../src/MiniDpp.Api/Endpoints/ImportEndpoints.cs).

`MapPost`, `POST /api/products/import` adresini tanımlar. Handler parametreleri:

- `IFormFile? file`: İstekteki `file` adlı form alanından alınır.
- `CsvImportService service`: Dependency injection tarafından sağlanır.
- `CancellationToken`: İstek iptal edilirse devam eden işlerin iptalini iletir.

Önce dosyanın varlığı ve boyutu kontrol edilir. Sonra UTF-8 olarak okunur.
Geçersiz UTF-8, dosya hatasıdır; henüz hiçbir kayıt yazılmadan 400 döner.

`StreamReader`, `new UTF8Encoding(false, true)` ile geçersiz bayt dizilerini
sessizce değiştirmek yerine hata bildirir. Başlangıçtaki UTF-8 BOM'u CSV
okuyucusu ayıklar.

`Results.Ok(report)`, raporu JSON olarak döndürür. Dosya düzeyindeki sorunlar
`Results.Problem` ile standart hata yanıtı olur. Satır hataları ise dosyanın
geri kalanını işlemeyi engellemez ve başarılı işleme raporunda listelenir.

`DisableAntiforgery`, hesap/cookie kimlik doğrulaması kullanmayan bu yerel
demo yüklemesinde form token zorunluluğunu kaldırır.

## 11. CSV okuyucusunun görevi

Dosyalar: [CsvProductReader.cs](../src/MiniDpp.Api/Importing/CsvProductReader.cs),
[CsvInputRow.cs](../src/MiniDpp.Api/Importing/CsvInputRow.cs).

CsvHelper 34 kullanıyoruz. Bu sürümde ayarlar `CsvReaderOptions` üzerinden
verilir; eski sürümlerin `CsvConfiguration` örnekleriyle API'si farklıdır.

Okuyucu başlığı kontrol eder, sütunları isimlerinden eşleştirir ve her kaydı
satır numarasıyla birlikte `CsvInputRow` olarak döndürür.

**ÖNEMLİ:** `Split(',')`, tırnak içindeki virgülü de ayırır. CSV kütüphanesi
tırnak, kaçırılmış tırnak ve çok satırlı alan sınırlarını doğru ele alır.

**ÖNEMLİ:** `TrackLineNumbers = true` ve `LineNumber`, dosyadaki fiziksel
satırı verir. Başlık 1'dir. `RowNumber` ise kütüphanenin başlığı saymayan veri
kaydı sayısıdır; ödevin istediği satır numarası için onu kullanmıyoruz.

`SkipEmptyLines = false`, boş bir veri satırının hata raporuna girmesini sağlar.
Bozuk tırnaklı kayıt kurtarılabilse bile callback ile işaretlenir ve kaydedilmez.

Kaynak: [CsvHelper okuyucu ayarları](https://joshclose.github.io/CsvHelper/api/csvreaderoptions.html).

## 12. Kayıt hizmeti ve transaction

Dosya: [CsvImportService.cs](../src/MiniDpp.Api/Importing/CsvImportService.cs).

İçe aktarmanın akışı:

1. CSV okuyucusu başlığı ve kayıt yapısını çözer.
2. Yapısal hatalı satır doğrudan hata listesine eklenir.
3. Diğer satır `ProductRowValidator` ile doğrulanır.
4. Geçerli satır için transaction açılır.
5. GTIN zaten varsa aynı bilgiler mi diye kontrol edilir.
6. Yeni üründe mevcut model ve parti aranır; gerekiyorsa oluşturulur.
7. `Products.Add`, ürün nesnesini ve yeni ilişkili nesneleri izlemeye alır.
8. `SaveChangesAsync`, SQL komutlarını çalıştırır.
9. `CommitAsync`, o satırın işlemini kalıcılaştırır.

**ÖNEMLİ:** Transaction bütün dosya için açılmaz. Aynı satırın model, parti ve
ürün değişiklikleri bir bütündür. Bir satır başarısızsa o satır geri alınır;
daha önce başarıyla kaydedilen satırlar korunur.

`await using`, commit edilmeden çıkılan transaction'ı dispose eder ve
işlemin geri alınmasını sağlar. Kayıt hatası raporlandıktan sonra sonraki
satırın işlenmesine devam edilir.

**ÖNEMLİ:** Veritabanı işleminin geri alınması, EF Core'un bellekte izlediği
nesneleri otomatik kaldırmaz. `finally` içindeki `ChangeTracker.Clear`,
başarısız satırın nesnelerinin sonraki `SaveChanges` çağrısına taşınmasını önler.

## 13. Aynı dosya neden tekrar tekrar ürün oluşturmuyor?

GTIN sorgusu transaction içerisinde yapılır. `HasSameValues`, mevcut kaydı
gelen model, parti, elyaf, yer ve karbon değerleriyle karşılaştırır.

- Aynı bilgiler varsa kayıt değiştirilmeden `unchanged` olarak raporlanır.
- Bilgiler farklıysa açıklayıcı bir satır hatası döner.
- GTIN yoksa yeni ürün kaydedilir.

Bu davranış aynı dosyanın tekrar gönderilmesini güvenli ve öngörülebilir yapar.
Güncelleme ve sürüm geçmişi bonusunu eklemiyoruz; README bu kararı açıklar.

`CsvImportReport`, toplam, yeni, değişmeyen ve reddedilen kayıt sayılarını;
satır bazlı hataları ve kabul edilen ürünlerin temel kimlik bilgilerini taşır.

## 14. Gerçek HTTP akışını nasıl test ediyoruz?

Dosyalar: [MiniDppFactory.cs](../tests/MiniDpp.Tests/MiniDppFactory.cs),
[CsvImportApiTests.cs](../tests/MiniDpp.Tests/CsvImportApiTests.cs),
[CsvProductReaderTests.cs](../tests/MiniDpp.Tests/CsvProductReaderTests.cs).

`WebApplicationFactory<Program>`, uygulamayı test sunucusunda başlatır.
`CreateClient`, bu sunucuya HTTP isteği gönderen istemciyi oluşturur.
Testler de kullanıcı gibi multipart form ile CSV dosyası gönderir.

Her factory ayrı bir bellek içi SQLite bağlantısı kullanır. Aynı migration'lar
bu veritabanına uygulanır; uygulamanın yerel dosyasına dokunulmaz.

Önemli senaryolar:

- İşverenin gerçek CSV dosyası yüklenir: 3 ürün, 4. ve 5. satırda hata.
- Tekrar yükleme kayıt sayısını artırmaz.
- Modeller ve partiler tekrar kullanılır.
- Aynı GTIN farklı bilgilerle geldiğinde eski değerler korunur.
- Teste özel SQLite trigger'ı bir ürün eklemesini bilerek başarısız yapar.
  Model ve partinin de geri alındığı ve sonraki ürünün kaydedildiği doğrulanır.
- Eksik dosya, hatalı başlık, fazla boyut ve geçersiz UTF-8 kontrol edilir.

## 15. Pasaport verisi ve görünürlük ayrımı

Dosyalar: [PassportService.cs](../src/MiniDpp.Api/Passports/PassportService.cs),
[ProductPassport.cs](../src/MiniDpp.Api/Passports/ProductPassport.cs),
[DppEndpoints.cs](../src/MiniDpp.Api/Endpoints/DppEndpoints.cs).

Endpoint, `rol` parametresinin tam olarak `yetkili` olup olmadığını kontrol eder.
Bu bilgi `PassportService.GetAsync` metoduna `includeCarbon` olarak gider.

Hizmet, veritabanı entity'sini doğrudan döndürmek yerine `Select` ile bir
`ProductPassport` nesnesi oluşturur. Bu sorguda ilişkili parti ve model
alanlarına erişmek, EF Core'un gerekli bağlantıları SQL'de kurmasını sağlar.
`AsNoTracking`, okuma sorgusunun kayıt değiştirme amacı taşımadığını belirtir.

**ÖNEMLİ:** Tüketici görünümünde `CarbonKgCo2e` alanı bu nesnede null olur.
Karbon değeri HTML şablonuna veya JSON-LD oluşturucusuna aktarılmaz.
Gizleme kararı CSS'e, JavaScript'e veya tarayıcıya bırakılmaz.

`ProductPassport.GetProperties`, iki çıktının da kullanacağı alan listesini
oluşturur. Karbon yalnızca bir değeri varsa bu listeye eklenir. `HasValue`
kontrolü sayesinde 0 değeri de yetkili görünümünde korunur.

Demo rolü gerçek kullanıcı hesabına bağlı değildir. Ödevin kapsamı gereği
görünüm URL parametresiyle seçilir.

## 16. HTML neden metinleri kodluyor?

Dosya: [PassportHtmlRenderer.cs](../src/MiniDpp.Api/Passports/PassportHtmlRenderer.cs).

Oluşturucu, ortak alan listesini `<dl>`, `<dt>` ve `<dd>` etiketleriyle gösterir.
JSON-LD ve QR bağlantılarını da ekler. Yetkili görünümünden JSON-LD'ye giden
bağlantı rol parametresini korur.

**ÖNEMLİ:** CSV'den gelen metinler HTML'e yazılmadan önce `HtmlEncoder` ile
kodlanır. Örneğin `<script>` metni bir HTML etiketi olarak çalıştırılmak yerine
sayfada metin olarak görünür. Başlık, alan değerleri ve bağlantı özelliklerinde
bu kurala uyulur.

CSS dosyası `wwwroot/styles.css` içindedir ve `UseStaticFiles` ile sunulur.
Sayfanın mobil düzeni CSS media query ile tek sütuna geçer.

## 17. JSON-LD nasıl oluşturuluyor?

Dosya: [PassportJsonLdRenderer.cs](../src/MiniDpp.Api/Passports/PassportJsonLdRenderer.cs).

JSON-LD, JSON verisine alanların anlamını belirten bir bağlam ekler:

- `@context`: `https://schema.org`
- `@type`: `Product`
- `@id` ve `url`: Ürünün kalıcı tüketici pasaportu adresi
- `name`: Model adı
- `gtin14`: Baştaki sıfırları korunmuş GTIN metni
- `material`: Elyaf bileşiminin okunabilir açıklaması
- `additionalProperty`: Ortak listedeki ürün özellikleri

Ek alanların her biri `PropertyValue` yapısındadır. `propertyID`, CSV alanına
karşılık gelen kimliktir; `name` etikettir; `value` asıl değerdir. Birim varsa
`unitText` eklenir. Sayısal değerlerin `object` alanında tutulması onları
metne dönüştürmez; JSON serileştiricisi gerçek `decimal` değerini sayı olarak yazar.

**ÖNEMLİ:** Karbon için ayrı bir JSON gizleme kuralı yazmıyoruz. Alan ortak
listede yoksa JSON-LD'de de bulunmaz. Böylece iki görünüm aynı kurala dayanır.

Kaynaklar: [Product](https://schema.org/Product),
[PropertyValue](https://schema.org/PropertyValue).

## 18. QR hedefi ve yapılandırma

Dosyalar: [DppOptions.cs](../src/MiniDpp.Api/Passports/DppOptions.cs),
[PassportUrlBuilder.cs](../src/MiniDpp.Api/Passports/PassportUrlBuilder.cs),
[QrCodeService.cs](../src/MiniDpp.Api/Passports/QrCodeService.cs).

`DppOptions`, `Dpp:PublicBaseUrl` ayarını taşır. `ValidateOnStart`, geçersiz
adresle sunucu çalışmaya başlamasını engeller. Kök HTTP/HTTPS adresi beklenir.

`PassportUrlBuilder`, bütün pasaport bağlantılarının yolunu tek yerde oluşturur.
Kalıcı URL, ayarlanan sunucu adresi ile GTIN yolunun birleştirilmesidir.

**ÖNEMLİ:** QR hedefi `HtmlUrl` ile oluşturulur ve rol parametresi içermez.
Yetkili görünümündeki QR da tüketici pasaportunu açar. Hedef adresi, isteğin
değiştirilebilir `Host` başlığından alınmaz.

QRCoder, URL metnini QR verisine çevirir. `ECCLevel.Q`, hata düzeltme seviyesini
seçer. `PngByteQRCode.GetGraphic(8)`, modül boyutu 8 piksel olan PNG baytlarını
üretir. `Results.File`, bunları `image/png` olarak HTTP yanıtına yazar.
Tarayıcıdaki `download` özelliği, bağlantıya tıklanınca PNG indirilmesini ister.

Uygulamanın çalışması için Python veya harici QR servisi gerekmez.

## 19. Pasaport testleri neleri doğruluyor?

Dosyalar: [PassportApiTests.cs](../tests/MiniDpp.Tests/PassportApiTests.cs),
[DppOptionsTests.cs](../tests/MiniDpp.Tests/DppOptionsTests.cs).

- Örnek dosyadaki üç geçerli ürünün HTML ve JSON-LD alanları karşılaştırılır.
- Tüketici görünümünde karbon etiketi ve değeri iki yanıtta da bulunmaz.
- Yetkili görünümünde karbon ve birimi iki yanıtta da vardır; 0 değeri de korunur.
- Geçersiz roller tüketici görünümünü kullanır.
- Kayıtsız veya bozuk GTIN için 404 döner.
- CSV'deki HTML etiketleri çalışabilir işaretleme hâlinde sunulmaz.
- JSON-LD içerik türü, PNG imzası ve CSS erişimi kontrol edilir.
- QR yanıtı rol parametresinden etkilenmez.
- İsteğin `Host` başlığı değiştirilse bile kalıcı adres yapılandırmadan alınır.

Ek yerel kontrolde üç PNG QR kodu bir QR okuyucu ile çözümlendi ve her
birinin doğru tüketici URL'sini içerdiği doğrulandı.

HTML'deki `98.0` ile JSON'daki `98`, aynı sayısal değeri temsil eder.
Karşılaştırma testleri sayıların yazımını değil, değer ve birim eşitliğini denetler.

## 20. Görüşmede açıklayabilmen gerekenler

- GTIN neden sayısal tür yerine metin olarak saklanıyor?
- Model ve parti neden ayrı tablolar? Bağlantı hangi alanlarda tutuluyor?
- `DbSet` tanımlamak ile migration uygulamak arasında ne fark var?
- `required` ile veritabanındaki yabancı anahtar kuralı neyi denetliyor?
- GS1 kontrol basamağı nasıl hesaplanıyor?
- Sayılar neden `decimal` ve `InvariantCulture` ile okunuyor?
- Yüzde toplamından önce neden aralık kontrolü yapılıyor?
- Hatalı bir satırdan neden `ValidatedProduct` oluşturulmuyor?
- Başlık 1 sayılırken CSV satır numarası nasıl doğru bulunuyor?
- Bir satırın kayıt hatası neden bütün dosyayı geri almıyor?
- Transaction geri alınınca neden `ChangeTracker.Clear` çağrılıyor?
- Aynı GTIN tekrar veya farklı bilgilerle gelirse ne oluyor?
- Karbon alanı iki çıktıdan aynı kuralla nasıl kaldırılıyor?
- Sıfır karbon değeri neden görünürlük kontrolünde kaybolmuyor?
- JSON-LD'deki `@context`, `@type` ve `PropertyValue` ne işe yarıyor?
- CSV'den gelen metinler HTML'e neden doğrudan yazılmıyor?
- QR neden tüketici adresini içeriyor ve neden sunucu adresi ayardan okunuyor?
