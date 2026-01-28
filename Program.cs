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

                // Carregar configurações do appsettings.json
                var config = new ConfigurationBuilder()
                    .SetBasePath(appDataDir)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
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
                            .SetBasePath(appDataDir)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
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
                DatabaseInitialize();

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
<<<<<<< HEAD
=======

        static void DatabaseInitialize()
        {
            using var context = new BiometricSystem.Database.AppDbContext();
            context.Database.Migrate();
        }
>>>>>>> baa86cf (Atualização do sistema: build limpo, banco limpo, config de publish e instalador ajustados)
    }
}

