# API ve veri sözleşmesi

Komutlar depo kökünde çalıştırılır; örnek sunucu adresi `http://localhost:5100`.

## Endpoint'ler

| Metot | Yol | Görevi |
|---|---|---|
| GET | `/` | CSV yükleme ekranı ve tüketici ürün listesi |
| GET | `/sample.csv` | Depodaki özgün örnek CSV'yi indirme |
| GET | `/api/products` | Karbon içermeyen temel ürün listesi |
| POST | `/api/products/import` | CSV içe aktarma |
| GET | `/dpp/{gtin}` | HTML pasaport |
| GET | `/dpp/{gtin}.jsonld` | schema.org/Product JSON-LD |
| GET | `/dpp/{gtin}/qr.png` | Tüketici HTML adresini kodlayan PNG |
| GET | `/health` | `{"status":"ok"}` yanıtı |

## CSV isteği

İçerik türü `multipart/form-data`, dosya alanı `file` olmalıdır:

```powershell
curl.exe --silent --show-error --fail-with-body -F "file=@Assignment/urunler_duzeltilmis_test.csv;type=text/csv" http://localhost:5100/api/products/import
```

Dosya sınırı 1 MiB'dir. UTF-8 ve UTF-8 BOM desteklenir. Ayırıcı virgüldür;
tırnak içindeki virgül, kaçırılmış çift tırnak ve çok satırlı alanlar desteklenir.
Başlık şu yedi alanı içermelidir:

```csv
gtin,model_adi,batch_no,elyaf_pamuk_yuzde,elyaf_elastan_yuzde,uretim_yeri,karbon_kg_co2e
```

Sütun sırası değişebilir. Başlıkta büyük/küçük harf ve uç boşlukları dikkate
alınmaz. Eksik, fazladan veya yinelenen sütun, dosya düzeyinde hatadır.
Boş veri satırları atlanmaz; satır hatası olarak raporlanır.

## Doğrulama

| Alan | Kural |
|---|---|
| GTIN | Boş olamaz; tam 14 ASCII rakamı, geçerli GS1 kontrol basamağı |
| Model adı, parti, üretim yeri | Uç boşlukları temizlendikten sonra boş olamaz |
| Pamuk ve elastan | Sayısal, ayrı ayrı 0-100, toplamları tam 100 |
| Karbon | Sayısal ve negatif olmayan değer |

Ondalık ayırıcı noktadır. Binlik ayırıcı, bilimsel gösterim, `NaN` ve `Infinity`
kabul edilmez. Sayılar `decimal` ile, bilgisayarın bölge ayarından bağımsız okunur.
Bir satırın bütün alan hataları birlikte döner; hatalı satırın model/parti/ürünü kaydedilmez.

## İçe aktarma yanıtı

CSV işlendiğinde, bazı veya bütün satırlar hatalı olsa da HTTP 200 ile rapor döner.
Örnek dosyanın ilk yüklenmesinde sayaçlar:

```json
{
  "totalRows": 5,
  "importedCount": 3,
  "unchangedCount": 0,
  "rejectedCount": 2
}
```

Tam yanıt bu sayaçlara ek olarak `errors` ve `products` listelerini içerir:

```json
{
  "rowNumber": 4,
  "errors": ["GTIN boş olamaz."]
}
```

Yukarıdaki yapı `errors` listesinin bir elemanıdır. `products` elemanı örneği:

```json
{
  "gtin": "08690000000012",
  "modelName": "Slim Denim 501",
  "batchNumber": "B2027-01",
  "status": "created"
}
```

`rowNumber`, başlığı 1 sayan fiziksel satır numarasıdır. Çok satırlı bir alan
varsa kaydın başladığı satır raporlanır. `totalRows`, başlık hariç işlenen
kayıt sayısıdır. Sayaçlar için `importedCount + unchangedCount + rejectedCount = totalRows`.

### Tekrar gönderme ve transaction

- Yeni GTIN: `importedCount` artar; `status` değeri `created` olur.
- Aynı GTIN ve aynı bilgiler: `unchangedCount` artar; `status` değeri `unchanged` olur.
- Aynı GTIN ve farklı bilgiler: Satır reddedilir; önceki ürün değiştirilmez.
- Aynı model/parti yeniden kullanılır. Karşılaştırma büyük/küçük harfe duyarlıdır.
- Her satır ayrı transaction kullanır. O satırdaki model, parti ve ürün birlikte
  kaydedilir veya birlikte geri alınır. Diğer satırlar devam eder.
- Bir kaydın güvenilir biçimde ayrıştırılamadığı CSV yapısal hatasında,
  ayrıştırıcının kurtarabildiği sonraki kayıtlara devam edilir.

Örnek dosyanın tekrar yüklemesi **0 yeni, 3 değişmeyen, 2 hatalı** döner.

### Dosya düzeyindeki hatalar

Dosya yok/boşsa, UTF-8 geçersizse, başlık hatalıysa veya veri kaydı yoksa 400
döner. Dosya boyutu kontrolü 1 MiB üzerinde 413 döndürür. Bu hata yanıtları
`application/problem+json` biçimindedir; açıklama `detail` alanından okunabilir.

## Ürün listesi

`GET /api/products`, her üründe yalnızca `gtin`, `modelName`, `batchNumber` ve
`productionLocation` alanlarını döndürür. Model adı ve GTIN'e göre sıralıdır.
Rol parametresi verilse de bu özet liste karbon alanı içermez.

## HTML, JSON-LD ve görünürlük

Varsayılan görünüm tüketicidir. Tam olarak `?rol=yetkili` verildiğinde karbon
alanı HTML ve JSON-LD'ye birlikte eklenir. Tanınmayan veya yinelenen rol
değerlerinde tüketici görünümü kullanılır. Sıfır karbon, yetkili görünümünde gösterilir.

Bu parametre, ödevin istediği demo görünüm ayrımıdır. Kullanıcı hesabı, JWT
veya kullanıcı oturumu uygulanmaz. Yanıtlar `Cache-Control: no-store` kullanır.
Kayıtsız veya geçersiz GTIN için 404 döner.

JSON-LD içerik türü `application/ld+json`'dır. `@context`, `https://schema.org`;
`@type`, `Product`'tır. `gtin14` metindir. Ek özellikler `additionalProperty`
içinde `PropertyValue` olarak, sayısal değerleri sayı ve birimleri `unitText`
olarak sunulur. Üretim yeri bir şehir olduğu için `countryOfOrigin` alanına
yerleştirilmez. HTML ve JSON-LD aynı özellik listesini kullanır.

## QR ve adres ayarı

PNG endpoint'i `image/png` döndürür; dosyalar istek sırasında QRCoder ile üretilir.
Kalıcı dosya kopyaları tutulmaz. QR tüketici HTML adresini içerir; rol parametresi
QR'ın hedefini değiştirmez. HTML'den PNG indirilebilir.

Hedef adresinin kökü `Dpp:PublicBaseUrl` ayarıdır. Yol, sorgu, fragment veya
kullanıcı bilgisi içermeyen HTTP/HTTPS adresi beklenir. İstek `Host` başlığı,
bu yapılandırmanın yerine kullanılmaz. HTML'deki yerel gezinme yolları aynı
uygulamaya göreli bağlantılardır; QR'daki adres mutlaktır.

## Migration ve Release çıktısı

Şema değişikliğinden sonra:

```powershell
dotnet ef migrations add DegisiklikAdi --project .\src\MiniDpp.Api --startup-project .\src\MiniDpp.Api
```

Migration kaynak kodu ve snapshot commit edilir. Başlangıçta migration'lar
uygulanır; mevcut veriler sıfırlanmaz. Elle uygulamak için:

```powershell
dotnet ef database update --project .\src\MiniDpp.Api --startup-project .\src\MiniDpp.Api
```

İsteğe bağlı yerel yayımlanabilir çıktı:

```powershell
dotnet publish .\src\MiniDpp.Api\MiniDpp.Api.csproj -c Release -o .artifacts\publish
Set-Location .artifacts\publish
dotnet .\MiniDpp.Api.dll --urls http://localhost:5100
```

Çıktıya örnek CSV ve statik dosyalar da dahil edilir. Bu komutlar buluta yayınlama yapmaz.
