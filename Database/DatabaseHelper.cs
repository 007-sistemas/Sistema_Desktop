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
                    HospitalId TEXT,
                    SetorId INTEGER,
                    Status TEXT,
                    IsManual INTEGER NOT NULL DEFAULT 0,
                    RelatedId TEXT,
                    Date TEXT,
                    Entrada TEXT,
                    Saida TEXT,
                    Observacao TEXT,
                    BiometriaEntradaHash TEXT,
                    BiometriaSaidaHash TEXT,
                    SyncedToNeon INTEGER NOT NULL DEFAULT 0,
                    LastSyncTime TEXT
                )";

            // Tabela de Setores (cache local para uso offline)
            string createSetoresTable = @"
                CREATE TABLE IF NOT EXISTS Setores (
                    Id INTEGER PRIMARY KEY,
                    Nome TEXT NOT NULL,
                    HospitalId TEXT NOT NULL,
                    LastSyncTime TEXT NOT NULL
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

            using (var cmd = new SQLiteCommand(createSetoresTable, connection))
                cmd.ExecuteNonQuery();

            // Ajustar esquema legado para suportar hospital/setor e status (idempotente)
            EnsureColumnExists(connection, "Pontos", "HospitalId", "TEXT");
            EnsureColumnExists(connection, "Pontos", "SetorId", "INTEGER");
            EnsureColumnExists(connection, "Pontos", "Status", "TEXT");
            EnsureColumnExists(connection, "Pontos", "IsManual", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists(connection, "Pontos", "RelatedId", "TEXT");
            EnsureColumnExists(connection, "Pontos", "Date", "TEXT");
            EnsureColumnExists(connection, "Pontos", "Entrada", "TEXT");
            EnsureColumnExists(connection, "Pontos", "Saida", "TEXT");
            EnsureColumnExists(connection, "Pontos", "Observacao", "TEXT");
            EnsureColumnExists(connection, "Pontos", "BiometriaEntradaHash", "TEXT");
            EnsureColumnExists(connection, "Pontos", "BiometriaSaidaHash", "TEXT");

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
            
            // Índices para setores
            CreateIndexIfNotExists(connection, "idx_setores_hospital", "Setores", "HospitalId");
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

        private void EnsureColumnExists(SQLiteConnection connection, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                string checkQuery = $"PRAGMA table_info({tableName})";
                using var checkCmd = new SQLiteCommand(checkQuery, connection);
                using var reader = checkCmd.ExecuteReader();

                bool exists = false;
                while (reader.Read())
                {
                    if (reader[1].ToString()?.Equals(columnName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    string alter = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                    using var alterCmd = new SQLiteCommand(alter, connection);
                    alterCmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Silenciar para ambientes com SQLite antigo; não é crítico se já existir
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

        public bool SalvarPontoLocal(
            string cooperadoId,
            string cooperadoNome,
            string tipo,
            string local,
            string? hospitalId,
            int? setorId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string id = Guid.NewGuid().ToString();
                string? relatedId = null;

                // Normalizar tipo (web espera ENTRADA/SAIDA em maiúsculas)
                string tipoNormalizado = tipo.Equals("Saída", StringComparison.OrdinalIgnoreCase) ? "SAIDA" : tipo.Equals("Entrada", StringComparison.OrdinalIgnoreCase) ? "ENTRADA" : tipo.ToUpperInvariant();

                // Reutilizar código e vincular saída à última entrada
                var entradaAnterior = ObterUltimaEntrada(cooperadoId);
                string codigo;
                if (tipoNormalizado == "SAIDA" && entradaAnterior != null)
                {
                    codigo = entradaAnterior.Value.Codigo;
                    relatedId = entradaAnterior.Value.Id;
                }
                else
                {
                    codigo = Guid.NewGuid().ToString();
                }

                var agora = DateTime.Now;
                string timestampIso = agora.ToString("yyyy-MM-ddTHH:mm:sszzz");
                string data = agora.ToString("yyyy-MM-dd");
                string hora = agora.ToString("HH:mm");
                string? entrada = tipoNormalizado == "ENTRADA" ? hora : null;
                string? saida = tipoNormalizado == "SAIDA" ? hora : null;
                string status = tipoNormalizado == "ENTRADA" ? "Aberto" : "Fechado";

                string query = @"INSERT INTO Pontos (
                                    Id, Codigo, CooperadoId, CooperadoNome, Timestamp, Tipo, Local,
                                    HospitalId, SetorId, Status, IsManual, RelatedId, Date, Entrada, Saida,
                                    Observacao, BiometriaEntradaHash, BiometriaSaidaHash, SyncedToNeon)
                                VALUES (
                                    @Id, @Codigo, @CooperadoId, @CooperadoNome, @Timestamp, @Tipo, @Local,
                                    @HospitalId, @SetorId, @Status, @IsManual, @RelatedId, @Date, @Entrada, @Saida,
                                    @Observacao, @BiometriaEntradaHash, @BiometriaSaidaHash, 0)";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Codigo", codigo);
                cmd.Parameters.AddWithValue("@CooperadoId", cooperadoId);
                cmd.Parameters.AddWithValue("@CooperadoNome", cooperadoNome);
                cmd.Parameters.AddWithValue("@Timestamp", timestampIso);
                cmd.Parameters.AddWithValue("@Tipo", tipoNormalizado);
                cmd.Parameters.AddWithValue("@Local", local ?? "");
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SetorId", setorId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@IsManual", 0); // ponto biométrico
                cmd.Parameters.AddWithValue("@RelatedId", relatedId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Date", data);
                cmd.Parameters.AddWithValue("@Entrada", entrada ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Saida", saida ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Observacao", (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BiometriaEntradaHash", (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BiometriaSaidaHash", (object)DBNull.Value);

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

        private (string Id, string Codigo)? ObterUltimaEntrada(string cooperadoId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = @"SELECT Id, Codigo
                                FROM Pontos
                                WHERE CooperadoId = @CooperadoId AND Tipo = 'ENTRADA'
                                ORDER BY datetime(Timestamp) DESC
                                LIMIT 1";

                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@CooperadoId", cooperadoId);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return (reader.GetString(0), reader.GetString(1));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar última entrada: {ex.Message}");
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
        public class PontoLocal
        {
            public string Id { get; set; } = string.Empty;
            public string Codigo { get; set; } = string.Empty;
            public string CooperadoId { get; set; } = string.Empty;
            public string CooperadoNome { get; set; } = string.Empty;
            public string Tipo { get; set; } = string.Empty;
            public string Local { get; set; } = string.Empty;
            public string? HospitalId { get; set; }
            public int? SetorId { get; set; }
            public string? Status { get; set; }
            public bool IsManual { get; set; }
            public string? RelatedId { get; set; }
            public string? Date { get; set; }
            public string? Entrada { get; set; }
            public string? Saida { get; set; }
            public string? Observacao { get; set; }
            public string? BiometriaEntradaHash { get; set; }
            public string? BiometriaSaidaHash { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public List<PontoLocal> BuscarPontosNaoSincronizados()
        {
            var pontos = new List<PontoLocal>();
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = @"SELECT Id, Codigo, CooperadoId, CooperadoNome, Tipo, Local, HospitalId, SetorId, Status, IsManual,
                                        RelatedId, Date, Entrada, Saida, Observacao, BiometriaEntradaHash, BiometriaSaidaHash, Timestamp
                                FROM Pontos WHERE SyncedToNeon = 0 LIMIT 500";
                using var cmd = new SQLiteCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var ponto = new PontoLocal
                    {
                        Id = reader.GetString(0),
                        Codigo = reader.GetString(1),
                        CooperadoId = reader.GetString(2),
                        CooperadoNome = reader.GetString(3),
                        Tipo = reader.GetString(4),
                        Local = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        HospitalId = reader.IsDBNull(6) ? null : reader.GetString(6),
                        SetorId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                        Status = reader.IsDBNull(8) ? null : reader.GetString(8),
                        IsManual = !reader.IsDBNull(9) && reader.GetInt32(9) == 1,
                        RelatedId = reader.IsDBNull(10) ? null : reader.GetString(10),
                        Date = reader.IsDBNull(11) ? null : reader.GetString(11),
                        Entrada = reader.IsDBNull(12) ? null : reader.GetString(12),
                        Saida = reader.IsDBNull(13) ? null : reader.GetString(13),
                        Observacao = reader.IsDBNull(14) ? null : reader.GetString(14),
                        BiometriaEntradaHash = reader.IsDBNull(15) ? null : reader.GetString(15),
                        BiometriaSaidaHash = reader.IsDBNull(16) ? null : reader.GetString(16),
                        Timestamp = DateTime.TryParse(reader.GetString(17), out var ts) ? ts : DateTime.Now
                    };

                    pontos.Add(ponto);
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

        #region Setores Cache

        /// <summary>
        /// Salva/atualiza setores de um hospital no cache local
        /// </summary>
        public void SalvarSetoresLocal(string hospitalId, List<(int Id, string Nome)> setores)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                // Limpar setores antigos deste hospital
                string deleteQuery = "DELETE FROM Setores WHERE HospitalId = @HospitalId";
                using (var deleteCmd = new SQLiteCommand(deleteQuery, connection))
                {
                    deleteCmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                    deleteCmd.ExecuteNonQuery();
                }

                // Inserir novos setores
                string insertQuery = @"
                    INSERT INTO Setores (Id, Nome, HospitalId, LastSyncTime)
                    VALUES (@Id, @Nome, @HospitalId, @LastSyncTime)";

                string syncTime = DateTime.UtcNow.ToString("O");

                foreach (var setor in setores)
                {
                    using var insertCmd = new SQLiteCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("@Id", setor.Id);
                    insertCmd.Parameters.AddWithValue("@Nome", setor.Nome);
                    insertCmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                    insertCmd.Parameters.AddWithValue("@LastSyncTime", syncTime);
                    insertCmd.ExecuteNonQuery();
                }

                System.Diagnostics.Debug.WriteLine($"✅ {setores.Count} setores salvos localmente para hospital {hospitalId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao salvar setores localmente: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca setores de um hospital do cache local
        /// </summary>
        public List<(int Id, string Nome)> BuscarSetoresLocal(string hospitalId)
        {
            var setores = new List<(int Id, string Nome)>();

            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT Id, Nome FROM Setores WHERE HospitalId = @HospitalId ORDER BY Nome";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    setores.Add((
                        Id: reader.GetInt32(0),
                        Nome: reader.GetString(1)
                    ));
                }

                System.Diagnostics.Debug.WriteLine($"📂 {setores.Count} setores carregados do cache local");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao buscar setores locais: {ex.Message}");
            }

            return setores;
        }

        /// <summary>
        /// Verifica se existem setores no cache local para um hospital
        /// </summary>
        public bool TemSetoresLocal(string hospitalId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT COUNT(*) FROM Setores WHERE HospitalId = @HospitalId";
                using var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);

                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
