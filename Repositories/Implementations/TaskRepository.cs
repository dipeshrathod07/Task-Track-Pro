using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class TaskRepository : ITaskInterface
    {
        private readonly NpgsqlConnection _con;

        public TaskRepository(NpgsqlConnection connection)
        {
            _con = connection;
        }

        #region Add
        public async Task<int> Add(Models.Task model)
        {
            Console.WriteLine(model);

            string query = @"
            INSERT INTO ttp.t_task (c_userid, c_title, c_description, c_estimated_days, c_start_date, c_end_date, c_status)
            VALUES (@c_userid, @c_title, @c_description, @c_estimated_days, @c_start_date, @c_end_date, @c_status)";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_userid", NpgsqlDbType.Uuid, model.UserId);
                    cmd.Parameters.AddWithValue("@c_title", model.Title);
                    cmd.Parameters.AddWithValue("@c_description", (object?)model.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_estimated_days", (object?)model.EstimatedDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_start_date", NpgsqlDbType.Date, model.StartDate);
                    cmd.Parameters.AddWithValue("@c_end_date", (object?)model.EndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_status", model.Status);

                    await _con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Add Task Error: {e.Message}");
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
            string query = "DELETE FROM ttp.t_task WHERE c_taskid = @c_taskid";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_taskid", Guid.Parse(id));
                    await _con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Delete Task Error: {e.Message}");
                return 0;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion

        #region GetAll
        public async Task<List<Models.Task>> GetAll()
        {
            List<Models.Task> modelList = new List<Models.Task>();
            string query = @"
            SELECT 
                t.*,
                u.c_role, u.c_first_name, u.c_last_name, u.c_email, 
                u.c_address, u.c_contact, u.c_gender, u.c_image,
                u.c_created_at as user_created_at, u.c_updated_at as user_updated_at
            FROM ttp.t_task t
            INNER JOIN ttp.t_user u ON t.c_userid = u.c_userid";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    await _con.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            // In GetAll method
                            modelList.Add(new Models.Task
                            {
                                TaskId = reader.GetGuid("c_taskid"),
                                UserId = reader.GetGuid("c_userid"),
                                Title = reader.GetString("c_title"),
                                Description = reader.IsDBNull("c_description") ? null : reader.GetString("c_description"),
                                EstimatedDays = reader.IsDBNull("c_estimated_days") ? null : reader.GetInt32("c_estimated_days"),
                                StartDate = reader.GetFieldValue<DateOnly>("c_start_date"),
                                EndDate = reader.IsDBNull("c_end_date") ? null : reader.GetFieldValue<DateOnly>("c_end_date"),
                                Status = reader.GetString("c_status"),
                                CreatedAt = reader.GetDateTime("c_created_at"),
                                UpdatedAt = reader.GetDateTime("c_updated_at"),
                                User = new Models.User
                                {
                                    UserId = reader.GetGuid("c_userid"),
                                    Role = reader.GetString("c_role")[0],
                                    FirstName = reader.GetString("c_first_name"),
                                    LastName = reader.GetString("c_last_name"),
                                    Email = reader.GetString("c_email"),
                                    Address = reader.IsDBNull("c_address") ? null : reader.GetString("c_address"),
                                    Contact = reader.IsDBNull("c_contact") ? null : reader.GetString("c_contact"),
                                    Gender = reader.IsDBNull("c_gender") ? null : reader.GetChar("c_gender"),
                                    Image = reader.IsDBNull("c_image") ? null : reader.GetString("c_image"),
                                    CreatedAt = reader.GetDateTime("user_created_at"),
                                    UpdatedAt = reader.GetDateTime("user_updated_at")
                                }
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

        #region GetAllByUser
        public async Task<List<Models.Task>> GetAllByUser(string id)
        {
            List<Models.Task> modelList = new List<Models.Task>();
            string query = @"
            SELECT 
                t.*,
                u.c_role, u.c_first_name, u.c_last_name, u.c_email, 
                u.c_address, u.c_contact, u.c_gender, u.c_image,
                u.c_created_at as user_created_at, u.c_updated_at as user_updated_at
            FROM ttp.t_task t
            INNER JOIN ttp.t_user u ON t.c_userid = u.c_userid
            WHERE
                t.c_userid = @c_userid";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    await _con.OpenAsync();

                    cmd.Parameters.AddWithValue("@c_userid", Guid.Parse(id));
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            // Same change in GetAllByUser method
                            modelList.Add(new Models.Task
                            {
                                TaskId = reader.GetGuid("c_taskid"),
                                UserId = reader.GetGuid("c_userid"),
                                Title = reader.GetString("c_title"),
                                Description = reader.IsDBNull("c_description") ? null : reader.GetString("c_description"),
                                EstimatedDays = reader.IsDBNull("c_estimated_days") ? null : reader.GetInt32("c_estimated_days"),
                                StartDate = reader.GetFieldValue<DateOnly>("c_start_date"),
                                EndDate = reader.IsDBNull("c_end_date") ? null : reader.GetFieldValue<DateOnly>("c_end_date"),
                                Status = reader.GetString("c_status"),
                                CreatedAt = reader.GetDateTime("c_created_at"),
                                UpdatedAt = reader.GetDateTime("c_updated_at"),
                                User = new Models.User
                                {
                                    UserId = reader.GetGuid("c_userid"),
                                    Role = reader.GetString("c_role")[0],
                                    FirstName = reader.GetString("c_first_name"),
                                    LastName = reader.GetString("c_last_name"),
                                    Email = reader.GetString("c_email"),
                                    Address = reader.IsDBNull("c_address") ? null : reader.GetString("c_address"),
                                    Contact = reader.IsDBNull("c_contact") ? null : reader.GetString("c_contact"),
                                    Gender = reader.IsDBNull("c_gender") ? null : reader.GetChar("c_gender"),
                                    Image = reader.IsDBNull("c_image") ? null : reader.GetString("c_image"),
                                    CreatedAt = reader.GetDateTime("user_created_at"),
                                    UpdatedAt = reader.GetDateTime("user_updated_at")
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetAllByUser Error: {e.Message}");
            }
            finally
            {
                await _con.CloseAsync();
            }

            return modelList;
        }
        #endregion

        #region GetOne
        public async Task<Models.Task?> GetOne(string id)
        {
            var query = @"
            SELECT 
                t.*,
                u.c_role, u.c_first_name, u.c_last_name, u.c_email, 
                u.c_address, u.c_contact, u.c_gender, u.c_image,
                u.c_created_at as user_created_at, u.c_updated_at as user_updated_at
            FROM ttp.t_task t
            INNER JOIN ttp.t_user u ON t.c_userid = u.c_userid
            WHERE t.c_taskid = @c_taskid;";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_taskid", Guid.Parse(id));
                    await _con.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            // Same change in GetOne method
                            return new Models.Task
                            {
                                TaskId = reader.GetGuid("c_taskid"),
                                UserId = reader.GetGuid("c_userid"),
                                Title = reader.GetString("c_title"),
                                Description = reader.IsDBNull("c_description") ? null : reader.GetString("c_description"),
                                EstimatedDays = reader.IsDBNull("c_estimated_days") ? null : reader.GetInt32("c_estimated_days"),
                                StartDate = reader.GetFieldValue<DateOnly>("c_start_date"),
                                EndDate = reader.IsDBNull("c_end_date") ? null : reader.GetFieldValue<DateOnly>("c_end_date"),
                                Status = reader.GetString("c_status"),
                                CreatedAt = reader.GetDateTime("c_created_at"),
                                UpdatedAt = reader.GetDateTime("c_updated_at"),
                                User = new Models.User
                                {
                                    UserId = reader.GetGuid("c_userid"),
                                    Role = reader.GetString("c_role")[0],
                                    FirstName = reader.GetString("c_first_name"),
                                    LastName = reader.GetString("c_last_name"),
                                    Email = reader.GetString("c_email"),
                                    Address = reader.IsDBNull("c_address") ? null : reader.GetString("c_address"),
                                    Contact = reader.IsDBNull("c_contact") ? null : reader.GetString("c_contact"),
                                    Gender = reader.IsDBNull("c_gender") ? null : reader.GetChar("c_gender"),
                                    Image = reader.IsDBNull("c_image") ? null : reader.GetString("c_image"),
                                    CreatedAt = reader.GetDateTime("user_created_at"),
                                    UpdatedAt = reader.GetDateTime("user_updated_at")
                                }
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
                await _con.CloseAsync();
            }
        }
        #endregion

        #region Update
        public async Task<int> Update(Models.Task model)
        {
            string query = @"
            UPDATE 
                ttp.t_task 
            SET
                c_userid = @c_userid, 
                c_title = @c_title, 
                c_description = @c_description, 
                c_estimated_days = @c_estimated_days, 
                c_start_date = @c_start_date, 
                c_end_date = @c_end_date, 
                c_status = @c_status
            WHERE 
                c_taskid = @c_taskid";

            try
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    await _con.CloseAsync();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@c_userid", model.UserId);
                    cmd.Parameters.AddWithValue("@c_title", model.Title);
                    cmd.Parameters.AddWithValue("@c_description", (object?)model.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_estimated_days", (object?)model.EstimatedDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_start_date", NpgsqlDbType.Date, model.StartDate);
                    cmd.Parameters.AddWithValue("@c_end_date", (object?)model.EndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c_status", model.Status);
                    cmd.Parameters.AddWithValue("@c_taskid", (object?)model.TaskId ?? DBNull.Value);

                    await _con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Update Error: {e.Message}");
                return 0;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion
    }
}