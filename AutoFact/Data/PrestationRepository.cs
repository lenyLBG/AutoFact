using autofact.Models;
using MySqlConnector;

namespace autofact.Data
{
    /// <summary>
    /// Accès aux données pour le catalogue de prestations.
    /// </summary>
    internal class PrestationRepository(Bdd db)
    {
        // ??????????????????????????????????????????????????????????????????????
        // LECTURE
        // ??????????????????????????????????????????????????????????????????????

        public async Task<List<Prestation>> ToutesAsync(string? recherche = null)
        {
            var list = new List<Prestation>();
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                """
                SELECT id, nom, type, prixUnitaire
                FROM   prestation
                WHERE  (@q IS NULL OR nom LIKE @q OR type LIKE @q)
                ORDER  BY nom ASC
                LIMIT  1000
                """;
            string? like = recherche is null ? null : $"%{recherche.Trim()}%";
            cmd.Parameters.AddWithValue("@q", like as object ?? DBNull.Value);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(Map(r));

            return list;
        }

        public async Task<Prestation?> ParIdAsync(int id)
        {
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT id, nom, type, prixUnitaire FROM prestation WHERE id = @id LIMIT 1";
            cmd.Parameters.AddWithValue("@id", id);

            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        // ??????????????????????????????????????????????????????????????????????
        // ÉCRITURE
        // ??????????????????????????????????????????????????????????????????????

        public async Task<int> CreerAsync(Prestation p)
        {
            var result = await db.ExecuteScalarAsync(
                """
                INSERT INTO prestation (nom, type, prixUnitaire)
                VALUES (@nom, @type, @prix);
                SELECT LAST_INSERT_ID();
                """,
                [
                    new MySqlParameter("@nom",  p.Nom.Trim()),
                    new MySqlParameter("@type", p.Type.Trim()),
                    new MySqlParameter("@prix", p.PrixUnitaire)
                ]);
            return Convert.ToInt32(result);
        }

        public async Task ModifierAsync(Prestation p)
        {
            await db.ExecuteNonQueryAsync(
                """
                UPDATE prestation
                SET    nom          = @nom,
                       type         = @type,
                       prixUnitaire = @prix
                WHERE  id = @id
                """,
                [
                    new MySqlParameter("@nom",  p.Nom.Trim()),
                    new MySqlParameter("@type", p.Type.Trim()),
                    new MySqlParameter("@prix", p.PrixUnitaire),
                    new MySqlParameter("@id",   p.Id)
                ]);
        }

        public async Task SupprimerAsync(int id)
        {
            await db.ExecuteNonQueryAsync(
                "DELETE FROM prestation WHERE id = @id",
                [new MySqlParameter("@id", id)]);
        }

        // ?? Mapping ???????????????????????????????????????????????????????????
        private static Prestation Map(MySqlDataReader r) => new()
        {
            Id           = r.GetInt32(0),
            Nom          = r.IsDBNull(1) ? "" : r.GetString(1),
            Type         = r.IsDBNull(2) ? "" : r.GetString(2),
            Description  = "",
            PrixUnitaire = r.IsDBNull(3) ? 0  : r.GetDecimal(3)
        };
    }
}
