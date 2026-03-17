using autofact.Models;
using MySqlConnector;

namespace autofact.Data
{
    /// <summary>
    /// Accès aux données pour les clients.
    /// </summary>
    internal class ClientRepository(Bdd db)
    {
        // ??????????????????????????????????????????????????????????????????????
        // LECTURE
        // ??????????????????????????????????????????????????????????????????????

        public async Task<List<Client>> TousAsync(string? recherche = null)
        {
            var list = new List<Client>();
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText =
                """
                SELECT id, nom, mail, telephone, adresse
                FROM   client
                WHERE  (@q IS NULL
                        OR nom       LIKE @q
                        OR mail      LIKE @q
                        OR telephone LIKE @q)
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

        public async Task<Client?> ParIdAsync(int id)
        {
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT id, nom, mail, telephone, adresse FROM client WHERE id = @id LIMIT 1";
            cmd.Parameters.AddWithValue("@id", id);

            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        // ??????????????????????????????????????????????????????????????????????
        // ÉCRITURE
        // ??????????????????????????????????????????????????????????????????????

        /// <summary>Insère un client et retourne son nouvel id.</summary>
        public async Task<int> CreerAsync(Client c)
        {
            var result = await db.ExecuteScalarAsync(
                """
                INSERT INTO client (nom, mail, telephone, adresse)
                VALUES (@nom, @mail, @tel, @adr);
                SELECT LAST_INSERT_ID();
                """,
                [
                    new MySqlParameter("@nom",  c.Nom.Trim()),
                    new MySqlParameter("@mail", c.Email.Trim()),
                    new MySqlParameter("@tel",  c.Telephone.Trim()),
                    new MySqlParameter("@adr",  c.Adresse.Trim())
                ]);
            return Convert.ToInt32(result);
        }

        /// <summary>Met à jour les champs d'un client existant.</summary>
        public async Task ModifierAsync(Client c)
        {
            await db.ExecuteNonQueryAsync(
                """
                UPDATE client
                SET    nom       = @nom,
                       mail      = @mail,
                       telephone = @tel,
                       adresse   = @adr
                WHERE  id = @id
                """,
                [
                    new MySqlParameter("@nom",  c.Nom.Trim()),
                    new MySqlParameter("@mail", c.Email.Trim()),
                    new MySqlParameter("@tel",  c.Telephone.Trim()),
                    new MySqlParameter("@adr",  c.Adresse.Trim()),
                    new MySqlParameter("@id",   c.Id)
                ]);
        }

        /// <summary>
        /// Supprime un client.
        /// Lance une exception si des factures lui sont rattachées (FK RESTRICT).
        /// </summary>
        public async Task SupprimerAsync(int id)
        {
            await db.ExecuteNonQueryAsync(
                "DELETE FROM client WHERE id = @id",
                [new MySqlParameter("@id", id)]);
        }

        // ?? Mapping ???????????????????????????????????????????????????????????
        private static Client Map(MySqlDataReader r) => new()
        {
            Id        = r.GetInt32(0),
            Nom       = r.IsDBNull(1) ? "" : r.GetString(1),
            Email     = r.IsDBNull(2) ? "" : r.GetString(2),
            Telephone = r.IsDBNull(3) ? "" : r.GetString(3),
            Adresse   = r.IsDBNull(4) ? "" : r.GetString(4)
        };
    }
}
