using QRCoder;

namespace MiniDpp.Api.Passports;

public static class QrCodeService
{
    public static byte[] CreatePng(string passportUrl)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(passportUrl, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }
}
