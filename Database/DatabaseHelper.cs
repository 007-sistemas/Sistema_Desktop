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

            // Tabela de Biometrias do NEON (sincronização local)
            string createBiometriasTable = @"
                CREATE TABLE IF NOT EXISTS Biometrias (
                    Id TEXT PRIMARY KEY,
                    CooperadoId TEXT NOT NULL,
                    CooperadoNome TEXT NOT NULL,
                    FingerIndex INTEGER NOT NULL,
                    Hash TEXT,
                    Template BLOB NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    SyncedToNeon INTEGER NOT NULL DEFAULT 0,
                    LastSyncTime TEXT
                )";

            // Tabela de Pontos do NEON (sincronização local)
            string createPontosTable = @"
                CREATE TABLE IF NOT EXISTS Pontos (
                    Id TEXT PRIMARY KEY,
                    Codigo TEXT NOT NULL,
                    CooperadoId TEXT NOT NULL,
                    CooperadoNome TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Tipo TEXT NOT NULL,
                    Local TEXT,
                    SyncedToNeon INTEGER NOT NULL DEFAULT 0,
                    LastSyncTime TEXT
                )";

            // Criar tabelas
            using (var cmd = new SQLiteCommand(createEmployeesTable, connection))
                cmd.ExecuteNonQuery();

            using (var cmd = new SQLiteCommand(createTimeRecordsTable, connection))
                cmd.ExecuteNonQuery();

            using (var cmd = new SQLiteCommand(createBiometriasTable, connection))
                cmd.ExecuteNonQuery();

            using (var cmd = new SQLiteCommand(createPontosTable, connection))
                cmd.ExecuteNonQuery();

            // Criar índices para melhor performance
            CreateIndexIfNotExists(connection, "idx_employees_cpf", "Employees", "CPF");
            CreateIndexIfNotExists(connection, "idx_employees_active", "Employees", "IsActive");
            CreateIndexIfNotExists(connection, "idx_employees_synced", "Employees", "SyncedToCloud");
            CreateIndexIfNotExists(connection, "idx_timerecords_employee", "TimeRecords", "EmployeeId");
            CreateIndexIfNotExists(connection, "idx_timerecords_timestamp", "TimeRecords", "Timestamp");
            CreateIndexIfNotExists(connection, "idx_timerecords_synced", "TimeRecords", "SyncedToCloud");
            
            // Índices para biometrias e pontos
            CreateIndexIfNotExists(connection, "idx_biometrias_cooperado", "Biometrias", "CooperadoId");
            CreateIndexIfNotExists(connection, "idx_biometrias_synced", "Biometrias", "SyncedToNeon");
            CreateIndexIfNotExists(connection, "idx_pontos_cooperado", "Pontos", "CooperadoId");
            CreateIndexIfNotExists(connection, "idx_pontos_timestamp", "Pontos", "Timestamp");
            CreateIndexIfNotExists(connection, "idx_pontos_synced", "Pontos", "SyncedToNeon");
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

        public Employee? GetEmployeeById(int employeeId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT * FROM Employees WHERE Id = @Id AND IsActive = 1";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", employeeId);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new Employee
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        CPF = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Position = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        FingerprintTemplate = reader.IsDBNull(5) ? null : (byte[])reader[5],
                        CreatedAt = DateTime.Parse(reader.GetString(6)),
                        IsActive = reader.GetInt32(7) == 1
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
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

        // ==================== MÉTODOS PARA BIOMETRIAS ====================

        public bool SalvarBiometriaLocal(string cooperadoId, string cooperadoNome, byte[] template, string hash = "")
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string id = Guid.NewGuid().ToString();
                string query = @"INSERT INTO Biometrias (Id, CooperadoId, CooperadoNome, FingerIndex, Hash, Template, CreatedAt, SyncedToNeon) 
                                VALUES (@Id, @CooperadoId, @CooperadoNome, @FingerIndex, @Hash, @Template, @CreatedAt, 0)";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@CooperadoId", cooperadoId);
                cmd.Parameters.AddWithValue("@CooperadoNome", cooperadoNome);
                cmd.Parameters.AddWithValue("@FingerIndex", 0);
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Parameters.AddWithValue("@Template", template);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar biometria local: {ex.Message}");
                return false;
            }
        }

        public List<(string CooperadoId, string CooperadoNome, byte[] Template)> BuscarBiometriasLocais()
        {
            var biometrias = new List<(string CooperadoId, string CooperadoNome, byte[] Template)>();

            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT CooperadoId, CooperadoNome, Template FROM Biometrias ORDER BY CreatedAt DESC LIMIT 500";
                using var cmd = new SQLiteCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string cooperadoId = reader.GetString(0);
                    string cooperadoNome = reader.GetString(1);
                    byte[] template = (byte[])reader[2];
                    biometrias.Add((cooperadoId, cooperadoNome, template));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar biometrias locais: {ex.Message}");
            }

            return biometrias;
        }

        // ==================== MÉTODOS PARA PONTOS ====================

        public bool SalvarPontoLocal(string cooperadoId, string cooperadoNome, string tipo, string local)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string id = Guid.NewGuid().ToString();
                string codigo = Guid.NewGuid().ToString();
                string query = @"INSERT INTO Pontos (Id, Codigo, CooperadoId, CooperadoNome, Timestamp, Tipo, Local, SyncedToNeon) 
                                VALUES (@Id, @Codigo, @CooperadoId, @CooperadoNome, @Timestamp, @Tipo, @Local, 0)";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Codigo", codigo);
                cmd.Parameters.AddWithValue("@CooperadoId", cooperadoId);
                cmd.Parameters.AddWithValue("@CooperadoNome", cooperadoNome);
                cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@Tipo", tipo);
                cmd.Parameters.AddWithValue("@Local", local ?? "");

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar ponto local: {ex.Message}");
                return false;
            }
        }

        public string? GetUltimoPontoTipo(string cooperadoId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT Tipo FROM Pontos WHERE CooperadoId = @CooperadoId ORDER BY Timestamp DESC LIMIT 1";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@CooperadoId", cooperadoId);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return reader.GetString(0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar último ponto: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Busca biometrias que ainda não foram sincronizadas com NEON
        /// </summary>
        public List<(string Id, string CooperadoId, byte[] Template, int FingerIndex)> BuscarBiometriasNaoSincronizadas()
        {
            var biometrias = new List<(string Id, string CooperadoId, byte[] Template, int FingerIndex)>();
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT Id, CooperadoId, Template, FingerIndex FROM Biometrias WHERE SyncedToNeon = 0 LIMIT 500";
                using var cmd = new SQLiteCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string cooperadoId = reader.GetString(1);
                    byte[] template = (byte[])reader[2];
                    int fingerIndex = reader.GetInt32(3);

                    biometrias.Add((id, cooperadoId, template, fingerIndex));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar biometrias não sincronizadas: {ex.Message}");
            }

            return biometrias;
        }

        /// <summary>
        /// Marca uma biometria como sincronizada com NEON
        /// </summary>
        public bool MarcabiometriaComoSincronizada(string biometriaId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "UPDATE Biometrias SET SyncedToNeon = 1, LastSyncTime = @LastSyncTime WHERE Id = @Id";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", biometriaId);
                cmd.Parameters.AddWithValue("@LastSyncTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao marcar biometria como sincronizada: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Busca pontos que ainda não foram sincronizados com NEON
        /// </summary>
        public List<(string Id, string CooperadoId, string CooperadoNome, string Tipo, string Local)> BuscarPontosNaoSincronizados()
        {
            var pontos = new List<(string Id, string CooperadoId, string CooperadoNome, string Tipo, string Local)>();
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT Id, CooperadoId, CooperadoNome, Tipo, Local FROM Pontos WHERE SyncedToNeon = 0 LIMIT 500";
                using var cmd = new SQLiteCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string cooperadoId = reader.GetString(1);
                    string cooperadoNome = reader.GetString(2);
                    string tipo = reader.GetString(3);
                    string local = reader.GetString(4);

                    pontos.Add((id, cooperadoId, cooperadoNome, tipo, local));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar pontos não sincronizados: {ex.Message}");
            }

            return pontos;
        }

        /// <summary>
        /// Marca um ponto como sincronizado com NEON
        /// </summary>
        public bool MarcaPontoComoSincronizado(string pontoId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "UPDATE Pontos SET SyncedToNeon = 1, LastSyncTime = @LastSyncTime WHERE Id = @Id";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", pontoId);
                cmd.Parameters.AddWithValue("@LastSyncTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao marcar ponto como sincronizado: {ex.Message}");
                return false;
            }
        }
    }
}
