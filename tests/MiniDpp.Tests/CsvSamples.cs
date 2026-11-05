using System.Text;

namespace MiniDpp.Tests;

internal static class CsvSamples
{
    public const string Header =
        "gtin,model_adi,batch_no,elyaf_pamuk_yuzde,elyaf_elastan_yuzde,uretim_yeri,karbon_kg_co2e";

    public const string FirstRow = "08690000000012,Slim Denim 501,B2027-01,98,2,Denizli,12.4";
    public const string SecondRow = "08690000000029,Regular Denim 502,B2027-02,99,1,Denizli,11.8";

    public static string Build(params string[] rows) =>
        Header + "\n" + string.Join("\n", rows) + "\n";

    public static string ReadAssignmentFile() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestData", "urunler_duzeltilmis_test.csv"), Encoding.UTF8);
}
