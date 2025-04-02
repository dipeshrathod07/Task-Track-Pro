using System.Data;
using Npgsql;
using NpgsqlTypes;
// 
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class UserRepository : IUserInterface
    {
        private readonly NpgsqlConnection _con;

        public UserRepository(NpgsqlConnection connection)
        {
            _con = connection;
        }

        #region Login
        public async Task<User?> Login(LoginVM model)
        {
            var query = @"
            SELECT c_userid, c_role, c_first_name, c_last_name, c_email, c_password, c_address, c_contact, c_gender, c_image
            FROM ttp.t_user
            WHERE c_email = @c_email AND c_password = @c_password";

            try
            {
                await _con.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_email", model.Email ?? string.Empty);
                    cmd.Parameters.AddWithValue("@c_password", model.Password ?? string.Empty);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserId = reader.GetGuid("c_userid"),
                                Role = reader.GetChar("c_role"),
                                FirstName = reader.GetString("c_first_name"),
                                LastName = reader.GetString("c_last_name"),
                                Email = reader.GetString("c_email"),
                                Password = reader.GetString("c_password"),
                                Address = reader.IsDBNull("c_address") ? null : reader.GetString("c_address"),
                                Contact = reader.IsDBNull("c_contact") ? null : reader.GetString("c_contact"),
                                Gender = reader.IsDBNull("c_gender") ? null : reader.GetChar("c_gender"),
                                Image = reader.IsDBNull("c_image") ? null : reader.GetString("c_image")
                            };
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Login: {ex.Message}");
                return null;
            }
            finally
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync(); // Ensure the connection is closed properly
            }
        }
        #endregion

        #region Add
        public async Task<int> Add(User model)
        {
            string query = @"
            INSERT INTO ttp.t_user (
                c_role, c_first_name, c_last_name, c_email, c_password, 
                c_address, c_contact, c_gender, c_image
            ) VALUES (
                @c_role, @c_first_name, @c_last_name, @c_email, @c_password, 
                @c_address, @c_contact, @c_gender, @c_image
            )";

            string check_query = "SELECT * FROM ttp.t_user WHERE c_email = @c_email";
            
            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                await _con.OpenAsync();
                
                using (NpgsqlCommand cmd = new NpgsqlCommand(check_query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_email", model.Email ?? "");
                    if (cmd.ExecuteScalar() != null) return -1;
                }

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_role", (object?)model.Role ?? 'E');
                    cmd.Parameters.AddWithValue("@c_first_name", (object?)model.FirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_last_name", (object?)model.LastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_email", (object?)model.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_password", (object?)model.Password ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_address", (object?)model.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_contact", (object?)model.Contact ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_image", (object?)model.Image ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion


        #region Delete
        public async Task<int> Delete(string id)
        {
            const string query = @"
            DELETE FROM ttp.t_user
            WHERE c_userId = @c_userId
            ";
            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                await _con.OpenAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_userId", id);
                    await cmd.ExecuteNonQueryAsync();
                    return 1;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"[ERROR] UserRepository - Delete - {ex.Message}");
                return 0;
            }
            finally {
                await _con.CloseAsync();
            }
        }
        #endregion


        #region GetAll
        public async Task<List<User>> GetAll()
        {
            List<User> modelList = new List<User>();
            string query = "SELECT * FROM ttp.t_user";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();
                    
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    await _con.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            modelList.Add(new User
                            {
                                UserId = reader.GetGuid("c_userid"),
                                Role = reader.GetChar("c_role"),
                                FirstName = reader.GetString("c_first_name"),
                                LastName = reader.GetString("c_last_name"),
                                Email = reader.GetString("c_email"),
                                Password = reader.GetString("c_password"),
                                Address = reader.IsDBNull("c_address") ? null : reader.GetString("c_address"),
                                Contact = reader.IsDBNull("c_contact") ? null : reader.GetString("c_contact"),
                                Gender = reader.IsDBNull("c_gender") ? null : reader.GetChar("c_gender"),
                                Image = reader.IsDBNull("c_image") ? null : reader.GetString("c_image"),
                                CreatedAt = reader.GetDateTime("c_created_at"),
                                UpdatedAt = reader.GetDateTime("c_updated_at")
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetAll Error: {e.Message}");
            }
            finally
            {
                await _con.CloseAsync();
            }

            return modelList;
        }
        #endregion


        #region GetOne
        public async Task<User?> GetOne(string id)
        {
            string query = "SELECT * FROM ttp.t_user WHERE c_userid = @c_userid";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_userid", Guid.Parse(id));
                    await _con.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserId = reader.GetGuid("c_userid"),
                                Role = reader.GetChar("c_role"),
                                FirstName = reader.GetString("c_first_name"),
                                LastName = reader.GetString("c_last_name"),
                                Email = reader.GetString("c_email"),
                                Password = reader.GetString("c_password"),
                                Address = reader.IsDBNull("c_address") ? null : reader.GetString("c_address"),
                                Contact = reader.IsDBNull("c_contact") ? null : reader.GetString("c_contact"),
                                Gender = reader.IsDBNull("c_gender") ? null : reader.GetChar("c_gender"),
                                Image = reader.IsDBNull("c_image") ? null : reader.GetString("c_image"),
                                CreatedAt = reader.GetDateTime("c_created_at"),
                                UpdatedAt = reader.GetDateTime("c_updated_at")
                            };
                        }
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetOne Error: {e.Message}");
                return null;
            }
            finally
            {
                if (_con.State == System.Data.ConnectionState.Open)
                {
                    await _con.CloseAsync();
                }
            }
        }
        #endregion


        #region Update
        public async Task<int> Update(User model)
        {
            string query = @"
            UPDATE ttp.t_user 
            SET c_first_name = @c_first_name,
                c_last_name = @c_last_name,
                c_email = @c_email,
                c_image = @c_image
            WHERE c_userid = @c_userid";

            try
            {
                if (_con.State == ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_userid", (object?)model.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_first_name", (object?)model.FirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_last_name", (object?)model.LastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_email", (object?)model.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_image", (object?)model.Image ?? DBNull.Value);

                    await _con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                    return 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Update Error: {e.Message}");
                return 0;
            }
            finally
            {
                if (_con.State == System.Data.ConnectionState.Open)
                {
                    await _con.CloseAsync();
                }
            }
        }
        #endregion


        #region UpdatePassword
        public async Task<int> UpdatePassword(ChangePasswordVM model)
        {
            try
            {
                const string checkQuery = "SELECT c_password FROM ttp.t_user WHERE c_userid = @c_userid";
                const string updateQuery = "UPDATE ttp.t_user SET c_password = @c_password WHERE c_userid = @c_userid";

                if (_con.State == ConnectionState.Open)
                    await _con.CloseAsync();

                await _con.OpenAsync();

                await using (var checkCmd = new NpgsqlCommand(checkQuery, _con))
                {
                    checkCmd.Parameters.AddWithValue("@c_userid", (object?)model.UserId ?? DBNull.Value);

                    await using (var reader = await checkCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string oldPassword = reader.GetString(reader.GetOrdinal("c_password"));
                            if (oldPassword != model.OldPassword)
                            {
                                return -1;
                            }
                        }
                    }
                }

                await using (var updateCmd = new NpgsqlCommand(updateQuery, _con))
                {
                    updateCmd.Parameters.AddWithValue("@c_password", model.NewPassword ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@c_userid",  (object?)model.UserId ?? DBNull.Value);

                    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0 ? 1 : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdatePassword Error: {ex.Message}");
                return 0;
            }
            finally
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();
            }
        }
        #endregion

    }
}