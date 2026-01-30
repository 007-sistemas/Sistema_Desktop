using Npgsql;
using BiometricSystem.Models;
using System.Diagnostics;

namespace BiometricSystem.Database
{
    public class NeonHelper
    {
        private readonly string _connectionString;
        private const string TableName = "pontos";

        public NeonHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Insere um registro de ponto na tabela 'pontos' do Neon
        /// </summary>
        public async Task<bool> InsertRegistroPontoAsync(RegistroPonto ponto)
        {
            NpgsqlConnection connection = null;
            try
            {
                connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = $@"
                    INSERT INTO {TableName} (
                        id, codigo, cooperado_id, cooperado_nome, timestamp, tipo, 
                        local, hospital_id, setor_id, observacao, related_id, 
                        status, is_manual, validado_por, rejeitado_por, motivo_rejeicao,
                        biometria_entrada_hash, biometria_saida_hash, created_at, updated_at
                    ) VALUES (
                        @id, @codigo, @cooperado_id, @cooperado_nome, @timestamp, @tipo,
                        @local, @hospital_id, @setor_id, @observacao, @related_id,
                        @status, @is_manual, @validado_por, @rejeitado_por, @motivo_rejeicao,
                        @biometria_entrada_hash, @biometria_saida_hash, @created_at, @updated_at
                    ) RETURNING id";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", ponto.Id);
                cmd.Parameters.AddWithValue("@codigo", ponto.Codigo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cooperado_id", ponto.CooperadoId);
                cmd.Parameters.AddWithValue("@cooperado_nome", ponto.CooperadoNome);
                cmd.Parameters.AddWithValue("@timestamp", ponto.Timestamp.ToUniversalTime().ToString("O")); // ISO 8601
                cmd.Parameters.AddWithValue("@tipo", ponto.Tipo);
                cmd.Parameters.AddWithValue("@local", ponto.Local ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@hospital_id", ponto.HospitalId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@setor_id", ponto.SetorId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@observacao", ponto.Observacao ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@related_id", ponto.RelatedId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", ponto.Status);
                cmd.Parameters.AddWithValue("@is_manual", ponto.IsManual);
                cmd.Parameters.AddWithValue("@validado_por", ponto.ValidadoPor ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@rejeitado_por", ponto.RejeitadoPor ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@motivo_rejeicao", ponto.MotivoRejeicao ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@biometria_entrada_hash", ponto.BiometriaEntradaHash ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@biometria_saida_hash", ponto.BiometriaSaidaHash ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);

                var result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao inserir ponto no Neon: {ex.Message}");
                return false;
            }
            finally
            {
                // Garantir que a conexão seja fechada e recursos liberados
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
        /// Sincroniza registros locais não sincronizados com o Neon
        /// </summary>
        public async Task SyncPendingRecordsAsync(DatabaseHelper localDb)
        {
            try
            {
                // Obter registros não sincronizados do SQLite
                var unsyncedRecords = localDb.GetUnsyncedTimeRecords();

                if (unsyncedRecords.Count == 0)
                {
                    Debug.WriteLine("✅ Nenhum registro pendente de sincronização");
                    return;
                }

                Debug.WriteLine($"⏳ Sincronizando {unsyncedRecords.Count} registros com Neon...");

                int syncedCount = 0;
                foreach (var record in unsyncedRecords)
                {
                    try
                    {
                        // Obter dados do funcionário para completar o ponto
                        var employee = localDb.GetEmployeeById(record.EmployeeId);
                        if (employee == null) continue;

                        // Converter TimeRecord para RegistroPonto
                        var ponto = new RegistroPonto
                        {
                            Id = Guid.NewGuid().ToString(),
                            Codigo = $"MAN-{record.Timestamp.Ticks}",
                            CooperadoId = employee.CPF, // Usar CPF como ID único
                            CooperadoNome = employee.Name,
                            Timestamp = record.Timestamp,
                            Tipo = record.Type.ToUpper() == "ENTRADA" ? "ENTRADA" : "SAIDA",
                            Local = record.Notes ?? "Desktop",
                            Observacao = $"Setor: {record.Notes}",
                            Status = "Aberto", // Inicia como aberto, será validado manualmente
                            IsManual = false, // Registrado por biometria
                            BiometriaEntradaHash = null // Sera preenchido se houver template
                        };

                        // Tentar inserir no Neon
                        if (await InsertRegistroPontoAsync(ponto))
                        {
                            // Marcar como sincronizado no SQLite
                            localDb.MarkTimeRecordAsSynced(record.Id, ponto.Id);
                            syncedCount++;
                            
                            // Adicionar pequeno delay entre inserções para não sobrecarregar o pool
                            await Task.Delay(200); // 200ms entre registros
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ Erro ao sincronizar registro individual: {ex.Message}");
                    }
                }

                Debug.WriteLine($"✅ {syncedCount} registros sincronizados com sucesso!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao sincronizar com Neon: {ex.Message}");
            }
        }

        /// <summary>
        /// Testa a conexão com Neon
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            NpgsqlConnection connection = null;
            try
            {
                connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                Debug.WriteLine("✅ Conexão com Neon bem-sucedida!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao conectar com Neon: {ex.Message}");
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
    }
}
