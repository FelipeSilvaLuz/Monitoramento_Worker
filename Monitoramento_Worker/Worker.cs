using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitoramento_Worker.Model;
using Newtonsoft.Json;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Monitoramento_Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceConfigurations _serviceConfigurations;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            _serviceConfigurations = new ServiceConfigurations();
            new ConfigureFromConfigurationOptions<ServiceConfigurations>(
                configuration.GetSection("ServiceConfigurations")).Configure(_serviceConfigurations);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker executando em: {time}",
                    DateTimeOffset.Now);

                foreach (string host in _serviceConfigurations.Hosts)
                {
                    _logger.LogInformation(
                        $"Verificando a disponibilidade do host {host}");

                    var resultado = new ResultadoMonitoramento();
                    resultado.Horario =
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    resultado.Host = host;

                    try
                    {
                        using (Ping p = new Ping())
                        {
                            var resposta = p.Send(host);
                            resultado.Status = resposta.Status.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado.Status = "Exception";
                        resultado.Exception = ex;
                    }

                    string jsonResultado =
                        JsonConvert.SerializeObject(resultado);
                    if (resultado.Exception == null)
                        _logger.LogInformation(jsonResultado);
                    else
                        _logger.LogError(jsonResultado);
                }

                await Task.Delay(
                    _serviceConfigurations.Intervalo, stoppingToken);
            }
        }
    }
}