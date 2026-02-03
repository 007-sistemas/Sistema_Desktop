using BiometricSystem.Database;
using System.Diagnostics;

namespace BiometricSystem.Services
{
    /// <summary>
    /// Gerencia sincronização periódica com Neon
    /// </summary>
    public class SyncService
    {
        private readonly DatabaseHelper _localDb;
        private readonly NeonHelper _neonDb;
        private System.Windows.Forms.Timer? _syncTimer;
        private bool _isSyncing = false;
        private int _syncFailureCount = 0;
        private const int MAX_RETRY_FAILURES = 5;

        public SyncService(DatabaseHelper localDb, string neonConnectionString)
        {
            _localDb = localDb;
            _neonDb = new NeonHelper(neonConnectionString);
        }

        /// <summary>
        /// Inicializa o serviço de sincronização automática
        /// </summary>
        public async void StartAutoSync(int intervalSeconds = 30)
        {
            // Testar conexão inicialmente
            bool canConnect = await _neonDb.TestConnectionAsync();
            if (!canConnect)
            {
                Debug.WriteLine("⚠️ Não foi possível conectar ao Neon. Sincronização desabilitada.");
                return;
            }

            _syncFailureCount = 0;

            // Sincronizar registros pendentes imediatamente
            await SyncNow();

            // Configurar timer para sincronização periódica (aumentado para 60s para evitar sobrecarga)
            _syncTimer = new System.Windows.Forms.Timer();
            _syncTimer.Interval = Math.Max(60, intervalSeconds) * 1000; // Mínimo 60s
            _syncTimer.Tick += async (s, e) => await SyncNow();
            _syncTimer.Start();

            Debug.WriteLine($"✅ SyncService iniciado. Sincronização a cada {Math.Max(60, intervalSeconds)}s");
        }

        /// <summary>
        /// Sincroniza registros pendentes imediatamente
        /// </summary>
        public async Task SyncNow()
        {
            if (_isSyncing)
            {
                Debug.WriteLine("⏳ Sincronização já em andamento...");
                return;
            }

            // Se muitas falhas, pausar sincronização
            if (_syncFailureCount >= MAX_RETRY_FAILURES)
            {
                Debug.WriteLine($"⚠️ Muito muitas falhas de sincronização ({_syncFailureCount}). Pausando por agora.");
                return;
            }

            _isSyncing = true;
            try
            {
                Debug.WriteLine("🔄 Iniciando sincronização com Neon...");
                await _neonDb.SyncPendingRecordsAsync(_localDb);
                _syncFailureCount = 0; // Reset contador de erros
                Debug.WriteLine("✅ Sincronização concluída com sucesso");
            }
            catch (Exception ex)
            {
                _syncFailureCount++;
                Debug.WriteLine($"❌ Erro durante sincronização (tentativa {_syncFailureCount}/{MAX_RETRY_FAILURES}): {ex.Message}");
                
                // Adicionar delay exponencial entre tentativas
                if (_syncFailureCount < MAX_RETRY_FAILURES)
                {
                    int delayMs = 2000 * (int)Math.Pow(2, Math.Min(_syncFailureCount - 1, 3)); // 2s, 4s, 8s, 16s
                    await Task.Delay(delayMs);
                }
            }
            finally
            {
                _isSyncing = false;
                // Adicionar pequeno delay entre sincronizações para evitar picos de conexão
                await Task.Delay(500);
            }
        }

        /// <summary>
        /// Para o serviço de sincronização automática
        /// </summary>
        public void StopAutoSync()
        {
            if (_syncTimer != null)
            {
                _syncTimer.Stop();
                _syncTimer.Dispose();
                Debug.WriteLine("🛑 SyncService parado");
            }
        }

        /// <summary>
        /// Obtém status de sincronização
        /// </summary>
        public (int pending, int synced) GetSyncStatus()
        {
            var pending = _localDb.GetUnsyncedTimeRecords().Count;
            return (pending, 0); // Pode-se implementar contagem de sincronizados depois
        }
    }
}
