using autofact.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Fonts;

namespace autofact.Services
{
    /// <summary>
    /// Generates professional PDF exports for invoices (factures), quotes (devis), and credit notes (avoirs).
    /// Uses PdfSharp for reliable, built-in PDF generation without external dependencies.
    /// </summary>
    internal static class PdfService
    {
        // Initialize font resolver on first use
        static PdfService()
        {
            try
            {
                // Use built-in font resolver for Windows
                if (GlobalFontSettings.FontResolver == null)
                    GlobalFontSettings.FontResolver = new PdfSharpDefaultFontResolver();
            }
            catch { /* Fallback if font resolver fails */ }
        }
        // ════════════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Generates PDF and saves to file.
        /// </summary>
        public static void GenererPdf(DocumentFacturation doc,
                                      InfosEntreprise entreprise,
                                      string cheminFichier)
        {
            using var pdf = CreatePdfDocument(doc, entreprise);
            pdf.Save(cheminFichier);
        }

        /// <summary>
        /// Generates PDF as byte array (useful for preview or email).
        /// </summary>
        public static byte[] GenererPdfBytes(DocumentFacturation doc, InfosEntreprise entreprise)
        {
            using var pdf = CreatePdfDocument(doc, entreprise);
            using var stream = new MemoryStream();
            pdf.Save(stream, false);
            return stream.ToArray();
        }

        // ════════════════════════════════════════════════════════════════════════
        // PDF DOCUMENT CREATION
        // ════════════════════════════════════════════════════════════════════════

        private static PdfDocument CreatePdfDocument(DocumentFacturation doc, InfosEntreprise ent)
        {
            var pdf = new PdfDocument();
            var page = pdf.AddPage();
            page.Width = XUnit.FromPoint(595);
            page.Height = XUnit.FromPoint(842);

            using var gfx = XGraphics.FromPdfPage(page);
            DrawPage(gfx, page, doc, ent);

            return pdf;
        }

        private static void DrawPage(XGraphics gfx, PdfPage page, DocumentFacturation doc, InfosEntreprise ent)
        {
            const float margin = 40;
            float y = margin;
            const float pageWidth = 595 - 80; // A4 width minus margins
            const float lineHeight = 14;

            // Colors (PdfSharp uses ARGB constructor: XColor(a, r, g, b) or predefined)
            var colorPrimary = XColors.RoyalBlue;
            var colorText = XColor.FromArgb(255, 26, 26, 46);
            var colorTextLight = XColor.FromArgb(255, 107, 114, 128);
            var colorBorder = XColor.FromArgb(255, 229, 231, 235);
            var colorBg = XColor.FromArgb(255, 248, 250, 252);

            // Fonts - use Arial as fallback (standard system font)
            var fontTitle = new XFont("Arial", 22, XFontStyleEx.Bold);
            var fontH2 = new XFont("Arial", 16, XFontStyleEx.Bold);
            var fontH3 = new XFont("Arial", 11, XFontStyleEx.Bold);
            var fontNormal = new XFont("Arial", 9);
            var fontSmall = new XFont("Arial", 7.5);

            var brushText = new XSolidBrush(colorText);
            var brushLight = new XSolidBrush(colorTextLight);
            var brushPrimary = new XSolidBrush(colorPrimary);
            var penBorder = new XPen(colorBorder, 0.5);
            var penPrimary = new XPen(colorPrimary, 1.5);
            var brushBg = new XSolidBrush(colorBg);

            // ─── HEADER ───────────────────────────────────────────────────────
            // Company name
            gfx.DrawString(ent.Nom, fontTitle, brushText, margin, y);
            y += 28;

            // Company details
            gfx.DrawString(ent.Adresse, fontSmall, brushLight, margin, y);
            y += lineHeight;
            gfx.DrawString($"Tél : {ent.Telephone} — {ent.Email}", fontSmall, brushLight, margin, y);
            y += lineHeight;
            if (!string.IsNullOrWhiteSpace(ent.Siret))
            {
                gfx.DrawString($"SIRET : {ent.Siret}", fontSmall, brushLight, margin, y);
                y += lineHeight;
            }

            y += 8;

            // Document type header on right
            string docTypeLabel = doc.Type switch
            {
                TypeDocument.Facture => "FACTURE",
                TypeDocument.Devis => "DEVIS",
                TypeDocument.Avoir => "AVOIR",
                _ => "DOCUMENT"
            };

            var docTypeSize = gfx.MeasureString(docTypeLabel, fontH2);
            gfx.DrawString(docTypeLabel, fontH2, brushPrimary, 555 - docTypeSize.Width, y);

            // Document number and dates
            y += 28;
            gfx.DrawString($"N° {doc.Numero}", fontH3, brushText, margin, y);
            y += 16;

            var rightX = 555 - 130;
            gfx.DrawString($"Date : {doc.DateEmission:dd/MM/yyyy}", fontNormal, brushLight, rightX, y);
            y += lineHeight;

            if (doc.DateEcheance.HasValue)
            {
                gfx.DrawString($"Échéance : {doc.DateEcheance:dd/MM/yyyy}", fontNormal, brushLight, rightX, y);
                y += lineHeight;
            }

            if (!string.IsNullOrWhiteSpace(doc.NumeroDocumentParent))
            {
                gfx.DrawString($"Ref. : {doc.NumeroDocumentParent}", fontNormal, brushLight, rightX, y);
                y += lineHeight;
            }

            y += 12;

            // ─── RECIPIENT BLOCK ──────────────────────────────────────────────
            var recipientRect = new XRect(margin, y, pageWidth, 40);
            gfx.DrawRectangle(brushBg, recipientRect);
            gfx.DrawRectangle(penBorder, recipientRect);
            gfx.DrawString("Destinataire", fontSmall, brushLight, margin + 6, y + 4);
            gfx.DrawString(doc.ClientNom, fontH3, brushText, margin + 6, y + 16);

            y += 65;

            // ─── LINE ITEMS TABLE ─────────────────────────────────────────────
            // Column layout (proper spacing for A4 page 595pt width, margins 40pt):
            // Usable width: 515pt (40 to 555)
            const float col1Start = margin;      // Description starts at 40
            const float col1Width = 245;         // Description: 40-285

            const float col2Start = 285;         // Qty starts at 285
            const float col2Width = 50;          // Qty: 285-335

            const float col3Start = 335;         // P.U. HT starts at 335
            const float col3Width = 70;          // P.U. HT: 335-405

            const float col4Start = 405;         // Remise starts at 405
            const float col4Width = 65;          // Remise: 405-470

            const float col5Start = 470;         // Montant HT starts at 470
            const float col5Width = 85;          // Montant HT: 470-555

            float tableY = y;
            float rowHeight = 20;

            // Header row background
            var headerRect = new XRect(margin, tableY, pageWidth, rowHeight);
            gfx.DrawRectangle(brushBg, headerRect);
            gfx.DrawRectangle(penBorder, headerRect);

            // Draw vertical lines between columns
            gfx.DrawLine(penBorder, col2Start, tableY, col2Start, tableY + rowHeight);
            gfx.DrawLine(penBorder, col3Start, tableY, col3Start, tableY + rowHeight);
            gfx.DrawLine(penBorder, col4Start, tableY, col4Start, tableY + rowHeight);
            gfx.DrawLine(penBorder, col5Start, tableY, col5Start, tableY + rowHeight);

            // Headers (left-aligned for text, right-aligned for numbers)
            gfx.DrawString("Désignation", fontH3, brushLight, col1Start + 4, tableY + 12);
            gfx.DrawString("Qté", fontH3, brushLight, col2Start + col2Width - 8, tableY + 5, XStringFormats.TopRight);
            gfx.DrawString("P.U. HT", fontH3, brushLight, col3Start + col3Width - 4, tableY + 5, XStringFormats.TopRight);
            gfx.DrawString("Remise", fontH3, brushLight, col4Start + col4Width - 4, tableY + 5, XStringFormats.TopRight);
            gfx.DrawString("Montant HT", fontH3, brushLight, col5Start + col5Width - 4, tableY + 5, XStringFormats.TopRight);

            tableY += rowHeight + 1;

            // Data rows
            bool altRow = false;
            foreach (var ligne in doc.Lignes)
            {
                var rowRect = new XRect(margin, tableY, pageWidth, rowHeight);
                if (altRow)
                    gfx.DrawRectangle(new XSolidBrush(XColors.White), rowRect);
                else
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(255, 250, 251, 252)), rowRect);

                gfx.DrawRectangle(penBorder, rowRect);

                // Draw vertical column separators
                gfx.DrawLine(penBorder, col2Start, tableY, col2Start, tableY + rowHeight);
                gfx.DrawLine(penBorder, col3Start, tableY, col3Start, tableY + rowHeight);
                gfx.DrawLine(penBorder, col4Start, tableY, col4Start, tableY + rowHeight);
                gfx.DrawLine(penBorder, col5Start, tableY, col5Start, tableY + rowHeight);

                // Description (left-aligned, can wrap)
                var descRect = new XRect(col1Start + 4, tableY + 0, col1Width - 8, rowHeight - 4);
                gfx.DrawString(ligne.Designation, fontNormal, brushText, descRect, XStringFormats.TopLeft);

                // Quantity (right-aligned)
                var qtyStr = ligne.Quantite.ToString();
                var qtyRect = new XRect(col2Start + 2, tableY + 2, col2Width - 6, rowHeight - 4);
                gfx.DrawString(qtyStr, fontNormal, brushText, qtyRect, XStringFormats.TopRight);

                // Unit price (right-aligned)
                var priceStr = $"{ligne.PrixUnitaire:F2} €";
                var priceRect = new XRect(col3Start + 2, tableY + 2, col3Width - 6, rowHeight - 4);
                gfx.DrawString(priceStr, fontNormal, brushText, priceRect, XStringFormats.TopRight);

                // Discount (right-aligned)
                string remiseText = ligne.TauxRemise > 0
                    ? $"{ligne.TauxRemise:F0} %"
                    : "—";
                var remiseRect = new XRect(col4Start + 2, tableY + 2, col4Width - 6, rowHeight - 4);
                gfx.DrawString(remiseText, fontNormal, brushText, remiseRect, XStringFormats.TopRight);

                // Amount HT (right-aligned, bold)
                var amtStr = $"{ligne.MontantHT:F2} €";
                var amtRect = new XRect(col5Start + 2, tableY + 1, col5Width - 6, rowHeight - 4);
                gfx.DrawString(amtStr, fontNormal, brushText, amtRect, XStringFormats.TopRight);

                tableY += rowHeight + 1;
                altRow = !altRow;
            }

            y = tableY + 15;

            // ─── TOTALS ───────────────────────────────────────────────────────
            // Align with table columns for visual consistency
            float totalsEndX = col5Start + col5Width;  // 470 + 85 = 555 (right edge of Amount column)

            // Total HT
            gfx.DrawString("Total HT", fontH3, brushText, col1Start, y);
            gfx.DrawString($"{doc.TotalHT:F2} €", fontH3, brushText, totalsEndX - 4, y, XStringFormats.TopRight);
            y += lineHeight + 4;

            // TVA note
            gfx.DrawString("TVA non applicable – art. 293 B du CGI", fontSmall, brushLight, col1Start, y);
            y += lineHeight + 8;

            // Grand total (Net à payer) - full width box aligned with table
            var totalRect = new XRect(col1Start, y, totalsEndX - col1Start, 20);
            gfx.DrawRectangle(brushBg, totalRect);
            gfx.DrawRectangle(penPrimary, totalRect);

            gfx.DrawString("Net à payer", fontH3, brushPrimary, col1Start + 4, y + 12);
            gfx.DrawString($"{doc.TotalNet:F2} €", fontH3, brushPrimary, totalsEndX - 4, y + 5, XStringFormats.TopRight);

            y += 32;

            // ─── FOOTER ───────────────────────────────────────────────────────
            gfx.DrawLine(penBorder, margin, y, margin + pageWidth, y);
            y += 8;

            if (!string.IsNullOrWhiteSpace(ent.Iban))
            {
                gfx.DrawString($"Virement : {ent.Iban}", fontSmall, brushLight, margin, y);
                y += lineHeight;
            }

            gfx.DrawString("Merci de votre confiance.", fontSmall, brushLight, margin, y);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // COMPANY INFORMATION
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Company information displayed on every document.
    /// </summary>
    internal sealed class InfosEntreprise
    {
        public string Nom { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Siret { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
    }

    // ════════════════════════════════════════════════════════════════════════
    // FONT RESOLVER
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Custom font resolver for PdfSharp to use system fonts reliably.
    /// </summary>
    internal sealed class PdfSharpDefaultFontResolver : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            // Map font names to standard Windows fonts
            string mappedName = faceName switch
            {
                "Segoe UI" => "arial",
                "Calibri" => "arial",
                "Helvetica" => "arial",
                _ => faceName.ToLower()
            };

            try
            {
                // Try to get from system fonts directory
                string fontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                // Try common Windows font filenames
                string[] candidates = new[]
                {
                    Path.Combine(fontsPath, $"{mappedName}.ttf"),
                    Path.Combine(fontsPath, $"{mappedName}bd.ttf"),
                    Path.Combine(fontsPath, $"arial.ttf"),
                    Path.Combine(fontsPath, $"arialbd.ttf"),
                };

                foreach (var path in candidates)
                {
                    if (File.Exists(path))
                        return File.ReadAllBytes(path);
                }
            }
            catch { }

            // Fallback: return empty array (PdfSharp will use default)
            return Array.Empty<byte>();
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Map to standard font
            string faceName = familyName switch
            {
                "Segoe UI" or "Calibri" or "Helvetica" => "Arial",
                _ => familyName
            };

            return new FontResolverInfo(faceName, isBold, isItalic);
        }
    }
}
