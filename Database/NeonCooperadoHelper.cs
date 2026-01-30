using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BiometricSystem.Models;

namespace BiometricSystem.Database
{
    /// <summary>
    /// Helper para consultar cooperados da tabela 'cooperados' do Neon
    /// Com Connection Pooling para evitar limite de conexões
    /// </summary>
        public class NeonCooperadoHelper
        {
            /// <summary>
            /// Valida senha na tabela managers (autenticação administrativa)
            /// </summary>
            public async Task<bool> ValidarManagerByPasswordAsync(string password)
            {
                NpgsqlConnection? connection = null;
                try
                {
                    connection = new NpgsqlConnection(_pooledConnectionString);
                    await connection.OpenAsync();
                    string query = @"SELECT COUNT(*) FROM managers WHERE password = @password";
                    using var cmd = new NpgsqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@password", password);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && int.TryParse(result.ToString(), out int count))
                    {
                        return count > 0;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"❌ Erro ao validar manager: {ex.Message}");
                    return false;
                }
                finally
                {
                    if (connection != null)
                    {
                        try { connection.Close(); connection.Dispose(); } catch { }
                    }
                }
            }
        private readonly string _connectionString;
        private static string GetLogPath()
        {
            string logRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(logRoot))
                logRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(logRoot))
                logRoot = "C:\\Temp";
            string logDir = System.IO.Path.Combine(logRoot, "BiometricSystem");
            System.IO.Directory.CreateDirectory(logDir);
            return System.IO.Path.Combine(logDir, "biometric_log.txt");
        }
        
        // Connection String com pooling configurado
        private readonly string _pooledConnectionString;

        public NeonCooperadoHelper(string connectionString)
        {
            _connectionString = connectionString;
            
            // Adicionar configurações de pooling à connection string
            // MaxPoolSize: limite máximo de conexões no pool (padrão: 20)
            // MinPoolSize: mínimo de conexões mantidas abertas (padrão: 1)
            // ConnectionIdleLifetime: segundos antes de fechar uma conexão ociosa (padrão: 300)
            _pooledConnectionString = $"{connectionString};MaxPoolSize=10;MinPoolSize=1;ConnectionIdleLifetime=60;";
        }

        private void Log(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(GetLogPath(), logMessage + Environment.NewLine);
                Debug.WriteLine(logMessage);
            }
            catch { }
        }

        /// <summary>
        /// Modelo para representar um cooperado
        /// </summary>
        public class Cooperado
        {
            public string Id { get; set; } = string.Empty;
            public string Nome { get; set; } = string.Empty;
            public string? Cpf { get; set; }
            public string? Email { get; set; }
            public string? Telefone { get; set; }
            public DateTime? CriadoEm { get; set; }
            public bool Ativo { get; set; } = true;

            public override string ToString()
            {
                return $"{Nome}";
            }
        }

        /// <summary>
        /// Salva uma biometria na tabela 'biometrias' do NEON
        /// </summary>
        public async Task<bool> SalvarBiometriaAsync(string cooperadoId, byte[] template, int fingerIndex = 0)
        {
            NpgsqlConnection connection = null;

            try
            {
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();

                // Gerar hash do template
                string hash = GerarHashBiometria(template);

                string query = @"
                    INSERT INTO biometrias (cooperado_id, finger_index, hash, template, created_at)
                    VALUES (@cooperado_id, @finger_index, @hash, @template, NOW())";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@cooperado_id", cooperadoId);
                cmd.Parameters.AddWithValue("@finger_index", fingerIndex);
                cmd.Parameters.AddWithValue("@hash", hash);
                cmd.Parameters.AddWithValue("@template", template);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                Debug.WriteLine($"✅ Biometria salva no NEON. Rows affected: {rowsAffected}");

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao salvar biometria no NEON: {ex.Message}");
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Gera um hash SHA256 do template biométrico
        /// </summary>
        private string GerarHashBiometria(byte[] template)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(template);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Busca todas as biometrias cadastradas no NEON
        /// </summary>
        public async Task<List<BiometriaRecord>> GetAllBiometriasAsync()
        {
            var biometrias = new List<BiometriaRecord>();
            NpgsqlConnection connection = null;

            try
            {
                Log("🔍 Iniciando busca de biometrias no NEON...");
                Log($"   Connection String: {_connectionString.Substring(0, Math.Min(80, _connectionString.Length))}...");
                
                try
                {
                    Log("   Criando objeto NpgsqlConnection...");
                    connection = new NpgsqlConnection(_pooledConnectionString);
                    Log("   ✓ Objeto NpgsqlConnection criado com sucesso");
                    
                    Log("   Abrindo conexão (OpenAsync)...");
                    await connection.OpenAsync();
                    Log("✅ Conexão com NEON aberta com sucesso");
                }
                catch (Exception connEx)
                {
                    Log($"❌ ERRO NA CONEXÃO: {connEx.Message}");
                    Log($"   Tipo: {connEx.GetType().Name}");
                    if (connEx.InnerException != null)
                        Log($"   Inner: {connEx.InnerException.Message}");
                    throw;
                }

                // Primeiro, verificar se a tabela existe e tem dados
                string checkQuery = "SELECT COUNT(*) FROM biometrias";
                try
                {
                    using var checkCmd = new NpgsqlCommand(checkQuery, connection);
                    checkCmd.CommandTimeout = 5; // Timeout de 5 segundos
                    var totalRecords = await checkCmd.ExecuteScalarAsync();
                    Log($"📊 Total de registros na tabela biometrias: {totalRecords}");
                }
                catch (Exception checkEx)
                {
                    Log($"⚠️ Erro ao verificar tabela: {checkEx.Message}");
                }

                string query = @"
                    SELECT b.id, b.cooperado_id, b.finger_index, b.template, c.name
                    FROM biometrias b
                    INNER JOIN cooperados c ON b.cooperado_id = c.id
                    ORDER BY b.created_at_db DESC
                    LIMIT 500";

                Log($"📋 Executando query principal...");
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.CommandTimeout = 15; // Timeout de 15 segundos
                using var reader = await cmd.ExecuteReaderAsync();

                int count = 0;
                while (await reader.ReadAsync())
                {
                    count++;
                    var biometria = new BiometriaRecord
                    {
                        Id = reader.GetString(0),
                        CooperadoId = reader.GetString(1),
                        FingerIndex = reader.GetInt32(2),
                        Template = (byte[])reader.GetValue(3),
                        CooperadoNome = reader.GetString(4)
                    };
                    biometrias.Add(biometria);
                    Log($"   ✓ Biometria {count}: {biometria.CooperadoNome} (ID: {biometria.CooperadoId}, Template: {biometria.Template.Length} bytes)");
                }

                Log($"✅ {biometrias.Count} biometrias carregadas do NEON");
                return biometrias;
            }
            catch (Exception ex)
            {
                Log($"❌ Erro ao buscar biometrias: {ex.Message}");
                Log($"   Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Log($"   Inner: {ex.InnerException.Message}");
                }
                return biometrias;
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Registra um ponto (entrada/saída) na tabela 'pontos' do NEON
        /// </summary>
        public async Task<bool> RegistrarPontoAsync(RegistroPonto ponto)
        {
            NpgsqlConnection? connection = null;

            try
            {
                Log($"📤 Iniciando registro de ponto no NEON: {ponto.CooperadoNome} - {ponto.Tipo}");
                
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();

                // Query compatível com o backend web (ordem e campos usados lá)
                string query = @"
                    INSERT INTO pontos (
                        id, codigo, cooperado_id, cooperado_nome, date, tipo, entrada, saida,
                        hospital_id, setor_id, biometria_entrada_hash, biometria_saida_hash, timestamp,
                        related_id, status, is_manual, local, validado_por, rejeitado_por, motivo_rejeicao, observacao
                    ) VALUES (
                        @id, @codigo, @cooperado_id, @cooperado_nome, @date, @tipo, @entrada, @saida,
                        @hospital_id, @setor_id, @biometria_entrada_hash, @biometria_saida_hash, @timestamp,
                        @related_id, @status, @is_manual, @local, @validado_por, @rejeitado_por, @motivo_rejeicao, @observacao
                    )";

                Log($"   SQL Insert ponto id={ponto.Id}, codigo={ponto.Codigo}, coop={ponto.CooperadoId}, tipo={ponto.Tipo}, hosp={ponto.HospitalId}, setor={ponto.SetorId}");
                
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", ponto.Id);
                cmd.Parameters.AddWithValue("@codigo", ponto.Codigo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cooperado_id", ponto.CooperadoId);
                cmd.Parameters.AddWithValue("@cooperado_nome", ponto.CooperadoNome);
                cmd.Parameters.AddWithValue("@date", ponto.Timestamp.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@tipo", ponto.Tipo);
                cmd.Parameters.AddWithValue("@entrada", ponto.Entrada ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@saida", ponto.Saida ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@hospital_id", (object?)ponto.HospitalId ?? DBNull.Value);

                // Setor pode ser string ou número; o web aceita texto. Tentar converter para int se vier como número.
                object setorParam;
                if (int.TryParse(ponto.SetorId ?? string.Empty, out var setorInt))
                    setorParam = setorInt;
                else if (ponto.SetorId != null)
                    setorParam = ponto.SetorId;
                else
                    setorParam = DBNull.Value;
                cmd.Parameters.AddWithValue("@setor_id", setorParam);

                cmd.Parameters.AddWithValue("@biometria_entrada_hash", ponto.BiometriaEntradaHash ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@biometria_saida_hash", ponto.BiometriaSaidaHash ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@timestamp", ponto.Timestamp.ToString("O"));
                cmd.Parameters.AddWithValue("@related_id", ponto.RelatedId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", ponto.Status ?? "Aberto");
                cmd.Parameters.AddWithValue("@is_manual", ponto.IsManual);
                cmd.Parameters.AddWithValue("@local", ponto.Local ?? "");
                cmd.Parameters.AddWithValue("@validado_por", ponto.ValidadoPor ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@rejeitado_por", ponto.RejeitadoPor ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@motivo_rejeicao", ponto.MotivoRejeicao ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@observacao", ponto.Observacao ?? (object)DBNull.Value);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log($"   ✅ Ponto registrado no NEON (rows affected: {rowsAffected})");
                
                Debug.WriteLine($"✅ Ponto registrado no NEON. Tipo: {ponto.Tipo}, Cooperado: {ponto.CooperadoNome}");

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Log($"   ❌ ERRO ao registrar ponto: {ex.Message}");
                Log($"      Type: {ex.GetType().Name}");
                Log($"      Full Exception: {ex}");
                if (ex.InnerException != null)
                    Log($"      Inner: {ex.InnerException.Message}");
                
                Debug.WriteLine($"❌ Erro ao registrar ponto no NEON: {ex.Message}");
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Busca o último registro de ponto de um cooperado
        /// </summary>
        public async Task<PontoRecord?> GetUltimoPontoAsync(string cooperadoId)
        {
            NpgsqlConnection connection = null;

            try
            {
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT id, codigo, cooperado_id, cooperado_nome, timestamp, tipo, local
                    FROM pontos
                    WHERE cooperado_id = @cooperado_id
                    ORDER BY timestamp DESC
                    LIMIT 1";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@cooperado_id", cooperadoId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new PontoRecord
                    {
                        Id = reader.GetString(0),
                        Codigo = reader.GetString(1),
                        CooperadoId = reader.GetString(2),
                        CooperadoNome = reader.GetString(3),
                        Timestamp = reader.GetDateTime(4),
                        Tipo = reader.GetString(5),
                        Local = reader.GetString(6)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao buscar último ponto: {ex.Message}");
                return null;
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Modelo para representar uma biometria
        /// </summary>
        public class BiometriaRecord
        {
            public string Id { get; set; } = string.Empty;
            public string CooperadoId { get; set; } = string.Empty;
            public int FingerIndex { get; set; }
            public byte[] Template { get; set; } = Array.Empty<byte>();
            public string CooperadoNome { get; set; } = string.Empty;
        }

        /// <summary>
        /// Modelo para representar um registro de ponto
        /// </summary>
        public class PontoRecord
        {
            public string Id { get; set; } = string.Empty;
            public string Codigo { get; set; } = string.Empty;
            public string CooperadoId { get; set; } = string.Empty;
            public string CooperadoNome { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string Tipo { get; set; } = string.Empty;
            public string Local { get; set; } = string.Empty;
        }

        /// <summary>
        /// Obtém todos os cooperados cadastrados no NEON
        /// </summary>
        public async Task<List<Cooperado>> GetCooperadosAsync()
        {
            var cooperados = new List<Cooperado>();
            NpgsqlConnection connection = null;

            try
            {
                connection = new NpgsqlConnection(_pooledConnectionString);
                Debug.WriteLine($"🔌 Abrindo conexão NEON...");
                await connection.OpenAsync();
                Debug.WriteLine($"✅ Conexão aberta com sucesso!");

                // Primeiro, listar todas as tabelas para diagnóstico
                string listTablesQuery = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    LIMIT 20";
                
                using (var cmd = new NpgsqlCommand(listTablesQuery, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        Debug.WriteLine("📋 Tabelas disponíveis no NEON:");
                        while (await reader.ReadAsync())
                        {
                            Debug.WriteLine($"   - {reader.GetString(0)}");
                        }
                    }
                }

                // Tentar a query principal - usando nome correto das colunas do NEON
                string query = @"
                    SELECT id, name, cpf, email, phone
                    FROM cooperados
                    ORDER BY name ASC
                    LIMIT 1000";

                Debug.WriteLine($"🔍 Executando query: {query.Trim()}");
                
                using var cmd2 = new NpgsqlCommand(query, connection);
                using var reader2 = await cmd2.ExecuteReaderAsync();

                while (await reader2.ReadAsync())
                {
                    cooperados.Add(new Cooperado
                    {
                        Id = reader2.IsDBNull(0) ? string.Empty : reader2.GetString(0),
                        Nome = reader2.IsDBNull(1) ? string.Empty : reader2.GetString(1),
                        Cpf = reader2.IsDBNull(2) ? null : reader2.GetString(2),
                        Email = reader2.IsDBNull(3) ? null : reader2.GetString(3),
                        Telefone = reader2.IsDBNull(4) ? null : reader2.GetString(4),
                        CriadoEm = DateTime.Now,
                        Ativo = true
                    });
                }

                Debug.WriteLine($"✅ {cooperados.Count} cooperados carregados do NEON");
                return cooperados;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao obter cooperados do NEON: {ex.Message}");
                Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Debug.WriteLine($"Connection String (primeiros 60 chars): {_connectionString.Substring(0, Math.Min(60, _connectionString.Length))}...");
                return new List<Cooperado>();
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Obtém um cooperado específico pelo ID
        /// </summary>
        public async Task<Cooperado> GetCooperadoByIdAsync(string id)
        {
            NpgsqlConnection connection = null;

            try
            {
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT id, name, cpf, email, phone
                    FROM cooperados
                    WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new Cooperado
                    {
                        Id = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Cpf = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Telefone = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CriadoEm = DateTime.Now,
                        Ativo = true
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao obter cooperado {id}: {ex.Message}");
                return null;
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Pesquisa cooperados por nome
        /// </summary>
        public async Task<List<Cooperado>> SearchCooperadosAsync(string searchTerm)
        {
            var cooperados = new List<Cooperado>();
            NpgsqlConnection connection = null;

            try
            {
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT id, name, cpf, email, phone
                    FROM cooperados
                    WHERE 
                        name ILIKE @search OR 
                        cpf ILIKE @search OR 
                        email ILIKE @search
                    ORDER BY name ASC
                    LIMIT 100";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    cooperados.Add(new Cooperado
                    {
                        Id = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Cpf = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Telefone = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CriadoEm = DateTime.Now,
                        Ativo = true
                    });
                }

                return cooperados;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao pesquisar cooperados: {ex.Message}");
                return new List<Cooperado>();
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Testa a conexão com NEON e a tabela 'cooperados'
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            NpgsqlConnection connection = null;

            try
            {
                Debug.WriteLine($"🔍 Testando conexão com NEON...");
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();

                Debug.WriteLine($"✅ Conexão aberta com sucesso!");

                // Verificar se a tabela existe
                string query = @"
                    SELECT 1 FROM information_schema.tables 
                    WHERE table_name = 'cooperados' 
                    AND table_schema = 'public'";

                using var cmd = new NpgsqlCommand(query, connection);
                var result = await cmd.ExecuteScalarAsync();

                if (result != null)
                {
                    Debug.WriteLine("✅ Conexão com NEON bem-sucedida! Tabela 'cooperados' encontrada.");
                    
                    // Contar registros
                    string countQuery = "SELECT COUNT(*) FROM cooperados";
                    using var countCmd = new NpgsqlCommand(countQuery, connection);
                    var count = await countCmd.ExecuteScalarAsync();
                    Debug.WriteLine($"📊 Total de cooperados no NEON: {count}");
                    
                    return true;
                }
                else
                {
                    Debug.WriteLine("❌ Tabela 'cooperados' não encontrada no NEON");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao conectar com NEON: {ex.Message}");
                Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Modelo para representar um hospital
        /// </summary>
        public class Hospital
        {
            public string Id { get; set; } = string.Empty;
            public string Nome { get; set; } = string.Empty;
            public string Codigo { get; set; } = string.Empty;

            public override string ToString() => $"{Codigo} - {Nome}";
        }

        public class SetorInfo
        {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;

            public override string ToString() => Nome;
        }

        /// <summary>
        /// Busca todos os hospitais cadastrados (distintos, agrupados)
        /// </summary>
        public async Task<List<Hospital>> GetHospitaisAsync()
        {
            var hospitais = new List<Hospital>();
            NpgsqlConnection? connection = null;

            try
            {
                Log("🏥 Buscando hospitais no NEON...");
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();
                Log("   ✅ Conexão com NEON estabelecida");

                // Tabela hospitals: colunas reais (id, nome, slug, usuario_acesso, ...)
                // Usar nome como display e slug como código
                string query = "SELECT id, nome, slug FROM hospitals ORDER BY nome LIMIT 100";
                Log($"   📝 Query SQL: {query}");
                    
                using var cmd = new NpgsqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                Log("   🔍 Executando query...");
                int count = 0;
                
                while (await reader.ReadAsync())
                {
                    count++;
                    var hospital = new Hospital
                    {
                        Id = reader.GetString(0),           // id (text)
                        Nome = reader.GetString(1),         // nome (text)
                        Codigo = reader.IsDBNull(2) ? "" : reader.GetString(2) // slug (text)
                    };
                    hospitais.Add(hospital);
                    Log($"   🏥 Hospital #{count}: ID={hospital.Id}, Nome={hospital.Nome}, Codigo={hospital.Codigo}");
                }

                Log($"   ✅ Total: {hospitais.Count} hospitais encontrados");
            }
            catch (Exception ex)
            {
                Log($"   ❌ Erro ao buscar hospitais: {ex.Message}");
                Log($"   ❌ Stack trace: {ex.StackTrace}");
                Debug.WriteLine($"❌ Erro ao buscar hospitais: {ex.Message}");
                throw; // Re-throw para o formulário tratar
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }

            return hospitais;
        }

        /// <summary>
        /// Busca os setores de um hospital específico com ID e nome
        /// Salva no cache local para uso offline
        /// </summary>
        public async Task<List<SetorInfo>> GetSetoresDoHospitalAsync(string hospitalId, DatabaseHelper? localDb = null)
        {
            var setores = new List<SetorInfo>();
            NpgsqlConnection? connection = null;

            try
            {
                Log($"🏢 Buscando setores do hospital {hospitalId}...");
                connection = new NpgsqlConnection(_pooledConnectionString);
                await connection.OpenAsync();
                Log("   ✅ Conexão estabelecida");

                string query = @"
                    SELECT DISTINCT s.id, s.nome 
                    FROM hospital_setores hs
                    INNER JOIN setores s ON hs.setor_id = s.id
                    WHERE hs.hospital_id = @hospital_id
                    ORDER BY s.nome";
                    
                Log($"   📝 Query SQL: {query}");
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@hospital_id", hospitalId);
                
                using var reader = await cmd.ExecuteReaderAsync();
                int count = 0;

                while (await reader.ReadAsync())
                {
                    count++;
                    var setor = new SetorInfo
                    {
                        Id = reader.GetInt32(0),
                        Nome = reader.GetString(1)
                    };
                    setores.Add(setor);
                    Log($"   🏢 Setor #{count}: ID={setor.Id}, Nome={setor.Nome}");
                }

                Log($"   ✅ Total: {setores.Count} setores encontrados");

                // Salvar no cache local se DatabaseHelper foi fornecido
                if (localDb != null && setores.Any())
                {
                    var setoresParaSalvar = setores.Select(s => (s.Id, s.Nome)).ToList();
                    localDb.SalvarSetoresLocal(hospitalId, setoresParaSalvar);
                    Log($"   💾 Setores salvos no cache local");
                }
            }
            catch (Exception ex)
            {
                Log($"   ❌ Erro ao buscar setores: {ex.Message}");
                Log($"   ❌ Stack trace: {ex.StackTrace}");
                Debug.WriteLine($"❌ Erro ao buscar setores: {ex.Message}");
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch { }
                }
            }

            return setores;
        }
    }
}
