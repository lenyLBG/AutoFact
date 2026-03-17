using autofact.Models;
using MySqlConnector;

namespace autofact.Data
{
    /// <summary>
    /// Accès aux données pour les codes promotionnels.
    /// </summary>
    internal class CodePromoRepository(Bdd db)
    {
        // ??????????????????????????????????????????????????????????????????????
        // LECTURE
        // ??????????????????????????????????????????????????????????????????????

        public async Task<List<CodePromo>> TousAsync(bool actifSeulement = false)
        {
            var list = new List<CodePromo>();
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                """
                SELECT id, code, taux_remise, date_expiration, actif
                FROM   code_promo
                WHERE  (@actifSeulement = 0 OR actif = 1)
                ORDER  BY code ASC
                """;
            cmd.Parameters.AddWithValue("@actifSeulement", actifSeulement ? 1 : 0);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(Map(r));

            return list;
        }

        /// <summary>Recherche un code actif et non expiré. Retourne null si invalide.</summary>
        public async Task<CodePromo?> TrouverActifAsync(string code)
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

            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        // ??????????????????????????????????????????????????????????????????????
        // ÉCRITURE
        // ??????????????????????????????????????????????????????????????????????

        public async Task<int> CreerAsync(CodePromo cp)
        {
            var result = await db.ExecuteScalarAsync(
                """
                INSERT INTO code_promo (code, taux_remise, date_expiration, actif)
                VALUES (@code, @taux, @exp, @actif);
                SELECT LAST_INSERT_ID();
                """,
                [
                    new MySqlParameter("@code",  cp.Code.Trim().ToUpperInvariant()),
                    new MySqlParameter("@taux",  cp.TauxRemise),
                    new MySqlParameter("@exp",   cp.DateExpiration as object ?? DBNull.Value),
                    new MySqlParameter("@actif", cp.Actif ? 1 : 0)
                ]);
            return Convert.ToInt32(result);
        }

        public async Task ModifierAsync(CodePromo cp)
        {
            await db.ExecuteNonQueryAsync(
                """
                UPDATE code_promo
                SET    code             = @code,
                       taux_remise      = @taux,
                       date_expiration  = @exp,
                       actif            = @actif
                WHERE  id = @id
                """,
                [
                    new MySqlParameter("@code",  cp.Code.Trim().ToUpperInvariant()),
                    new MySqlParameter("@taux",  cp.TauxRemise),
                    new MySqlParameter("@exp",   cp.DateExpiration as object ?? DBNull.Value),
                    new MySqlParameter("@actif", cp.Actif ? 1 : 0),
                    new MySqlParameter("@id",    cp.Id)
                ]);
        }

        public async Task SupprimerAsync(int id)
        {
            await db.ExecuteNonQueryAsync(
                "DELETE FROM code_promo WHERE id = @id",
                [new MySqlParameter("@id", id)]);
        }

        // ?? Mapping ???????????????????????????????????????????????????????????
        private static CodePromo Map(MySqlDataReader r) => new()
        {
            Id             = r.GetInt32(0),
            Code           = r.GetString(1),
            TauxRemise     = r.GetDecimal(2),
            DateExpiration = r.IsDBNull(3) ? null : r.GetDateTime(3),
            Actif          = r.GetBoolean(4)
        };
    }
}
