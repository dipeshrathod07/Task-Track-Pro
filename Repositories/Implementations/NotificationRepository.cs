using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class NotificationRepository : INotificationInterface
    {
        private readonly NpgsqlConnection _con;

        public NotificationRepository(NpgsqlConnection connection)
        {
            _con = connection;
        }

        #region GetAllByUserId
        public async Task<List<Notification>?> GetAllByUserId(Guid userId)
        {
            var notifications = new List<Notification>();
            var query = @"
                SELECT c_notificationid, c_userid, c_title, c_description, c_type, c_isread, c_created_at 
                FROM ttp.t_notification 
                WHERE c_userid = @userId
                ORDER BY c_created_at DESC";
            try
            {

                await _con.CloseAsync();
                await _con.OpenAsync();
                using var cmd = new NpgsqlCommand(query, _con);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = reader.GetInt32(0),
                        UserId = reader.GetGuid(1),
                        Title = reader.GetString(2),
                        Description = reader.GetString(3),
                        Type = reader.GetString(4),
                        IsRead = reader.GetBoolean(5),
                        CreatedAt = reader.GetDateTime(6)
                    });
                }

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationRepository - GetAllByUserId() - {ex.Message}");
                return null;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion

        #region GetAllUnreadNotifications
        public async Task<List<Notification>> GetAllUnreadNotifications()
        {
            var notifications = new List<Notification>();
            var query = @"
        SELECT c_notificationid, c_userid, c_title, c_description, c_type, c_isread, c_created_at 
        FROM ttp.t_notification 
        WHERE c_isread = false
        ORDER BY c_created_at DESC";

            try
            {
                await _con.CloseAsync();
                await _con.OpenAsync();
                using var cmd = new NpgsqlCommand(query, _con);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = reader.GetInt32(0),
                        UserId = reader.GetGuid(1),
                        Title = reader.GetString(2),
                        Description = reader.GetString(3),
                        Type = reader.GetString(4),
                        IsRead = reader.GetBoolean(5),
                        CreatedAt = reader.GetDateTime(6)
                    });
                }

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationRepository - GetAllUnreadNotifications() - {ex.Message}");
                return new List<Notification>();
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion


        #region GetAllByUnreadUserId
        public async Task<List<Notification>?> GetAllUnreadByUserId(Guid userId)
        {
            var notifications = new List<Notification>();
            var query = @"
                SELECT c_notificationid, c_userid, c_title, c_description, c_type, c_isread, c_created_at 
                FROM ttp.t_notification 
                WHERE c_userid = @userId AND c_isread = false
                ORDER BY c_created_at DESC";
            try
            {

                await _con.CloseAsync();
                await _con.OpenAsync();
                using var cmd = new NpgsqlCommand(query, _con);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = reader.GetInt32(0),
                        UserId = reader.GetGuid(1),
                        Title = reader.GetString(2),
                        Description = reader.GetString(3),
                        Type = reader.GetString(4),
                        IsRead = reader.GetBoolean(5),
                        CreatedAt = reader.GetDateTime(6)
                    });
                }

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationRepository - GetAllUnreadByUserId() - {ex.Message}");
                return null;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion


        #region MarkAsRead
        public async Task<bool> MarkAsRead(int notificationId)
        {
            var query = "UPDATE ttp.t_notification SET c_isread = true WHERE c_notificationid = @notificationId";

            try
            {
                await _con.CloseAsync();
                await _con.OpenAsync();
                using var cmd = new NpgsqlCommand(query, _con);
                cmd.Parameters.AddWithValue("@notificationId", notificationId);

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationRepository - MarkAsRead() - {ex.Message}");
                return false;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion

        #region MarkAllAsRead
        public async Task<bool> MarkAllAsRead(Guid userId)
        {
            var query = "UPDATE ttp.t_notification SET c_isread = true WHERE c_userid = @userId AND c_isread = false";

            try
            {
                await _con.OpenAsync();
                using var cmd = new NpgsqlCommand(query, _con);
                cmd.Parameters.AddWithValue("@userId", userId);

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationRepository - MarkAllAsRead() - {ex.Message}");
                return false;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion


        #region Add
        public async Task<int> Add(Notification notification)
        {
            var query = @"
                INSERT INTO ttp.t_notification (c_userid, c_title, c_description, c_type)
                VALUES (@userId, @title, @description, @type)
                RETURNING c_notificationid";

            try
            {
                await _con.OpenAsync();
                using var cmd = new NpgsqlCommand(query, _con);
                cmd.Parameters.AddWithValue("@userId", notification.UserId);
                cmd.Parameters.AddWithValue("@title", notification.Title);
                cmd.Parameters.AddWithValue("@description", notification.Description);
                cmd.Parameters.AddWithValue("@type", notification.Type);

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationRepository - Add() - {ex.Message}");
                return -1;
            }
            finally
            {
                await _con.CloseAsync();
            }
        }
        #endregion
    }
}