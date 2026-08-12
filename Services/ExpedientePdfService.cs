using Back_end_RepostesSAE.Models.Dto;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Back_end_RepostesSAE.Services;

/// <summary>
/// PDF del expediente completo de UN alumno, para el especialista (PSI/COM/APR) que ya
/// puede ver esta misma información en pantalla vía GET /api/clinical/alumnos/{id}/expediente
/// (ClinicalExpedienteController). A diferencia del reporte del Tutor (backend-core,
/// StudentReportPdfService), aquí SÍ se incluyen notas clínicas — es la misma información
/// que ya ve el especialista, solo que en formato imprimible.
/// </summary>
public sealed class ExpedientePdfService
{
    private const string Navy = "#123047";
    private const string Teal = "#0F766E";
    private const string Slate = "#475569";
    private const string LightSlate = "#F8FAFC";
    private const string LightTeal = "#ECFDF5";
    private const string Border = "#E2E8F0";

    public byte[] Build(ExpedienteDto expediente)
    {
        var alumno = expediente.Alumno;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.MarginHorizontal(38);
                page.MarginVertical(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Navy));
                page.Header().Element(c => ReportHeader(c, alumno.NombreCompleto));

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Element(c => DatosGenerales(c, alumno));

                    col.Item().Element(c => Section(c, "Plan de acción · Actividades asignadas", section =>
                    {
                        if (expediente.Actividades.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin actividades registradas."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.4f);
                                columns.RelativeColumn(1.3f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2.4f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Actividad");
                                header.Cell().Element(TableHeaderCell).Text("Estado");
                                header.Cell().Element(TableHeaderCell).Text("Límite");
                                header.Cell().Element(TableHeaderCell).Text("Retroalimentación");
                            });
                            for (var i = 0; i < expediente.Actividades.Count; i++)
                            {
                                var a = expediente.Actividades[i];
                                var bg = Bg(i);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(a.MaterialTitulo);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(StatusLabel(a.Estado));
                                table.Cell().Element(c => TableBodyCell(c, bg))
                                    .Text(a.FechaLimite?.ToString("dd/MM/yyyy") ?? "Sin fecha");
                                table.Cell().Element(c => TableBodyCell(c, bg))
                                    .Text(string.IsNullOrWhiteSpace(a.Retroalimentacion) ? "—" : a.Retroalimentacion);
                            }
                        });
                    }));

                    col.Item().Element(c => Section(c, "Canalizaciones", section =>
                    {
                        if (expediente.Canalizaciones.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin canalizaciones registradas."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.3f);
                                columns.RelativeColumn(3.2f);
                                columns.RelativeColumn(1.3f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Fecha");
                                header.Cell().Element(TableHeaderCell).Text("Área");
                                header.Cell().Element(TableHeaderCell).Text("Motivo");
                                header.Cell().Element(TableHeaderCell).Text("Estado");
                            });
                            for (var i = 0; i < expediente.Canalizaciones.Count; i++)
                            {
                                var c2 = expediente.Canalizaciones[i];
                                var bg = Bg(i);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(c2.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(c2.AreaNombre ?? "—");
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(c2.Motivo);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(StatusLabel(c2.Estado));
                            }
                        });
                    }));

                    col.Item().Element(c => Section(c, "Evaluaciones psicopedagógicas", section =>
                    {
                        if (expediente.Evaluaciones.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin evaluaciones registradas."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2f);
                                columns.RelativeColumn(1.3f);
                                columns.RelativeColumn(1.3f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Ciclo");
                                header.Cell().Element(TableHeaderCell).Text("Estado");
                                header.Cell().Element(TableHeaderCell).Text("Fecha");
                            });
                            for (var i = 0; i < expediente.Evaluaciones.Count; i++)
                            {
                                var e = expediente.Evaluaciones[i];
                                var bg = Bg(i);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(e.SchoolYearName ?? e.SchoolYearId.ToString());
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(StatusLabel(e.Status));
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(e.CreatedAt.ToString("dd/MM/yyyy"));
                            }
                        });
                    }));

                    col.Item().Element(c => Section(c, "Historial TEA", section =>
                    {
                        if (expediente.TeaHistorial.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin tamizajes TEA registrados."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.3f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Fecha");
                                header.Cell().Element(TableHeaderCell).Text("Puntaje");
                                header.Cell().Element(TableHeaderCell).Text("Alerta");
                                header.Cell().Element(TableHeaderCell).Text("Canalización");
                            });
                            for (var i = 0; i < expediente.TeaHistorial.Count; i++)
                            {
                                var t = expediente.TeaHistorial[i];
                                var bg = Bg(i);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(t.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Element(c => TableBodyCell(c, bg)).AlignCenter().Text(t.PuntajeTotal?.ToString() ?? "—");
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(StatusLabel(t.NivelAlerta));
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(t.RequiereCanalizacion ? "Sí" : "No");
                            }
                        });
                    }));

                    col.Item().Element(c => Section(c, "Citas", section =>
                    {
                        if (expediente.Citas.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin citas registradas."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(1.2f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Fecha");
                                header.Cell().Element(TableHeaderCell).Text("Hora");
                                header.Cell().Element(TableHeaderCell).Text("Tipo");
                                header.Cell().Element(TableHeaderCell).Text("Estado");
                            });
                            for (var i = 0; i < expediente.Citas.Count; i++)
                            {
                                var c3 = expediente.Citas[i];
                                var bg = Bg(i);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(c3.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(c3.Hora.ToString("hh\\:mm"));
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(c3.TipoCita);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(StatusLabel(c3.Estado));
                            }
                        });
                    }));

                    col.Item().Element(c => Section(c, "Sesiones", section =>
                    {
                        if (expediente.Sesiones.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin sesiones registradas."));
                            return;
                        }

                        foreach (var s in expediente.Sesiones)
                        {
                            section.Item().PaddingBottom(8).BorderBottom(1).BorderColor(Border).Column(item =>
                            {
                                item.Item().Text($"{s.Fecha:dd/MM/yyyy} · {s.Tipo ?? "Sesión"}")
                                    .FontSize(8).SemiBold().FontColor(Teal);
                                if (!string.IsNullOrWhiteSpace(s.Motivo))
                                    item.Item().PaddingTop(2).Text($"Motivo: {s.Motivo}").FontSize(7.5f).FontColor(Slate);
                                item.Item().PaddingTop(2).Text(s.Nota).FontSize(8).FontColor(Navy).LineHeight(1.3f);
                                if (!string.IsNullOrWhiteSpace(s.Acuerdos))
                                    item.Item().PaddingTop(2).Text($"Acuerdos: {s.Acuerdos}").FontSize(7.5f).Italic().FontColor(Slate);
                            });
                        }
                    }));

                    col.Item().Element(c => Section(c, "Resumen CIE", section =>
                    {
                        if (expediente.CieResumen.Count == 0)
                        {
                            section.Item().Element(c => EmptyState(c, "Sin evaluación CIE registrada."));
                            return;
                        }

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.4f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderCell).Text("Dimensión");
                                header.Cell().Element(TableHeaderCell).Text("Avance");
                                header.Cell().Element(TableHeaderCell).Text("Indicadores");
                            });
                            for (var i = 0; i < expediente.CieResumen.Count; i++)
                            {
                                var r = expediente.CieResumen[i];
                                var bg = Bg(i);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text(r.DimensionName);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text($"{r.Percentage:0.#}%").Bold().FontColor(Teal);
                                table.Cell().Element(c => TableBodyCell(c, bg)).Text($"{r.CompletedIndicators}/{r.TotalIndicators}");
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

    private static string Bg(int i) => i % 2 == 0 ? "#FFFFFF" : LightSlate;

    private static void ReportHeader(IContainer container, string studentName)
    {
        container.Background(Navy).Padding(16).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("SIEMBRAEDU").FontSize(8).Bold().FontColor("#5EEAD4").LetterSpacing(0.12f);
                column.Item().PaddingTop(3).Text("Expediente del alumno").FontSize(17).Bold().FontColor(Colors.White);
                column.Item().PaddingTop(2).Text(studentName).FontSize(9.5f).FontColor("#CBD5E1");
            });
            row.ConstantItem(120).AlignRight().AlignMiddle().Column(column =>
            {
                column.Item().AlignRight().Text("USO CLÍNICO").FontSize(6.5f).Bold().FontColor("#99F6E4");
                column.Item().PaddingTop(4).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                    .FontSize(7.5f).FontColor(Colors.White);
            });
        });
    }

    private static void DatosGenerales(IContainer container, ExpedienteAlumnoDto alumno)
    {
        container.Border(1).BorderColor(Border).Background(Colors.White).Padding(12).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                DetailCell(table, "CURP", alumno.Curp ?? "No registrada");
                DetailCell(table, "Escuela", alumno.EscuelaNombre ?? "No registrada");
                DetailCell(table, "Grupo", alumno.Grupo ?? "No registrado");
                DetailCell(table, "Grado", alumno.Grado?.ToString() ?? "No registrado");
            });
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Áreas de apoyo: ").SemiBold().FontColor(Teal);
                    text.Span(alumno.AreasAtencion.Count == 0 ? "Ninguna registrada" : string.Join(", ", alumno.AreasAtencion));
                });
            });
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Discapacidades: ").SemiBold().FontColor(Teal);
                    text.Span(alumno.Discapacidades.Count == 0 ? "Ninguna registrada" : string.Join(", ", alumno.Discapacidades));
                });
            });
        });
    }

    private static void DetailCell(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingBottom(6).Column(column =>
        {
            column.Item().Text(label.ToUpperInvariant()).FontSize(6.5f).Bold().FontColor("#64748B");
            column.Item().PaddingTop(2).Text(value).FontSize(9).FontColor(Navy);
        });
    }

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Border(1).BorderColor(Border).Background(Colors.White).Column(column =>
        {
            column.Item().Background(LightTeal).BorderBottom(1).BorderColor("#A7F3D0")
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
            .Text("Documento confidencial de uso clínico/pedagógico interno. No debe compartirse fuera del equipo de atención sin autorización.")
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
        "BORRADOR" => "Borrador",
        "EN_REVISION" => "En revisión",
        "PROGRAMADA" => "Programada",
        "REALIZADA" => "Realizada",
        "CANCELADA" => "Cancelada",
        "COMPLETADO" or "COMPLETADA" => "Completado",
        "EVALUADO" => "Evaluado",
        "LEVE" => "Leve",
        "MODERADO" => "Moderado",
        "SIGNIFICATIVO" => "Significativo",
        _ => status?.Replace('_', ' ') ?? "Sin estado"
    };
}
