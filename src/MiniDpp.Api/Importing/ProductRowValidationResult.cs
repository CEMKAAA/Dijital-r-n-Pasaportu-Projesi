namespace MiniDpp.Api.Importing;

public sealed record ProductRowValidationResult(
    ValidatedProduct? Product,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Product is not null && Errors.Count == 0;
}
