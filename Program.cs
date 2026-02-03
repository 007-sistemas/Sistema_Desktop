using BiometricSystem.Forms;
using Microsoft.Extensions.Configuration;

namespace BiometricSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();

                // Caminho seguro para configuração do usuário
                string? appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(appDataRoot))
                    appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(appDataRoot))
                    appDataRoot = "C:\\Temp";
                string appDataDir = Path.Combine(appDataRoot, "BiometricSystem");
                Directory.CreateDirectory(appDataDir);
                string appSettingsPath = Path.Combine(appDataDir, "appsettings.json");

                // Se não existir em %APPDATA%, copiar modelo do diretório do executável
                if (!File.Exists(appSettingsPath))
                {
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory ?? "C:\\Temp";
                    string modelPath = Path.Combine(exeDir, "appsettings.json");
                    if (File.Exists(modelPath))
                    {
<<<<<<< HEAD
                        try 
                        { 
                            File.Copy(modelPath, appSettingsPath, true);
                            System.Diagnostics.Debug.WriteLine($"✅ appsettings.json copiado de {modelPath} para {appSettingsPath}");
                        }
                        catch (Exception copyEx)
                        { 
                            System.Diagnostics.Debug.WriteLine($"❌ Erro ao copiar appsettings.json: {copyEx.Message}");
                            // Se falhar, cria um novo com a string padrão
                            File.WriteAllText(appSettingsPath, "{\"Neon\":{\"ConnectionString\":\"Host=ep-dry-dawn-ahl0dlm6-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_lOhyE4z1QBtc;SSL Mode=Require;Trust Server Certificate=true\"},\"Hospital\":{},\"Logging\":{\"LogLevel\":{\"Default\":\"Information\"}}}\n");
=======
                        try { File.Copy(modelPath, appSettingsPath, true); }
                        catch { /* Se falhar, cria um novo padrão mínimo */
                            File.WriteAllText(appSettingsPath, "{\"Neon\":{\"ConnectionString\":\"\"},\"Hospital\":{},\"Logging\":{\"LogLevel\":{\"Default\":\"Information\"}}}\n");
>>>>>>> 284890dc0bdfd4337dc1e38b5dd5d62aa158dfd5
                        }
                    }
                    else
                    {
<<<<<<< HEAD
                        File.WriteAllText(appSettingsPath, "{\"Neon\":{\"ConnectionString\":\"Host=ep-dry-dawn-ahl0dlm6-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_lOhyE4z1QBtc;SSL Mode=Require;Trust Server Certificate=true\"},\"Hospital\":{},\"Logging\":{\"LogLevel\":{\"Default\":\"Information\"}}}\n");
=======
                        File.WriteAllText(appSettingsPath, "{\"Neon\":{\"ConnectionString\":\"\"},\"Hospital\":{},\"Logging\":{\"LogLevel\":{\"Default\":\"Information\"}}}\n");
>>>>>>> 284890dc0bdfd4337dc1e38b5dd5d62aa158dfd5
                    }
                }

                // Carregar configurações do appsettings.json
                var config = new ConfigurationBuilder()
                    .AddJsonFile(Path.Combine(appDataDir, "appsettings.json"), optional: true, reloadOnChange: true)
                    .Build();

                // Verificar se o hospital está configurado
                var hospitalId = config["Hospital:Id"];
                if (string.IsNullOrEmpty(hospitalId))
                {
                    // Tentar mostrar tela de configuração de hospital
                    var neonConnectionString = config.GetConnectionString("DefaultConnection")
                        ?? config["Neon:ConnectionString"];

                    try
                    {
                        using (var configForm = new ConfigurarHospitalForm(neonConnectionString!))
                        {
                            if (configForm.ShowDialog() != DialogResult.OK)
                            {
                                // Usuário cancelou, fechar aplicação
                                return;
                            }
                        }

                        // Recarregar configuração após salvar
                        config = new ConfigurationBuilder()
                            .AddJsonFile(Path.Combine(appDataDir, "appsettings.json"), optional: true, reloadOnChange: true)
                            .Build();
                    }
                    catch (Exception configEx)
                    {
                        // Se falhar ao configurar, mostre o erro e permita continuar em modo offline
                        var result = MessageBox.Show(
                            $"Não foi possível conectar ao banco de dados:\n\n{configEx.Message}\n\n" +
                            "Deseja continuar em modo offline?\n\n" +
                            "Nota: Sem a configuração de hospital, o sistema funcionará apenas com o banco local.",
                            "Erro de Configuração",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );

                        if (result != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                }

                // Inicializa o banco SQLite no LocalApplicationData e aplica migrations
                // DatabaseInitialize removido: método não existe

                // Passar configuração para a form
                Application.Run(new LoginForm(config));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro fatal ao inicializar a aplicação:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Erro de Inicialização",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}

