using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Tests.Unit;

/// <summary>Lectura del JSON del modelo y armado del prompt (FR-002 a FR-005).</summary>
public class ModelOutputParserTests
{
    [Fact]
    public void Lee_eventos_gastos_y_avisos()
    {
        const string json = """
            {"events":[{"title":"Dentista","date":"2026-10-20","time":"16:00"}],
             "expenses":[{"amount":25000,"category":"Salud","date":null}],
             "warnings":["AMBIGUOUS_DATE"]}
            """;

        var output = ModelOutputParser.Parse(json);

        Assert.Equal(new ModelEvent("Dentista", new DateOnly(2026, 10, 20), "16:00"), output.Events.Single());
        Assert.Equal(new ModelExpense(25000, "Salud", null), output.Expenses.Single());
        Assert.Equal(["AMBIGUOUS_DATE"], output.Warnings);
    }

    [Fact]
    public void Tolera_un_bloque_de_codigo_alrededor_del_json()
    {
        var output = ModelOutputParser.Parse("```json\n{\"events\":[],\"expenses\":[],\"warnings\":[]}\n```");

        Assert.Empty(output.Events);
    }

    [Fact]
    public void Una_hora_con_formato_invalido_se_trata_como_sin_hora()
    {
        var output = ModelOutputParser.Parse("""{"events":[{"title":"X","date":"2026-10-20","time":"4 pm"}],"expenses":[],"warnings":[]}""");

        Assert.Null(output.Events.Single().Time);
    }

    [Theory]
    [InlineData("no hay json")]
    [InlineData("""{"events":[{"title":"X","date":"mañana","time":null}],"expenses":[],"warnings":[]}""")]
    [InlineData("""{"events":[],"expenses":[{"amount":"mucho","category":null,"date":null}],"warnings":[]}""")]
    public void Una_salida_ilegible_cuenta_como_falla_de_la_IA(string raw) =>
        Assert.Throws<ExtractionUnavailableException>(() => ModelOutputParser.Parse(raw));

    [Fact]
    public void El_prompt_incluye_la_fecha_de_hoy_el_dia_y_las_categorias()
    {
        var prompt = ExtractionPrompt.BuildSystemPrompt(new DateOnly(2026, 10, 14));

        Assert.Contains("2026-10-14", prompt);
        Assert.Contains("miércoles", prompt);
        foreach (var category in new[] { "Comida", "Transporte", "Salidas", "Salud", "Otros" })
        {
            Assert.Contains(category, prompt);
        }
    }
}
