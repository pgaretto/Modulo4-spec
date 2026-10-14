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

    // --- Eventos (US2: FR-002, FR-003b, FR-005) ---

    private static Proposal MapWithEvents(ModelEvent[] events, params ModelExpense[] expenses) =>
        ProposalMapper.Map(new ModelOutput(events, expenses, []), Today, Category.System);

    [Fact]
    public void Un_evento_con_hora_se_propone_tal_cual()
    {
        var proposal = MapWithEvents([new ModelEvent("Dentista", new DateOnly(2026, 10, 20), "16:00")]);

        Assert.Equal(new EventInput("Dentista", new DateOnly(2026, 10, 20), "16:00"), proposal.Event);
        Assert.Null(proposal.Expense);
    }

    [Fact]
    public void Un_evento_sin_hora_queda_sin_hora_es_decir_todo_el_dia()
    {
        var proposal = MapWithEvents([new ModelEvent("Dentista", new DateOnly(2026, 10, 16), null)]);

        Assert.Null(proposal.Event!.Time);
    }

    [Fact]
    public void El_gasto_sin_fecha_propia_toma_la_fecha_del_evento_de_la_frase()
    {
        var proposal = MapWithEvents(
            [new ModelEvent("Dentista", new DateOnly(2026, 10, 20), "16:00")],
            new ModelExpense(25000, "Salud", null));

        Assert.Equal(new DateOnly(2026, 10, 20), proposal.Expense!.Date);
    }

    [Fact]
    public void El_gasto_con_fecha_propia_la_conserva_aunque_haya_evento()
    {
        var proposal = MapWithEvents(
            [new ModelEvent("Dentista", new DateOnly(2026, 10, 20), null)],
            new ModelExpense(25000, "Salud", new DateOnly(2026, 10, 14)));

        Assert.Equal(new DateOnly(2026, 10, 14), proposal.Expense!.Date);
    }

    [Fact]
    public void Con_varios_eventos_propone_el_primero_y_avisa()
    {
        var proposal = MapWithEvents([
            new ModelEvent("Dentista", new DateOnly(2026, 10, 20), "16:00"),
            new ModelEvent("Cine", new DateOnly(2026, 10, 21), "21:00"),
        ]);

        Assert.Equal("Dentista", proposal.Event!.Title);
        Assert.Contains(ProposalWarnings.MultipleEvents, proposal.Warnings);
    }

    [Fact]
    public void Una_frase_con_evento_y_gasto_devuelve_ambos_sin_avisos()
    {
        var proposal = MapWithEvents(
            [new ModelEvent("Almuerzo con mamá", new DateOnly(2026, 10, 15), null)],
            new ModelExpense(15000, "Comida", null));

        Assert.NotNull(proposal.Event);
        Assert.NotNull(proposal.Expense);
        Assert.Empty(proposal.Warnings);
    }
}
