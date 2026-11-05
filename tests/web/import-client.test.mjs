import test from "node:test";
import assert from "node:assert/strict";
import { getProducts, MAX_UPLOAD_BYTES, uploadCsv, validateFile } from "../../src/MiniDpp.Api/wwwroot/import-client.js";

const file = () => new File(["gtin,model_adi\n"], "urunler.csv", { type: "text/csv" });
const report = {
  totalRows: 5, importedCount: 3, unchangedCount: 0, rejectedCount: 2,
  errors: [{ rowNumber: 4, errors: ["GTIN boş olamaz."] }, { rowNumber: 5, errors: ["Toplam %100 olmalıdır."] }],
  products: [
    { gtin: "08690000000012", modelName: "Slim Denim 501", batchNumber: "B2027-01", status: "created" },
    { gtin: "08690000000029", modelName: "Regular Denim 502", batchNumber: "B2027-02", status: "created" },
    { gtin: "08690000000050", modelName: "Mom Jean 505", batchNumber: "B2027-05", status: "created" }
  ]
};
const json = (data, status = 200) => new Response(JSON.stringify(data), {
  status, headers: { "Content-Type": "application/json" }
});

test("missing, empty and oversized files are rejected; the size boundary is accepted", () => {
  assert.match(validateFile(null), /dosyası seç/);
  assert.match(validateFile({ size: 0 }), /boş/);
  assert.match(validateFile({ size: MAX_UPLOAD_BYTES + 1 }), /1 MiB/);
  assert.equal(validateFile({ size: MAX_UPLOAD_BYTES }), null);
});

test("invalid files never reach the server", async () => {
  let requested = false;
  await assert.rejects(uploadCsv(null, async () => { requested = true; }), /dosyası seç/);
  assert.equal(requested, false);
});

test("upload uses the file form field and preserves a partial-success report", async () => {
  const result = await uploadCsv(file(), async (url, options) => {
    assert.equal(url, "/api/products/import");
    assert.equal(options.method, "POST");
    assert.ok(options.body instanceof FormData);
    assert.equal(options.body.get("file").name, "urunler.csv");
    assert.equal(options.headers, undefined);
    return json(report);
  });
  assert.deepEqual(result, report);
});

test("problem-details messages reach the caller", async () => {
  await assert.rejects(uploadCsv(file(), async () => json({ detail: "Başlık geçersiz." }, 400)), /Başlık geçersiz/);
  await assert.rejects(uploadCsv(file(), async () => json({ title: "Dosya çok büyük" }, 413)), /Dosya çok büyük/);
  await assert.rejects(uploadCsv(file(), async () => json(null, 400)), /HTTP 400/);
});

test("network failures produce an understandable message", async () => {
  const offline = async () => { throw new TypeError("Failed to fetch"); };
  await assert.rejects(uploadCsv(file(), offline), /Sunucuya ulaşılamadı/);
  await assert.rejects(getProducts(offline), /Sunucuya ulaşılamadı/);
});

test("non-JSON failures do not become successful imports", async () => {
  await assert.rejects(uploadCsv(file(), async () => new Response("Server error", { status: 500 })), /HTTP 500/);
});

test("incomplete or inconsistent reports are rejected", async () => {
  await assert.rejects(uploadCsv(file(), async () => json({})), /beklenen yükleme raporu/);
  await assert.rejects(uploadCsv(file(), async () => json({ ...report, totalRows: 100 })), /beklenen yükleme raporu/);
  await assert.rejects(uploadCsv(file(), async () => json({ ...report, importedCount: -1 })), /beklenen yükleme raporu/);
});

test("product-list refresh bypasses cached data and preserves GTIN text", async () => {
  const products = [{ gtin: "08690000000012", modelName: "Slim Denim 501", batchNumber: "B2027-01", productionLocation: "Denizli" }];
  assert.deepEqual(await getProducts(async (url, options) => {
    assert.equal(url, "/api/products");
    assert.equal(options.cache, "no-store");
    return json(products);
  }), products);
});

test("unexpected product-list responses are reported", async () => {
  await assert.rejects(getProducts(async () => json({ error: "unexpected" })), /beklenen ürün listesi/);
  await assert.rejects(getProducts(async () => json([null])), /beklenen ürün listesi/);
  await assert.rejects(getProducts(async () => json([{ gtin: 8690000000012 }])), /beklenen ürün listesi/);
});
