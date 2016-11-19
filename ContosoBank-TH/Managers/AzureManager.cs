using ContosoBank_TH.Models;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
namespace ContosoBank_TH.Managers
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<User> userTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("http://contosobanktable1.azurewebsites.net");
            this.userTable = this.client.GetTable<User>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstace
        {
            get
            {
                if(instance == null)
                {
                    instance = new AzureManager();
                }
                return instance;
            }
        }

        public async Task<List<User>> GetUsers()
        {
            return await this.userTable.ToListAsync();
        }
    }
}