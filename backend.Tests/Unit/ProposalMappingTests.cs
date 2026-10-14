using SmartAgenda.Api.Domain;
using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Tests.Unit;

/// <summary>Convierte la salida cruda del modelo en una propuesta (FR-003b, FR-005, FR-014).</summary>
public class ProposalMappingTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);
    private static readonly Guid Comida = Category.System[0].Id;
    private static readonly Guid Transporte = Category.System[1].Id;
    private static readonly Guid Otros = Category.OtrosId;

    private static Proposal Map(params ModelExpense[] expenses) =>
        ProposalMapper.Map(new ModelOutput([], expenses, []), Today, Category.System);

    [Fact]
    public void Un_gasto_sin_fecha_toma_la_de_hoy()
    {
        var proposal = Map(new ModelExpense(2500, "Comida", null));

        Assert.Null(proposal.Event);
        Assert.Equal(new ExpenseInput(2500, Comida, Today), proposal.Expense);
        Assert.Empty(proposal.Warnings);
    }

    [Fact]
    public void Resuelve_la_categoria_por_nombre_sin_distinguir_mayusculas()
    {
        var proposal = Map(new ModelExpense(5000, "transporte", null));

        Assert.Equal(Transporte, proposal.Expense!.CategoryId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mascotas")]
    public void Sin_categoria_valida_cae_en_Otros_con_aviso(string? category)
    {
        var proposal = Map(new ModelExpense(1000, category, null));

        Assert.Equal(Otros, proposal.Expense!.CategoryId);
        Assert.Contains(ProposalWarnings.DefaultCategory, proposal.Warnings);
    }

    [Fact]
    public void Con_varios_gastos_propone_el_primero_y_avisa()
    {
        var proposal = Map(new ModelExpense(2500, "Comida", null), new ModelExpense(4000, "Transporte", null));

        Assert.Equal(2500, proposal.Expense!.Amount);
        Assert.Equal(Comida, proposal.Expense.CategoryId);
        Assert.Contains(ProposalWarnings.MultipleExpenses, proposal.Warnings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Un_monto_no_positivo_se_descarta(decimal amount)
    {
        var proposal = Map(new ModelExpense(amount, "Comida", null));

        Assert.Null(proposal.Expense);
    }

    [Fact]
    public void La_fecha_que_trae_el_modelo_se_respeta()
    {
        var proposal = Map(new ModelExpense(900, "Comida", new DateOnly(2026, 10, 20)));

        Assert.Equal(new DateOnly(2026, 10, 20), proposal.Expense!.Date);
    }

    [Fact]
    public void Los_avisos_del_modelo_se_filtran_a_los_conocidos_y_sin_repetir()
    {
        var output = new ModelOutput([], [new ModelExpense(100, "Comida", null)],
            [ProposalWarnings.AmbiguousDate, ProposalWarnings.AmbiguousDate, "INVENTADO"]);

        var proposal = ProposalMapper.Map(output, Today, Category.System);

        Assert.Equal([ProposalWarnings.AmbiguousDate], proposal.Warnings);
    }
}
