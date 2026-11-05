import { getProducts, uploadCsv, validateFile } from "./import-client.js";

const form = document.getElementById("upload-form");
const fileInput = document.getElementById("csv-file");
const uploadButton = document.getElementById("upload-button");
const refreshButton = document.getElementById("refresh-button");
const uploadError = document.getElementById("upload-error");
const catalogError = document.getElementById("catalog-error");
const catalog = document.getElementById("catalog-section");
let uploading = false;
let refreshing = false;

function showMessage(element, message) {
  element.textContent = message;
  element.hidden = !message;
}

function cell(value) {
  const element = document.createElement("td");
  // ÖNEMLİ: CSV'den gelen değerler innerHTML yerine metin olarak yazılır.
  element.textContent = String(value);
  return element;
}

function renderProducts(products) {
  const body = document.getElementById("product-rows");
  const fragment = document.createDocumentFragment();
  for (const product of products) {
    const row = document.createElement("tr");
    const name = cell(product.modelName);
    name.className = "model-cell";
    const gtin = document.createElement("td");
    const code = document.createElement("code");
    code.textContent = product.gtin;
    gtin.append(code);
    const links = document.createElement("td");
    links.className = "product-links";
    const productPath = "/dpp/" + encodeURIComponent(product.gtin);
    for (const [label, suffix] of [["HTML", ""], ["JSON-LD", ".jsonld"], ["QR", "/qr.png"]]) {
      const link = document.createElement("a");
      link.textContent = label;
      link.href = productPath + suffix;
      if (label === "QR") link.download = product.gtin + "-qr.png";
      links.append(link);
    }
    row.append(name, gtin, cell(product.batchNumber), cell(product.productionLocation), links);
    fragment.append(row);
  }
  if (products.length === 0) {
    const row = document.createElement("tr");
    const empty = cell("Henüz ürün yok. Bir CSV dosyası yükleyerek başlayabilirsin.");
    empty.colSpan = 5;
    empty.className = "empty-state";
    row.append(empty);
    fragment.append(row);
  }
  body.replaceChildren(fragment);
  document.getElementById("product-count").textContent = `(${products.length})`;
}

function renderReport(report) {
  const values = {
    "total-count": report.totalRows,
    "imported-count": report.importedCount,
    "unchanged-count": report.unchangedCount,
    "rejected-count": report.rejectedCount
  };
  for (const [id, value] of Object.entries(values)) {
    document.getElementById(id).textContent = String(value);
  }

  const errorRows = document.createDocumentFragment();
  for (const error of report.errors) {
    const row = document.createElement("tr");
    const reasons = document.createElement("td");
    const list = document.createElement("ul");
    for (const message of error.errors) {
      const item = document.createElement("li");
      item.textContent = message;
      list.append(item);
    }
    reasons.append(list);
    row.append(cell(error.rowNumber), reasons);
    errorRows.append(row);
  }
  document.getElementById("row-errors").replaceChildren(errorRows);
  document.getElementById("row-errors-section").hidden = report.errors.length === 0;
  const result = document.getElementById("import-result");
  result.hidden = false;
  document.getElementById("result-status").textContent = report.rejectedCount
    ? "Dosya işlendi. Hatalı satırları aşağıdaki nedenlere göre düzeltebilirsin."
    : "Dosya işlendi. Hatalı satır bulunmadı.";
  result.focus();
}

async function refreshProducts() {
  if (refreshing) return;
  refreshing = true;
  refreshButton.disabled = true;
  uploadButton.disabled = true;
  catalog.setAttribute("aria-busy", "true");
  showMessage(catalogError, "");
  try {
    renderProducts(await getProducts());
  } catch (error) {
    // Yükleme raporu korunur; liste hatası başarılı yüklemeyi başarısız göstermez.
    showMessage(catalogError, "Ürün listesi yenilenemedi. " + error.message);
  } finally {
    refreshing = false;
    refreshButton.disabled = uploading;
    uploadButton.disabled = uploading;
    catalog.setAttribute("aria-busy", "false");
  }
}

form.addEventListener("submit", async event => {
  event.preventDefault();
  if (uploading || refreshing) return;
  const file = fileInput.files[0];
  const error = validateFile(file);
  showMessage(uploadError, error || "");
  fileInput.setAttribute("aria-invalid", error ? "true" : "false");
  if (error) return;

  uploading = true;
  uploadButton.disabled = true;
  refreshButton.disabled = true;
  fileInput.disabled = true;
  uploadButton.textContent = "Yükleniyor...";
  form.setAttribute("aria-busy", "true");
  document.getElementById("import-result").hidden = true;
  try {
    renderReport(await uploadCsv(file));
    await refreshProducts();
  } catch (failure) {
    showMessage(uploadError, failure.message);
  } finally {
    uploading = false;
    uploadButton.disabled = false;
    refreshButton.disabled = false;
    fileInput.disabled = false;
    uploadButton.textContent = "CSV'yi yükle";
    form.setAttribute("aria-busy", "false");
  }
});

fileInput.addEventListener("change", () => {
  showMessage(uploadError, "");
  fileInput.setAttribute("aria-invalid", "false");
});
refreshButton.addEventListener("click", refreshProducts);
