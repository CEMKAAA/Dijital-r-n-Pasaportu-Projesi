namespace MiniDpp.Api.Importing;

public static class GtinValidator
{
    public static bool HasValidFormat(string? gtin) =>
        gtin is { Length: 14 } && gtin.All(char.IsAsciiDigit);

    public static bool IsValid(string? gtin)
    {
        if (!HasValidFormat(gtin))
        {
            return false;
        }

        // ÖNEMLİ: GTIN-14'te son karakter kontrol basamağıdır.
        // İlk 13 rakam soldan başlayarak 3, 1, 3, 1... ile çarpılır.
        // GTIN'in tamamı sayıya çevrilmez; baştaki sıfırlar korunur.
        var weightedSum = 0;
        for (var index = 0; index < 13; index++)
        {
            var digit = gtin![index] - '0';
            weightedSum += digit * (index % 2 == 0 ? 3 : 1);
        }

        var expectedCheckDigit = (10 - weightedSum % 10) % 10;
        return gtin![13] - '0' == expectedCheckDigit;
    }
}
