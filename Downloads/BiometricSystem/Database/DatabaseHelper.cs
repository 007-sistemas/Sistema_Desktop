using System.Data.SQLite;
using BiometricSystem.Models;

namespace BiometricSystem.Database
{
    public class DatabaseHelper
    {
        private readonly string connectionString;

        public DatabaseHelper()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "biometric.db");
            connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            // Tabela de Funcionários com campos de sincronização
            string createEmployeesTable = @"
                CREATE TABLE IF NOT EXISTS Employees (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    CPF TEXT UNIQUE NOT NULL,
                    Email TEXT,
                    Position TEXT,
                    FingerprintTemplate BLOB NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    SyncedToCloud INTEGER NOT NULL DEFAULT 0,
                    LastSyncTime TEXT,
                    CloudId TEXT
                )";

            // Tabela de Registros de Ponto com campos de sincronização
            string createTimeRecordsTable = @"
                CREATE TABLE IF NOT EXISTS TimeRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EmployeeId INTEGER NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Notes TEXT,
                    SyncedToCloud INTEGER NOT NULL DEFAULT 0,
                    LastSyncTime TEXT,
                    CloudId TEXT,
                    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
                )";

            // Criar tabelas
            using (var cmd = new SQLiteCommand(createEmployeesTable, connection))
                cmd.ExecuteNonQuery();

            using (var cmd = new SQLiteCommand(createTimeRecordsTable, connection))
                cmd.ExecuteNonQuery();

            // Criar índices para melhor performance
            CreateIndexIfNotExists(connection, "idx_employees_cpf", "Employees", "CPF");
            CreateIndexIfNotExists(connection, "idx_employees_active", "Employees", "IsActive");
            CreateIndexIfNotExists(connection, "idx_employees_synced", "Employees", "SyncedToCloud");
            CreateIndexIfNotExists(connection, "idx_timerecords_employee", "TimeRecords", "EmployeeId");
            CreateIndexIfNotExists(connection, "idx_timerecords_timestamp", "TimeRecords", "Timestamp");
            CreateIndexIfNotExists(connection, "idx_timerecords_synced", "TimeRecords", "SyncedToCloud");
        }

        private void CreateIndexIfNotExists(SQLiteConnection connection, string indexName, string tableName, string columnName)
        {
            string query = $"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName}({columnName})";
            using var cmd = new SQLiteCommand(query, connection);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch { /* Índice já existe */ }
        }

        public bool AddEmployee(Employee employee)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = @"INSERT INTO Employees (Name, CPF, Email, Position, FingerprintTemplate, CreatedAt, IsActive) 
                                VALUES (@Name, @CPF, @Email, @Position, @Template, @CreatedAt, @IsActive)";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Name", employee.Name);
                cmd.Parameters.AddWithValue("@CPF", employee.CPF);
                cmd.Parameters.AddWithValue("@Email", employee.Email ?? "");
                cmd.Parameters.AddWithValue("@Position", employee.Position ?? "");
                cmd.Parameters.AddWithValue("@Template", employee.FingerprintTemplate ?? Array.Empty<byte>());
                cmd.Parameters.AddWithValue("@CreatedAt", employee.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@IsActive", employee.IsActive ? 1 : 0);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        public List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT * FROM Employees WHERE IsActive = 1 ORDER BY Name";
            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CPF = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Position = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    FingerprintTemplate = reader.IsDBNull(5) ? null : (byte[])reader[5],
                    CreatedAt = DateTime.Parse(reader.GetString(6)),
                    IsActive = reader.GetInt32(7) == 1
                });
            }

            return employees;
        }

        public Employee? FindEmployeeByFingerprint(byte[] template)
        {
            // Implementação simplificada - em produção, usar comparação de templates
            return null;
        }

        public bool RegisterTimeRecord(TimeRecord record)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = @"INSERT INTO TimeRecords (EmployeeId, Timestamp, Type, Notes, SyncedToCloud) 
                                VALUES (@EmployeeId, @Timestamp, @Type, @Notes, 0)";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@EmployeeId", record.EmployeeId);
                cmd.Parameters.AddWithValue("@Timestamp", record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@Type", record.Type);
                cmd.Parameters.AddWithValue("@Notes", record.Notes ?? "");

                int result = cmd.ExecuteNonQuery();
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao registrar ponto: {ex.Message}");
                return false;
            }
        }

        public List<TimeRecord> GetEmployeeTimeRecords(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var records = new List<TimeRecord>();

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT * FROM TimeRecords WHERE EmployeeId = @EmployeeId";
            if (startDate.HasValue)
                query += " AND Timestamp >= @StartDate";
            if (endDate.HasValue)
                query += " AND Timestamp <= @EndDate";
            query += " ORDER BY Timestamp DESC";

            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
            if (startDate.HasValue)
                cmd.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            if (endDate.HasValue)
                cmd.Parameters.AddWithValue("@EndDate", endDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                records.Add(new TimeRecord
                {
                    Id = reader.GetInt32(0),
                    EmployeeId = reader.GetInt32(1),
                    Timestamp = DateTime.Parse(reader.GetString(2)),
                    Type = reader.GetString(3),
                    Notes = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            return records;
        }

        /// <summary>
        /// Obtém todos os funcionários não sincronizados com a nuvem
        /// </summary>
        public List<Employee> GetUnsyncedEmployees()
        {
            var employees = new List<Employee>();

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT * FROM Employees WHERE SyncedToCloud = 0 AND IsActive = 1 ORDER BY CreatedAt DESC";
            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CPF = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Position = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    FingerprintTemplate = reader.IsDBNull(5) ? null : (byte[])reader[5],
                    CreatedAt = DateTime.Parse(reader.GetString(6)),
                    IsActive = reader.GetInt32(7) == 1
                });
            }

            return employees;
        }

        /// <summary>
        /// Obtém todos os registros de ponto não sincronizados com a nuvem
        /// </summary>
        public List<TimeRecord> GetUnsyncedTimeRecords()
        {
            var records = new List<TimeRecord>();

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT tr.* FROM TimeRecords tr
                WHERE tr.SyncedToCloud = 0
                ORDER BY tr.Timestamp DESC";
            
            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                records.Add(new TimeRecord
                {
                    Id = reader.GetInt32(0),
                    EmployeeId = reader.GetInt32(1),
                    Timestamp = DateTime.Parse(reader.GetString(2)),
                    Type = reader.GetString(3),
                    Notes = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            return records;
        }

        /// <summary>
        /// Marca um funcionário como sincronizado
        /// </summary>
        public bool MarkEmployeeAsSynced(int employeeId, string cloudId = null)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = @"UPDATE Employees 
                                SET SyncedToCloud = 1, LastSyncTime = @LastSyncTime, CloudId = @CloudId
                                WHERE Id = @Id";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", employeeId);
                cmd.Parameters.AddWithValue("@LastSyncTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@CloudId", cloudId ?? (object)DBNull.Value);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Marca um registro de ponto como sincronizado
        /// </summary>
        public bool MarkTimeRecordAsSynced(int timeRecordId, string cloudId = null)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = @"UPDATE TimeRecords 
                                SET SyncedToCloud = 1, LastSyncTime = @LastSyncTime, CloudId = @CloudId
                                WHERE Id = @Id";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", timeRecordId);
                cmd.Parameters.AddWithValue("@LastSyncTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@CloudId", cloudId ?? (object)DBNull.Value);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
