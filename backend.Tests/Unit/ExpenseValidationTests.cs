using SmartAgenda.Api.Domain;
using SmartAgenda.Api.Endpoints;

namespace SmartAgenda.Tests.Unit;

/// <summary>Validación del gasto: monto, fecha y categoría visible para el usuario (FR-009, FR-004b).</summary>
public class ExpenseValidationTests
{
    private static readonly Guid Visible = Category.System[0].Id;
    private static readonly Guid DeOtroUsuario = Guid.NewGuid();

    private static IReadOnlyList<string> Validate(decimal amount, Guid? category = null, DateOnly? date = null) =>
        ExpenseValidator.Validate(
            new ExpenseInput(amount, category ?? Visible, date ?? new DateOnly(2026, 10, 14)),
            id => id == Visible);

    [Fact]
    public void Un_gasto_correcto_no_tiene_errores() => Assert.Empty(Validate(2500));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000000000000)]
    public void El_monto_debe_ser_mayor_que_cero_y_acotado(decimal amount) =>
        Assert.NotEmpty(Validate(amount));

    [Fact]
    public void El_monto_admite_como_maximo_dos_decimales()
    {
        Assert.Empty(Validate(10.50m));
        Assert.NotEmpty(Validate(10.505m));
    }

    [Fact]
    public void La_fecha_no_puede_faltar()
    {
        Assert.NotEmpty(Validate(100, date: default(DateOnly)));
    }

    [Fact]
    public void La_categoria_debe_ser_del_sistema_o_propia_del_usuario() =>
        Assert.NotEmpty(Validate(100, category: DeOtroUsuario));
}
