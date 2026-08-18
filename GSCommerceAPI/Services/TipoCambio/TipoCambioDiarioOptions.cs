namespace GSCommerceAPI.Services.TipoCambio;

public class TipoCambioDiarioOptions
{
    public const string SectionName = "TipoCambioDiario";

    public bool Enabled { get; set; } = true;

    public int RunHour { get; set; } = 6;

    public int RunMinute { get; set; } = 0;
}
