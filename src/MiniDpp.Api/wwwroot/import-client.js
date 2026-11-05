export const MAX_UPLOAD_BYTES = 1024 * 1024;

export function validateFile(file) {
  if (!file) return "Önce bir CSV dosyası seç.";
  if (file.size === 0) return "Seçilen dosya boş.";
  if (file.size > MAX_UPLOAD_BYTES) return "CSV dosyası en fazla 1 MiB olabilir.";
  return null;
}

async function readJson(response) {
  let data;
  try {
    data = await response.json();
  } catch {
    throw new Error(`Sunucu yanıtı okunamadı (HTTP ${response.status}).`);
  }

  if (!response.ok) {
    throw new Error(data?.detail || data?.title || `İşlem tamamlanamadı (HTTP ${response.status}).`);
  }
  return data;
}

async function sendRequest(url, options, fetchRequest) {
  let response;
  try {
    response = await fetchRequest(url, options);
  } catch {
    throw new Error("Sunucuya ulaşılamadı. Uygulamanın çalıştığını kontrol edip tekrar dene.");
  }
  return readJson(response);
}

export async function uploadCsv(file, fetchRequest = globalThis.fetch) {
  const error = validateFile(file);
  if (error) throw new Error(error);

  const form = new FormData();
  form.append("file", file, file.name || "urunler.csv");

  // Content-Type elle verilmez; tarayıcı multipart boundary değerini ekler.
  const report = await sendRequest("/api/products/import", { method: "POST", body: form }, fetchRequest);
  const counts = ["totalRows", "importedCount", "unchangedCount", "rejectedCount"];
  if (!report || !counts.every(key => Number.isInteger(report[key]) && report[key] >= 0)
      || !Array.isArray(report.errors) || !Array.isArray(report.products)
      || report.importedCount + report.unchangedCount + report.rejectedCount !== report.totalRows) {
    throw new Error("Sunucudan beklenen yükleme raporu alınamadı.");
  }
  return report;
}

export async function getProducts(fetchRequest = globalThis.fetch) {
  const products = await sendRequest("/api/products", { cache: "no-store" }, fetchRequest);
  const fields = ["gtin", "modelName", "batchNumber", "productionLocation"];
  if (!Array.isArray(products) || products.some(product =>
      !product || !fields.every(field => typeof product[field] === "string"))) {
    throw new Error("Sunucudan beklenen ürün listesi alınamadı.");
  }
  return products;
}
