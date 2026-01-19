using BiometricSystem.Forms;
using Microsoft.Extensions.Configuration;

namespace BiometricSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
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
                // Mostrar tela de configuração de hospital
                var neonConnectionString = config.GetConnectionString("DefaultConnection") 
                    ?? config["Neon:ConnectionString"];
                    
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

            // Passar configuração para a form
            Application.Run(new LoginForm(config));
        }
    }
}

