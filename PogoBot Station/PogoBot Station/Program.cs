using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacoBot_Station
{
    internal class Program
    {
        private static void Main(string[] args) => new Program().MainSync().GetAwaiter().GetResult();

        public async Task MainSync()
        {
            PacoBot pacoBot = new PacoBot();
            await pacoBot.MainAsync();
        }
    }
}