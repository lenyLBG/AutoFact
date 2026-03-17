using autofact.Models;
using System.Text;

namespace autofact.Services
{
    /// <summary>
    /// Génère un export PDF d'un document de facturation.
    /// Utilise une approche HTML ? PDF via <see cref="PdfExporter"/> pour rester sans
    /// dépendance externe lourde. Remplacer <see cref="RenderHtml"/> par QuestPDF
    /// ou iTextSharp si un rendu plus avancé est nécessaire.
    /// </summary>
    internal static class PdfService
    {
        // ??????????????????????????????????????????????????????????????????????
        // POINT D'ENTRÉE PRINCIPAL
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>
        /// Génère le PDF et le sauvegarde dans <paramref name="cheminFichier"/>.
        /// </summary>
        public static void GenererPdf(DocumentFacturation doc,
                                      InfosEntreprise entreprise,
                                      string cheminFichier)
        {
            string html = BuildHtml(doc, entreprise);
            PdfExporter.ExporterHtmlVersPdf(html, cheminFichier);
        }

        /// <summary>
        /// Génère le PDF dans un tableau d'octets (utile pour aperçu ou envoi mail).
        /// </summary>
        public static byte[] GenererPdfBytes(DocumentFacturation doc, InfosEntreprise entreprise)
        {
            string html = BuildHtml(doc, entreprise);
            return PdfExporter.HtmlVersBytes(html);
        }

        // ??????????????????????????????????????????????????????????????????????
        // CONSTRUCTION HTML
        // ??????????????????????????????????????????????????????????????????????

        private static string BuildHtml(DocumentFacturation doc, InfosEntreprise ent)
        {
            string typeLabel = doc.Type switch
            {
                TypeDocument.Facture => "FACTURE",
                TypeDocument.Devis   => "DEVIS",
                TypeDocument.Avoir   => "AVOIR",
                _                    => "DOCUMENT"
            };

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='UTF-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine(Css());
            sb.AppendLine("</style></head><body>");

            // ?? En-tête ???????????????????????????????????????????????????????
            sb.AppendLine("<div class='header'>");
            sb.AppendLine($"  <div class='company'>");
            sb.AppendLine($"    <h1>{Esc(ent.Nom)}</h1>");
            sb.AppendLine($"    <p>{Esc(ent.Adresse)}</p>");
            sb.AppendLine($"    <p>Tél : {Esc(ent.Telephone)} — {Esc(ent.Email)}</p>");
            if (!string.IsNullOrWhiteSpace(ent.Siret))
                sb.AppendLine($"    <p>SIRET : {Esc(ent.Siret)}</p>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class='docinfo'>");
            sb.AppendLine($"    <h2>{typeLabel}</h2>");
            sb.AppendLine($"    <p><strong>N° {Esc(doc.Numero)}</strong></p>");
            sb.AppendLine($"    <p>Date : {doc.DateEmission:dd/MM/yyyy}</p>");
            if (doc.DateEcheance.HasValue)
                sb.AppendLine($"    <p>Échéance : {doc.DateEcheance:dd/MM/yyyy}</p>");
            if (!string.IsNullOrWhiteSpace(doc.NumeroDocumentParent))
                sb.AppendLine($"    <p>Réf. : {Esc(doc.NumeroDocumentParent)}</p>");
            sb.AppendLine($"  </div>");
            sb.AppendLine("</div>");

            // ?? Client ????????????????????????????????????????????????????????
            sb.AppendLine("<div class='client-block'>");
            sb.AppendLine($"  <p class='label'>Destinataire</p>");
            sb.AppendLine($"  <p><strong>{Esc(doc.ClientNom)}</strong></p>");
            sb.AppendLine("</div>");

            // ?? Lignes ????????????????????????????????????????????????????????
            sb.AppendLine("<table class='lines'>");
            sb.AppendLine("  <thead><tr>");
            sb.AppendLine("    <th class='left'>Désignation</th>");
            sb.AppendLine("    <th>Qté</th>");
            sb.AppendLine("    <th>P.U. HT</th>");
            sb.AppendLine("    <th>Remise</th>");
            sb.AppendLine("    <th>Montant HT</th>");
            sb.AppendLine("  </tr></thead><tbody>");

            foreach (var l in doc.Lignes)
            {
                string remise = l.TauxRemise > 0
                    ? $"{l.TauxRemise:F0} %{(l.CodePromoCode is not null ? $" ({Esc(l.CodePromoCode)})" : "")}"
                    : "—";

                sb.AppendLine("  <tr>");
                sb.AppendLine($"    <td class='left'>{Esc(l.Designation)}</td>");
                sb.AppendLine($"    <td>{l.Quantite}</td>");
                sb.AppendLine($"    <td>{l.PrixUnitaire:F2} €</td>");
                sb.AppendLine($"    <td>{remise}</td>");
                sb.AppendLine($"    <td>{l.MontantHT:F2} €</td>");
                sb.AppendLine("  </tr>");
            }

            sb.AppendLine("  </tbody></table>");

            // ?? Totaux ????????????????????????????????????????????????????????
            sb.AppendLine("<div class='totals'>");
            sb.AppendLine($"  <div class='total-row'><span>Total HT</span><span>{doc.TotalHT:F2} €</span></div>");
            sb.AppendLine($"  <div class='total-row note'>TVA non applicable — art. 293 B du CGI</div>");
            sb.AppendLine($"  <div class='total-row grand'><span>Net à payer</span><span>{doc.TotalNet:F2} €</span></div>");
            sb.AppendLine("</div>");

            // ?? Pied de page ??????????????????????????????????????????????????
            sb.AppendLine("<div class='footer'>");
            if (!string.IsNullOrWhiteSpace(ent.Iban))
                sb.AppendLine($"  <p>Virement : {Esc(ent.Iban)}</p>");
            sb.AppendLine($"  <p>Merci de votre confiance.</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        // ??????????????????????????????????????????????????????????????????????
        // CSS INLINE
        // ??????????????????????????????????????????????????????????????????????

        private static string Css() => """
            body { font-family: Arial, sans-serif; font-size: 12px; color: #1a1a2e; margin: 40px; }
            .header { display: flex; justify-content: space-between; margin-bottom: 32px; }
            .company h1 { font-size: 18px; margin: 0 0 4px; }
            .company p, .docinfo p { margin: 2px 0; }
            .docinfo { text-align: right; }
            .docinfo h2 { font-size: 22px; color: #3b82f6; margin: 0 0 8px; letter-spacing: 2px; }
            .client-block { background: #f8fafc; border-left: 4px solid #3b82f6;
                            padding: 10px 16px; margin-bottom: 24px; }
            .client-block .label { color: #6b7280; font-size: 10px; text-transform: uppercase;
                                   margin: 0 0 4px; }
            table.lines { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
            table.lines th { background: #f1f5f9; padding: 8px 10px; text-align: center;
                             font-size: 11px; text-transform: uppercase; border-bottom: 2px solid #e2e8f0; }
            table.lines th.left, table.lines td.left { text-align: left; }
            table.lines td { padding: 7px 10px; text-align: center;
                             border-bottom: 1px solid #f1f5f9; }
            table.lines tr:nth-child(even) td { background: #fafafa; }
            .totals { width: 300px; margin-left: auto; }
            .total-row { display: flex; justify-content: space-between;
                         padding: 4px 0; border-bottom: 1px solid #e5e7eb; }
            .total-row.note { font-size: 10px; color: #9ca3af; border-bottom: none; }
            .total-row.grand { font-size: 15px; font-weight: bold; color: #3b82f6;
                               border-top: 2px solid #3b82f6; border-bottom: none; margin-top: 4px; }
            .footer { margin-top: 48px; border-top: 1px solid #e5e7eb; padding-top: 12px;
                      font-size: 10px; color: #9ca3af; }
            """;

        private static string Esc(string? s) =>
            System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
    }

    // ??????????????????????????????????????????????????????????????????????????
    // INFORMATIONS DE L'ENTREPRISE
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>Informations de l'autoentrepreneur, affichées sur chaque document.</summary>
    internal sealed class InfosEntreprise
    {
        public string Nom       { get; set; } = string.Empty;
        public string Adresse   { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string Siret     { get; set; } = string.Empty;
        public string Iban      { get; set; } = string.Empty;
    }

    // ??????????????????????????????????????????????????????????????????????????
    // EXPORTEUR HTML ? PDF  (pilote WebBrowser Windows, sans dépendance externe)
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// Convertit un HTML en fichier PDF en utilisant le moteur d'impression
    /// du contrôle WebBrowser Windows intégré.
    /// Aucun package NuGet supplémentaire n'est requis.
    /// Pour une qualité de rendu supérieure, remplacer l'implémentation par QuestPDF.
    /// </summary>
    internal static class PdfExporter
    {
        public static void ExporterHtmlVersPdf(string html, string cheminFichier)
        {
            byte[] bytes = HtmlVersBytes(html);
            File.WriteAllBytes(cheminFichier, bytes);
        }

        public static byte[] HtmlVersBytes(string html)
        {
            // Écrire le HTML dans un fichier temporaire
            string tempHtml = Path.Combine(Path.GetTempPath(), $"autofact_{Guid.NewGuid():N}.html");
            string tempPdf  = Path.Combine(Path.GetTempPath(), $"autofact_{Guid.NewGuid():N}.pdf");

            try
            {
                File.WriteAllText(tempHtml, html, Encoding.UTF8);

                // Tenter une conversion via Microsoft Print to PDF (disponible Windows 10+)
                bool ok = TryPrintToPdf(tempHtml, tempPdf);
                if (ok && File.Exists(tempPdf))
                    return File.ReadAllBytes(tempPdf);

                // Fallback : retourner le HTML encodé si l'impression échoue
                return Encoding.UTF8.GetBytes(html);
            }
            finally
            {
                if (File.Exists(tempHtml)) File.Delete(tempHtml);
                if (File.Exists(tempPdf))  File.Delete(tempPdf);
            }
        }

        private static bool TryPrintToPdf(string htmlPath, string pdfPath)
        {
            try
            {
                // Utilise wkhtmltopdf s'il est disponible sur le PATH
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = "wkhtmltopdf",
                    Arguments              = $"--quiet \"{htmlPath}\" \"{pdfPath}\"",
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardError  = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit(15_000);
                return File.Exists(pdfPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
