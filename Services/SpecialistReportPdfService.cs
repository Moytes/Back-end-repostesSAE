using Back_end_RepostesSAE.Models.Dto;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Back_end_RepostesSAE.Services;

public sealed class SpecialistReportPdfService
{
    private const string Navy = "#123047";
    private const string Teal = "#0F766E";
    private const string Slate = "#475569";
    private const string LightSlate = "#F8FAFC";
    private const string Border = "#E2E8F0";

    public byte[] BuildTeaAlertsPdf(IReadOnlyList<TeaAlertDto> alerts, int? schoolYearId, int? schoolId)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.MarginHorizontal(38);
                page.MarginVertical(30);
                page.Size(PageSizes.Letter);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Navy));
                page.Header().Element(c => ReportHeader(
                    c,
                    "Monitor de alertas TEA",
                    "Reporte de seguimiento y priorización de casos"));

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Element(c => FilterSummary(c, schoolYearId, schoolId));

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => MetricCard(c, alerts.Count, "Alertas activas", Navy));
                        row.Spacing(8);
                        row.RelativeItem().Element(c => MetricCard(
                            c, alerts.Count(a => a.AlertLevel == 0), "Nivel leve", "#D97706"));
                        row.Spacing(8);
                        row.RelativeItem().Element(c => MetricCard(
                            c, alerts.Count(a => a.AlertLevel == 1), "Nivel moderado", "#EA580C"));
                        row.Spacing(8);
                        row.RelativeItem().Element(c => MetricCard(
                            c, alerts.Count(a => a.AlertLevel >= 2), "Significativo", "#DC2626"));
                    });

                    col.Item().Element(c => Section(c, "Detalle de alertas", section =>
                    {
                        if (alerts.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin alertas TEA en el alcance seleccionado."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.1f);
                                columns.RelativeColumn(2.1f);
                                columns.RelativeColumn(1.15f);
                                columns.RelativeColumn(.8f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Alumno");
                                header.Cell().Element(TableHeaderCell).Text("Escuela / grupo");
                                header.Cell().Element(TableHeaderCell).Text("Nivel");
                                header.Cell().Element(TableHeaderCell).Text("Puntaje");
                                header.Cell().Element(TableHeaderCell).Text("Aplicación");
                                header.Cell().Element(TableHeaderCell).Text("Seguimiento");
                            });

                            for (var i = 0; i < alerts.Count; i++)
                            {
                                var alert = alerts[i];
                                var background = i % 2 == 0 ? "#FFFFFF" : LightSlate;
                                var school = string.Join(" · ", new[]
                                {
                                    alert.SchoolName,
                                    alert.GroupName
                                }.Where(x => !string.IsNullOrWhiteSpace(x)));

                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text(alert.StudentName).SemiBold();
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text(string.IsNullOrWhiteSpace(school) ? "Sin registro" : school);
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text(AlertLevelLabel(alert.AlertLevel))
                                    .Bold().FontColor(AlertColor(alert.AlertLevel));
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .AlignCenter().Text(alert.PuntajeTotal?.ToString() ?? "—");
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text(alert.ScreeningDate.ToString("dd/MM/yyyy"));
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text(StatusLabel(alert.SeguimientoEstado));
                            }
                        });
                    }));

                    col.Item().Element(ConfidentialityNotice);
                });

                page.Footer().Element(ReportFooter);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] BuildMonthlyPackPdf(
        int year,
        int month,
        IReadOnlyList<TeaAlertDto> teaAlerts,
        IReadOnlyList<CieSummaryDto> cieSummary,
        IReadOnlyList<CanalizacionMonthCountDto> canalizacionCounts)
    {
        var monthName = new DateOnly(year, month, 1)
            .ToString("MMMM yyyy", new CultureInfo("es-MX"));
        var canalizacionesTotal = canalizacionCounts.Sum(x => x.Total);
        var cieStudents = cieSummary.Select(x => x.StudentId).Distinct().Count();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.MarginHorizontal(38);
                page.MarginVertical(30);
                page.Size(PageSizes.Letter);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Navy));
                page.Header().Element(c => ReportHeader(
                    c,
                    $"Informe mensual · {Capitalize(monthName)}",
                    "Concentrado de atención psicopedagógica y seguimiento"));

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(16);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => MetricCard(c, teaAlerts.Count, "Alertas TEA", "#DC2626"));
                        row.Spacing(8);
                        row.RelativeItem().Element(c => MetricCard(c, cieStudents, "Alumnos con CIE", "#7C3AED"));
                        row.Spacing(8);
                        row.RelativeItem().Element(c => MetricCard(c, cieSummary.Count, "Dimensiones CIE", "#2563EB"));
                        row.Spacing(8);
                        row.RelativeItem().Element(c => MetricCard(c, canalizacionesTotal, "Canalizaciones", Teal));
                    });

                    col.Item().Element(c => Section(c, "Alertas TEA del periodo", section =>
                    {
                        if (teaAlerts.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin alertas TEA en el periodo."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.3f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.1f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Alumno");
                                header.Cell().Element(TableHeaderCell).Text("Escuela");
                                header.Cell().Element(TableHeaderCell).Text("Nivel");
                                header.Cell().Element(TableHeaderCell).Text("Fecha");
                            });
                            for (var i = 0; i < teaAlerts.Count; i++)
                            {
                                var alert = teaAlerts[i];
                                var background = i % 2 == 0 ? "#FFFFFF" : LightSlate;
                                table.Cell().Element(c => TableBodyCell(c, background)).Text(alert.StudentName);
                                table.Cell().Element(c => TableBodyCell(c, background)).Text(alert.SchoolName ?? "Sin registro");
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text(AlertLevelLabel(alert.AlertLevel)).Bold().FontColor(AlertColor(alert.AlertLevel));
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text(alert.ScreeningDate.ToString("dd/MM/yyyy"));
                            }
                        });
                    }));

                    col.Item().Element(c => Section(c, "Avance de evaluaciones CIE", section =>
                    {
                        if (cieSummary.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin evaluaciones CIE en el alcance seleccionado."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(1.8f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.1f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Alumno");
                                header.Cell().Element(TableHeaderCell).Text("Dimensión");
                                header.Cell().Element(TableHeaderCell).Text("Avance");
                                header.Cell().Element(TableHeaderCell).Text("Indicadores");
                            });
                            for (var i = 0; i < cieSummary.Count; i++)
                            {
                                var item = cieSummary[i];
                                var background = i % 2 == 0 ? "#FFFFFF" : LightSlate;
                                table.Cell().Element(c => TableBodyCell(c, background)).Text(item.StudentName);
                                table.Cell().Element(c => TableBodyCell(c, background)).Text(item.DimensionName);
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text($"{item.Percentage:0.#}%").Bold().FontColor(Teal);
                                table.Cell().Element(c => TableBodyCell(c, background))
                                    .Text($"{item.CompletedIndicators}/{item.TotalIndicators}");
                            }
                        });
                    }));

                    col.Item().Element(c => Section(c, "Canalizaciones por estado", section =>
                    {
                        if (canalizacionCounts.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin canalizaciones registradas en el periodo."));
                            return;
                        }

                        section.Item().Row(row =>
                        {
                            foreach (var item in canalizacionCounts)
                            {
                                row.RelativeItem().Background(LightSlate).Border(1).BorderColor(Border)
                                    .Padding(10).Column(card =>
                                    {
                                        card.Item().Text(item.Total.ToString()).FontSize(16).Bold().FontColor(Teal);
                                        card.Item().Text(StatusLabel(item.Estado)).FontSize(7.5f).FontColor(Slate);
                                    });
                                row.Spacing(7);
                            }
                        });
                    }));

                    col.Item().Element(ConfidentialityNotice);
                });

                page.Footer().Element(ReportFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static string AlertLevelLabel(int level) => level switch
    {
        2 => "Significativo",
        1 => "Moderado",
        _ => "Leve"
    };

    private static string AlertColor(int level) => level switch
    {
        2 => "#B91C1C",
        1 => "#C2410C",
        _ => "#B45309"
    };

    private static void ReportHeader(IContainer container, string title, string subtitle)
    {
        container.Background(Navy).Padding(16).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("SIEMBRAEDU").FontSize(8).Bold().FontColor("#5EEAD4").LetterSpacing(0.12f);
                column.Item().PaddingTop(3).Text(title).FontSize(17).Bold().FontColor(Colors.White);
                column.Item().PaddingTop(2).Text(subtitle).FontSize(8).FontColor("#CBD5E1");
            });
            row.ConstantItem(120).AlignRight().AlignMiddle().Column(column =>
            {
                column.Item().AlignRight().Text("CENTRO DE REPORTES")
                    .FontSize(6.5f).Bold().FontColor("#99F6E4");
                column.Item().PaddingTop(4).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                    .FontSize(7.5f).FontColor(Colors.White);
            });
        });
    }

    private static void FilterSummary(IContainer container, int? schoolYearId, int? schoolId)
    {
        var filters = new List<string>();
        if (schoolYearId.HasValue) filters.Add($"Ciclo escolar: {schoolYearId}");
        if (schoolId.HasValue) filters.Add($"Escuela: {schoolId}");

        container.Background("#F0FDFA").Border(1).BorderColor("#99F6E4").Padding(9).Text(text =>
        {
            text.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Slate));
            text.Span("ALCANCE DEL REPORTE  ").Bold().FontColor(Teal);
            text.Span(filters.Count == 0 ? "Todas las escuelas y ciclos disponibles" : string.Join("  ·  ", filters));
        });
    }

    private static void MetricCard(IContainer container, int value, string label, string accent)
    {
        container.Border(1).BorderColor(Border).Background(Colors.White).Padding(10).Column(column =>
        {
            column.Item().Text(value.ToString()).FontSize(18).Bold().FontColor(accent);
            column.Item().Text(label).FontSize(7.3f).FontColor(Slate);
        });
    }

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Border(1).BorderColor(Border).Background(Colors.White).Column(column =>
        {
            column.Item().Background("#ECFDF5").BorderBottom(1).BorderColor("#A7F3D0")
                .PaddingVertical(8).PaddingHorizontal(11)
                .Text(title).FontSize(9.5f).Bold().FontColor(Teal);
            column.Item().Padding(10).Column(content);
        });
    }

    private static IContainer TableHeaderCell(IContainer container) =>
        container.Background(Navy).PaddingVertical(7).PaddingHorizontal(7)
            .DefaultTextStyle(x => x.FontSize(7).Bold().FontColor(Colors.White));

    private static IContainer TableBodyCell(IContainer container, string background) =>
        container.Background(background).BorderBottom(1).BorderColor(Border)
            .PaddingVertical(7).PaddingHorizontal(7)
            .DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Navy));

    private static void EmptyState(IContainer container, string message) =>
        container.Background(LightSlate).Padding(12).AlignCenter()
            .Text(message).FontSize(8).Italic().FontColor(Slate);

    private static void ConfidentialityNotice(IContainer container) =>
        container.Background("#FFF7ED").Border(1).BorderColor("#FED7AA").Padding(9)
            .Text("Documento confidencial para uso institucional. La información presentada orienta el seguimiento educativo y no sustituye una valoración diagnóstica formal.")
            .FontSize(7).FontColor("#9A3412").LineHeight(1.3f);

    private static void ReportFooter(IContainer container)
    {
        container.PaddingTop(8).BorderTop(1).BorderColor(Border).Row(row =>
        {
            row.RelativeItem().Text("SiembraEdu · Sistema Integral de Atención Educativa")
                .FontSize(7).FontColor(Slate);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(s => s.FontSize(7).FontColor(Slate));
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
    }

    private static string StatusLabel(string? status) => status?.ToUpperInvariant() switch
    {
        "ACTIVA" => "Activa",
        "EN_MONITOREO" => "En monitoreo",
        "NOTIFICADA" => "Notificada",
        "RESUELTA" => "Resuelta",
        "PENDIENTE" => "Pendiente",
        "RECIBIDA" => "Recibida",
        "EN_PROCESO" => "En proceso",
        "CERRADA" => "Cerrada",
        _ => status?.Replace('_', ' ') ?? "Sin estado"
    };

    private static string Capitalize(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? value
            : char.ToUpper(value[0], new CultureInfo("es-MX")) + value[1..];
}
