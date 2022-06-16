using log4net;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Threading.Tasks;

namespace DataConnector.Intg.SocialMedia.Common
{    
    public class KeyVaultService
    {

        private ILog log;
        public KeyVaultService(ILog _log)
        {
            log = _log;
        }
        /// <summary>
        /// Get value from Azure KeyVault
        /// </summary>
        /// <param name="keyName">name of the key whom value need to be fetch</param>        
        /// <returns>secretValue</returns>
        public async Task<string> GetSecretValue(string keyName)
        {
            try
            {
                log.Info("AzureCommon GetSecretValue: Method Start");
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                log.Info("AzureCommon GetSecretValue: create keyvault client");
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                ////keyvault should be keyvault DNS Name  
                log.Info("AzureCommon GetSecretValue: get secret value based on pass keyName");
                var secretBundle = await keyVaultClient.GetSecretAsync(Environment.GetEnvironmentVariable("KeyVaultURL") + keyName).ConfigureAwait(false);
                log.Info("AzureCommon GetSecretValue: Method End");
                return secretBundle.Value;
            }
            catch (Exception ex)
            {
                log.Error("AzureCommon GetSecretValue: Exception found " + ex);
                throw;
            }
        }

    }
}
