using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;

namespace PermissionSync.Database
{
    internal class DBConnectionManager
    {
        internal MySqlConnection CreateConnection()
        {
            MySqlConnection connection = null;

            try
            {
                if(PermissionSync.Instance.Configuration.Instance.DatabasePort == 0)
                {
                    PermissionSync.Instance.Configuration.Instance.DatabasePort = 3306;
                }
                connection = new MySqlConnection(
                $"SERVER={PermissionSync.Instance.Configuration.Instance.DatabaseAddress};DATABASE={PermissionSync.Instance.Configuration.Instance.DatabaseName};UID={PermissionSync.Instance.Configuration.Instance.DatabaseUsername};PASSWORD={PermissionSync.Instance.Configuration.Instance.DatabasePassword};PORT={PermissionSync.Instance.Configuration.Instance.DatabasePort};");
            }
            catch(Exception ex)
            {
                Logger.LogException(ex);
            }
            return connection;
        }

        /// <summary>
        /// Executes a MySql query.
        /// </summary>
        /// <param name="isScalar">If the query is expected to return a value.</param>
        /// <param name="query">The query to execute.</param>
        /// <returns>The value if isScalar is true, null otherwise.</returns>
        internal object ExecuteQuery(bool isScalar, string query)
        {
            // This method is to reduce the amount of copy paste that there was within this class.
            // Initiate result and connection globally instead of within TryCatch context.
            var connection = CreateConnection();
            object result = null;

            try
            {
                // Initialize command within try context, and execute within it as well.
                var command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();
                if (isScalar)
                    result = command.ExecuteScalar();
                else
                    command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Catch and log any errors during execution, like connection or similar.
                Logger.LogException(ex);
            }
            finally
            {
                // No matter what happens, close the connection at the end of execution.+
                connection.Close();
            }

            return result;
        }
    }
}
