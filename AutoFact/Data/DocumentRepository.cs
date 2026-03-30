using autofact.Models;
using autofact.Services;
using MySqlConnector;

namespace autofact.Data
{
    /// <summary>
    /// Accès aux données pour les documents de facturation (devis, factures, avoirs)
    /// et les codes promo. S'appuie sur la couche <see cref="Bdd"/> existante.
    /// </summary>
    internal class DocumentRepository(Bdd db)
    {
        // ??????????????????????????????????????????????????????????????????????
        // NUMÉROTATION
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>Retourne le dernier index de séquence utilisé pour un type/année.</summary>
        public async Task<int> DernierIndexAsync(TypeDocument type, int annee)
        {
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                SELECT COALESCE(MAX(sequence_index), 0)
                FROM   document_facturation
                WHERE  type = @type AND YEAR(date_emission) = @annee
                """;
            cmd.Parameters.AddWithValue("@type",  type.ToString());
            cmd.Parameters.AddWithValue("@annee", annee);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        /// <summary>
        /// Génère le prochain numéro disponible pour un type de document.
        /// Exemple : "FAC-2024-0003"
        /// </summary>
        public async Task<string> ProchainNumeroAsync(TypeDocument type)
        {
            int index = await DernierIndexAsync(type, DateTime.Today.Year);
            return NumerotationService.Generer(type, DateTime.Today.Year, index);
        }

        // ??????????????????????????????????????????????????????????????????????
        // CRÉATION
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>
        /// Insère un document et toutes ses lignes dans une transaction atomique.
        /// Retourne l'id généré.
        /// </summary>
        public async Task<int> CreerDocumentAsync(DocumentFacturation doc)
        {
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Document principal
                await using var cmdDoc = conn.CreateCommand();
                cmdDoc.Transaction = tx;
                cmdDoc.CommandText =
                    """
                    INSERT INTO document_facturation
                        (numero, type, statut, date_emission, date_echeance,
                         client_id, numero_document_parent, sequence_index)
                    VALUES
                        (@numero, @type, @statut, @dateEmission, @dateEcheance,
                         @clientId, @parent, @seqIndex);
                    SELECT LAST_INSERT_ID();
                    """;

                int seqIndex = int.Parse(doc.Numero.Split('-').Last());
                cmdDoc.Parameters.AddWithValue("@numero",       doc.Numero);
                cmdDoc.Parameters.AddWithValue("@type",         doc.Type.ToString());
                cmdDoc.Parameters.AddWithValue("@statut",       doc.Statut.ToString());
                cmdDoc.Parameters.AddWithValue("@dateEmission", doc.DateEmission);
                cmdDoc.Parameters.AddWithValue("@dateEcheance", doc.DateEcheance as object ?? DBNull.Value);
                cmdDoc.Parameters.AddWithValue("@clientId",     doc.ClientId);
                cmdDoc.Parameters.AddWithValue("@parent",       doc.NumeroDocumentParent as object ?? DBNull.Value);
                cmdDoc.Parameters.AddWithValue("@seqIndex",     seqIndex);

                int docId = Convert.ToInt32(await cmdDoc.ExecuteScalarAsync());

                // 2. Lignes
                foreach (var ligne in doc.Lignes)
                {
                    await using var cmdLigne = conn.CreateCommand();
                    cmdLigne.Transaction = tx;
                    cmdLigne.CommandText =
                        """
                        INSERT INTO ligne_document
                            (document_id, prestation_id, designation,
                             quantite, prix_unitaire, code_promo_id, taux_remise)
                        VALUES
                            (@docId, @prestId, @desig,
                             @qte, @pu, @codePromoId, @taux)
                        """;
                    cmdLigne.Parameters.AddWithValue("@docId",       docId);
                    cmdLigne.Parameters.AddWithValue("@prestId",     ligne.PrestationId);
                    cmdLigne.Parameters.AddWithValue("@desig",       ligne.Designation);
                    cmdLigne.Parameters.AddWithValue("@qte",         ligne.Quantite);
                    cmdLigne.Parameters.AddWithValue("@pu",          ligne.PrixUnitaire);
                    cmdLigne.Parameters.AddWithValue("@codePromoId", ligne.CodePromoId as object ?? DBNull.Value);
                    cmdLigne.Parameters.AddWithValue("@taux",        ligne.TauxRemise);
                    await cmdLigne.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return docId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ??????????????????????????????????????????????????????????????????????
        // LECTURE
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>Charge les documents (sans leurs lignes) avec filtres optionnels.</summary>
        public async Task<List<DocumentFacturation>> ChargerDocumentsAsync(
            TypeDocument? filtre = null, int? annee = null, int? clientId = null)
        {
            var liste = new List<DocumentFacturation>();

            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                SELECT d.id, d.numero, d.type, d.statut,
                       d.date_emission, d.date_echeance,
                       d.client_id, c.nom AS client_nom,
                       d.numero_document_parent
                FROM   document_facturation d
                JOIN   client c ON c.id = d.client_id
                WHERE  (@type     IS NULL OR d.type      = @type)
                  AND  (@annee    IS NULL OR YEAR(d.date_emission) = @annee)
                  AND  (@clientId IS NULL OR d.client_id = @clientId)
                ORDER  BY d.date_emission DESC
                LIMIT  1000
                """;
            cmd.Parameters.AddWithValue("@type",     filtre.HasValue   ? filtre.Value.ToString() : DBNull.Value);
            cmd.Parameters.AddWithValue("@annee",    annee.HasValue    ? annee.Value             : DBNull.Value);
            cmd.Parameters.AddWithValue("@clientId", clientId.HasValue ? clientId.Value          : DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                liste.Add(new DocumentFacturation
                {
                    Id                   = reader.GetInt32(0),
                    Numero               = reader.GetString(1),
                    Type                 = Enum.Parse<TypeDocument>(reader.GetString(2)),
                    Statut               = Enum.Parse<StatutDocument>(reader.GetString(3)),
                    DateEmission         = reader.GetDateTime(4),
                    DateEcheance         = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    ClientId             = reader.GetInt32(6),
                    ClientNom            = reader.GetString(7),
                    NumeroDocumentParent = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }
            return liste;
        }

        /// <summary>Charge les lignes d'un document et les attache à l'objet.</summary>
        public async Task<List<LigneDocument>> ChargerLignesAsync(int documentId)
        {
            var lignes = new List<LigneDocument>();

            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                SELECT l.id, l.document_id, l.prestation_id, l.designation,
                       l.quantite, l.prix_unitaire,
                       l.code_promo_id, cp.code, l.taux_remise
                FROM   ligne_document l
                LEFT JOIN code_promo cp ON cp.id = l.code_promo_id
                WHERE  l.document_id = @docId
                ORDER  BY l.id
                """;
            cmd.Parameters.AddWithValue("@docId", documentId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lignes.Add(new LigneDocument
                {
                    Id            = reader.GetInt32(0),
                    DocumentId    = reader.GetInt32(1),
                    PrestationId  = reader.GetInt32(2),
                    Designation   = reader.GetString(3),
                    Quantite      = reader.GetInt32(4),
                    PrixUnitaire  = reader.GetDecimal(5),
                    CodePromoId   = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    CodePromoCode = reader.IsDBNull(7) ? null : reader.GetString(7),
                    TauxRemise    = reader.GetDecimal(8)
                });
            }
            return lignes;
        }

        // ????????????????????????????????????????????????????????????????????
        // MISE À JOUR DU STATUT
        // ????????????????????????????????????????????????????????????????????

        public async Task MettreAJourStatutAsync(int documentId, StatutDocument statut)
        {
            await db.ExecuteNonQueryAsync(
                "UPDATE document_facturation SET statut = @statut WHERE id = @id",
                [
                    new MySqlParameter("@statut", statut.ToString()),
                    new MySqlParameter("@id",     documentId)
                ]);
        }

        // ????????????????????????????????????????????????????????????????????
        // SUPPRESSION
        // ????????????????????????????????????????????????????????????????????

        /// <summary>
        /// Supprime un document et toutes ses lignes associées (transaction atomique).
        /// </summary>
        public async Task SupprimerDocumentAsync(int documentId)
        {
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Supprimer les lignes
                await using var cmdLignes = conn.CreateCommand();
                cmdLignes.Transaction = tx;
                cmdLignes.CommandText = "DELETE FROM ligne_document WHERE document_id = @docId";
                cmdLignes.Parameters.AddWithValue("@docId", documentId);
                await cmdLignes.ExecuteNonQueryAsync();

                // 2. Supprimer le document
                await using var cmdDoc = conn.CreateCommand();
                cmdDoc.Transaction = tx;
                cmdDoc.CommandText = "DELETE FROM document_facturation WHERE id = @docId";
                cmdDoc.Parameters.AddWithValue("@docId", documentId);
                await cmdDoc.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ????????????????????????????????????????????????????????????????????
        // CODES PROMO
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>
        /// Recherche un code promo actif et non expiré.
        /// Retourne null si introuvable ou expiré.
        /// </summary>
        public async Task<CodePromo?> TrouverCodePromoAsync(string code)
        {
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                SELECT id, code, taux_remise, date_expiration, actif
                FROM   code_promo
                WHERE  code = @code
                  AND  actif = 1
                  AND  (date_expiration IS NULL OR date_expiration >= CURDATE())
                LIMIT  1
                """;
            cmd.Parameters.AddWithValue("@code", code.Trim().ToUpperInvariant());

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new CodePromo
            {
                Id             = reader.GetInt32(0),
                Code           = reader.GetString(1),
                TauxRemise     = reader.GetDecimal(2),
                DateExpiration = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                Actif          = reader.GetBoolean(4)
            };
        }
    }
}
