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
                
                // Carregar configurações do appsettings.json
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
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
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
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

